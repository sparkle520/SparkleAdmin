﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 远程IP中间件，nginx代理服务的时候需要使用才能通过RemoteIpAddress获取客户端真实IP
/// </summary>
public class RealIpMiddleware
{
    private readonly RequestDelegate _next;

    public RealIpMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task Invoke(HttpContext context)
    {
        var headers = context.Request.Headers;
        if (headers.TryGetValue("X-Forwarded-For", out var header))
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse(header.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries)[0]);
        }

        return _next(context);
    }
}
