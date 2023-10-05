# Changelog

<details><summary>Current Mod Changelogs</summary>

* 1.2.1
  * Update for Valheim 0.217.22 
* 1.2.0
    * Fix compatibility with my [FirstPersonMode](https://valheim.thunderstore.io/package/Azumatt/FirstPersonMode/) mod
* 1.1.3
    * Fix the default for Move_With_Respect_To_World to be off, like it was in the original mod.

* 1.1.0 / 1.1.1 / 1.1.2

    * Add ServerSync to the mod
        * This is meant to prevent exploiting. The mod will now version check with itself and the server. If the server
          is not running the same version as the client, the client will not be able to connect to the server.
            * This doesn't prevent you from using the mod on only the client
    * Update README in v1.1.1
    * Fix a fuckup in v1.1.2. Forgot to change the csjproj file to reflect having ServerSync

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
    * Fix camera panning (i.e. movement) speed: mousewheel does not change panning speed. Panning speed is about the
      same as walking speed. Hold shift to speed up. Add configuration option to change speed.
    * Don't allow looking so far up or down that camera is now upside down.
    * Camera turn speed respects user's Invert Mouse and Mouse Sensitivity options.
    * When entering build mode, we reset the view direction of the build camera, so that it matches the player's current
      view direction.
    * Add configurable option Move_With_Respect_To_World: When true, camera panning input (e.g. pressing WASD) moves the
      camera with respect to the world coordinates, not current camera view direction.
    * Don't move camera when user is in the menu, chat, etc.
    * Change Camera_Range_Multiplier default to 1 to provide an experience as close to vanilla as possible.
    * When the config option Verbose_Logging is true, explain 3 reasons why build mode is not activated.
* Version 1.1
    * Fix: don't only show the sky.
* Version 1.0.0.0
    * Initial release.

</details>