/*******************************************************
 * 
 * 作者：胡庆访
 * 创建时间：201101
 * 说明：此文件只包含一个类，具体内容见类型注释。
 * 运行环境：.NET 4.0
 * 版本号：1.0.0
 * 
 * 历史记录：
 * 创建文件 胡庆访 201001
 * 
*******************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Diagnostics;
using System.Timers;
using System.IO;

namespace AppShortcuts
{
    public partial class MainWindow : Window
    {
        private static readonly string CurrentVersion = "0.2.6.0";

        public static readonly RoutedUICommand ItemDelete = new RoutedUICommand("ItemDelete", "ItemDelete", typeof(MainWindow));

        public static readonly RoutedUICommand OpenItemDir = new RoutedUICommand("OpenItemDir", "OpenItemDir", typeof(MainWindow));

        private AppSettingCollection _allData;

        private ICollectionView _allDataView;

        public MainWindow()
        {
            InitializeComponent();

            this.LoadData();

            if (this._allData.Count == 0)
            {
                //刚开始使用时，显示帮助，并自动添加一条数据
                this.Loaded += (o, e) =>
                {
                    var timer = new Timer()
                    {
                        AutoReset = false,
                        Interval = 500
                    };
                    timer.Elapsed += (oo, ee) => ShowHelp();
                    timer.Start();
                    this.AddNew();
                };
            }
            else if (this._allData.Count > 10)
            {
                this.SizeToContent = SizeToContent.Manual;
            }

            this.InitSeaching();
        }

        private void LoadData()
        {
            this._allData = new AppSettingCollection();
            this._allDataView = CollectionViewSource.GetDefaultView(this._allData);
            this.DataContext = this._allDataView;

            this.ResetSearchState();
            this._allDataView.CollectionChanged += (o, e) => this.ResetSearchState();
        }

        #region 数据维护

        private void ItemDelete_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            var item = e.Parameter as AppSetting;
            this._allData.Remove(item);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.LoadData();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this._allData.Save();
                MessageBox.Show("保存成功！");
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show("保存失败：没有访问注册表的权限！" + Environment.NewLine + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败：" + Environment.NewLine + ex.Message);
            }
        }

        private const string DefaultAppName = "名称";
        private const string DefaultExePath = "路径（支持直接拖动文件或文件夹到这里）";

        private void AddNew()
        {
            this._allData.Insert(0, new AppSetting
            {
                AppName = DefaultAppName,
                ExePath = DefaultExePath
            });
        }

        #endregion

        #region 拖拽文件

        private void ExePath_PreviewDrag(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Link;
            e.Handled = true;
        }

        private void ExePath_PreviewDrop(object sender, DragEventArgs e)
        {
            var data = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (data != null)
            {
                var item = (e.Source as TextBox).DataContext as AppSetting;
                item.ExePath = data[0];
            }
        }

        #endregion

        #region 过滤 搜索

        private const string SeachInitText = "输入搜索";

        private Timer _searchInterval;

        private void InitSeaching()
        {
            this.Loaded += (o, e) =>
            {
                tbSearch.Focus();
                tbSearch.SelectAll();
            };

            this._searchInterval = new Timer();
            this._searchInterval.AutoReset = false;
            this._searchInterval.Interval = 300;
            this._searchInterval.Elapsed += new ElapsedEventHandler(_searchInterval_Elapsed);
        }

        private void _searchInterval_Elapsed(object sender, ElapsedEventArgs e)
        {
            Action a = () =>
            {
                string searchText = tbSearch.Text.ToLower().Replace(SeachInitText, string.Empty);
                bool searchPath = cbSearchPath.IsChecked.GetValueOrDefault();
                bool isTemp = cbFilterTemp.IsChecked.GetValueOrDefault();

                this._allDataView.Filter = o =>
                {
                    var i = o as AppSetting;

                    if (isTemp && !i.IsTemp) return false;

                    return i.AppName.ToLower().Contains(searchText) ||
                        searchPath && i.ExePath.ToLower().Contains(searchText);
                };
            };
            this.Dispatcher.BeginInvoke(a);
        }

        private void RefreshFilter()
        {
            if (this._searchInterval != null)
            {
                this._searchInterval.Stop();
                this._searchInterval.Start();
            }
        }

        private void tbSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.RefreshFilter();
        }

        private void cbFilterTemp_Click(object sender, RoutedEventArgs e)
        {
            this.RefreshFilter();
        }

        private void cbSearchPath_Click(object sender, RoutedEventArgs e)
        {
            this.RefreshFilter();
        }

        private void tbSearch_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (tbSearch.Text == SeachInitText) tbSearch.Text = string.Empty;
        }

        private void ResetSearchState()
        {
            if (this._allData.Count >= 5)
            {
                tbSearch.Visibility = cbSearchPath.Visibility = cbFilterTemp.Visibility
                    = Visibility.Visible;
            }
        }

        #endregion

        #region 其它方法

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            this.AddNew();
        }

        private void TextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var text = e.Source as TextBox;
            if (text.Text == DefaultAppName
                || text.Text == DefaultExePath)
            {
                text.Tag = text.Text;
                text.Clear();
            }
        }

        private void TextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var text = e.Source as TextBox;
            if (string.IsNullOrEmpty(text.Text))
            {
                text.AppendText(text.Tag.ToString());
                text.Tag = null;
            }
        }

        private void OpenItemDir_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            OpenItemDirCore(e.Parameter as AppSetting);
        }

        internal bool TryOpenItemDir(string appName)
        {
            var appSetting = this._allData.FirstOrDefault(a => a.AppName == appName);
            if (appSetting != null)
            {
                OpenItemDirCore(appSetting);
                return true;
            }
            return false;
        }

        private static void OpenItemDirCore(AppSetting item)
        {
            var path = item.ExePath.Replace("\"", string.Empty);

            if (File.Exists(path))
            {
                Process.Start("explorer.exe", "/select," + path);
            }
            else
            {
                //文件不存在时，打开上层目录。
                while (!Directory.Exists(path))
                {
                    path = System.IO.Path.GetDirectoryName(path);
                }
                Process.Start(path);
            }
        }

        private static void ShowHelp()
        {
            return;
            var txtHelp = File.ReadAllText(@"Help.txt");
            MessageBox.Show(txtHelp + @"

版本号：" + CurrentVersion + @"
作者：胡庆访", "帮助");
        }


        #endregion

        /// <summary>
        /// 扫描所有快捷方式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnScan_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("注册表扫描？", "扫描提示！", MessageBoxButton.YesNoCancel);
            if (res == MessageBoxResult.Yes)
            {
                ScanLnks.ScanRegistry(this._allData);
                return;
            }

            res = MessageBox.Show("全盘扫描？", "扫描提示！", MessageBoxButton.YesNoCancel);
            if (res == MessageBoxResult.Yes)
            {
                ScanLnks.ScanAll(this._allData);
            }
        }
    }
}