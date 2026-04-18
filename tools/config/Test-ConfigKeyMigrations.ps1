param(
    [string]$MigrationMapPath = "tools/config/config-key-migrations.json",
    [string]$RegistryPath = "ubot_keys.txt",
    [string]$InventoryPath = "tools/config/reports/config-key-inventory.json",
    [string]$InventoryMarkdownPath = "tools/config/reports/config-key-inventory.md",
    [string]$ConfigRoot = "Build/User",
    [switch]$SkipConfigScan,
    [switch]$RefreshInventory,
    [switch]$FailOnWarning
)

$ErrorActionPreference = "Stop"

function Resolve-PathFromRoot {
    param(
        [string]$Root,
        [string]$PathValue
    )

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return [System.IO.Path]::GetFullPath($PathValue)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $Root $PathValue))
}

function Normalize-Severity {
    param([string]$Severity)

    if ([string]::IsNullOrWhiteSpace($Severity)) {
        return "error"
    }

    $normalized = $Severity.Trim().ToLowerInvariant()
    if ($normalized -ne "error" -and $normalized -ne "warning") {
        return "error"
    }

    return $normalized
}

$repoRoot = (Resolve-Path ".\").Path
$migrationMapAbs = Resolve-PathFromRoot -Root $repoRoot -PathValue $MigrationMapPath
$inventoryAbs = Resolve-PathFromRoot -Root $repoRoot -PathValue $InventoryPath
$inventoryMdAbs = Resolve-PathFromRoot -Root $repoRoot -PathValue $InventoryMarkdownPath
$configRootAbs = Resolve-PathFromRoot -Root $repoRoot -PathValue $ConfigRoot

if (-not (Test-Path $migrationMapAbs)) {
    throw "Migration map file not found: $migrationMapAbs"
}

$exportScriptAbs = Resolve-PathFromRoot -Root $repoRoot -PathValue "tools/config/Export-UsedConfigKeyInventory.ps1"
if ($RefreshInventory -or -not (Test-Path $inventoryAbs)) {
    & $exportScriptAbs -RegistryPath $RegistryPath -OutputPath $InventoryPath -MarkdownPath $InventoryMarkdownPath | Out-Host
}

if (-not (Test-Path $inventoryAbs)) {
    throw "Inventory file not found: $inventoryAbs"
}

$migrationMap = Get-Content -Path $migrationMapAbs -Raw | ConvertFrom-Json
$inventory = Get-Content -Path $inventoryAbs -Raw | ConvertFrom-Json

if ($null -eq $migrationMap.migrations -or $migrationMap.migrations.Count -eq 0) {
    throw "No migration entries found in $migrationMapAbs"
}

$usedKeySet = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)
foreach ($entry in $inventory.usedKeys) {
    [void]$usedKeySet.Add([string]$entry.key)
}

$registryExactSet = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)
foreach ($entry in $inventory.registry.exactKeys) {
    [void]$registryExactSet.Add([string]$entry)
}

$migrationFromSet = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)
$migrationToSet = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)
foreach ($migration in $migrationMap.migrations) {
    if (-not [string]::IsNullOrWhiteSpace([string]$migration.from)) {
        [void]$migrationFromSet.Add([string]$migration.from)
    }
    if (-not [string]::IsNullOrWhiteSpace([string]$migration.to)) {
        [void]$migrationToSet.Add([string]$migration.to)
    }
}

$issues = New-Object System.Collections.Generic.List[object]

$missingRegistryKeys = @($inventory.missingRegistryKeys)
foreach ($missingKey in $missingRegistryKeys) {
    $key = [string]$missingKey
    if ([string]::IsNullOrWhiteSpace($key)) {
        continue
    }

    if ($migrationFromSet.Contains($key)) {
        $issues.Add([ordered]@{
            severity = "warning"
            code = "MIG010"
            key = $key
            scope = "registry"
            message = "Missing registry key '$key' matches a deprecated migration source key."
        })
        continue
    }

    if ($migrationToSet.Contains($key)) {
        $issues.Add([ordered]@{
            severity = "error"
            code = "MIG011"
            key = $key
            scope = "registry"
            message = "Missing registry key '$key' matches a migration target key and must be registered."
        })
        continue
    }

    $issues.Add([ordered]@{
        severity = "error"
        code = "MIG012"
        key = $key
        scope = "registry"
        message = "Used key '$key' is not present in ubot_keys.txt."
    })
}

