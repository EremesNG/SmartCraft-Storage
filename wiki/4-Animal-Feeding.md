# Animal Feeding

Tameable animals (boar, wolf, lox, and any other creature with the game's
own taming component) that are hungry automatically pull compatible food
from a nearby chest — both while taming a wild animal and afterward, to
keep breeding active in a pen.

The mod spawns the actual food item near the animal (it doesn't magically
make the animal "fed"): the animal walks over and eats it normally, with
the same animation as always — only the food's source (a chest instead of
you manually dropping it) is automated.

Each species only ever receives an item it actually accepts (the same list
the game itself already uses to decide what that animal eats) — a chest
holding both berries and wolf meat will never feed a boar the wrong thing.

## Important: it also feeds wild, untamed animals

This automation also feeds wild animals that aren't tamed yet, or any
tameable creature that isn't in alert/combat mode, as long as there's a
chest with compatible food within range — it doesn't distinguish "this one
I want to feed" from "this one's just passing by the chest." This is by
design (it also speeds up taming a wild animal from scratch), but be aware
of it if you have a chest with animal food near an area wild animals pass
through.

There's currently no separate toggle to restrict this to only already-
tamed animals — `AnimalAutoFeed` is all or nothing.
