using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace AppShortcuts
{
    public class AppSetting : INotifyPropertyChanged
    {
        public const string AuthToken = "{92057119-C46A-484C-B6F8-DEE20F6C8C71}";
        public const string DefaultExePath = "路径（支持直接拖动文件或文件夹到这里）";
        public const string DefaultAppName = "新增名称";

        public AppSetting() { }

        public AppSetting(string name = DefaultAppName, string path = DefaultExePath)
        {
            _AppName = name;
            _ExePath = path;
            _IsTemp = true;
            IsNew = true;
        }

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

        [XmlIgnore]
        public bool IsNew { get; set; }

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
}
