
`Version checks with itself. If installed on the server, it will kick clients who do not have it installed.`

`This mod uses ServerSync, if installed on the server and all clients, it will sync all configs to client`

`This mod uses a file watcher. If the configuration file is not changed with BepInEx Configuration manager, but changed in the file directly on the server, upon file save, it will sync the changes to all clients.`


Fork from [Build Camera](https://github.com/gittywithexcitement/ValheimBuildCamera) (CookieMilk's version specifically)

# Usage

1. Equip your hammer,
2. Go within build range of a build station (e.g. a workbench),
3. Press B (default - can be changed in config) to activate the build camera.
4. The camera is now disconnected from your avatar. Look around and move as usual (e.g. using mouse and keyboard); use jump (Space) to go up and stealth (Control) to go down. Hold run (Shift) to move the camera faster. Gamepad users: left trigger and right trigger move the camera up and down. Right joystick turns the camera. Left joystick pans the camera left, right, forward, and backward.
5. Build (left click) and choose items to build (right click) as usual.

## Other details

  * This mod changes how far from a work station you're able to build to several times the game default, see the configuration options Distance_Can_Build_From_Avatar and Distance_Can_Build_From_Workbench. There was much demand for this feature.
  * Deactivate build mode by unequipping the hammer or pressing B or R (the keybind for "hide" weapons).
  * Also works with the hoe and the cultivator.
  * The camera must stay within the build area, although the range is configurable with Camera_Range_Multiplier.

# Installation

1. Install [BepInEx for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
2. Place `BuildCameraCHE.dll` in your BepInEx plugins directory, like this: `Steam\steamapps\common\Valheim\BepInEx\plugins\BuildCameraCHE.dll`.

# Configuration

Start the game with the plugin installed, then edit the file `\BepInEx\config\Azumatt.BuildCameraCHE.cfg`. There are several configurable options:

  * Distance Can Build From Avatar
  * Distance Can Build From Workbench
  * Toggle build mode hotkey
  * Camera Move Speed Multiplier
  * Camera Range Multiplier
  * Move With Respect To World
  * Verbose Logging
  
# Incompatible with

  * Valheim Plus first person mode. Sorry, I'm not sure how to make these compatible, both mods are taking over the camera.
  * Masa's FirstPerson mod, on R2ModManager

I've been told Build Camera is compatible with with kailen37's FirstPerson mod on nexusmods.com.

# Changelog

<details><summary>Current Mod Changelogs</summary>

* 1.1.0 / 1.1.1

   * Add ServerSync to the mod
      * This is meant to prevent exploiting. The mod will now version check with itself and the server. If the server is not running the same version as the client, the client will not be able to connect to the server.
        * This doesn't prevent you from using the mod on only the client 
   * Update README in v1.1.1

* 1.0.0

  * Initial release
    * Forked from CookieMilk's version of Build Camera
    * Updated to add FileWatcher to the code for live direct file changes.

</details>


<details><summary>Original Mod Changelogs</summary>

 * Version 1.6.3
    * Added automatic detection of tool (Thanks MSchmoecker!!)
 * Version 1.6.2
    * Added support for custom hammers.
 * Version 1.6.1
    * Fix camera's controller up and down movement.
 * Version 1.6
    * Rebuild for Hearth and Home update.
    * Change build camera's controller up and down movement to reuse same buttons as controller jump and crouch.
    * Add support for ImprovedHammer from BuildIt mod.
 * Version 1.5.1
    * Reduce spam when changing the two build distances (now uses LogDebug).
    * When changing the two build distances, be a little more agressive: change if current value is less than setting.
 * Version 1.5
    * Change build distances: "distance can build from avatar" and "distance can build from workbench"
    * Fix gamepad joystick.
 * Version 1.4
    * Stop ignoring the "Hide equipped tool/weapon" hotkey; allow it to put the tool away (and disable build camera).
 * Version 1.3
    * Build Mode is usable with Hoe and Cultivator.
    * Don't turn build camera when user has piece selection HUD visible.
 * Version 1.2
    * Fix camera panning (i.e. movement) speed: mousewheel does not change panning speed. Panning speed is about the same as walking speed. Hold shift to speed up. Add configuration option to change speed.
    * Don't allow looking so far up or down that camera is now upside down.
    * Camera turn speed respects user's Invert Mouse and Mouse Sensitivity options.
    * When entering build mode, we reset the view direction of the build camera, so that it matches the player's current view direction.
    * Add configurable option Move_With_Respect_To_World: When true, camera panning input (e.g. pressing WASD) moves the camera with respect to the world coordinates, not current camera view direction.
    * Don't move camera when user is in the menu, chat, etc.
    * Change Camera_Range_Multiplier default to 1 to provide an experience as close to vanilla as possible.
    * When the config option Verbose_Logging is true, explain 3 reasons why build mode is not activated.
 * Version 1.1
    * Fix: don't only show the sky.
 * Version 1.0.0.0
    * Initial release.

</details>

# Source code

Original source can be found at https://github.com/gittywithexcitement/ValheimBuildCamera.

Current source can be found at https://github.com/AzumattDev/BuildCameraCustomHammersEdition

Contributions are welcome!

# Acknowledgements
All previous contributors to the original mod, remakes, or other!


### Current Mod Maintainer

---


`Feel free to reach out to me on discord if you need manual download assistance.`

### Azumatt

`DISCORD:` Azumatt#2625

`STEAM:` https://steamcommunity.com/id/azumatt/

For Questions or Comments, find me in the Odin Plus Team Discord or in mine:

[![https://i.imgur.com/XXP6HCU.png](https://i.imgur.com/XXP6HCU.png)](https://discord.gg/Pb6bVMnFb2)
<a href="https://discord.gg/pdHgy6Bsng"><img src="https://i.imgur.com/Xlcbmm9.png" href="https://discord.gg/pdHgy6Bsng" width="175" height="175"></a>
