using AppShortcuts;

namespace AppShortcutsModel
{
    class Program
    {
        static void Main(string[] args)
        {
            //防止直接打开 AppShortcutsModel.exe 清除所有记录
            if (args == null
                || args.Length != 1
                || args[0] != AppSetting.AuthToken)
                return;

            System.Console.WriteLine("开始写入注册表...");
            AppSettingCollection.SaveShortcutsToRegistry();
            System.Console.WriteLine("写入注册表成功!");
#if DEBUG
            System.Console.WriteLine("按任意键结束...");
            System.Console.ReadLine();
#endif
        }
    }
}
