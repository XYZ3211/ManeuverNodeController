using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using KSP.Game;
using KSP.Messages;
using KSP.Sim.impl;
using KSP.UI.Binding;
using NodeManager;
using SpaceWarp;
using SpaceWarp.API.Assets;
using SpaceWarp.API.Mods;
using SpaceWarp.API.UI.Appbar;
using System.Reflection;
using UitkForKsp2.API;
using UnityEngine;
using UnityEngine.UIElements;
using UitkForKsp2;
using BepInEx.Configuration;
using JetBrains.Annotations;

namespace ManeuverNodeController;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
[BepInDependency(UitkForKsp2Plugin.ModGuid, UitkForKsp2Plugin.ModVer)]
[BepInDependency(NodeManagerPlugin.ModGuid, NodeManagerPlugin.ModVer)]
public class ManeuverNodeControllerMod : BaseSpaceWarpPlugin
{
    public static ManeuverNodeControllerMod Instance { get; private set; }

    // These are useful in case some other mod wants to add a dependency to this one
    [PublicAPI] public const string ModGuid = MyPluginInfo.PLUGIN_GUID;
    [PublicAPI] public const string ModName = MyPluginInfo.PLUGIN_NAME;
    [PublicAPI] public const string ModVer = MyPluginInfo.PLUGIN_VERSION;

    // Control game input state while user has clicked into a TextField.
    // private bool gameInputState = true;
    // public List<String> inputFields = new List<String>();

    // GUI stuff
    private static bool loaded = false;
    private bool interfaceEnabled = false;
    private bool GUIenabled = true;

    private ConfigEntry<KeyboardShortcut> _keybind;
    private ConfigEntry<KeyboardShortcut> _keybind2;
    public ConfigEntry<bool> previousNextEnable;
    public ConfigEntry<bool> postNodeEventLookahead;
    public ConfigEntry<bool> autoLaunch;
    public ConfigEntry<bool> autoClose;
    public ConfigEntry<double> autoCloseDelay;
    public ConfigEntry<long> repeatDelay;
    public ConfigEntry<long> repeatInterval;
    public bool forceOpen = false;

    public enum PatchEventType
    {
        StartOfPatch,
        MidPatch,
        EndOfPatch
    }

    public SimulationObjectModel currentTarget;

    // private GameInstance game;

    internal int SelectedNodeIndex = 0;

    private MncUiController controller;

    // App bar button(s)
    public static string ToolbarFlightButtonID = "BTN-ManeuverNodeControllerFlight";
    // private const string ToolbarOABButtonID = "BTN-ManeuverNodeControllerOAB";

