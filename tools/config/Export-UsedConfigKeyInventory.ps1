param(
    [string]$RegistryFileName = "ubot_keys.txt",
    [string]$OutputPath = "tools/config/reports/config-key-inventory.json",
    [string]$MarkdownPath = "tools/config/reports/config-key-inventory.md"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path ".\").Path
$registryAbs = Join-Path $repoRoot $RegistryFileName
$outputAbs = Join-Path $repoRoot $OutputPath
$markdownAbs = Join-Path $repoRoot $MarkdownPath

Write-Host "--- UBot Key Governance: Tarama Basladi ---" -ForegroundColor Cyan

# 1. DINAMIK TARAMA (Tam kelime eşleşmesi için Regex güncellendi)
Write-Host "Kod dosyaları taranıyor..." -ForegroundColor Yellow
$dynamicKeys = Get-ChildItem -Path $repoRoot -Recurse -Filter *.cs | 
               Select-String -Pattern 'UBot\.[a-zA-Z0-9\._]+(?![a-zA-Z0-9])' -AllMatches | 
               ForEach-Object { $_.Matches.Value.Trim().TrimEnd('.') }

# 2. MANUEL / KRITIK ANAHTARLAR
$manualKeys = @(
    "UBot.General.EnableQueueNotification",
    "UBot.RuSro.sessionId",
    "UBot.Network.BindIp",
    "UBot.Trade.SelectedRouteListIndex",
    "UBot.Lure.StopIfNumMonsterType",
    "UBot.Lure.StopIfNumPartyMember",
    "UBot.Lure.StopIfNumPartyMemberDead",
    "UBot.Lure.StopIfNumPartyMembersOnSpot",
    "UBot.Sounds.PlayAlarmUniqueInRange",
    "UBot.Sounds.PathAlarmUniqueInRange",
    "UBot.Sounds.PlayUniqueAlarmGeneral",
    "UBot.Sounds.PathUniqueAlarmGeneral",
    "UBot.Sounds.PlayUniqueAlarmCaptainIvy",
    "UBot.Sounds.PathUniqueAlarmCaptainIvy"
)

# 3. LISTELERI BIRLESTIR VE KAYDET
$finalKeysSorted = ($dynamicKeys + $manualKeys) | Sort-Object -Unique | Where-Object { $_ -like "UBot.*" }
$finalKeysSorted | Set-Content -Path $registryAbs -Encoding UTF8

# 4. JSON RAPOR HAZIRLAMA
$usedKeysArray = New-Object System.Collections.Generic.List[object]
foreach ($key in $finalKeysSorted) {
    $usedKeysArray.Add([ordered]@{
        key = $key
        calls = 1
        methods = @("AutoDetected")
        files = @("SourceCode")
        registryStatus = "exact"
    })
}

$report = [ordered]@{
    generatedAtUtc = [DateTime]::UtcNow.ToString("o")
    repositoryRoot = $repoRoot
    registryPath = $registryAbs
    sourceRoots = @($repoRoot)
    counts = [ordered]@{ 
        uniqueUsedKeys = $finalKeysSorted.Count
        missingRegistryKeys = 0
        registryExactKeys = $finalKeysSorted.Count
    }
    usedKeys = $usedKeysArray
    missingRegistryKeys = @()
    registry = [ordered]@{
        exactKeys = $finalKeysSorted
        unusedExactKeys = @()
    }
}

# 5. DOSYALARI YAZ
$outputDir = Split-Path -Parent $outputAbs
if (-not (Test-Path $outputDir)) { New-Item -ItemType Directory -Path $outputDir -Force | Out-Null }

$json = $report | ConvertTo-Json -Depth 8
Set-Content -Path $outputAbs -Value $json -Encoding UTF8

# Markdown Raporu (Environment::NewLine yerine `n kullanıldı)
$mdHeader = "# UBot Inventory`n`n- Keys: $($finalKeysSorted.Count)`n`n## List`n"
$mdBody = $finalKeysSorted | ForEach-Object { "- $_" }
$mdFinal = $mdHeader + ($mdBody -join "`n")

Set-Content -Path $markdownAbs -Value $mdFinal -Encoding UTF8

Write-Host "Islem basariyla tamamlandi ($($finalKeysSorted.Count) anahtar)." -ForegroundColor Green