using System;
using UnityEngine;

namespace Valheim_Build_Camera.Compatibility.WardIsLove;

public class CustomCheck : ModCompat
{
    public static Type ClassType()
    {
        return Type.GetType("WardIsLove.Util.CustomCheck, WardIsLove");
    }

    public static bool CheckAccess(long playerID, Vector3 point, float radius = 0f, bool flash = true)
    {
        return InvokeMethod<bool>(ClassType(), null, "CheckAccess", new object[] { playerID, point, radius, flash });
    }
}