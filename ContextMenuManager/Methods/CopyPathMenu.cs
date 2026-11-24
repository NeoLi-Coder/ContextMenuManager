using ContextMenuManager.Properties;
using Microsoft.Win32;
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

        private static string HelperPath => Path.Combine(AppContext.BaseDirectory, "ContextMenuManager.CopyPath.exe");

        private static string GetHelper()
        {
            var helper = HelperPath;
            if (!File.Exists(helper)) throw new FileNotFoundException("未找到复制路径程序，请使用完整的发布包。", helper);
            return helper;
        }

        public static string GetCommand(string mode)
        {
            // 用路径中不允许的字符隔开末尾反斜杠与引号，复制程序会移除该字符。
            return SingleCommand(GetHelper(), mode);
        }

        private static string SingleCommand(string helper, string mode) => $"\"{helper}\" {mode} \"%1|\"";
        private static string ServerKey(string id) => $@"HKEY_CLASSES_ROOT\CLSID\{id}\LocalServer32";
        private static string ServerCommand(string mode, string id) => $"\"{GetHelper()}\" --drop {mode} {id}";

        private static string TargetId(string mode) => mode switch
        {
            "native" => "{84546BBA-201A-49BC-BA5F-9AE94AA4821B}",
            "forward" => "{C31987DC-D4FD-4D80-A12E-C30DB338745D}",
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };

        public static void RegisterTarget(string verbPath, string mode)
        {
            var id = TargetId(mode);
            Registry.SetValue(ServerKey(id), "", ServerCommand(mode, id));
            Registry.SetValue(verbPath + @"\DropTarget", "CLSID", id);
            Registry.SetValue(verbPath, "CopyPathMode", mode);
        }

        public static void UnregisterTarget(string verbPath, string mode)
        {
            var id = TargetId(mode);
            if (Equals(Registry.GetValue(verbPath + @"\DropTarget", "CLSID", null), id))
                Registry.ClassesRoot.DeleteSubKeyTree($@"CLSID\{id}", false);
        }

        public static void Upgrade(XmlDocument dictionary)
        {
            var shell = dictionary?.SelectSingleNode(GroupQuery);
            if (shell == null) return;
            XmlDocument defaults = null;
            foreach (var name in new[] { "CopyAsPath", "Windows.CopyAsPath" })
            {
                var hasCurrent = shell.SelectSingleNode($"Item[@KeyName='{name}']/SubKey/DropTarget[@CopyPathMode]") != null;
                foreach (XmlElement item in shell.SelectNodes($"Item[@KeyName='{name}']"))
                {
                    var command = (XmlElement)item.SelectSingleNode("SubKey/Command");
                    var oldCommand = command?.GetAttribute("Default");
                    var isLegacy = name == "CopyAsPath"
                        ? oldCommand == OldPowerShell || oldCommand == OldMshta
                        : item.SelectSingleNode($"Value/REG_SZ[@VerbHandler='{OldHandler}']") != null;
                    if (!isLegacy && command?.GetAttribute("CopyPathMode") != (name == "CopyAsPath" ? "native" : "forward")) continue;
                    if (hasCurrent) shell.RemoveChild(item);
                    else
                    {
                        if (defaults == null)
                        {
                            defaults = new XmlDocument();
                            defaults.LoadXml(AppResources.EnhanceMenusDic);
                        }
                        var replacement = defaults.SelectSingleNode($"{GroupQuery}/Item[@KeyName='{name}']");
                        shell.ReplaceChild(dictionary.ImportNode(replacement, true), item);
                    }
                    hasCurrent = true;
                }
                var current = (XmlElement)shell.SelectSingleNode($"Item[@KeyName='{name}']/SubKey/DropTarget");
                if (current?.HasAttribute("CopyPathMode") != true) continue;
                UpgradeRegistry(name, current.GetAttribute("CopyPathMode"));
            }
        }

        private static void UpgradeRegistry(string name, string mode)
        {
            // 用户字典中的同名项由用户维护，不自动迁移。
            if (XmlDicHelper.EnhanceMenusDic[1]?.SelectSingleNode($"{GroupQuery}/Item[@KeyName='{name}']") != null) return;
            var verbPath = ShellPath + name;
            using var key = RegistryEx.GetRegistryKey(verbPath);
            if (key == null) return;
            using var commandKey = key.OpenSubKey("command");
            var oldCommand = commandKey?.GetValue("") as string;
            var isLegacy = name == "CopyAsPath"
                ? oldCommand == OldPowerShell || oldCommand == OldMshta
                : string.Equals(key.GetValue("VerbHandler") as string, OldHandler, StringComparison.OrdinalIgnoreCase);
            var wasSingle = oldCommand != null && (oldCommand == SingleCommand(HelperPath, mode)
                || (key.GetValue("CopyPathMode") as string == mode
                    && oldCommand.EndsWith($"\\ContextMenuManager.CopyPath.exe\" {mode} \"%1|\"", StringComparison.OrdinalIgnoreCase)));
            using var target = key.OpenSubKey("DropTarget");
            var id = TargetId(mode);
            var isCurrent = Equals(key.GetValue("CopyPathMode"), mode) && Equals(target?.GetValue("CLSID"), id);
            if (!isLegacy && !wasSingle && !isCurrent) return;
            var menuText = mode == "forward" ? (AppConfig.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "Copy Path (Forward Slashes)" : "复制路径（正斜杠）") : "@shell32.dll,-30329";
            if (!isLegacy && !wasSingle)
            {
                var server = ServerCommand(mode, id);
                if (!Equals(Registry.GetValue(ServerKey(id), "", null), server))
                    Registry.SetValue(ServerKey(id), "", server);
                if (Equals(key.GetValue("MultiSelectModel"), "Player") && Equals(key.GetValue("MUIVerb"), menuText)) return;
            }
            using var destination = RegistryEx.GetRegistryKey(verbPath, true);
            if (destination == null) return;
            if (isLegacy || wasSingle)
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
                RegisterTarget(verbPath, mode);
                if (commandKey != null)
                {
                    using var oldKey = destination.OpenSubKey("command", true);
                    oldKey.DeleteValue("", false);
                    if (oldKey.ValueCount == 0 && oldKey.SubKeyCount == 0) destination.DeleteSubKey("command");
                }
            }
            destination.SetValue("MultiSelectModel", "Player");
            destination.SetValue("MUIVerb", menuText);
            if (isLegacy)
            {
                foreach (var value in new[] { "CommandStateHandler", "CommandStateSync", "CanonicalName", "VerbHandler", "VerbName", "Description", "InvokeCommandOnSelection" })
                    destination.DeleteValue(value, false);
            }
        }
    }
}
