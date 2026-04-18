# Config Key Governance

Bu dizin, config key governance otomasyonunu tutar.

## 1) Kullanilan Key Envanteri

```powershell
powershell.exe -ExecutionPolicy Bypass .\tools\config\Export-UsedConfigKeyInventory.ps1
```

Uretilen raporlar:
- `tools/config/reports/config-key-inventory.json`
- `tools/config/reports/config-key-inventory.md`

## 2) Migration Check

```powershell
powershell.exe -ExecutionPolicy Bypass .\tools\config\Test-ConfigKeyMigrations.ps1 -RefreshInventory
```

Opsiyonel:
- `-SkipConfigScan` -> `Build/User` altini taramaz.
- `-FailOnWarning` -> warning durumunda da non-zero exit code verir.

Migration kurallari:
- `tools/config/config-key-migrations.json`

Key registry:
- `ubot_keys.txt`
