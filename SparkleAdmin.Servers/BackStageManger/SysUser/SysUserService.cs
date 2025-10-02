﻿using System.Collections.Generic;
using System.Linq.Expressions;
using SparkleAdmin.Common;
using SparkleAdmin.Common.Constant; 
using SparkleAdmin.Servers.SysRoleMenu;
using SparkleAdmin.Servers.SysUser;
using SparkleAdmin.Servers.SysUser.Dto; 
using Mapster;

namespace SparkleAdmin.Servers;

/// <summary>
/// 用户服务
/// </summary>
[ApiExplorerSettings(GroupName = nameof(ApiVersionGropInfo.BackStageManger))]
public class SysUserService : ApiControllerBase, ISysUserService
{
    private readonly IHttpContextAccessor _HttpContext;
    private readonly ISysLogService _sysLogService;
    private readonly ISysMenuService _sysMenuService;
    private readonly ISysRoleMenuService _sysRoleMenuService;
    private readonly ISysRolePermissionService _sysRolePermissionService;
    private readonly SqlSugarRepository<TSysUser> _sysUserRep; // 仓储
    private readonly ITokenService _TokenService;
    private readonly Common.ICacheService _cacheService;

    public SysUserService(SqlSugarRepository<TSysUser> sysUserRep,
        ITokenService tokenService, ISysRoleMenuService sysRoleMenuService,
        ISysMenuService sysMenuService, ISysRolePermissionService sysRolePermissionService,
        ISysLogService sysLogService, Common.ICacheService cacheService,
        IHttpContextAccessor httpContext)
    {
        _sysUserRep = sysUserRep;
        _sysLogService = sysLogService;
        _TokenService = tokenService;
        _HttpContext = httpContext;
        _sysRoleMenuService = sysRoleMenuService;
        _sysMenuService = sysMenuService;
        _sysRolePermissionService = sysRolePermissionService;
        _cacheService = cacheService;
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<SysUserLoginOut> Login(SysUserLoginIn input)
    { 
        var user = await _sysUserRep
            .Where(t => t.Account.ToLower() == input.Account.ToLower()).FirstAsync();
        if (user == null) throw ApiException.Exception207Bad("未找到用户");

        if (user.PassWord != Md5Util.Encrypt(input.PassWord).ToUpper()) throw ApiException.Exception207Bad("密码输入错误");
        if (user.Status != 1) throw ApiException.Exception207Bad("该账户已被冻结");

        var UserRolePer = new List<TSysRolePermission>();
        user.UserRolesId.ForEach(x =>
        {
            UserRolePer.AddRange(_sysRolePermissionService.GetRoleButtonPermiss(x).Result);
        });

        var button = UserRolePer.DistinctBy(x => x.UserPermiss).Select(x => x.UserPermiss).ToList();

        if (user.IsSuperAdmin)
            button = _sysRolePermissionService.GetAllButen().Result.Select(x => x.PermissionId).ToList();

        //缓存当前用户的菜单
        App.Cache.Set(CacheConstant.UserButton+ user.Id, button); 
         
        var token = await _TokenService.GenerateTokenAsync(new TokenData { 
            UserAccount = user.Account,
            UserId= user.Id, 
            UserRolesId = user.UserRolesId,
            UserName = user.Name,
            UserPermiss = button ,
            IsSuperAdmin=user.IsSuperAdmin
        }); 

        //await _sysLogService.AddLog("后台日志", $"用户{user.Name}登录了系统");

        return new SysUserLoginOut { Id = user.Id, Name = user.Name, Token = token };
    }

    /// <summary>
    /// 获取用户的信息
    /// </summary>
    /// <returns></returns>
    public async Task<GetUserInfoOut> GetUserInfo()
    { 
        var userinfo= await _sysUserRep
            .Where(t => t.Id== App.CurrentUser.UserId).FirstAsync();
        if (userinfo is null)
        {
            throw ApiException.Exception401("请重新登录");
        }

        var otherButton = new List<string>() 
        {
            //"api:Syslogin:ResetPassword",//修改密码的权限
        };
        var userButton = await _sysRolePermissionService.GetUserPermiss(userinfo.Id);
        return new GetUserInfoOut
        {
            userId = userinfo.Id,
            userName = userinfo.Name,
            roles = userinfo.UserRolesId.Select(x => x.ToString()).ToList(),
            avatar = userinfo.Avatar,
            buttons = otherButton.Concat(userButton).ToList(),
            userInfo= userinfo.Adapt<SysUserInfo>()
        };
    }



    /// <summary>
    /// 更改用户信息
    /// </summary>
    /// <returns></returns>
    public async Task<bool> UpdateUserInfo(SysUserInfo input)
    { 
        var entity = input.Adapt<TSysUser>(); 
        return await  _sysUserRep.Context.Updateable<TSysUser>(entity)
            .UpdateColumns(x=> new { x.Name,x.Remark,x.Tel,x.Email} )
            .Where(x => x.Id == App.CurrentUser.UserId).ExecuteCommandAsync()>0;
        //return await _sysUserRep.UpdateAsync(content).where >0; 
    }

    /// <summary>
    /// 更改密码
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<bool> ResetPassWord(ResetPassWord input)
    { 
        var user = await _sysUserRep.Where(x => x.Id == App.CurrentUser.UserId).FirstAsync();
        if (user == null) throw ApiException.Exception207Bad("未找到用户");

        if (user.PassWord != Md5Util.Encrypt(input.OldPassWord).ToUpper()) throw ApiException.Exception207Bad("旧密码不正确");
        if (user.PassWord == Md5Util.Encrypt(input.NewPassWord).ToUpper()) throw ApiException.Exception207Bad("旧密码不能与新密码相同");
        user.PassWord = Md5Util.Encrypt(input.NewPassWord);
        return await _sysUserRep.UpdateAsync(user) > 0; 
    }

    /// <summary>
    /// 用户列表分页
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet]
    [Permission("用户信息分页查询")]
    public async Task<PageList<TSysUser>> PageList([FromQuery] UserPageIn input)
    {
        var dictTypes = await _sysUserRep.AsQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(input.KeyWord), u => u.Name.Contains(input.KeyWord.Trim()))
            .WhereIF(input.Status != null, u => u.Status == input.Status)
            //.Select<UserPageOut>()
            .ToPagedListAsync(input.PageNo, input.PageSize);
        return dictTypes.PagedResult();
    }

    /// <summary>
    /// 添加用户
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Permission("用户信息添加")]
    public async Task<bool> Add(UserAddAndUpIn input)
    {
        var isExist = await _sysUserRep.Where(x => x.Account == input.Account).AnyAsync();
        if (isExist) throw ApiException.Exception207Bad("已存在当前账号");
        var entity = input.Adapt<TSysUser>();
        entity.PassWord = Md5Util.Encrypt(input.PassWord);
        return await _sysUserRep.InsertReturnIdentityAsync(entity) > 0;
    }


    /// <summary>
    /// 删除用户
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [HttpPost("{id:int}")]
    [Permission("用户信息删除")]
    [ReadOnly]
    public async Task<bool> Delete(int id)
    {
        var entity = await _sysUserRep.FirstOrDefaultAsync(u => u.Id == id);
        if (entity == null) throw ApiException.Exception207Bad("未找到当前账号");
        entity.SysIsDelete = true;
        //Todo 删除用户缓存

        return await _sysUserRep.UpdateAsync(entity) > 0;
    }

    /// <summary>
    /// 更新用户
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [Permission("用户信息更新")]
    [ReadOnly]
    public async Task<bool> Update(UserAddAndUpIn input)
    {
        var entity = await _sysUserRep.FirstOrDefaultAsync(u => u.Id == input.Id);
        if (entity == null) throw ApiException.Exception207Bad("未找到当前账号");
        //Todo 更新用户缓存
        //优化  更新的字段

        var sysUser = input.Adapt<TSysUser>();
        return await _sysUserRep.UpdateNotNullColumnsAsync(sysUser)>0;
    }


    /// <summary>
    /// 获取登录用户的菜单权限
    /// </summary>
    /// <returns></returns>
    public async Task<UserMenuOut> GetUserMenu()
    {
 
        //缓存
        var cache_user_menus = _cacheService.Get<UserMenuOut>(CacheConstant.UserMenus+ App.CurrentUser.UserId);
        if (cache_user_menus == null)
        {
            //返回的菜单权限
            var userMenuOut = new UserMenuOut();
           
            //获取所有的菜单权限
            var menutree =new List<TSysMenu>(); 


            //超级管理员，直接构建菜单路由
            if (App.CurrentUser.IsSuperAdmin) 
            {
                //所有菜单权限
                menutree = await _sysUserRep.Context.Queryable<TSysMenu>().ToListAsync(); 
            }
            else
            {
                //获取当前用户的菜单权限
                //var menuid = await _sysRoleMenuService.RoleUserMenu();
                var userroleIds = App.CurrentUser.UserRolesId;

                menutree = await _sysUserRep.Context.Queryable<TSysMenu>()
                    .LeftJoin<TSysRoleMenu>((m,rm)=>m.Id==rm.MenuId)
                    .Where((m,rm)=> userroleIds.Contains(rm.RoleId)  )
                    .ToListAsync();
            }
             
            var menuTree = BuildMenuTree(menutree);
            var userMenus = menuTree.Select(ConvertMenu).ToList();
              
            userMenuOut.Home = userMenus.FirstOrDefault()?.Name;
            userMenuOut.Routes = userMenus;
            //进行缓存
            _cacheService.Set(CacheConstant.UserMenus + App.CurrentUser.UserId, userMenuOut, 60*60*60);
            return userMenuOut;
        }
        return cache_user_menus;




    }

    private List<TSysMenu> BuildMenuTree(List<TSysMenu> flatMenus, int? parentId = 0)
    {
        return flatMenus
            .Where(m => m.ParentId == parentId)
            .OrderBy(m => m.Sort)
            .Select(m => {
                m.Children = BuildMenuTree(flatMenus, m.Id);
                return m;
            })
            .ToList();
    }

   /// <summary>
   /// 构建前端所需路由
   /// </summary>
   /// <param name="menu"></param>
   /// <returns></returns>
    private UserMenu ConvertMenu(TSysMenu menu)
    {
        return new UserMenu
        {
            Name = menu.RouteName,
            Path = menu.RoutePath,
            Component = menu.Component,
            Meta = new Meta
            {
                Title = menu.MenuName,
                Icon = menu.Icon,
                Order = menu.Sort,
                HideInMenu = menu.HideInMenu,
                Href = menu.Href
            },
            Children = menu.Children?.Select(ConvertMenu).ToList()
        };
    }





}