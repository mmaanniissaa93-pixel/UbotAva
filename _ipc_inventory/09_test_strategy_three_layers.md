# UBot Test Strategy v1 (3 Layers)

Bu dokuman, Desktop Bridge mimarisi icin testleri 3 katmana ayirir:
1) IPC Contract Tests
2) Bridge Integration Tests
3) Soak/Load Tests

Amac: veri akisinda uyumluluk, dayaniklilik ve performansi olculebilir sekilde garanti etmek.

---

## 1) IPC Contract Tests (Schema Uyumu)

### Hedef
- C# contract source ile TS/JSON generated contract dosyalari arasinda drift olmasin.
- `core.get-ipc-metrics` ve event envelope alanlari geriye donuk uyumlu kalsin.
- Operasyon telemetry zorunlu alanlari her zaman mevcut olsun:
  - `command_name`
  - `correlation_id`
  - `plugin`
  - `latency_ms`
  - `result`

### Kapsam
- `DesktopBridgeIpcContracts.cs` vs:
  - `Application/UBot.Desktop/src/generated/ipc-contracts.ts`
  - `Application/UBot.Desktop/electron/generated/ipc-contracts.json`
- Version parity:
  - `protocolVersion`
  - `eventCatalogVersion`
- Komut/event list parity.
- Channel policy parity (`responses`, `events.high-frequency`, `events.durable`, `events.ui-only`).
- `core.get-ipc-metrics` response shape:
  - `dashboard.ipcLatency`
  - `dashboard.queueDepth`
  - `dashboard.reconnectCount`
  - `dashboard.droppedEventTrend`
  - `operations.recent[*]` required fields.

### Pass/Fail
- Pass: sync sonrasi generated dosyalarda diff yok.
- Fail: herhangi bir drift veya required alan eksigi.

---

## 2) Bridge Integration Tests (Pipe + Reconnect + Retry)

### Hedef
- UI -> Pipe -> Runtime invoke -> response zinciri deterministik calissin.
- Disconnect/reconnect ve retry davranisi dogru olsun.
- Correlation ve idempotency davranisi bozulmasin.

### Senaryo Seti
- Happy path:
  - `core.get-status` invoke -> `ok=true`, response envelope korunsun.
- Correlation propagation:
  - request `correlationId` gonder -> telemetry `operations.recent` kaydinda ayni id gorulsun.
- Retry path:
  - gecici bridge hatasi simule edilince bir sonraki denemede basari donsun.
- Reconnect:
  - pipe baglantisini kes -> reconnect count artsin.
- Backpressure:
  - event backlog senaryosunda channel policy’ye uygun davranis:
    - high-frequency: drop_newest
    - ui-only: drop_oldest
    - durable/responses: wait

### Dogrulama Metrikleri
- `pipe.reconnectCount`
- `pipe.outboundQueueDepthByChannel`
- `pipe.droppedEventsByClass`
- `operations.recent` zorunlu alanlar ve `result` dagilimi.

### Pass/Fail
- Pass: reconnect/retry senaryolarinda beklenen policy ve metric degisimi.
- Fail: beklenmeyen drop paterni, correlation kaybi, timeout artisi.

---

## 3) Soak/Load Tests (Event Storm + p95/p99 + Drop Rate)

### Hedef
- Uzun sureli yuk altinda gecikme, kuyruk ve drop davranisini olcmek.
- Kapasite sinirlarini netlestirmek.

### Profil
- Warm-up: 5 dk
- Steady load: 20-30 dk
- Spike: 3-5 dk
- Cooldown: 2 dk

### Trafik Siniflari
- High-frequency events: `player.move`, `player.cast`, `log.add`
- Durable events: `plugin.state.changed`, `plugin.config.changed`, `core.status` vb.
- Invoke mix:
  - read agirlikli (`core.get-status`, `plugin.get-state`)
  - write/islem (`plugin.set-config`, `plugin.invoke-action`)

### Izlenecek KPI
- IPC latency: p95/p99 (`dashboard.ipcLatency`)
- Queue depth: `dashboard.queueDepth` + channel bazli depth
- Reconnect count: `dashboard.reconnectCount`
- Drop trend: `dashboard.droppedEventTrend`
- Drop rate:
  - `delta(droppedEvents) / delta(eventSequence)` (zaman penceresi bazli)

### Baslangic SLO Onerisi
- Invoke p95 < 120ms
- Invoke p99 < 250ms
- Steady load’da drop rate < %0.5
- Reconnect storm yoksa reconnectCount artisi 0

---

## CI/Operasyon Cadence

- PR Gate:
  - IPC Contract Tests (zorunlu)
  - Kisa integration smoke (zorunlu)
- Nightly:
  - Tam integration matrix
  - 30 dk soak
- Weekly:
  - Stress/spike profili + trend karsilastirmasi

---

## Cikti Standardi

Her test run su artefaktlari uretmeli:
- `test_results/contract/*.json`
- `test_results/integration/*.json`
- `test_results/soak/*.json`
- `test_results/summary.md`

Summary en az su basliklari icermeli:
- p95/p99 latency
- max queue depth
- reconnect count
- dropped event trend ozeti
- fail eden command/result dagilimi

