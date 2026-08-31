using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Valheim_Build_Camera.Compatibility.WardIsLove;

namespace Valheim_Build_Camera
{
	[HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
	static class Localization_SetupLanguage_Patch
	{
		static void Postfix(Localization __instance)
		{
			Utils.AddLocalizations(__instance);
			PickupBlockedHud.LanguageChanged();
		}
	}

	[HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.HaveBuildStationInRange))]
	static class CraftingStation_HaveBuildStationInRange_Patch
	{
		static void Postfix(string name, Vector3 point, ref CraftingStation __result)
		{
			if (!__result && Utils.InBuildMode() && Utils.FindCameraStation(name, point) is { } station)
				__result = station;
		}
	}

	[HarmonyPatch]
	static class Player_BuildCameraPlacementDistance_Patch
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.DeclaredMethod(typeof(Player), "PieceRayTest");
			yield return AccessTools.DeclaredMethod(typeof(Player), "UpdateWearNTearHover");
			yield return AccessTools.DeclaredMethod(typeof(Player), "CopyPiece");
			yield return AccessTools.DeclaredMethod(typeof(Player), "RemovePiece");
		}

		static void Prefix(ref float ___m_maxPlaceDistance, out float __state)
		{
			__state = ___m_maxPlaceDistance;
			if (Utils.InBuildMode())
				___m_maxPlaceDistance = Mathf.Max(___m_maxPlaceDistance, Valheim_Build_CameraPlugin.distanceCanBuildFromAvatar.Value);
		}

		static Exception Finalizer(Exception __exception, ref float ___m_maxPlaceDistance, float __state)
		{
			___m_maxPlaceDistance = __state;
			return __exception;
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.SetLocalPlayer))]
	static class Player_SetLocalPlayer_Patch
	{
		static void Postfix()
		{
			PickupBlockedHud.Cleanup();
			Utils.DisableBuildMode();
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.Update))]
	static class Player_Update_Patch
	{
		private static readonly string[] HotbarButtons =
		{
			"Hotbar1", "Hotbar2", "Hotbar3", "Hotbar4", "Hotbar5", "Hotbar6", "Hotbar7", "Hotbar8",
		};

		static void Prefix(Player __instance, ref bool __runOriginal)
		{
			if (!Utils.IsLocalPlayer(__instance) || !Utils.InBuildMode())
			{
				__runOriginal = true;
				return;
			}

			if (!Utils.CanUseCamera())
			{
				Utils.ShowComfortMessage();
				Utils.DisableBuildMode();
				__runOriginal = true;
				return;
			}

			if (Utils.ShouldDeactivateBuildMode(__instance))
			{
				Utils.DisableBuildMode();
				__runOriginal = true;
				return;
			}

			__runOriginal = false;
			if (!__instance.TakeInput()) return;
			for (int index = 0; index < HotbarButtons.Length; ++index)
			{
				if (Input.GetKeyDown(KeyCode.Alpha1 + index) || ZInput.GetButtonDown(HotbarButtons[index]))
					__instance.UseHotbarItem(index + 1);
			}

			if (ZInput.GetButtonDown("Hide") || ZInput.GetButtonDown("JoyHide"))
			{
				if ((__instance.GetRightItem() != null || __instance.GetLeftItem() != null) && !__instance.InAttack())
					__instance.HideHandItems();
			}

			__instance.UpdatePlacement(true, Time.deltaTime);
		}

		static void Postfix(Player __instance)
		{
			if (!Utils.IsLocalPlayer(__instance) || !Valheim_Build_CameraPlugin.toggleBuildMode.Value.IsDown()) return;
			if (Utils.InBuildMode())
			{
				if (__instance.TakeInput()) Utils.DisableBuildMode();
				return;
			}

			if (!__instance.TakeInput())
			{
				Utils.LogWhenVerbose("Build Mode not enabled because chat, console, menu, inventory, map, or similar is open.");
				return;
			}

			if (!Utils.ToolIsEquipped(__instance))
			{
				Utils.LogWhenVerbose("Build Mode not enabled because hammer is not equipped.");
				return;
			}

			if (!Utils.BuildStationInRange(__instance))
			{
				Utils.LogWhenVerbose("Build Mode not enabled because no build station (e.g. workbench) is in range.");
				return;
			}

			if (!Utils.CanUseCamera())
			{
				Utils.ShowComfortMessage();
				Utils.LogWhenVerbose("Build Mode not enabled because the cozy and comfort requirement is not met.");
				return;
			}

			Utils.EnableBuildMode();
		}
	}

	[HarmonyPatch(typeof(PlayerController), nameof(PlayerController.TakeInput))]
	static class PlayerController_TakeInput_Patch
	{
		static void Prefix(ref bool __result, ref bool __runOriginal)
		{
			if (!Utils.InBuildMode())
			{
				__runOriginal = true;
				return;
			}

			__result = false;
			__runOriginal = false;
		}
	}

	[HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateCamera))]
	[HarmonyBefore("Azumatt.FirstPersonMode")]
	[HarmonyPriority(Priority.VeryHigh)]
	static class GameCamera_UpdateCamera_Patch
	{
		private const float PickupScanInterval = 0.1f;
		private static float _nextPickupScan;

		static void Prefix(float dt, GameCamera __instance, ref bool __runOriginal)
		{
			if (!Utils.InBuildMode())
			{
				_nextPickupScan = 0f;
				__runOriginal = true;
				return;
			}

			Utils.UpdateBuildCamera(dt, __instance);
			if (Time.time >= _nextPickupScan)
			{
				_nextPickupScan = Time.time + PickupScanInterval;
				if (Utils.TryGetAutoPickupPlayer(out Player player))
				{
					if (WardAccess.Check(player.GetPlayerID(), __instance.transform.position)) Utils.AutoPickup(__instance, player);
				}
			}

			__runOriginal = false;
		}

		static void Postfix(GameCamera __instance)
		{
			if (Utils.InBuildMode()) DvergrCircletCameraLight.Update(__instance);
		}
	}
}
