## Features

- Allows vehicles to be used underwater
- Supports modular cars, snowmobiles, magnet cranes, minicopters and scrap transport helicopters
- Allows adjusting underwater drag for ground vehicles

## Configuration

Default configuration:

```json
{
  "ModularCar": {
    "Enabled": false,
    "DragMultiplier": 1.0
  },
  "Snowmobile": {
    "Enabled": false,
    "DragMultiplier": 1.0
  },
  "Tomaha": {
    "Enabled": false,
    "DragMultiplier": 1.0
  },
  "MagnetCrane": {
    "Enabled": false
  },
  "Minicopter": {
    "Enabled": false
  },
  "ScrapTransportHelicopter": {
    "Enabled": false
  }
}
```

- `Enable` (`true` or `false`) -- Determines whether vehicles of each type can be driven or flown underwater.
- `DragMultiplier` -- Determines how much drag vehicles receive for being in water. Set to `1.0` for vanilla drag, `0.0` for no drag. Set somewhere between `0.25` and `0.5` to maintain some balance.
  - Note: Setting this to non-`0` does take a minor performance cost, which gets counted in the plugin's total hook time so that you can monitor it.