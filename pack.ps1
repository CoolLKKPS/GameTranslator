param(
    [string]$Configuration = "Release",
    [string]$TargetFramework = "net481",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = $PSScriptRoot
$projectPath = Join-Path $root "GameTranslator\GameTranslator.csproj"
$outputDir = Join-Path $root "artifacts\bin\GameTranslator\${Configuration}_$TargetFramework"
$manifestPath = Join-Path $root "manifest.json"
$iconPath = Join-Path $root "icon.png"
$readmePath = Join-Path $root "README.md"
$licensePath = Join-Path $root "LICENSE"
$changelogPath = Join-Path $root "CHANGELOG.md"
$thunderstoreDir = Join-Path $root "Thunderstore"
$tempDir = Join-Path $root "temp_package"

$xunityDllPath = "D:\Runtime dependencies (local)\BepInEx\core\Hayrizan-XUnity_AutoTranslator\XUnity.Common.dll"

$manifest = Get-Content $manifestPath | ConvertFrom-Json
$version = $manifest.version_number

if (-not $SkipBuild) {
    dotnet build $projectPath -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0) {
        exit 1
    }
}

if (Test-Path $tempDir) {
    Remove-Item -Recurse -Force $tempDir
}
New-Item -ItemType Directory -Path $tempDir | Out-Null

Copy-Item $manifestPath $tempDir
Copy-Item $iconPath $tempDir
Copy-Item $readmePath $tempDir
Copy-Item $licensePath $tempDir
Copy-Item $changelogPath $tempDir

$pluginsDir = Join-Path $tempDir "BepInEx\plugins"
$coreDir = Join-Path $tempDir "BepInEx\core"
New-Item -ItemType Directory -Path $pluginsDir -Force | Out-Null
New-Item -ItemType Directory -Path $coreDir -Force | Out-Null

$mainDll = Join-Path $outputDir "GameTranslator.dll"
if (-not (Test-Path $mainDll)) {
    exit 1
}
Copy-Item $mainDll $pluginsDir

if (Test-Path $xunityDllPath) {
    Copy-Item $xunityDllPath $coreDir
}

$zipName = "CoolLKK_Group-GameTranslator-$version.zip"
$zipPath = Join-Path $thunderstoreDir $zipName
Get-ChildItem $thunderstoreDir -Filter "CoolLKK_Group-GameTranslator-*.zip" | Remove-Item -Force

Compress-Archive -Path "$tempDir\*" -DestinationPath $zipPath -CompressionLevel Optimal

Remove-Item -Recurse -Force $tempDir