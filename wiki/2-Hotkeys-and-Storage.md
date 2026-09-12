# Hotkeys & Storage

Defaults below — every hotkey is configurable, all four as full key combos
you set by clicking the value in the
[Configuration Manager](https://valheim.thunderstore.io/package/Azumatt/Official_BepInEx_ConfigurationManager/)
and pressing whatever combo you want (no fixed base key). `LockClickShortcut`
and `RestockMarkClickShortcut` are fully independent of each other — holding
the restock-mark combo never requires the lock combo to also be held. They
work with a free cursor (no need to have a chest open).

| Hotkey (default) | Action |
|---|---|
| `Shift + E` | **Quick-stack**: stashes items from your inventory into nearby chests that already contain that item. Locked (🔒) and equipped items are never moved. |
| `Ctrl + E` | **Restock**: pulls from nearby chest(s) enough of each item marked for restock to fill a full stack in your inventory — even if you currently have none of that item. |
| `Alt + left-click` on an inventory item | Toggles the **lock** (🔒) on that item — a locked item is never moved by quick-stack. |
| `Alt + Ctrl + left-click` on an inventory item | Toggles the **restock** mark (🔵) on that item — defines the list the restock hotkey uses. |

Both modifier-clicks replace the normal click (they don't open/move the
item) only while the modifier is held.

Quick-stack and restock's on-screen messages are localized based on your
in-game language (English, Portuguese-Brazilian, and Spanish so far — more
can be added if there's demand).

## Nearby chests: how they're chosen

Every automation in this mod (quick-stack, restock, crafting-from-nearby-
chests, the stations, and the animal feeder) uses the same rule to decide
which chests count as "nearby":

- Within the configured radius (see the Configuration page)
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
