using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Underwater Vehicles", "WhiteThunder", "1.0.0")]
    [Description("Allows helicopters and modular cars to be used underwater.")]
    internal class UnderwaterVehicles : CovalencePlugin
    {
        private UnderwaterVehiclesConfig PluginConfig;

        private void Init()
        {
            PluginConfig = Config.ReadObject<UnderwaterVehiclesConfig>();
        }

        private void OnServerInitialized()
        {
            var allowedVehicles = PluginConfig.AllowedVehicles;

            if (allowedVehicles.Minicopter)
            {
                foreach (var heli in BaseNetworkable.serverEntities.OfType<MiniCopter>())
                {
                    if (!allowedVehicles.ScrapTransportHelicopter && heli is ScrapTransportHelicopter)
                        continue;

                    MoveWaterSample(heli.waterSample);
                }
            }
            else if (allowedVehicles.ScrapTransportHelicopter)
            {
                foreach (var heli in BaseNetworkable.serverEntities.OfType<ScrapTransportHelicopter>())
                    MoveWaterSample(heli.waterSample);
            }

            if (allowedVehicles.ModularCar)
            {
                foreach (var car in BaseNetworkable.serverEntities.OfType<ModularCar>())
                    MoveWaterSample(car.waterSample);
            }
        }

        void OnEntitySpawned(ModularCar car)
        {
            if (PluginConfig.AllowedVehicles.ModularCar)
                MoveWaterSample(car.waterSample);
        }

        void OnEntitySpawned(MiniCopter heli)
        {
            if (heli is ScrapTransportHelicopter)
            {
                if (PluginConfig.AllowedVehicles.ScrapTransportHelicopter)
                    MoveWaterSample(heli.waterSample);
            }
            else if (PluginConfig.AllowedVehicles.Minicopter)
                MoveWaterSample(heli.waterSample);
        }

        private void MoveWaterSample(Transform transform)
        {
            if (transform.parent == null) return;

            transform.SetParent(null);
            transform.SetPositionAndRotation(Vector3.up * 1000, new Quaternion());
        }

        protected override void LoadDefaultConfig() => Config.WriteObject(GetDefaultConfig(), true);

        private UnderwaterVehiclesConfig GetDefaultConfig() => new UnderwaterVehiclesConfig();

        internal class UnderwaterVehiclesConfig
        {
            [JsonProperty("AllowedVehicles")]
            public AllowedVehiclesConfig AllowedVehicles = new AllowedVehiclesConfig();
        }

        internal class AllowedVehiclesConfig
        {
            [JsonProperty("Minicopter")]
            public bool Minicopter = false;

            [JsonProperty("ModularCar")]
            public bool ModularCar = false;

            [JsonProperty("ScrapTransportHelicopter")]
            public bool ScrapTransportHelicopter = false;
        }
    }
}
