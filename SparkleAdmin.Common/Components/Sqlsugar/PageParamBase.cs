﻿/// <summary>
///通用分页输入参数
/// </summary>
public class PageParamBase
{
    /// <summary>
    ///搜索值
    /// </summary>
    public string? KeyWord { get; set; }

    /// <summary>
    ///当前页码
    /// </summary>
    public  int PageNo { get; set; } = 1;

    /// <summary>
    ///页码容量
    /// </summary>
    public  int PageSize { get; set; } = 20;

    /// <summary>
    ///搜索开始时间
    /// </summary>
    public string? SearchBeginTime { get; set; }

    /// <summary>
    ///搜索结束时间
    /// </summary>
    public string? SearchEndTime { get; set; }

    /// <summary>
    ///排序字段
    /// </summary>
    public string? SortField { get; set; }

    /// <summary>
    ///排序方法,默认升序,否则降序(配合antd前端,约定参数为 Ascend,Dscend)
    /// </summary>
    public  string? SortOrder { get; set; }

    /// <summary>
    ///降序排序(不要问我为什么是descend不是desc，前端约定参数就是这样)
    /// </summary>
    public virtual string DescStr { get; set; } = "descend";
}