using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace ContextMenuManager.CopyPath
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length != 2 || (args[0] != "native" && args[0] != "forward")) return 1;
            if (!args[1].EndsWith('|')) return 1;
            var path = args[1][..^1];
            if (!Path.IsPathFullyQualified(path)) return 1;
            if (args[0] == "forward") path = path.Replace('\\', '/');
            try
            {
                Clipboard.SetDataObject(path, true);
                return 0;
            }
            catch (ExternalException)
            {
                MessageBox.Show("剪贴板当前不可用，请关闭占用剪贴板的程序后重新复制。", "复制路径", MessageBoxButton.OK, MessageBoxImage.Error);
                return 1;
            }
        }
    }
}
