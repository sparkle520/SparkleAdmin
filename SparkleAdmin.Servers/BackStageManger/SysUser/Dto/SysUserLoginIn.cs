﻿using System.ComponentModel.DataAnnotations;

namespace SparkleAdmin.Servers;


public class SysUserLoginIn
{
    /// <summary>
    /// 登录账号
    /// </summary>
    /// <example>admin</example>
    [Required] 
    public string? Account { get; set; }


    /// <summary>
    /// 密码
    /// </summary>
    /// <example>1q2w3e</example>
    public string? PassWord { get; set; }
}