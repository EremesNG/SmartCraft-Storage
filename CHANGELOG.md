# Changelog

## 0.7.7

- Fix the native chest scrollbar moving into the grid when a terminal is the
  first container opened with Valheim Plus inventory layout enabled. Initialize
  the native layout before applying terminal coordinates.
- Show only occupied grouped entries in the terminal. Remove misleading empty
  cells and fit the view to one through four rows; the capacity counter still
  describes the physical chests. Blank areas retain native deposit handling.

## 0.7.6

- Compact terminal inventory: eight columns, four visible rows, search beside
  display order, and one row of action buttons. The scrollbar ends above the
  buttons, and the drag/Shift/Ctrl help footer is removed.
- Show the full network weight in Valheim's native chest weight indicator,
  including items hidden by search. Operation feedback uses the header.
- Default the configurable network naming shortcut to `Alt + N`. Migrate the
  old exact `Alt + T` default once; preserve other custom or disabled bindings
  and all later changes.

## 0.7.5
- Fixed intermittent ignored clicks and blinking terminal controls when a refresh occurred while the mouse button was held. Organize and the other controls remain clickable while idle; dragging, splitting and pending-transfer protections remain active.

## 0.7.4
- Fixed character movement while typing in the terminal's search field. Character controls pause while search has focus and resume when focus leaves the field or the terminal closes.

## 0.7.3
- Fixed the terminal's blank inventory grid when Valheim Plus reapplies chest layout margins. The terminal now preserves native horizontal stretching and the existing panel width, keeping cells and controls visible.
- Added an isolated Unity regression check for empty/populated grids, external margin changes and ordinary-chest layout restoration. Full-mod gameplay acceptance remains pending for this local test candidate.

## 0.7.2
- Changed network naming to `Alt + T` while looking at a chest or terminal, freeing the former `Alt + E` naming shortcut for other mods. Customize it with `Hotkeys.NetworkNameShortcut`; hover help follows the configured shortcut.
- Naming uses the normal interaction range and access checks, and shortcuts stay inactive while the terminal or naming dialog is open. This remains a local test candidate.

## 0.7.1
- Replaced the terminal's item lists and transfer buttons with Valheim's native player and chest grids, including drag, split, quick transfer, search and pooled quantity labels.
- Fixed single-player linking that remained at "waiting" after a successful immediate reply. Naming drafts and pending state now belong to their individual chest; confirmed owner requests are not replayed.
- Fixed stale journal object references when returning to the same world, incomplete item data that broke terminal rendering, and the invalid Unity Start method signature.
- Directed withdrawals use the selected player slot and reject incompatible or changed destinations. Projected entries cannot be dropped, equipped, consumed or moved as real items.
- Native Take all and Stack all run one confirmed operation at a time. Stack all deposits existing network identities, protecting equipped, quest and locked items; closing cancels unsent steps.
- Preserved finite network capacity, Organize, station access and the direct range of Shift+E. This is a local test candidate; visual and multiplayer acceptance remain pending.

## 0.7.0
- Added a buildable storage terminal: link ordinary chests by network name and radius, search pooled contents, deposit, withdraw and organize compatible stacks into fewer chests.
- Crafting and supported processing inputs/outputs can use nearby terminal networks without opening the terminal or backing chests. Wards, chest privacy and finite slot capacity still apply.
- Coordinated transfers track pending operations and receipts across owners. A delayed reply does not trigger a second delivery or a duplicate ground drop.
- Quick-stack and restock retain their direct chest radius and now queue coordinated transfers. Building, animals, harvesting and Epic Loot remain direct; synchronous writes require an available local owner.
- Matching SmartCraftStorage versions are now required on the server and all clients. MultiUserChest remains optional.
- Added English/Spanish terminal controls and an offline contract harness. Live UI and dedicated-server acceptance remain required before publishing this candidate.

## 0.6.0
- If you also have Epic Loot installed, the Enchanter can now use materials from nearby chests too, the same way crafting and building already do. Not required, only kicks in when Epic Loot is present

