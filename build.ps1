# Usage:
# `build.ps1
# -Clean[False, optional]
# -DoNotStart[False, optional]
# -Configuration[Debug, optional]
# -SkipIconCacheRefresh[False, optional]`

param(
    [string]$Configuration = "Debug",
    [switch]$Clean,
    [switch]$DoNotStart,
    [switch]$SkipIconCacheRefresh
)

taskkill /F /IM UBot.exe > $null 2>&1
taskkill /F /IM sro_client.exe > $null 2>&1

if ($Clean) {
    Write-Output "Performing a clean build..."
    New-Item  -ItemType Directory ".\temp" -ErrorAction SilentlyContinue > $null
    Move-Item ".\Build\User" ".\temp" -ErrorAction SilentlyContinue > $null
    Remove-Item -Recurse -Force ".\Build" -ErrorAction SilentlyContinue > $null
}

Write-Output "Removing Mark-of-the-Web flags from repository files..."
Get-ChildItem -Path "." -Recurse -File -ErrorAction SilentlyContinue |
    Unblock-File -ErrorAction SilentlyContinue

Write-Output "Building with '$Configuration' configuration..."
$vsWherePath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$vsPath = $null

if (Test-Path $vsWherePath) {
    # Insider/Preview sürümlerini bulabilmesi için -prerelease eklendi
    $vsPath = & $vsWherePath -latest -prerelease -property installationPath
}

$msBuildPath = if (![string]::IsNullOrWhiteSpace($vsPath)) {
    Join-Path $vsPath "MSBuild\Current\Bin\MSBuild.exe"
}
else {
    # Eğer vswhere bulamazsa, Insider yolunu en öncelikli kontrol et
    @(
        "C:\Program Files\Microsoft Visual Studio\2022\Preview\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($msBuildPath)) {
    $msBuildCommand = Get-Command "MSBuild.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($msBuildCommand) {
        $msBuildPath = $msBuildCommand.Source
    }
}

if ([string]::IsNullOrWhiteSpace($msBuildPath) -or -not (Test-Path $msBuildPath)) {
    Write-Error "MSBuild.exe was not found. Lütfen Visual Studio Installer üzerinden MSBuild bileşeninin kurulu olduğunu kontrol edin."
    exit 1
}

Write-Output "Using MSBuild at: $msBuildPath"

& $msBuildPath /p:Configuration=$Configuration /p:Platform=x86 UBot.sln > build.log
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed with exit code $LASTEXITCODE. Check .\\build.log for details."
    if (Test-Path "build.log") {
        Get-Content -Path "build.log" -Tail 100
    }
    exit $LASTEXITCODE
}

$targetAssistAssemblyPath = ".\Build\Data\Plugins\UBot.TargetAssist.dll"
$targetAssistProjectPath = ".\Plugins\UBot.TargetAssist\UBot.TargetAssist.csproj"
if ((-not (Test-Path $targetAssistAssemblyPath)) -and (Test-Path $targetAssistProjectPath)) {
    Write-Output "UBot.TargetAssist.dll is missing after solution build. Building TargetAssist plugin project directly..."
    & $msBuildPath /restore /p:Configuration=$Configuration /p:Platform=x86 $targetAssistProjectPath >> build.log
    if ($LASTEXITCODE -ne 0) {
        Write-Error "TargetAssist plugin build failed with exit code $LASTEXITCODE. Check .\\build.log for details."
        if (Test-Path "build.log") {
            Get-Content -Path "build.log" -Tail 100
        }
        exit $LASTEXITCODE
    }
}

Write-Output "NOTE: This is a truncated view of the build logs. For the full log, refer to .\build.log"
Get-Content -Path "build.log" -Tail 100

# Copy runtime data from Dependencies into Build\Data.
if (Test-Path ".\Dependencies") {
    New-Item -ItemType Directory -Path ".\Build\Data" -Force > $null
    Copy-Item -Path ".\Dependencies\*" -Destination ".\Build\Data\" -Recurse -Force
}

# Copy plugin manifests beside plugin assemblies as <AssemblyName>.manifest.json
$pluginManifestFiles = Get-ChildItem -Path ".\Plugins" -Recurse -Filter "plugin.manifest.json" -ErrorAction SilentlyContinue
if ($pluginManifestFiles -and $pluginManifestFiles.Count -gt 0) {
    New-Item -ItemType Directory -Path ".\Build\Data\Plugins" -Force > $null

    foreach ($manifestFile in $pluginManifestFiles) {
        $pluginAssemblyName = Split-Path $manifestFile.DirectoryName -Leaf
        $targetManifest = Join-Path ".\Build\Data\Plugins" "$pluginAssemblyName.manifest.json"
        Copy-Item -Path $manifestFile.FullName -Destination $targetManifest -Force
    }
}

# Ensure required runtime data exists after build (languages and town scripts).
$requiredRuntimeFiles = @(
    ".\Build\Data\Languages\langs.rsl",
    ".\Build\Data\Scripts\Towns\22106.rbs"
)

$missingRuntimeFiles = $requiredRuntimeFiles | Where-Object { -not (Test-Path $_) }
if ($missingRuntimeFiles.Count -gt 0) {
    Write-Output "Runtime data is missing. Restoring Build\Data assets from repository..."

    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        Write-Error "git was not found. Could not restore required runtime files."
        exit 1
    }

    git restore Build/Data/Languages Build/Data/Scripts/Towns
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to restore runtime data files. Please check your git working tree state."
        exit 1
    }

    $missingRuntimeFiles = $requiredRuntimeFiles | Where-Object { -not (Test-Path $_) }
    if ($missingRuntimeFiles.Count -gt 0) {
        Write-Error "Runtime data is still missing after restore: $($missingRuntimeFiles -join ', ')"
        exit 1
    }
}

if ($Clean) {
    Move-Item ".\temp\User" ".\Build\User" -ErrorAction SilentlyContinue > $null
    Remove-Item -Recurse -Force ".\temp" -ErrorAction SilentlyContinue > $null
}

if (-not $SkipIconCacheRefresh) {
    $iconRefreshTool = Join-Path $env:WINDIR "System32\ie4uinit.exe"
    if (Test-Path $iconRefreshTool) {
        Write-Output "Refreshing Windows icon cache..."
        & $iconRefreshTool -show > $null
    }
}

if (!$DoNotStart) {
    $botExecutable = ".\Build\UBot.exe"
    Write-Output "Starting $(Split-Path $botExecutable -Leaf)..."
    & $botExecutable
}
