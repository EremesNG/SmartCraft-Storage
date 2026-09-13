# Changelog

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
