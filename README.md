# SmartCraft-Storage

A personal Valheim 1.0 mod that combines storage and station automation
into one cohesive package: mass-stashing items into nearby chests,
crafting/building with materials pulled straight from chests without
opening them, keeping fireplaces, smelters, charcoal kilns and cooking
stations fueled and self-collecting, automatically feeding tameable
animals from a nearby chest, and repairing all your gear at once.

Requires [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
and [Jotunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/).

- **Quick-stack:** Press `Shift + E` to stash matching items into nearby chests.
- **Lock and restock:** `Alt + left-click` locks an item so quick-stack never moves it. `Alt + Ctrl + left-click` marks it for restock instead.
- **Restock:** Press `Ctrl + E` to refill every marked item to a full stack from nearby chests, even from zero.
- **Crafting and building:** Use materials from nearby chests within a configurable radius (default 20m) — no need to open them.
- **Repair-all:** The station's Repair button fixes every repairable equipped item in one click instead of one at a time.
- **Fuel and ingredients:** Fireplaces, smelters, charcoal kilns, and cooking stations pull fuel/ingredients from nearby chests and store their output automatically — each behavior toggleable on its own.
- **Animal feeding:** Automatically feeds hungry tameable animals from nearby chests, taming or already-tamed.

## Hotkeys

All hotkeys use **E** as the base key, combined with a modifier. They work
with a free cursor (no need to have a chest open).

| Hotkey | Action |
|---|---|
| `Shift + E` | **Quick-stack**: stashes items from your inventory into nearby chests that already contain that item. Locked (🔒) and equipped items are never moved. |
| `Ctrl + E` | **Restock**: pulls from nearby chest(s) enough of each item marked for restock to fill a full stack in your inventory — even if you currently have none of that item. |
| `Alt + left-click` on an inventory item | Toggles the **lock** (🔒) on that item — a locked item is never moved by quick-stack. |
| `Alt + Ctrl + left-click` on an inventory item | Toggles the **restock** mark (🔵) on that item — defines the list `Ctrl+E` uses. |

Both modifier-clicks replace the normal click (they don't open/move the
item) only while the modifier is held.

## Nearby chests: how they're chosen

Every automation in this mod (quick-stack, restock, crafting-from-nearby-
chests, the 4 stations, and the automatic animal feeder) uses the same
rule to decide which chests count as "nearby":

- Within the configured radius (see the configuration section)
- Not a dead player's coffin (`TombStone`)
- Not currently in use by anyone else (open by another player)
- You have access permission on the chest (respects public/private/group)
- The chest is inside a Ward (protection) area that grants you access — a
  chest outside your ward, or inside someone else's ward without
  permission, is ignored

Works in multiplayer: when the mod needs to write to a chest owned by
another player (different ZDO owner), it claims ownership before touching
it, the same way the game itself does when you open a chest manually.

## Mass storage (Quick-Stack)

`Shift + E` — for each item in your inventory (except equipped and locked
items), looks for a nearby chest that already has that item (same name and
quality) and moves it there. It tops off existing stacks in the chest
first; if there's leftover quantity and the chest has free space, it
creates a new stack there too. It only moves an item into a chest that
**already contains** it — this isn't a "stash everything," it's "find
where that item already lives."

## Item lock

`Alt + left-click` on an item marks it with an orange border. A locked
item is never moved by quick-stack, even if a chest with that same item is
nearby. Useful for keeping ammo, food, or building material always in your
inventory.

## Restock

Mark which items you always want restocked with `Alt + Ctrl + left-click`
(marks it with a blue dot in the corner of the slot). Then, `Ctrl + E`
pulls enough of each marked item from nearby chests to fill a full stack
in your inventory — even if you currently have zero of that item. Example:
mark arrows and cooked meat; every time you press `Ctrl+E`, it fills your
inventory with both from whatever's in nearby chests.

## Crafting and building from nearby chests

While crafting at a workbench/forge/etc. or building (hammer in hand), the
game treats items in nearby chests as if they were in your inventory —
without needing to open any chest. It always prioritizes consuming what
you're already carrying first, only pulling from a chest for what's
missing. The count shown in the crafting UI already includes nearby
chests too.

## Repair-all

At a crafting station (workbench, forge, etc.), the **Repair** button now
fixes every equipped item the current station can repair in one go —
instead of having to click repeatedly until nothing's left. It respects
the exact same vanilla rule (station level vs. the item's minimum required
level): an item that needs a more advanced station still won't get
repaired there. No extra on-screen message, no configuration — just
instant repair.

## Automatic stations

The 4 station categories pull material from nearby chests on their own
and store the output instead of dropping it on the ground. Each has its
own radius and can be disabled individually (see the configuration
table). Skill XP (Cooking) from automatic food collection always goes to
whoever owns the station (typically whoever built it or interacted with
it first) — in multiplayer, not necessarily whoever's nearby or supplied
the ingredient.

### Fireplace, torch and hearth

Refuels fuel (wood, resin, etc.) by pulling from the nearest chest until
full, one unit at a time. It has no buffer/queue — it's just a fuel tank,
so it tops up to the max whenever there's room.

### Smelter (ore forge)

Automatically pulls ore and fuel from nearby chests and stores the
produced bar in the nearest chest with space. If no chest has room left,
the remainder drops on the ground as usual (default game behavior, with
no duplication or loss of what was already stored).

### Charcoal kiln

Same radius as the smelter (configured together). Unlike the fireplace, it
has a configurable **buffer** (default: 3) — it only keeps that much wood
in its internal queue instead of filling it all at once, letting ongoing
production finish before pulling more. It also has a configurable cap on
coal accumulated in nearby chests: past that cap, it stops pulling new
wood (without interrupting what's already processing). The coal it
produces first tries to feed nearby smelters that are low on fuel
(configurable strategy: prioritize the one with the least fuel, or the
nearest one); only the leftover goes to a chest.

### Cooking station (fire spit, cauldron, etc.)

Pulls raw food (and its own fuel, if the station uses one) from nearby
chests and cooks on its own. When an item finishes cooking, it's collected
automatically and stored in the nearest chest — no need to interact with
the station to take the finished food. Automatic collection goes through
the same code path as a manual interaction, so skill XP and yield bonuses
keep working normally (see the note above about who gets the XP).

## Automatic animal feeding

Tameable animals (boar, wolf, lox, and any other creature with the game's
own taming component) that are hungry automatically pull compatible food
from a nearby chest — both while taming a wild animal and afterward, to
keep breeding active in a pen. The mod spawns the actual food item near
the animal (it doesn't magically make the animal "fed"): the animal walks
over and eats it normally, with the same animation as always — only the
food's source (a chest instead of you manually dropping it) is automated.
Each species only ever receives an item it actually accepts (the same list
the game itself already uses to decide what that animal eats).

Note: this automation also feeds wild animals that aren't tamed yet, or
any tameable creature that isn't in alert/combat mode, as long as there's
a chest with compatible food within range — it doesn't distinguish "this
one I want to feed" from "this one's just passing by the chest."

## Configuration

All options live in BepInEx's [Configuration Manager](https://valheim.thunderstore.io/package/Azumatt/Official_BepInEx_ConfigurationManager/),
split into four sections.

**Radii** (storage/restock/crafting-from-chest):

| Option | Default | Description |
|---|---|---|
| `QuickStackRadius` | 20m | Radius in which quick-stack and restock search for chests |
| `CraftingChestRadius` | 20m | Radius in which crafting/building considers items from nearby chests |

**Stations** (radii and per-behavior on/off):

| Option | Default | Description |
|---|---|---|
| `FireplaceRadius` | 10m | Radius for fireplaces/torches/hearths |
| `SmelterKilnRadius` | 10m | Radius shared between smelters and charcoal kilns |
| `CookingStationRadius` | 10m | Radius for cooking stations |
| `FireplaceAutoRefuel` | on | Fireplaces automatically pull fuel |
| `SmelterAutoRefuel` | on | Smelters automatically pull ore/fuel |
| `SmelterAutoCollect` | on | Smelters store their output in a chest |
| `KilnAutoRefuel` | on | Kilns automatically pull wood |
| `KilnAutoCollect` | on | Kilns store/redirect the coal they produce |
| `CookingStationAutoRefuel` | on | Cooking stations automatically pull raw food/fuel |
| `CookingStationAutoCollect` | on | Cooking stations collect and store on their own |

**Charcoal kiln** (kiln-specific tuning):

| Option | Default | Description |
|---|---|---|
| `KilnWoodBuffer` | 3 | Wood level kept in the internal queue (not the kiln's max capacity) |
| `KilnMaxCoalInChest` | 50 | Coal cap in nearby chests before pausing new wood pulls |
| `KilnFeedStrategy` | `LeastFuelFirst` | How to pick which nearby smelter to feed first: `LeastFuelFirst` or `Nearest` |

**Animals** (automatic feeding):

| Option | Default | Description |
|---|---|---|
| `AnimalFeederRadius` | 20m | Radius in which hungry tameable animals search nearby chests for food |
| `AnimalAutoFeed` | on | Tameable animals automatically pull compatible food from nearby chests |

Every option also has its own description inside the Configuration
Manager.
