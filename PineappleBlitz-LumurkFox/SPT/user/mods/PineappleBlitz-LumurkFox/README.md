# Pineapple Blitz Grenade

A custom grenade mod for SPT 4.0.x that adds the Pineapple Blitz Grenade (PBG) - a short fuse, high-damage explosive perfect for taking out enemies before they can escape!

## Features

- **Short Fuse**: 1.5 second delay (configurable)
- **High Fragmentation**: 250 fragments for maximum coverage
- **Large Blast Radius**: 20-25m explosion range
- **Heavy Damage**: 50 damage per fragment with 120 penetration
- **Available at Prapor**: Loyalty Level 1, 5000 RUB
- **Grenadier Quest Compatible**: Kills with this grenade count towards the Grenadier quest

## Installation

1. Copy the `PineappleBlitz` folder to your `SPT/user/mods/` directory
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
  "Price": 5000               // Price in Roubles at Prapor
}
```

## Credits

- **Author**: LumurkFox
- **Original Concept**: Echo55

## License

Mozilla Public License 2.0
