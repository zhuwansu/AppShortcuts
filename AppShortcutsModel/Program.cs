using AppShortcuts;
using System.Windows;

namespace AppShortcutsModel
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args != null)
            {
                //防止直接打开 AppShortcutsModel.exe 清除所有记录
                if (args.Length == 1 && args[0] == AppSetting.AuthToken)
                {
                    AppSettingCollection.SaveShortcutsToRegistry();
                }
            }
#if DEBUG
            System.Console.WriteLine("按任意键结束...");
            System.Console.ReadLine();
#endif
        }
    }
}
