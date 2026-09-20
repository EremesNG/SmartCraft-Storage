# Native chest restoration and terminal padding — 0.7.7 root checks

These are root execution checks, not an independent verdict or a claim of live
0.7.7 game acceptance. User screenshots are native-chest-076-scrollbar.png and
terminal-076-padding.png. The user further observed that restarting and opening
a normal chest before the terminal keeps both subsequent normal-chest views
correct. That is pre-fix evidence consistent with a first-view cache dependency.

## Cause and correction

The inspected Valheim Plus InventoryGrid_UpdateGui_Patch caches container width,
viewport right inset and scrollbar anchored X the first time it sees the native
container grid. SCS 0.7.6 configured its right-anchored scrollbar at X=-16 before
that hook could initialize. When SCS restored the native center anchor, the
other hook replayed -16, moving the bar near the center. Rect restoration alone
therefore could not repair the cached coordinate system.

The Unity regression replays that source-backed lazy cache at the real native
UpdateGui boundary. Pre-fix: native width 612 and scrollbar X=-16 after closing
the first terminal. Corrected: native width 600 and scrollbar X=284, including
three more terminal/chest alternations. This fixture represents the inspected
layout/cache contract, not the full Valheim Plus plugin.

InitializeNativeLayout now lets native UpdateGui/layout hooks observe the native
baseline before any terminal geometry changes. If there is no prior inventory,
it temporarily supplies an empty one-cell UI inventory and restores the original
reference in finally. Calling UpdateGui directly avoids a second gamepad-input
pass. No physical inventory, external private cache, save or installed config
is modified by this initialization.

The prior four-row minimum created visible padding unrelated to finite physical
capacity. Projection inventories now use their required row count, at least one.
The viewport and action row adapt up to four visible rows, preserving the panel's
top edge. CanvasGroups hide unused projection cells while retaining their native
raycast/input behavior. New ordinary grids are recreated on close, so their real
empty slots stay visible. Occupied aggregate entries, capacity counters, native
weight, transactions, search focus, hotkeys and station discovery are unchanged.

## Evidence

- native-scrollbar-cache-red.log: reproduced the first-terminal native-bar failure.
- native-scrollbar-cache-green.log: baseline and repeated alternation checks pass.
- terminal-padding-red.log: two-row height, visible padding and empty-result checks fail before correction.
- terminal-padding-input.log: final 27/27 real Unity/config checks pass with zero
  captured engine errors. Includes adaptive height, hidden padding, UGUI raycast
  hits on a transparent cell, native selection callback, entry appearance after
  deposition and ordinary empty-slot restoration. Existing scroll/hotkey cases pass.
- Pointer verification waits two Canvas frames with graphics enabled; in Awake,
  graphics have depth -1 and cannot be hit. Fixture-only startup suppression and
  persistent test roots avoid scene/platform initialization or unloads. These
  fixture qualification issues were fixed before accepting the pointer result.
- restore-grid-contracts.log: 40/40; restore-grid-runtime.log: 4/4.
- restore-grid-package.log: Release 0 errors, 3 existing CS0436 warnings.
- restore-grid-patches.log: 39 Harmony targets/arguments, 5 lifecycle methods.
- restore-grid-package-inspection.json: exact five distributable entries,
  matching embedded/Release DLL, all versions 0.7.7 / file 0.7.7.0, prior 0.7.6 unchanged.
- restore-grid-ready.json: full ready validator valid; existing conditional
  checklist warning only. This bounded UI correction adds no storage contract risk.

The fixture's sixteen entries reproduce the visible grouping shape; it does not
simulate two actual full chests. The physical capacity counter still reads the
unchanged StorageView UsedSlots/TotalSlots fields. A live 30/30 counter with
sixteen visible entries, native skin/mod interaction, controller/touch and
multiplayer remain acceptance observations.

Simplify: one PositionActions method follows the same clamped row count as the
viewport, MaxVisibleRows states the upper bound explicitly, and initialization
restores its temporary inventory reference through finally. No reflection into
Valheim Plus, global UI patch or new storage layer was added. The test callback
waits only for Canvas readiness and is excluded from the package.
