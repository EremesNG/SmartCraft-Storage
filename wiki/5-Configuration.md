# Configuration

All options live in BepInEx's [Configuration Manager](https://valheim.thunderstore.io/package/Azumatt/Official_BepInEx_ConfigurationManager/),
split into four sections. Every option also has its own description
inside the Configuration Manager itself.

## Radii (storage/restock/crafting-from-chest)

| Option | Default | Description |
|---|---|---|
| `QuickStackRadius` | 20m | Radius in which quick-stack and restock search for chests |
| `CraftingChestRadius` | 20m | Radius in which crafting/building considers items from nearby chests |

## Stations (radii and per-behavior on/off)

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

## Charcoal kiln (kiln-specific tuning)

| Option | Default | Description |
|---|---|---|
| `KilnWoodBuffer` | 3 | Wood level kept in the internal queue (not the kiln's max capacity) |
| `KilnMaxCoalInChest` | 50 | Coal cap in nearby chests before pausing new wood pulls |
| `KilnFeedStrategy` | `LeastFuelFirst` | How to pick which nearby smelter to feed first: `LeastFuelFirst` or `Nearest` |

## Animals (automatic feeding)

| Option | Default | Description |
|---|---|---|
| `AnimalFeederRadius` | 20m | Radius in which hungry tameable animals search nearby chests for food |
| `AnimalAutoFeed` | on | Tameable animals automatically pull compatible food from nearby chests |
