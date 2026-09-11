param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$MsBuild = "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
if (-not (Test-Path $MsBuild)) {
    $MsBuild = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
}
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$AppProject = Join-Path $Root "src\VKVideoDesktop.App\VKVideoDesktop.App.csproj"
$SetupProject = Join-Path $Root "src\VKVideoDesktop.Setup\VKVideoDesktop.Setup.csproj"
$PublishDir = Join-Path $Root "src\VKVideoDesktop.App\bin\$Configuration\net9.0-windows10.0.22621.0\win-x64\publish"
$ObjDir = Join-Path $Root "src\VKVideoDesktop.App\obj\$Configuration\net9.0-windows10.0.22621.0\win-x64"
$ZipPath = Join-Path $Root "VKVideoDesktop-win-x64.zip"

Write-Host "=== VK Video Desktop Build ===" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration"

# 1. Clean
Write-Host "`n[1/6] Clean..." -ForegroundColor Yellow
Remove-Item -Recurse -Force (Join-Path $Root "src\VKVideoDesktop.App\obj") -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force (Join-Path $Root "src\VKVideoDesktop.Setup\obj") -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force (Join-Path $Root "src\VKVideoDesktop.Setup\bin") -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force $PublishDir -ErrorAction SilentlyContinue

# 2. Restore
Write-Host "[2/6] Restore..." -ForegroundColor Yellow
& dotnet restore VKVideoDesktop.sln 2>&1 | Out-Null

# 3. Build solution with VS MSBuild (XAML compilation)
Write-Host "[3/6] Build solution..." -ForegroundColor Yellow
& $MsBuild VKVideoDesktop.sln /p:Configuration=$Configuration /p:Platform="Any CPU" /t:Build /v:minimal /m

# 4. Publish win-x64 self-contained
Write-Host "[4/6] Publish win-x64..." -ForegroundColor Yellow
& $MsBuild $AppProject /p:Configuration=$Configuration /p:RuntimeIdentifier=win-x64 /p:SelfContained=true /t:Publish /v:minimal

# 5. Copy XBF files from obj to publish
Write-Host "[5/6] Copy XBF files..." -ForegroundColor Yellow
$xbfFiles = Get-ChildItem $ObjDir -Recurse -Filter "*.xbf" -File -ErrorAction SilentlyContinue
$xbfFiles | ForEach-Object {
    Copy-Item $_.FullName (Join-Path $PublishDir $_.Name) -Force
}
$xbfCount = (Get-ChildItem $PublishDir -Filter "*.xbf" -File).Count
Write-Host "  XBF files: $xbfCount"

# Create zip
Remove-Item $ZipPath -Force -ErrorAction SilentlyContinue
Compress-Archive -Path "$PublishDir\*" -DestinationPath $ZipPath -Force
$zipSize = [math]::Round((Get-Item $ZipPath).Length / 1MB, 1)
Write-Host "  Zip: $zipSize MB"

# 6. Build installer
Write-Host "[6/6] Build installer..." -ForegroundColor Yellow
& $MsBuild $SetupProject /p:Configuration=$Configuration /t:Build /v:minimal

$setupExe = Join-Path $Root "src\VKVideoDesktop.Setup\bin\$Configuration\net9.0-windows\VKVideoDesktopSetup.exe"
if (Test-Path $setupExe) {
    $setupSize = [math]::Round((Get-Item $setupExe).Length / 1MB, 1)
    Write-Host "`n=== BUILD COMPLETE ===" -ForegroundColor Green
    Write-Host "App publish: $PublishDir"
    Write-Host "Zip: $ZipPath ($zipSize MB)"
    Write-Host "Installer: $setupExe ($setupSize MB)"
} else {
    Write-Host "`n=== BUILD FAILED ===" -ForegroundColor Red
    exit 1
}
