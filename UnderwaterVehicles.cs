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

        private const string SnowmobileShortPrefabName = "snowmobile";
        private const string TomahaShortPrefabName = "tomahasnowmobile";

        #endregion

        #region Hooks

        private void Init()
        {
            _pluginInstance = this;

            if (!_pluginConfig.IsAnyDragMultiplerEnabled())
            {
                Unsubscribe(nameof(OnEntityMounted));
                Unsubscribe(nameof(OnEntityDismounted));
            }
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
                if (_pluginConfig.ModularCar.Enabled)
                {
                    UnderwaterVehicleComponent.AddToVehicle(car, _pluginConfig.ModularCar.DragMultiplier);
                }
                return;
            }

            var snowmobile = vehicle as Snowmobile;
            if (snowmobile != null)
            {
                if (snowmobile.ShortPrefabName == SnowmobileShortPrefabName)
                {
                    if (_pluginConfig.Snowmobile.Enabled)
                    {
                        UnderwaterVehicleComponent.AddToVehicle(snowmobile, _pluginConfig.Snowmobile.DragMultiplier);
                    }
                }
                else if (snowmobile.ShortPrefabName == TomahaShortPrefabName)
                {
                    if (_pluginConfig.Tomaha.Enabled)
                    {
                        UnderwaterVehicleComponent.AddToVehicle(snowmobile, _pluginConfig.Tomaha.DragMultiplier);
                    }
                }
                return;
            }
        }

        private void OnEntitySpawned(MiniCopter heli)
        {
            if (heli is ScrapTransportHelicopter)
            {
                if (_pluginConfig.ScrapTransportHelicopter.Enabled)
                {
                    UnderwaterVehicleComponent.AddToVehicle(heli);
                }
            }
            else if (_pluginConfig.Minicopter.Enabled)
            {
                UnderwaterVehicleComponent.AddToVehicle(heli);
            }
        }

        private void OnEntityMounted(BaseVehicleSeat seat)
        {
            var groundVehicle = GetParentVehicle(seat) as GroundVehicle;
            if (groundVehicle == null
                || groundVehicle.NumMounted() > 1
                || _pluginConfig.GetDragMultiplier(groundVehicle) == 1)
            {
                return;
            }

            var component = UnderwaterVehicleComponent.GetForVehicle(groundVehicle);
            if (component == null)
            {
                return;
            }

            component.EnableCustomDrag();
        }

        private void OnEntityDismounted(BaseVehicleSeat seat)
        {
            var groundVehicle = GetParentVehicle(seat) as GroundVehicle;
            if (groundVehicle == null
                || groundVehicle.NumMounted() > 0
                || _pluginConfig.GetDragMultiplier(groundVehicle) == 1)
            {
                return;
            }

            var component = UnderwaterVehicleComponent.GetForVehicle(groundVehicle);
            if (component == null)
            {
                return;
            }

            component.DisableCustomDrag();
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

        private static BaseVehicle GetParentVehicle(BaseEntity entity)
        {
            var parent = entity.GetParentEntity();
            if (parent == null)
            {
                return null;
            }

            var vehicleModule = parent as BaseVehicleModule;
            if (vehicleModule != null)
            {
                return vehicleModule.Vehicle;
            }

            return parent as BaseVehicle;
        }

        #endregion

        #region Helper Classes

        private class UnderwaterVehicleComponent : FacepunchBehaviour
        {
            public static void AddToVehicle(BaseVehicle vehicle, float dragMultiplier = 1) =>
                vehicle.gameObject.AddComponent<UnderwaterVehicleComponent>().Init(dragMultiplier);

            public static UnderwaterVehicleComponent GetForVehicle(BaseVehicle vehicle) =>
                vehicle.gameObject.GetComponent<UnderwaterVehicleComponent>();

            public static void RemoveFromVehicle(BaseVehicle vehicle) =>
                DestroyImmediate(GetForVehicle(vehicle));

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

                if (_groundVehicle.AnyMounted())
                {
                    EnableCustomDrag();
                }
            }

            public void EnableCustomDrag()
            {
                SetTimeSinceWaterCheck(_groundVehicle, float.MinValue);
                InvokeRandomized(CustomDragCheck, 0.25f, 0.25f, 0.05f);
            }

            public void DisableCustomDrag()
            {
                SetTimeSinceWaterCheck(_groundVehicle, UnityEngine.Random.Range(0f, 0.25f));
                CancelInvoke(CustomDragCheck);
            }

            private void CustomDragCheck()
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
                    DisableCustomDrag();
                }
            }
        }

        #endregion

        #region Configuration

        private Configuration GetDefaultConfig() => new Configuration();

        private class VehicleConfig
        {
            [JsonProperty("Enabled", Order = -3)]
            public bool Enabled;
        }

        private class GroundVehicleConfig : VehicleConfig
        {
            [JsonProperty("DragMultiplier", Order = -2)]
            public float DragMultiplier = 1;
        }

        private class DeprecatedAllowedVehiclesConfig
        {
            [JsonProperty("Minicopter")]
            public bool Minicopter;

            [JsonProperty("ModularCar")]
            public bool ModularCar;

            [JsonProperty("ScrapTransportHelicopter")]
            public bool ScrapTransportHelicopter;

            [JsonProperty("Snowmobile")]
            public bool Snowmobile;

            [JsonProperty("TomahaSnowmobile")]
            public bool TomahaSnowmobile;
        }

        private class DeprecatedModularCarSettings
        {
            [JsonProperty("UnderwaterDragMultiplier")]
            public float UnderwaterDragMultiplier = 1;
        }

        private class Configuration : SerializableConfiguration
        {
            [JsonProperty("ModularCar")]
            public GroundVehicleConfig ModularCar = new GroundVehicleConfig();

            [JsonProperty("Snowmobile")]
            public GroundVehicleConfig Snowmobile = new GroundVehicleConfig();

            [JsonProperty("Tomaha")]
            public GroundVehicleConfig Tomaha = new GroundVehicleConfig();

            [JsonProperty("Minicopter")]
            public VehicleConfig Minicopter = new VehicleConfig();

            [JsonProperty("ScrapTransportHelicopter")]
            public VehicleConfig ScrapTransportHelicopter = new VehicleConfig();

            [JsonProperty("AllowedVehicles")]
            public DeprecatedAllowedVehiclesConfig AllowedVehicles
            {
                set
                {
                    ModularCar.Enabled = value.ModularCar;
                    Minicopter.Enabled = value.Minicopter;
                    ScrapTransportHelicopter.Enabled = value.ScrapTransportHelicopter;
                }
            }

            [JsonProperty("ModularCarSettings")]
            public DeprecatedModularCarSettings ModularCarSettings
            { set { ModularCar.DragMultiplier = value.UnderwaterDragMultiplier; } }

            public bool IsAnyDragMultiplerEnabled()
            {
                return ModularCar.Enabled && ModularCar.DragMultiplier != 1
                    || Snowmobile.Enabled && Snowmobile.DragMultiplier != 1
                    || Tomaha.Enabled && Tomaha.DragMultiplier != 1;
            }

            public float GetDragMultiplier(GroundVehicle groundVehicle)
            {
                if (groundVehicle is ModularCar)
                {
                    return ModularCar.DragMultiplier;
                }

                if (groundVehicle is Snowmobile)
                {
                    if (groundVehicle.ShortPrefabName == SnowmobileShortPrefabName)
                    {
                        return Snowmobile.DragMultiplier;
                    }
                    else if (groundVehicle.ShortPrefabName == TomahaShortPrefabName)
                    {
                        return Tomaha.DragMultiplier;
                    }
                }

                return 1;
            }
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
