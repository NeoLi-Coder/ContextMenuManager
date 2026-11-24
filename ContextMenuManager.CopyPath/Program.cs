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
            if (args.Length >= 3 && args[0] == "--drop" && args[1] is "native" or "forward"
                && Guid.TryParse(args[2], out var clsid))
                return DropServer.Run(clsid, args[1]);
            if (args.Length != 2 || (args[0] != "native" && args[0] != "forward")) return 1;
            if (!args[1].EndsWith('|')) return 1;
            return Copy(args[0], new[] { args[1][..^1] });
        }

        internal static int Copy(string mode, string[] paths)
        {
            if (paths.Length == 0 || !Array.TrueForAll(paths, Path.IsPathFullyQualified)) return 1;
            var text = string.Join(Environment.NewLine, paths);
            if (mode == "forward") text = text.Replace('\\', '/');
            try
            {
                Clipboard.SetDataObject(text, true);
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
