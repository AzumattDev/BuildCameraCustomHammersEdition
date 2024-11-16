using HarmonyLib;
using UnityEngine;
using Valheim_Build_Camera.Compatibility.WardIsLove;

namespace Valheim_Build_Camera
{
    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    static class Player_Awake
    {
        static void Prefix(Player __instance, ref float ___m_maxPlaceDistance)
        {
            if (___m_maxPlaceDistance < Valheim_Build_CameraPlugin.distanceCanBuildFromAvatar.Value)
            {
                Valheim_Build_CameraPlugin.BuildCameraCHELogger.LogDebug($"in Player_Awake, changing maxPlaceDistance from {___m_maxPlaceDistance} to {Valheim_Build_CameraPlugin.distanceCanBuildFromAvatar.Value}");
                ___m_maxPlaceDistance = Valheim_Build_CameraPlugin.distanceCanBuildFromAvatar.Value;
            }
            else
            {
                Valheim_Build_CameraPlugin.BuildCameraCHELogger.LogDebug($"Not changing distanceCanBuildFromAvatar (AKA maxPlaceDistance) as it seems another mod has already changed it.");
            }
        }
    }

    [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Start))]
    static class CraftingStation_Start_Patch
    {
        static void Prefix(CraftingStation __instance, ref float ___m_rangeBuild)
        {
            if (___m_rangeBuild < Valheim_Build_CameraPlugin.distanceCanBuildFromWorkbench.Value)
            {
                Valheim_Build_CameraPlugin.BuildCameraCHELogger.LogDebug($"in CraftingStation_Start, changing rangeBuild from {___m_rangeBuild} to {Valheim_Build_CameraPlugin.distanceCanBuildFromWorkbench.Value}");
                ___m_rangeBuild = Valheim_Build_CameraPlugin.distanceCanBuildFromWorkbench.Value;
            }
            else
            {
                Valheim_Build_CameraPlugin.BuildCameraCHELogger.LogDebug($"Not changing distanceCanBuildFromWorkbench (AKA rangeBuild) as it seems another mod has already changed it.");
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.SetLocalPlayer))]
    static class Player_SetLocalPlayer_Patch
    {
        static void Postfix(Player __instance)
        {
            Utils.DisableBuildMode();
        }
    }


    /// <summary>
    /// Skip the game's Update when in build mode, to disallow actions like
    /// Interact(). Only allow UpdatePlacement.
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="__runOriginal"></param>
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    static class Player_Update_Patch
    {
        static void Prefix(ref Player __instance, ref bool __runOriginal)
        {
            if (Utils.IsLocalPlayer(__instance) && Utils.InBuildMode())
            {
                if (Utils.ShouldDeactivateBuildMode(__instance))
                {
                    // The user might have unequipped the hammer (e.g. by using hotbar
                    // items or unequipping via the inventory), so deactivate build mode.
                    Utils.DisableBuildMode();

                    __runOriginal = true;
                }
                else
                {
                    __runOriginal = false;

                    // Allow hotkeys so that hammer can be unequipped, which exits build mode
                    // game source: Player.Update
                    if (__instance.TakeInput())
                    {
                        if (Input.GetKeyDown(KeyCode.Alpha1) || ZInput.GetButtonDown("Hotbar1"))
                        {
                            __instance.UseHotbarItem(1);
                        }

                        if (Input.GetKeyDown(KeyCode.Alpha2) || ZInput.GetButtonDown("Hotbar2"))
                        {
                            __instance.UseHotbarItem(2);
                        }

                        if (Input.GetKeyDown(KeyCode.Alpha3) || ZInput.GetButtonDown("Hotbar3"))
                        {
                            __instance.UseHotbarItem(3);
                        }

                        if (Input.GetKeyDown(KeyCode.Alpha4) || ZInput.GetButtonDown("Hotbar4"))
                        {
                            __instance.UseHotbarItem(4);
                        }

                        if (Input.GetKeyDown(KeyCode.Alpha5) || ZInput.GetButtonDown("Hotbar5"))
                        {
                            __instance.UseHotbarItem(5);
                        }

                        if (Input.GetKeyDown(KeyCode.Alpha6) || ZInput.GetButtonDown("Hotbar6"))
                        {
                            __instance.UseHotbarItem(6);
                        }

                        if (Input.GetKeyDown(KeyCode.Alpha7) || ZInput.GetButtonDown("Hotbar7"))
                        {
                            __instance.UseHotbarItem(7);
                        }

                        if (Input.GetKeyDown(KeyCode.Alpha8) || ZInput.GetButtonDown("Hotbar8"))
                        {
                            __instance.UseHotbarItem(8);
                        }

                        if (ZInput.GetButtonDown("Hide") || ZInput.GetButtonDown("JoyHide"))
                        {
                            if ((__instance.GetRightItem() != null || __instance.GetLeftItem() != null) && !__instance.InAttack())
                            {
                                __instance.HideHandItems();
                            }
                        }

                        __instance.UpdatePlacement(true, Time.deltaTime);
                    }
                }
            }
            else
            {
                __runOriginal = true;
            }
        }

        static void Postfix(ref Player __instance)
        {
            if (Utils.IsLocalPlayer(__instance) && Valheim_Build_CameraPlugin.toggleBuildMode.Value.IsDown() &&
                __instance.TakeInput())
            {
                if (!Utils.InBuildMode() && Utils.ToolIsEquipped(__instance) && Utils.BuildStationInRange(__instance))
                {
                    Utils.EnableBuildMode();
                    return;
                }
                else if (Utils.InBuildMode())
                {
                    Utils.DisableBuildMode();
                    return;
                }
            }

            if (Utils.IsLocalPlayer(__instance) && Valheim_Build_CameraPlugin.toggleBuildMode.Value.IsDown())
            {
                if (!__instance.TakeInput())
                {
                    Utils.LogWhenVerbose("Build Mode not enabled because chat, console, menu, inventory, map, or similar is open.");
                }
                else if (!Utils.ToolIsEquipped(__instance))
                {
                    Utils.LogWhenVerbose("Build Mode not enabled because hammer is not equipped.");
                }
                else if (!Utils.BuildStationInRange(__instance))
                {
                    Utils.LogWhenVerbose("Build Mode not enabled because no build station (e.g. workbench) is in range.");
                }
            }
        }
    }

    /// <summary>
    /// Stops the player's avatar from moving when in build mode.
    /// </summary>
    /// <param name="__result"></param>
    /// <param name="__runOriginal"></param>
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.TakeInput))]
    static class PlayerController_TakeInput_Patch
    {
        static void Prefix(PlayerController __instance, ref bool __result, ref bool __runOriginal)
        {
            if (Utils.InBuildMode())
            {
                __result = false;
                __runOriginal = false;
            }
            else
            {
                __runOriginal = true;
            }
        }
    }

    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateCamera))]
    [HarmonyBefore("Azumatt.FirstPersonMode")]
    [HarmonyPriority(Priority.VeryHigh)]
    static class GameCamera_UpdateCamera_Patch
    {
        static void Prefix(float dt, ref GameCamera __instance, ref bool __runOriginal)
        {
            if (Utils.InBuildMode())
            {
                Utils.UpdateBuildCamera(dt, ref __instance);
                if (WardIsLovePlugin.IsLoaded() && CustomCheck.CheckAccess(Player.m_localPlayer.GetPlayerID(), __instance.transform.position, flash: false))
                {
                    Utils.AutoPickup(dt, ref __instance);
                }
                else if (!WardIsLovePlugin.IsLoaded() && PrivateArea.CheckAccess(__instance.transform.position, flash: false, wardCheck: true))
                {
                    Utils.AutoPickup(dt, ref __instance);
                }

                __runOriginal = false;
            }
            else
            {
                __runOriginal = true;
            }
        }
    }
}