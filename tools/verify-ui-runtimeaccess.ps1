<#
.SYNOPSIS
    Guard script to prevent RuntimeAccess usage in UI files.
#>

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$UiBasePath = Join-Path $ProjectRoot "Application\UBot.Avalonia"

$ForbiddenPatterns = @("RuntimeAccess", "UBot\.Core\.RuntimeAccess")

$AllowedUsing = @(
    "using UBot\.Core\.Components;",
    "using UBot\.Core\.Plugins;",
    "using UBot\.Core\.Network;"
)

$TempExceptions = @("LureRecorderWindow")

$Violations = @()

$UiPaths = @(
    (Join-Path $UiBasePath "Features"),
    (Join-Path $UiBasePath "ViewModels"),
    (Join-Path $UiBasePath "Dialogs"),
    (Join-Path $UiBasePath "MainWindow.axaml.cs")
)

Write-Host "Scanning UI files for forbidden RuntimeAccess patterns..." -ForegroundColor Cyan
Write-Host "Project Root: $ProjectRoot" -ForegroundColor Gray
Write-Host ""

$AllUiFiles = @()
foreach ($Path in $UiPaths) {
    if (Test-Path $Path) {
        if ($Path -like "*.axaml.cs") {
            $AllUiFiles += Get-Item $Path -ErrorAction SilentlyContinue
        } else {
            $AllUiFiles += Get-ChildItem -Path $Path -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue
        }
    }
}

$AllUiFiles = $AllUiFiles | Sort-Object -Property FullName -Unique

foreach ($File in $AllUiFiles) {
    $FileName = $File.BaseName

    $IsTempException = $false
    foreach ($Exc in $TempExceptions) {
        if ($FileName -like "*$Exc*") {
            $IsTempException = $true
            break
        }
    }

    if ($IsTempException) {
        Write-Host "[TEMP EXCEPTION] $($File.Name)" -ForegroundColor Yellow
        continue
    }

    $Content = Get-Content $File.FullName -Raw -ErrorAction SilentlyContinue
    if (-not $Content) {
        continue
    }

    $LineNumber = 0
    $Content -split "`n" | ForEach-Object {
        $LineNumber++
        $Line = $_

        $IsAllowedUsing = $false
        foreach ($Allowed in $AllowedUsing) {
            if ($Line -match $Allowed) {
                $IsAllowedUsing = $true
                break
            }
        }

        if ($IsAllowedUsing) {
            return
        }

        foreach ($Pattern in $ForbiddenPatterns) {
            if ($Line -match $Pattern) {
                $Violations += [PSCustomObject]@{
                    File = $File.FullName.Replace($ProjectRoot, ".")
                    Line = $LineNumber
                    Content = $Line.Trim()
                }
            }
        }
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor $(if ($Violations.Count -eq 0) { "Green" } else { "Yellow" })
Write-Host "Scan Results: $($Violations.Count) violation(s) found" -ForegroundColor $(if ($Violations.Count -eq 0) { "Green" } else { "Red" })
Write-Host "========================================" -ForegroundColor $(if ($Violations.Count -eq 0) { "Green" } else { "Yellow" })
Write-Host ""

if ($Violations.Count -gt 0) {
    Write-Host "Violations:" -ForegroundColor Red
    $Violations | ForEach-Object {
        Write-Host "  $($_.File):$($_.Line)" -ForegroundColor Red
        Write-Host "    $($_.Content)" -ForegroundColor DarkGray
        Write-Host ""
    }
    Write-Host "FAILED: Forbidden RuntimeAccess usage found in UI files." -ForegroundColor Red
    exit 1
} else {
    Write-Host "PASS: No forbidden RuntimeAccess patterns found in UI files." -ForegroundColor Green
    exit 0
}