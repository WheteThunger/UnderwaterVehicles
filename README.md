## Features

- Allows vehicles to be used underwater, optionally requiring permission
- Supports modular cars, snowmobiles, magnet cranes, minicopters and scrap transport helicopters
- Allows adjusting underwater drag for ground vehicles

## Permissions

Using the plugin's default configuration, permissions have no effect. However, if you configure a given vehicle type to require occupant permission, then vehicles of that type will only be usable underwater while one of the vehicle's occupants (mounted players) has the corresponding permission.

- `underwatervehicles.occupant.modularcar`
- `underwatervehicles.occupant.snowmobile`
- `underwatervehicles.occupant.tomaha`
- `underwatervehicles.occupant.magnetcrane`
- `underwatervehicles.occupant.minicopter`
- `underwatervehicles.occupant.scraptransport`

## Configuration

Default configuration:

```json
{
  "ModularCar": {
    "Enabled": false,
    "RequireOccupantPermission": false,
    "DragMultiplier": 1.0
  },
  "Snowmobile": {
    "Enabled": false,
    "RequireOccupantPermission": false,
    "DragMultiplier": 1.0
  },
  "Tomaha": {
    "Enabled": false,
    "RequireOccupantPermission": false,
    "DragMultiplier": 1.0
  },
  "MagnetCrane": {
    "Enabled": false,
    "RequireOccupantPermission": false
  },
  "Minicopter": {
    "Enabled": false,
    "RequireOccupantPermission": false
  },
  "ScrapTransportHelicopter": {
    "Enabled": false,
    "RequireOccupantPermission": false
  }
}
```

- `Enabled` (`true` or `false`) -- Determines whether vehicles of this type can be used underwater.
- `RequireOccupantPermission` (`true` or `false`) -- Determines whether vehicles of this type require that the at least one occupant (mounted player) to have permission for the vehicle to be usable underwater.
- `DragMultiplier` -- Determines how much drag ground vehicles receive for being in water. Set to `1.0` for vanilla drag, `0.0` for no drag. Set somewhere between `0.25` and `0.5` to maintain some balance.
  - Note: Setting this to non-`0` does take a minor performance cost, which gets counted in the plugin's total hook time so that you can monitor it.

## Developer Hooks

#### OnVehicleUnderwaterEnable

```cs
object OnVehicleUnderwaterEnable(BaseVehicle vehicle)
```

- Called when this plugin is about to make a vehicle underwater capable
- Returning `false` will prevent the vehicle from becoming underwater capable
- Returning `null` will result in the default behavior
