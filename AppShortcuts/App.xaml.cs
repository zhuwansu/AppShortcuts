using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;

namespace AppShortcuts
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Length == 0)
            {
                var mainWin = new MainWindow();
                mainWin.Show();
                return;
            }

            try
            {
                var firstArg = e.Args[0];
                if (firstArg == "-p"
                    || firstArg == "-path")
                {
                    if (Clipboard.ContainsText())
                    {
                        var text = Clipboard.GetText();
                        AppShortcuts.MainWindow.OpenDir(text);
                        return;
                    }
                }
                if (firstArg == "-c"
                    || firstArg == "-chrome")
                {
                    if (Clipboard.ContainsText())
                    {
                        var text = Clipboard.GetText();
                        if (UrlCheck(text))
                        {
                            Process.Start(text);
                            return;
                        }
                    }
                }
                new MainWindow().TryOpenItemDir(firstArg);
            }
            finally
            {
                this.Shutdown();
            }

        }
        private bool UrlCheck(string strUrl)
        {
            if (!strUrl.Contains("http://") && !strUrl.Contains("https://"))
            {
                strUrl = "http://" + strUrl;
            }
            try
            {
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(strUrl);
                myRequest.Method = "HEAD";
                myRequest.Timeout = 10000;  //超时时间10秒
                HttpWebResponse res = (HttpWebResponse)myRequest.GetResponse();
                return (res.StatusCode == HttpStatusCode.OK);
            }
            catch
            {
                return false;
            }
        }
    }
}
