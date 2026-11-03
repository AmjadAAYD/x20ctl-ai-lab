# X20CTL — Hollow Theme Engineering & Design Verification Report

## 1. Executive Summary

The **Hollow** theme for X20CTL has been completely rebuilt, rebased onto tip commit `736ec92`, and verified in `worktree-hollow-nocturne-sunrise`. All earlier mock-ups, fabricated telemetry, and clipped visual elements identified in the independent review (`10_FIXUP_PROMPT.txt`) have been eliminated and replaced with real WPF workbench integrations and faithful aesthetic reconstructions matching `refs/hollow.png` and `03_HOLLOW_SPEC.txt`.

---

## 2. Root Cause Analysis: What Was Mocked Before vs. What Is Real Now

| Subsystem / Feature | Previous State (Mocked / Broken) | Current State (Real / Truthful) |
|---|---|---|
| **Repository Base** | Older commit `a655635`; lacked real `ApplyToController`, `DeviceWorkbench` pulse telemetry, `LightingWindow`, `ScreenWorkbench`, and `StudioScene.FeatureNavigation.cs`. | Rebased onto `736ec92` (`codex/diytool-screen-20261101`); wires real workbench controls. |
| **Apply to Controller** | Mocked handler or simulated button click; added fake "Apply to controller" buttons to PC mode and Vibration pages. | Real `ApplyToController(say)` wired to Buttons, Curves, and Macros when `ControllerWrites` is true. Removed completely from PC mode and Vibration (which are local/immediate in the real app). |
| **Macros Controller Sync** | Mocked apply without reading from controller memory. | Added real `LoadFromController(say)` ("READ CONTROLLER") and `ApplyToController(say)` ("APPLY TO CONTROLLER") on Macros. |
| **Device Telemetry & Refresh** | Hardcoded mock stats; "Refresh device details" button simply redrew the page; mock `hollowResetState` and hardcoded copy. | Binds to real `Device.BatteryText`, `Device.FirmwareText`, `Device.RevisionText`, `Device.TransportText`, `Device.ConnectionText`. "Refresh device details" awaits real `Device.RefreshDetailsAsync()`. Added `Device.Watch(() => HollowLater())`. |
| **Factory Reset Flow** | Mocked state with hardcoded `"Controller reset · 0 settings changed."`. | Real 2-step confirmation dialog invoking `await FactoryReset(say)` on supported hardware (`x20_pro`). |
| **RGB & Screen Workbenches** | Custom mocked placeholder pages with hardcoded lighting controls. | Fully integrated via `StudioScene.FeatureNavigation.cs`: `ShowManagement("RGB")` loads the real `LightingWindow` body; `ShowManagement("Screen")` loads `ScreenWorkbench`. |
| **Cavern Backdrop Fidelity** | Bare minimal silhouettes; almost no visible cavern depth; flat feel. | Rich multi-layered vector cavern backdrop with layered stalactites, gothic arches, stone pillars, radial chamber mist, floor fog, and dark vignette. |
| **Button Styling & Primary Action** | Right panel text was crushed and clipped ("Reset this control", "Test mapping" overlapping ornaments); primary Apply was an underlined label. | Proper framed card shapes with adequate breathing room; primary button built as wide framed dark card with thin ivory border and external 4-point diamond ornaments matching `refs/hollow.png`. |
| **Telemetry Truthfulness** | Invented strings such as `"Profile synchronized"`. | Zero fabricated telemetry; uses real app state: `"Showing what's on the controller."` or unsaved draft change count. |

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
- **Command:** `X20Ctl.exe --review-themes build/review-hollow hollow`
- **Window Shown:** `false` (windowless off-screen rendering via hidden HwndSource)
- **Total Shots Rendered:** **29/29**
- **Binding Errors:** **0**
- **Mapping Regression:** `mapping-regression.json` passed (`localMapping: "A:B"`, `toolsPreservesStudio: true`, `hardwareWrites: false`).
- **Notice Regression:** `notice-regression.json` passed (restart and driver consent modal dialogs verified).

### 4. Fabricated Telemetry Scan
- **Grep Target:** `(1000\s*Hz|bus jitter|1\.1ms|4\.12V|v1\.[0-9]|Profile synchron)`
- **Hits in Hollow:** **0**

---

## 4. Summary of Files Changed

- `desktop-dotnet/X20Ctl.Product/Themes/Hollow/HollowKit.cs`: Rebuilt cavern backdrop; added `HollowPrimaryApplyButton` with diamond flanks; cleaned `HollowButton`.
- `desktop-dotnet/X20Ctl.Product/StudioScene.HollowButtons.cs`: Fixed right panel layout; hooked real `ApplyToController`; truthful footer status.
- `desktop-dotnet/X20Ctl.Product/StudioScene.HollowCurves.cs`: Hooked real `ApplyToController`.
- `desktop-dotnet/X20Ctl.Product/StudioScene.HollowMacros.cs`: Hooked real `ApplyToController` and `LoadFromController`.
- `desktop-dotnet/X20Ctl.Product/StudioScene.HollowDevice.cs`: Bound to real `Device` telemetry; hooked `RefreshDetailsAsync()` and real `FactoryReset(say)` flow; registered `Device.Watch`.
- `desktop-dotnet/X20Ctl.Product/StudioScene.HollowPcMode.cs` & `StudioScene.HollowVibration.cs`: Removed fake Apply buttons; added real informational status notes.
- `desktop-dotnet/X20Ctl.Product/StudioScene.HollowLighting.cs` & `StudioScene.HollowScreen.cs`: Delegated to real `ShowManagement("RGB")` and `ShowManagement("Screen")`.
