using System;
using System.Transactions;

namespace SparkleAdmin.Common.Attributes;

/// <summary>
/// 事务注解（声明式事务特性）
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class TransactionalAttribute : Attribute
{
    /// <summary>
    /// 事务隔离级别（默认：可重复读）
    /// </summary>
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.RepeatableRead;

    /// <summary>
    /// 是否为多租户全局事务（默认：false，仅当前库）
    /// </summary>
    public bool IsGlobalTransaction { get; set; } = false;
}