using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valheim_Build_Camera
{
	public static class Utils
	{
		private const float ComfortRefreshInterval = 0.2f;
		private static readonly Collider[] NearbyPickupColliders = new Collider[128];
		private static readonly HashSet<int> NearbyPickupItemIds = new();
		private static readonly Dictionary<string, CraftingStation> CameraStationsByName = new();
		private static readonly int TerrainMask = LayerMask.GetMask("terrain");
		private static CraftingStation? _cameraStation;
		private static Vector3 _lastSafeCameraPosition;
		private static bool _hasLastSafeCameraPosition;
		private static Player? _comfortPlayer;
		private static bool _hasRequiredComfort;
		private static float _nextComfortRefresh;
		private static float _nextComfortMessageTime;

		internal static void LogWhenVerbose(string message)
		{
			if (Valheim_Build_CameraPlugin.verboseLogging.Value != Valheim_Build_CameraPlugin.Toggle.On) return;
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, message);
			Valheim_Build_CameraPlugin.BuildCameraCHELogger.LogInfo(message);
		}

		public static bool InBuildMode()
		{
			return Player.m_localPlayer && Valheim_Build_CameraPlugin.BuildCameraActive;
		}

		internal static void DisableBuildMode()
		{
			Valheim_Build_CameraPlugin.BuildCameraActive = false;
			_cameraStation = null;
			CameraStationsByName.Clear();
			_hasLastSafeCameraPosition = false;
			_comfortPlayer = null;
			_hasRequiredComfort = false;
			_nextComfortRefresh = 0f;
			_nextComfortMessageTime = 0f;
			DvergrCircletCameraLight.Cleanup();
			SE_DemisterCameraPatch.Cleanup();
		}

		internal static void EnableBuildMode()
		{
			if (!Player.m_localPlayer || !TryFindCameraStation(Player.m_localPlayer.transform.position, out _cameraStation)) return;
			Valheim_Build_CameraPlugin.BuildCameraActive = true;
			Quaternion rotation = Player.m_localPlayer.m_eye.transform.rotation;
			Valheim_Build_CameraPlugin.CameraPitch = rotation.eulerAngles.x;
			Valheim_Build_CameraPlugin.CameraYaw = rotation.eulerAngles.y;
			if (GameCamera.instance)
			{
				_lastSafeCameraPosition = GameCamera.instance.transform.position;
				_hasLastSafeCameraPosition = true;
			}

			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Entering Build Mode.");
		}

		internal static bool IsLocalPlayer(in Player player)
		{
			return Player.m_localPlayer && player == Player.m_localPlayer;
		}

		private static bool IsTool(in ItemDrop.ItemData itemData)
		{
			return itemData?.m_shared.m_buildPieces;
		}

		internal static bool ToolIsEquipped(in Player player)
		{
			return IsTool(player.m_rightItem);
		}

		internal static bool ShouldDeactivateBuildMode(in Player player)
		{
			return !ToolIsEquipped(player);
		}

		internal static bool BuildStationInRange(in Player player)
		{
			return TryFindCameraStation(player.transform.position, out _);
		}

		private static bool TryFindCameraStation(Vector3 point, out CraftingStation? station)
		{
			station = null;
			float nearestDistanceSquared = float.MaxValue;
			foreach (CraftingStation candidate in CraftingStation.m_allStations)
			{
				if (!candidate) continue;
				float distanceSquared = HorizontalDistanceSquared(candidate.transform.position, point);
				float range = GetCameraStationRange(candidate);
				if (distanceSquared <= range * range && distanceSquared < nearestDistanceSquared)
				{
					station = candidate;
					nearestDistanceSquared = distanceSquared;
				}
			}

			return station;
		}

		internal static CraftingStation? FindCameraStation(string name, Vector3 point)
		{
			if (CameraStationsByName.TryGetValue(name, out CraftingStation cachedStation) && IsCameraStationInRange(cachedStation, name, point))
				return cachedStation;

			CraftingStation? closest = null;
			float nearestDistanceSquared = float.MaxValue;
			foreach (CraftingStation station in CraftingStation.m_allStations)
			{
				if (!station || station.m_name != name) continue;
				float distanceSquared = HorizontalDistanceSquared(station.transform.position, point);
				float range = GetCameraStationRange(station);
				if (distanceSquared < range * range && distanceSquared < nearestDistanceSquared)
				{
					closest = station;
					nearestDistanceSquared = distanceSquared;
				}
			}

			if (closest) CameraStationsByName[name] = closest;
			else CameraStationsByName.Remove(name);
			return closest;
		}

		private static bool IsCameraStationInRange(CraftingStation station, string name, Vector3 point)
		{
			if (!station || station.m_name != name) return false;
			float range = GetCameraStationRange(station);
			return HorizontalDistanceSquared(station.transform.position, point) < range * range;
		}

		private static float GetCameraStationRange(CraftingStation station)
		{
			return Mathf.Max(station.GetStationBuildRange(), Valheim_Build_CameraPlugin.distanceCanBuildFromWorkbench.Value);
		}

		private static float HorizontalDistanceSquared(Vector3 first, Vector3 second)
		{
			float x = first.x - second.x;
			float z = first.z - second.z;
			return x * x + z * z;
		}

		private static Vector3 ClampToStationRange(Vector3 position)
		{
			if (!_cameraStation)
			{
				DisableBuildMode();
				return position;
			}

			Vector3 stationPosition = _cameraStation.transform.position;
			float limit = GetCameraStationRange(_cameraStation) * Valheim_Build_CameraPlugin.cameraRangeMultiplier.Value;
			Vector3 offset = position - stationPosition;
			float distanceSquared = offset.sqrMagnitude;
			return distanceSquared <= limit * limit ? position : stationPosition + offset * (limit / Mathf.Sqrt(distanceSquared));
		}

		private static Vector3 ClampToTerrain(Vector3 currentPosition, Vector3 wantedPosition)
		{
			float clearance = Valheim_Build_CameraPlugin.cameraTerrainClearance.Value;
			Vector3 movement = wantedPosition - currentPosition;
			float distanceSquared = movement.sqrMagnitude;
			if (distanceSquared <= 0.000001f) return currentPosition;

			float distance = Mathf.Sqrt(distanceSquared);
			Vector3 direction = movement / distance;
			if (Physics.SphereCast(currentPosition, clearance, direction, out RaycastHit hit, distance, TerrainMask, QueryTriggerInteraction.Ignore))
				wantedPosition = currentPosition + direction * Mathf.Max(0f, hit.distance - 0.02f);

			if (Physics.CheckSphere(wantedPosition, clearance, TerrainMask, QueryTriggerInteraction.Ignore))
			{
				if (_hasLastSafeCameraPosition && !Physics.CheckSphere(_lastSafeCameraPosition, clearance, TerrainMask, QueryTriggerInteraction.Ignore))
					return _lastSafeCameraPosition;
				return currentPosition;
			}

			_lastSafeCameraPosition = wantedPosition;
			_hasLastSafeCameraPosition = true;
			return wantedPosition;
		}

		private static Quaternion UpdateBuildCameraViewDirection(float dt)
		{
			float mouseHorizontalPolarity = Valheim_Build_CameraPlugin.invertMouseLookHorizontal.Value == Valheim_Build_CameraPlugin.Toggle.On ? -1f : 1f;
			float controllerHorizontalPolarity = Valheim_Build_CameraPlugin.invertControllerLookHorizontal.Value == Valheim_Build_CameraPlugin.Toggle.On ? -1f : 1f;
			Valheim_Build_CameraPlugin.CameraYaw += mouseHorizontalPolarity * PlayerController.m_mouseSens * Input.GetAxis("Mouse X") + controllerHorizontalPolarity * ZInput.GetJoyRightStickX() * 110f * dt;

			float vanillaMousePolarity = PlayerController.m_invertMouse ? -1f : 1f;
			float mouseVerticalPolarity = Valheim_Build_CameraPlugin.invertMouseLookVertical.Value == Valheim_Build_CameraPlugin.Toggle.On ? -vanillaMousePolarity : vanillaMousePolarity;
			float controllerVerticalPolarity = Valheim_Build_CameraPlugin.invertControllerLookVertical.Value == Valheim_Build_CameraPlugin.Toggle.On ? -1f : 1f;
			float pitch = Valheim_Build_CameraPlugin.CameraPitch - (mouseVerticalPolarity * PlayerController.m_mouseSens * Input.GetAxis("Mouse Y") - controllerVerticalPolarity * ZInput.GetJoyRightStickY() * 110f * dt);
			Valheim_Build_CameraPlugin.CameraPitch = Mathf.Clamp(pitch, -89f, 89f);
			return Quaternion.Euler(0f, Valheim_Build_CameraPlugin.CameraYaw, 0f) * Quaternion.Euler(Valheim_Build_CameraPlugin.CameraPitch, 0f, 0f);
		}

		private static Vector3 GetMovementInput(float dt)
		{
			Vector3 movement = Vector3.zero;
			if (ZInput.GetButton("Left")) movement -= Vector3.right;
			if (ZInput.GetButton("Right")) movement += Vector3.right;
			if (ZInput.GetButton("Forward")) movement += Vector3.forward;
			if (ZInput.GetButton("Backward")) movement -= Vector3.forward;

			Character.takeInputDelay = Mathf.Max(0f, Character.takeInputDelay - dt);
			if (ZInput.GetButton("Jump") || ZInput.GetButton("JoyJump") && Character.takeInputDelay <= 0f && !Hud.IsPieceSelectionVisible())
				movement += Vector3.up;
			if (ZInput.GetButton("Crouch") || ZInput.GetButtonPressedTimer("JoyCrouch") > 0.33f)
				movement -= Vector3.up;

			movement.Normalize();
			movement += Vector3.right * ZInput.GetJoyLeftStickX();
			movement -= Vector3.forward * ZInput.GetJoyLeftStickY();
			float baseSpeed = ZInput.GetButton("Run") ? Player.m_localPlayer.m_runSpeed : Player.m_localPlayer.m_walkSpeed;
			return movement * (dt * baseSpeed * Valheim_Build_CameraPlugin.cameraMoveSpeedMultiplier.Value);
		}

		internal static void UpdateBuildCamera(float dt, GameCamera camera)
		{
			if (Console.IsVisible() || !Player.m_localPlayer.TakeInput() || Hud.IsPieceSelectionVisible()) return;
			Vector3 movement = GetMovementInput(dt);
			Transform cameraTransform = camera.transform;
			if (Valheim_Build_CameraPlugin.moveWithRespectToWorld.Value != Valheim_Build_CameraPlugin.Toggle.On)
				movement = cameraTransform.TransformVector(movement);
			Vector3 wantedPosition = ClampToStationRange(cameraTransform.position + movement);
			cameraTransform.position = ClampToTerrain(cameraTransform.position, wantedPosition);
			cameraTransform.rotation = UpdateBuildCameraViewDirection(dt);
		}

		private static bool HasRequiredComfort()
		{
			Player player = Player.m_localPlayer;
			if (!player) return false;
			if (player == _comfortPlayer && Time.time < _nextComfortRefresh) return _hasRequiredComfort;

			_comfortPlayer = player;
			_nextComfortRefresh = Time.time + ComfortRefreshInterval;
			_hasRequiredComfort = CalculateComfort(player);
			return _hasRequiredComfort;
		}

		private static bool CalculateComfort(Player player)
		{
			if (player.GetComfortLevel() < Valheim_Build_CameraPlugin.minimumComfortLevel.Value) return false;
			SEMan statusEffects = player.GetSEMan();
			if (statusEffects == null) return false;
			if (statusEffects.HaveStatusEffect(SEMan.s_statusEffectResting)) return true;

			bool nearFire = statusEffects.HaveStatusEffect(SEMan.s_statusEffectCampFire);
			bool shelteredOrSitting = player.InShelter() || player.IsSitting();
			bool insideWarmArea = EffectArea.IsPointInsideArea(player.transform.position, EffectArea.Type.WarmCozyArea, 1f);
			bool wet = statusEffects.HaveStatusEffect(SEMan.s_statusEffectWet) && !insideWarmArea;
			bool cold = statusEffects.HaveStatusEffect(SEMan.s_statusEffectCold) || statusEffects.HaveStatusEffect(SEMan.s_statusEffectFreezing);
			return nearFire && shelteredOrSitting && !player.IsSensed() && !wet && !cold && !statusEffects.HaveStatusEffect(SEMan.s_statusEffectBurning);
		}

		internal static bool CanUseCamera()
		{
			return Valheim_Build_CameraPlugin.restrictionMode.Value != Valheim_Build_CameraPlugin.RestrictionMode.CameraNeedsCoziness || HasRequiredComfort();
		}

		internal static bool CanPickUpFromCamera()
		{
			return Valheim_Build_CameraPlugin.restrictionMode.Value != Valheim_Build_CameraPlugin.RestrictionMode.CameraPickUpNeedsCoziness || HasRequiredComfort();
		}

		internal static void ShowComfortMessage()
		{
			if (!Player.m_localPlayer || Time.time < _nextComfortMessageTime) return;
			_nextComfortMessageTime = Time.time + 1.5f;
			string text = Localization.instance.Localize("$buildcamera_needs_cozy", Valheim_Build_CameraPlugin.minimumComfortLevel.Value.ToString());
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, text);
		}

		internal static bool ShouldShowPickupWarning()
		{
			return InBuildMode() && !CanPickUpFromCamera() && HasNearbyPickup();
		}

		private static bool HasNearbyPickup()
		{
			if (!GameCamera.instance || !Player.m_localPlayer) return false;
			Vector3 pickupCenter = GameCamera.instance.transform.position + Vector3.up;
			int count = Physics.OverlapSphereNonAlloc(pickupCenter, Valheim_Build_CameraPlugin.resourcePickupRange.Value, NearbyPickupColliders, Player.m_localPlayer.m_autoPickupMask);
			try
			{
				for (int i = 0; i < count; ++i)
				{
					if (TryFindItemDrop(NearbyPickupColliders[i], out ItemDrop item) && item.m_autoPickup && !item.IsPiece())
						return true;
				}

				return false;
			}
			finally
			{
				Array.Clear(NearbyPickupColliders, 0, count);
			}
		}

		internal static bool TryGetAutoPickupPlayer(out Player player)
		{
			player = Player.m_localPlayer;
			return player && !player.IsTeleporting() && Player.m_enableAutoPickup && CanPickUpFromCamera();
		}

		internal static void AutoPickup(GameCamera camera, Player player)
		{
			Vector3 pickupCenter = camera.transform.position + Vector3.up;
			int count = Physics.OverlapSphereNonAlloc(pickupCenter, Valheim_Build_CameraPlugin.resourcePickupRange.Value, NearbyPickupColliders, player.m_autoPickupMask);
			NearbyPickupItemIds.Clear();
			try
			{
				for (int i = 0; i < count; ++i)
				{
					if (!TryFindItemDrop(NearbyPickupColliders[i], out ItemDrop item) || !item.m_autoPickup || item.IsPiece() || player.HaveUniqueKey(item.m_itemData.m_shared.m_name)) continue;
					if (!NearbyPickupItemIds.Add(item.GetInstanceID())) continue;
					ZNetView netView = item.m_nview;
					if (!netView || !netView.IsValid()) continue;
					if (!item.CanPickup())
					{
						item.RequestOwn();
						continue;
					}

					if (item.InTar()) continue;
					item.Load();
					if (!player.m_inventory.CanAddItem(item.m_itemData) || item.m_itemData.GetWeight() + player.m_inventory.GetTotalWeight() > player.GetMaxCarryWeight()) continue;
					player.Pickup(item.gameObject);
				}
			}
			finally
			{
				Array.Clear(NearbyPickupColliders, 0, count);
				NearbyPickupItemIds.Clear();
			}
		}

		private static bool TryFindItemDrop(Collider collider, out ItemDrop item)
		{
			item = null!;
			if (!collider || !collider.attachedRigidbody) return false;
			Rigidbody rigidbody = collider.attachedRigidbody;
			item = rigidbody.GetComponent<ItemDrop>();
			if (item) return true;
			FloatingTerrainDummy floatingTerrain = rigidbody.GetComponent<FloatingTerrainDummy>();
			if (!floatingTerrain || !floatingTerrain.m_parent) return false;
			item = floatingTerrain.m_parent.GetComponent<ItemDrop>();
			return item;
		}

		internal static void AddLocalizations(Localization localization)
		{
			localization.AddWord("buildcamera_needs_cozy", "Be cozy to use Build Camera (requires comfort $1).");
			localization.AddWord("buildcamera_pickup_blocked_title", "Build Camera Pickup");
			localization.AddWord("buildcamera_pickup_blocked", "Get cozy before collecting items through the build camera.\nRequired comfort: $1");
		}
	}
}
