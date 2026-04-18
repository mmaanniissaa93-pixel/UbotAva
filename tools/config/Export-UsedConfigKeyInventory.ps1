param(
    [string]$RegistryPath = "ubot_keys.txt",
    [string[]]$SourceRoots = @("Application", "Library", "Plugins", "Botbases", "Tests"),
    [string]$OutputPath = "tools/config/reports/config-key-inventory.json",
    [string]$MarkdownPath = "tools/config/reports/config-key-inventory.md",
    [switch]$PassThru
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

function Convert-CSharpStringLiteral {
    param([string]$Literal)

    if ([string]::IsNullOrWhiteSpace($Literal)) {
        return ""
    }

    if ($Literal.StartsWith('@"') -and $Literal.EndsWith('"')) {
        $body = $Literal.Substring(2, $Literal.Length - 3)
        return $body.Replace('""', '"')
    }

    if ($Literal.StartsWith('"') -and $Literal.EndsWith('"')) {
        $body = $Literal.Substring(1, $Literal.Length - 2)
        return [regex]::Unescape($body)
    }

    return $Literal
}

$repoRoot = (Resolve-Path ".\").Path
$registryAbs = Resolve-PathFromRoot -Root $repoRoot -PathValue $RegistryPath
$outputAbs = Resolve-PathFromRoot -Root $repoRoot -PathValue $OutputPath
$markdownAbs = Resolve-PathFromRoot -Root $repoRoot -PathValue $MarkdownPath

if (-not (Test-Path $registryAbs)) {
    throw "Key registry file not found: $registryAbs"
}

$sourceDirs = @()
foreach ($sourceRoot in $SourceRoots) {
    $resolved = Resolve-PathFromRoot -Root $repoRoot -PathValue $sourceRoot
    if (Test-Path $resolved) {
        $sourceDirs += $resolved
    }
}

if ($sourceDirs.Count -eq 0) {
    throw "No source directories found."
}

$files = @()
foreach ($sourceDir in $sourceDirs) {
    $files += Get-ChildItem -Path $sourceDir -Recurse -File -Filter *.cs
}

$configCallPattern = [regex]'(?<owner>PlayerConfig|GlobalConfig)\s*\.\s*(?<method>Get|Set|GetArray|SetArray|GetEnum|GetEnums)\s*(?:<[^>\r\n]+>)?\s*\(\s*(?<keyLiteral>@\"(?:[^\"]|\"\")*\"|\"(?:\\.|[^\"])*\")'
$dynamicLinePattern = [regex]'(?<owner>PlayerConfig|GlobalConfig)\s*\.\s*(?<method>Get|Set|GetArray|SetArray|GetEnum|GetEnums)\s*(?:<[^>]+>)?\s*\(\s*(?!@?")(?<expr>[^,\)\r\n]+)'

$usedKeyMap = @{}
$dynamicUsages = New-Object System.Collections.Generic.List[object]
$totalMatches = 0

foreach ($file in $files) {
    $raw = Get-Content -Path $file.FullName -Raw
    $matches = $configCallPattern.Matches($raw)

    foreach ($match in $matches) {
        $totalMatches++
        $literal = $match.Groups["keyLiteral"].Value
        $key = Convert-CSharpStringLiteral -Literal $literal
        if ([string]::IsNullOrWhiteSpace($key) -or -not $key.StartsWith("UBot.", [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        if (-not $usedKeyMap.ContainsKey($key)) {
            $usedKeyMap[$key] = [ordered]@{
                calls = 0
                methods = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)
                files = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)
            }
        }

        $entry = $usedKeyMap[$key]
        $entry.calls++
        [void]$entry.methods.Add($match.Groups["method"].Value)
        [void]$entry.files.Add($file.FullName)
    }

    $lineNo = 0
    foreach ($line in Get-Content -Path $file.FullName) {
        $lineNo++
        $dynMatches = $dynamicLinePattern.Matches($line)
        foreach ($dynMatch in $dynMatches) {
            $expr = $dynMatch.Groups["expr"].Value.Trim()
            if ([string]::IsNullOrWhiteSpace($expr)) {
                continue
            }

            $dynamicUsages.Add(
                [ordered]@{
                    file = $file.FullName
                    line = $lineNo
                    method = $dynMatch.Groups["method"].Value
                    expression = $expr
                    snippet = $line.Trim()
                }
            )
        }
    }
}

$registryLines = Get-Content -Path $registryAbs | ForEach-Object { $_.Trim() } | Where-Object {
    -not [string]::IsNullOrWhiteSpace($_) -and -not $_.StartsWith("#") -and -not $_.StartsWith("//")
}

$registryExact = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)
$registryPrefix = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)

foreach ($line in $registryLines) {
    if ($line.EndsWith(".") -or $line.EndsWith("_")) {
        [void]$registryPrefix.Add($line)
    }
    else {
        [void]$registryExact.Add($line)
    }
}

$usedKeys = @()
$missingKeys = New-Object System.Collections.Generic.List[string]
$prefixMatchedKeys = New-Object System.Collections.Generic.List[string]

foreach ($key in ($usedKeyMap.Keys | Sort-Object)) {
    $status = "missing"
    if ($registryExact.Contains($key)) {
        $status = "exact"
    }
    else {
        foreach ($prefix in $registryPrefix) {
            if ($key.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
                $status = "prefix"
                break
            }
        }
    }

    if ($status -eq "missing") {
        $missingKeys.Add($key)
    }
    elseif ($status -eq "prefix") {
        $prefixMatchedKeys.Add($key)
    }

    $entry = $usedKeyMap[$key]
    $usedKeys += [ordered]@{
        key = $key
        calls = $entry.calls
        methods = @($entry.methods | Sort-Object)
        files = @($entry.files | Sort-Object)
        registryStatus = $status
    }
}

$unusedRegistryExact = @()
foreach ($key in ($registryExact | Sort-Object)) {
    if (-not $usedKeyMap.ContainsKey($key)) {
        $unusedRegistryExact += $key
    }
}

$missingRegistryKeysSorted = @([string[]]$missingKeys.ToArray() | Sort-Object -Unique)
$prefixMatchedKeysSorted = @([string[]]$prefixMatchedKeys.ToArray() | Sort-Object -Unique)

$counts = [ordered]@{}
$counts.scannedFiles = $files.Count
$counts.rawConfigCallMatches = $totalMatches
$counts.uniqueUsedKeys = $usedKeys.Count
$counts.missingRegistryKeys = $missingKeys.Count
$counts.prefixMatchedKeys = $prefixMatchedKeys.Count
$counts.dynamicKeyUsages = $dynamicUsages.Count
$counts.registryExactKeys = $registryExact.Count
$counts.registryPrefixEntries = $registryPrefix.Count
$counts.registryUnusedExactKeys = $unusedRegistryExact.Count

$registryReport = [ordered]@{}
$registryReport.exactKeys = @($registryExact | Sort-Object)
$registryReport.prefixEntries = @($registryPrefix | Sort-Object)
$registryReport.unusedExactKeys = $unusedRegistryExact

$report = [ordered]@{}
$report["generatedAtUtc"] = [DateTime]::UtcNow.ToString("o")
$report["repositoryRoot"] = $repoRoot
$report["registryPath"] = $registryAbs
$report["sourceRoots"] = @($sourceDirs)
$report["counts"] = $counts
$report["usedKeys"] = $usedKeys
$report["missingRegistryKeys"] = $missingRegistryKeysSorted
$report["prefixMatchedKeys"] = $prefixMatchedKeysSorted
$report["dynamicKeyUsages"] = $dynamicUsages.ToArray()
$report["registry"] = $registryReport

$outputDir = Split-Path -Parent $outputAbs
$markdownDir = Split-Path -Parent $markdownAbs
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}
if (-not (Test-Path $markdownDir)) {
    New-Item -ItemType Directory -Path $markdownDir -Force | Out-Null
}

