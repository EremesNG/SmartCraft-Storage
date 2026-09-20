# SmartCraft-Storage

A personal Valheim 1.0 mod that combines storage and station automation
into one cohesive package: mass-stashing items into nearby chests,
crafting/building with materials pulled straight from chests without
opening them, keeping fireplaces, smelters, charcoal kilns and cooking
stations fueled and self-collecting, automatically feeding tameable
animals from a nearby chest, and repairing all your gear at once.

Requires [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
and [Jotunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/).

**Version 0.7.8 requires the same SmartCraftStorage version on the server and
every client**, with BepInEx and Jotunn. The server coordinates storage transfers;
the owning client performs player and machine effects. Automation still operates
in loaded areas with a nearby player. This does not run an unloaded base offline.

This is a local validation candidate. The offline contract suite and plugin
build are reproducible below; visual and two-client game acceptance are still
required before publishing it.

- **Quick-stack:** Press `Shift + E` to stash matching items into nearby chests.
- **Storage terminal:** Manage the combined contents of named nearby chests from one window, including deposits, withdrawals, searching and physical organization.
- **Lock and restock:** `Alt + left-click` locks an item so quick-stack never moves it. `Alt + Ctrl + left-click` marks it for restock instead.
- **Restock:** Press `Ctrl + E` to refill every marked item to a full stack from nearby chests, even from zero.
- **Crafting and building:** Use materials from nearby chests within a configurable radius (default 20m) — no need to open them.
- **Available amounts:** Every ingredient shows what you can actually spend in brackets after the amount it needs — `10 (34)` — in both the crafting panel and the build HUD.
- **Repair-all:** The station's Repair button fixes every repairable equipped item in one click instead of one at a time.
- **Fuel and ingredients:** Fireplaces, smelters, charcoal kilns, and cooking stations pull fuel/ingredients from nearby chests and store their output automatically — each behavior toggleable on its own.
- **Beehives:** Honey is harvested automatically as soon as it's ready and stored in the nearest chest, no need to visit the hive.
- **Fermenter:** Automatically pulls any mead/potion base from nearby chests and stores the finished product once ready.
- **Animal feeding:** Automatically feeds hungry tameable animals from nearby chests, taming or already-tamed.
- **Plant harvest (in test, off by default):** Optionally auto-harvests ripe crops near the player into the nearest chest.
- **Epic Loot compatibility (optional):** If [Epic Loot](https://valheim.thunderstore.io/package/RandyKnapp/EpicLoot/) is also installed, the Enchanter can use materials from nearby chests too.

## Storage terminal

1. Build **Storage terminal** from the hammer's Furniture category, near a
   workbench, using 10 Wood and 2 Bronze. It initially uses the wooden chest model.
2. Open it with `E` and select **Network** to name it, or use `Alt + N` on the
   terminal. Enter a network name and select **Link**.
3. Use `Alt + N` on each ordinary backing chest and give it the same network name.
   Names ignore leading/trailing spaces and case. An empty name unlinks a chest.
4. Keep the chests inside `Storage network → TerminalRadius` (default 32 m).
   Access requires permission at both the terminal and the physical chests.
5. Use the native chest grid: drag from your inventory to deposit, drag to an
   empty or compatible player slot to withdraw, `Shift + click` to split, or
   `Ctrl + click` to move one stack. Equipped, quest and locked items are protected.

Change the naming shortcut in Configuration Manager under
`Hotkeys → NetworkNameShortcut`, or in the mod's config file. It is a local player
setting. Upgrading from 0.7.5 or earlier changes the former exact default `Alt + T` to
`Alt + N`; other bindings, including a disabled shortcut, are preserved. Later
changes remain yours, including choosing `Alt + T` again.

The terminal shows eight columns and as many rows as its entries need, up to
four visible rows, with scrolling for more items. Only occupied entries have
visible cells: these are grouped contents, not physical slots. For example,
16 entries can fill 30/30 physical slots and appear in two rows. Empty padding
stays invisible but still accepts dragged deposits; an empty result retains one
blank deposit row. Search and display order sit above the grid; the three action
buttons sit below it. The native chest weight indicator shows the total weight
of the whole network, including items hidden by search. There is no keyboard-help footer.

Each cell shows the total amount of one exact item identity across the network.
A normal click, split or quick move handles at most one legal stack. Dropping on
an incompatible occupied player slot changes nothing; the terminal does not swap
items. Dropping a network entry outside the window cancels that drag.

While the search field has focus, typing does not move or control the character.
Click outside the field, press `Enter` or `Escape`, or close the terminal to
leave text entry and restore normal controls.

**Take all** withdraws from the full network, including items hidden by search.
**Stack all** deposits unlocked carried stacks whose exact identity already exists
in the network. Both commands wait for confirmation before submitting the next
item. Closing the window cancels remaining unsent steps. Search and view sorting
never restrict station resources or change the physical layout.

The terminal adds no storage slots. Its capacity comes from linked physical
chests, and their contents stay in their normal inventories. Graves, carts,
ships, other terminals and BottomlessChest containers cannot be backing members.
Other mods that replace a chest with a hidden or virtual inventory are unsupported.

**Organize network** combines strictly compatible stacks and packs mixed items
into fewer chests when possible. It does not assign a permanent item category to
any chest. Sorting the grid by name or quantity changes only the display. Hover
an item to inspect its usual tooltip; different quality, variant, durability,
crafter or custom data remain separate identities.

Crafting/upgrading and enabled processor automation discover terminals inside
their existing radius. From there, they can use that terminal's linked chests
inside its separate network radius. The terminal window can stay closed. A
physical chest reached directly or through several terminals is counted once.

| Consumer | Network behavior |
|---|---|
| Workbench/forge and other crafting stations | Recipe materials and upgrades |
| Smelter / charcoal kiln | Ore, wood, fuel and processed output |
| Cooking station | Ingredients, fuel and finished food |
| Fireplace / torch / hearth | Fuel |
| Fermenter | Bases and finished product |
| Beehive, when collection is enabled | Honey output |

`Shift + E`, restock, hammer building, animal feeding, plant harvesting and Epic
Loot keep their direct chest radius. Quick-stack and restock may now show pending
completion while the server coordinates a move. Synchronous features such as
building and Epic Loot use only currently owned, available direct inventories;
they skip foreign-owned or reserved chests instead of taking ownership mid-transfer.

Processing fills available input slots incrementally. Kiln wood restrictions,
wood buffer, coal cap and direct nearby-smelter priority remain in effect.
Ordinary processed output that cannot fit is dropped only after its accepted
quantity is confirmed. Automatically harvested honey waits for full storage
capacity instead of falling on the ground. A ready fermenter's output is captured
at tapping, so recovery does not depend on its transient delayed-drop callback.

## Pending transfers and multiplayer

An operation reserves its participants and records its identity and receipts.
Closing the terminal does not cancel a submitted transfer. Missing responses
remain pending; reopening/reconnecting resumes the same operation. Ordinary
opening, inventory moves and dismantling are blocked while the affected participant
is reserved. Keep its owner and area loaded until recovery can finish.

Do not delete SCS journal or character custom data to clear a pending message.
Finish pending transfers before changing worlds. Before downgrading, resolve
transfers, unlink the network, and dismantle terminals.
The real chests remain ordinary storage. Restoring world and character saves from
different historical moments is outside the recovery guarantee. The recovery
journal is part of the world, and player requests/receipts use character custom
data; this does not make separately saved world and character files crash-atomic.

MultiUserChest is optional; SCS guards the native inventory mutation boundaries.
Compatibility with its live simultaneous-access flow still needs the test below.
Arbitrary third-party writes that bypass those boundaries are not coordinated.

Server-synced settings also bound member count, queued operations and work per
update. Defaults are 64 members, 128 pending operations and 4 transitions per
update. A limit or unavailable member must not be interpreted as extra capacity.

For the first local 0.7.8 test:

- Install the same candidate on the server and client. Join and wait before
  using storage, then test processor inputs/outputs, terminal transfers and a
  reconnect. Pending work should recover without repeated send-limit messages.
  Recovery now backs off from half a second to eight seconds while waiting;
  acknowledged progress can continue immediately. Keep the previous DLL for
  comparison if the issue returns.

- Restart Valheim and open the terminal before opening any ordinary chest.
  Close it and open a normal chest: its scrollbar must stay at the right edge.
  Repeat terminal/chest alternation, including differently sized chests.
- Fill a 30-slot network with 16 grouped entries. Confirm 16 visible cells in
  two rows, with the counter still showing 30/30. Search for one entry, then a
  missing entry, and clear search. Deposit by dragging onto a blank grid area;
  only available physical capacity may be used.

- Open a network with more than 32 distinct item entries. Confirm eight columns
  and four complete rows, then scroll to the last row. The scrollbar must stay
  beside the grid, clear of the single row of action buttons. Check both the
  normal window size and your preferred UI scale.
- Check the native weight pouch beside the container. Search for a single item:
  the displayed weight must still include the entire network. Deposit or withdraw
  a stack and confirm it updates after the transfer.
- Try `Alt + N`, then choose your own `Hotkeys → NetworkNameShortcut` and restart.
  Confirm that the custom binding remains and the hover hint matches it.

- Add mixed partial stacks to linked chests, deposit/withdraw through the
  terminal, close/reopen it, then hold Organize for half a second before
  releasing. The controls must not blink or cancel the click. Repeat with a
  quick click and confirm organization works without first opening backing chests.
- Click search and type letters including WASD: the character must stay still.
  Click outside, then repeat with Enter, Escape and closing the terminal; normal
  movement must return after each exit. Other open text dialogs must still block it.
- Open the terminal with Valheim Plus inventory settings enabled. Confirm that
  cells, item icons and quantities appear below search, and that an ordinary
  chest still shows its normal grid after closing the terminal.
- Name a terminal and two ordinary chests with your configured shortcut, close and reopen each
  dialog, then leave and re-enter the same world. Confirm names persist and the
  BepInEx log has no repeated SCS exceptions.
- Put more than one stack of the same material in the backing chests. Confirm
  the total, search, drag in both directions, split and Ctrl-click. Check that an
  incompatible player slot and dropping outside leave quantities unchanged.
- Use Stack all, Take all and Organize; close during a pending step and reopen.
  Then open an ordinary chest and check its normal layout and controls.

Before using this candidate in a shared world, test on a copy with two clients:

- Link four finite chests, search/deposit/withdraw without opening them, and
  confirm Organize conserves all quantities and becomes a no-op on repetition.
- Put materials outside direct station range but inside terminal range. Craft
  and upgrade, then exercise each enabled machine's input and output with the
  terminal closed. Include a network-only single-ingredient recipe.
- Leave capacity for eight units of a ten-unit output; confirm only the
  actual remainder drops. Test automatic honey with insufficient capacity.
- Run concurrent terminal actions and direct quick-stack; change wards and
  reconnect during pending transfers. Confirm no double payment or delivery,
  and repeat with MultiUserChest enabled.
- Inspect the window at 1280×720 and 1920×1080, including scrolling, split dialogs,
  search keyboard focus, Escape, controller input, English and Spanish labels.

## Hotkeys

Defaults below — every hotkey is fully configurable in the [Configuration Manager](https://valheim.thunderstore.io/package/Azumatt/Official_BepInEx_ConfigurationManager/),
all as full key combos you set by clicking the value and pressing whatever
combo you want. The lock and restock-mark combos are independent of each
other. They work with a free cursor (no need to have a chest open).

| Hotkey (default) | Action |
|---|---|
| `Shift + E` | **Quick-stack**: stashes items from your inventory into nearby chests that already contain that item. Locked (🔒) and equipped items are never moved. |
| `Ctrl + E` | **Restock**: pulls from nearby chest(s) enough of each item marked for restock to fill a full stack in your inventory — even if you currently have none of that item. |
| `E` on a terminal | Open the network manager. |
| `Alt + N` while looking at a chest or terminal | Set or clear its network name. Configurable as `Hotkeys.NetworkNameShortcut`; existing quick-stack/restock shortcuts take priority. |
| `Alt + left-click` on an inventory item | Toggles the **lock** (🔒) on that item — a locked item is never moved by quick-stack. |
| `Alt + Ctrl + left-click` on an inventory item | Toggles the **restock** mark (🔵) on that item — defines the list the restock hotkey uses. |

Both modifier-clicks replace the normal click (they don't open/move the
item) only while the modifier is held.

Messages shown by quick-stack and restock are localized (English,
Portuguese-Brazilian, Spanish so far) based on your in-game language.

## Building

Requires the [.NET SDK](https://dotnet.microsoft.com/download) and a Valheim
install. Set `VALHEIM_INSTALL` if yours is not in the default Steam location.

```pwsh
dotnet build -c Release          # just the plugin -> bin/Release/net48/
./scripts/package.ps1            # the installable zip -> dist/
./scripts/package.ps1 -Version 0.2.1   # override the version for a test build
```

With local cached dependencies and a directory of game reference assemblies:

```pwsh
dotnet run --project tests/Storage.Contracts.Tests --no-restore
dotnet run --project tests/Storage.Runtime.Tests --no-restore
dotnet build -c Release --no-restore -p:ValheimManagedDir='C:/path/to/Managed'
./scripts/check-patches.ps1 -ValheimManagedDir 'C:/path/to/Managed'
./scripts/package.ps1 -ValheimManagedDir 'C:/path/to/Managed' -NoRestore
```

The test harnesses require .NET 10; the plugin targets .NET Framework 4.8.
On a fresh offline checkout, restore the harness from your local NuGet cache
first. The harness runs production planning, transaction, effect and station
controllers. The runtime fixture also exercises the production journal and item
adapter against a minimal external game boundary. Neither launches Unity nor
substitutes for the gameplay tests above.

`package.ps1` produces a Thunderstore-layout zip you can also hand to
r2modman or Thunderstore Mod Manager directly through **Settings → Import
local mod**. Note that `bin/Release/net48/` additionally contains the game's
own assemblies (`assembly_valheim.dll`, `Jotunn.dll`, the UnityEngine
modules) because they are build references — the package deliberately ships
only `SmartCraftStorage.dll`.

## Links

[GitHub](https://github.com/Zellds/SmartCraft-Storage) · [@urano_jpg](https://x.com/urano_jpg)

## Full documentation

Detailed docs for every feature, the full configuration reference, and a
FAQ/troubleshooting page live on the
[wiki](https://thunderstore.io/c/valheim/p/Zellds/SmartCraftStorage/wiki):
how nearby chests are chosen, station-by-station behavior, animal feeding
details, every config option with its default, and common questions
(multiplayer/ward behavior, does the mod need updating when the game
updates, etc).
