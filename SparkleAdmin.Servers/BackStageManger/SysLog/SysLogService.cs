﻿ 
using SparkleAdmin.Servers;

namespace SparkleAdmin.Servers;

/// <summary>
/// 日志服务
/// </summary>
//[DisabledRequestRecord]
[ApiExplorerSettings(GroupName = nameof(ApiVersionGropInfo.BackStageManger))]
public class SysLogService : ApiControllerBase, ISysLogService
{
    private readonly SqlSugarRepository<TSysLogVis> _rep; // 仓储

    public SysLogService(SqlSugarRepository<TSysLogVis> rep)
    {
        _rep = rep;
    }

    /// <summary>
    /// 系统操作日志
    /// </summary>
    /// <param name="type"></param>
    /// <param name="Message"></param>
    /// <returns></returns>
    [NonAction]
    public async Task AddLog(string type, string Message)
    {
        var entity = new TSysLogOp
        {
            LogType = type,
            Message = Message,
            LogDateTime = DateTime.Now
        };
        _rep.Context.Insertable(entity).SplitTable().ExecuteReturnSnowflakeId();
    }

    /// <summary>
    /// 访问 日志列表分页
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns> 
    [HttpGet]

    public async Task<PageList<TSysLogVis>> VisPageList([FromQuery] SysLogPageIn input)
    {
        var dictTypes = await _rep.Context.Queryable<TSysLogVis>()
            .SplitTable(tabs => tabs.Take(1))
            .WhereIF(!string.IsNullOrWhiteSpace(input.KeyWord), u => u.Name.Contains(input.KeyWord.Trim()))
            .OrderByDescending(u => u.OpTime)
            .ToPagedListAsync(input.PageNo, input.PageSize);
        return dictTypes.PagedResult();
    }


    /// <summary>
    /// 异常 日志列表分页
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns
    [HttpGet]
    public async Task<PageList<TSysLogErr>> ErrPageList([FromQuery] SysLogPageIn input)
    {
        var dictTypes = await _rep.Context.Queryable<TSysLogErr>()
            .SplitTable(tabs => tabs.Take(1))
            .WhereIF(!string.IsNullOrWhiteSpace(input.KeyWord), u => u.Message.Contains(input.KeyWord.Trim()))
            .OrderByDescending(u => u.LogDateTime)
            .ToPagedListAsync(input.PageNo, input.PageSize);
        return dictTypes.PagedResult();
    }


    /// <summary>
    /// 操作日志分页
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<PageList<TSysLogOp>> OpPageList([FromQuery] SysLogPageIn input)
    {
        var dictTypes = await _rep.Context.Queryable<TSysLogOp>()
            .SplitTable(tabs => tabs.Take(1))
            .WhereIF(!string.IsNullOrWhiteSpace(input.KeyWord), u => u.Message.Contains(input.KeyWord.Trim()))
            .OrderByDescending(u => u.LogDateTime)
            .ToPagedListAsync(input.PageNo, input.PageSize);
        return dictTypes.PagedResult();
    }
}