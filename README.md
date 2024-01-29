# Maneuver Node Controller
 Provides an interface to finely tune maneuver nodes. Enable the GUI with ALT + N when controlling a vessel.
 
![Maneuver Node Controller Screenshot](https://github.com/schlosrat/ManeuverNodeController/blob/main/Images/MNC-Banner1.png)

## Compatibility
* Tested with Kerbal Space Program 2 v0.2.0, SpaceWarp 1.8.1, and UITK For KSP2 2.4.2
* Requires [SpaceWarp 1.8.0+](https://spacedock.info/mod/3277/Space%20Warp%20+%20BepInEx)
* Requires [Node Manager 0.7.1+](https://spacedock.info/mod/3366/Node%20Manager)

# Installation Instructions
## CKAN
## Manual Installation
1. Download and extract BepInEx mod loader with SpaceWarp (see link above) into your game folder and run the game, then close it. If you've done this before, you can skip this step. If you've installed the game via Steam, then this is probably here: `C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program 2`. If you complete this step correctly you'll have a **BepInEx** subfolder in that directory along with the following files (in addition to what was there before): **changelog.txt, doorstop_config.ini, winhttp.dll**
1. Install Node Manager (see link above). From the NodeManager-x.x.x.zip file copy the `BepInEx` folder on top of your game's install folder. If done correctly, you should have the following folder structure within your KSP2 game folder: `...\Kerbal Space Program 2\BepInEx\plugins\node_manager`.
1. Download and extract this mod into the game folder. From the FlightPlan-x.x.x.zip file copy the `BepInEx` folder on top of your game's install folder. If done correctly, you should have the following folder structure within your KSP2 game folder: `...\Kerbal Space Program 2\BepInEx\plugins\maneuver_node_controller`.

# Features
## Post-Node Event Lookahead
[![Watch the video](https://img.youtube.com/vi/Y8UYwdgtOhE/default.jpg)](https://youtu.be/Y8UYwdgtOhE)

# License
Maneuver Node Controller and its originally authored code are distributed under the CC BY-SA 4.0 license.
Some dependencies, including MechJebLib, are dually licensed under the GNU General Public License, Version 2, and MIT License. Consult the top-level comment of these files for more information.
* https://creativecommons.org/licenses/by-sa/4.0/
* https://www.gnu.org/licenses/old-licenses/gpl-2.0.en.html
* https://opensource.org/license/mit/

# Attribution
GUI code and `.csproj` file based on Lazy Orbit by https://github.com/Halbann.  
Extensive refactoring and new v0.8.0 features contributed by https://github.com/schlosrat and https://github.com/highfly1000.
