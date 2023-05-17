using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using KSP.Game;
using KSP.Map;
using KSP.Messages;
using KSP.Sim.impl;
using KSP.Sim.Maneuver;
using KSP.UI.Binding;
using ManeuverNodeController.UI;
using MNCUtilities;
using NodeManager;
using SpaceWarp;
using SpaceWarp.API.Assets;
using SpaceWarp.API.Mods;
using SpaceWarp.API.UI;
using SpaceWarp.API.UI.Appbar;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace ManeuverNodeController;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
[BepInDependency(NodeManagerPlugin.ModGuid, NodeManagerPlugin.ModVer)]
public class ManeuverNodeControllerMod : BaseSpaceWarpPlugin
{
    public static ManeuverNodeControllerMod Instance { get; set; }

    // These are useful in case some other mod wants to add a dependency to this one
    public const string ModGuid = MyPluginInfo.PLUGIN_GUID;
    public const string ModName = MyPluginInfo.PLUGIN_NAME;
    public const string ModVer = MyPluginInfo.PLUGIN_VERSION;

    // Control game input state while user has clicked into a TextField.
    private bool gameInputState = true;
    public List<String> inputFields = new List<String>();
    
    // GUI stuff
    static bool loaded = false;
    private bool interfaceEnabled = false;
    private bool GUIenabled = true;
    private Rect windowRect;
    private int windowWidth = Screen.width / 5; //384px on 1920x1080
    private int windowHeight = Screen.height / 3; //360px on 1920x1080
    //private string progradeString = "0";
    //private string normalString = "0";
    //private string radialString = "0";
    //private string absoluteValueString = "0";
    //private string smallStepString = "5";
    //private string bigStepString = "25";
    //private string timeSmallStepString = "5";
    //private string timeLargeStepString = "25";
    // private double absoluteValue, smallStep, bigStep, timeSmallStep, timeLargeStep;
    private bool pAbs, pInc1, pInc2, pDec1, pDec2, nAbs, nInc1, nInc2, nDec1, nDec2, rAbs, rInc1, rInc2, rDec1, rDec2, timeInc1, timeInc2, timeDec1, timeDec2, orbitInc, orbitDec;
    private bool snapToAp, snapToPe, snapToANe, snapToDNe, snapToANt, snapToDNt, addNode, delNode, decNode, incNode;
    private bool advancedMode, spitNode;

    private SimulationObjectModel currentTarget;
    private ManeuverNodeData thisNode = null;
    private Vector3d burnParams;
    private PatchedConicsOrbit orbit;

    private GameInstance game;

    internal int SelectedNodeIndex = 0;
    
    // App bar button(s)
    private const string ToolbarFlightButtonID = "BTN-ManeuverNodeControllerFlight";
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

		MNCSettings.Init(SettingsPath);

        Instance = this;

        game = GameManager.Instance.Game;
        Logger = base.Logger;

        // SubscribeToMessages();

        // Subscribe to messages that indicate it's OK to raise the GUI
        // StateChanges.FlightViewEntered += message => GUIenabled = true;
        // StateChanges.Map3DViewEntered += message => GUIenabled = true;

        // Subscribe to messages that indicate it's not OK to raise the GUI
        // StateChanges.FlightViewLeft += message => GUIenabled = false;
        // StateChanges.Map3DViewLeft += message => GUIenabled = false;
        // StateChanges.VehicleAssemblyBuilderEntered += message => GUIenabled = false;
        // StateChanges.KerbalSpaceCenterStateEntered += message => GUIenabled = false;
        // StateChanges.BaseAssemblyEditorEntered += message => GUIenabled = false;
        // StateChanges.MainMenuStateEntered += message => GUIenabled = false;
        // StateChanges.ColonyViewEntered += message => GUIenabled = false;
        // StateChanges.TrainingCenterEntered += message => GUIenabled = false;
        // StateChanges.MissionControlEntered += message => GUIenabled = false;
        // StateChanges.TrackingStationEntered += message => GUIenabled = false;
        // StateChanges.ResearchAndDevelopmentEntered += message => GUIenabled = false;
        // StateChanges.LaunchpadEntered += message => GUIenabled = false;
        // StateChanges.RunwayEntered += message => GUIenabled = false;

        GameManager.Instance.Game.Messages.Subscribe<ManeuverRemovedMessage>(msg =>
        {
            var message = (ManeuverRemovedMessage)msg;
            OnManeuverRemovedMessage(message);
        });

        //GameManager.Instance.Game.Messages.Subscribe<ManeuverCreatedMessage>(msg =>
        //{
        //    var message = (ManeuverCreatedMessage)msg;
        //    OnManeuverCreatedMessage(message);
        //});

        // Setup the list of input field names associated with TextField GUI inputs
        inputFields.Add("Prograde ∆v");
        inputFields.Add("Normal ∆v");
        inputFields.Add("Radial ∆v");
        inputFields.Add("Absolute ∆v");
        inputFields.Add("Small Step ∆v");
        inputFields.Add("Large Step ∆v");
        inputFields.Add("Small Time Step");
        inputFields.Add("Large Time Step");

        Logger.LogInfo("Loaded");
        if (loaded)
        {
            Destroy(this);
        }
        loaded = true;

        gameObject.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(gameObject);

        // _spaceWarpUISkin = Skins.ConsoleSkin;

        // horizontalDivider.fixedHeight = 2;
        // horizontalDivider.margin = new RectOffset(0, 0, 4, 4);

        // Register Flight AppBar button
        Appbar.RegisterAppButton(
            "Maneuver Node Cont.",
            ToolbarFlightButtonID,
            AssetManager.GetAsset<Texture2D>($"{SpaceWarpMetadata.ModID}/images/icon.png"),
            ToggleButton);
            
