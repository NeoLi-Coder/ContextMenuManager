using ContextMenuManager.Properties;
using System;
using System.IO;
using System.Xml;

namespace ContextMenuManager.Methods
{
    internal static class CopyPathMenu
    {
        private const string ShellPath = @"HKEY_CLASSES_ROOT\AllFilesystemObjects\shell\";
        private const string GroupQuery = "Data/Group[RegPath='HKEY_CLASSES_ROOT\\AllFilesystemObjects']/Shell";
        private const string OldPowerShell = "powershell -WindowStyle Hidden -Command \"Set-Clipboard -Value $args[0]\" \"%1\"";
        private const string OldMshta = "mshta vbscript:clipboarddata.setdata(\"text\",\"%1\")(close)";
        private const string OldHandler = "{f3d06e7c-1e45-4a26-847e-f9fcdee59be0}";

        public static string GetCommand(string mode)
        {
            var helper = Path.Combine(AppContext.BaseDirectory, "ContextMenuManager.CopyPath.exe");
            if (!File.Exists(helper)) throw new FileNotFoundException("未找到复制路径程序，请使用完整的发布包。", helper);
            // 用路径中不允许的字符隔开末尾反斜杠与引号，复制程序会移除该字符。
            return $"\"{helper}\" {mode} \"%1|\"";
        }

        public static void Upgrade(XmlDocument dictionary)
        {
            var shell = dictionary?.SelectSingleNode(GroupQuery);
            if (shell == null) return;
            var defaults = new XmlDocument();
            defaults.LoadXml(AppResources.EnhanceMenusDic);
            foreach (var name in new[] { "CopyAsPath", "Windows.CopyAsPath" })
            {
                var replacement = defaults.SelectSingleNode($"{GroupQuery}/Item[@KeyName='{name}']");
                var hasCurrent = shell.SelectSingleNode($"Item[@KeyName='{name}']/SubKey/Command[@CopyPathMode]") != null;
                foreach (XmlElement item in shell.SelectNodes($"Item[@KeyName='{name}']"))
                {
                    var command = (XmlElement)item.SelectSingleNode("SubKey/Command");
                    var oldCommand = command?.GetAttribute("Default");
                    var isLegacy = name == "CopyAsPath"
                        ? oldCommand == OldPowerShell || oldCommand == OldMshta
                        : item.SelectSingleNode($"Value/REG_SZ[@VerbHandler='{OldHandler}']") != null;
                    if (!isLegacy) continue;
                    if (hasCurrent) shell.RemoveChild(item);
                    else shell.ReplaceChild(dictionary.ImportNode(replacement, true), item);
                    hasCurrent = true;
                }
                var current = (XmlElement)shell.SelectSingleNode($"Item[@KeyName='{name}']/SubKey/Command");
                if (current?.HasAttribute("CopyPathMode") != true) continue;
                UpgradeRegistry(name, current.GetAttribute("CopyPathMode"));
            }
        }

        private static void UpgradeRegistry(string name, string mode)
        {
            // 用户字典中的同名项由用户维护，不自动迁移。
            if (XmlDicHelper.EnhanceMenusDic[1]?.SelectSingleNode($"{GroupQuery}/Item[@KeyName='{name}']") != null) return;
            using var key = RegistryEx.GetRegistryKey(ShellPath + name, true);
            if (key == null) return;
            using var commandKey = key.OpenSubKey("command");
            var oldCommand = commandKey?.GetValue("") as string;
            var isLegacy = name == "CopyAsPath"
                ? oldCommand == OldPowerShell || oldCommand == OldMshta
                : string.Equals(key.GetValue("VerbHandler") as string, OldHandler, StringComparison.OrdinalIgnoreCase);
            var command = GetCommand(mode);
            var isManaged = key.GetValue("CopyPathMode") as string == mode
                && oldCommand?.EndsWith($"\\ContextMenuManager.CopyPath.exe\" {mode} \"%1|\"", StringComparison.OrdinalIgnoreCase) == true;
            if (!isLegacy && oldCommand != command && !isManaged) return;
            if (isLegacy)
            {
                var backup = Path.Combine(AppConfig.RegBackupDir, "CopyPathUpgrade");
                Directory.CreateDirectory(backup);
                using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("reg.exe")
                {
                    Arguments = $"export \"{ShellPath + name}\" \"{Path.Combine(backup, name + "-" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".reg")}\" /y",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                process.WaitForExit();
                if (process.ExitCode != 0) throw new IOException("复制路径菜单备份失败，未执行迁移。");
            }
            key.SetValue("MultiSelectModel", "Single");
            key.SetValue("CopyPathMode", mode);
            key.SetValue("MUIVerb", mode == "forward" ? (AppConfig.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "Copy Path (Forward Slashes)" : "复制路径（正斜杠）") : "@shell32.dll,-30329");
            using var destination = key.CreateSubKey("command");
            destination.SetValue("", command);
            if (isLegacy)
            {
                foreach (var value in new[] { "CommandStateHandler", "CommandStateSync", "CanonicalName", "VerbHandler", "VerbName", "Description", "InvokeCommandOnSelection" })
                    key.DeleteValue(value, false);
            }
        }
    }
}
