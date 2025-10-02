﻿using System.Diagnostics;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace SparkleAdmin.Common.Helper;

/// <summary>
/// 服务器信息工具类
/// </summary>
public class ServerInfoUtil
{
    /// <summary>
    /// CPU数量
    /// </summary>
    public static int CpuCount => Environment.ProcessorCount;

    /// <summary>
    /// 计算机名称
    /// </summary>
    public static string MachineName => Environment.MachineName;

    /// <summary>
    /// 系统名称
    /// </summary>
    public static string OSName => RuntimeInformation.OSDescription;

    /// <summary>
    /// 系统架构
    /// </summary>
    public static string OSArchitecture => RuntimeInformation.OSArchitecture.ToString();

    /// <summary>
    /// .Net版本名称
    /// </summary>
    public static string DoNetName => RuntimeInformation.FrameworkDescription;

    /// <summary>
    /// 占用内存
    /// </summary>
    public static string UseRam => ((double)Process.GetCurrentProcess().WorkingSet64 / 1048576).ToString("N2") + " MB";

    /// <summary>
    /// 服务开始运行时间
    /// </summary>
    public static string StartTime => Process.GetCurrentProcess().StartTime.ToString("G");

    /// <summary>
    /// 服务运行时间
    /// </summary>
    public static string RunTime => (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString();

    /// <summary>
    /// ip
    /// </summary>
    public static List<string> IP
    {
        get
        {
            var strIp = new List<string>();

            //NetworkInterface：提供网络接口的配置和统计信息。
            var adapters = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var adapter in adapters)
            {
                var adapterProperties = adapter.GetIPProperties();
                var allAddress = adapterProperties.UnicastAddresses;

                //这里是根据网络适配器名称找到对应的网络，adapter.Name即网络适配器的名称
                if (allAddress.Count > 0 && adapter.Name == "WLAN")
                    foreach (var addr in allAddress)
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                            strIp.Add(addr.Address.ToString());
            }

            if (strIp.Count == 0)
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                        strIp.Add(ip.ToString());
            }

            return strIp;
        }
    }

    /// <summary>
    /// 是否Unix系统
    /// </summary>
    /// <returns></returns>
    public static bool IsUnix => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                                 RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <summary>
    /// 内存使用综合情况
    /// </summary>
    /// <returns></returns>
    public static MemoryInfo MemoryInfo
    {
        get
        {
            var MemoryInfo = IsUnix ? GetUnixMetrics() : GetWindowsMetrics();
            try
            {
                MemoryInfo.FreeRam = Math.Round(MemoryInfo.Free / 1024, 2) + "GB";
                MemoryInfo.UsedRam = Math.Round(MemoryInfo.Used / 1024, 2) + "GB";
                MemoryInfo.TotalRAM = Math.Round(MemoryInfo.Total / 1024, 2);
                MemoryInfo.RAMRate = Math.Ceiling(100 * MemoryInfo.Used / MemoryInfo.Total);
                if (IsUnix)
                {
                    var output = ShellUtil.Bash("top -b -n1 | grep \"Cpu(s)\" | awk '{print $2 + $4}'");
                    MemoryInfo.CPURate = Math.Ceiling(Convert.ToDouble(output.Trim()));
                }
                else
                {
                    var output = ShellUtil.Cmd("wmic", "cpu get LoadPercentage");
                    MemoryInfo.CPURate =
                        Math.Ceiling(Convert.ToDouble(output.Replace("LoadPercentage", string.Empty).Trim()));
                }

                MemoryInfo.AppRAMRate =
                    Math.Round((double)Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024 / MemoryInfo.Total, 2);
            }
            catch
            {
            }

            return MemoryInfo;
        }
    }


    /// <summary>
    /// 磁盘使用综合大小
    /// </summary>
    /// <returns></returns>
    public static List<DiskInfo> DiskInfo
    {
        get
        {
            var diskInfos = new List<DiskInfo>();

            if (IsUnix)
            {
                try
                {
                    var output = ShellUtil.Bash("df -m / | awk '{print $2,$3,$4,$5,$6}'");
                    var arr = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    if (arr.Length == 0)
                        return diskInfos;

                    var rootDisk = arr[1].Split(' ', (char)StringSplitOptions.RemoveEmptyEntries);
                    if (rootDisk == null || rootDisk.Length == 0) return diskInfos;
                    var diskInfo = new DiskInfo
                    {
                        DiskName = "/",
                        TotalSize = long.Parse(rootDisk[0]) / 1024,
                        Used = long.Parse(rootDisk[1]) / 1024,
                        AvailableFreeSpace = long.Parse(rootDisk[2]) / 1024,
                        AvailablePercent = decimal.Parse(rootDisk[3].Replace("%", ""))
                    };
                    diskInfos.Add(diskInfo);
                }
                catch
                {
                }
            }
            else
            {
                var driv = DriveInfo.GetDrives();
                foreach (var item in driv)
                    try
                    {
                        var obj = new DiskInfo
                        {
                            DiskName = item.Name,
                            TypeName = item.DriveType.ToString(),
                            TotalSize = item.TotalSize / 1024 / 1024 / 1024,
                            AvailableFreeSpace = item.AvailableFreeSpace / 1024 / 1024 / 1024
                        };
                        obj.Used = obj.TotalSize - obj.AvailableFreeSpace;
                        obj.AvailablePercent = decimal.Ceiling(obj.Used / (decimal)obj.TotalSize * 100);
                        diskInfos.Add(obj);
                    }
                    catch
                    {
                    }
            }

            return diskInfos;
        }
    }

    /// <summary>
    /// windows系统获取内存信息
    /// </summary>
    /// <returns></returns>
    public static MemoryInfo GetWindowsMetrics()
    {
        var metrics = new MemoryInfo();

        // 查询总内存
        using (var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem"))
        {
            foreach (var obj in searcher.Get())
            {
                metrics.Total = Math.Round(double.Parse(obj["TotalVisibleMemorySize"].ToString()) / 1024, 0);
            }
        }

        // 查询空闲内存
        using (var searcher = new ManagementObjectSearcher("SELECT FreePhysicalMemory FROM Win32_OperatingSystem"))
        {
            foreach (var obj in searcher.Get())
            {
                metrics.Free = Math.Round(double.Parse(obj["FreePhysicalMemory"].ToString()) / 1024, 0);
            }
        }

        // 计算已使用内存
        metrics.Used = metrics.Total - metrics.Free;

        return metrics;
    }

    /// <summary>
    /// Unix系统获取
    /// </summary>
    /// <returns></returns>
    public static MemoryInfo GetUnixMetrics()
    {
        var output = ShellUtil.Bash("free -m | awk '{print $2,$3,$4,$5,$6}'");
        var metrics = new MemoryInfo();
        var lines = output.Split('\n', (char)StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length <= 0)
            return metrics;

        if (lines != null && lines.Length > 0)
        {
            var memory = lines[1].Split(' ', (char)StringSplitOptions.RemoveEmptyEntries);
            if (memory.Length >= 3)
            {
                metrics.Total = double.Parse(memory[0]);
                metrics.Used = double.Parse(memory[1]);
                metrics.Free = double.Parse(memory[2]); //m
            }
        }

        return metrics;
    }
}

