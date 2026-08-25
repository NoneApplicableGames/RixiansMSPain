# 1. Path to your props file in the current directory
$PropsPath = Join-Path $PSScriptRoot "Directory.Build.props"

if (-not (Test-Path -Path $PropsPath)) {
    Write-Error "Could not find Directory.Build.props at: $PropsPath"
    Exit
}

# 2. Load and parse the XML
[xml]$PropsConfig = Get-Content -Path $PropsPath

# 3. Extract the Godot path from the XML
$GodotPath = $PropsConfig.Project.PropertyGroup.GodotPath

# 4. Set the project path to THIS directory (where the script is running)
$ProjectPath = $PSScriptRoot

if ([string]::IsNullOrEmpty($GodotPath)) {
    Write-Error "Failed to extract GodotPath from Directory.Build.props."
    Exit
}

# 5. Enforce targeted framework translation constraints
$env:DOTNET_ROOT = "C:\Program Files\dotnet"

# Tells .NET to roll forward to the highest minor release, but NEVER change the major release.
# This builds a hard ceiling that traps it within .NET 9.x and completely hides .NET 10.
$env:DOTNET_ROLL_FORWARD = "Minor"

# Prevents the host from falling back to higher major versions if a perfect match isn't found.
$env:DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX = "0"

Write-Host "Configuring hybrid environment (.NET 8 Editor -> .NET 9 Target)..." -ForegroundColor Cyan
Write-Host "Launching MegaDot: $GodotPath" -ForegroundColor Green
Write-Host "Opening Current Project: $ProjectPath" -ForegroundColor Green

# 6. Execute MegaDot inside this directory
Start-Process -FilePath $GodotPath -ArgumentList "--path `"$ProjectPath`" --editor"