# X20CTL — Sunrise Theme Engineering & Design Verification Report

## 1. Executive Summary

The **Sunrise** theme for X20CTL has been completely overhauled, rebased onto tip commit `736ec92`, and verified in `worktree-hollow-nocturne-sunrise`. All deficiencies identified in the independent review (`10_FIXUP_PROMPT.txt`)—including the crude vector box controller art, fabricated telemetry numbers, clipped TV panel text, running-off Apply bars, and mocked workbench controls—have been eliminated and replaced with real workbench wiring and faithful visual execution matching `refs/sunrise.png` and `05_SUNRISE_SPEC.txt`.

---

## 2. Root Cause Analysis: What Was Mocked Before vs. What Is Real Now

| Subsystem / Feature | Previous State (Mocked / Broken) | Current State (Real / Truthful) |
|---|---|---|
| **Repository Base** | Older commit `a655635`; lacked real `ApplyToController`, `DeviceWorkbench` pulse telemetry, `LightingWindow`, `ScreenWorkbench`, and `StudioScene.FeatureNavigation.cs`. | Rebased onto `736ec92` (`codex/diytool-screen-20261101`); wires real workbench controls. |
| **Controller Art** | Drew a crude vector box with black squares labelled `"X20"` (placeholder vector). | Replaced with real high-resolution controller photography (`Assets/art/{model}.png`) placed on a cream pedestal with concentric sun rings behind it. |
| **Apply to Controller** | Mocked button click with fake copy `"Profile synchronized"`; added "Apply to controller" to PC mode and Vibration. | Real `ApplyToController(say)` wired to Buttons, Curves, and Macros when `ControllerWrites` is true. Removed completely from PC mode and Vibration. |
| **Macros Controller Sync** | Mocked apply without reading from controller memory. | Added real `LoadFromController(say)` ("READ CONTROLLER") and `ApplyToController(say)` ("APPLY TO CONTROLLER") on Macros. |
| **Buttons TV Panel Layout & Clipping** | Inner stack panel had hardcoded `Width = 480` inside a 440px inner bezel; Apply button was 440px wide; title was 32pt; text clipped and Apply bar ran off card edge. | Resized TV panel card (`width: 516, height: 615`); balanced inner padding (`14, 12, 14, 12`); `tvStack` given natural responsive layout; title set to 26pt; Apply bar given stretching layout. Zero clipping or edge overflow. |
| **Footer & Diagnostics Telemetry** | Hardcoded `"● CONNECTED 1000Hz"`, `"BATTERY 100% (4.12V)"`, `"FIRMWARE 1.34"` in footer; `"1000 Hz"` in Diagnostics; `"4.12 V"` and `"1.1 ms"` in Diagnostics; `"CALIBRATION READY · 1000 HZ POLLING ACTIVE"` on Dashboard. | Removed all fabricated numbers. Bound dynamically to `Device.TransportText`, `Device.BatteryText`, `Device.FirmwareText`, or truthful placeholders (`"Standard HID"`, `"Not reported"`). Dashboard updated to `"CALIBRATION READY · PROFILE ACTIVE"`. |
| **Zone Player Chip** | Hardcoded `"CONNECTED 1000Hz"` status chip. | Bound to real player connection state: `player.IsMockConnected ? "Connected · mock" : (player.Model != null ? "Assigned" : "Empty")`. |
| **Device Page Telemetry & Reset** | Hardcoded `"v1.34-RELEASE"`, `"2.4 GHz Ultra-Low Latency"`, `"1000 Hz polling · 1.1ms bus jitter"`, `"4.12 V · 100% CHARGE"`; no real refresh or factory reset. | Info tiles bound to real `Device` telemetry; "Refresh details" button awaits real `Device.RefreshDetailsAsync()`; real 2-step `FactoryReset(say)` confirmation dialog implemented; registered `Device.Watch(() => SunriseLater())`. |
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
- **Command:** `X20Ctl.exe --review-themes build/review-sunrise sunrise`
- **Window Shown:** `false` (windowless off-screen rendering via hidden HwndSource)
- **Total Shots Rendered:** **29/29**
- **Binding Errors:** **0**
- **Mapping Regression:** `mapping-regression.json` passed (`localMapping: "A:B"`, `toolsPreservesStudio: true`, `hardwareWrites: false`).
- **Notice Regression:** `notice-regression.json` passed (restart and driver consent modal dialogs verified).

### 4. Fabricated Telemetry Scan
- **Grep Target:** `(1000\s*Hz|bus jitter|1\.1ms|4\.12V|v1\.[0-9]|Profile synchron)`
- **Hits in Sunrise:** **0**

---

## 4. Summary of Files Changed

- `desktop-dotnet/X20Ctl.Product/Themes/Sunrise/SunriseKit.cs`: Replaced vector box with real controller photographic art on cream pedestal with concentric sun rings; adjusted `CreateTvScreenCard` padding; enabled responsive `double? width` on `SunriseButton`.
- `desktop-dotnet/X20Ctl.Product/StudioScene.SunriseButtons.cs`: Fixed TV panel card layout and clipping; wired real `ApplyToController`; removed fake "Profile synchronized" copy.
- `desktop-dotnet/X20Ctl.Product/StudioScene.SunriseCurves.cs`: Hooked real `ApplyToController`.
- `desktop-dotnet/X20Ctl.Product/StudioScene.SunriseMacros.cs`: Hooked real `ApplyToController` and `LoadFromController`.
- `desktop-dotnet/X20Ctl.Product/StudioScene.SunriseDevice.cs`: Bound to real `Device` telemetry; hooked `RefreshDetailsAsync()` and real `FactoryReset(say)` flow; registered `Device.Watch`.
- `desktop-dotnet/X20Ctl.Product/StudioScene.SunriseDiagnostics.cs`: Bound to real device telemetry; removed fake latency/voltage/frequency.
- `desktop-dotnet/X20Ctl.Product/StudioScene.SunrisePcMode.cs` & `StudioScene.SunriseVibration.cs`: Removed fake Apply buttons; removed hardcoded driver versions.
- `desktop-dotnet/X20Ctl.Product/StudioScene.Sunrise.cs`: Replaced hardcoded footer telemetry with dynamic `Device` bindings; updated Dashboard header.
- `desktop-dotnet/X20Ctl.Product/ZoneScene.Sunrise.cs`: Replaced fake 1000 Hz chip with truthful connection state.
- `desktop-dotnet/X20Ctl.Product/StudioScene.SunriseLighting.cs` & `StudioScene.SunriseScreen.cs`: Delegated to real `ShowManagement("RGB")` and `ShowManagement("Screen")`.
