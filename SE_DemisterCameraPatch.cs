using HarmonyLib;
using UnityEngine;

namespace Valheim_Build_Camera
{

    [HarmonyPatch(typeof(SE_Demister), nameof(SE_Demister.UpdateStatusEffect))]
    public static class SE_DemisterCameraPatch
    {
        private static readonly System.Collections.Generic.Dictionary<GameObject, ForceFieldData> originalForceFields = new();
        private struct ForceFieldData
        {
            public float endRange;
        }
        
        public static bool Prefix(SE_Demister __instance, float dt)
        {
            Player player = __instance.m_character as Player;
            if (!player || !Utils.IsLocalPlayer(player) || !Utils.InBuildMode() || Valheim_Build_CameraPlugin.demisterFollowCamera.Value != Valheim_Build_CameraPlugin.Toggle.On)
            {
                RestoreNormalForceField(__instance);
                return true;
            }

            IncreaseForceFieldRange(__instance);

            UpdateDemisterForCamera(__instance, player, dt);

            return false;
        }

        private static void IncreaseForceFieldRange(SE_Demister instance)
        {
            if (!instance.m_ballInstance || originalForceFields.ContainsKey(instance.m_ballInstance))
                return;

            ParticleSystemForceField forceField = instance.m_ballInstance.GetComponentInChildren<ParticleSystemForceField>();
            if (!forceField)
                return;

            originalForceFields[instance.m_ballInstance] = new ForceFieldData
            {
                endRange = forceField.endRange
            };

            forceField.endRange *= Valheim_Build_CameraPlugin.demisterRangeMultiplier.Value;
        }

        private static void RestoreNormalForceField(SE_Demister instance)
        {
            if (!instance.m_ballInstance || !originalForceFields.ContainsKey(instance.m_ballInstance))
                return;

            // The ParticleSystemForceField is on a child GameObject, not the root ball instance
            ParticleSystemForceField forceField = instance.m_ballInstance.GetComponentInChildren<ParticleSystemForceField>();
            if (!forceField)
                return;

            // Restore original values
            ForceFieldData originalData = originalForceFields[instance.m_ballInstance];
            forceField.endRange = originalData.endRange;

            originalForceFields.Remove(instance.m_ballInstance);
        }

        private static void UpdateDemisterForCamera(SE_Demister instance, Player player, float dt)
        {
            Vector3 cameraPos = GetCameraPosition();
            Vector3 cameraForward = GetCameraForward();

            if (!instance.m_ballInstance)
            {
                instance.m_ballInstance = Object.Instantiate(instance.m_ballPrefab, cameraPos + cameraForward * 0.5f, Quaternion.identity);
                return;
            }

            bool isUnderRoof = IsUnderRoof(instance);

            Vector3 ballPos = instance.m_ballInstance.transform.position;

            Vector3 offset = isUnderRoof ? instance.m_offsetInterior : instance.m_offset;
            float noiseDistance = isUnderRoof ? instance.m_noiseDistanceInterior : instance.m_noiseDistance;

            Vector3 cameraRight = GetCameraRight();
            Vector3 cameraUp = GetCameraUp();

            Vector3 transformedOffset = cameraRight * offset.x + cameraUp * offset.y + cameraForward * offset.z;
            Vector3 targetPos = cameraPos + transformedOffset;

            float time = Time.time * instance.m_noiseSpeed;
            Vector3 noiseOffset = new Vector3(Mathf.Sin(time * 4f), Mathf.Sin(time * 2f) * instance.m_noiseDistanceYScale, Mathf.Cos(time * 5f)) * noiseDistance;
            Vector3 targetWithNoise = targetPos + noiseOffset;

            float distance = Vector3.Distance(targetWithNoise, ballPos);

            if (distance > instance.m_maxDistance * 2.0f)
            {
                ballPos = targetWithNoise;
            }
            else if (distance > instance.m_maxDistance)
            {
                Vector3 direction = (ballPos - targetWithNoise).normalized;
                ballPos = targetWithNoise + direction * instance.m_maxDistance;
            }

            instance.m_ballVel += (targetWithNoise - ballPos).normalized * instance.m_ballAcceleration * dt;

            if (instance.m_ballVel.magnitude > instance.m_ballMaxSpeed)
            {
                instance.m_ballVel = instance.m_ballVel.normalized * instance.m_ballMaxSpeed;
            }

            if (!isUnderRoof)
            {
                instance.m_ballVel += player.GetVelocity() * instance.m_characterVelocityFactor * dt;
            }

            instance.m_ballVel -= instance.m_ballVel * instance.m_ballFriction;

            instance.m_ballInstance.transform.position = ballPos + instance.m_ballVel * dt;

            instance.m_ballInstance.transform.rotation *= Quaternion.Euler(instance.m_rotationSpeed, 0.0f, instance.m_rotationSpeed * 0.5321f);
        }

        private static Vector3 GetCameraPosition()
        {
            return GameCamera.instance ? GameCamera.instance.transform.position : Vector3.zero;
        }

        private static Vector3 GetCameraForward()
        {
            return GameCamera.instance ? GameCamera.instance.transform.forward : Vector3.forward;
        }

        private static Vector3 GetCameraRight()
        {
            return GameCamera.instance ? GameCamera.instance.transform.right : Vector3.right;
        }

        private static Vector3 GetCameraUp()
        {
            return GameCamera.instance ? GameCamera.instance.transform.up : Vector3.up;
        }

        private static bool IsUnderRoof(SE_Demister instance)
        {
            Vector3 cameraPos = GetCameraPosition();
            return Physics.Raycast(cameraPos, Vector3.up, out RaycastHit _, 4f, instance.m_coverRayMask);
        }

        internal static void CleanupBall(GameObject ball)
        {
            if (ball)
            {
                originalForceFields.Remove(ball);
            }
        }
    }

    [HarmonyPatch(typeof(SE_Demister), nameof(SE_Demister.RemoveEffects))]
    public static class SE_DemisterCleanupPatch
    {
        [HarmonyPrefix]
        public static void RemoveEffects_Prefix(SE_Demister __instance)
        {
            SE_DemisterCameraPatch.CleanupBall(__instance.m_ballInstance);
        }
    }
}