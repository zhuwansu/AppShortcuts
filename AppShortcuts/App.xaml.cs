using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace AppShortcuts
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWin = new MainWindow();

            if (e.Args.Length != 0 && mainWin.TryOpenItemDir(e.Args[0]))
            {
                this.Shutdown();
            }
            else
            {
                mainWin.Show();
            }
        }
    }
}
