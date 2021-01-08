**Underwater Vehicles** allows modular cars, mincopters and scrap transport helicopters to be driven or flown underwater.

## Configuration

Default configuration:

```json
{
  "AllowedVehicles": {
    "Minicopter": false,
    "ModularCar": false,
    "ScrapTransportHelicopter": false
  },
  "ModularCarSettings": {
    "UnderwaterDragMultiplier": 1.0
  }
}
```

- `AllowedVehicles` -- To enable a particular type of vehicle to be driven or flown underwater, simply change the corresponding option to `true` (and reload the plugin). If you set any option back to `false`, it won't take effect until the next server restart.
- `ModularCarSettings`
  - `UnderwaterDragMultiplier` -- This value controls how much drag cars receive for being in water. Set to `1.0` for vanilla drag, `0.0` for no drag. If you want cars to be just a bit slower in water than on land but you think vanilla drag is too much, try a value like `0.25`.

Note: If you unload this plugin for any reason, vehicles that were already spawned will still have underwater capability until the next server restart.
