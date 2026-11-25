#requires -Version 7.0
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '../ContextMenuManager'
$stage = New-Item -ItemType Directory -Path (Join-Path ([IO.Path]::GetTempPath()) ("ContextMenuManager-publish-" + [guid]::NewGuid()))
try {
    $portable = Join-Path $stage.FullName 'ContextMenuManager_Portable'
    dotnet publish "$project/ContextMenuManager.csproj" -p:PublishProfile=Net10.0-Win64.pubxml --output $portable | Out-Host
    if ($LASTEXITCODE -ne 0) { throw '便携版发布失败。' }

    $version = (Get-Item "$portable/ContextMenuManager.dll").VersionInfo.FileVersion
    $name = "ContextMenuManager-$version-win-x64-portable.zip"
    $archive = Join-Path $stage.FullName $name
    [IO.Compression.ZipFile]::CreateFromDirectory($portable, $archive, 'Optimal', $true)

    $zip = [IO.Compression.ZipFile]::OpenRead($archive)
    try {
        foreach ($app in 'ContextMenuManager', 'ContextMenuManager.CopyPath') {
            $entry = $zip.GetEntry("ContextMenuManager_Portable/$app.runtimeconfig.json")
            $reader = [IO.StreamReader]::new($entry.Open())
            try { $runtime = ($reader.ReadToEnd() | ConvertFrom-Json).runtimeOptions } finally { $reader.Dispose() }
            if ($runtime.frameworks -or 'Microsoft.WindowsDesktop.App' -notin $runtime.includedFrameworks.name) {
                throw "不能发布：ZIP 中的 $app 不是自包含桌面程序。"
            }
        }
    } finally { $zip.Dispose() }

    $out = New-Item -ItemType Directory -Force -Path "$project/bin/$version"
    Copy-Item -LiteralPath $archive -Destination $out.FullName
    Get-Item (Join-Path $out.FullName $name)
} finally {
    Remove-Item -LiteralPath $stage.FullName -Recurse -Force
}
