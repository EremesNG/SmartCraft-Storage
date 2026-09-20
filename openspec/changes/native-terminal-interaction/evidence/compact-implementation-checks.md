# Compact terminal 0.7.6 — root execution evidence

This report records implementation checks, not independent approval or live-game
acceptance. The generated image is a design reference, not a game capture.

## Scope and implementation

- NativeTerminalInventory now projects eight columns with at least four rows.
  Search and sort share a row. Take all, Stack all and Organize share one row below
  the viewport. No idle gesture-help label or bottom instruction strip remains.
- NativeTerminalLayout gives the grid four cell heights, a separate right inset
  for the scrollbar and an eight-unit gap before actions. The native scrollbar
  is positioned alongside the viewport. Its original hierarchy/rect, the panel,
  grid and buttons are captured/restored on close; title alignment is restored.
- After external inventory layout hooks, absolute column alignment prevents
  accumulating position changes and preserves the scroll offset. Opening,
  search/order changes and row-count changes reset the native view after layout.
- `_networkWeight` sums `StorageView.Rows` with `Sample.GetWeight(Amount)` before
  filtering. Render writes its ceiling into `InventoryGui.m_containerWeight`.
  The existing weight widget, placement and original text restoration remain.
  No weight value is drawn in the header. Native ItemData.GetWeight includes
  quality-scaled weight and accepts the complete aggregated amount.
- Header feedback remains for pending/completed/error operations. Search input
  still uses the live focus gate; raw mouse-down freezes projection refresh
  without disabling buttons. Transfer/crafting/processor/ward/range logic and
  native gesture callbacks are unchanged by this refinement.
- Hotkeys.NetworkNameShortcut remains an editable local KeyboardShortcut entry,
  default LeftAlt+N. A hidden persisted migration marker upgrades only the exact
  old LeftAlt+T default once. Custom/disabled and later explicit Alt+T survive.
  Neither development nor fixture execution modifies the installed user config.

## Verification

- Pre-fix compact geometry: 24 visible cells, expected 32; see compact-layout-red.log.
- Pre-fix shortcut persistence: fresh Alt+N and old-default migration fail;
  existing custom/disabled/later bindings pass; see compact-shortcut-red.log.
- Final actual Unity/native InventoryGrid plus BepInEx config fixture: 18
  assertions pass, zero captured engine errors. Includes all four complete
  rows, last-row scrolling, scrollbar/footer separation, repeated native inset
  updates, ordinary grid/scrollbar restoration, and persisted config upgrades.
  The fixture's old unassigned GuiBar reference was repaired and its platform
  startup suppressed so synthetic components and unavailable Steam do not emit
  unrelated exceptions. Headless TMP font warnings do not establish font rendering.
- Existing production contract suite: 40/40. Runtime suite: 4/4.
- Release/package: 0 errors, 3 existing CS0436 attribute warnings.
- Metadata: 39 Harmony targets/arguments, 5 Unity lifecycle signatures.
- ZIP: exactly five distributable entries; manifest/project/file versions agree
  at 0.7.6; embedded DLL hash matches Release. Previous 0.7.5 package unchanged.
- Full ready validator: valid, only the existing conditional-checklist warning.
  This refinement does not expand the storage contract or activate that checklist.

## Simplify and limits

The cleanup keeps dimensions/scrollbar restoration and absolute column placement
in the existing tested layout helper. It removes the unused gesture-help
translations and the premature ResetView call. It introduces no generic layout
framework, new dependency or storage abstraction. Behavior preserved: aggregate
identities, legal stack limits, transactions, native gestures, search focus,
button click guards, wards and ordinary chest restoration.

The fixture creates native grid components with synthetic cells and a real
ScrollRect in an isolated headless game runtime. It does not load a world,
InventoryGui's complete skin, the full installed mod set or a character.
Only the generated concept was visually inspected; no live 0.7.6 UI screenshot
is claimed. User acceptance still covers preferred resolution/UI scale, label
fit, native weight presentation, gestures/search, mod interaction and multiplayer.
The selected concept and final built-in imagegen prompts are saved beside this
report. Product code uses native widgets, not the generated raster.
