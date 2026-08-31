`Version checks with itself. If installed on the server, it will kick clients who do not have it installed.`

`This mod uses ServerSync, if installed on the server and all clients, it will sync all configs to client`

`This mod uses a file watcher. If the configuration file is not changed with BepInEx Configuration manager, but changed in the file directly on the server, upon file save, it will sync the changes to all clients.`

Fork from [Build Camera](https://github.com/gittywithexcitement/ValheimBuildCamera) (CookieMilk's version specifically)

# Usage

1. Equip your hammer,
2. Go within build range of a build station (e.g. a workbench),
3. Press B (default - can be changed in config) to activate the build camera.
4. The camera is now disconnected from your avatar. Look around and move as usual (e.g. using mouse and keyboard); use
   jump (Space) to go up and stealth (Control) to go down. Hold run (Shift) to move the camera faster. Gamepad users:
   left trigger and right trigger move the camera up and down. Right joystick turns the camera. Left joystick pans the
   camera left, right, forward, and backward.
5. Build (left click) and choose items to build (right click) as usual.

## Other details

* Build Camera uses its own configurable station range. It does not expand workbench effect areas or suppress raids
  outside the station's real player-base radius.
* The camera cannot cross terrain. HearthBelow cave floors, walls, and ceilings use the same collision path.
* Camera entry or camera item pickup can require coziness and a minimum comfort level.
* An equipped Dvergr circlet can follow the detached camera, including CircletExtended and RaziCirclet custom slots.
  The camera copy keeps the equipped light's settings unless its config overrides them.
* Blocked pickups use a configurable HUD panel with Valheim's font and build-menu styling.
* Deactivate build mode by unequipping the hammer or pressing B or R (the keybind for "hide" weapons).
* Also works with the hoe and the cultivator.
* The camera must stay within the build area, although the range is configurable with Camera_Range_Multiplier.

# Installation

1. Install [BepInEx for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
2. Place `BuildCameraCHE.dll` in your BepInEx plugins directory, like
   this: `Steam\steamapps\common\Valheim\BepInEx\plugins\BuildCameraCHE.dll`.

# Configuration

Start the game with the plugin installed, then edit the file `\BepInEx\config\Azumatt.BuildCameraCHE.cfg`. There are
several configurable options:

* Distance Can Build From Avatar
* Distance Can Build From Workbench
* Toggle build mode hotkey
* Camera Move Speed Multiplier
* Camera Range Multiplier
* Camera Terrain Clearance
* Restriction Mode
* Minimum Comfort Level
* Follow Dvergr Circlet Light
* Camera Light Forward/Back Offset
* Camera Light Up/Down Offset
* Camera Light Left/Right Offset
* Camera Light Intensity Multiplier
* Camera Light Range Multiplier
* Camera Light Color
* Camera Light Shadows
* Pickup Blocked HUD Position and Font Size
* Move With Respect To World
* Verbose Logging

# Incompatible with

* Valheim Plus first person mode. Sorry, ValheimPlus would have to provide compatibility in their mod, both mods are
  taking over the camera.
* Masa's FirstPerson mod, on R2ModManager

# Compatible with

* My [FirstPersonMode](https://valheim.thunderstore.io/package/Azumatt/FirstPersonMode/) mod, on [Thunderstore's Valheim Community mods](https://valheim.thunderstore.io/)
* HearthBelow
* CircletExtended and RaziCirclet
* I've been told Build Camera is compatible with with kailen37's FirstPerson mod on nexusmods.com. I haven't tested it
  myself.

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
