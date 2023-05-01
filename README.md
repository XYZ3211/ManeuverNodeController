# Maneuver Node Controller
 Provides an interface to finely tune maneuver nodes. Enable the GUI with ALT + N when controlling a vessel.
 
![Maneuver Node Controller Screenshot](https://github.com/schlosrat/ManeuverNodeController/blob/main/Images/MNC-Banner1.png)

## Compatibility
* Tested with Kerbal Space Program 2 v0.1.2.0.22258 & SpaceWarp 1.1.3
* Requires [SpaceWarp 1.0.1+](https://spacedock.info/mod/3277/Space%20Warp%20+%20BepInEx)
* Requires [Node Manager 0.5.3+](https://spacedock.info/mod/3366/Node%20Manager)

# Installation Instructions
1. Download and extract BepInEx mod loader with SpaceWarp 1.0.1 or later (see link above) into your game folder and run the game, then close it. If you've done this before, you can skip this step. If you've installed the game via Steam, then this is probably here: `C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program 2`. If you complete this step correctly you'll have a **BepInEx** subfolder in that directory along with the following files (in addition to what was there before): **changelog.txt, doorstop_config.ini, winhttp.dll**
1. Install Node Manager 0.5.2 or later (see link above). From the NodeManager-x.x.x.zip file copy the `BepInEx` folder on top of your game's install folder. If done correctly, you should have the following folder structure within your KSP2 game folder: `...\Kerbal Space Program 2\BepInEx\plugins\node_manager`.
1. Download and extract this mod into the game folder. From the FlightPlan-x.x.x.zip file copy the `BepInEx` folder on top of your game's install folder. If done correctly, you should have the following folder structure within your KSP2 game folder: `...\Kerbal Space Program 2\BepInEx\plugins\maneuver_node_controller`.

# License
Maneuver Node Controller and its originally authored code is distributed under the CC BY-SA 4.0 license.
Some dependencies, including MechJebLib, are dual licensed under the GNU General Public License, Version2 and MIT License. Consult the top level comment of these files for more information.
* https://creativecommons.org/licenses/by-sa/4.0/
* https://www.gnu.org/licenses/old-licenses/gpl-2.0.en.html
* https://opensource.org/license/mit/

# Attribution
GUI code and `.csproj` file based off Lazy Orbit by https://github.com/Halbann.  
Extensive refactor and new v0.8.0 features contributed by https://github.com/schlosrat and https://github.com/highfly1000.