        // Register all Harmony patches in the project
        Harmony.CreateAndPatchAll(typeof(ManeuverNodeControllerMod).Assembly);
    }

    /// <summary>
    /// Subscribe to Messages KSP2 is using
    /// </summary>
    //private void SubscribeToMessages()
    //{
    //    MNCUtility.RefreshGameManager();

    //    // While in OAB we use the VesselDeltaVCalculationMessage event to refresh data as it's triggered a lot less frequently than Update()
    //    // Utility.MessageCenter.Subscribe<VesselDeltaVCalculationMessage>(new Action<MessageCenterMessage>(this.RefreshStagingDataOAB));

    //    // We are loading layout state when entering Flight or OAB game state
    //    // Utility.MessageCenter.Subscribe<GameStateEnteredMessage>(new Action<MessageCenterMessage>(this.GameStateEntered));

    //    // We are saving layout state when exiting from Flight or OAB game state
    //    // Utility.MessageCenter.Subscribe<GameStateLeftMessage>(new Action<MessageCenterMessage>(this.GameStateLeft));

    //    // Resets node index
    //    MNCUtility.MessageCenter.Subscribe<ManeuverRemovedMessage>(new Action<MessageCenterMessage>(this.OnManeuverRemovedMessage));
    //}

    private void OnManeuverRemovedMessage(MessageCenterMessage message)
    {
        // Update the lsit of nodes to capture the effect of the node deletion
        var nodeCount = NodeManagerPlugin.Instance.RefreshManeuverNodes();
        if (nodeCount == 0)
            SelectedNodeIndex = 0;
        if (SelectedNodeIndex + 1 > nodeCount && nodeCount > 0)
            SelectedNodeIndex = nodeCount - 1;
    }

    //private void OnManeuverCreatedMessage(MessageCenterMessage message)
    //{
    //    var nodeCount = NodeManagerPlugin.Instance.RefreshManeuverNodes();
    //    // SelectedNodeIndex = 0;
    //}

    private void ToggleButton(bool toggle)
    {
        interfaceEnabled = toggle;
        GameObject.Find(ToolbarFlightButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(interfaceEnabled);
    }

    public void LaunchMNC()
    {
        ToggleButton(true);
    }

    void Awake()
    {
        windowRect = new Rect((Screen.width * 0.7f) - (windowWidth / 2), (Screen.height / 2) - (windowHeight / 2), 0, 0);
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.N))
        {
            ToggleButton(!interfaceEnabled);
            Logger.LogInfo("Update: UI toggled with hotkey");
        }
    }

    void save_rect_pos()
    {
        MNCSettings.window_x_pos = (int)windowRect.xMin;
        MNCSettings.window_y_pos = (int)windowRect.yMin;
    }

    /// <summary>
    /// Draws a simple UI window when <code>this._isWindowOpen</code> is set to <code>true</code>.
    /// </summary>
    void OnGUI()
    {
        GUIenabled = false;
        var gameState = Game?.GlobalGameState?.GetState();
        if (gameState == GameState.Map3DView) GUIenabled = true;
        if (gameState == GameState.FlightView) GUIenabled = true;

        MNCUtility.RefreshActiveVesselAndCurrentManeuver();

        currentTarget = MNCUtility.activeVessel?.TargetObject;
        if (MNCUtility.activeVessel != null)
            orbit = MNCUtility.activeVessel.Orbit;

        // Set the UI
        if (interfaceEnabled && GUIenabled && MNCUtility.activeVessel != null)
        {
        	MNCStyles.Init();
        	UI.UIWindow.check_main_window_pos(ref windowRect);
            GUI.skin = MNCStyles.skin;
            windowRect = GUILayout.Window(
                GUIUtility.GetControlID(FocusType.Passive),
                windowRect,
                FillWindow,
                "<color=#696DFF>MANEUVER NODE CONTROLLER</color>",
                GUILayout.Height(windowHeight),
                GUILayout.Width(windowWidth));

            save_rect_pos();

            // Draw the tool tip if needed
            ToolTipsManager.DrawToolTips();
            
            // check editor focus and unset Input if needed
            UI_Fields.CheckEditor();
        }
    }

    private void FillWindow(int windowID)
    {
        TopButtons.Init(windowRect.width);
        if ( TopButtons.IconButton(MNCStyles.cross))
            CloseWindow();
            
        // Add a MNC icon to the upper left corner of the GUI
        GUI.Label(new Rect(9, 2, 29, 29), MNCStyles.icon, MNCStyles.icons_label);

        double UT;
        double dvRemaining;
        bool doButtons = true;

        if (NodeManagerPlugin.Instance.Nodes.Count == 0)
            NodeManagerPlugin.Instance.RefreshManeuverNodes();
        if (NodeManagerPlugin.Instance.Nodes.Count > 0)
        {
            //if (SelectedNodeIndex >= NodeControl.Nodes.Count)
            //    SelectedNodeIndex = NodeControl.Nodes.Count - 1;
            try { thisNode = NodeManagerPlugin.Instance.Nodes[SelectedNodeIndex]; }
            catch
            {
                Logger.LogWarning($"OnGUI: NodeControl.Nodes.Count = {NodeManagerPlugin.Instance.Nodes.Count}");
                Logger.LogWarning($"OnGUI: SelectedNodeIndex = {SelectedNodeIndex} > (Nodes.Count - 1)!");
                thisNode = null;
                doButtons = false;
            }
        }
        else
        {
            SelectedNodeIndex = 0;
            thisNode = null;
        }

        GUILayout.BeginVertical();
        if (thisNode == null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("The active vessel has no maneuver nodes.", MNCStyles.error);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            addNode = GUILayout.Button("Add Node", GUILayout.Width(windowWidth / 4));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            handleButtons();
        }
        else
        {
            DrawEntry2Button($"Node: {(SelectedNodeIndex + 1)} of {NodeManagerPlugin.Instance.Nodes.Count}", MNCStyles.label, ref decNode, "<", ref incNode, ">");
            Draw2Button(ref delNode, "Del Node", ref addNode, "Add Node");
#if DEBUG
            DrawButton(ref spitNode, "Check Node");
#endif
            GUILayout.Box("", MNCStyles.horizontalDivider);

            DrawEntry("Total Maneuver ∆v", thisNode.BurnRequiredDV.ToString("n2"), MNCStyles.label, "m/s");
            if (SelectedNodeIndex == 0)
                dvRemaining = (MNCUtility.activeVessel.Orbiter.ManeuverPlanSolver.GetVelocityAfterFirstManeuver(out UT).vector - orbit.GetOrbitalVelocityAtUTZup(UT)).magnitude;
            else
                dvRemaining = thisNode.BurnRequiredDV;
            UT = game.UniverseModel.UniversalTime;
            DrawEntry("∆v Remaining", dvRemaining.ToString("n2"), MNCStyles.label, "m/s");
            string start = MNCUtility.SecondsToTimeString(thisNode.Time - UT, false);
            string duration = MNCUtility.SecondsToTimeString(thisNode.BurnDuration);
            if (thisNode.Time < UT)
                Draw2LEntries("Start", "Duration", MNCStyles.label, start, duration, "", false, MNCStyles.error);
            else if (thisNode.Time < UT + 30)
                Draw2LEntries("Start", "Duration", MNCStyles.label, start, duration, "", false, MNCStyles.warning);
            else
                Draw2LEntries("Start", "Duration", MNCStyles.label, start, duration, "", false);
            GUILayout.Box("", MNCStyles.horizontalDivider);
            DrawEntry("Prograde ∆v", thisNode.BurnVector.z.ToString("n2"), MNCStyles.progradeStyle, "m/s");
            DrawEntry("Normal ∆v", thisNode.BurnVector.y.ToString("n2"), MNCStyles.normalStyle, "m/s");
            DrawEntry("Radial ∆v", thisNode.BurnVector.x.ToString("n2"), MNCStyles.radialStyle, "m/s");
            GUILayout.Box("", MNCStyles.horizontalDivider);
            if (advancedMode)
            {
                //advancedMode not yet enabled
                drawAdvancedMode();
            }
            else
            {
                drawSimpleMode();
            }

            DrawGUIStatus();

            if (doButtons)
                handleButtons();
        }
        GUILayout.EndVertical();
        GUI.DragWindow(new Rect(0, 0, 10000, 500));
    }

    private void drawAdvancedMode()
    {
        burnParams.z = DrawEntryTextField("Prograde ∆v", burnParams.z, "m/s"); // was: ref burnParams.z
        // double.TryParse(progradeString, out burnParams.z);
        burnParams.y = DrawEntryTextField("Normal ∆v", burnParams.y, "m/s"); // was: ref burnParams.z
        // double.TryParse(normalString, out burnParams.y);
        burnParams.x = DrawEntryTextField("Radial ∆v", burnParams.x, "m/s"); // was: ref burnParams.z
        // double.TryParse(radialString, out burnParams.x);
        if (GUILayout.Button("Apply Changes to Node"))
        {
            ManeuverNodeData nodeData = GameManager.Instance.Game.SpaceSimulation.Maneuvers.GetNodesForVessel(GameManager.Instance.Game.ViewController.GetActiveVehicle(true).Guid)[0];
            game.UniverseModel.FindVesselComponent(nodeData.RelatedSimID)?.SimulationObject.FindComponent<ManeuverPlanComponent>().UpdateChangeOnNode(nodeData, burnParams);
            game.UniverseModel.FindVesselComponent(nodeData.RelatedSimID)?.SimulationObject.FindComponent<ManeuverPlanComponent>().RefreshManeuverNodeState(0);
            Logger.LogInfo($"drawAdvancedMode: {nodeData.ToString()}");
        }
    }

    private void drawSimpleMode()
    {
        string nextApA, nextPeA, nextInc, nextEcc, nextLAN, previousApA, previousPeA, previousInc, previousEcc, previousLAN;

        MNCSettings.absolute_value = DrawEntryTextField("Absolute ∆v", MNCSettings.absolute_value, "m/s"); // was: ref absoluteValue
        // double.TryParse(absoluteValueString, out absoluteValue);
        MNCSettings.small_step = DrawEntryTextField("Small Step ∆v", MNCSettings.small_step, "m/s"); // was: ref smallStep
        // double.TryParse(smallStepString, out smallStep);
        MNCSettings.big_step = DrawEntryTextField("Large Step ∆v", MNCSettings.big_step, "m/s"); // was: ref bigStep
        // double.TryParse(bigStepString, out bigStep);
        GUILayout.Box("", MNCStyles.horizontalDivider);
        DrawEntry5Button("Prograde", MNCStyles.progradeStyle, ref pDec2, "<<", ref pDec1, "<", ref pInc1, ">", ref pInc2, ">>", ref pAbs, "Abs");
        DrawEntry5Button("Normal", MNCStyles.normalStyle, ref nDec2, "<<", ref nDec1, "<", ref nInc1, ">", ref nInc2, ">>", ref nAbs, "Abs");
        DrawEntry5Button("Radial", MNCStyles.radialStyle, ref rDec2, "<<", ref rDec1, "<", ref rInc1, ">", ref rInc2, ">>", ref rAbs, "Abs");
        GUILayout.Box("", MNCStyles.horizontalDivider);
        GUILayout.Box("", MNCStyles.horizontalDivider);
        SnapSelectionGUI();
        GUILayout.Box("", MNCStyles.horizontalDivider);
        MNCSettings.time_small_step = DrawEntryTextField("Small Time Step", MNCSettings.time_small_step, "s"); // was: ref timeSmallStep
        // double.TryParse(timeSmallStepString, out timeSmallStep);
        MNCSettings.time_large_step = DrawEntryTextField("Large Time Step", MNCSettings.time_large_step, "s"); // was: ref timeLargeStep
        // double.TryParse(timeLargeStepString, out timeLargeStep);
        GUILayout.Box("", MNCStyles.horizontalDivider);
        DrawEntry4Button("Time", MNCStyles.label, ref timeDec2, "<<", ref timeDec1, "<", ref timeInc1, ">", ref timeInc2, ">>");
        GUILayout.Box("", MNCStyles.horizontalDivider);
        var numOrbits = Math.Truncate((thisNode.Time - game.UniverseModel.UniversalTime) / game.UniverseModel.FindVesselComponent(thisNode.RelatedSimID).Orbit.period).ToString("n0");
        DrawEntry2Button("+/- Orbitial Period", MNCStyles.label, ref orbitDec, "-", ref orbitInc, "+");
        DrawEntry("Maneuver Node in", $"{numOrbits} orbit(s)");
        GUILayout.Box("", MNCStyles.horizontalDivider);
        Draw2Entries("Previous Orbit", "Next Orbit", MNCStyles.label, "", false);

        var Orbiter = MNCUtility.activeVessel.Orbiter;
        var ManeuverPlanSolver = Orbiter?.ManeuverPlanSolver;
        var PatchedConicsList = ManeuverPlanSolver?.PatchedConicsList;
        // ManeuverPlanComponent activeVesselPlan = Utility.activeVessel?.SimulationObject?.FindComponent<ManeuverPlanComponent>();
        // var nodes = activeVesselPlan?.GetNodes();

        if (NodeManagerPlugin.Instance.Nodes.Count == 0) // No nodes: Just show current orbit - shouldn't ever get here...
        {
            if (orbit.eccentricity < 1)
                previousApA = (orbit.ApoapsisArl / 1000).ToString("n3");
            else
                previousApA = "Inf";
            previousPeA = (orbit.PeriapsisArl / 1000).ToString("n3");
            previousInc = orbit.inclination.ToString("n3");
            previousEcc = orbit.eccentricity.ToString("n3");
            previousLAN = orbit.longitudeOfAscendingNode.ToString("n3");
            nextApA = previousApA;
            nextPeA = previousPeA;
            nextInc = previousInc;
            nextEcc = previousEcc;
            nextLAN = previousLAN;
        }
        else
        {
            // Get the patch info and index for the patch that contains this node
            // ManeuverPlanSolver.FindPatchContainingUt(NodeManagerPlugin.Instance.Nodes[SelectedNodeIndex].Time, PatchedConicsList, out var patch, out var patchIndex);
            var nodeTime = NodeManagerPlugin.Instance.Nodes[SelectedNodeIndex].Time;
            PatchedConicsOrbit patch = null;
            int patchIdx = 0;
            if (nodeTime < PatchedConicsList[0].StartUT)
            {
                patchIdx = (PatchedConicsList[0].PatchEndTransition == KSP.Sim.PatchTransitionType.Encounter) ? 1 : 0;
                patch = PatchedConicsList[patchIdx];
            }
            else
            {
                for (int i = 0; i < PatchedConicsList.Count - 1; i++)
                {
                    if (PatchedConicsList[i].StartUT < nodeTime && nodeTime <= PatchedConicsList[i + 1].StartUT)
                    {
                        patchIdx = (PatchedConicsList[i + 1].PatchEndTransition == KSP.Sim.PatchTransitionType.Encounter && i < PatchedConicsList.Count - 2) ? i + 2 : i + 1;
                        patch = PatchedConicsList[patchIdx];
                        break;
                    }
                }
            }
            if (patch == null)
            {
                patch = PatchedConicsList.Last();
                patchIdx = PatchedConicsList.Count - 1;
            }

            if (SelectedNodeIndex == 0) // One or more nodes, and the selected node is the first
            {
                // The previous orbit info will be from our current orbit
                if (orbit.eccentricity < 1)
                    previousApA = (orbit.ApoapsisArl / 1000).ToString("n3");
                else
                    previousApA = "Inf";
                previousPeA = (orbit.PeriapsisArl / 1000).ToString("n3");
                previousInc = orbit.inclination.ToString("n3");
                previousEcc = orbit.eccentricity.ToString("n3");
                previousLAN = orbit.longitudeOfAscendingNode.ToString("n3");

                // The next orbit info will be from PatchedConicsList[0]
                nextApA = (patch.ApoapsisArl / 1000).ToString("n3");
                nextPeA = (patch.PeriapsisArl / 1000).ToString("n3");
                nextInc = patch.inclination.ToString("n3");
                nextEcc = patch.eccentricity.ToString("n3");
                nextLAN = patch.longitudeOfAscendingNode.ToString("n3");
            }
            else // One or more nodes, and the selected node is not the first
            {
                if (patchIdx > 1)
                {
                    // The previous orbit info will be from PatchedConicsList[SelectedNodeIndex - 1]
                    previousApA = (PatchedConicsList[patchIdx - 1].ApoapsisArl / 1000).ToString("n3");
                    previousPeA = (PatchedConicsList[patchIdx - 1].PeriapsisArl / 1000).ToString("n3");
                    previousInc = PatchedConicsList[patchIdx - 1].inclination.ToString("n3");
                    previousEcc = PatchedConicsList[patchIdx - 1].eccentricity.ToString("n3");
                    previousLAN = PatchedConicsList[patchIdx - 1].longitudeOfAscendingNode.ToString("n3");
                }
                else
                {
                    // The previous orbit info will be from our current orbit
                    if (orbit.eccentricity < 1)
                        previousApA = (orbit.ApoapsisArl / 1000).ToString("n3");
                    else
                        previousApA = "Inf";
                    previousPeA = (orbit.PeriapsisArl / 1000).ToString("n3");
                    previousInc = orbit.inclination.ToString("n3");
                    previousEcc = orbit.eccentricity.ToString("n3");
                    previousLAN = orbit.longitudeOfAscendingNode.ToString("n3");
                }
                // The next orbit info will be from PatchedConicsList[SelectedNodeIndex]
                nextApA = (patch.ApoapsisArl / 1000).ToString("n3");
                nextPeA = (patch.PeriapsisArl / 1000).ToString("n3");
                nextInc = patch.inclination.ToString("n3");
                nextEcc = patch.eccentricity.ToString("n3");
                nextLAN = patch.longitudeOfAscendingNode.ToString("n3");
            }
        }

        Draw2Entries("Ap", "Ap", MNCStyles.name_label_r, previousApA, nextApA, "km", true, MNCStyles.value_label_l, MNCStyles.value_label_l);
        Draw2Entries("Pe", "Pe", MNCStyles.name_label_r, previousPeA, nextPeA, "km", true, MNCStyles.value_label_l, MNCStyles.value_label_l);
        Draw2Entries("Inc", "Inc", MNCStyles.name_label_r, previousInc, nextInc, "°", false, MNCStyles.value_label_l, MNCStyles.value_label_l);
        Draw2Entries("Ecc", "Ecc", MNCStyles.name_label_r, previousEcc, nextEcc, "", false, MNCStyles.value_label_l, MNCStyles.value_label_l);
        Draw2Entries("LAN", "LAN", MNCStyles.name_label_r, previousLAN, nextLAN, "°", false, MNCStyles.value_label_l, MNCStyles.value_label_l);
        GUILayout.Box("", MNCStyles.horizontalDivider);
    }

    private void CloseWindow()
    {
        GameObject.Find(ToolbarFlightButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(false);
        interfaceEnabled = false;
        Logger.LogDebug("CloseWindow: Restoring Game Input on window close.");
        // game.Input.Flight.Enable();
        GameManager.Instance.Game.Input.Enable();
        ToggleButton(interfaceEnabled);
    }

    //private void DrawSoloToggle(string sectionNamem, ref bool toggle)
    //{
    //    GUILayout.Space(5);
    //    GUILayout.BeginHorizontal();
    //    toggle = GUILayout.Toggle(toggle, sectionNamem, sectionToggleStyle);
    //    GUILayout.EndHorizontal();
    //    GUILayout.Space(-5);
    //}

    private void DrawEntry(string entryName, string value, GUIStyle entryStyle = null, string unit = "", bool unitSpace = true)
    {
        if (entryStyle == null)
            entryStyle = MNCStyles.name_label;
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{entryName}:", entryStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label(value, MNCStyles.value_label);
        if (unitSpace)
            GUILayout.Space(5);
        GUILayout.Label(unit, MNCStyles.unit_label);
        GUILayout.EndHorizontal();
        GUILayout.Space(MNCStyles.spacingAfterEntry);
    }

    private void Draw2Entries(string entryName1, string entryName2, GUIStyle entryStyle = null, string unit = "", bool unitSpace = true)
    {
        if (entryStyle == null)
        {
            entryStyle = MNCStyles.name_label;
            entryStyle.fixedHeight = MNCStyles.value_label.fixedHeight;
        }
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{entryName1}: ", entryStyle);
        if (unit.Length > 0)
        {
            if (unitSpace)
                GUILayout.Space(5);
            GUILayout.Label(unit, MNCStyles.unit_label);
        }
        GUILayout.FlexibleSpace();
        GUILayout.Label($"{entryName2}: ", entryStyle);
        if (unit.Length > 0)
        {
            if (unitSpace)
                GUILayout.Space(5);
            GUILayout.Label(unit, MNCStyles.unit_label);
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(MNCStyles.spacingAfterEntry);
    }

    private void Draw2Entries(string entryName1, string entryName2, GUIStyle entryStyle = null, string value1 = "", string value2 = "", string unit = "", bool unitSpace = true, GUIStyle value1Style = null, GUIStyle value2Style = null)
    {
        if (entryStyle == null)
        {
            entryStyle = MNCStyles.name_label;
            entryStyle.fixedHeight = MNCStyles.value_label.fixedHeight;
        }
        if (value1Style == null)
        {
            value1Style = MNCStyles.value_label;
        }
        if (value2Style == null)
        {
            value2Style = MNCStyles.value_label;
        }
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{entryName1}:", entryStyle, GUILayout.Width(40));
        if (value1.Length > 0 && !unitSpace)
            GUILayout.Label($"{value1}{unit}", value1Style, GUILayout.Width(90));
        if (value1.Length > 0 && unitSpace)
            GUILayout.Label($"{value1} {unit}", value1Style, GUILayout.Width(90));
        GUILayout.FlexibleSpace();
        GUILayout.Label($"{entryName2}: ", entryStyle, GUILayout.Width(40));
        if (value2.Length > 0 && !unitSpace)
            GUILayout.Label($"{value2}{unit}", value2Style, GUILayout.Width(90));
        if (value2.Length > 0 && unitSpace)
            GUILayout.Label($"{value2} {unit}", value2Style, GUILayout.Width(90));
        GUILayout.EndHorizontal();
        GUILayout.Space(MNCStyles.spacingAfterEntry);
    }

    private void Draw2LEntries(string entryName1, string entryName2, GUIStyle entryStyle = null, string value1 = "", string value2 = "", string unit = "", bool unitSpace = true, GUIStyle value1Style = null, GUIStyle value2Style = null)
    {
        if (entryStyle == null)
        {
            entryStyle = MNCStyles.name_label;
            entryStyle.fixedHeight = MNCStyles.value_label.fixedHeight;
        }
        if (value1Style == null)
        {
            value1Style = MNCStyles.value_label;
        }
        if (value2Style == null)
        {
            value2Style = MNCStyles.value_label;
        }
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{entryName1}: ", entryStyle);
        if (value1.Length > 0)
        {
            GUILayout.Space(5);
            GUILayout.Label(value1, value1Style);
            if (unit.Length > 0)
            {
                if (unitSpace)
                    GUILayout.Space(5);
                GUILayout.Label(unit, MNCStyles.unit_label);
            }
        }
        GUILayout.FlexibleSpace();
        GUILayout.Label($"{entryName2}: ", entryStyle);
        if (value2.Length > 0)
        {
            GUILayout.Space(5);
            GUILayout.Label(value2, value2Style);
            if (unit.Length > 0)
            {
                if (unitSpace)
                    GUILayout.Space(5);
                GUILayout.Label(unit, MNCStyles.unit_label);
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(MNCStyles.spacingAfterEntry);
    }

    private void DrawButton(ref bool button, string buttonStr)
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        button = GUILayout.Button(buttonStr, MNCStyles.small_btn);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(MNCStyles.spacingAfterEntry);
    }

    private void Draw2Button(ref bool button1, string buttonStr1, ref bool button2, string buttonStr2)
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        button1 = GUILayout.Button(buttonStr1, MNCStyles.small_btn);
        GUILayout.FlexibleSpace();
        button2 = GUILayout.Button(buttonStr2, MNCStyles.small_btn);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(MNCStyles.spacingAfterEntry);
    }
    private void DrawEntry2Button(string entryName, GUIStyle entryStyle, ref bool button1, string button1Str, ref bool button2, string button2Str, string value = "")
    {
        GUILayout.BeginHorizontal();
        button1 = GUILayout.Button(button1Str, MNCStyles.ctrl_button);
        GUILayout.FlexibleSpace();
        GUILayout.Label(entryName, entryStyle);
        if (value.Length > 0)
        {
            GUILayout.Space(5);
            GUILayout.Label(value, entryStyle);
        }
        GUILayout.FlexibleSpace();
        button2 = GUILayout.Button(button2Str, MNCStyles.ctrl_button);
        GUILayout.EndHorizontal();
        GUILayout.Space(MNCStyles.spacingAfterEntry);
    }

    private void Draw3Button(ref bool button1, string button1Str, ref bool button2, string button2Str, ref bool button3, string button3Str)
    {
        GUILayout.BeginHorizontal();
        button1 = GUILayout.Button(button1Str, MNCStyles.ctrl_button);
        GUILayout.FlexibleSpace();
        button2 = GUILayout.Button(button2Str, MNCStyles.ctrl_button);
        GUILayout.FlexibleSpace();
        button3 = GUILayout.Button(button3Str, MNCStyles.ctrl_button);
        GUILayout.EndHorizontal();
        GUILayout.Space(MNCStyles.spacingAfterEntry);
    }

    private void DrawEntry4Button(string entryName, GUIStyle entryStyle, ref bool button1, string button1Str, ref bool button2, string button2Str, ref bool button3, string button3Str, ref bool button4, string button4Str)
    {
        GUILayout.BeginHorizontal();
        button1 = GUILayout.Button(button1Str, MNCStyles.ctrl_button);
        GUILayout.Space(5);
        button2 = GUILayout.Button(button2Str, MNCStyles.ctrl_button);
        GUILayout.FlexibleSpace();
        GUILayout.Label(entryName, entryStyle);
        GUILayout.FlexibleSpace();
        button3 = GUILayout.Button(button3Str, MNCStyles.ctrl_button);
        GUILayout.Space(5);
        button4 = GUILayout.Button(button4Str, MNCStyles.ctrl_button);
        GUILayout.EndHorizontal();
        GUILayout.Space(MNCStyles.spacingAfterEntry);
    }

    private void DrawEntry5Button(string entryName, GUIStyle entryStyle, ref bool button1, string button1Str, ref bool button2, string button2Str, ref bool button3, string button3Str, ref bool button4, string button4Str, ref bool button5, string button5Str)
    {
        GUILayout.BeginHorizontal();
        button1 = GUILayout.Button(button1Str, MNCStyles.ctrl_button);
        GUILayout.Space(5);
        button2 = GUILayout.Button(button2Str, MNCStyles.ctrl_button);
        GUILayout.FlexibleSpace();
        GUILayout.Label(entryName, entryStyle);
        GUILayout.FlexibleSpace();
        button3 = GUILayout.Button(button3Str, MNCStyles.ctrl_button);
        GUILayout.Space(5);
        button4 = GUILayout.Button(button4Str, MNCStyles.ctrl_button);
        GUILayout.Space(5);
        button5 = GUILayout.Button(button5Str, MNCStyles.ctrl_button);
        GUILayout.EndHorizontal();
        GUILayout.Space(MNCStyles.spacingAfterEntry);
    }

    private void DrawEntryTextField(string entryName, ref string textEntry, string unit = "", bool unitSpace = true)
    {
        double num;
        Color normal;
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{entryName}: ", MNCStyles.name_label);
        normal = GUI.color;
        bool parsed = double.TryParse(textEntry, out num);
        if (!parsed) GUI.color = Color.red;
        GUI.SetNextControlName(entryName);
        textEntry = GUILayout.TextField(textEntry, MNCStyles.text_input);
        // value = UI_Fields.DoubleField(entryName, value);
        GUI.color = normal;
        if (unitSpace)
            GUILayout.Space(5);
        GUILayout.Label(unit, MNCStyles.unit_label);
        GUILayout.EndHorizontal();
        GUILayout.Space(MNCStyles.spacingAfterEntry);
    }

    private double DrawEntryTextField(string entryName, double value, string unit = "", bool unitSpace = true)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{entryName}: ", MNCStyles.name_label);
        value = UI_Fields.DoubleField(entryName, value);
        if (unitSpace)
            GUILayout.Space(5);
        GUILayout.Label(unit, MNCStyles.unit_label);
        GUILayout.EndHorizontal();
        GUILayout.Space(MNCStyles.spacingAfterEntry);
        return value;
    }

    private void DrawGUIStatus()
    {
        // Indication to User that its safe to type, or why vessel controls aren't working
        GUILayout.BeginHorizontal();
        string inputStateString = UI_Fields.GameInputState ? "<b>Enabled</b>" : "<b>Disabled</b>";
        GUILayout.Label("Game Input: ", MNCStyles.label);
        if (UI_Fields.GameInputState)
            GUILayout.Label(inputStateString, MNCStyles.label);
        else
            GUILayout.Label(inputStateString, MNCStyles.warning);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(MNCStyles.spacingAfterEntry);
    }

    // Draws the snap selection GUI.
    private void SnapSelectionGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("SnapTo: ", MNCStyles.name_label); //  GUILayout.Width(windowWidth / 5));
        snapToAp = GUILayout.Button("Ap", MNCStyles.snap_button);
        GUILayout.Space(5);
        snapToPe = GUILayout.Button("Pe", MNCStyles.snap_button);
        GUILayout.Space(5);
        snapToANe = GUILayout.Button("ANe", MNCStyles.snap_button);
        GUILayout.Space(5);
        snapToDNe = GUILayout.Button("DNe", MNCStyles.snap_button);
        if (currentTarget != null)
        {
            GUILayout.Space(5);
            snapToANt = GUILayout.Button("ANt", MNCStyles.snap_button);
            GUILayout.Space(5);
            snapToDNt = GUILayout.Button("DNt", MNCStyles.snap_button);
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(MNCStyles.spacingAfterEntry);
    }

    private void handleButtons()
    {
        // var orbit = GetLastOrbit() as PatchedConicsOrbit;

        MapCore mapCore = null;
        GameManager.Instance.Game.Map.TryGetMapCore(out mapCore);
        var m3d = mapCore.map3D;
        var maneuverManager = m3d.ManeuverManager;

        // Get the ManeuverPlanComponent for the active vessel
        ManeuverPlanComponent maneuverPlanComponent = null;
        if (MNCUtility.activeVessel != null)
        {
            // var universeModel = game.UniverseModel;
            // var vesselComponent = universeModel?.FindVesselComponent(thisNode.RelatedSimID);
            var simObject = MNCUtility.activeVessel?.SimulationObject;
            maneuverPlanComponent = simObject?.FindComponent<ManeuverPlanComponent>();
        }

        if (thisNode == null && MNCUtility.activeVessel != null)
        {
            if (addNode)
            {
                //int nodeCount;
                //var nodeCount = NodeControl.RefreshManeuverNodes();
                //Logger.LogInfo($"addNode (button): Number of nodes before add:         {nodeCount}");
                NodeManagerPlugin.Instance.AddNode();
                SelectedNodeIndex = 0;
                NodeManagerPlugin.Instance.SpitNode(SelectedNodeIndex);
                //nodeCount = NodeControl.RefreshManeuverNodes();
                //Logger.LogInfo($"addNode (button): Number of nodes after add:          {nodeCount}");
                StartCoroutine(NodeManagerPlugin.Instance.RefreshNodes());
                //nodeCount = NodeControl.RefreshManeuverNodes();
                //Logger.LogInfo($"addNode (button): Number of nodes after RefreshNodes: {nodeCount}");
                SelectedNodeIndex = 0;
            }
            return;
        }
        else if (spitNode && MNCUtility.activeVessel != null)
        {
            NodeManagerPlugin.Instance.SpitNode(SelectedNodeIndex);
        }
        else if ((pAbs || pInc1 || pInc2 || pDec1 || pDec2 || nAbs || nInc1 || nInc2 || nDec1 || nDec2 || rAbs || rInc1 || rInc2 || rDec1 || rDec2) && MNCUtility.activeVessel != null)
        {
            burnParams = Vector3d.zero;  // Burn update vector, this is added to the existing burn

            if (pAbs) // Set the prograde burn to the absoluteValue
            {
                burnParams.z = MNCSettings.absolute_value - thisNode.BurnVector.z;
                // thisNode.BurnVector.z = MNCSettings.absolute_value;
            }
            else if (pInc1) // Add smallStep to the prograde burn
            {
                burnParams.z += MNCSettings.small_step;
                // thisNode.BurnVector.z += MNCSettings.small_step;
            }
            else if (pInc2) // Add bigStep to the prograde burn
            {
                burnParams.z += MNCSettings.big_step;
                // thisNode.BurnVector.z += MNCSettings.big_step;
            }
            else if (pDec1) // Subtract smallStep from the prograde burn
            {
                burnParams.z -= MNCSettings.small_step;
                // thisNode.BurnVector.z -= MNCSettings.small_step;
            }
            else if (pDec2) // Subtract bigStep from the prograde burn
            {
                burnParams.z -= MNCSettings.big_step;
                // thisNode.BurnVector.z -= MNCSettings.big_step;
            }
            else if (nAbs) // Set the normal burn to the absoluteValue
            {
                burnParams.y = MNCSettings.absolute_value - thisNode.BurnVector.y;
                // thisNode.BurnVector.y = MNCSettings.absolute_value;
            }
            else if (nInc1) // Add smallStep to the normal burn
            {
                burnParams.y += MNCSettings.small_step;
                // thisNode.BurnVector.y += MNCSettings.small_step;
            }
            else if (nInc2) // Add bigStep to the normal burn
            {
                burnParams.y += MNCSettings.big_step;
                // thisNode.BurnVector.y += MNCSettings.big_step;
            }
            else if (nDec1) // Subtract smallStep from the normal burn
            {
                burnParams.y -= MNCSettings.small_step;
                // thisNode.BurnVector.y -= MNCSettings.small_step;
            }
            else if (nDec2) // Subtract bigStep from the normal burn
            {
                burnParams.y -= MNCSettings.big_step;
                // thisNode.BurnVector.y -= MNCSettings.big_step;
            }
            else if (rAbs) // Set the radial burn to the absoluteValue
            {
                burnParams.x = MNCSettings.absolute_value - thisNode.BurnVector.x;
                // thisNode.BurnVector.x = MNCSettings.absolute_value;
            }
            else if (rInc1) // Add smallStep to the radial burn
            {
                burnParams.x += MNCSettings.small_step;
                // thisNode.BurnVector.x += MNCSettings.small_step;
            }
            else if (rInc2) // Add bigStep to the radial burn
            {
                burnParams.x += MNCSettings.big_step;
                // thisNode.BurnVector.x += MNCSettings.big_step;
            }
            else if (rDec1) // Subtract smallStep from the radial burn
            {
                burnParams.x -= MNCSettings.small_step;
                // thisNode.BurnVector.x -= MNCSettings.small_step;
            }
            else if (rDec2) // Subtract bigStep from the radial burn
            {
                burnParams.x -= MNCSettings.big_step;
                // thisNode.BurnVector.x -= MNCSettings.big_step;
            }

            // Push the update to the node
            //Logger.LogDebug("handleButtons: Pushing new burn info to node");
            //Logger.LogDebug($"handleButtons: burnParams         [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s");
            maneuverPlanComponent.UpdateChangeOnNode(thisNode, burnParams);

            // Call RefreshNodes to update the nodes in a way that allows the game to catch up with the updates
            StartCoroutine(NodeManagerPlugin.Instance.RefreshNodes());

            Logger.LogDebug($"handleButtons: Updated BurnVector    [{thisNode.BurnVector.x:F3}, {thisNode.BurnVector.y:F3}, {thisNode.BurnVector.z:F3}] m/s");
            //Logger.LogDebug($"handleButtons: BurnVector.normalized [{thisNode.BurnVector.normalized.x}, {thisNode.BurnVector.normalized.y}, {thisNode.BurnVector.normalized.z}] m/s");

            // If we touched a later node, go touch the first node or the patch after the later node
            // may (will?) be messed up. This solves that
            //if (SelectedNodeIndex > 0)
            //{
            //    maneuverPlanComponent.UpdateTimeOnNode(NodeControl.Nodes[0], NodeControl.Nodes[0].Time);
            //    StartCoroutine(NodeManagerPlugin.Instance.RefreshNodes());
            //}

            //StartCoroutine(NodeManagerPlugin.Instance.RefreshNodes());
        }
        else if ((timeDec1 || timeDec2 || timeInc1 || timeInc2 || orbitDec || orbitInc || snapToAp || snapToPe || snapToANe || snapToDNe || snapToANt || snapToDNt) && MNCUtility.activeVessel != null)
        {
            // Get some objects and info we need
            var vessel = game.UniverseModel.FindVesselComponent(thisNode.RelatedSimID);
            var target = vessel?.TargetObject;
            var UT = game.UniverseModel.UniversalTime;
            var oldBurnTime = thisNode.Time;
            var timeOfNodeFromNow = oldBurnTime - UT;

            double nodeTime = thisNode.Time;
            double minTime = UT + Math.Max(MNCSettings.time_small_step, 5);
            double maxTime = UT - 1;
            if (SelectedNodeIndex > 0)
                minTime = NodeManagerPlugin.Instance.Nodes[SelectedNodeIndex - 1].Time + Math.Max(MNCSettings.time_small_step, 5);
            if (SelectedNodeIndex < NodeManagerPlugin.Instance.Nodes.Count - 1)
                maxTime = NodeManagerPlugin.Instance.Nodes[SelectedNodeIndex + 1].Time - Math.Max(MNCSettings.time_small_step, 5);

            // Logger.LogDebug($"SelectedNodeIndex      : {SelectedNodeIndex}");
            // for (int i= 0; i < NodeManagerPlugin.Instance.Nodes.Count; i++)
            // {
            //     var thisNodeTime = NodeManagerPlugin.Instance.Nodes[i].Time;
            //     if (i == SelectedNodeIndex)
            //         Logger.LogDebug($"nodeTime[{i}]*         : {thisNodeTime - UT} from now.");
            //     else
            //         Logger.LogDebug($"nodeTime[{i}]          : {thisNodeTime - UT} from now.");
            // }
            // Logger.LogDebug($"minTime for node adjust: {minTime - UT} from now.");
            // Logger.LogDebug($"maxTime for node adjust: {maxTime - UT} from now.");
            // Logger.LogDebug($"nodeTime               : {nodeTime - UT} from now.");

            if (timeDec1) // Subtract timeSmallStep
            {
                if (MNCSettings.time_small_step < timeOfNodeFromNow) // If there is enough time
                {
                    nodeTime -= MNCSettings.time_small_step;
                    // thisNode.Time -= MNCSettings.time_small_step;
                }
            }
            else if (timeDec2) // Subtract timeLargeStep
            {
                if (MNCSettings.time_large_step < timeOfNodeFromNow) // If there is enough time
                {
                    nodeTime -= MNCSettings.time_large_step;
                    // thisNode.Time -= MNCSettings.time_large_step;
                }
            }
            else if (timeInc1) // Add timeSmallStep
            {
                nodeTime += MNCSettings.time_small_step;
                // thisNode.Time += MNCSettings.time_small_step;
            }
            else if (timeInc2) // Add timeLargeStep
            {
                nodeTime += MNCSettings.time_large_step;
                // thisNode.Time += MNCSettings.time_large_step;
            }
            else if (orbitDec) // Subtract one orbital period
            {
                if (vessel.Orbit.period < timeOfNodeFromNow) // If there is enough time
                {
                    nodeTime -= vessel.Orbit.period;
                    // thisNode.Time -= vessel.Orbit.period;
                }
            }
            else if (orbitInc) // Add one orbital period
            {
                nodeTime += vessel.Orbit.period;
                // thisNode.Time += vessel.Orbit.period;
            }
            else if (snapToAp) // Snap the maneuver time to the next Ap
            {
                nodeTime = UT + vessel.Orbit.TimeToAp;
                // thisNode.Time = UT + vessel.Orbit.TimeToAp;
            }
            else if (snapToPe) // Snap the maneuver time to the next Pe
            {
                nodeTime = UT + vessel.Orbit.TimeToPe;
                // thisNode.Time = UT + vessel.Orbit.TimeToPe;
            }
            else if (snapToANe) // Snap the maneuver time to the AN relative to the equatorial plane
            {
                nodeTime = vessel.Orbit.TimeOfANEquatorial(UT);
                // thisNode.Time = vessel.Orbit.TimeOfANEquatorial(UT);
            }
            else if (snapToDNe) // Snap the maneuver time to the DN relative to the equatorial plane
            {
                nodeTime = vessel.Orbit.TimeOfDNEquatorial(UT);
                // thisNode.Time = vessel.Orbit.TimeOfDNEquatorial(UT);
            }
            else if (snapToANt) // Snap the maneuver time to the AN relative to selected target's orbit
            {
                nodeTime = vessel.Orbit.TimeOfAN(target.Orbit, UT);
                // thisNode.Time = vessel.Orbit.TimeOfAN(target.Orbit, UT);
            }
            else if (snapToDNt) // Snap the maneuver time to the DN relative to selected target's orbit
            {
                nodeTime = vessel.Orbit.TimeOfDN(target.Orbit, UT);
                // thisNode.Time = vessel.Orbit.TimeOfDN(target.Orbit, UT);
            }

            if (nodeTime < minTime) // Not allowed to move the node prior to anopther node
            {
                nodeTime = minTime;
                Logger.LogDebug($"Limiting nodeTime to no less than {(nodeTime - UT):F3} from now.");
            }
            if (maxTime > minTime && nodeTime > maxTime) // Not allowed to move the node ahead of a later node
            {
                nodeTime = maxTime;
                Logger.LogDebug($"Limiting nodeTime to no more than {(nodeTime - UT):F3} from now.");
            }
            Logger.LogDebug($"nodeTime after adjust  : {(nodeTime - UT):F3} from now.");

            // Push the update to the node
            // thisNode.Time = nodeTime;
            maneuverPlanComponent.UpdateTimeOnNode(thisNode, nodeTime);

            // Call RefreshNodes to update the nodes in a way that allows the game to catch up with the updates
            StartCoroutine(NodeManagerPlugin.Instance.RefreshNodes());

            // Logger.LogDebug($"handleButtons: Burn time was {oldBurnTime}, is {thisNode.Time}");

            // If we touched a later node's time, go touch the first node or the patch after the later node
            // may (will?) be messed up. This solves that
            //if (SelectedNodeIndex > 0)
            //{
            //    maneuverPlanComponent.UpdateTimeOnNode(NodeControl.Nodes[0], NodeControl.Nodes[0].Time);
            //    StartCoroutine(NodeManagerPlugin.Instance.RefreshNodes());
            //}

            //StartCoroutine(NodeManagerPlugin.Instance.RefreshNodes());
        }
        else if ((decNode || incNode || delNode || addNode) && MNCUtility.activeVessel != null)
        {
            if (decNode && SelectedNodeIndex > 0)
            {
                SelectedNodeIndex--;
            }
            else if (incNode && SelectedNodeIndex + 1 < NodeManagerPlugin.Instance.Nodes.Count)
            {
                SelectedNodeIndex++;
            }
            else if (delNode)
            {
                NodeManagerPlugin.Instance.DeleteNodes(SelectedNodeIndex);
                if (SelectedNodeIndex > 0)
                    SelectedNodeIndex--;
                else
                    SelectedNodeIndex = 0;
            }
            else if (addNode)
            {
                // int nodeCount;
                bool pass;
                // nodeCount =  NodeManagerPlugin.Instance.RefreshManeuverNodes();

                StartCoroutine(NodeManagerPlugin.Instance.RefreshNodes());

                // Logger.LogDebug($"addNode (button): Number of nodes before add:         {nodeCount}");
                pass = NodeManagerPlugin.Instance.AddNode();
                SelectedNodeIndex = NodeManagerPlugin.Instance.Nodes.Count - 1;
                NodeManagerPlugin.Instance.SpitNode(SelectedNodeIndex);
                // nodeCount = NodeManagerPlugin.Instance.RefreshManeuverNodes();
                // Logger.LogDebug($"addNode (button): Number of nodes after add:          {nodeCount}");
                // StartCoroutine(NodeManagerPlugin.Instance.RefreshNodes());
                //nodeCount = NodeControl.RefreshManeuverNodes();
                //Logger.LogInfo($"addNode (button): Number of nodes after RefreshNodes: {nodeCount}");
                // NodeManagerPlugin.Instance.SpitNode(SelectedNodeIndex);

                //StartCoroutine(NodeManagerPlugin.Instance.RefreshNodes());
                //nodeCount = NodeControl.RefreshManeuverNodes();
                //Logger.LogInfo($"addNode (button): Number of nodes after RefreshNodes: {nodeCount}");

                // thisNode = NodeControl.Nodes[SelectedNodeIndex];

            }
            //else if (addNode2) // FOR TESTING - ONLY AVAILABLE IN DEBUG BUILD
            //{
            //    int nodeCount;
            //    //nodeCount = NodeControl.RefreshManeuverNodes();
            //    //Logger.LogInfo($"addNode (button): Number of nodes before add:         {nCount}");
            //    MNCNodeControl.AddNode(orbit);
            //    SelectedNodeIndex = MNCNodeControl.Nodes.Count - 1;
            //    MNCNodeControl.SpitNode(SelectedNodeIndex);
            //    //nodeCount = NodeControl.RefreshManeuverNodes();
            //    //Logger.LogInfo($"addNode (button): Number of nodes after add:          {nodeCount}");
            //    StartCoroutine(NodeManagerPlugin.Instance.RefreshNodes());
            //    //nodeCount = NodeControl.RefreshManeuverNodes();
            //    //Logger.LogInfo($"addNode (button): Number of nodes after RefreshNodes: {nodeCount}");
            //    // NodeManagerPlugin.Instance.SpitNode(SelectedNodeIndex);

            //    //StartCoroutine(NodeManagerPlugin.Instance.RefreshNodes());
            //    //nodeCount = NodeControl.RefreshManeuverNodes();
            //    //Logger.LogInfo($"addNode (button): Number of nodes after RefreshNodes: {nodeCount}");

            //    // thisNode = NodeControl.Nodes[SelectedNodeIndex];
            //}

        }
    }
    
    private IEnumerator RefreshNodes(ManeuverPlanComponent maneuverPlanComponent)
    {
        // yield return (object)new WaitForFixedUpdate();

        for (int i = 0; i < NodeManagerPlugin.Instance.Nodes.Count; i++) // was i = SelectedNodeIndex
        {
            // Logger.LogDebug($"RefreshNodes: Updateing Node {i}");
            var node = NodeManagerPlugin.Instance.Nodes[i];
            // maneuverPlanComponent.UpdateTimeOnNode(node, node.Time);
            maneuverPlanComponent.UpdateNodeDetails(node);
            //yield return (object)new WaitForFixedUpdate();
            //maneuverPlanComponent.RefreshManeuverNodeState(i);
        }

        for (int i = 0; i < NodeManagerPlugin.Instance.Nodes.Count; i++) // was i = SelectedNodeIndex
        {
            // Logger.LogDebug($"RefreshNodes: Refreshing Node {i}");
            try { maneuverPlanComponent.RefreshManeuverNodeState(i); }
            catch (NullReferenceException e)
            {
                Logger.LogError($"RefreshNodes: Suppressed NRE for Node {i}: {e}");
                Logger.LogError($"RefreshNodes: Node {i}: {NodeManagerPlugin.Instance.Nodes[i]}");
            }
        }

        yield return (object)new WaitForFixedUpdate();
        // NodeControl.RefreshManeuverNodes();
        // yield return (object)new WaitForFixedUpdate();

        for (int i = 0; i < NodeManagerPlugin.Instance.Nodes.Count; i++) // was i = SelectedNodeIndex
        {
            // Logger.LogDebug($"RefreshNodes: Updateing Node {i}");
            var node = NodeManagerPlugin.Instance.Nodes[i];
            // maneuverPlanComponent.UpdateTimeOnNode(node, node.Time);
            maneuverPlanComponent.UpdateNodeDetails(node);
            //yield return (object)new WaitForFixedUpdate();
            //maneuverPlanComponent.RefreshManeuverNodeState(i);
        }

        for (int i = 0; i < NodeManagerPlugin.Instance.Nodes.Count; i++) // was i = SelectedNodeIndex
        {
            // Logger.LogDebug($"RefreshNodes: Refreshing Node {i}");
            try { maneuverPlanComponent.RefreshManeuverNodeState(i); }
            catch (NullReferenceException e)
            {
                Logger.LogError($"RefreshNodes: Suppressed NRE for Node {i}: {e}");
                Logger.LogError($"RefreshNodes: Node {i}: {NodeManagerPlugin.Instance.Nodes[i]}");
            }
        }

        // yield return (object)new WaitForFixedUpdate();

        NodeManagerPlugin.Instance.RefreshManeuverNodes();
    }
}   
