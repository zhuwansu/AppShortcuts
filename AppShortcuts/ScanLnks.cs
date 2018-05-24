using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AppShortcuts
{
    public static class ScanLnks
    {
        /// <summary>
        /// 扫描所有磁盘
        /// </summary>
        /// <param name="res"></param>
        public static void ScanAll(AppSettingCollection res)
        {
            DriveInfo[] allDirves = DriveInfo.GetDrives();
            var dirs = allDirves.Where(m => m.DriveType == DriveType.Fixed
            && m.IsReady).Select(m => m.RootDirectory);

            foreach (var dir in dirs)
            {
                var allChild = dir.RecursionDirs();
                foreach (var child in allChild)
                {
                    try
                    {
                        var files = child.GetFileSystemInfos();
                        foreach (var item in files)
                        {
                            try
                            {
                                if (item.Extension.ToLower() != ".lnk")
                                {
                                    continue;
                                }
                                if (res.Any(appset => appset.AppName == item.Name))
                                {
                                    //不添加相同项
                                    continue;
                                }
                                res.Add(new AppSetting()
                                {
                                    AppName = item.Name,
                                    ExePath = item.FullName,
                                    IsTemp = true
                                });
                            }
                            catch { }

                        }

                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// 通过扫描注册表 获取注册的所有程序
        /// </summary>
        /// <returns></returns>
        public static void ScanRegistry(AppSettingCollection res)
        {
            var appPath = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths");

            foreach (var subKeyName in appPath.GetSubKeyNames())
            {
                var cItemKey = appPath.OpenSubKey(subKeyName);
                var exePath = cItemKey.GetValue("")?.ToString();
                var path = cItemKey.GetValue("Path");
                if (string.IsNullOrEmpty(exePath))
                {
                    exePath = path + subKeyName;
                }
                if (File.Exists(exePath))
                {
                    if (res.Any(m => m.AppName == subKeyName))
                    {
                        //不添加相同项
                        continue;
                    }
                    res.Add(new AppSetting()
                    {
                        AppName = subKeyName,
                        ExePath = exePath,
                        IsTemp = true
                    });
                }

            }
        }


        /// <summary>
        /// 递归所有子目录
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        private static List<DirectoryInfo> RecursionDirs(this DirectoryInfo dir)
        {
            var res = new List<DirectoryInfo>();

            res.Add(dir);

            var step = dir.GetDirectories().ToList();
            step = res.AddFilterRange(step);

            while (true)
            {
                var stepChilds = new List<DirectoryInfo>();
                foreach (var item in step)
                {
                    try
                    {
                        stepChilds.AddFilterRange(item.GetDirectories().ToList());
                    }
                    catch { }
                }
                step = stepChilds;
                if (step.Count == 0) break;
                res.AddRange(step);
            }
            return res;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="me"></param>
        /// <param name="toAdd"></param>
        /// <returns>过滤后的结果</returns>
        private static List<DirectoryInfo> AddFilterRange(this List<DirectoryInfo> me, List<DirectoryInfo> toAdd)
        {
            //todo:可配置项 根据 tostring 判断某些用户不扫描 如 .NET v2.0 用户
            var range = toAdd.Where(
            m => m.Name != "Windows.old"
            //&& m.CanIndex()
            //&& !m.IsHidden()
            && m.Name != "WinSxS"
            && m.Name.ToLower() != "roaming"
            && m.Name.ToLower() != "recent"
            && !m.Attributes.ToString().Contains("Temporary")
            && !m.Name.StartsWith("$")
            && !m.Name.StartsWith("_"));

            me.AddRange(range);
            return range.ToList();
        }

        public static bool IsHidden(this DirectoryInfo me)
        {

            return me.Attributes.ToString().Contains("Hidden");
        }

        public static bool CanIndex(this DirectoryInfo me)
        {
            return !me.Attributes.HasFlag(FileAttributes.NotContentIndexed);
        }
    }
}
