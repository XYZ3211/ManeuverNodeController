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
using BepInEx.Logging;
using static ManeuverNodeController.NodeControl;
using KSP.Sim;
using KSP.Map;
using UnityEngine.Networking.Types;
using KSP.Api;
using KSP.Logging;
using KSP.Messages;
using KSP.Sim.Converters;
using KSP.Sim.Definitions;
using KSP.Sim.DeltaV;
using KSP.Sim.State;
using System;
using System.Collections;
using System.Collections.Generic;
using static AwesomeTechnologies.External.ClipperLib.Clipper;

namespace ManeuverNodeController;

[BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
[BepInPlugin("com.github.xyz3211.maneuver_node_controller", "Maneuver Node Controller", "0.7.0")]
public class ManeuverNodeControllerMod : BaseSpaceWarpPlugin
{
    // These are useful in case some other mod wants to add a dependency to this one
    public const string ModGuid = MyPluginInfo.PLUGIN_GUID;
    public const string ModName = MyPluginInfo.PLUGIN_NAME;
    public const string ModVer = MyPluginInfo.PLUGIN_VERSION;

    public static ManeuverNodeControllerMod Instance { get; set; }

    static bool loaded = false;
    private bool interfaceEnabled = false;
    private bool GUIenabled = true;
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
    private bool snapToAp, snapToPe, snapToANe, snapToDNe, snapToANt, snapToDNt, addNode, delNode, decNode, incNode;
    private bool advancedMode;

    // Control game input state while user has clicked into a TextField.
    private bool gameInputState = true;
    public List<String> inputFields = new List<String>();

    private SimulationObjectModel currentTarget;
    private ManeuverNodeData thisNode = null;
    private Vector3d burnParams;
    private PatchedConicsOrbit orbit;

    private GUIStyle errorStyle, warnStyle, progradeStyle, normalStyle, radialStyle, labelStyle;
    private GameInstance game;
    private GUIStyle horizontalDivider = new GUIStyle();
    private GUISkin _spaceWarpUISkin;
    private GUIStyle ctrlBtnStyle;
    private GUIStyle smallBtnStyle;
    private GUIStyle textInputStyle;
    private GUIStyle closeBtnStyle;
    private GUIStyle snapBtnStyle;
    private GUIStyle nameLabelStyle;
    private GUIStyle valueLabelStyle;
    private GUIStyle unitLabelStyle;
    private int spacingAfterEntry = -12;

    internal int SelectedNodeIndex = 0;

    //public ManualLogSource logger;
    public new static ManualLogSource Logger { get; set; }

    public override void OnInitialized()
    {
        base.OnInitialized();

        Instance = this;

        game = GameManager.Instance.Game;
        Logger = base.Logger;

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

        _spaceWarpUISkin = Skins.ConsoleSkin;

        textInputStyle = new GUIStyle(_spaceWarpUISkin.textField)
        {
            alignment = TextAnchor.LowerCenter,
            padding = new RectOffset(10, 10, 0, 0),
            contentOffset = new Vector2(0, 2),
            fixedHeight = 18,
            fixedWidth = (float)(windowWidth / 4),
            clipping = TextClipping.Overflow,
            margin = new RectOffset(0, 0, 10, 0)
        };

        ctrlBtnStyle = new GUIStyle(_spaceWarpUISkin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(0, 0, 0, 3),
            contentOffset = new Vector2(0, 2),
            fixedHeight = 16,
            fixedWidth = (float)(windowWidth / 9) - 5,
            // fontSize = 16,
            clipping = TextClipping.Overflow,
            margin = new RectOffset(0, 0, 10, 0)
        };

        smallBtnStyle = new GUIStyle(_spaceWarpUISkin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(10, 10, 0, 3),
            contentOffset = new Vector2(0, 2),
            fixedHeight = 25, // 16,
            // fixedWidth = 95,
            fontSize = 16,
            clipping = TextClipping.Overflow,
            margin = new RectOffset(0, 0, 10, 0)
        };

        snapBtnStyle = new GUIStyle(_spaceWarpUISkin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(0, 0, 0, 3),
            contentOffset = new Vector2(0, 2),
            fixedHeight = 20,
            fixedWidth = (float)(windowWidth / 8) - 5,
            // fontSize = 16,
            clipping = TextClipping.Overflow,
            margin = new RectOffset(0, 0, 10, 0)
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

        closeBtnStyle = new GUIStyle(_spaceWarpUISkin.button)
        {
            fontSize = 8
        };

        closeBtnRect = new Rect(windowWidth - 23, 6, 16, 16);

        labelStyle = warnStyle = new GUIStyle(_spaceWarpUISkin.label);
        errorStyle = new GUIStyle(_spaceWarpUISkin.label);
        errorStyle.normal.textColor = Color.red;
        warnStyle = new GUIStyle(_spaceWarpUISkin.label);
        warnStyle.normal.textColor = Color.yellow;
        progradeStyle = new GUIStyle(_spaceWarpUISkin.label);
        progradeStyle.normal.textColor = Color.green;
        progradeStyle.fixedHeight = 24;
        normalStyle = new GUIStyle(_spaceWarpUISkin.label);
        normalStyle.normal.textColor = Color.magenta;
        normalStyle.fixedHeight = 24;
        radialStyle = new GUIStyle(_spaceWarpUISkin.label);
        radialStyle.normal.textColor = Color.cyan;
        radialStyle.fixedHeight = 24;
        horizontalDivider.fixedHeight = 2;
        horizontalDivider.margin = new RectOffset(0, 0, 4, 4);

        Appbar.RegisterAppButton(
            "Maneuver Node Cont.",
            "BTN-ManeuverNodeController",
            AssetManager.GetAsset<Texture2D>($"{SpaceWarpMetadata.ModID}/images/icon.png"),
            ToggleButton);
    }

    private void ToggleButton(bool toggle)
    {
        interfaceEnabled = toggle;
        GameObject.Find("BTN-ManeuverNodeController")?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(interfaceEnabled);
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

    void OnGUI()
    {
        GUIenabled = false;
        var gameState = Game?.GlobalGameState?.GetState();
        if (gameState == GameState.Map3DView) GUIenabled = true;
        if (gameState == GameState.FlightView) GUIenabled = true;

        Utility.RefreshActiveVesselAndCurrentManeuver();
        NodeControl.RefreshManeuverNodes();
        if (SelectedNodeIndex < NodeControl.Nodes.Count)
            thisNode = NodeControl.Nodes[SelectedNodeIndex];
        else thisNode = null;

        currentTarget = Utility.activeVessel?.TargetObject;
        if (Utility.activeVessel != null)
            orbit = Utility.activeVessel.Orbit;

        if (interfaceEnabled && GUIenabled && Utility.activeVessel != null)
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

        Utility.RefreshActiveVesselAndCurrentManeuver();

        double UT;
        double dvRemaining;

        GUILayout.BeginVertical();
        if (thisNode == null)
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
            DrawEntry2Button("Node:", labelStyle, ref decNode, "<", ref incNode, ">", (SelectedNodeIndex + 1).ToString());
            Draw2Button(ref delNode, "Del Node", ref addNode, "Add Node");
            GUILayout.Box("", horizontalDivider);

            DrawEntry("Total Maneuver ∆v", thisNode.BurnRequiredDV.ToString("n2"), labelStyle, "m/s");
            if (SelectedNodeIndex == 0)
                dvRemaining = (Utility.activeVessel.Orbiter.ManeuverPlanSolver.GetVelocityAfterFirstManeuver(out UT).vector - orbit.GetOrbitalVelocityAtUTZup(UT)).magnitude;
            else
                dvRemaining = thisNode.BurnRequiredDV;
            UT = game.UniverseModel.UniversalTime;
            DrawEntry("∆v Remaining", dvRemaining.ToString("n3"), labelStyle, "m/s");
            Draw2Entries("Start", "Duration", labelStyle, (thisNode.Time - UT).ToString("n2"), thisNode.BurnDuration.ToString("n2"), "s");
            GUILayout.Box("", horizontalDivider);
            DrawEntry("Prograde ∆v", thisNode.BurnVector.z.ToString("n2"), progradeStyle, "m/s");
            DrawEntry("Normal ∆v", thisNode.BurnVector.y.ToString("n2"), normalStyle, "m/s");
            DrawEntry("Radial ∆v", thisNode.BurnVector.x.ToString("n2"), radialStyle, "m/s");
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

            DrawGUIStatus();

            handleButtons();
        }
        GUILayout.EndVertical();
        GUI.DragWindow(new Rect(0, 0, 10000, 500));
    }

    private void drawAdvancedMode()
    {
        DrawEntryTextField("Prograde ∆v", ref progradeString, "m/s");
        double.TryParse(progradeString, out burnParams.z);
        DrawEntryTextField("Normal ∆v", ref normalString, "m/s");
        double.TryParse(normalString, out burnParams.y);
        DrawEntryTextField("Radial ∆v", ref radialString, "m/s");
        double.TryParse(radialString, out burnParams.x);
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
        string nextApA, nextPeA, nextInc, nextEcc, nextLAN, previousApA, previousPeA, previousInc, previousEcc, previousLAN;

        DrawEntryTextField("Absolute ∆v", ref absoluteValueString, "m/s");
        double.TryParse(absoluteValueString, out absoluteValue);
        DrawEntryTextField("Small Step ∆v", ref smallStepString, "m/s");
        double.TryParse(smallStepString, out smallStep);
        DrawEntryTextField("Large Step ∆v", ref bigStepString, "m/s");
        double.TryParse(bigStepString, out bigStep);
        GUILayout.Box("", horizontalDivider);
        DrawEntry5Button("Prograde", progradeStyle, ref pDec2, "<<", ref pDec1, "<", ref pInc1, ">", ref pInc2, ">>", ref pAbs, "Abs");
        DrawEntry5Button("Normal", normalStyle, ref nDec2, "<<", ref nDec1, "<", ref nInc1, ">", ref nInc2, ">>", ref nAbs, "Abs");
        DrawEntry5Button("Radial", radialStyle, ref rDec2, "<<", ref rDec1, "<", ref rInc1, ">", ref rInc2, ">>", ref rAbs, "Abs");
        GUILayout.Box("", horizontalDivider);
        GUILayout.Box("", horizontalDivider);
        SnapSelectionGUI();
        GUILayout.Box("", horizontalDivider);
        DrawEntryTextField("Small Time Step", ref timeSmallStepString, "seconds");
        double.TryParse(timeSmallStepString, out timeSmallStep);
        DrawEntryTextField("Large Time Step", ref timeLargeStepString, "seconds");
        double.TryParse(timeLargeStepString, out timeLargeStep);
        GUILayout.Box("", horizontalDivider);
        DrawEntry4Button("Time", labelStyle, ref timeDec2, "<<", ref timeDec1, "<", ref timeInc1, ">", ref timeInc2, ">>");
        GUILayout.Box("", horizontalDivider);
        var numOrbits = Math.Truncate((thisNode.Time - game.UniverseModel.UniversalTime) / game.UniverseModel.FindVesselComponent(thisNode.RelatedSimID).Orbit.period).ToString("n0");
        DrawEntry("Maneuver Node in", $"{numOrbits} orbit(s)");
        DrawEntry2Button("Orbit", labelStyle, ref orbitDec, "-", ref orbitInc, "+");
        GUILayout.Box("", horizontalDivider);
        Draw2Entries("Previous Orbit", "Next Orbit", labelStyle);

        var Orbiter = Utility.activeVessel.Orbiter;
        var ManeuverPlanSolver = Orbiter?.ManeuverPlanSolver;
        var PatchedConicsList = ManeuverPlanSolver?.PatchedConicsList;
        // ManeuverPlanComponent activeVesselPlan = Utility.activeVessel?.SimulationObject?.FindComponent<ManeuverPlanComponent>();
        // var nodes = activeVesselPlan?.GetNodes();

        if (NodeControl.Nodes.Count == 0) // No nodes: Just show current orbit - shouldn't ever get here...
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
        else if (SelectedNodeIndex == 0) // One or more nodes, and the selected node is the first
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
            nextApA = (PatchedConicsList[SelectedNodeIndex].ApoapsisArl / 1000).ToString("n3");
            nextPeA = (PatchedConicsList[SelectedNodeIndex].PeriapsisArl / 1000).ToString("n3");
            nextInc = PatchedConicsList[SelectedNodeIndex].inclination.ToString("n3");
            nextEcc = PatchedConicsList[SelectedNodeIndex].eccentricity.ToString("n3");
            nextLAN = PatchedConicsList[SelectedNodeIndex].longitudeOfAscendingNode.ToString("n3");
        }
        else // One or more nodes, and the selected node is not the first
        {
            // The previous orbit info will be from PatchedConicsList[SelectedNodeIndex - 1]
            previousApA = (PatchedConicsList[SelectedNodeIndex - 1].ApoapsisArl / 1000).ToString("n3");
            previousPeA = (PatchedConicsList[SelectedNodeIndex - 1].PeriapsisArl / 1000).ToString("n3");
            previousInc = PatchedConicsList[SelectedNodeIndex - 1].inclination.ToString("n3");
            previousEcc = PatchedConicsList[SelectedNodeIndex - 1].eccentricity.ToString("n3");
            previousLAN = PatchedConicsList[SelectedNodeIndex - 1].longitudeOfAscendingNode.ToString("n3");

            // The next orbit info will be from PatchedConicsList[SelectedNodeIndex]
            nextApA = (PatchedConicsList[SelectedNodeIndex].ApoapsisArl / 1000).ToString("n3");
            nextPeA = (PatchedConicsList[SelectedNodeIndex].PeriapsisArl / 1000).ToString("n3");
            nextInc = PatchedConicsList[SelectedNodeIndex].inclination.ToString("n3");
            nextEcc = PatchedConicsList[SelectedNodeIndex].eccentricity.ToString("n3");
            nextLAN = PatchedConicsList[SelectedNodeIndex].longitudeOfAscendingNode.ToString("n3");
        }

        Draw2Entries("Ap", "Ap", nameLabelStyle, previousApA, nextApA, "km");
        Draw2Entries("Pe", "Pe", nameLabelStyle, previousPeA, nextPeA, "km");
        Draw2Entries("Inc", "Inc", nameLabelStyle, previousInc, nextInc, "°");
        Draw2Entries("Ecc", "Ecc", nameLabelStyle, previousEcc, nextEcc);
        Draw2Entries("LAN", "LAN", nameLabelStyle, previousLAN, nextLAN, "°");
        GUILayout.Box("", horizontalDivider);
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

    //private void DrawSoloToggle(string sectionNamem, ref bool toggle)
    //{
    //    GUILayout.Space(5);
    //    GUILayout.BeginHorizontal();
    //    toggle = GUILayout.Toggle(toggle, sectionNamem, sectionToggleStyle);
    //    GUILayout.EndHorizontal();
    //    GUILayout.Space(-5);
    //}

    private void DrawEntry(string entryName, string value, GUIStyle entryStyle = null, string unit = "")
    {
        if (entryStyle == null)
            entryStyle = nameLabelStyle;
        GUILayout.BeginHorizontal();
        if (unit.Length > 0)
            GUILayout.Label($"{entryName} ({unit}): ", entryStyle);
        else
            GUILayout.Label($"{entryName}: ", entryStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label(value, valueLabelStyle);
        GUILayout.Space(5);
        GUILayout.Label(unit, unitLabelStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void Draw2Entries(string entryName1, string entryName2, GUIStyle entryStyle = null, string value1 = "", string value2 = "", string unit = "")
    {
        if (entryStyle == null)
        {
            entryStyle = nameLabelStyle;
            entryStyle.fixedHeight = valueLabelStyle.fixedHeight;
        }
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{entryName1} : ", entryStyle);
        if (value1.Length > 0)
        {
            GUILayout.Space(5);
            GUILayout.Label(value1, valueLabelStyle);
            GUILayout.Space(5);
            GUILayout.Label(unit, unitLabelStyle);
        }
        GUILayout.FlexibleSpace();
        GUILayout.Label($"{entryName2}: ", entryStyle);
        if (value2.Length > 0)
        {
            GUILayout.Space(5);
            GUILayout.Label(value2, valueLabelStyle);
            GUILayout.Space(5);
            GUILayout.Label(unit, unitLabelStyle);
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void DrawButton(ref bool button, string buttonStr)
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        button = GUILayout.Button(buttonStr, smallBtnStyle);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void Draw2Button(ref bool button1, string buttonStr1, ref bool button2, string buttonStr2)
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        button1 = GUILayout.Button(buttonStr1, smallBtnStyle);
        GUILayout.FlexibleSpace();
        button2 = GUILayout.Button(buttonStr2, smallBtnStyle);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }
    private void DrawEntry2Button(string entryName, GUIStyle entryStyle, ref bool button1, string button1Str, ref bool button2, string button2Str, string value = "")
    {
        GUILayout.BeginHorizontal();
        button1 = GUILayout.Button(button1Str, ctrlBtnStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label(entryName, entryStyle);
        if (value.Length > 0)
        {
            GUILayout.Space(5);
            GUILayout.Label(value, entryStyle);
        }
        GUILayout.FlexibleSpace();
        button2 = GUILayout.Button(button2Str, ctrlBtnStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void Draw3Button(ref bool button1, string button1Str, ref bool button2, string button2Str, ref bool button3, string button3Str)
    {
        GUILayout.BeginHorizontal();
        button1 = GUILayout.Button(button1Str, ctrlBtnStyle);
        GUILayout.FlexibleSpace();
        button2 = GUILayout.Button(button2Str, ctrlBtnStyle);
        GUILayout.FlexibleSpace();
        button3 = GUILayout.Button(button3Str, ctrlBtnStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void DrawEntry4Button(string entryName, GUIStyle entryStyle, ref bool button1, string button1Str, ref bool button2, string button2Str, ref bool button3, string button3Str, ref bool button4, string button4Str)
    {
        GUILayout.BeginHorizontal();
        button1 = GUILayout.Button(button1Str, ctrlBtnStyle);
        GUILayout.Space(5);
        button2 = GUILayout.Button(button2Str, ctrlBtnStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label(entryName, entryStyle);
        GUILayout.FlexibleSpace();
        button3 = GUILayout.Button(button3Str, ctrlBtnStyle);
        GUILayout.Space(5);
        button4 = GUILayout.Button(button4Str, ctrlBtnStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void DrawEntry5Button(string entryName, GUIStyle entryStyle, ref bool button1, string button1Str, ref bool button2, string button2Str, ref bool button3, string button3Str, ref bool button4, string button4Str, ref bool button5, string button5Str)
    {
        GUILayout.BeginHorizontal();
        button1 = GUILayout.Button(button1Str, ctrlBtnStyle);
        GUILayout.Space(5);
        button2 = GUILayout.Button(button2Str, ctrlBtnStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label(entryName, entryStyle);
        GUILayout.FlexibleSpace();
        button3 = GUILayout.Button(button3Str, ctrlBtnStyle);
        GUILayout.Space(5);
        button4 = GUILayout.Button(button4Str, ctrlBtnStyle);
        GUILayout.Space(5);
        button5 = GUILayout.Button(button5Str, ctrlBtnStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void DrawEntryTextField(string entryName, ref string textEntry, string unit = "")
    {
        double num;
        Color normal;
        GUILayout.BeginHorizontal();
        if (unit.Length > 0)
            GUILayout.Label($"{entryName} ({unit}): ", nameLabelStyle);
        else
            GUILayout.Label($"{entryName}: ", nameLabelStyle);
        normal = GUI.color;
        bool parsed = double.TryParse(textEntry, out num);
        if (!parsed) GUI.color = Color.red;
        GUI.SetNextControlName(entryName);
        textEntry = GUILayout.TextField(textEntry, textInputStyle);
        GUI.color = normal;
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void DrawGUIStatus()
    {
        // Indication to User that its safe to type, or why vessel controls aren't working
        GUILayout.BeginHorizontal();
        string inputStateString = gameInputState ? "<b>Enabled</b>" : "<b>Disabled</b>";
        GUILayout.Label("Game Input: ", labelStyle);
        if (gameInputState)
            GUILayout.Label(inputStateString, labelStyle);
        else
            GUILayout.Label(inputStateString, warnStyle);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    // Draws the snap selection GUI.
    private void SnapSelectionGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("SnapTo: ", nameLabelStyle); //  GUILayout.Width(windowWidth / 5));
        snapToAp = GUILayout.Button("Ap", snapBtnStyle);
        GUILayout.Space(5);
        snapToPe = GUILayout.Button("Pe", snapBtnStyle);
        GUILayout.Space(5);
        snapToANe = GUILayout.Button("ANe", snapBtnStyle);
        GUILayout.Space(5);
        snapToDNe = GUILayout.Button("DNe", snapBtnStyle);
        if (currentTarget != null)
        {
            GUILayout.Space(5);
            snapToANt = GUILayout.Button("ANt", snapBtnStyle);
            GUILayout.Space(5);
            snapToDNt = GUILayout.Button("DNt", snapBtnStyle);
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void handleButtons()
    {
        // var orbit = GetLastOrbit() as PatchedConicsOrbit;

        MapCore mapCore = null;
        GameManager.Instance.Game.Map.TryGetMapCore(out mapCore);
        var m3d = mapCore.map3D;
        var maneuverManager = m3d.ManeuverManager;

        if (thisNode == null)
        {
            if (addNode) AddNode(orbit);
            return;
        }
        else if (pAbs || pInc1 || pInc2 || pDec1 || pDec2 || nAbs || nInc1 || nInc2 || nDec1 || nDec2 || rAbs || rInc1 || rInc2 || rDec1 || rDec2)
        {
            burnParams = Vector3d.zero;  // Burn update vector, this is added to the existing burn

            // Get the ManeuverPlanComponent for the active vessel
            var universeModel = game.UniverseModel;
            var vesselComponent = universeModel.FindVesselComponent(thisNode.RelatedSimID);
            var simObject = vesselComponent.SimulationObject;
            var maneuverPlanComponent = simObject.FindComponent<ManeuverPlanComponent>();

            if (pAbs) // Set the prograde burn to the absoluteValue
            {
                burnParams.z = absoluteValue - thisNode.BurnVector.z;
                // thisNode.BurnVector.z = absoluteValue;
            }
            else if (pInc1) // Add smallStep to the prograde burn
            {
                burnParams.z += smallStep;
                // thisNode.BurnVector.z += smallStep;
            }
            else if (pInc2) // Add bigStep to the prograde burn
            {
                burnParams.z += bigStep;
                // thisNode.BurnVector.z += bigStep;
            }
            else if (pDec1) // Subtract smallStep from the prograde burn
            {
                burnParams.z -= smallStep;
                // thisNode.BurnVector.z -= smallStep;
            }
            else if (pDec2) // Subtract bigStep from the prograde burn
            {
                burnParams.z -= bigStep;
                // thisNode.BurnVector.z -= bigStep;
            }
            else if (nAbs) // Set the normal burn to the absoluteValue
            {
                burnParams.y = absoluteValue - thisNode.BurnVector.y;
                // thisNode.BurnVector.y = absoluteValue;
            }
            else if (nInc1) // Add smallStep to the normal burn
            {
                burnParams.y += smallStep;
                // thisNode.BurnVector.y += smallStep;
            }
            else if (nInc2) // Add bigStep to the normal burn
            {
                burnParams.y += bigStep;
                // thisNode.BurnVector.y += bigStep;
            }
            else if (nDec1) // Subtract smallStep from the normal burn
            {
                burnParams.y -= smallStep;
                // thisNode.BurnVector.y -= smallStep;
            }
            else if (nDec2) // Subtract bigStep from the normal burn
            {
                burnParams.y -= bigStep;
                // thisNode.BurnVector.y -= bigStep;
            }
            else if (rAbs) // Set the radial burn to the absoluteValue
            {
                burnParams.x = absoluteValue - thisNode.BurnVector.x;
                // thisNode.BurnVector.x = absoluteValue;
            }
            else if (rInc1) // Add smallStep to the radial burn
            {
                burnParams.x += smallStep;
                // thisNode.BurnVector.x += smallStep;
            }
            else if (rInc2) // Add bigStep to the radial burn
            {
                burnParams.x += bigStep;
                // thisNode.BurnVector.x += bigStep;
            }
            else if (rDec1) // Subtract smallStep from the radial burn
            {
                burnParams.x -= smallStep;
                // thisNode.BurnVector.x -= smallStep;
            }
            else if (rDec2) // Subtract bigStep from the radial burn
            {
                burnParams.x -= bigStep;
                // thisNode.BurnVector.x -= bigStep;
            }

            // Push the update to the node
            //Logger.LogInfo("handleButtons: Pushing new burn info to node");
            //Logger.LogInfo($"handleButtons: burnParams         [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s");
            maneuverPlanComponent.UpdateChangeOnNode(thisNode, burnParams);

            // Call FixStuff to update the nodes in a way that allows the game to catch up with the updates
            StartCoroutine(FixStuff(maneuverPlanComponent));

            Logger.LogInfo($"handleButtons: Updated BurnVector    [{thisNode.BurnVector.x}, {thisNode.BurnVector.y}, {thisNode.BurnVector.z}] m/s");
            //Logger.LogInfo($"handleButtons: BurnVector.normalized [{thisNode.BurnVector.normalized.x}, {thisNode.BurnVector.normalized.y}, {thisNode.BurnVector.normalized.z}] m/s");

        }
        else if (timeDec1 || timeDec2 || timeInc1 || timeInc2 || orbitDec || orbitInc || snapToAp || snapToPe || snapToANe || snapToDNe || snapToANt || snapToDNt)
        {
            // Get the ManeuverPlanComponent for the active vessel
            var universeModel = game.UniverseModel;
            var vesselComponent = universeModel.FindVesselComponent(thisNode.RelatedSimID);
            var simObject = vesselComponent.SimulationObject;
            // var maneuverProvider = WHERE CAN WE FIND THIS? It's defined in KSP.SIM.Maneuver.
            var maneuverPlanComponent = simObject.FindComponent<ManeuverPlanComponent>();

            // Get some objects and info we need
            var vessel = game.UniverseModel.FindVesselComponent(thisNode.RelatedSimID);
            var target = vessel?.TargetObject;
            var UT = game.UniverseModel.UniversalTime;
            var oldBurnTime = thisNode.Time;
            var timeOfNodeFromNow = oldBurnTime - UT;

            double nodeTime = thisNode.Time;

            if (timeDec1) // Subtract timeSmallStep
            {
                if (timeSmallStep < timeOfNodeFromNow) // If there is enough time
                {
                    nodeTime -= timeSmallStep;
                    // thisNode.Time -= timeSmallStep;
                }
            }
            else if (timeDec2) // Subtract timeLargeStep
            {
                if (timeLargeStep < timeOfNodeFromNow) // If there is enough time
                {
                    nodeTime -= timeLargeStep;
                    // thisNode.Time -= timeLargeStep;
                }
            }
            else if (timeInc1) // Add timeSmallStep
            {
                nodeTime += timeSmallStep;
                // thisNode.Time += timeSmallStep;
            }
            else if (timeInc2) // Add timeLargeStep
            {
                nodeTime += timeLargeStep;
                // thisNode.Time += timeLargeStep;
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

            // Push the update to the node
            // thisNode.Time = nodeTime;
            maneuverPlanComponent.UpdateTimeOnNode(thisNode, nodeTime);

            // Call FixStuff to update the nodes in a way that allows the game to catch up with the updates
            StartCoroutine(FixStuff(maneuverPlanComponent));

            Logger.LogInfo($"handleButtons: Burn time was {oldBurnTime}, is {thisNode.Time}");

            // UpdateNode(thisNode);

        }
        else if (decNode || incNode || delNode || addNode)
        {
            if (decNode && SelectedNodeIndex > 0)
            {
                SelectedNodeIndex--;
            }
            else if (incNode && SelectedNodeIndex + 1 < NodeControl.Nodes.Count)
            {
                SelectedNodeIndex++;
            }
            else if (delNode)
            {
                DeleteNodes(SelectedNodeIndex);
                SelectedNodeIndex = 0;
            }
            else if (addNode)
            {
                AddNode(orbit);
            }

        }
    }
    private IEnumerator FixStuff(ManeuverPlanComponent maneuverPlanComponent)
    {
        yield return (object)new WaitForFixedUpdate();

        NodeControl.RefreshManeuverNodes();

        yield return (object)new WaitForFixedUpdate();

        for (int i = SelectedNodeIndex; i < NodeControl.Nodes.Count; i++)
        {
            var node = NodeControl.Nodes[i];
            maneuverPlanComponent.UpdateNodeDetails(node);
            maneuverPlanComponent.RefreshManeuverNodeState(i);
        }

        yield return (object)new WaitForFixedUpdate();

        NodeControl.RefreshManeuverNodes();
    }
}   
