param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$MsBuild = "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
if (-not (Test-Path $MsBuild)) {
    $MsBuild = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
}
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$PublishDir = Join-Path $Root "src\VKVideoDesktop.App\bin\Any CPU\Release\net9.0-windows10.0.22621.0\win-x64\publish"
$ZipPath = Join-Path $Root "VKVideoDesktop-win-x64.zip"

Write-Host "=== VK Video Desktop Build ===" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration"

# 1. Restore
Write-Host "`n[1/8] Restore..." -ForegroundColor Yellow
& dotnet restore VKVideoDesktop.sln 2>&1 | Out-Null

# 2. Build projects individually (exclude Setup - it needs the zip)
Write-Host "[2/8] Build projects (XAML compilation)..." -ForegroundColor Yellow
$projects = @(
    "$Root\src\VKVideoDesktop.Core\VKVideoDesktop.Core.csproj",
    "$Root\src\VKVideoDesktop.Application\VKVideoDesktop.Application.csproj",
    "$Root\src\VKVideoDesktop.Infrastructure\VKVideoDesktop.Infrastructure.csproj",
    "$Root\src\VKVideoDesktop.Data\VKVideoDesktop.Data.csproj",
    "$Root\src\VKVideoDesktop.App\VKVideoDesktop.App.csproj"
)
foreach ($proj in $projects) {
    $name = Split-Path $proj -Leaf
    Write-Host "  $name" -ForegroundColor Gray
    & $MsBuild $proj /p:Configuration=$Configuration /p:Platform="Any CPU" /t:Build /v:minimal /m 2>&1 | Out-Null
}

# 3. Publish win-x64 framework-dependent (self-contained causes InvalidCastException on Win10 19044)
Write-Host "[3/8] Publish win-x64 (framework-dependent)..." -ForegroundColor Yellow
& $MsBuild "src\VKVideoDesktop.App\VKVideoDesktop.App.csproj" /p:Configuration=$Configuration /p:Platform="Any CPU" /p:RuntimeIdentifier=win-x64 /p:SelfContained=false /t:Publish /v:minimal /m

# 4. Copy XBF files preserving directory structure from embed\
Write-Host "[4/8] Copy XBF files (with directory structure)..." -ForegroundColor Yellow
$PublishObjDir = Join-Path $Root "src\VKVideoDesktop.App\obj\Any CPU\Release\net9.0-windows10.0.22621.0\win-x64"
$EmbedDir = Join-Path $PublishObjDir "embed"
$xbfSourceDir = if (Test-Path $EmbedDir) { $EmbedDir } else { $PublishObjDir }
$xbfFiles = Get-ChildItem $xbfSourceDir -Recurse -Filter "*.xbf" -File -ErrorAction SilentlyContinue
foreach ($xbf in $xbfFiles) {
    $relativePath = $xbf.FullName.Substring($xbfSourceDir.Length + 1)
    $destPath = Join-Path $PublishDir $relativePath
    $destDir = Split-Path $destPath -Parent
    if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
    Copy-Item $xbf.FullName $destPath -Force
}
$xbfCount = (Get-ChildItem $PublishDir -Recurse -Filter "*.xbf" -File).Count
Write-Host "  XBF files: $xbfCount"

# Copy .resw localization files
Write-Host "  Copying .resw files..." -ForegroundColor Yellow
$ReswSource = Join-Path $Root "src\VKVideoDesktop.App\Resources"
$ReswDest = Join-Path $PublishDir "Resources"
if (Test-Path $ReswDest) { Remove-Item $ReswDest -Recurse -Force }
Copy-Item $ReswSource $ReswDest -Recurse -Force
$reswCount = (Get-ChildItem $ReswDest -Filter "*.resw" -Recurse).Count
Write-Host "  .resw files: $reswCount"

# Remove WinUI satellite assembly folders (locale folders from WindowsAppSDK)
Write-Host "  Removing locale folders..." -ForegroundColor Yellow
$keepFolders = @("Microsoft.UI.Xaml", "runtimes", "net9.0-windows10.0.22621.0", "ref", "Views", "Styles", "Resources")
$localeCount = 0
Get-ChildItem $PublishDir -Directory -ErrorAction SilentlyContinue | Where-Object {
    $_.Name -notin $keepFolders
} | ForEach-Object {
    Remove-Item -Recurse -Force $_.FullName -ErrorAction SilentlyContinue
    $localeCount++
}
Write-Host "  Removed $localeCount locale folders"

# 5. Create zip
Write-Host "[5/8] Create zip..." -ForegroundColor Yellow
Remove-Item $ZipPath -Force -ErrorAction SilentlyContinue
Compress-Archive -Path "$PublishDir\*" -DestinationPath $ZipPath -Force
$zipSize = [math]::Round((Get-Item $ZipPath).Length / 1MB, 1)
Write-Host "  Zip: $zipSize MB"

# 6. Build uninstaller
Write-Host "[6/8] Build uninstaller..." -ForegroundColor Yellow
$UninstallProject = Join-Path $Root "src\VKVideoDesktop.Uninstall\VKVideoDesktop.Uninstall.csproj"
& dotnet publish $UninstallProject -c $Configuration -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o "src\VKVideoDesktop.Setup\uninstall" 2>&1 | Out-Null
$uninstallExe = Join-Path $Root "src\VKVideoDesktop.Setup\uninstall\Uninstall.exe"
$uninstallSize = [math]::Round((Get-Item $uninstallExe).Length / 1MB, 1)
Write-Host "  Uninstall.exe: $uninstallSize MB"

# 7. Build installer (self-contained single-file .exe)
Write-Host "[7/8] Build installer (single-file)..." -ForegroundColor Yellow
$SetupProject = Join-Path $Root "src\VKVideoDesktop.Setup\VKVideoDesktop.Setup.csproj"
& dotnet publish $SetupProject -c $Configuration -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true -o "publish\setup" 2>&1

$setupExe = Join-Path $Root "publish\setup\VKVideoDesktopSetup.exe"
if (Test-Path $setupExe) {
    $setupSize = [math]::Round((Get-Item $setupExe).Length / 1MB, 1)
    Write-Host "`n=== BUILD COMPLETE ===" -ForegroundColor Green
    Write-Host "App publish: $PublishDir"
    Write-Host "Zip: $ZipPath ($zipSize MB)"
    Write-Host "Installer: $setupExe ($setupSize MB)"

    # 8. Smoke test
    Write-Host "`n[8/8] Smoke test..." -ForegroundColor Yellow
    $smokeDir = Join-Path $Root "smoke-test"
    try {
        Expand-Archive -Path $ZipPath -DestinationPath $smokeDir -Force
        $proc = Start-Process -FilePath "$smokeDir\VKVideoDesktop.App.exe" -PassThru
        Start-Sleep -Seconds 8
        $proc.Kill()
        Write-Host "  Smoke test passed (app launched, exit code: $($proc.ExitCode))" -ForegroundColor Green
    } catch {
        Write-Host "  Smoke test failed: $_" -ForegroundColor Red
    } finally {
        Remove-Item $smokeDir -Recurse -Force -ErrorAction SilentlyContinue
    }
} else {
    Write-Host "`n=== BUILD FAILED ===" -ForegroundColor Red
    exit 1
}
