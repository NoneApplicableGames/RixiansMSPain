param([switch]$Clean)
$ErrorActionPreference = "Stop"

# This script lives in build/, but local.props, .godot, and Downfall.csproj
# live at the project root — one level up.
$ProjectRoot = Split-Path $PSScriptRoot -Parent
Set-Location $ProjectRoot

if (-not (Test-Path "Directory.Build.props")) {
    Write-Host "ERROR: Directory.Build.props not found. Copy Directory.Build.props.example to Directory.Build.props." -ForegroundColor Red
    exit 1
}
if ($Clean) {
    Remove-Item -Recurse -Force .godot -ErrorAction SilentlyContinue
    Write-Host "Cleaned .godot"
}
# Write-Host "=== Generating images (ImageGen) ==="
# dotnet run --project ImageGen/ImageGen.csproj
# if ($LASTEXITCODE -ne 0) { throw "ImageGen failed" }

Write-Host "=== Building HideDetailsMod ==="
dotnet build HideDetailsMod.csproj --nologo -v q
if ($LASTEXITCODE -ne 0) {
    Write-Host "Retrying HideDetailsMod (cold publicizer cache)..." -ForegroundColor Yellow
    dotnet build Downfall.csproj --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "HideDetailsMod build failed" }
}

$pubDir = ".godot\mono\temp\obj\Debug\PublicizedAssemblies"
$sts2Pub = Get-ChildItem -Path $pubDir -Recurse -Filter "sts2.dll" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $sts2Pub) { throw "Publicized sts2.dll not found" }
Write-Host "Publicized assemblies ready"

Write-Host "`HideDetailsMod BUILT SUCCESSFULLY" -ForegroundColor Green