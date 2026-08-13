# Pineapple Blitz Grenade

A custom grenade mod for SPT 4.1 that adds the Pineapple Blitz Grenade (PBG) - a short fuse, high-damage explosive perfect for taking out enemies before they can escape!

## Features

- **Short Fuse**: 1.5 second delay (configurable)
- **High Fragmentation**: 250 fragments for maximum coverage
- **Large Blast Radius**: 20-25m explosion range
- **Heavy Damage**: 50 damage per fragment with 120 penetration
- **Available at Prapor**: Loyalty Level 1, 5000 RUB
- **Counts for grenade quests**: kills with this grenade count towards every quest that
  restricts kills to a grenade whitelist - currently Grenadier, The Art of Explosion and
  Fearless Beast

## Installation

1. Copy the `PineappleBlitz-LumurkFox` folder to your `SPT_Runtime/user/mods/` directory
2. Start the SPT server

## Configuration

Edit `config/config.json` to customize the grenade:

```json
{
  "FuzeTimer": 1.5,          // Fuse delay in seconds
  "Fragmentations": 250,      // Number of fragments
  "ExplosionMinimum": 20,     // Minimum blast radius (meters)
  "ExplosionMaximum": 25,     // Maximum blast radius (meters)
  "HeavyBleedPercent": 0.57,  // Heavy bleed chance (0-1)
  "LightBleedPercent": 0.87,  // Light bleed chance (0-1)
  "Damage": 50,               // Damage per fragment
  "Penetration": 120,         // Armor penetration
  "Price": 5000,              // Price in Roubles at Prapor
  "BlacklistFromBots": true   // Keep the grenade out of generated bot loot
}
```

Config values are re-applied on every server start, so edits take effect on the next boot even
though the item itself is only created once.

## Quest support

Rather than hardcoding quest IDs, the mod scans every quest at load time and adds the grenade to
any kill-condition whose weapon whitelist already contains a hand grenade. This matters because
BSG reuses condition IDs across quests (Grenadier and The Art of Explosion share the condition ID
`5c0d1a47d09282029e2fffb7`), and because Fearless Beast whitelists only the *event* F-1 rather than
the base one. The scan covers all three today and picks up any quest added later, including quests
added by other mods.

## Requirements

- SPT 4.1.x
- .NET 10.0 (included with SPT)

## Building from Source

```
dotnet build -c Release
```

The build resolves the SPT assemblies from `F:\SPT 4.1\SPT_Runtime` by default and copies the
result straight into that server's `user/mods` folder. If your install lives elsewhere:

```
dotnet build -c Release -p:SptServerDir="D:\Your\SPT\SPT_Runtime"
```

Pass `-p:DeployToServer=false` to build without copying it into the server.

## SPT 4.1 notes

Version 2.1.0 is the port to SPT 4.1. The 4.0 build will not load on 4.1:

- Mods now target **.NET 10** (4.0 was .NET 9)
- `AbstractModMetadata` was replaced by the `IModMetadata` interface (`IsBundleMod` is gone,
  `HasPrepatcher` was added)
- `IOnLoad.OnLoad()` became `IOnLoad.OnLoadAsync(CancellationToken)`
- `DatabaseService.GetTables()` was removed in favour of directly injectable tables
  (`TemplateTable`, `TradersTable`) and configs (`PmcConfig`, `ItemConfig`)
- `CustomItemService.CreateItemFromClone` now takes the calling assembly
- `CustomItemService` moved to `SPTarkov.Server.Core.Services.Modding.Custom`
- The server folder was renamed from `SPT/` to `SPT_Runtime/`

Because the 4.1 database models are strongly typed, the whole reflection/`dynamic` layer the 4.0
version relied on has been removed.

## Credits

- **Author**: LumurkFox
- **Original Concept**: Echo55

## License

Mozilla Public License 2.0
