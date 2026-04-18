# UBot Test Layers

Bu dizin, 3 katmanli test stratejisinin otomasyon giris noktalarini tutar.

## 1) IPC Contract Tests

```powershell
powershell.exe -ExecutionPolicy Bypass .\tools\tests\Test-IpcContractParity.ps1
```

Opsiyonel:

```powershell
powershell.exe -ExecutionPolicy Bypass .\tools\tests\Test-IpcContractParity.ps1 -NoSync
```

## 2) Bridge Integration Tests

Bu katman icin standart senaryo matrisi:
- pipe connect/disconnect
- reconnect + retry
- correlation propagation
- idempotency davranisi
- channel policy backpressure

Not: Bu matriste `core.get-ipc-metrics` ve `bridge.get-pipe-metrics` snapshotlari dogrulama kaynagi olarak kullanilmalidir.

## 3) Soak/Load Tests

Bu katmanda event storm altinda su KPI’lar raporlanir:
- p95/p99 invoke latency
- queue depth
- reconnect count
- dropped event trend

Detayli kapsam ve SLO hedefleri icin:
- `_ipc_inventory/09_test_strategy_three_layers.md`
