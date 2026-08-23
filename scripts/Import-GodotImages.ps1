param(
    [string]$InputPath = $env:INPUT_PATH,
    [string]$OutputPath = $env:OUTPUT_PATH,
    [string]$PropsPath = "./Directory.Build.props",
    [switch]$DoImport = $false
)

# Configuration
$GdScript = "res://import_images.gd"

# Allow non-fatal stderr warnings (like missing game paths) to pass through
$ErrorActionPreference = "Continue"

if (-not (Test-Path $PropsPath)) {
    throw "Property file not found at '$PropsPath'"
}

[xml]$props = Get-Content $PropsPath
$godotExe = $props.Project.PropertyGroup.GodotPath.Trim();

if ([string]::IsNullOrWhiteSpace($godotExe)) {
    throw "GodotPath element is missing or empty in '$PropsPath'"
}

if ($DoImport) {
    cmd /c `"$godotExe`" --headless --import
}

if ([string]::IsNullOrWhiteSpace($InputPath) -or [string]::IsNullOrWhiteSpace($OutputPath)) {
    if ($DoImport) {
        exit;
    }
    throw "Both InputPath and OutputPath must be specified via arguments or environment variables (INPUT_PATH, OUTPUT_PATH)."
}

# Run Godot via CMD wrapper to bypass PowerShell stream interception
cmd /c `"$godotExe`" --headless -s $GdScript -- $InputPath $OutputPath
if ($LastExitCode -ne 0) {
    Write-Warning "First attempt failed with exit code $LastExitCode (Imports were missing). Retrying..."

    # Second attempt
    cmd /c `"$godotExe`" --headless -s $GdScript -- $InputPath $OutputPath
}
# & "$godotExe" --headless --import -s $GdScript -- $InputPath $OutputPath

# Catch real Godot crashes
if ($LASTEXITCODE -ne 0) {
    [Console]::Stderr.WriteLine("[ERROR] Godot exited with code $LASTEXITCODE")
    exit $LASTEXITCODE
}