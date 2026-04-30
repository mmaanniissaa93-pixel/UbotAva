Write-Host ">>> UBot Governance Pipeline Başlatılıyor..." -ForegroundColor Cyan

# Adım 1: Koddan anahtarları çek ve ubot_keys.txt dosyasını oluştur/güncelle
Write-Host "`n[1/2] Envanter taranıyor ve ubot_keys.txt güncelleniyor..." -ForegroundColor Yellow
& ".\tools\config\Export-UsedConfigKeyInventory.ps1"

# Adım 2: Migration kurallarını denetle
Write-Host "`n[2/2] Migration kuralları ve uyumluluk denetleniyor..." -ForegroundColor Yellow
& ".\tools\config\Test-ConfigKeyMigrations.ps1"

Write-Host "`n>>> Tüm işlemler tamamlandı!" -ForegroundColor Green