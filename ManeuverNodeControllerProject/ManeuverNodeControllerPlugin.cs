using BepInEx;
using UnityEngine;
using KSP.Game;
using KSP.Sim.impl;
using KSP.Sim.Maneuver;
using SpaceWarp;
// using SpaceWarp.API;
using SpaceWarp.API.Mods;
using SpaceWarp.API.Assets;
using SpaceWarp.API.UI;
using SpaceWarp.API.UI.Appbar;
using KSP.UI.Binding;
// using static UnityEngine.RemoteConfigSettingsHelper;
// using KSP.Messages.PropertyWatchers;
using KSP.Sim;
using KSP.Map;
// using MoonSharp.Interpreter.Tree;
// using KSP.Messages.PropertyWatchers;
// using Unity.Collections.LowLevel.Unsafe;
// using EdyCommonTools;
using MuMech;

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

    // SnapTo selection.
    //private enum SnapOptions
    //{
    //    Apoapsis,
    //    Periapsis,
    //    AN,
    //    DN
    //}
    // private SnapOptions selectedSnapOption = SnapOptions.Apoapsis;
    // private readonly List<string> snapOptions = new List<string> { "Apoapsis", "Periapsis", "AN", "DN" };
    // private bool selectingSnapOption = false;
    // private static Vector2 scrollPositionSnapOptions;
    // private bool applySnapOption;

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
            Logger.LogInfo("UI toggled with hotkey");
        }
    }

    void OnGUI()
    {
        activeVessel = GameManager.Instance?.Game?.ViewController?.GetActiveVehicle(true)?.GetSimVessel(true);
        if (activeVessel.TargetObject != null)
            currentTarget = activeVessel.TargetObject;
        else currentTarget = null;
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
        game = GameManager.Instance.Game;
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
            Logger.LogInfo(nodeData.ToString());
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
        //if (!selectingSnapOption)
        //{
        //if (GUILayout.Button(Enum.GetName(typeof(SnapOptions), selectedSnapOption)))
        //    selectingSnapOption = true;
        //}
        //else
        //{
        //GUILayout.BeginVertical(GUI.skin.GetStyle("Box"));
        //scrollPositionSnapOptions = GUILayout.BeginScrollView(scrollPositionSnapOptions, false, true, GUILayout.Height(70));
        //foreach (string snapOption in Enum.GetNames(typeof(SnapOptions)).ToList())
        //{
        //    if (GUILayout.Button(snapOption))
        //    {
        //        Enum.TryParse(snapOption, out selectedSnapOption);
        //        selectingSnapOption = false;
        //    }
        //}
        //GUILayout.EndScrollView();
        //GUILayout.EndVertical();
        //}
        //applySnapOption = GUILayout.Button("Snap", GUILayout.Width(windowWidth / 5));
        GUILayout.EndHorizontal();
    }
    /// <summary>
    /// Creates a maneuver node at a given true anomaly
    /// </summary>
    /// <param name="burnVector"></param>
    /// <param name="TrueAnomaly"></param>
    private void CreateManeuverNode(Vector3d burnVector, double UT)
    {
        PatchedConicsOrbit referencedOrbit = activeVessel.Orbit as PatchedConicsOrbit;
        if (referencedOrbit == null)
        {
            Logger.LogError("CreateManeuverNode: referencedOrbit is null!");
            return;
        }
        // double TrueAnomalyRad = TrueAnomaly * Math.PI / 180;
        // double UT = referencedOrbit.GetUTforTrueAnomaly(TrueAnomaly, 0);
        ManeuverNodeData maneuverNodeData = new ManeuverNodeData(activeVessel.SimulationObject.GlobalId, true, UT);
        IPatchedOrbit orbit = referencedOrbit;
        orbit.PatchStartTransition = PatchTransitionType.Maneuver;
        orbit.PatchEndTransition = PatchTransitionType.Final;
        maneuverNodeData.SetManeuverState((PatchedConicsOrbit)orbit);
        maneuverNodeData.BurnVector = burnVector;
        currentNode = maneuverNodeData;
        AddManeuverNode(maneuverNodeData);
    }
    private void AddManeuverNode(ManeuverNodeData maneuverNodeData)
    {
        Game.SpaceSimulation.Maneuvers.AddNodeToVessel(maneuverNodeData);
        MapCore mapCore = null;
        Game.Map.TryGetMapCore(out mapCore);
        mapCore.map3D.ManeuverManager.GetNodeDataForVessels();
        mapCore.map3D.ManeuverManager.UpdatePositionForGizmo(maneuverNodeData.NodeID);
        mapCore.map3D.ManeuverManager.UpdateAll();
        // mapCore.map3D.ManeuverManager.RemoveAll();
    }
    private void handleButtons()
    {
        if (currentNode == null)
        {
            if (addNode)
            {
                // Add an empty maneuver node
                Logger.LogInfo("Adding New Node");
                // Define empty node data
                burnParams = Vector3d.zero;
                double UT = game.UniverseModel.UniversalTime;
                if (activeVessel.Orbit.eccentricity < 1)
                {
                    UT += activeVessel.Orbit.TimeToAp;
                }
                // Create the nodeData structure
                ManeuverNodeData nodeData = new ManeuverNodeData(activeVessel.SimulationObject.GlobalId, false, game.UniverseModel.UniversalTime);
                // Populate the nodeData structure
                nodeData.BurnVector.x = 0;
                nodeData.BurnVector.y = 0;
                nodeData.BurnVector.z = 0;
                nodeData.Time = UT;
                // Add the new node to the vessel
                GameManager.Instance.Game.SpaceSimulation.Maneuvers.AddNodeToVessel(nodeData);
                // Update the map so the gizmo will be there
                MapCore mapCore = null;
                Game.Map.TryGetMapCore(out mapCore);
                mapCore.map3D.ManeuverManager.GetNodeDataForVessels();
                mapCore.map3D.ManeuverManager.UpdatePositionForGizmo(nodeData.NodeID);
                mapCore.map3D.ManeuverManager.UpdateAll();
                // Refresh stuff
                activeVessel.SimulationObject.ManeuverPlan.UpdateChangeOnNode(nodeData, burnParams);
                activeVessel.SimulationObject.ManeuverPlan.RefreshManeuverNodeState(0);
                // Set teh currentNode to be the node we just added
                currentNode = nodeData;
                // addNode = false;
            }
            else return;
        }
        if (pAbs || pInc1 || pInc2 || pDec1 || pDec2 || nAbs || nInc1 || nInc2 || nDec1 || nDec2 || rAbs || rInc1 || rInc2 || rDec1 || rDec2 || timeDec1 || timeDec2 || timeInc1 || timeInc2 || orbitDec || orbitInc || snapToAp || snapToPe || snapToANe || snapToDNe || snapToANt || snapToDNt)
        {
            burnParams = Vector3d.zero;
            if (pAbs)
            {
                currentNode.BurnVector.z = absoluteValue;
            }
            else if (pInc1)
            {
                burnParams.z += smallStep;
            }
            else if (pInc2)
            {
                burnParams.z += bigStep;
            }
            else if (nAbs)
            {
                currentNode.BurnVector.y = absoluteValue;
            }
            else if (nInc1)
            {
                burnParams.y += smallStep;
            }
            else if (nInc2)
            {
                burnParams.y += bigStep;
            }
            else if (rAbs)
            {
                currentNode.BurnVector.x = absoluteValue;
            }
            else if (rInc1)
            {
                burnParams.x += smallStep;
            }
            else if (rInc2)
            {
                burnParams.x += bigStep;
            }
            else if (pDec1)
            {
                burnParams.z -= smallStep;
            }
            else if (pDec2)
            {
                burnParams.z -= bigStep;
            }
            else if (nDec1)
            {
                burnParams.y -= smallStep;
            }
            else if (nDec2)
            {
                burnParams.y -= bigStep;
            }
            else if (rDec1)
            {
                burnParams.x -= smallStep;
            }
            else if (rDec2)
            {
                burnParams.x -= bigStep;
            }
            else if (timeDec1)
            {
                currentNode.Time -= timeSmallStep;
            }
            else if (timeDec2)
            {
                currentNode.Time -= timeLargeStep;
            }
            else if (timeInc1)
            {
                currentNode.Time += timeSmallStep;
            }
            else if (timeInc2)
            {
                currentNode.Time += timeLargeStep;
            }
            else if (orbitDec)
            {
                if (game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID).Orbit.period < (currentNode.Time - game.UniverseModel.UniversalTime))
                {
                    currentNode.Time -= game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID).Orbit.period;
                }
            }
            else if (orbitInc)
            {
                currentNode.Time += game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID).Orbit.period;
            }
            else if (snapToAp) // Snap the maneuver time to the next Ap
            {
                currentNode.Time = game.UniverseModel.UniversalTime + game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID).Orbit.TimeToAp;
            }
            else if (snapToPe) // Snap the maneuver time to the next Pe
            {
                currentNode.Time = game.UniverseModel.UniversalTime + game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID).Orbit.TimeToPe;
            }
            else if (snapToANe) // Snap the maneuver time to the AN relative to the equatorial plane
            {
                Logger.LogInfo("Snapping Maneuver Time to TimeOfAscendingNodeEquatorial");
                var UT = game.UniverseModel.UniversalTime;
                var TAN = activeVessel.Orbit.TimeOfAscendingNodeEquatorial(UT);
                var ANTA = activeVessel.Orbit.AscendingNodeEquatorialTrueAnomaly();
                // var TAN = game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID).Orbit.TimeOfAscendingNodeEquatorial(UT);
                currentNode.Time = TAN; // game.UniverseModel.UniversalTime + TAN;
                Logger.LogInfo($"UT: {UT}");
                Logger.LogInfo($"AscendingNodeEquatorialTrueAnomaly: {ANTA}");
                Logger.LogInfo($"TimeOfAscendingNodeEquatorial: {TAN}");
                // Logger.LogInfo($"UT + TimeOfAscendingNodeEquatorial: {TAN + UT}");
            }
            else if (snapToDNe) // Snap the maneuver time to the DN relative to the equatorial plane
            {
                Logger.LogInfo("Snapping Maneuver Time to TimeOfDescendingNodeEquatorial");
                var UT = game.UniverseModel.UniversalTime;
                var TDN = activeVessel.Orbit.TimeOfDescendingNodeEquatorial(UT);
                var DNTA = activeVessel.Orbit.DescendingNodeEquatorialTrueAnomaly();
                // var TDN = game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID).Orbit.TimeOfDescendingNodeEquatorial(UT);
                currentNode.Time = TDN; // game.UniverseModel.UniversalTime + TDN;
                Logger.LogInfo($"UT: {UT}");
                Logger.LogInfo($"DescendingNodeEquatorialTrueAnomaly: {DNTA}");
                Logger.LogInfo($"TimeOfDescendingNodeEquatorial: {TDN}");
                // Logger.LogInfo($"UT + TimeOfDescendingNodeEquatorial: {TDN + UT}");
            }
            else if (snapToANt) // Snap the maneuver time to the AN relative to selected target's orbit
            {
                Logger.LogInfo("Snapping Maneuver Time to TimeOfAscendingNode");
                var UT = game.UniverseModel.UniversalTime;
                var TANt = activeVessel.Orbit.TimeOfAscendingNode(currentTarget.Orbit, UT);
                var ANTA = activeVessel.Orbit.AscendingNodeTrueAnomaly(currentTarget.Orbit);
                // var TANt = game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID).Orbit.TimeOfAscendingNode(currentTarget.Orbit, UT);
                currentNode.Time = TANt; // game.UniverseModel.UniversalTime + TANt;
                Logger.LogInfo($"UT: {UT}");
                Logger.LogInfo($"AscendingNodeTrueAnomaly: {ANTA}");
                Logger.LogInfo($"TimeOfAscendingNode: {TANt}");
                // Logger.LogInfo($"UT + TimeOfAscendingNode: {TANt + UT}");
            }
            else if (snapToDNt) // Snap the maneuver time to the DN relative to selected target's orbit
            {
                Logger.LogInfo("Snapping Maneuver Time to TimeOfDescendingNode");
                var UT = game.UniverseModel.UniversalTime;
                var TDNt = activeVessel.Orbit.TimeOfDescendingNode(currentTarget.Orbit, UT);
                var DNTA = activeVessel.Orbit.DescendingNodeTrueAnomaly(currentTarget.Orbit);
                // var TDNt = game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID).Orbit.TimeOfDescendingNode(currentTarget.Orbit, UT);
                currentNode.Time = TDNt; // game.UniverseModel.UniversalTime + TDNt;
                Logger.LogInfo($"UT: {UT}");
                Logger.LogInfo($"DescendingNodeTrueAnomaly: {DNTA}");
                Logger.LogInfo($"TimeOfDescendingNode: {TDNt}");
                // Logger.LogInfo($"UT + TimeOfDescendingNode: {TDNt + UT}");
            }
            activeVessel.SimulationObject.ManeuverPlan.UpdateChangeOnNode(currentNode, burnParams);
            activeVessel.SimulationObject.ManeuverPlan.RefreshManeuverNodeState(0);
            // game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID)?.SimulationObject.FindComponent<ManeuverPlanComponent>().UpdateChangeOnNode(currentNode, burnParams);
            // game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID)?.SimulationObject.FindComponent<ManeuverPlanComponent>().RefreshManeuverNodeState(0);
        }
    }
}   