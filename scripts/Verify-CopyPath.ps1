param(
    [string]$HelperPath = "$PSScriptRoot\..\ContextMenuManager\bin\ContextMenuManager_Portable\ContextMenuManager.CopyPath.exe"
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore
$helper = (Resolve-Path -LiteralPath $HelperPath).Path
$original = [Windows.Clipboard]::GetDataObject()
$paths = @('C:\', 'C:\中文 目录\a''b & $x; (1) [2] %.txt', '\\server\共享目录\文件.txt')
try {
    foreach ($mode in @('native', 'forward')) {
        foreach ($path in $paths) {
            $info = [Diagnostics.ProcessStartInfo]::new($helper)
            $info.UseShellExecute = $false
            $info.Arguments = $mode + ' "' + $path + '|"'
            $process = [Diagnostics.Process]::Start($info)
            $process.WaitForExit()
            if ($process.ExitCode -ne 0) { throw "复制程序退出码：$($process.ExitCode)" }
            $expected = if ($mode -eq 'forward') { $path.Replace('\', '/') } else { $path }
            if ([Windows.Clipboard]::GetText() -cne $expected) { throw "路径复制不一致：$mode / $path" }
            $process.Dispose()
        }
    }
    Write-Output '两种格式的盘符根目录、中文、空格、特殊字符及 UNC 路径验证通过。'
}
finally {
    if ($null -eq $original) { [Windows.Clipboard]::Clear() }
    else { [Windows.Clipboard]::SetDataObject($original, $true) }
}
