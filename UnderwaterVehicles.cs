using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Underwater Vehicles", "WhiteThunder", "1.2.0")]
    [Description("Allows modular cars, snowmobiles and helicopters to be used underwater.")]
    internal class UnderwaterVehicles : CovalencePlugin
    {
        #region Fields

        private static UnderwaterVehicles _pluginInstance;
        private Configuration _pluginConfig;

        #endregion

        #region Hooks

        private void Init()
        {
            _pluginInstance = this;
        }

        private void Unload()
        {
            foreach (var entity in BaseNetworkable.serverEntities)
            {
                var heli = entity as MiniCopter;
                if (heli != null)
                {
                    UnderwaterVehicleComponent.RemoveFromVehicle(heli);
                    continue;
                }

                var groundVehicle = entity as GroundVehicle;
                if (groundVehicle != null)
                {
                    UnderwaterVehicleComponent.RemoveFromVehicle(groundVehicle);
                    continue;
                }
            }

            _pluginInstance = null;
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
                    UnderwaterVehicleComponent.AddToVehicle(car, _pluginConfig.DragSettings.ModularCar);
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
                        UnderwaterVehicleComponent.AddToVehicle(snowmobile, _pluginConfig.DragSettings.Snowmobile);
                    }
                }
                else if (snowmobile.ShortPrefabName == "tomahasnowmobile")
                {
                    if (_pluginConfig.AllowedVehicles.TomahaSnowmobile)
                    {
                        UnderwaterVehicleComponent.AddToVehicle(snowmobile, _pluginConfig.DragSettings.TomahaSnowmobile);
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
                    UnderwaterVehicleComponent.AddToVehicle(heli);
                }
            }
            else if (_pluginConfig.AllowedVehicles.Minicopter)
            {
                UnderwaterVehicleComponent.AddToVehicle(heli);
            }
        }

        #endregion

        #region Helper Methods

        private static void SetTimeSinceWaterCheck(GroundVehicle groundVehicle, float value)
        {
            var snowmobile = groundVehicle as Snowmobile;
            if (snowmobile != null)
            {
                snowmobile.carPhysics.timeSinceWaterCheck = value;
                return;
            }

            var car = groundVehicle as ModularCar;
            if (car != null)
            {
                car.carPhysics.timeSinceWaterCheck = value;
                return;
            }
        }

        #endregion

        #region Helper Classes

        private class UnderwaterVehicleComponent : FacepunchBehaviour
        {
            public static void AddToVehicle(BaseVehicle vehicle, float dragMultiplier = 1) =>
                vehicle.gameObject.AddComponent<UnderwaterVehicleComponent>().Init(dragMultiplier);

            public static void RemoveFromVehicle(BaseVehicle vehicle) =>
                DestroyImmediate(vehicle.gameObject.GetComponent<UnderwaterVehicleComponent>());

            private BaseVehicle _vehicle;
            private Transform _waterLoggedPoint;
            private Vector3 _waterLoggedPointLocalPosition;

            private GroundVehicle _groundVehicle;
            private float _dragMultiplier = 1;

            private void Awake()
            {
                _vehicle = GetComponent<BaseVehicle>();
                _groundVehicle = _vehicle as GroundVehicle;

                var groundVehicle = _vehicle as GroundVehicle;
                if (groundVehicle != null)
                {
                    _waterLoggedPoint = groundVehicle.waterloggedPoint;
                }

                var heli = _vehicle as MiniCopter;
                if (heli != null)
                {
                    _waterLoggedPoint = heli.waterSample;
                }

                if (_waterLoggedPoint != null && _waterLoggedPoint.parent != null)
                {
                    _waterLoggedPointLocalPosition = _waterLoggedPoint.localPosition;
                    _waterLoggedPoint.SetParent(null);
                    _waterLoggedPoint.position = new Vector3(0, 1000, 0);
                }
            }

            public void Init(float dragMultiplier)
            {
                if (dragMultiplier == 1 || _groundVehicle == null)
                    return;

                _dragMultiplier = dragMultiplier;

                SetTimeSinceWaterCheck(_groundVehicle, float.MinValue);
                InvokeRandomized(CheckWater, 0.25f, 0.25f, 0.05f);
            }

            private void CheckWater()
            {
                _pluginInstance?.TrackStart();

                if (!_groundVehicle.rigidBody.IsSleeping())
                {
                    // Most of this code is identical to the vanilla drag computation
                    float throttleInput = _groundVehicle.IsOn() ? _groundVehicle.GetThrottleInput() : 0;
                    float waterFactor = _groundVehicle.WaterFactor() * _dragMultiplier;
                    float drag = 0f;
                    TriggerVehicleDrag triggerResult;
                    if (_groundVehicle.FindTrigger(out triggerResult))
                    {
                        drag = triggerResult.vehicleDrag;
                    }
                    float throttleDrag = (throttleInput != 0) ? 0 : 0.25f;
                    drag = Mathf.Max(waterFactor, drag);
                    drag = Mathf.Max(drag, _groundVehicle.GetModifiedDrag());
                    _groundVehicle.rigidBody.drag = Mathf.Max(throttleDrag, drag);
                    _groundVehicle.rigidBody.angularDrag = drag * 0.5f;
                }

                _pluginInstance?.TrackEnd();
            }

            private void OnDestroy()
            {
                if (_waterLoggedPoint != null)
                {
                    if (_vehicle != null && !_vehicle.IsDestroyed)
                    {
                        _waterLoggedPoint.SetParent(_vehicle.transform);
                        _waterLoggedPoint.transform.localPosition = _waterLoggedPointLocalPosition;
                    }
                    else
                    {
                        Destroy(_waterLoggedPoint.gameObject);
                    }
                }

                if (_dragMultiplier != 1 && _groundVehicle != null && !_groundVehicle.IsDestroyed)
                {
                    SetTimeSinceWaterCheck(_groundVehicle, UnityEngine.Random.Range(0f, 0.25f));
                }
            }
        }

        #endregion

        #region Configuration

        private Configuration GetDefaultConfig() => new Configuration();

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

        private class DragMultipliers
        {
            [JsonProperty("ModularCar")]
            public float ModularCar = 1;

            [JsonProperty("Snowmobile")]
            public float Snowmobile = 1;

            [JsonProperty("TomahaSnowmobile")]
            public float TomahaSnowmobile = 1;
        }

        private class ModularCarSettings
        {
            [JsonProperty("UnderwaterDragMultiplier")]
            public float UnderwaterDragMultiplier = 1;
        }

        private class Configuration : SerializableConfiguration
        {
            [JsonProperty("AllowedVehicles")]
            public AllowedVehiclesConfig AllowedVehicles = new AllowedVehiclesConfig();

            [JsonProperty("UnderwaterDragMultiplier")]
            public DragMultipliers DragSettings = new DragMultipliers();

            // Deprecated
            [JsonProperty("ModularCarSettings")]
            public ModularCarSettings ModularCarSettings
            { set { DragSettings.ModularCar = value.UnderwaterDragMultiplier; } }
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
