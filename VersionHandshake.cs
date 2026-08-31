using System;
using System.Collections.Generic;
using HarmonyLib;

namespace Valheim_Build_Camera;

[HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
internal static class RegisterAndCheckVersion
{
	private static void Prefix(ZNetPeer peer)
	{
		Valheim_Build_CameraPlugin.BuildCameraCHELogger.LogDebug("Registering version RPC handler");
		peer.m_rpc.Register(RpcHandlers.VersionCheckRpc, new Action<ZRpc, ZPackage>(RpcHandlers.ReceiveVersion));

		Valheim_Build_CameraPlugin.BuildCameraCHELogger.LogInfo("Invoking version check");
		ZPackage zpackage = new();
		zpackage.Write(Valheim_Build_CameraPlugin.ModVersion);
		peer.m_rpc.Invoke(RpcHandlers.VersionCheckRpc, zpackage);
	}
}

[HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo))]
internal static class VerifyClient
{
	private static bool Prefix(ZRpc rpc, ZNet __instance)
	{
		if (!__instance.IsServer() || RpcHandlers.ValidatedPeers.Contains(rpc)) return true;
		Valheim_Build_CameraPlugin.BuildCameraCHELogger.LogWarning($"Peer ({rpc.m_socket.GetHostName()}) never sent version or couldn't due to previous disconnect, disconnecting");
		rpc.Invoke("Error", 3);
		return false;
	}

	private static void Postfix()
	{
		ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), RpcHandlers.RequestAdminSyncRpc, new ZPackage());
	}
}

[HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.ShowConnectError))]
internal static class ShowConnectionError
{
	private static void Postfix(FejdStartup __instance)
	{
		if (__instance.m_connectionFailedPanel.activeSelf && !string.IsNullOrEmpty(Valheim_Build_CameraPlugin.ConnectionError))
		{
			__instance.m_connectionFailedError.text += "\n" + Valheim_Build_CameraPlugin.ConnectionError;
			Valheim_Build_CameraPlugin.ConnectionError = "";
		}
	}
}

[HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect))]
internal static class RemoveDisconnectedPeerFromVerified
{
	private static void Prefix(ZNetPeer peer, ZNet __instance)
	{
		if (!__instance.IsServer()) return;
		Valheim_Build_CameraPlugin.BuildCameraCHELogger.LogInfo($"Peer ({peer.m_rpc.m_socket.GetHostName()}) disconnected, removing from validated list");
		_ = RpcHandlers.ValidatedPeers.Remove(peer.m_rpc);
	}
}

[HarmonyPatch]
internal static class ClearValidatedPeersOnShutdown
{
	private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
	{
		yield return AccessTools.DeclaredMethod(typeof(ZNet), nameof(ZNet.Shutdown));
		yield return AccessTools.DeclaredMethod(typeof(ZNet), nameof(ZNet.ShutdownWithoutSave));
	}

	private static void Prefix()
	{
		RpcHandlers.ValidatedPeers.Clear();
	}
}

internal static class RpcHandlers
{
	internal const string VersionCheckRpc = Valheim_Build_CameraPlugin.ModName + "_VersionCheck";
	internal const string RequestAdminSyncRpc = Valheim_Build_CameraPlugin.ModName + "_RequestAdminSync";
	internal static readonly HashSet<ZRpc> ValidatedPeers = new();

	internal static void ReceiveVersion(ZRpc rpc, ZPackage pkg)
	{
		string version = pkg.ReadString();
		Valheim_Build_CameraPlugin.BuildCameraCHELogger.LogInfo($"Version check, local: {Valheim_Build_CameraPlugin.ModVersion},  remote: {version}");
		if (version != Valheim_Build_CameraPlugin.ModVersion)
		{
			if (!ZNet.instance.IsServer())
			{
				Valheim_Build_CameraPlugin.ConnectionError =
					$"{Valheim_Build_CameraPlugin.ModName} Installed: {Valheim_Build_CameraPlugin.ModVersion}\n Needed: {version}";
				return;
			}
			Valheim_Build_CameraPlugin.BuildCameraCHELogger.LogWarning(
				$"Peer ({rpc.m_socket.GetHostName()}) has incompatible version, disconnecting");
			rpc.Invoke("Error", 3);
		}
		else
		{
			if (!ZNet.instance.IsServer())
			{
				Valheim_Build_CameraPlugin.ConnectionError = "";
				Valheim_Build_CameraPlugin.BuildCameraCHELogger.LogInfo(
					"Received same version from server!");
			}
			else
			{
				Valheim_Build_CameraPlugin.BuildCameraCHELogger.LogInfo(
					$"Adding peer ({rpc.m_socket.GetHostName()}) to validated list");
				ValidatedPeers.Add(rpc);
			}
		}
	}
}
