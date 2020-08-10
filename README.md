**Underwater Vehicles** allows modular cars, mincopters and scrap transport helicopters to be driven or flown underwater.

## Configuration

To enable a particular type of vehicle to be driven or flown underwater, simply change the corresponding configuration value to `true` and reload the plugin.

```json
{
  "AllowedVehicles": {
    "Minicopter": false,
    "ModularCar": false,
    "ScrapTransportHelicopter": false
  }
}
```

Note: If you unload this plugin for any reason, vehicles that were already spawned will still have underwater capability until the next server restart.