$json = $report | ConvertTo-Json -Depth 8
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($outputAbs, $json, $utf8NoBom)

$missingPreview = if ($report.missingRegistryKeys.Count -gt 0) {
    ($report.missingRegistryKeys | Select-Object -First 30) -join ", "
}
else {
    "none"
}

$unusedPreview = if ($report.registry.unusedExactKeys.Count -gt 0) {
    ($report.registry.unusedExactKeys | Select-Object -First 30) -join ", "
}
else {
    "none"
}

$markdown = @()
$markdown += "# Config Key Inventory"
$markdown += ""
$markdown += "- generatedAtUtc: $($report.generatedAtUtc)"
$markdown += "- scannedFiles: $($report.counts.scannedFiles)"
$markdown += "- uniqueUsedKeys: $($report.counts.uniqueUsedKeys)"
$markdown += "- missingRegistryKeys: $($report.counts.missingRegistryKeys)"
$markdown += "- prefixMatchedKeys: $($report.counts.prefixMatchedKeys)"
$markdown += "- dynamicKeyUsages: $($report.counts.dynamicKeyUsages)"
$markdown += "- registryUnusedExactKeys: $($report.counts.registryUnusedExactKeys)"
$markdown += ""
$markdown += "## Missing Registry Keys (first 30)"
$markdown += ""
$markdown += "$missingPreview"
$markdown += ""
$markdown += "## Unused Registry Exact Keys (first 30)"
$markdown += ""
$markdown += "$unusedPreview"
$markdown += ""
$markdown += "## Reports"
$markdown += ""
$markdown += "- json: $outputAbs"
$markdown += "- markdown: $markdownAbs"

[System.IO.File]::WriteAllText($markdownAbs, ($markdown -join [Environment]::NewLine), $utf8NoBom)

Write-Host "Config key inventory generated."
Write-Host "  JSON    : $outputAbs"
Write-Host "  Markdown: $markdownAbs"
Write-Host "  Unique keys: $($report.counts.uniqueUsedKeys)"
Write-Host "  Missing registry keys: $($report.counts.missingRegistryKeys)"
Write-Host "  Dynamic key usages: $($report.counts.dynamicKeyUsages)"

if ($PassThru) {
    return $report
}
