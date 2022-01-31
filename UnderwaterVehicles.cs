using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Underwater Vehicles", "WhiteThunder", "1.1.0")]
    [Description("Allows helicopters and modular cars to be used underwater.")]
    internal class UnderwaterVehicles : CovalencePlugin
    {
        private Configuration _pluginConfig;

        #region Hooks

        private void Unload()
        {
            if (_pluginConfig.AllowedVehicles.ModularCar && _pluginConfig.ModularCarSettings.UnderwaterDragMultiplier != 1.0f)
            {
                foreach (var car in BaseNetworkable.serverEntities.OfType<ModularCar>())
                {
                    TeardownCarDragReducer(car);
                }
            }
        }

        private void OnServerInitialized(bool initialBoot)
        {
            var allowedVehicles = _pluginConfig.AllowedVehicles;

            foreach (var entity in BaseNetworkable.serverEntities)
            {
                var heli = entity as MiniCopter;
                if (heli != null)
                {
                    OnEntitySpawned(heli);
                    continue;
                }

                var groundVehicle = entity as GroundVehicle;
                if (groundVehicle != null)
                {
                    OnEntitySpawned(groundVehicle);
                    continue;
                }
            }
        }

        private void OnEntitySpawned(GroundVehicle vehicle)
        {
            var car = vehicle as ModularCar;
            if (car != null)
            {
                if (_pluginConfig.AllowedVehicles.ModularCar)
                {
                    MoveWaterSample(car.waterloggedPoint);

                    if (_pluginConfig.ModularCarSettings.UnderwaterDragMultiplier != 1.0f)
                    {
                        SetupCarDragReducer(car);
                    }
                }
                return;
            }

            var snowmobile = vehicle as Snowmobile;
            if (snowmobile != null)
            {
                if (snowmobile.ShortPrefabName == "snowmobile")
                {
                    if (_pluginConfig.AllowedVehicles.Snowmobile)
                    {
                        LogWarning($"Enabling for snowmobile");
                        MoveWaterSample(snowmobile.waterloggedPoint);
                    }
                }
                else if (snowmobile.ShortPrefabName == "tomahasnowmobile")
                {
                    if (_pluginConfig.AllowedVehicles.TomahaSnowmobile)
                    {
                        MoveWaterSample(snowmobile.waterloggedPoint);
                    }
                }
                return;
            }
        }

        private void OnEntitySpawned(MiniCopter heli)
        {
            if (heli is ScrapTransportHelicopter)
            {
                if (_pluginConfig.AllowedVehicles.ScrapTransportHelicopter)
                {
                    MoveWaterSample(heli.waterSample);
                }
            }
            else if (_pluginConfig.AllowedVehicles.Minicopter)
            {
                MoveWaterSample(heli.waterSample);
            }
        }

        #endregion

        #region Helper Methods

        private void MoveWaterSample(Transform transform)
        {
            if (transform.parent == null)
                return;

            transform.SetParent(null);
            transform.SetPositionAndRotation(Vector3.up * 1000, Quaternion.identity);
        }

        private void SetupCarDragReducer(ModularCar car)
        {
            car.carPhysics.timeSinceWaterCheck = float.MinValue;
            car.gameObject.AddComponent<CarDragReducer>().underwaterDragMultiplier = _pluginConfig.ModularCarSettings.UnderwaterDragMultiplier;
        }

        private void TeardownCarDragReducer(ModularCar car)
        {
            car.carPhysics.timeSinceWaterCheck = 0;
            UnityEngine.Object.Destroy(car.gameObject.GetComponent<CarDragReducer>());
        }

        #endregion

        #region Helper Classes

        internal class CarDragReducer : MonoBehaviour
        {
            public float underwaterDragMultiplier = 1f;

            private TimeSince timeSinceWaterCheck = default(TimeSince);
            private ModularCar car;

            private void Awake()
            {
                car = GetComponent<ModularCar>();
            }

            private void FixedUpdate()
            {
                // Most of this code is identical to the vanilla drag computation
                if (timeSinceWaterCheck > 0.25f)
                {
                    float throttleInput = car.IsOn() ? car.GetThrottleInput() : 0;
                    float waterFactor = car.WaterFactor() * underwaterDragMultiplier;
                    float drag = 0f;
                    TriggerVehicleDrag triggerResult;
                    if (car.FindTrigger(out triggerResult))
                    {
                        drag = triggerResult.vehicleDrag;
                    }
                    float throttleDrag = (throttleInput != 0) ? 0 : 0.25f;
                    drag = Mathf.Max(waterFactor, drag);
                    drag = Mathf.Max(drag, car.GetModifiedDrag());
                    car.rigidBody.drag = Mathf.Max(throttleDrag, drag);
                    car.rigidBody.angularDrag = drag * 0.5f;
                    timeSinceWaterCheck = 0;
                }
            }
        }

        #endregion

        #region Configuration

        private Configuration GetDefaultConfig() => new Configuration();

        private class Configuration : SerializableConfiguration
        {
            [JsonProperty("AllowedVehicles")]
            public AllowedVehiclesConfig AllowedVehicles = new AllowedVehiclesConfig();

            [JsonProperty("ModularCarSettings")]
            public ModularCarSettings ModularCarSettings = new ModularCarSettings();
        }

        private class AllowedVehiclesConfig
        {
            [JsonProperty("Minicopter")]
            public bool Minicopter = false;

            [JsonProperty("ModularCar")]
            public bool ModularCar = false;

            [JsonProperty("ScrapTransportHelicopter")]
            public bool ScrapTransportHelicopter = false;

            [JsonProperty("Snowmobile")]
            public bool Snowmobile = false;

            [JsonProperty("TomahaSnowmobile")]
            public bool TomahaSnowmobile = false;
        }

        private class ModularCarSettings
        {
            [JsonProperty("UnderwaterDragMultiplier")]
            public float UnderwaterDragMultiplier = 1.0f;
        }

        #endregion

        #region Configuration Boilerplate

        private class SerializableConfiguration
        {
            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonHelper.Deserialize(ToJson()) as Dictionary<string, object>;
        }

        private static class JsonHelper
        {
            public static object Deserialize(string json) => ToObject(JToken.Parse(json));

            private static object ToObject(JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        return token.Children<JProperty>()
                                    .ToDictionary(prop => prop.Name,
                                                  prop => ToObject(prop.Value));

                    case JTokenType.Array:
                        return token.Select(ToObject).ToList();

                    default:
                        return ((JValue)token).Value;
                }
            }
        }

        private bool MaybeUpdateConfig(SerializableConfiguration config)
        {
            var currentWithDefaults = config.ToDictionary();
            var currentRaw = Config.ToDictionary(x => x.Key, x => x.Value);
            return MaybeUpdateConfigDict(currentWithDefaults, currentRaw);
        }

        private bool MaybeUpdateConfigDict(Dictionary<string, object> currentWithDefaults, Dictionary<string, object> currentRaw)
        {
            bool changed = false;

            foreach (var key in currentWithDefaults.Keys)
            {
                object currentRawValue;
                if (currentRaw.TryGetValue(key, out currentRawValue))
                {
                    var defaultDictValue = currentWithDefaults[key] as Dictionary<string, object>;
                    var currentDictValue = currentRawValue as Dictionary<string, object>;

                    if (defaultDictValue != null)
                    {
                        if (currentDictValue == null)
                        {
                            currentRaw[key] = currentWithDefaults[key];
                            changed = true;
                        }
                        else if (MaybeUpdateConfigDict(defaultDictValue, currentDictValue))
                            changed = true;
                    }
                }
                else
                {
                    currentRaw[key] = currentWithDefaults[key];
                    changed = true;
                }
            }

            return changed;
        }

        protected override void LoadDefaultConfig() => _pluginConfig = GetDefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _pluginConfig = Config.ReadObject<Configuration>();
                if (_pluginConfig == null)
                {
                    throw new JsonException();
                }

                if (MaybeUpdateConfig(_pluginConfig))
                {
                    LogWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch (Exception e)
            {
                LogError(e.Message);
                LogWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Log($"Configuration changes saved to {Name}.json");
            Config.WriteObject(_pluginConfig, true);
        }

        #endregion
    }
}
