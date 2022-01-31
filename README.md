## Features

- Allows vehicles to be used underwater
- Supports modular cars, snowmobiles, minicopters and scrap transport helicopters

## Configuration

Default configuration:

```json
{
  "AllowedVehicles": {
    "Minicopter": false,
    "ModularCar": false,
    "ScrapTransportHelicopter": false,
    "Snowmobile": false,
    "TomahaSnowmobile": false
  },
  "UnderwaterDragMultiplier": {
    "ModularCar": 1.0,
    "Snowmobile": 1.0,
    "TomahaSnowmobile": 1.0
  }
}
```

- `AllowedVehicles` -- Determines whether each type of vehicle can be driven or flown underwater.
- `UnderwaterDragMultiplier` -- Determines how much drag vehicles receive for being in water. Set to `1.0` for vanilla drag, `0.0` for no drag. Set somewhere between `0.25` and `0.5` to maintain some balance.
  - Note: Setting this to non-`0` does take a minor performance cost, which gets counted in the plugin's total hook time so that you can monitor it.
