# Inventory marking regression

Run on Windows with the .NET SDK and a Valheim installation containing BepInEx:

```powershell
./scripts/test-overlays.ps1 -ValheimInstall 'F:/Steam/steamapps/common/Valheim'
```

The runner builds a test-only plugin, copies the Unity launcher and BepInEx core
into a unique temporary directory, and links the installed game data/runtime.
It uses separate plugins, configuration, logs and a save path. It runs before
Steam/world initialization and quits automatically; it does not load characters,
connect to a server or change the installed mod. The artifact directory printed
by the runner retains `overlay-results.txt` and logs. Do not install the test DLL
into your normal game.

The test boundary is `InventoryGrid.UpdateInventory`: the real game method and
Harmony overlay patch update native Unity `Image` and `RectTransform` objects.
Only platform input is replaced to avoid initializing Steam or using the mouse.
The fixture reproduces icon cloning and slot positioning from the other mods;
it does not load their complete plugins or replace a live multiplayer check.

Checks cover icon copies, moves into and out of extra slots, reopening, icon
scales 0.2/0.75/2, label draw order, non-intercepting graphics, per-stack locks,
and restock marks shared by item name. A separate food fixture checks moving a
restock-only item between two regular cells, without any extra slots.

## Why the copy matters

On 2026-09-19 a temporary read-only probe captured an active
`shield/SmartCraft_LockedOverlay` on an empty regular inventory cell:
`owned=False`, `used=False`, `item=EMPTY`, with 36 renderer vertices.
The original cached overlay was disabled. The user reproduced the issue more
often on a dedicated server; the captured defect is in the client's UI.
The same probe also captured `excluded/SmartCraft_RestockOverlay` on an empty
regular cell (`owned=False`, `used=False`, 4 renderer vertices), matching the
user's food movement report within the regular inventory.

Shield Me Bruh 2.1.0's `AutoShield.CreateShieldedImage` and
`WeaponExclusion.CreateExcludedImage` clone `element.m_icon`, including its
children. Disabling the copied parent Image leaves child Images active. The
previous SmartCraft implementation attached marks under the icon, so the copied
marks escaped its dictionaries and remained visible after moving the item.

ExtraSlots 1.2.12 moves the same inventory element roots into its panel.
MyLittleUI 1.2.23 changes icon scale. Attaching marks to the slot, immediately
after the icon in draw order, addresses both transformations and prevents the
confirmed copies without changing either mod or the saved marking data.
Relevant upstream sources:
[ExtraSlots EquipmentPanel](https://github.com/shudnal/ExtraSlots/blob/67ec757b69b2fc675adb8d2ce310bb093b510d9b/EquipmentPanel.cs),
[MyLittleUI ItemIcon](https://github.com/shudnal/MyLittleUI/blob/7ebc605a55c2f5a27cdc973961ec4d6ccebe2b44/ItemIcon.cs).

The initial native reproduction failed old-cell cleanup and reopening before the
parenting fix, then passed both. The expanded suite covers 18 checks. Production
builds have the same three pre-existing CS0436 publicizer warnings.
