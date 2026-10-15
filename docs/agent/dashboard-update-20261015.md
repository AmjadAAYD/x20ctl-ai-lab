# Dashboard update

Lower row: a vertically scrollable wrapping games grid with Add a game/folder on the left; selected artwork, title/store, Change icon and Play on the right. Artwork uses Uniform to preserve the complete image. Clicking a tile selects it; Play launches it. Four-way arrow navigation follows the wrapping grid.

Custom artwork is decoded and copied as PNG into LocalAppData/x20ctl/game-art/custom. Identity uses store, ID and launch URI; rename does not lose the image. Replacement uses a temporary file and atomic move after successful decoding. Game installation files are untouched.

Ready to play now has Buttons, Curves, Macros and Diagnostics shortcuts. The controller uses each model's own front/rear images. A gentle vertical hover continues while it turns edge-on and switches face every five seconds. Entry transition completion starts the motion; hidden/unloaded pages stop timers and animation. Reduced motion stays static.

Verification: Product build; isolated 18-game layout at 1500x740 and 1100x520 stays within the viewport. Artwork persistence/rename/failed-replacement checks. No hardware or full-app review harness. Live motion and file-dialog interaction require user review.

## Follow-up: five areas and Game setup

The lower row now separates the games library, complete-artwork Play panel and Game setup panel. Play actions are below the title to fit the narrower panel. Game setup filters saved snapshots by controller model, loads the selected snapshot into existing working settings via the existing loader, and links to Setups, Buttons, Curves and Macros. Empty libraries show a clear message. This loads current working settings; automatic per-game associations are not implemented. Existing native snapshot validation restrictions remain in force.

Focused component verification passed: matching-model setup filtering and load callback, 18-game layouts at 1500x740 and 1100x520, and artwork copy/rename/failed replacement preservation.
