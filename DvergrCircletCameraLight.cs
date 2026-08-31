using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Valheim_Build_Camera;

internal static class DvergrCircletCameraLight
{
	private const float EquipmentRefreshInterval = 0.25f;
	private const float LightSettingsRefreshInterval = 0.1f;
	private const string OriginalColor = "Original";

	private sealed class CustomSlotAdapter
	{
		private readonly MethodInfo? _getEquippedCirclet;
		private readonly MethodInfo? _getCircletVisuals;
		private readonly FieldInfo? _circletObject;

		internal CustomSlotAdapter(string modNamespace)
		{
			const BindingFlags staticMethod = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
			Type? humanoidExtensions = FindLoadedType(modNamespace + ".HumanoidExtension");
			Type? visualExtensions = FindLoadedType(modNamespace + ".VisEquipmentExtension");
			Type? visualData = FindLoadedType(modNamespace + ".VisEquipmentCirclet");
			_getEquippedCirclet = humanoidExtensions?.GetMethod("GetCirclet", staticMethod, null, new[] { typeof(Humanoid) }, null);
			_getCircletVisuals = visualExtensions?.GetMethod("GetCircletData", staticMethod, null, new[] { typeof(VisEquipment) }, null);
			_circletObject = visualData?.GetField("m_circletItemInstance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		}

		internal bool TryGetCirclet(Player player, VisEquipment visualEquipment, out ItemDrop.ItemData? item, out GameObject? circletObject)
		{
			item = null;
			circletObject = null;
			if (_getEquippedCirclet == null || _getCircletVisuals == null || _circletObject == null) return false;

			try
			{
				SingleArgumentBuffer[0] = player;
				item = _getEquippedCirclet.Invoke(null, SingleArgumentBuffer) as ItemDrop.ItemData;
				SingleArgumentBuffer[0] = visualEquipment;
				object? visualData = _getCircletVisuals.Invoke(null, SingleArgumentBuffer);
				circletObject = visualData == null ? null : _circletObject.GetValue(visualData) as GameObject;
				return item != null && circletObject;
			}
			catch
			{
				return false;
			}
			finally
			{
				SingleArgumentBuffer[0] = null;
			}
		}

		private static Type? FindLoadedType(string fullName)
		{
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				Type? type = assembly.GetType(fullName, false);
				if (type != null) return type;
			}

			return null;
		}
	}

	private sealed class CameraLightCopy
	{
		internal readonly Light SourceLight;
		internal readonly Light CopiedLight;
		internal readonly GameObject CopiedObject;
		internal int SourceCullingMask;

		internal CameraLightCopy(Light sourceLight, Light copiedLight, GameObject copiedObject)
		{
			SourceLight = sourceLight;
			CopiedLight = copiedLight;
			CopiedObject = copiedObject;
			SourceCullingMask = sourceLight.cullingMask;
		}
	}

	private static readonly Dictionary<int, CameraLightCopy> CameraLightCopies = new();
	private static readonly HashSet<int> EquippedLightIds = new();
	private static readonly List<int> LightsToRemove = new();
	private static readonly List<Light> CircletLights = new();
	private static readonly object?[] SingleArgumentBuffer = new object?[1];
	private static readonly CustomSlotAdapter[] CustomSlotAdapters =
	{
		new("CircletExtended"),
		new("RaziCirclet"),
	};

	private static float _nextEquipmentRefresh;
	private static float _nextLightSettingsRefresh;
	private static float _nextWarningTime;
	private static string _cachedColorSetting = string.Empty;
	private static bool _cachedColorOverride;
	private static Color _cachedColor;

	internal static void Update(GameCamera camera)
	{
		try
		{
			UpdateLights(camera);
		}
		catch (Exception exception)
		{
			if (Time.time >= _nextWarningTime)
			{
				_nextWarningTime = Time.time + 2f;
				Valheim_Build_CameraPlugin.BuildCameraCHELogger.LogWarning($"Dvergr camera light failed: {exception.Message}");
			}

			Cleanup();
		}
	}

	internal static void Cleanup()
	{
		foreach (CameraLightCopy lightCopy in CameraLightCopies.Values)
			RestoreSourceLight(lightCopy);

		CameraLightCopies.Clear();
		EquippedLightIds.Clear();
		LightsToRemove.Clear();
		CircletLights.Clear();
		SingleArgumentBuffer[0] = null;
		_nextEquipmentRefresh = 0f;
		_nextLightSettingsRefresh = 0f;
		_nextWarningTime = 0f;
	}