    private static string _assemblyFolder;
    private static string AssemblyFolder =>
        _assemblyFolder ?? (_assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

    private static string _settingsPath;
    private static string SettingsPath =>
        _settingsPath ?? (_settingsPath = Path.Combine(AssemblyFolder, "settings.json"));

    //public ManualLogSource logger;
    public new static ManualLogSource Logger { get; set; }

    /// <summary>
    /// Runs when the mod is first initialized.
    /// </summary>
    public override void OnInitialized()
    {
        base.OnInitialized();

        // MNCSettings.Init(SettingsPath);

        Instance = this;

        // game = GameManager.Instance.Game;
        Logger = base.Logger;

        // GameManager.Instance.Game.Messages.Subscribe<ManeuverRemovedMessage>(msg =>
        Game.Messages.Subscribe<ManeuverRemovedMessage>(msg =>
        {
            MessageCenterMessage message = (ManeuverRemovedMessage)msg;
            OnManeuverRemovedMessage(message);
        });

        // GameManager.Instance.Game.Messages.Subscribe<ManeuverCreatedMessage>(msg =>
        Game.Messages.Subscribe<ManeuverCreatedMessage>(msg =>
        {
            MessageCenterMessage message = (ManeuverCreatedMessage)msg;
            OnManeuverCreatedMessage(message);
        });

        var mncUxml = AssetManager.GetAsset<VisualTreeAsset>($"{Info.Metadata.GUID}/mnc_ui/mnc_ui.uxml");
        var mncWindow = Window.CreateFromUxml(mncUxml, "Maneuver Node Controller Main Window", transform, true);
        mncWindow.hideFlags |= HideFlags.HideAndDontSave;

        controller = mncWindow.gameObject.AddComponent<MncUiController>();

        _keybind = Config.Bind(
        new ConfigDefinition("Keybindings", "First Keybind"),
        new KeyboardShortcut(KeyCode.N, KeyCode.LeftAlt),
        new ConfigDescription("Keybind to open mod window")
        );

        _keybind2 = Config.Bind(
        new ConfigDefinition("Keybindings", "Second Keybind"),
        new KeyboardShortcut(KeyCode.N, KeyCode.RightAlt, KeyCode.AltGr),
        new ConfigDescription("Keybind to open mod window")
        );

        //GameManager.Instance.Game.Messages.Subscribe<ManeuverRemovedMessage>(msg =>
        //{
        //    var message = (ManeuverRemovedMessage)msg;
        //    OnManeuverRemovedMessage(message);
        //});

        Logger.LogInfo("Loaded");
        if (loaded)
        {
            Destroy(this);
        }
        loaded = true;

        // Register Flight AppBar button
        Appbar.RegisterAppButton(
            "Maneuver Node Cont.",
            ToolbarFlightButtonID,
            AssetManager.GetAsset<Texture2D>($"{Info.Metadata.GUID}/images/icon.png"),
            ToggleButton);

        // Register all Harmony patches in the project
        Harmony.CreateAndPatchAll(typeof(ManeuverNodeControllerMod).Assembly);
        // Harmony.CreateAndPatchAll(typeof(STFUPatches));

        previousNextEnable = Config.Bind<bool>("Features Section", "Previous / Next Orbit Display", true, "Enable/Disable the display of the PRevious Obrit / Next Orbit information block");
        postNodeEventLookahead = Config.Bind<bool>("Features Section", "Post-Node Event Lookahead", true, "Enable/Disable the display of the Post-Node Event Lookahead information block");
        autoLaunch = Config.Bind<bool>("Control Section", "Automatic Launch Enable", false, "Enable/Disable automatically raising the Maneuver Node Controler GUI when nodes are created");
        autoClose = Config.Bind<bool>("Control Section", "Automatic Shutdown Enable", false, "Enable/Disable automatic dismissal of the Maneuver Node Controller GUI when there are no nodes");
        autoCloseDelay = Config.Bind<double>("Control Section", "Automatic Shutdown Delay", 10, "Seconds after predicted end of last node burn at which to trigger Automatic Shutdown");
        repeatDelay = Config.Bind<long>("Control Section", "Button Repeat Delay", 1000, "Milliseconds delay before button repeat commences when button is held down");
        repeatInterval = Config.Bind<long>("Control Section", "Button Repeat Interval", 100, "Milliseconds between application of repeated button effect when button is held down");
    }

    private void OnManeuverRemovedMessage(MessageCenterMessage message)
    {
        // Update the list of nodes to capture the effect of the node deletion
        var nodeCount = NodeManagerPlugin.Instance.RefreshManeuverNodes();

        // If there are no nodes remaining
        if (nodeCount == 0)
        {
            SelectedNodeIndex = 0;
            if (autoClose.Value)
            {
                Logger.LogInfo("Automatically closing Maneuver Node Controller due to autoClose == true and nodeCount == 0");
                ToggleButton(false);
            }
        }
        else
        {
            // If the SelectedNodeIndex points to a node index not in the current list
            if (SelectedNodeIndex + 1 > nodeCount)
                SelectedNodeIndex = nodeCount - 1;

            bool keepGui = false;
            double ut = Game.UniverseModel.UniverseTime;
            for (int i = 0; i < nodeCount; i++)
            {
                if (NodeManagerPlugin.Instance.Nodes[i].Time + NodeManagerPlugin.Instance.Nodes[i].BurnDuration + autoCloseDelay.Value > ut)
                {
                    Logger.LogInfo($"Node[{i}].Time + Node[{i}].BurnDuration + {autoCloseDelay.Value} > UT: Setting keepGui = true");
                    keepGui = true;
                }
                else
                    Logger.LogInfo($"Node[{i}].Time + Node[{i}].BurnDuration + {autoCloseDelay.Value} <= UT");
            }
            if (autoClose.Value && !keepGui)
                ToggleButton(false);
        }
    }

    private void OnManeuverCreatedMessage(MessageCenterMessage message)
    {
        // If we're configured to automatically launch when a node is created
        if (autoLaunch.Value)
        {
            ToggleButton(true);
            if (NodeManagerPlugin.Instance.Nodes.Count == 0)
                forceOpen = true;
        }
    }

    public void ToggleButton(bool toggle)
    {
        interfaceEnabled = toggle;
        GameObject.Find(ToolbarFlightButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(interfaceEnabled);
        controller.SetEnabled(toggle);
        if (NodeManagerPlugin.Instance.Nodes.Count == 0)
            forceOpen = true;
    }

    public void LaunchMNC()
    {
        ToggleButton(true);
        if (NodeManagerPlugin.Instance.Nodes.Count == 0)
            forceOpen = true;
    }

    private void Awake()
    {
        // windowRect = new Rect((Screen.width * 0.7f) - (windowWidth / 2), (Screen.height / 2) - (windowHeight / 2), 0, 0);
    }

    private void Update()
    {
        if ((_keybind != null && _keybind.Value.IsDown()) || (_keybind2 != null && _keybind2.Value.IsDown()))
        {
            ToggleButton(!interfaceEnabled);
            if (interfaceEnabled) forceOpen = true;
            if (_keybind != null && _keybind.Value.IsDown())
                Logger.LogDebug($"Update: UI toggled with _keybind, hotkey {_keybind.Value}");
            if (_keybind2 != null && _keybind2.Value.IsDown())
                Logger.LogDebug($"Update: UI toggled with _keybind2, hotkey {_keybind2.Value}");
        }

        // Stuff from OnGUI that we still need to do at least once each frame
        GUIenabled = false;
        var gameState = Game?.GlobalGameState?.GetState();
        if (gameState == GameState.Map3DView) GUIenabled = true;
        if (gameState == GameState.FlightView) GUIenabled = true;

        MncUtility.RefreshActiveVesselAndCurrentManeuver();

        currentTarget = MncUtility.activeVessel?.TargetObject;
        //if (MNCUtility.activeVessel != null)
        //  orbit = MNCUtility.activeVessel.Orbit;

        if (autoClose != null)
        {
            if (autoClose.Value)
            {
                if (!forceOpen)
                {
                    bool keepGui = false;
                    for (int i = 0; i < NodeManagerPlugin.Instance.Nodes.Count; i++)
                        if (NodeManagerPlugin.Instance.Nodes[i].Time + NodeManagerPlugin.Instance.Nodes[i].BurnDuration + 10 > Game.UniverseModel.UniverseTime)
                            keepGui = true;
                    if (!keepGui)
                        ToggleButton(false);
                }
                if (forceOpen && NodeManagerPlugin.Instance.Nodes.Count > 0)
                    forceOpen = false;
            }
        }
    }
}