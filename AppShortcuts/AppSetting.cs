using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
namespace AppShortcuts
{
    public class AppSetting : INotifyPropertyChanged
    {
        private string _AppName;

        private string _ExePath;

        private bool _IsTemp;

        public string AppName
        {
            get { return this._AppName; }
            set
            {
                this._AppName = value;
                this.OnPropertyChanged("AppName");
            }
        }

        public string ExePath
        {
            get { return this._ExePath; }
            set
            {
                this._ExePath = value;
                this.OnPropertyChanged("ExePath");
            }
        }

        public bool IsTemp
        {
            get { return this._IsTemp; }
            set
            {
                this._IsTemp = value;
                this.OnPropertyChanged("IsTemp");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string property)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }

    public class AppSettingCollection : ObservableCollection<AppSetting>
    {
        private static readonly string DataFileName = "Settings.xml";

        #region 读取

        public AppSettingCollection()
        {
            if (File.Exists(DataFileName))
            {
                var content = File.ReadAllText(DataFileName);
                var reader = new StringReader(content);
                var s = new XmlSerializer(typeof(AppSetting[]));
                var result = s.Deserialize(reader) as AppSetting[];

                foreach (var item in result.OrderBy(i => i.AppName))
                {
                    this.Add(item);
                }
            }
        }

        #endregion

        #region 保存

        public void Save()
        {
            this.CheckConflict();

            this.SaveShortcutsToRegistry();

            this.SaveToXml();
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

        private void SaveShortcutsToRegistry()
        {
            //删除
            //先删除原来的所有文件
            var oldSettings = new AppSettingCollection();
            foreach (var oldItem in oldSettings)
            {
                Registry.LocalMachine.DeleteSubKey(GetSubRegistryKey(oldItem), false);
            }
            //删除 Shortcut 文件
            if (Directory.Exists(ShortcutDir)) { Directory.Delete(ShortcutDir, true); }

            //重建
            //建立新的文件夹
            Directory.CreateDirectory(ShortcutDir);
            foreach (var item in this)
            {
                //创建 Shortcut 文件
                var shortcut = CreateShortcut(item);
                if (shortcut != null)
                {
                    var key = GetSubRegistryKey(item);
                    Registry.LocalMachine.CreateSubKey(key).SetValue(string.Empty, shortcut);
                }
            }
        }

        private void SaveToXml()
        {
            var s = new XmlSerializer(typeof(AppSetting[]));
            var writer = new StringWriter();
            s.Serialize(writer, this.ToArray());

            var content = writer.ToString();
            File.WriteAllText(DataFileName, content);
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
                item.ExePath = shortcut.TargetPath;
                shortcut.Save();
            }
            catch
            {
                return null;
            }

            return scFile;
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
