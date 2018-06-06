using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace AppShortcuts
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                if (e.Args.Length != 0)
                {
                    var firstArg = e.Args[0];
                    if (firstArg == "-p"
                        || firstArg == "-path")
                    {
                        if (Clipboard.ContainsText())
                        {
                            var text = Clipboard.GetText();
                            AppShortcuts.MainWindow.OpenDir(text);
                        }
                    }
                    else
                    {
                        var mainWin = new MainWindow();
                        mainWin.TryOpenItemDir(firstArg);
                    }
                }
                else
                {
                    var mainWin = new MainWindow();
                    mainWin.Show();
                    return;
                }
            }
            finally
            {
                this.Shutdown();
            }
        }
    }
}
