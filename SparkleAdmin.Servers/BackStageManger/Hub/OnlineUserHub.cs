﻿using SparkleAdmin.Common;
using Microsoft.AspNetCore.SignalR;
using ICacheService = SparkleAdmin.Common.ICacheService;
using Parser = UAParser.Parser;

namespace SparkleAdmin.Servers.Hub;

public class OnlineUserHub : Hub<IOnlineUserHub>
{
    private readonly ICacheService _cacheService;
    private readonly IHubContext<OnlineUserHub, IOnlineUserHub> _onlineUserHubContext;
    private readonly SqlSugarRepository<TSysOnlineUser> _rep; // 仓储

    public OnlineUserHub(ICacheService cacheService, IHubContext<OnlineUserHub, IOnlineUserHub> onlineUserHubContext,
        SqlSugarRepository<TSysOnlineUser> rep)
    {
        _rep = rep;
        _cacheService = cacheService;
        _onlineUserHubContext = onlineUserHubContext;
    }

    /// <summary>
    /// 当用户连接
    /// </summary>
    /// <returns></returns>
    public override async Task OnConnectedAsync()
    {
        await Console.Out.WriteLineAsync("用户连接:" + Context.ConnectionId);

        var httpContext = Context.GetHttpContext();
       
 
      
        var client = Parser.GetDefault().Parse(httpContext.Request.Headers["User-Agent"]);

        var token = httpContext?.Request.Query["token"].ToString();
        var user= App.Cache.Get<TokenData>(CacheConstant.UserToken + token);

        if (user is not null)
        {
            var onlineUser = new TSysOnlineUser
            {
                ConnectionId = Context.ConnectionId,
                UserId = user.UserId,
                UserName = user.UserAccount,
                RealName = user.UserName,
                Time = DateTime.Now,
                Ip = httpContext.Connection.RemoteIpAddress.MapToIPv4().ToString(),
                Browser = client.UA.Family + client.UA.Major,
                Os = client.OS.Family + client.OS.Major
            };

            await _rep.InsertAsync(onlineUser);

            _cacheService.Set(CacheConstant.OnlineUser + Context.ConnectionId, onlineUser, 60 * 60);

            await _onlineUserHubContext.Clients.All.PublicNotice($"{App.CurrentUser.UserName},上线了");
        }
       
    }

    /// <summary>
    /// 用户断开连接
    /// </summary>
    /// <returns></returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Console.Out.WriteLineAsync("用户断开连接:" + Context.ConnectionId);

        await _rep.DeleteAsync(u => u.ConnectionId == Context.ConnectionId);

        _cacheService.Remove(CacheConstant.OnlineUser + Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// 定义一个方法供客户端调用
    /// </summary>
    /// <param name="username"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task SendMessage(string username, string message)
    {
        await Console.Out.WriteLineAsync("用户发送消息:" + username + "message");

        //收到的消息发送所有人,调用服务端的messageReceived方法
        await _onlineUserHubContext.Clients.All.ReceiveMessage(new { username, message });
    }

    /// <summary>
    /// 强制下线
    /// </summary>
    /// <param name="connectionId"></param>
    /// <returns></returns>
    public async Task ForceOffline(string connectionId)
    {
        await _onlineUserHubContext.Clients.Client(connectionId).ForceOffline("强制下线");
    }
}