foreach ($migration in $migrationMap.migrations) {
    $fromKey = [string]$migration.from
    $toKey = [string]$migration.to
    $severity = Normalize-Severity -Severity ([string]$migration.severity)
    $configSeverity = Normalize-Severity -Severity ([string]$migration.configSeverity)
    $allowCodeUsage = [bool]$migration.allowCodeUsage
    $description = [string]$migration.description

    if ([string]::IsNullOrWhiteSpace($fromKey) -or [string]::IsNullOrWhiteSpace($toKey)) {
        $issues.Add([ordered]@{
            severity = "error"
            code = "MIG000"
            key = $fromKey
            scope = "migration-map"
            message = "Migration entry is invalid (from/to is required)."
        })
        continue
    }

    if (-not $registryExactSet.Contains($toKey)) {
        $issues.Add([ordered]@{
            severity = "error"
            code = "MIG002"
            key = $toKey
            scope = "registry"
            message = "Target key '$toKey' is missing in key registry."
        })
    }

    if ($usedKeySet.Contains($fromKey)) {
        if ($allowCodeUsage) {
            $issues.Add([ordered]@{
                severity = "warning"
                code = "MIG001C"
                key = $fromKey
                scope = "source"
                message = "Legacy key '$fromKey' is still used in source (compat mode). $description"
            })
        }
        else {
            $issues.Add([ordered]@{
                severity = $severity
                code = "MIG001"
                key = $fromKey
                scope = "source"
                message = "Deprecated key '$fromKey' is still used in source. Replace with '$toKey'. $description"
            })
        }
    }
}

if (-not $SkipConfigScan -and (Test-Path $configRootAbs)) {
    $configFiles = Get-ChildItem -Path $configRootAbs -Recurse -File -Include *.rs
    foreach ($configFile in $configFiles) {
        $lineNo = 0
        foreach ($line in Get-Content -Path $configFile.FullName) {
            $lineNo++
            if ([string]::IsNullOrWhiteSpace($line) -or $line.IndexOf("{") -lt 0) {
                continue
            }

            $key = $line.Split("{")[0].Trim()
            if ([string]::IsNullOrWhiteSpace($key)) {
                continue
            }

            foreach ($migration in $migrationMap.migrations) {
                $fromKey = [string]$migration.from
                $toKey = [string]$migration.to
                if (-not $key.Equals($fromKey, [System.StringComparison]::OrdinalIgnoreCase)) {
                    continue
                }

                $configSeverity = Normalize-Severity -Severity ([string]$migration.configSeverity)
                $issues.Add([ordered]@{
                    severity = $configSeverity
                    code = "MIG003"
                    key = $fromKey
                    scope = "config"
                    file = $configFile.FullName
                    line = $lineNo
                    message = "Config file still uses migrated key '$fromKey'. Migrate to '$toKey'."
                })
            }
        }
    }
}

$errors = @($issues | Where-Object { $_.severity -eq "error" })
$warnings = @($issues | Where-Object { $_.severity -eq "warning" })

Write-Host "Config key migration check completed."
Write-Host "  Inventory : $inventoryAbs"
Write-Host "  Markdown  : $inventoryMdAbs"
Write-Host "  Errors    : $($errors.Count)"
Write-Host "  Warnings  : $($warnings.Count)"

if ($issues.Count -gt 0) {
    Write-Host ""
    Write-Host "Issues:"
    foreach ($issue in $issues) {
        $location = if ($issue.file) { " ($($issue.file):$($issue.line))" } else { "" }
        Write-Host "[$($issue.severity.ToUpperInvariant())][$($issue.code)] $($issue.message)$location"
    }
}

if ($errors.Count -gt 0 -or ($FailOnWarning -and $warnings.Count -gt 0)) {
    exit 1
}
