# Controller-specific programmable buttons

Checked 15 October 2026. Physical button counts are separate from PC macro playback triggers and from proven firmware-write support.

| Controller | Physical programmable buttons | Location / evidence |
|---|---:|---|
| X20 Pro | 6 (M1-M6) | Two mini shoulders, two rear buttons, two removable paddles; [EasySMX specification](https://www.easysmx.com/ja/products/easysmx%C2%AE-x20-pro-wireless-multiplatform-gaming-controller) |
| X20 | 4 (M1-M4) | [EasySMX support](https://www.easysmx.com/en-de/pages/support-about-easysmx-x20-controller) |
| Dune | 4 (M1-M4) | [EasySMX launch specification](https://www.easysmx.com/blogs/news/easysmx-launches-dune-8k-gaming-controller-featuring-native-8000hz-polling-rate-tmr-sticks-and-interactive-screen) |
| X05 Pro | 2 (M1-M2) | On top, not rear paddles; [EasySMX specification](https://www.easysmx.com/products/easysmx-x05pro-multiplatform-wireless-gaming-controller-with-noise-canceling-button) |
| X10 | 2 (M1-M2) | [EasySMX support, programmable-button section](https://www.easysmx.com/pages/support-about-easysmx-x10-controller) |
| X15 | 2 (M1-M2) | [EasySMX specification](https://www.easysmx.com/collections/best-selling-collection/products/easysmx-x15-pc-controller-with-rgb-light-and-hall-joysticks) |
| D10 | 2 (M1-M2) | [EasySMX specification](https://www.easysmx.com/products/easysmx-d10-multiplatform-gaming-controller-black-version-tmr-sticks-trigger-lock-and-charging-dock) |
| X05 | 0 | Retained existing owner-confirmed catalog and artwork inventory; no new hardware inspection in this check |

## Fixed software paths

The macro draft previously allocated M1-M4 for every model. The macro workbench filtered out M5/M6, and Studio navigation and PC-mode loading/saving repeated that four-slot assumption. These paths now use the selected draft's model-specific slot names. Agent paddle validation shares the physical inventory. Catalog counts are covered by regression tests.

The X05 retains four clearly labelled PC-only software macro slots, not four fictional physical paddles. PC mode starts sequences using an ordinary gameplay button selected in the editor. It does not independently receive M1-M6 from XInput or program controller firmware.

The legacy native X20 setup-file format remains four slots and X20-only. This change does not unlock native setup formats or firmware writes for other models. X20 Pro's six PC sequences are saved and loaded through PC-mode profiles.
