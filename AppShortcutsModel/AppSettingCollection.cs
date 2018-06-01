using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace AppShortcuts
{
    public class AppSettingCollection : ObservableCollection<AppSetting>
    {
        private const string DataFileName = "Settings.xml";
        private const string TempDataFileName = "TempSettings.xml";

        private int NewItemCount
        {
            get
            {
                return this.Count(m => m.IsNew);
            }
        }

        #region 读取

        public AppSettingCollection(string fileName = DataFileName)
        {
            if (File.Exists(fileName))
            {
                var content = File.ReadAllText(fileName);
                var reader = new StringReader(content);
                var s = new XmlSerializer(typeof(AppSetting[]));
                var result = s.Deserialize(reader) as AppSetting[];

                foreach (var item in result.OrderBy(i => i.AppName))
                {
                    item.IsNew = false;
                    this.Add(item);
                }
            }
        }

        #endregion

        #region 保存

        public void Save()
        {
            try
            {
                this.CheckConflict();

                this.SaveToXml(TempDataFileName);

                var p = Process.Start(nameof(AppShortcutsModel), AppSetting.AuthToken);
                p.WaitForExit();
                if (p.ExitCode == 0)
                {
                    foreach (var item in this)
                    {
                        item.IsNew = false;
                    }
                    this.SaveToXml();
                }

            }
            finally
            {
                File.Delete(TempDataFileName);
            }
        }

        private void CheckConflict()
        {
            foreach (var item in this)
            {
                if (this.Any(i => i != item && string.Compare(i.AppName, item.AppName, true) == 0))
                {
                    throw new InvalidOperationException("不可以添加相同的项：" + item.AppName);
                }
            }
        }

        public static void SaveShortcutsToRegistry()
        {
            //删除
            //先删除原来的所有文件
            var oldSettings = new AppSettingCollection();
            foreach (var oldItem in oldSettings)
            {
                Console.WriteLine("删除" + oldItem.AppName);
                Registry.LocalMachine.DeleteSubKey(GetSubRegistryKey(oldItem), false);
            }
            //删除 Shortcut 文件
            if (Directory.Exists(ShortcutDir)) { Directory.Delete(ShortcutDir, true); }

            //重建
            //建立新的文件夹
            Directory.CreateDirectory(ShortcutDir);
            var newAppSettings = new AppSettingCollection(TempDataFileName);
            foreach (var item in newAppSettings)
            {
                //创建 Shortcut 文件
                var shortcut = CreateShortcut(item);
                if (shortcut != null)
                {
                    Console.WriteLine("创建" + item.AppName);
                    var key = GetSubRegistryKey(item);
                    Registry.LocalMachine.CreateSubKey(key).SetValue(string.Empty, shortcut);
                }
            }
        }

        private void SaveToXml(string fileName = DataFileName)
        {
            var s = new XmlSerializer(typeof(AppSetting[]));
            var writer = new StringWriter();
            s.Serialize(writer, this.ToArray());
            var content = writer.ToString();
            File.WriteAllText(fileName, content);
        }

        private static string GetSubRegistryKey(AppSetting item)
        {
            var key = string.Format(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{0}.exe", item.AppName);
            return key;
        }

        #endregion

        #region Shortcut

        private static readonly string ShortcutDir = "_Shortcuts\\";

        private static string GetShortcutsFile(AppSetting item)
        {
            return Path.Combine(ShortcutDir, item.AppName + ".lnk");
        }

        private static string CreateShortcut(AppSetting item)
        {
            var targetPath = item.ExePath;

            string scFile = GetShortcutsFile(item);
            scFile = ToAbsolute(scFile);

            var shell = new IWshRuntimeLibrary.WshShell();
            var shortcut = shell.CreateShortcut(scFile) as IWshRuntimeLibrary.IWshShortcut;

            var quota = "\"";
            //如果是以下格式：
            //"C:\Documents and Settings\huqf\桌面\Shortcuts\GIX4Clients.exe" gpm
            if (targetPath.StartsWith(quota) && !targetPath.EndsWith(quota))
            {
                var exe = targetPath.Substring(1);
                var args = exe.Substring(exe.IndexOf(quota) + 1).Trim();
                exe = exe.Remove(exe.IndexOf(quota));
                if (File.Exists(exe))
                {
                    shortcut.Arguments = args;
                    shortcut.TargetPath = exe;
                    shortcut.WorkingDirectory = Path.GetDirectoryName(exe);
                }
            }
            else if (File.Exists(targetPath))
            {
                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
            }
            else
            {
                shortcut.TargetPath = targetPath;
            }

            shortcut.WindowStyle = 1;
            shortcut.Description = "快捷专家 - " + item.AppName;
            //shortcut.IconLocation = System.Environment.SystemDirectory + "\\" + "shell32.dll, 165";
            try
            {
                shortcut.Save();
            }
            catch
            {
                return null;
            }

            return scFile;
        }

        #endregion


        #region 增加

        /// <summary>
        /// 在顶层插入多个
        /// </summary>
        /// <param name="filterDistinct">是否过滤重复项</param>
        /// <param name="exePathData"></param>
        public void InsertMany(bool filterDistinct = true, params string[] exePathData)
        {
            int i = NewItemCount;
            var paths = this.Select(m => m.ExePath.Trim());
            var toInsert = exePathData.Reverse().Where(m => !paths.Contains(m.Trim()));
            foreach (var item in toInsert)
            {
                Insert(0, new AppSetting(AppSetting.DefaultAppName + ++i, item));
            };
        }

        #endregion

        private static string ToAbsolute(string relativePath)
        {
            return Path.Combine(
                AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                relativePath
                );
        }
    }
}
