using System;
using System.Reflection;
using BepInEx.Bootstrap;
using UnityEngine;

namespace Valheim_Build_Camera.Compatibility.WardIsLove;

internal static class WardAccess
{
	private const string PluginGuid = "Azumatt.WardIsLove";
	private static readonly System.Version MinimumVersion = new System.Version(2, 3, 3);

	private delegate bool CheckAccessHandler(long playerId, Vector3 point, float radius, bool flash);

	private static readonly bool WardIsLoveLoaded = Chainloader.PluginInfos.TryGetValue(PluginGuid, out BepInEx.PluginInfo plugin) && plugin.Metadata.Version.CompareTo(MinimumVersion) >= 0;
	private static readonly CheckAccessHandler? WardIsLoveCheck = WardIsLoveLoaded ? ResolveWardIsLoveCheck() : null;

	internal static bool Check(long playerId, Vector3 point)
	{
		if (!WardIsLoveLoaded) return PrivateArea.CheckAccess(point, flash: false, wardCheck: true);
		return WardIsLoveCheck?.Invoke(playerId, point, 0f, false) ?? false;
	}

	private static CheckAccessHandler? ResolveWardIsLoveCheck()
	{
		try
		{
			Type? type = Type.GetType("WardIsLove.Util.CustomCheck, WardIsLove", false);
			MethodInfo? method = type?.GetMethod("CheckAccess", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(long), typeof(Vector3), typeof(float), typeof(bool) }, null);
			return method == null ? null : (CheckAccessHandler)Delegate.CreateDelegate(typeof(CheckAccessHandler), method);
		}
		catch
		{
			return null;
		}
	}
}
