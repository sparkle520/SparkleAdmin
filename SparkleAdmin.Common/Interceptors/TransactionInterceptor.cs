using Castle.DynamicProxy;
using FreeRedis;
using SparkleAdmin.Common;
using SparkleAdmin.Common.Attributes;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using System;
using System.Transactions;

namespace SparkleAdmin.Common.Interceptors;

/// <summary>
/// 事务拦截器（处理 @Transactional 注解）
/// </summary>
public class TransactionInterceptor : Castle.DynamicProxy.IInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    // 通过构造函数注入服务提供器，用于获取仓储类
    public TransactionInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Intercept(IInvocation invocation)
    {
        // 1. 检查当前方法是否标记了 [Transactional] 特性
        var transactionAttr = invocation.Method.GetCustomAttributes(
            typeof(TransactionalAttribute), inherit: true)
            .FirstOrDefault() as TransactionalAttribute;

        if (transactionAttr == null)
        {
            // 无注解：直接执行方法，不开启事务
            invocation.Proceed();
            return;
        }

        // 2. 获取任意一个仓储实例（用于调用事务方法，此处以 SqlSugarRepository<object> 为例）
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetService<SqlSugarRepository<object>>();
        if (repo == null)
        {
            throw new InvalidOperationException("未找到 SqlSugarRepository 实例，无法开启事务");
        }

        try
        {
            // 3. 开启事务（根据注解配置选择“全局事务”或“当前库事务”）
            if (transactionAttr.IsGlobalTransaction)
                repo.BeginTran(); // 多租户全局事务
            else
                repo.CurrentBeginTran(); // 当前库事务

            // 4. 执行目标方法（即业务层方法，如“新增用户+新增角色”）
            invocation.Proceed();

            // 5. 方法无异常：提交事务
            if (transactionAttr.IsGlobalTransaction)
                repo.CommitTran();
            else
                repo.CurrentCommitTran();
        }
        catch (Exception ex)
        {
            // 6. 方法异常：回滚事务，并重新抛出异常
            if (transactionAttr.IsGlobalTransaction)
                repo.RollbackTran();
            else
                repo.CurrentRollbackTran();

            throw new InvalidOperationException($"事务执行失败，已回滚：{ex.Message}", ex);
        }
    }

    public void Before(InterceptorBeforeEventArgs args)
    {
        throw new NotImplementedException();
    }

    public void After(InterceptorAfterEventArgs args)
    {
        throw new NotImplementedException();
    }
}