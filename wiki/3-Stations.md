# Stations

The station automations pull material from nearby chests on their own and
store the output instead of dropping it on the ground. Each has its own
radius and can be disabled individually (see the Configuration page).

Skill XP (Cooking) from automatic food collection always goes to whoever
owns the station (typically whoever built it or interacted with it first)
— in multiplayer, not necessarily whoever's nearby or supplied the
ingredient.

## Fireplace, torch and hearth

Refuels fuel (wood, resin, etc.) by pulling from the nearest chest until
full, one unit at a time. It has no buffer/queue — it's just a fuel tank,
so it tops up to the max whenever there's room.

## Smelter (ore forge)

Automatically pulls ore and fuel from nearby chests and stores the
produced bar in the nearest chest with space. If no chest has room left,
the remainder drops on the ground as usual (default game behavior, with
no duplication or loss of what was already stored).

## Charcoal kiln

Same radius as the smelter (configured together). Unlike the fireplace, it
has a configurable buffer (default: 3) — it only keeps that much wood in
its internal queue instead of filling it all at once, letting ongoing
production finish before pulling more. It also has a configurable cap on
coal accumulated in nearby chests: past that cap, it stops pulling new
wood (without interrupting what's already processing). The coal it
produces first tries to feed nearby smelters that are low on fuel
(configurable strategy: prioritize the one with the least fuel, or the
nearest one); only the leftover goes to a chest.

## Cooking station (fire spit, cauldron, etc.)

Pulls raw food (and its own fuel, if the station uses one) from nearby
chests and cooks on its own. When an item finishes cooking, it's collected
automatically and stored in the nearest chest — no need to interact with
the station to take the finished food. Automatic collection goes through
the same code path as a manual interaction, so skill XP and yield bonuses
keep working normally (see the note above about who gets the XP).
