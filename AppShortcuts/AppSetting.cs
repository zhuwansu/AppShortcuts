﻿using System.ComponentModel;
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
}
