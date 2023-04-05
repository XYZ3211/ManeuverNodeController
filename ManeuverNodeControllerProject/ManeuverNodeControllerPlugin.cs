using BepInEx;
using UnityEngine;
using KSP.Game;
using KSP.Sim.impl;
using KSP.Sim.Maneuver;
using SpaceWarp;
using SpaceWarp.API.Mods;
using SpaceWarp.API.Assets;
using SpaceWarp.API.UI;
using SpaceWarp.API.UI.Appbar;
using KSP.UI.Binding;
using KSP.Sim;
using KSP.Map;
using MuMech;
using System.Collections;

namespace ManeuverNodeController;

[BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
[BepInPlugin("com.github.schlosrat.maneuvernodecontroller", "Maneuver Node Controller", "0.7.0")]
public class ManeuverNodeControllerMod : BaseSpaceWarpPlugin
{
    private static ManeuverNodeControllerMod Instance { get; set; }

    static bool loaded = false;
    private bool interfaceEnabled = false;
    private Rect windowRect;
    private int windowWidth = Screen.width / 5; //384px on 1920x1080
    private int windowHeight = Screen.height / 3; //360px on 1920x1080
    private Rect closeBtnRect;
    private string progradeString = "0";
    private string normalString = "0";
    private string radialString = "0";
    private string absoluteValueString = "0";
    private string smallStepString = "5";
    private string bigStepString = "25";
    private string timeSmallStepString = "5";
    private string timeLargeStepString = "25";
    private double absoluteValue, smallStep, bigStep, timeSmallStep, timeLargeStep;
    private bool pAbs, pInc1, pInc2, pDec1, pDec2, nAbs, nInc1, nInc2, nDec1, nDec2, rAbs, rInc1, rInc2, rDec1, rDec2, timeInc1, timeInc2, timeDec1, timeDec2, orbitInc, orbitDec;
    private bool snapToAp, snapToPe, snapToANe, snapToDNe, snapToANt, snapToDNt, addNode;
    private bool advancedMode;

    // Control game input state while user has clicked into a TextField.
    private bool gameInputState = true;
    public List<String> inputFields = new List<String>();

    private VesselComponent activeVessel;
    private SimulationObjectModel currentTarget;
    private ManeuverNodeData currentNode = null;
    List<ManeuverNodeData> activeNodes;
    private Vector3d burnParams;

    private GUIStyle errorStyle, warnStyle, progradeStyle, normalStyle, radialStyle, labelStyle;
    private GameInstance game;
    private GUIStyle horizontalDivider = new GUIStyle();
    private GUISkin _spaceWarpUISkin;
    private GUIStyle ctrlBtnStyle;
    private GUIStyle mainWindowStyle;
    private GUIStyle textInputStyle;
    private GUIStyle sectionToggleStyle;
    private GUIStyle closeBtnStyle;
    private GUIStyle nameLabelStyle;
    private GUIStyle valueLabelStyle;
    private GUIStyle unitLabelStyle;
    private string unitColorHex;
    private int spacingAfterHeader = -12;
    private int spacingAfterEntry = -12;
    private int spacingAfterSection = 5;

    // Set things up so we'll have access to mapCpre.Map3D.ManeuverManager
    MapCore mapCore = null;
    Map3DView m3d;
    Map3DManeuvers maneuverManager;

    public override void OnInitialized()
    {
        base.OnInitialized();
        game = GameManager.Instance.Game;

        // Setup the list of input field names associated with TextField GUI inputs
        inputFields.Add("Prograde dV");
        inputFields.Add("Normal dV");
        inputFields.Add("Radial dV");
        inputFields.Add("Absolute dV");
        inputFields.Add("Small Step dV");
        inputFields.Add("Large Step dV");
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

        _spaceWarpUISkin = Skins.ConsoleSkin;
        
        mainWindowStyle = new GUIStyle(_spaceWarpUISkin.window)
        {
            padding = new RectOffset(8, 8, 20, 8),
            contentOffset = new Vector2(0, -22),
            fixedWidth = windowWidth
        };

        textInputStyle = new GUIStyle(_spaceWarpUISkin.textField)
        {
            alignment = TextAnchor.LowerCenter,
            padding = new RectOffset(10, 10, 0, 0),
            contentOffset = new Vector2(0, 2),
            fixedHeight = 18,
            clipping = TextClipping.Overflow,
            margin = new RectOffset(0, 0, 10, 0)
        };

        ctrlBtnStyle = new GUIStyle(_spaceWarpUISkin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(0, 0, 0, 3),
            contentOffset = new Vector2(0, 2),
            fixedHeight = 16,
            fixedWidth = 16,
            fontSize = 16,
            clipping = TextClipping.Overflow,
            margin = new RectOffset(0, 0, 10, 0)
        };

        sectionToggleStyle = new GUIStyle(_spaceWarpUISkin.toggle)
        {
            padding = new RectOffset(14, 0, 3, 3)
        };

        nameLabelStyle = new GUIStyle(_spaceWarpUISkin.label);
        nameLabelStyle.normal.textColor = new Color(.7f, .75f, .75f, 1);

        valueLabelStyle = new GUIStyle(_spaceWarpUISkin.label)
        {
            alignment = TextAnchor.MiddleRight
        };
        valueLabelStyle.normal.textColor = new Color(.6f, .7f, 1, 1);

        unitLabelStyle = new GUIStyle(valueLabelStyle)
        {
            fixedWidth = 24,
            alignment = TextAnchor.MiddleLeft
        };
        unitLabelStyle.normal.textColor = new Color(.7f, .75f, .75f, 1);
        unitColorHex = ColorUtility.ToHtmlStringRGBA(unitLabelStyle.normal.textColor);

        closeBtnStyle = new GUIStyle(_spaceWarpUISkin.button)
        {
            fontSize = 8
        };

        closeBtnRect = new Rect(windowWidth - 23, 6, 16, 16);
        
        Appbar.RegisterAppButton(
            "Maneuver Node Cont.",
            "BTN-ManeuverNodeController",
            AssetManager.GetAsset<Texture2D>($"{SpaceWarpMetadata.ModID}/images/icon.png"),
            ToggleButton);

        //try { Game.Map.TryGetMapCore(out mapCore); }
        //catch { Logger.LogError("OnInitialized: Caught exception on call to Game.Map.TryGetMapCore(out mapCore)"); }
        //try { m3d = mapCore.map3D; }
        //catch { Logger.LogError("OnInitialized: Caught exception attempting to set m3d = mapCore.map3D)"); }
        //try { maneuverManager = m3d.ManeuverManager; }
        //catch { Logger.LogError("OnInitialized: Caught exception attempting to set maneuverManager = m3d.ManeuverManager)"); }
    }

    private void ToggleButton(bool toggle)
    {
        interfaceEnabled = toggle;
        GameObject.Find("BTN-ManeuverNodeController")?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(toggle);
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

    void OnGUI()
    {
        // Make sure we've got access to maneuverManager
        if (mapCore == null)
        {
            Game.Map.TryGetMapCore(out mapCore);
        }
        if (m3d == null)
        {
            m3d = mapCore.map3D;
        }
        if (maneuverManager == null)
        {
            maneuverManager = m3d.ManeuverManager;
        }

        activeVessel = GameManager.Instance?.Game?.ViewController?.GetActiveVehicle(true)?.GetSimVessel(true);
        currentTarget = activeVessel?.TargetObject;
        if (interfaceEnabled)
        {
            GUI.skin = Skins.ConsoleSkin;
            windowRect = GUILayout.Window(
                GUIUtility.GetControlID(FocusType.Passive),
                windowRect,
                FillWindow,
                "<color=#696DFF>// MANEUVER NODE CONTROLLER</color>",
                GUILayout.Height(windowHeight),
                GUILayout.Width(windowWidth));

            if (gameInputState && inputFields.Contains(GUI.GetNameOfFocusedControl()))
            {
                gameInputState = false;
                GameManager.Instance.Game.Input.Disable();
            }

            if (!gameInputState && !inputFields.Contains(GUI.GetNameOfFocusedControl()))
            {
                gameInputState = true;
                GameManager.Instance.Game.Input.Enable();
            }
        }
        else
        {
            if (!gameInputState)
            {
                gameInputState = true;
                GameManager.Instance.Game.Input.Enable();
            }
        }
    }

    private void FillWindow(int windowID)
    {
        if (CloseButton())
        {
            CloseWindow();
        }
        
        labelStyle = warnStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        errorStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        errorStyle.normal.textColor = Color.red;
        warnStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        warnStyle.normal.textColor = Color.yellow;
        progradeStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        progradeStyle.normal.textColor = Color.yellow;
        normalStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        normalStyle.normal.textColor = Color.magenta;
        radialStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        radialStyle.normal.textColor = Color.cyan;
        horizontalDivider.fixedHeight = 2;
        horizontalDivider.margin = new RectOffset(0, 0, 4, 4);
        // game = GameManager.Instance.Game;
        activeNodes = game.SpaceSimulation.Maneuvers.GetNodesForVessel(GameManager.Instance.Game.ViewController.GetActiveVehicle(true).Guid);
        currentNode = (activeNodes.Count() > 0) ? activeNodes[0] : null;
        double UT;
        double dvRemaining;

        GUILayout.BeginVertical();
        if (currentNode == null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("The active vessel has no maneuver nodes.", errorStyle);
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
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Total Maneuver ∆v (m/s): ");
            GUILayout.FlexibleSpace();
            GUILayout.Label(currentNode.BurnRequiredDV.ToString("n2"));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"∆v Remaining (m/s): ");
            GUILayout.FlexibleSpace();
            dvRemaining = (activeVessel.Orbiter.ManeuverPlanSolver.GetVelocityAfterFirstManeuver(out UT).vector - activeVessel.Orbit.GetOrbitalVelocityAtUTZup(UT)).magnitude;
            GUILayout.Label(dvRemaining.ToString("n3"));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Prograde ∆v (m/s): ");
            GUILayout.FlexibleSpace();
            GUILayout.Label(currentNode.BurnVector.z.ToString("n2"));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Normal ∆v (m/s): ");
            GUILayout.FlexibleSpace();
            GUILayout.Label(currentNode.BurnVector.y.ToString("n2"));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Radial ∆v (m/s): ");
            GUILayout.FlexibleSpace();
            GUILayout.Label(currentNode.BurnVector.x.ToString("n2"));
            GUILayout.EndHorizontal();
            GUILayout.Box("", horizontalDivider);
            if (advancedMode)
            {
                //advancedMode not yet enabled
                drawAdvancedMode();
            }
            else
            {
                drawSimpleMode();
            }

            GUILayout.BeginHorizontal();
            string inputStateString = gameInputState ? "Enabled" : "Disabled";
            GUILayout.Label($"Game Input: {inputStateString}");
            GUILayout.EndHorizontal();

            handleButtons();
        }
        GUILayout.EndVertical();
        GUI.DragWindow(new Rect(0, 0, 10000, 500));
    }

    private void drawAdvancedMode()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Prograde ∆v (m/s): ", GUILayout.Width(windowWidth / 2));
        GUI.SetNextControlName("Prograde dV");
        progradeString = GUILayout.TextField(progradeString, progradeStyle);
        double.TryParse(progradeString, out burnParams.z);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Normal ∆v (m/s): ", GUILayout.Width(windowWidth / 2));
        GUI.SetNextControlName("Normal dV");
        normalString = GUILayout.TextField(normalString, normalStyle);
        double.TryParse(normalString, out burnParams.y);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Radial ∆v (m/s): ", GUILayout.Width(windowWidth / 2));
        GUI.SetNextControlName("Radial dV");
        radialString = GUILayout.TextField(radialString, radialStyle);
        double.TryParse(radialString, out burnParams.x);
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Apply Changes to Node"))
        {
            ManeuverNodeData nodeData = GameManager.Instance.Game.SpaceSimulation.Maneuvers.GetNodesForVessel(GameManager.Instance.Game.ViewController.GetActiveVehicle(true).Guid)[0];
            //nodeData.BurnVector = burnParams;
            game.UniverseModel.FindVesselComponent(nodeData.RelatedSimID)?.SimulationObject.FindComponent<ManeuverPlanComponent>().UpdateChangeOnNode(nodeData, burnParams);
            game.UniverseModel.FindVesselComponent(nodeData.RelatedSimID)?.SimulationObject.FindComponent<ManeuverPlanComponent>().RefreshManeuverNodeState(0);
            Logger.LogInfo($"drawAdvancedMode: {nodeData.ToString()}");
        }
    }

    private void drawSimpleMode()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Absolute ∆v (m/s): ", GUILayout.Width(windowWidth / 2));
        GUI.SetNextControlName("Absolute dV");
        absoluteValueString = GUILayout.TextField(absoluteValueString);
        double.TryParse(absoluteValueString, out absoluteValue);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Small Step ∆v (m/s): ", GUILayout.Width(windowWidth / 2));
        GUI.SetNextControlName("Small Step dV");
        smallStepString = GUILayout.TextField(smallStepString);
        double.TryParse(smallStepString, out smallStep);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Large Step ∆v (m/s): ", GUILayout.Width(windowWidth / 2));
        GUI.SetNextControlName("Large Step dV");
        bigStepString = GUILayout.TextField(bigStepString);
        double.TryParse(bigStepString, out bigStep);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        pDec2 = GUILayout.Button("<<", GUILayout.Width(windowWidth / 9));
        pDec1 = GUILayout.Button("<", GUILayout.Width(windowWidth / 9));
        GUILayout.FlexibleSpace();
        GUILayout.Label("Prograde", progradeStyle);
        GUILayout.FlexibleSpace();
        pInc1 = GUILayout.Button(">", GUILayout.Width(windowWidth / 9));
        pInc2 = GUILayout.Button(">>", GUILayout.Width(windowWidth / 9));
        pAbs = GUILayout.Button("Abs", GUILayout.Width(windowWidth / 9));
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        nDec2 = GUILayout.Button("<<", GUILayout.Width(windowWidth / 9));
        nDec1 = GUILayout.Button("<", GUILayout.Width(windowWidth / 9));
        GUILayout.FlexibleSpace();
        GUILayout.Label("Normal", normalStyle);
        GUILayout.FlexibleSpace();
        nInc1 = GUILayout.Button(">", GUILayout.Width(windowWidth / 9));
        nInc2 = GUILayout.Button(">>", GUILayout.Width(windowWidth / 9));
        nAbs = GUILayout.Button("Abs", GUILayout.Width(windowWidth / 9));
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        rDec2 = GUILayout.Button("<<", GUILayout.Width(windowWidth / 9));
        rDec1 = GUILayout.Button("<", GUILayout.Width(windowWidth / 9));
        GUILayout.FlexibleSpace();
        GUILayout.Label("Radial", radialStyle);
        GUILayout.FlexibleSpace();
        rInc1 = GUILayout.Button(">", GUILayout.Width(windowWidth / 9));
        rInc2 = GUILayout.Button(">>", GUILayout.Width(windowWidth / 9));
        rAbs = GUILayout.Button("Abs", GUILayout.Width(windowWidth / 9));
        GUILayout.EndHorizontal();
        GUILayout.Box("", horizontalDivider);
        SnapSelectionGUI();
        GUILayout.Box("", horizontalDivider);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Small Time Step (seconds): ", GUILayout.Width(2*windowWidth / 3));
        GUI.SetNextControlName("Small Time Step");
        timeSmallStepString = GUILayout.TextField(timeSmallStepString);
        double.TryParse(timeSmallStepString, out timeSmallStep);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Large Time Step (seconds): ", GUILayout.Width(2*windowWidth / 3));
        GUI.SetNextControlName("Large Time Step");
        timeLargeStepString = GUILayout.TextField(timeLargeStepString);
        double.TryParse(timeLargeStepString, out timeLargeStep);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        timeDec2 = GUILayout.Button("<<", GUILayout.Width(windowWidth / 9));
        timeDec1 = GUILayout.Button("<", GUILayout.Width(windowWidth / 9));
        GUILayout.FlexibleSpace();
        GUILayout.Label("Time", labelStyle);
        GUILayout.FlexibleSpace();
        timeInc1 = GUILayout.Button(">", GUILayout.Width(windowWidth / 9));
        timeInc2 = GUILayout.Button(">>", GUILayout.Width(windowWidth / 9));
        GUILayout.EndHorizontal();
        GUILayout.Box("", horizontalDivider);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Maneuver Node in: ");
        GUILayout.FlexibleSpace();
        GUILayout.Label($"{Math.Truncate((currentNode.Time - game.UniverseModel.UniversalTime) / game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID).Orbit.period).ToString("n0")} orbit(s) ");
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        orbitDec = GUILayout.Button("-", GUILayout.Width(windowWidth / 7));
        GUILayout.FlexibleSpace();
        GUILayout.Label("Orbit", labelStyle);
        GUILayout.FlexibleSpace();
        orbitInc = GUILayout.Button("+", GUILayout.Width(windowWidth / 7));
        GUILayout.EndHorizontal();
    }

    private bool CloseButton()
    {
        return GUI.Button(closeBtnRect, "x", closeBtnStyle);
    }

    private void CloseWindow()
    {
        GameObject.Find("BTN-ManeuverNodeController")?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(false);
        interfaceEnabled = false;
        ToggleButton(interfaceEnabled);
    }

    // Draws the snap selection GUI.
    private void SnapSelectionGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("SnapTo: ", GUILayout.Width(windowWidth / 5));
        snapToAp = GUILayout.Button("Ap", GUILayout.Width(windowWidth / 9));
        snapToPe = GUILayout.Button("Pe", GUILayout.Width(windowWidth / 9));
        snapToANe = GUILayout.Button("ANe", GUILayout.Width(windowWidth / 9));
        snapToDNe = GUILayout.Button("DNe", GUILayout.Width(windowWidth / 9));
        if (currentTarget != null)
        {
            snapToANt = GUILayout.Button("ANt", GUILayout.Width(windowWidth / 9));
            snapToDNt = GUILayout.Button("DNt", GUILayout.Width(windowWidth / 9));
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private IEnumerator MakeNode(ManeuverNodeData nodeData)
    {
        // Add the new node to the vessel
        GameManager.Instance.Game.SpaceSimulation.Maneuvers.AddNodeToVessel(nodeData);
        
        // Wait a tick for things to get created
        yield return new WaitForFixedUpdate();

        // Update the map so the gizmo will be there
        // MapCore mapCore = null;
        // Game.Map.TryGetMapCore(out mapCore);
        var m3d = mapCore.map3D;
        var mm = m3d.ManeuverManager;
        maneuverManager.RemoveAll();
        try { maneuverManager?.GetNodeDataForVessels(); }
        catch { Debug.LogError("[Maneuver Node Controller] caught exception on call to mapCore.map3D.ManeuverManager.GetNodeDataForVessels()"); }
        if (nodeData != null)
        {
            currentNode = nodeData;
            try { maneuverManager.UpdatePositionForGizmo(nodeData.NodeID); }
            catch { Debug.LogError("[Maneuver Node Controller] caught exception on call to mapCore.map3D.ManeuverManager.UpdatePositionForGizmo()"); }
            try { maneuverManager.UpdateAll(); }
            catch { Debug.LogError("[Maneuver Node Controller] caught exception on call to mapCore.map3D.ManeuverManager.UpdateAll()"); }
        }

        // Refresh the node (may not need this unless we're actually updating it)
        var universeModel = game.UniverseModel;
        var vesselComponent = universeModel.FindVesselComponent(currentNode.RelatedSimID);
        var simObject = vesselComponent.SimulationObject;
        var maneuverPlanComponent = simObject.FindComponent<ManeuverPlanComponent>();
        if (currentNode != null)
        {
            maneuverPlanComponent.UpdateChangeOnNode(currentNode, burnParams);
            maneuverPlanComponent.RefreshManeuverNodeState(0); // Getting NREs here...
            //ManeuverNodeData nodeData = GameManager.Instance.Game.SpaceSimulation.Maneuvers.GetNodesForVessel(GameManager.Instance.Game.ViewController.GetActiveVehicle(true).Guid)[0];
            ////nodeData.BurnVector = burnParams;
            //game.UniverseModel.FindVesselComponent(nodeData.RelatedSimID)?.SimulationObject.FindComponent<ManeuverPlanComponent>().UpdateChangeOnNode(nodeData, burnParams);
            //game.UniverseModel.FindVesselComponent(nodeData.RelatedSimID)?.SimulationObject.FindComponent<ManeuverPlanComponent>().RefreshManeuverNodeState(0);
        }
    }

    private IPatchedOrbit GetLastOrbit()
    {
        Logger.LogInfo("GetLastOrbit");
        List<ManeuverNodeData> patchList =
            Game.SpaceSimulation.Maneuvers.GetNodesForVessel(activeVessel.SimulationObject.GlobalId);

        Logger.LogMessage($"GetLastOrbit: patchList.Count = {patchList.Count}");

        if (patchList.Count == 0)
        {
            Logger.LogMessage($"GetLastOrbit: activeVessel.Orbit = {activeVessel.Orbit}");
            return activeVessel.Orbit;
        }
        Logger.LogMessage($"GetLastOrbit: ManeuverTrajectoryPatch = {patchList[patchList.Count - 1].ManeuverTrajectoryPatch}");
        IPatchedOrbit orbit = patchList[patchList.Count - 1].ManeuverTrajectoryPatch;

        return orbit;
    }

    private void CreateManeuverNodeAtTA(Vector3d burnVector, double TrueAnomalyRad)
    {
        Logger.LogInfo("CreateManeuverNodeAtTA");
        PatchedConicsOrbit referencedOrbit = GetLastOrbit() as PatchedConicsOrbit;
        if (referencedOrbit == null)
        {
            Logger.LogError("CreateManeuverNodeAtTA: referencedOrbit is null!");
            return;
        }

        double UT = referencedOrbit.GetUTforTrueAnomaly(TrueAnomalyRad, 0);

        CreateManeuverNodeAtUT(burnVector, UT);
    }

    private void CreateManeuverNodeAtUT(Vector3d burnVector, double UT)
    {
        Logger.LogInfo("CreateManeuverNodeAtUT");
        PatchedConicsOrbit referencedOrbit = GetLastOrbit() as PatchedConicsOrbit;
        if (referencedOrbit == null)
        {
            Logger.LogError("CreateManeuverNodeAtUT: referencedOrbit is null!");
            return;
        }

        if (UT < game.UniverseModel.UniversalTime + 1) // Don't set node to now or in the past
            UT = game.UniverseModel.UniversalTime + 1;

        ManeuverNodeData nodeData = new ManeuverNodeData(activeVessel.SimulationObject.GlobalId, true, UT);

        IPatchedOrbit orbit = referencedOrbit;
        orbit.PatchStartTransition = PatchTransitionType.Maneuver;
        orbit.PatchEndTransition = PatchTransitionType.Final;

        nodeData.SetManeuverState((PatchedConicsOrbit)orbit);

        nodeData.BurnVector = burnVector;

        Logger.LogInfo($"CreateManeuverNodeAtUT: BurnVector [{burnVector.x}, {burnVector.y}, {burnVector.z}] m/s");
        Logger.LogInfo($"CreateManeuverNodeAtUT: BurnDuration {nodeData.BurnDuration} s");
        Logger.LogInfo($"CreateManeuverNodeAtUT: Burn Time {nodeData.Time} s");

        AddManeuverNode(nodeData);
    }

    private void AddManeuverNode(ManeuverNodeData nodeData)
    {
        Logger.LogInfo("AddManeuverNode");

        // Get the ManeuverPlanComponent for the active vessel
        var universeModel = game.UniverseModel;
        VesselComponent vesselComponent;
        if (currentNode != null)
        {
            vesselComponent = universeModel.FindVesselComponent(currentNode.RelatedSimID);
        }
        else
        {
            vesselComponent = activeVessel;
        }
        var simObject = vesselComponent.SimulationObject;
        var maneuverPlanComponent = simObject.FindComponent<ManeuverPlanComponent>();

        GameManager.Instance.Game.SpaceSimulation.Maneuvers.AddNodeToVessel(nodeData);

        // Make sure we've got access to maneuverManager
        //if (mapCore == null)
        //{
        //    Game.Map.TryGetMapCore(out mapCore);
        //}
        //if (m3d == null)
        //{
        //    m3d = mapCore.map3D;
        //}
        //if (maneuverManager == null)
        //{
        //    maneuverManager = m3d.ManeuverManager;
        //}

        // For KSP2, We want the to start burns early to make them centered on the node
        // nodeData = activeVessel.SimulationObject.ManeuverPlan.ActiveNode;
        nodeData.Time -= nodeData.BurnDuration / 2;

        Logger.LogInfo($"AddManeuverNode: BurnVector   [{nodeData.BurnVector.x}, {nodeData.BurnVector.y}, {nodeData.BurnVector.z}] m/s");
        Logger.LogInfo($"AddManeuverNode: BurnDuration {nodeData.BurnDuration} s");
        Logger.LogInfo($"AddManeuverNode: Burn Time    {nodeData.Time}");

        // Manage the maneuver on the map
        try { maneuverManager.RemoveAll(); }
        catch { Logger.LogError("AddManeuverNode: caught exception on call to mapCore.map3D.ManeuverManager.RemoveAll()"); }
        try { maneuverManager?.GetNodeDataForVessels(); }
        catch { Logger.LogError("AddManeuverNode: caught exception on call to mapCore.map3D.ManeuverManager.GetNodeDataForVessels()"); }
        try { maneuverManager.UpdatePositionForGizmo(nodeData.NodeID); }
        catch { Logger.LogError("AddManeuverNode: caught exception on call to mapCore.map3D.ManeuverManager.UpdatePositionForGizmo()"); }
        try { maneuverManager.UpdateAll(); }
        catch { Logger.LogError("AddManeuverNode: caught exception on call to mapCore.map3D.ManeuverManager.UpdateAll()"); }

        try { maneuverPlanComponent.RefreshManeuverNodeState(0); } // Occasionally getting NREs here...
        catch (NullReferenceException e) { Logger.LogError($"AddManeuverNode: caught NRE on call to maneuverPlanComponent.RefreshManeuverNodeState(0): {e}"); }
        
        // Set the currentNode  to the node we just created and added to the vessel
        currentNode = nodeData;
        Logger.LogInfo("AddManeuverNode Done");
    }

    private void handleButtons()
    {
        if (currentNode == null)
        {
            if (addNode)
            {
                // Add an empty maneuver node
                Logger.LogInfo("handleButtons: Adding New Node");

                // Define empty node data
                burnParams = Vector3d.zero;
                double UT = game.UniverseModel.UniversalTime;
                double burnUT;
                if (activeVessel.Orbit.eccentricity < 1)
                {
                    burnUT = UT + activeVessel.Orbit.TimeToAp;
                }
                else
                {
                    burnUT = UT + 30;
                }

                Vector3d burnVector;
                burnVector.x = 0;
                burnVector.y = 0;
                burnVector.z = 0;

                CreateManeuverNodeAtUT(burnVector, burnUT);

                //// Create the nodeData structure
                //ManeuverNodeData nodeData = new ManeuverNodeData(activeVessel.SimulationObject.GlobalId, false, game.UniverseModel.UniversalTime);

                //// Populate the nodeData structure
                //nodeData.BurnVector.x = 0;
                //nodeData.BurnVector.y = 0;
                //nodeData.BurnVector.z = 0;
                //nodeData.Time = UT;

                //// Call MakeNode as a Coroutine so that it can wait a tic between creating the node and updating the gizmo
                //StartCoroutine(MakeNode(nodeData));
            }
            
            return;
        }
        else if (pAbs || pInc1 || pInc2 || pDec1 || pDec2 || nAbs || nInc1 || nInc2 || nDec1 || nDec2 || rAbs || rInc1 || rInc2 || rDec1 || rDec2)
        {
            burnParams = Vector3d.zero;  // Burn update vector, this is added to the existing burn

            // Get the ManeuverPlanComponent for the active vessel
            var universeModel = game.UniverseModel;
            var vesselComponent = universeModel.FindVesselComponent(currentNode.RelatedSimID);
            var simObject = vesselComponent.SimulationObject;
            var maneuverPlanComponent = simObject.FindComponent<ManeuverPlanComponent>();

            if (pAbs) // Set the prograde burn to the absoluteValue
            {
                burnParams.z = absoluteValue - currentNode.BurnVector.z;
                // currentNode.BurnVector.z = absoluteValue;
            }
            else if (pInc1) // Add smallStep to the prograde burn
            {
                burnParams.z += smallStep;
            }
            else if (pInc2) // Add bigStep to the prograde burn
            {
                burnParams.z += bigStep;
            }
            else if (nAbs) // Set the normal burn to the absoluteValue
            {
                burnParams.y = absoluteValue - currentNode.BurnVector.y;
                // currentNode.BurnVector.y = absoluteValue;
            }
            else if (nInc1) // Add smallStep to the normal burn
            {
                burnParams.y += smallStep;
            }
            else if (nInc2) // Add bigStep to the normal burn
            {
                burnParams.y += bigStep;
            }
            else if (rAbs) // Set the radial burn to the absoluteValue
            {
                burnParams.x = absoluteValue - currentNode.BurnVector.x;
                // currentNode.BurnVector.x = absoluteValue;
            }
            else if (rInc1) // Add smallStep to the radial burn
            {
                burnParams.x += smallStep;
            }
            else if (rInc2) // Add bigStep to the radial burn
            {
                burnParams.x += bigStep;
            }
            else if (pDec1) // Subtract smallStep from the prograde burn
            {
                burnParams.z -= smallStep;
            }
            else if (pDec2) // Subtract bigStep from the prograde burn
            {
                burnParams.z -= bigStep;
            }
            else if (nDec1) // Subtract smallStep from the normal burn
            {
                burnParams.y -= smallStep;
            }
            else if (nDec2) // Subtract bigStep from the normal burn
            {
                burnParams.y -= bigStep;
            }
            else if (rDec1) // Subtract smallStep from the radial burn
            {
                burnParams.x -= smallStep;
            }
            else if (rDec2) // Subtract bigStep from the radial burn
            {
                burnParams.x -= bigStep;
            }

            // Push the update to the node
            maneuverPlanComponent.UpdateChangeOnNode(currentNode, burnParams);
            try { maneuverPlanComponent.RefreshManeuverNodeState(0); } // Occasionally getting NREs here...
            catch (NullReferenceException e) { Logger.LogError($"handleButtons: caught NRE on call to maneuverPlanComponent.RefreshManeuverNodeState(0): {e}"); }

            // Manage the maneuver on the map
            maneuverManager.RemoveAll();
            try { maneuverManager?.GetNodeDataForVessels(); }
            catch { Logger.LogError("handleButtons: caught exception on call to mapCore.map3D.ManeuverManager.GetNodeDataForVessels()"); }
            try { maneuverManager.UpdatePositionForGizmo(currentNode.NodeID); }
            catch { Logger.LogError("handleButtons: caught exception on call to mapCore.map3D.ManeuverManager.UpdatePositionForGizmo()"); }
            try { maneuverManager.UpdateAll(); }
            catch { Logger.LogError("handleButtons: caught exception on call to mapCore.map3D.ManeuverManager.UpdateAll()"); }
        }
        else if (timeDec1 || timeDec2 || timeInc1 || timeInc2 || orbitDec || orbitInc || snapToAp || snapToPe || snapToANe || snapToDNe || snapToANt || snapToDNt)
        {
            // Get the ManeuverPlanComponent for the active vessel
            var universeModel = game.UniverseModel;
            var vesselComponent = universeModel.FindVesselComponent(currentNode.RelatedSimID);
            var simObject = vesselComponent.SimulationObject;
            var maneuverPlanComponent = simObject.FindComponent<ManeuverPlanComponent>();

            // Get some objects and info we need
            var vessel = game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID);
            var target = vessel?.TargetObject;
            var UT = game.UniverseModel.UniversalTime;
            var timeOfNodeFromNow = currentNode.Time - UT;

            if (timeDec1) // Subtract timeSmallStep
            {
                if (timeSmallStep < timeOfNodeFromNow) // If there is enough time
                {
                    currentNode.Time -= timeSmallStep;
                }
            }
            else if (timeDec2) // Subtract timeLargeStep
            {
                if (timeLargeStep < timeOfNodeFromNow) // If there is enough time
                {
                    currentNode.Time -= timeLargeStep;
                }
            }
            else if (timeInc1) // Add timeSmallStep
            {
                currentNode.Time += timeSmallStep;
            }
            else if (timeInc2) // Add timeLargeStep
            {
                currentNode.Time += timeLargeStep;
            }
            else if (orbitDec) // Subtract one orbital period
            {
                if (vessel.Orbit.period < timeOfNodeFromNow) // If there is enough time
                {
                    currentNode.Time -= vessel.Orbit.period;
                }
            }
            else if (orbitInc) // Add one orbital period
            {
                currentNode.Time += vessel.Orbit.period;
            }
            else if (snapToAp) // Snap the maneuver time to the next Ap
            {
                currentNode.Time = game.UniverseModel.UniversalTime + vessel.Orbit.TimeToAp;
            }
            else if (snapToPe) // Snap the maneuver time to the next Pe
            {
                currentNode.Time = game.UniverseModel.UniversalTime + vessel.Orbit.TimeToPe;
            }
            else if (snapToANe) // Snap the maneuver time to the AN relative to the equatorial plane
            {
                currentNode.Time = vessel.Orbit.TimeOfAscendingNodeEquatorial(UT);
            }
            else if (snapToDNe) // Snap the maneuver time to the DN relative to the equatorial plane
            {
                currentNode.Time = vessel.Orbit.TimeOfDescendingNodeEquatorial(UT);
            }
            else if (snapToANt) // Snap the maneuver time to the AN relative to selected target's orbit
            {
                currentNode.Time = vessel.Orbit.TimeOfAscendingNode(target.Orbit, UT);
            }
            else if (snapToDNt) // Snap the maneuver time to the DN relative to selected target's orbit
            {
                currentNode.Time = vessel.Orbit.TimeOfDescendingNode(target.Orbit, UT);
            }

            maneuverPlanComponent.UpdateTimeOnNode(currentNode, currentNode.Time); // This may not be necessary?
            try { maneuverPlanComponent.RefreshManeuverNodeState(0); } // Occasionally getting NREs here...
            catch (NullReferenceException e) { Logger.LogError($"handleButtons: caught NRE on call to maneuverPlanComponent.RefreshManeuverNodeState(0): {e}"); }

            // Manage the maneuver on the map
            maneuverManager.RemoveAll();
            try { maneuverManager?.GetNodeDataForVessels(); }
            catch { Logger.LogError("handleButtons: caught exception on call to mapCore.map3D.ManeuverManager.GetNodeDataForVessels()"); }
            if (currentNode != null)
            {
                try { maneuverManager.UpdatePositionForGizmo(currentNode.NodeID); }
                catch { Logger.LogError("handleButtons: caught exception on call to mapCore.map3D.ManeuverManager.UpdatePositionForGizmo()"); }
                try { maneuverManager.UpdateAll(); }
                catch { Logger.LogError("handleButtons: caught exception on call to mapCore.map3D.ManeuverManager.UpdateAll()"); }
            }
        }
    }
}   