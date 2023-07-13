using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Valheim_Build_Camera
{
    public class Utils
    {
        internal static void LogWhenVerbose(string s)
        {
            if (Valheim_Build_CameraPlugin.verboseLogging.Value != Valheim_Build_CameraPlugin.Toggle.On) return;
            Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, s);
            Valheim_Build_CameraPlugin.BuildCameraCHELogger.LogInfo(s);
        }

        // Returns true when the player has Build Mode activated.
        public static bool InBuildMode()
        {
            return (bool)Player.m_localPlayer && Valheim_Build_CameraPlugin.inBuildMode[Player.m_localPlayer];
        }

        internal static void DisableBuildMode()
        {
            Valheim_Build_CameraPlugin.inBuildMode[Player.m_localPlayer] = false;
        }

        internal static void EnableBuildMode()
        {
            Valheim_Build_CameraPlugin.inBuildMode[Player.m_localPlayer] = true;

            // When entering build mode, we reset the view direction of the build
            // camera, so that it matches the player's current direction. Thus, when
            // entering build mode, there is no (abrupt) change to the camera.
            var r = Player.m_localPlayer.m_eye.transform.rotation;
            Valheim_Build_CameraPlugin.buildCameraViewDirection.pitch = r.eulerAngles.x;
            Valheim_Build_CameraPlugin.buildCameraViewDirection.yaw = r.eulerAngles.y;

            Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Entering Build Mode.");
        }

        /// <summary>
        /// Returns true when player is the local player.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        internal static bool IsLocalPlayer(in Player player)
        {
            return (bool)Player.m_localPlayer && player == Player.m_localPlayer;
        }

        /// <summary>
        /// Returns true when the item is a Build Camera-compatible tool such as hammer.
        /// </summary>
        /// <param name="itemData"></param>
        /// <returns></returns>
        static bool IsTool(in ItemDrop.ItemData itemData)
        {
            return itemData?.m_shared.m_buildPieces;
        }

        /// <summary>
        /// Returns true when this player has a Build Camera-compatible tool such as
        /// hammer equipped.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        internal static bool ToolIsEquipped(in Player player)
        {
            // Tools are always equipped in the right hand.
            return IsTool(player.m_rightItem);
        }

        /// <summary>
        /// Returns true when build mode should be deactivated: the hammer is
        /// unequipped.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        internal static bool ShouldDeactivateBuildMode(in Player player)
        {
            return !ToolIsEquipped(player);
        }

        /// <summary>
        /// Returns the NearestBuildStation. It can be a workbench or a stone cutting bench.
        /// </summary>
        /// <param name="playerOrCamera"></param>
        /// <returns></returns>
        static Valheim_Build_CameraPlugin.NearbyCraftingStation? GetNearestBuildStation(in Vector3 playerOrCamera)
        {
            if (CraftingStation.m_allStations.Count == 0)
            {
                return null;
            }
            else
            {
                List<Valheim_Build_CameraPlugin.NearbyCraftingStation> nearbyCraftingStations = new();
                foreach (CraftingStation station in CraftingStation.m_allStations)
                {
                    nearbyCraftingStations.Add(new Valheim_Build_CameraPlugin.NearbyCraftingStation
                    {
                        position = station.transform.position,
                        distance = Vector3.Distance(station.transform.position, playerOrCamera),
                        rangeBuild = station.m_rangeBuild
                    });
                }

                return nearbyCraftingStations.OrderBy(x => x.distance).First();
            }
        }

        /// <summary>
        /// Returns true when a build/craft station is within range.
        ///
        /// Note that range is determined by the specific build/craft station. The
        /// range is *not* multiplied by cameraRangeMultiplier. That is, we expect
        /// the player to enter build mode while within the build range of a
        /// crafting station. The camera may stray outside of the building range,
        /// but all pieces will (presumably) be placed within build range.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        internal static bool BuildStationInRange(in Player player)
        {
            var maybeStation = GetNearestBuildStation(player.transform.position);
            if (maybeStation is Valheim_Build_CameraPlugin.NearbyCraftingStation nearbyCraftingStation)
            {
                return nearbyCraftingStation.distance <= nearbyCraftingStation.rangeBuild;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Prevents the game camera from going out of range of the nearest build
        /// station (multiplied by the cameraRangeMultiplier).
        /// </summary>
        /// <param name="__instance"></param>
        static void StayNearWorkbench(ref GameCamera __instance)
        {
            var maybeStation = GetNearestBuildStation(__instance.transform.position);
            if (maybeStation is Valheim_Build_CameraPlugin.NearbyCraftingStation nearbyCraftingStation)
            {
                if (nearbyCraftingStation.distance
                    > nearbyCraftingStation.rangeBuild * Valheim_Build_CameraPlugin.cameraRangeMultiplier.Value)
                {
                    float error = nearbyCraftingStation.distance
                                  - nearbyCraftingStation.rangeBuild *
                                  Valheim_Build_CameraPlugin.cameraRangeMultiplier.Value;
                    Vector3 towardStation = nearbyCraftingStation.position - __instance.transform.position;
                    Vector3 correction = error * towardStation.normalized;
                    __instance.transform.position = __instance.transform.position + correction;
                }
            }
            else
            {
                DisableBuildMode();
            }
        }

        /// <summary>
        /// Prevents the game camera from going below ground.
        /// </summary>
        /// <param name="__instance"></param>
        static void StayAboveGround(ref GameCamera __instance)
        {
            if (ZoneSystem.instance.GetGroundHeight(__instance.transform.position, out float height))
            {
                if (__instance.transform.position.y < height)
                {
                    Vector3 p = __instance.transform.position;
                    p.y = height;
                    __instance.transform.position = p;
                }
            }
        }

        /// <summary>
        /// Updates buildCameraViewDirection (based on mouse and controller
        /// movement) and returns the pitch and yaw as a quanternion.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        static Quaternion UpdateBuildCameraViewDirection(float dt)
        {
            // Game source: GameCamera.UpdateFreeFly(float dt)
            Valheim_Build_CameraPlugin.buildCameraViewDirection.yaw +=
                (PlayerController.m_mouseSens * Input.GetAxis("Mouse X"))
                + (ZInput.GetJoyRightStickX() * 110f * dt);

            float polarity = PlayerController.m_invertMouse ? -1 : 1;
            float pitchUnchecked =
                Valheim_Build_CameraPlugin.buildCameraViewDirection.pitch -
                polarity
                * ((PlayerController.m_mouseSens * Input.GetAxis("Mouse Y"))
                   - (ZInput.GetJoyRightStickY() * 110f * dt));
            Valheim_Build_CameraPlugin.buildCameraViewDirection.pitch = Mathf.Clamp(pitchUnchecked, -89f, 89f);

            return
                Quaternion.Euler(0f, Valheim_Build_CameraPlugin.buildCameraViewDirection.yaw, 0f) *
                Quaternion.Euler(Valheim_Build_CameraPlugin.buildCameraViewDirection.pitch, 0f, 0f);
        }

        /// <summary>
        /// Returns the untransformed (i.e. unaffected by current camera view
        /// direction) vector by which the GameCamera should move (i.e. pan).
        ///
        /// Movement is based on keyboard (and controller) input.
        /// </summary>
        /// <returns></returns>
        static Vector3 UntransformedMovementVector(float dt)
        {
            // Game source: GameCamera.UpdateFreeFly(float dt)
            Vector3 vector = Vector3.zero;

            if (ZInput.GetButton("Left"))
            {
                vector -= Vector3.right;
            }

            if (ZInput.GetButton("Right"))
            {
                vector += Vector3.right;
            }

            if (ZInput.GetButton("Forward"))
            {
                vector += Vector3.forward;
            }

            if (ZInput.GetButton("Backward"))
            {
                vector -= Vector3.forward;
            }

            if (ZInput.GetButton("Jump") || ZInput.GetButton("JoyJump"))
            {
                vector += Vector3.up;
            }

            if (ZInput.GetButton("Crouch") || ZInput.GetButton("JoyCrouch"))
            {
                vector -= Vector3.up;
            }

            // I'm not sure if this is correct, but I'm going to normalize before
            // accounting for analog (joystick) movements. I would *not* want to
            // normalize after accounting for analog movement, because that would ruin
            // the whole point of having an analog input.
            vector.Normalize();

            vector += Vector3.right * ZInput.GetJoyLeftStickX();
            vector += -Vector3.forward * ZInput.GetJoyLeftStickY();

            float baseSpeed =
                ZInput.GetButton("Run") ? Player.m_localPlayer.m_runSpeed : Player.m_localPlayer.m_walkSpeed;

            // When I use m_walkSpeed to move the build camera, it moves very slow,
            // much slower than the avatar's walking speed. m_walkSpeed is used in
            // Character.UpdateWalking, but that function is so dense, I don't
            // understand why the avatar walks faster. So we speed up the build
            // camera's movement by cameraMoveSpeedMultiplier.
            return vector * (dt * baseSpeed * Valheim_Build_CameraPlugin.cameraMoveSpeedMultiplier.Value);
        }

        /// <summary>
        /// Pans and rotates the camera based on user input (e.g. mouse movement and WASD).
        ///
        /// Assumes that Build Mode is activated.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="__instance"></param>
        internal static void UpdateBuildCamera(float dt, ref GameCamera __instance)
        {
            // Game source: GameCamera.UpdateFreeFly(float dt)
            if (!Console.IsVisible() && Player.m_localPlayer.TakeInput() && !Hud.IsPieceSelectionVisible())
            {
                var untransformed = UntransformedMovementVector(dt);
                Vector3 moveBy = Valheim_Build_CameraPlugin.moveWithRespectToWorld.Value ==
                                 Valheim_Build_CameraPlugin.Toggle.On
                    ? untransformed
                    : __instance.transform.TransformVector(untransformed);

                __instance.transform.position += moveBy;
                StayNearWorkbench(ref __instance);
                StayAboveGround(ref __instance);

                __instance.transform.rotation = UpdateBuildCameraViewDirection(dt);
            }
        }
    }
}