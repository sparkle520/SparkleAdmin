﻿using SqlSugar;

namespace SparkleAdmin.Repository.Model;

[SugarTable("bs_bingwallpaper")]
public class BsBingWallpaper
{
    [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
    public int Id { get; set; }

    public string CopyRight { get; set; }

    [SugarColumn(IsNullable = true)] public string GitUrl { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartDate { get; set; }

    public string Url { get; set; }

    public string MobileUrl { get; set; }

    public bool IsDeleted { get; set; }
}