﻿using SparkleAdmin.Common.Components.Cache;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar.Extensions;

namespace SparkleAdmin.Common;

public static class CacheSetup
{
    /// <summary>
    /// 注册缓存服务，如有配置 Redis 则启用，没有则启用分布式内存缓存（DistributedMemoryCache）
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddCacheSetup(this IServiceCollection services)
    {
        // 根据情况，启用 Redis 或 DistributedMemoryCache
        if (App.Configuration["Redis:Enabled"].ObjToBool()) 
        {
            services.AddSingleton<ICacheService, RedisCacheService>();
        }
        else
        {
            // 注册 IMemoryCache 服务
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }


        //更多的缓存实现方式 
        return services;
    }
}