	private static void UpdateLights(GameCamera camera)
	{
		Player player = Player.m_localPlayer;
		if (!camera || !player || Valheim_Build_CameraPlugin.followDvergrCircletLight.Value != Valheim_Build_CameraPlugin.Toggle.On)
		{
			if (CameraLightCopies.Count != 0) Cleanup();
			return;
		}

		float now = Time.time;
		if (now >= _nextEquipmentRefresh)
		{
			_nextEquipmentRefresh = now + EquipmentRefreshInterval;
			RefreshEquippedLights(player);
		}
		if (CameraLightCopies.Count == 0) return;

		bool refreshSettings = now >= _nextLightSettingsRefresh;
		if (refreshSettings) _nextLightSettingsRefresh = now + LightSettingsRefreshInterval;
		Transform cameraTransform = camera.transform;
		Vector3 cameraOffset = cameraTransform.up * Valheim_Build_CameraPlugin.helmetLightOffsetUp.Value;
		cameraOffset += cameraTransform.forward * Valheim_Build_CameraPlugin.helmetLightOffsetForward.Value;
		cameraOffset += cameraTransform.right * Valheim_Build_CameraPlugin.helmetLightOffsetRight.Value;
		Vector3 lightPosition = cameraTransform.position + cameraOffset;
		Quaternion lightRotation = cameraTransform.rotation;
		foreach (CameraLightCopy lightCopy in CameraLightCopies.Values)
		{
			if (!lightCopy.SourceLight)
			{
				_nextEquipmentRefresh = 0f;
				continue;
			}

			MoveLightToCamera(lightCopy, lightPosition, lightRotation, refreshSettings);
		}
	}

	private static void RefreshEquippedLights(Player player)
	{
		EquippedLightIds.Clear();
		VisEquipment visualEquipment = player.m_visEquipment;
		if (visualEquipment)
		{
			if (IsDvergrCirclet(player.m_helmetItem) && visualEquipment.m_helmetItemInstance)
				AddCircletLights(visualEquipment.m_helmetItemInstance);

			foreach (CustomSlotAdapter customSlot in CustomSlotAdapters)
			{
				if (customSlot.TryGetCirclet(player, visualEquipment, out ItemDrop.ItemData? item, out GameObject? circletObject) && IsDvergrCirclet(item) && circletObject)
					AddCircletLights(circletObject);
			}
		}

		RemoveUnequippedLights();
	}

