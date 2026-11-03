# X20CTL — Nocturne Theme Engineering & Design Verification Report

## 1. Executive Summary

The **Nocturne** theme for X20CTL has been thoroughly audited, rebased onto tip commit `736ec92`, and verified in `worktree-hollow-nocturne-sunrise`. All fabricated numbers (fake 1000 Hz, 84% battery, 2.14 firmware, 1.1ms latency, 4.12V voltage), left-edge card clipping, and mocked handlers reported in `10_FIXUP_PROMPT.txt` have been corrected and verified against `refs/nocturne.png` and `04_NOCTURNE_SPEC.txt`.

---

## 2. Root Cause Analysis: What Was Mocked Before vs. What Is Real Now

| Subsystem / Feature | Previous State (Mocked / Broken) | Current State (Real / Truthful) |
|---|---|---|
| **Repository Base** | Older commit `a655635`; lacked real `ApplyToController`, `DeviceWorkbench` pulse telemetry, `LightingWindow`, `ScreenWorkbench`, and `StudioScene.FeatureNavigation.cs`. | Rebased onto `736ec92` (`codex/diytool-screen-20261101`); wires real workbench controls. |
| **Apply to Controller** | Mocked button click with fake copy `"Profile synchronized"`; added "Apply to controller" to PC mode and Vibration. | Real `ApplyToController(say)` wired to Buttons, Curves, and Macros when `ControllerWrites` is true. Removed completely from PC mode and Vibration. |
| **Macros Controller Sync** | Mocked apply without reading from controller memory; hardcoded `"1000 Hz true polling"`. | Added real `LoadFromController(say)` ("READ CONTROLLER") and `ApplyToController(say)` ("APPLY TO CONTROLLER") on Macros. Bound capacity to `Macros?.CapacityDescription`. |
| **Slanted Card Left Clipping** | White slanted card clipped its own content on the left ("ACE / A", "PRESS:", "GAME RECEIVES:" cut off due to -12° outer transform and child clipping). | Set `ClipToBounds = false` on `NocturneWhiteCard` and added slant-aware dynamic left padding (`Math.Max(34, (height * tan(12°) * 0.5) + 16)`). "FACE / A", "YOU PRESS:", and "GAME RECEIVES:" are 100% visible. |
| **Footer & Diagnostics Telemetry** | Hardcoded `"CONNECTION: BLUETOOTH · BATTERY: 84% · FIRMWARE: 2.14"` in footer; `"1000 Hz"` in Diagnostics, Macros, and PC mode; `"4.12 V"` and `"1.1 ms"` in Diagnostics. | Removed all fabricated numbers. Footer and Diagnostics now dynamically bind to `Device.TransportText`, `Device.BatteryText`, `Device.FirmwareText`, or truthful placeholders (`"Standard HID"`, `"Not reported"`). |
| **Zone Player Chip** | Hardcoded `"CONNECTED 1000Hz"` status chip. | Bound to real player connection state: `player.IsMockConnected ? "Connected · mock" : (player.Model != null ? "Assigned" : "Empty")`. |
| **Device Telemetry & Recovery** | Mocked stats and redrawing-only refresh; no real Factory Reset flow. | Bound to real `Device` telemetry; wired `await Device.RefreshDetailsAsync()`; wired real 2-step `FactoryReset(say)` flow; registered `Device.Watch(() => NocturneLater())`. |
| **RGB & Screen Workbenches** | Custom mocked placeholder pages with hardcoded lighting controls. | Fully integrated via `StudioScene.FeatureNavigation.cs`: `ShowManagement("RGB")` loads the real `LightingWindow` body; `ShowManagement("Screen")` loads `ScreenWorkbench`. |

---

## 3. Deterministic Verification & Evidence

### 1. Build Verification
- **Command:** `dotnet build desktop-dotnet/X20Ctl.Product/X20Ctl.Product.csproj`
- **Result:** **0 Errors**, 94 standard project warnings.
- **Exit Code:** `0`

### 2. Unit Test Suite
- **Command:** `dotnet test desktop-dotnet/Product.sln`
- **Result:** **Failed: 0, Passed: 823, Skipped: 0, Total: 823 (100% Green)**
- **Duration:** 15.2s
- **Exit Code:** `0`

### 3. Windowless Review Harness (`ThemeReview.Run`)
- **Command:** `X20Ctl.exe --review-themes build/review-nocturne nocturne`
- **Window Shown:** `false` (windowless off-screen rendering via hidden HwndSource)
- **Total Shots Rendered:** **29/29**
- **Binding Errors:** **0**
- **Mapping Regression:** `mapping-regression.json` passed (`localMapping: "A:B"`, `toolsPreservesStudio: true`, `hardwareWrites: false`).
- **Notice Regression:** `notice-regression.json` passed (restart and driver consent modal dialogs verified).

### 4. Fabricated Telemetry Scan
- **Grep Target:** `(1000\s*Hz|bus jitter|1\.1ms|4\.12V|84%|v2\.14|Profile synchron)`
- **Hits in Nocturne:** **0**

---

## 4. Summary of Files Changed

- `desktop-dotnet/X20Ctl.Product/Themes/Nocturne/NocturneKit.cs`: Fixed `NocturneWhiteCard` left-edge clipping via `ClipToBounds = false` and slant-aware padding.
- `desktop-dotnet/X20Ctl.Product/StudioScene.NocturneButtons.cs`: Wired real `ApplyToController`; eliminated clipping on slanted card; truthful status line.
- `desktop-dotnet/X20Ctl.Product/StudioScene.NocturneCurves.cs`: Hooked real `ApplyToController`.
- `desktop-dotnet/X20Ctl.Product/StudioScene.NocturneMacros.cs`: Hooked real `ApplyToController` and `LoadFromController`; removed fake 1000 Hz.
- `desktop-dotnet/X20Ctl.Product/StudioScene.NocturneDevice.cs`: Bound to real `Device` telemetry; hooked `RefreshDetailsAsync()` and real `FactoryReset(say)` flow; registered `Device.Watch`.
- `desktop-dotnet/X20Ctl.Product/StudioScene.NocturneDiagnostics.cs`: Bound to real device telemetry; removed fake latency/voltage/frequency.
- `desktop-dotnet/X20Ctl.Product/StudioScene.NocturnePcMode.cs` & `StudioScene.NocturneVibration.cs`: Removed fake Apply buttons; removed hardcoded driver versions.
- `desktop-dotnet/X20Ctl.Product/StudioScene.Nocturne.cs`: Replaced hardcoded footer telemetry with dynamic `Device` bindings.
- `desktop-dotnet/X20Ctl.Product/ZoneScene.Nocturne.cs`: Replaced fake 1000 Hz chip with truthful connection state.
- `desktop-dotnet/X20Ctl.Product/StudioScene.NocturneLighting.cs` & `StudioScene.NocturneScreen.cs`: Delegated to real `ShowManagement("RGB")` and `ShowManagement("Screen")`.
