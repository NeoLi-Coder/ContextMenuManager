# ContextMenuManager（NeoLi-Coder fork）

Windows 右键菜单管理器。本仓库 fork 自 [Jack251970/ContextMenuManager](https://github.com/Jack251970/ContextMenuManager)，主要维护下面列出的本地功能修改。

[项目主页](https://github.com/NeoLi-Coder/ContextMenuManager) · [发布下载](https://github.com/NeoLi-Coder/ContextMenuManager/releases) · [问题反馈](https://github.com/NeoLi-Coder/ContextMenuManager/issues)

## 本 fork 的特有修改

### 复制路径

- 使用独立的 `ContextMenuManager.CopyPath.exe` 写入剪贴板，替代旧的 PowerShell、MSHTA 命令和系统处理器。
- 提供“复制路径”和“复制路径（正斜杠）”两种格式，例如 `C:\目录\文件.txt` 与 `C:/目录/文件.txt`，复制结果不带引号。
- 支持盘符根目录、中文、空格、特殊字符和 UNC 路径；菜单按单项选择处理。
- 自动识别并迁移本程序旧版复制路径菜单，修改注册表前导出备份到 `Data/RegBackup/<计算机名>/CopyPathUpgrade`。用户字典中的同名菜单不自动迁移。
- 提供 [scripts/Verify-CopyPath.ps1](scripts/Verify-CopyPath.ps1)，用于验证两种格式的剪贴板结果。

### 窗口位置记忆

- 关闭普通状态的窗口时保存位置，下次启动恢复；首次启动在鼠标所在显示器居中。
- 按目标显示器 DPI 恢复窗口，显示器移除或工作区缩小时调整越界位置与尺寸。

### 便携版与发布打包

- 新使用时默认将配置、语言字典和备份保存在程序旁的 `Data` 目录；已有 AppData 数据且程序旁没有 `Data` 时继续使用原数据。
- 发布包包含语言、菜单字典与复制路径辅助程序，使用自包含的 Windows x64 ZIP 包。
- 移除程序内的捐赠页面、入口和相关资源。

### 中文语言

- 仅保留简体中文和繁体中文，语言加载、选择与在线下载均限定为这两种语言。

### 项目链接

- 关于页显示本 fork 和上游项目地址，异常反馈指向本仓库 Issues。
- 程序检查更新、在线字典与语言下载目前仍使用原有上游 GitHub/Gitee 配置；检查更新可能引导到上游发布页。本 fork 的版本请从本仓库发布页获取。

## 下载与使用

在[发布页](https://github.com/NeoLi-Coder/ContextMenuManager/releases)查看可用版本，下载完整便携版 ZIP，解压后运行 `ContextMenuManager.exe`。请保留同目录的辅助程序和其他发布文件，勿只复制主程序。

当前项目基于 .NET 10 / WPF，现有发布配置为 Windows x64、自包含部署，使用该发布包无需另装 .NET。旧版 Windows 的支持情况请参考上游历史版本，本 fork 不承诺兼容 Windows 7、8、8.1。

复制路径验证可在仓库根目录执行（默认使用本地发布目录）：

```powershell
powershell -NoProfile -STA -File .\scripts\Verify-CopyPath.ps1
```

也可通过 `-HelperPath` 指定辅助程序位置。脚本会临时使用剪贴板，并在结束时恢复原内容。

## 保留的上游功能

启用、禁用和管理文件、文件夹、新建、发送到、打开方式、文件类型及 WinX 等右键菜单；修改名称与图标、添加自定义命令、定位注册表和文件，以及备份与恢复菜单。

程序会修改注册表和文件，操作前建议备份。使用其他管理工具禁用的菜单，请先通过原工具恢复。

## 原作者与致谢

- 本 fork 的直接上游：[Jack251970/ContextMenuManager](https://github.com/Jack251970/ContextMenuManager)，感谢 [Jack251970](https://github.com/Jack251970) 的维护与开发。
- 项目最初基于 [BluePointLilac/ContextMenuManager](https://github.com/BluePointLilac/ContextMenuManager)，感谢原作者 [蓝点 lilac](https://github.com/BluePointLilac)。
- 感谢 [澜芸](https://github.com/LanYun2022) 与 [KamilDev](https://github.com/KamilDev) 制作图标，以及上游贡献者的工作。

本 fork 保留原作者说明及现有版权信息，沿用仓库中的 [GPL-3.0 许可证](LICENSE)。
