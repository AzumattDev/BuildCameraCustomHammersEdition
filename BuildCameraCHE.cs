using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

// Possible TODOS:

// maybe? don't open the map when in build mode.

// don't move camera when build menu is open and mouse is moving around.

// disallow and deactivate when swimming

// deactivate when taking damage

namespace Valheim_Build_Camera;

[BepInPlugin(ModGUID, ModName, VERSION)]
public class Valheim_Build_CameraPlugin : BaseUnityPlugin
{
    internal const string ModName = "BuildCameraCHE";
    internal const string ModVersion = "1.0.0";
    internal const string Author = "Azumatt";
    private const string ModGUID = Author + "." + ModName;
    private readonly Harmony _harmony = new(ModGUID);
    private const string VERSION = "1.0.0";
    private static string ConfigFileName = ModGUID + ".cfg";
    private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

    public static readonly ManualLogSource BuildCameraCHELogger =
        BepInEx.Logging.Logger.CreateLogSource(ModName);

    // This is how we "add" member variables to a class of the game.
    internal static Dictionary<Player, bool> inBuildMode = new();

    internal struct BuildCameraView
    {
        /// <summary>
        /// Turns the view left/right.
        /// </summary>
        public float yaw;

        /// <summary>
        /// Turns the view up/down.
        /// </summary>
        public float pitch;
    }

    /// <summary>
    /// A crafting station (e.g. workbench) near the player.
    /// </summary>
    internal struct NearbyCraftingStation
    {
        public Vector3 position;

        // Distance from the player
        public float distance;

        // The valid build range for this specific crafting station. Apparently
        // each station may have a different build range.
        public float rangeBuild;
    }

    /// <summary>
    /// The current pitch and yaw of the build camera.
    /// </summary>
    internal static BuildCameraView buildCameraViewDirection = new() { pitch = 0, yaw = 0 };

    public enum Toggle
    {
        On = 1,
        Off = 0
    }

    void Awake()
    {
        distanceCanBuildFromAvatar = Config.Bind("General", "Distance Can Build From Avatar", 100f,
            "Distance from your avatar that you can build or repair. (Valheim default is 8)");

        distanceCanBuildFromWorkbench
            = Config.Bind("General", "Distance Can Build From Workbench", 100f,
                "Distance from nearest workbench/stonecutter/etc. that you can build or repair. (Valheim default is 20)");

        cameraRangeMultiplier
            = Config.Bind("General", "Camera Range Multiplier", 1f,
                "Changes maximum range camera can move away from the build station. 1 means the build station's" +
                " range, 2 means twice the build station range, etc.");

        cameraMoveSpeedMultiplier
            = Config.Bind("General", "Camera Move Speed Multiplier", 3f,
                "Multiplies the speed at which the build camera pans (i.e. moves around).");

        moveWithRespectToWorld
            = Config.Bind("General", "Move With Respect To World", Toggle.On,
                "When true, camera panning input (e.g. pressing WASD) moves the camera with respect to the " +
                "world coordinates. This means that turning the camera has no effect on the direction of " +
                "movement. For example, pressing W will always move the camera toward the world's 'North', " +
                "as opposed to the direction the camera is currently facing.");

        toggleBuildMode =
            Config.Bind("Hotkeys", "Toggle build mode", new KeyboardShortcut(KeyCode.B),
                "See https://docs.unity3d.com/ScriptReference/KeyCode.html for the names of all key codes. To " +
                "add one or more modifier keys, separate them with +, like so: Toggle build mode = B + LeftControl");

        verboseLogging = Config.Bind("General", "Verbose Logging", Toggle.Off,
            "When true, increases verbosity of logging. Enable this if you're wondering why you're unable " +
            "to enable the Build Camera.");


        BuildCameraCHELogger.LogInfo(
            "Thank you to everyone who supported this mod on Github. I (Azumatt) will maintain this mod for as long as I can. Shoutout to the original devs and the git contributors. I hope you enjoy this mod!");

        Assembly assembly = Assembly.GetExecutingAssembly();
        _harmony.PatchAll(assembly);
        SetupWatcher();
    }

    private void OnDestroy()
    {
        Config.Save();
    }

    private void SetupWatcher()
    {
        FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
        watcher.Changed += ReadConfigValues;
        watcher.Created += ReadConfigValues;
        watcher.Renamed += ReadConfigValues;
        watcher.IncludeSubdirectories = true;
        watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        watcher.EnableRaisingEvents = true;
    }

    private void ReadConfigValues(object sender, FileSystemEventArgs e)
    {
        if (!File.Exists(ConfigFileFullPath)) return;
        try
        {
            BuildCameraCHELogger.LogDebug("ReadConfigValues called");
            Config.Reload();
        }
        catch
        {
            BuildCameraCHELogger.LogError($"There was an issue loading your {ConfigFileName}");
            BuildCameraCHELogger.LogError("Please check your config entries for spelling and format!");
        }
    }

    #region ConfigOptions

    internal static ConfigEntry<float> distanceCanBuildFromAvatar;
    internal static ConfigEntry<float> distanceCanBuildFromWorkbench;
    internal static ConfigEntry<float> cameraRangeMultiplier;
    internal static ConfigEntry<float> cameraMoveSpeedMultiplier;
    internal static ConfigEntry<Toggle> moveWithRespectToWorld;
    internal static ConfigEntry<KeyboardShortcut> toggleBuildMode;
    internal static ConfigEntry<Toggle> verboseLogging;

    #endregion
}