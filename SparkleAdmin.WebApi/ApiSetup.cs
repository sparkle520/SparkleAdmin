﻿using System.Configuration;
using System.Reflection;
using Castle.DynamicProxy;
using SparkleAdmin.Common.Components;
using SparkleAdmin.Common.Interceptors; // 引入事务拦截器命名空间
using SparkleAdmin.WebApi.Middleware;
using Microsoft.AspNetCore.Builder;
using Simple.DynamicWebApi;
using Simple.DynamicWebApi.Extensions;
using SqlSugar.Extensions;

namespace SparkleAdmin.WebApi
{

    // 扩展方法：服务注册
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            //注册动态代理生成器和事务拦截器
            services.AddSingleton<IProxyGenerator, ProxyGenerator>();
            services.AddSingleton<TransactionInterceptor>();
            // 跨域处理
            services.AddCorsSetup();

            // 缓存
            services.AddCacheSetup();

            // 基础服务注册
            services.AddBaseServices();

            // 添加过滤器
            services.AddControllers(options =>
            {
                // 授权筛选器 
                // options.Filters.Add<CustomAuthorizationFilter>();
                // 全局异常过滤
                //options.Filters.Add<GlobalExceptionFilter>();
                // 日志过滤器
                //options.Filters.Add<RequestActionFilter>();
                // 结果过滤器
                options.Filters.Add<ResultFilter>();

            }).AddDataValidation();

            // 配置Json选项
            services.AddJsonOptions();

            // 添加sqlsugar
            services.AddSqlsugarSetup();

            // 添加swagger配置
            services.AddSwaggerBaseSetup();

            //自动注入
            var dllnames = new string[] { "SparkleAdmin.Servers" };
            services.AddAutoServicesWithInterceptors(dllnames);
            //services.AddAutoInjection(Assembly.GetExecutingAssembly());

            // 添加授权
            //services.AddAuthorization();
            services.AddAuthorizationSetup();

            // 添加动态接口
            services.AddDynamicApiController();

            //rabbit
            //services.AddRabbitMqClientExtension();
            //services.AddEasyNetQExtension(); 


            // 查询所有的接口
            services.AddSingleton(services);
            // 将 IServiceCollection 注册为单例，以便查询所有的端口
            services.AddSingleton<ApiExplorerService>();


            // 打印所有注册的服务  
            Console.WriteLine("===== 已注册的服务列表 =====");
            foreach (var service in services)
            {
                string? serviceNs = service.ServiceType?.Namespace ?? "";

                bool isMicrosoftNamespace = serviceNs.StartsWith("Microsoft");

                if (!isMicrosoftNamespace)
                {
                    Console.WriteLine($"服务类型: {service.ServiceType.FullName}");
                    Console.WriteLine($"实现类型: {service.ImplementationType?.FullName ?? "(工厂或实例)"}");
                    Console.WriteLine($"生命周期: {service.Lifetime}");
                    Console.WriteLine("----------------------------------");
                }
            }

            return services;
        }
        // 新增：带拦截器的服务自动注册方法
        private static void AddAutoServicesWithInterceptors(this IServiceCollection services, string[] dllNames)
        {
            var proxyGenerator = services.BuildServiceProvider().GetRequiredService<IProxyGenerator>();
            var transactionInterceptor = services.BuildServiceProvider().GetRequiredService<TransactionInterceptor>();

            foreach (var dllName in dllNames)
            {
                try
                {
                    var assembly = Assembly.Load(dllName);
                    var types = assembly.GetTypes()
                        .Where(t => t is { IsClass: true, IsAbstract: false, Namespace: not null });

                    foreach (var type in types)
                    {
                        // 注册类实现的所有接口
                        var interfaces = type.GetInterfaces();
                        if (interfaces.Length > 0)
                        {
                            foreach (var interfaceType in interfaces)
                            {
                                services.AddScoped(interfaceType, provider =>
                                {
                                    var target = ActivatorUtilities.CreateInstance(provider, type);
                                    return proxyGenerator.CreateInterfaceProxyWithTarget(interfaceType, target, transactionInterceptor);
                                });
                            }
                        }
                        else
                        {
                            // 没有实现接口的类直接注册
                            services.AddScoped(type);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"加载程序集 {dllName} 时出错: {ex.Message}");
                }
            }
        }
    }
}

// 扩展方法：中间件配置
public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseApplicationMiddlewares(this WebApplication app)
    {

        app.ConfigureApplication();

        app.UseSwaggerExtension();

        // 全局异常中间件
        app.UseMiddleware<GlobalException>();

        // 获取远程真实ip,不是nginx代理部署可以不要
        app.UseMiddleware<RealIpMiddleware>();

        app.UseServiceInspector(); // 注册服务查询中间件

        app.UseHttpsRedirection(); // 确保所有请求都通过HTTPS

        app.UseStaticFiles(); // 启用静态文件服务

        app.UseDefaultFiles(); // 提供默认文件支持

        app.UseRouting(); // 确定路由

        app.UseCors(); // 配置跨域资源共享

        app.UseAuthentication(); // 启用身份验证中间件

        app.UseAuthorization(); // 启用授权中间件

        app.UseResponseCaching(); // 应用响应缓存

        app.MapControllers();

        Console.WriteLine("扫描所有api端口");
        Console.WriteLine("=============");

        var service = app.Services.GetRequiredService<ApiExplorerService>();
        var endpoints = service.GetAllEndpoints();
        foreach (var endpoint in endpoints)
        {
            Console.WriteLine(endpoint);
        }

        Console.WriteLine("=============");

        return app;
    }
}