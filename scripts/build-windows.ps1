param(
    [string]$Arch = "x64"
)

$ErrorActionPreference = "Stop"
$RID = "win-$Arch"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path "$ScriptDir\..").Path

$UiDir = "$RepoRoot\JobTracker\UserInterface"
$BuildOutput = "$UiDir\build"
$Wwwroot = "$RepoRoot\JobTracker\wwwroot"
$ResourcesWwwroot = "$RepoRoot\JobTracker\Resources\wwwroot"
$Project = "$RepoRoot\JobTracker\JobTracker.csproj"
$Templates = "$RepoRoot\templates\win"
$OutDir = "$RepoRoot\publish\out\$RID"
$DistDir = "$RepoRoot\publish\dist"

$Version = "0.0.0-$(Get-Date -Format 'yyyyMMddHHmmss')"
$PackageName = "JobTracker-$Version-$RID"
$Staging = "$OutDir\staging"

echo "=== Building JobTracker for Windows ($Arch) ==="
echo "Version : $Version"
echo "Output  : $DistDir\$PackageName.zip"
echo ""

# Clean
if (Test-Path $OutDir) { Remove-Item -Recurse -Force $OutDir }
New-Item -ItemType Directory -Force -Path $Staging | Out-Null
New-Item -ItemType Directory -Force -Path $DistDir | Out-Null

# Step 1: Build frontend
if (-not $env:CI) {
    echo "[1/4] Building frontend..."
    Set-Location $UiDir
    npm install
    npm run build
    echo "✓ Frontend built"
} else {
    echo "[1/4] Skipping frontend build (already built in CI)"
}

# Step 2: Copy frontend to wwwroot
echo "[2/4] Copying frontend to wwwroot..."
if (Test-Path $Wwwroot) { Remove-Item -Recurse -Force $Wwwroot }
if (Test-Path $ResourcesWwwroot) { Remove-Item -Recurse -Force $ResourcesWwwroot }
New-Item -ItemType Directory -Force -Path $Wwwroot | Out-Null
New-Item -ItemType Directory -Force -Path $ResourcesWwwroot | Out-Null
Copy-Item "$BuildOutput\*" $Wwwroot -Recurse -Force
Copy-Item "$BuildOutput\*" $ResourcesWwwroot -Recurse -Force
echo "  wwwroot populated"

# Step 3: Publish .NET project
echo "[3/4] Publishing .NET project..."
dotnet publish "$Project" `
    -r $RID `
    -f net8.0-windows `
    -c Release `
    --output "$OutDir\publish"
echo "  Published"

# Step 4: Assemble zip
echo "[4/4] Assembling package..."
Copy-Item "$OutDir\publish\JobTracker.exe" "$Staging\JobTracker.exe"

if (Test-Path $Templates) {
    Copy-Item "$Templates\*" $Staging -Recurse -Force
}

Compress-Archive -Path "$Staging\*" -DestinationPath "$DistDir\$PackageName.zip" -Force

# Clean up
Remove-Item -Recurse -Force $OutDir

echo ""
echo "  Done!"
echo "  Output: $DistDir\$PackageName.zip"