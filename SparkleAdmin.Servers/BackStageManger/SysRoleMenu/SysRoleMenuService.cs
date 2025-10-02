﻿using SparkleAdmin.Servers.SysRoleMenu.Dto;

namespace SparkleAdmin.Servers.SysRoleMenu;

/// <summary>
/// 角色菜单服务
/// </summary>
[ApiExplorerSettings(GroupName = nameof(ApiVersionGropInfo.BackStageManger))]
public class SysRoleMenuService : ApiControllerBase, ISysRoleMenuService
{
    private readonly ISqlSugarClient _db;
    private readonly SqlSugarRepository<TSysRoleMenu> _sysRoleMenuRep; // 仓储
    private readonly ITokenService _TokenService;

    public SysRoleMenuService(SqlSugarRepository<TSysRoleMenu> sysRoleMenuRep, ITokenService tokenService)
    {
        _sysRoleMenuRep = sysRoleMenuRep;
        _TokenService = tokenService;
    }


    /// <summary>
    /// 角色菜单获取
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet]
    [Permission("角色菜单查询")]
    public async Task<List<int>> RoleUserMenu(List<int> RoleId)
    {
        var HideorDisableMenusId = await _sysRoleMenuRep.Context.Queryable<TSysMenu>()
            .Where(x => x.Status == 2).Select(x => x.Id).ToListAsync();

        var Res = await _sysRoleMenuRep.Where(x => RoleId.Contains(x.RoleId))
            .Where(x => !HideorDisableMenusId.Contains(x.MenuId))
            .Select(x => x.MenuId)
            .ToListAsync();

        return Res;
    }

    /// <summary>
    /// 角色菜单设置
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [Permission("角色菜单更新")]
    [ReadOnly]
    public async Task<bool> SetRoleUserMenu(UpdateRoleUserMenuIn input)
    {
        await _sysRoleMenuRep.DeleteAsync(x => x.RoleId == input.RoleId);
        var list = new List<TSysRoleMenu>();
        input.MenuId.ForEach(x => { list.Add(new TSysRoleMenu { RoleId = input.RoleId, MenuId = x }); });
        var ResCount = await _sysRoleMenuRep.Context.Insertable<TSysRoleMenu>(list).ExecuteCommandAsync();

        return ResCount > 0;
    }
}