/// <summary>
/// 内存信息
/// </summary>
public class MemoryInfo
{
    public double Total { get; set; }

    public double Used { get; set; }

    public double Free { get; set; }

    public string UsedRam { get; set; }

    /// <summary>
    /// CPU使用率%
    /// </summary>
    public double CPURate { get; set; }

    /// <summary>
    /// 总内存 GB
    /// </summary>
    public double TotalRAM { get; set; }

    /// <summary>
    /// 内存使用率 %
    /// </summary>
    public double RAMRate { get; set; }

    /// <summary>
    /// 空闲内存
    /// </summary>
    public string FreeRam { get; set; }

    /// <summary>
    /// APP内存使用率
    /// </summary>
    public double AppRAMRate { get; set; }
}

/// <summary>
/// 磁盘信息
/// </summary>
public class DiskInfo
{
    /// <summary>
    /// 磁盘的名称。
    /// </summary>
    public string DiskName { get; set; }

    /// <summary>
    /// 磁盘的类型。
    /// </summary>
    public string TypeName { get; set; }

    /// <summary>
    /// 磁盘的剩余空间总量（字节）。
    /// </summary>
    public long TotalFree { get; set; }

    /// <summary>
    /// 磁盘的总大小（字节）。
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    /// 磁盘已使用的空间量（字节）。
    /// </summary>
    public long Used { get; set; }

    /// <summary>
    /// 磁盘可使用的剩余空间（字节）。
    /// </summary>
    public long AvailableFreeSpace { get; set; }

    /// <summary>
    /// 磁盘剩余空间的百分比。
    /// </summary>
    public decimal AvailablePercent { get; set; }
}