using Microsoft.Extensions.DependencyInjection;
using SqlSugar.IOC;

namespace SparkleAdmin.Servers;

/// <summary>
/// 客户服务
/// </summary>
[ApiExplorerSettings(GroupName = nameof(ApiVersionGropInfo.BusinessManger))]
public class BingWallpaperService: ApiControllerBase
{
    private readonly ISqlSugarClient _db;
    private readonly SqlSugarRepository<BsBingWallpaper> _repository; // 仓储
    private readonly ITokenService _TokenService;
    private readonly ISysLogService _sysLogService;

    public BingWallpaperService(SqlSugarRepository<BsBingWallpaper> repository,
        ITokenService tokenService, ISysLogService sysLogService)
    {
        _repository = repository;
        _TokenService = tokenService;
        _sysLogService = sysLogService;
    }


    /// <summary>
    ///  必应壁纸列表分页
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<PageList<BsBingWallpaper>> PageList([FromQuery] PageParamBase input)
    {
        await _sysLogService.AddLog("后台操作", $"用户{App.CurrentUser.UserId}获取了必应壁纸");
        var list = await _repository.AsQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(input.KeyWord),
                u => u.CopyRight.Contains(input.KeyWord.Trim()) )
            .OrderByDescending(u=>u.StartDate)
            .ToPagedListAsync(input.PageNo, input.PageSize);
        return list.PagedResult();
    }
}