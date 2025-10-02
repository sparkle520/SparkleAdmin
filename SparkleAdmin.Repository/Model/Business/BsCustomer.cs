﻿using SqlSugar;

namespace SparkleAdmin.Repository.Model;

/// <summary>
/// 微信客户表
/// </summary>
[SugarTable("bs_customer")]
public class BsCustomer
{
    /// <summary>
    /// Desc:
    /// Default:
    /// Nullable:False
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int UserID { get; set; }

    /// <summary>
    /// Desc:微信OpenID
    /// Default:
    /// </summary>
    public string OpenID { get; set; }

    /// <summary>
    /// Desc:微信昵称
    /// Default:
    /// Nullable:True
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// Desc:真实姓名
    /// Default:
    /// Nullable:True
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// Desc:头像
    /// Default:
    /// Nullable:True
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Desc:创建时间
    /// Default:DateTime.Now
    /// Nullable:True
    /// </summary>
    public DateTime? CreatedDate { get; set; }
}