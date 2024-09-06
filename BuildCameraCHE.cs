using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using System.Reflection;
using BepInEx.Logging;
using JetBrains.Annotations;
using ServerSync;
using UnityEngine;

// Possible TODOS:

// maybe? don't open the map when in build mode.

// don't move camera when build menu is open and mouse is moving around.

// disallow and deactivate when swimming

// deactivate when taking damage

namespace Valheim_Build_Camera;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class Valheim_Build_CameraPlugin : BaseUnityPlugin
{
    internal const string ModName = "BuildCameraCHE";
    internal const string ModVersion = "1.2.4";
    internal const string Author = "Azumatt";
    private const string ModGUID = Author + "." + ModName;
    private readonly Harmony _harmony = new(ModGUID);
    private static string ConfigFileName = ModGUID + ".cfg";
    private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
    internal static string ConnectionError = "";
    public static readonly ManualLogSource BuildCameraCHELogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

    private static readonly ConfigSync ConfigSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

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
        _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On,
            "If on, the configuration is locked and can be changed by server admins only.");
        _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

        distanceCanBuildFromAvatar = config("General", "Distance Can Build From Avatar", 100f,
            "Distance from your avatar that you can build or repair. (Valheim default is 8)");

        distanceCanBuildFromWorkbench
            = config("General", "Distance Can Build From Workbench", 100f,
                "Distance from nearest workbench/stonecutter/etc. that you can build or repair. (Valheim default is 20)");
        
        resourcePickupRange
            = config("General", "Resource Pickup Range", 10f,
                "Distance from which you can pick up resources on the ground while in build mode. (Valheim default is 2)");

        cameraRangeMultiplier
            = config("General", "Camera Range Multiplier", 1f,
                "Changes maximum range camera can move away from the build station. 1 means the build station's" +
                " range, 2 means twice the build station range, etc.");

        cameraMoveSpeedMultiplier
            = config("General", "Camera Move Speed Multiplier", 3f,
                "Multiplies the speed at which the build camera pans (i.e. moves around).");

        moveWithRespectToWorld
            = config("General", "Move With Respect To World", Toggle.Off,
                "When true, camera panning input (e.g. pressing WASD) moves the camera with respect to the " +
                "world coordinates. This means that turning the camera has no effect on the direction of " +
                "movement. For example, pressing W will always move the camera toward the world's 'North', " +
                "as opposed to the direction the camera is currently facing.");

        toggleBuildMode =
            config("Hotkeys", "Toggle build mode", new KeyboardShortcut(KeyCode.B),
                "See https://docs.unity3d.com/ScriptReference/KeyCode.html for the names of all key codes. To " +
                "add one or more modifier keys, separate them with +, like so: Toggle build mode = B + LeftControl",
                false);

        verboseLogging = config("General", "Verbose Logging", Toggle.Off,
            "When true, increases verbosity of logging. Enable this if you're wondering why you're unable " +
            "to enable the Build Camera.", false);


        BuildCameraCHELogger.LogMessage("Thank you to everyone who supported this mod on Github. I (Azumatt) will maintain this mod for as long as I can. Shoutout to the original devs and the git contributors. I hope you enjoy this mod!");

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

    private static ConfigEntry<Toggle> _serverConfigLocked = null!;
    internal static ConfigEntry<float> distanceCanBuildFromAvatar = null!;
    internal static ConfigEntry<float> distanceCanBuildFromWorkbench = null!;
    internal static ConfigEntry<float> resourcePickupRange = null!;
    internal static ConfigEntry<float> cameraRangeMultiplier = null!;
    internal static ConfigEntry<float> cameraMoveSpeedMultiplier = null!;
    internal static ConfigEntry<Toggle> moveWithRespectToWorld = null!;
    internal static ConfigEntry<KeyboardShortcut> toggleBuildMode = null!;
    internal static ConfigEntry<Toggle> verboseLogging = null!;

    private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
        bool synchronizedSetting = true)
    {
        ConfigDescription extendedDescription =
            new(
                description.Description +
                (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                description.AcceptableValues, description.Tags);
        ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
        //var configEntry = Config.Bind(group, name, value, description);

        SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

        return configEntry;
    }

    private ConfigEntry<T> config<T>(string group, string name, T value, string description,
        bool synchronizedSetting = true)
    {
        return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
    }

    private class ConfigurationManagerAttributes
    {
        [UsedImplicitly] public int? Order = null!;
        [UsedImplicitly] public bool? Browsable = null!;
        [UsedImplicitly] public string? Category = null!;
        [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer = null!;
    }

    class AcceptableShortcuts : AcceptableValueBase
    {
        public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
        {
        }

        public override object Clamp(object value) => value;
        public override bool IsValid(object value) => true;

        public override string ToDescriptionString() =>
            "# Acceptable values: " + string.Join(", ", UnityInput.Current.SupportedKeyCodes);
    }

    #endregion
}