﻿using System.Configuration;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace SparkleAdmin.Common;

public static class JwtSetup
{
    /// <summary>
    /// 添加jwt认证
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IServiceCollection AddJwtSetup(this IServiceCollection services)
    { 
        if (services == null) throw new ArgumentNullException(nameof(services));

        var jwtSection = App.Configuration.GetSection("Jwt"); 
        var jwtConfiguration = jwtSection.Get<JwtOptions>();
         
        var keyByteArray = Encoding.UTF8.GetBytes(jwtConfiguration.Secret);
        var signingKey = new SymmetricSecurityKey(keyByteArray);
        
        // 令牌验证参数
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true, // 是否验证SecurityKey 
            ValidateIssuer = true, // 是否验证Issuer 
            ValidateAudience = true, // 是否验证Audience
            ValidateLifetime = true, // 是否验证失效时间
            ClockSkew = TimeSpan.Zero, // 设置时钟偏移为0 
            IssuerSigningKey = signingKey, //签名验证的密钥
            ValidIssuer = jwtConfiguration.Issuer, // 发行人Issuer  指定发行者
            ValidAudience = jwtConfiguration.Audience // 订阅人Audience 指定受众
        };

        // events
        var jwtBearerEvents = new JwtBearerEvents
        {
            //  1. **OnMessageReceived**:
            //   -触发时机：当接收到一个身份验证请求。
            //   -用途：用来处理接收到的原始身份验证消息，你可以根据请求的具体情况来修改或取消身份验证过程。

            //2. * *OnTokenValidated * *:
            //   -触发时机：在JWT被成功验证后触发。
            //   -用途：用来处理已验证的token，例如，你可以在这里添加额外的日志记录或执行一些安全检查。

            //3. * *OnAuthenticationFailed * *:
            //   -触发时机：当身份验证失败时触发。
            //   -用途：用来处理身份验证失败的情况，例如，记录失败原因、执行额外的错误处理逻辑等。

            //4. * *OnChallenge * *:
            //   -触发时机：当需要向客户端发出一个挑战（例如，要求客户端提供凭据）时触发。
            //   -用途：自定义挑战的响应，例如，修改返回给客户端的`401 Unauthorized`响应。

            //5. * *OnForbidden * *:
            //   -触发时机：当授权失败时触发（即用户已通过身份验证，但没有足够的权限访问特定资源）。
            //   -用途：自定义处理禁止访问的情况，例如，返回一个自定义的错误消息或执行其他逻辑。

            OnMessageReceived = context =>
            {
                var token = context.Request.Headers["Authorization"].ToString()?.Replace("Bearer ", "");
                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                // 如果过期，把过期信息添加到头部
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Append("Token-Expired", "true");
                }

                return Task.CompletedTask;
            }
            //OnForbidden = async context =>
            //{
            //    context.Response.StatusCode = StatusCodes.Status403Forbidden;
            //    context.Response.ContentType = "application/json";
            //    await context.Response.WriteAsync("403");
            //}
        };

        //添加身份验证-
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            //验证token
            options.TokenValidationParameters = tokenValidationParameters;
            options.Events = jwtBearerEvents;
        });

        return services;
    }
}