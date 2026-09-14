# Changelog

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