	private static bool IsDvergrCirclet(ItemDrop.ItemData? item)
	{
		if (item == null) return false;
		string prefabName = item.m_dropPrefab ? item.m_dropPrefab.name : string.Empty;
		return prefabName.StartsWith("HelmetDverger", StringComparison.OrdinalIgnoreCase) || item.m_shared.m_name.IndexOf("helmet_dverger", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static void AddCircletLights(GameObject circletObject)
	{
		CircletLights.Clear();
		circletObject.GetComponentsInChildren(true, CircletLights);
		foreach (Light sourceLight in CircletLights)
		{
			if (!sourceLight) continue;
			int sourceLightId = sourceLight.GetInstanceID();
			if (!EquippedLightIds.Add(sourceLightId) || CameraLightCopies.ContainsKey(sourceLightId)) continue;
			CameraLightCopies.Add(sourceLightId, CreateCameraLightCopy(sourceLight));
		}
	}

	private static void RemoveUnequippedLights()
	{
		LightsToRemove.Clear();
		foreach (KeyValuePair<int, CameraLightCopy> pair in CameraLightCopies)
		{
			if (!pair.Value.SourceLight || !EquippedLightIds.Contains(pair.Key)) LightsToRemove.Add(pair.Key);
		}

		foreach (int sourceLightId in LightsToRemove)
		{
			RestoreSourceLight(CameraLightCopies[sourceLightId]);
			CameraLightCopies.Remove(sourceLightId);
		}
	}

	private static CameraLightCopy CreateCameraLightCopy(Light sourceLight)
	{
		GameObject copiedObject = new("BuildCamera_DvergrLight_" + sourceLight.name) { hideFlags = HideFlags.HideAndDontSave };
		CameraLightCopy lightCopy = new(sourceLight, copiedObject.AddComponent<Light>(), copiedObject);
		SyncLightSettings(lightCopy);
		sourceLight.cullingMask = 0;
		return lightCopy;
	}

	private static void MoveLightToCamera(CameraLightCopy lightCopy, Vector3 position, Quaternion rotation, bool refreshSettings)
	{
		Light sourceLight = lightCopy.SourceLight;
		if (sourceLight.cullingMask != 0) lightCopy.SourceCullingMask = sourceLight.cullingMask;
		if (refreshSettings) SyncLightSettings(lightCopy);
		sourceLight.cullingMask = 0;

		Transform lightTransform = lightCopy.CopiedObject.transform;
		lightTransform.position = position;
		lightTransform.rotation = rotation;
		lightCopy.CopiedLight.enabled = sourceLight.enabled;

		bool shouldBeActive = sourceLight.gameObject.activeInHierarchy;
		if (lightCopy.CopiedObject.activeSelf != shouldBeActive) lightCopy.CopiedObject.SetActive(shouldBeActive);
	}

	private static void SyncLightSettings(CameraLightCopy lightCopy)
	{
		Light sourceLight = lightCopy.SourceLight;
		Light copiedLight = lightCopy.CopiedLight;
		bool hasColorOverride = TryGetConfiguredColor(out Color configuredColor);
		copiedLight.type = sourceLight.type;
		copiedLight.color = hasColorOverride ? configuredColor : sourceLight.color;
		copiedLight.colorTemperature = sourceLight.colorTemperature;
		copiedLight.useColorTemperature = sourceLight.useColorTemperature && !hasColorOverride;
		copiedLight.intensity = sourceLight.intensity * Valheim_Build_CameraPlugin.helmetLightIntensityMultiplier.Value;
		copiedLight.range = sourceLight.range * Valheim_Build_CameraPlugin.helmetLightRangeMultiplier.Value;
		copiedLight.spotAngle = sourceLight.spotAngle;
		copiedLight.cookie = sourceLight.cookie;
		copiedLight.cookieSize = sourceLight.cookieSize;
		copiedLight.shadows = GetConfiguredShadows(sourceLight.shadows);
		copiedLight.shadowStrength = sourceLight.shadowStrength;
		copiedLight.shadowResolution = sourceLight.shadowResolution;
		copiedLight.renderMode = sourceLight.renderMode;
		copiedLight.cullingMask = lightCopy.SourceCullingMask;
	}

	private static bool TryGetConfiguredColor(out Color color)
	{
		string setting = Valheim_Build_CameraPlugin.helmetLightColor.Value ?? string.Empty;
		if (setting != _cachedColorSetting) CacheConfiguredColor(setting);
		color = _cachedColor;
		return _cachedColorOverride;
	}

	private static void CacheConfiguredColor(string setting)
	{
		_cachedColorSetting = setting;
		string color = setting.Trim();
		if (string.IsNullOrEmpty(color) || color.Equals(OriginalColor, StringComparison.OrdinalIgnoreCase))
		{
			_cachedColorOverride = false;
			return;
		}

		if (!color.StartsWith("#", StringComparison.Ordinal)) color = "#" + color;
		_cachedColorOverride = ColorUtility.TryParseHtmlString(color, out _cachedColor);
	}

	private static LightShadows GetConfiguredShadows(LightShadows sourceShadows)
	{
		return Valheim_Build_CameraPlugin.helmetLightShadows.Value switch
		{
			Valheim_Build_CameraPlugin.CameraLightShadows.Off => LightShadows.None,
			Valheim_Build_CameraPlugin.CameraLightShadows.Hard => LightShadows.Hard,
			Valheim_Build_CameraPlugin.CameraLightShadows.Soft => LightShadows.Soft,
			_ => sourceShadows,
		};
	}

	private static void RestoreSourceLight(CameraLightCopy lightCopy)
	{
		if (lightCopy.SourceLight) lightCopy.SourceLight.cullingMask = lightCopy.SourceCullingMask;
		if (lightCopy.CopiedObject) UnityEngine.Object.Destroy(lightCopy.CopiedObject);
	}
}