## 0.5.1
- Fixed an edge case where undoing a partial automatic beehive collection (when a nearby chest couldn't fit everything) could remove honey that was already sitting in the chest instead of just the honey that had been added (community contribution by [ManuelROAL](https://github.com/Zellds/SmartCraft-Storage/pull/9))

## 0.5.0
Community contribution by [uy8Uk4N56G](https://github.com/Zellds/SmartCraft-Storage/pull/6):
- Further reduced stutter/freezing while crafting or building near chests, building on the fix from 0.4.1
- Fixed a rare multiplayer issue where writing to a chest at the same moment another player opened it could cause problems
- Crafting and building now show how much of each material you actually have available (inventory plus nearby chests), not just how much is needed. Toggle this off in the settings if you prefer the old look
- Cleaned up some unnecessary warnings that could show up in the mod's log
- Now requires BepInExPack 5.4.2350 or newer

## 0.4.2
- Fixed a beehive item duplication bug: if the automatic honey collection could only partially fit the harvested honey into nearby chests, the leftover was also duplicated on the ground instead of just the leftover being dropped
- Automatic beehive collection now leaves the honey queued in the hive (instead of dropping any of it) when no nearby chest can fit it all; manually interacting with the hive still drops the leftover on the ground as usual

## 0.4.1
- Fixed a performance issue where crafting/building from nearby chests could re-scan for chests dozens of times per frame while the crafting panel or the build piece list was open, potentially stalling the host long enough to disconnect other players. Nearby-chest results are now cached for 0.1s and refreshed immediately after an actual consumption, instead of re-scanning on every check

## 0.4.0
- Fermenters now auto-pull any mead/potion base from nearby chests and auto-collect the finished product once ready, with their own radius (`FermenterRadius`, max 25m) and an optional duration override (`FermenterDurationOverride`, 0 = keep the vanilla duration)
- New experimental feature, off by default: `PlantAutoHarvest` auto-harvests ripe cultivated crops near the player into the nearest chest (its own radius, `PlantHarvestRadius`, max 25m); marked in test since the game only exposes one flag to tell a farmed crop apart from a wild pickable

## 0.3.0
- Beehives now harvest honey automatically as soon as it's ready and store it in the nearest chest, no need to visit the hive (`BeehiveRadius`/`BeehiveAutoCollect`)
- Charcoal kilns now default to pulling only regular Wood from nearby chests instead of any wood type, since Fine Wood and Core Wood convert to coal at the same rate and burning them was pure waste (configurable via `KilnRegularWoodOnly`)

## 0.2.0
- Nearby chests are now searched nearest-first, so automations that stop at the first usable chest prefer the closest one (community contribution by [BearFlinn](https://github.com/Zellds/SmartCraft-Storage/pull/1))
- Servers running this mod can now set gameplay config (radii, all station toggles, kiln settings, animal feeder) for every connecting client; hotkeys always stay per-player (community contribution by [BearFlinn](https://github.com/Zellds/SmartCraft-Storage/pull/2))

## 0.1.3
- Hotkeys are now fully configurable: `QuickStackShortcut`/`RestockShortcut` let you set any key combo via the Configuration Manager (click and press), no fixed base key
- Lock and restock-mark click combos (`LockClickShortcut`/`RestockMarkClickShortcut`) are also configurable, and fully independent of each other (marking for restock no longer requires the lock combo to also be held)
- Config section names are now all in English for consistency
- Quick-stack and restock on-screen messages are now localized (English, Portuguese-Brazilian, Spanish)
- Removed the on-screen action hint that was tied to the old fixed hotkey registration

## 0.1.2
- Set the website link (GitHub repo) in the package metadata
- Trimmed the README down to a quick reference; full docs moved to the wiki

## 0.1.1
- Declared BepInExPack_Valheim as an explicit dependency (was only pulled transitively through Jotunn)

## 0.1.0
- Initial release
- Quick-stack (`Shift + E`), item lock (`Alt + left-click`), and restock (`Ctrl + E`, mark items with `Alt + Ctrl + left-click`)
- Crafting and building using materials from nearby chests
- Repair-all at crafting stations in one click
- Automatic fuel/ingredient pulling and output collection for fireplaces, smelters, charcoal kilns, and cooking stations
- Automatic feeding of tameable animals from nearby chests
