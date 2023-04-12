﻿using BepInEx;
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
using BepInEx.Logging;

namespace ManeuverNodeController;

[BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
[BepInPlugin("com.github.schlosrat.maneuvernodecontroller", "Maneuver Node Controller", "0.7.0")]
public class ManeuverNodeControllerMod : BaseSpaceWarpPlugin
{
    private static ManeuverNodeControllerMod Instance { get; set; }

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
    private GUIStyle snapBtnStyle;
    private GUIStyle nameLabelStyle;
    private GUIStyle valueLabelStyle;
    private GUIStyle unitLabelStyle;
    private string unitColorHex;
    private int spacingAfterHeader = -12;
    private int spacingAfterEntry = -12;
    private int spacingAfterSection = 5;

    //public ManualLogSource logger;
    public new static ManualLogSource Logger { get; set; }

    public override void OnInitialized()
    {
        base.OnInitialized();
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
            Logger.LogInfo("Update: UI toggled with hotkey");
        }
    }

    void OnGUI()
    {
        GUIenabled = false;
        var gameState = Game.GlobalGameState.GetState();
        if (gameState == GameState.Map3DView) GUIenabled = true;
        if (gameState == GameState.FlightView) GUIenabled = true;

        activeVessel = GameManager.Instance?.Game?.ViewController?.GetActiveVehicle(true)?.GetSimVessel(true);
        currentTarget = activeVessel?.TargetObject;
        if (interfaceEnabled && GUIenabled && activeVessel != null)
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
        progradeStyle.normal.textColor = Color.green;
        progradeStyle.fixedHeight = 24;
        normalStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        normalStyle.normal.textColor = Color.magenta;
        normalStyle.fixedHeight = 24;
        radialStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        radialStyle.normal.textColor = Color.cyan;
        radialStyle.fixedHeight = 24;
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
            DrawEntry("Total Maneuver ∆v", currentNode.BurnRequiredDV.ToString("n2"), labelStyle, "m/s");
            dvRemaining = (activeVessel.Orbiter.ManeuverPlanSolver.GetVelocityAfterFirstManeuver(out UT).vector - activeVessel.Orbit.GetOrbitalVelocityAtUTZup(UT)).magnitude;
            DrawEntry("∆v Remaining", dvRemaining.ToString("n3"), labelStyle, "m/s");
            GUILayout.Box("", horizontalDivider);
            DrawEntry("Prograde ∆v", currentNode.BurnVector.z.ToString("n2"), progradeStyle, "m/s");
            DrawEntry("Normal ∆v", currentNode.BurnVector.y.ToString("n2"), normalStyle, "m/s");
            DrawEntry("Radial ∆v", currentNode.BurnVector.x.ToString("n2"), radialStyle, "m/s");
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
            GUILayout.Label($"Game Input: {inputStateString}", labelStyle);
            GUILayout.EndHorizontal();

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
        string nextApA, nextPeA, nextInc, nextEcc, nextLAN;
        var UT = game.UniverseModel.UniversalTime;

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
        var numOrbits = Math.Truncate((currentNode.Time - game.UniverseModel.UniversalTime) / game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID).Orbit.period).ToString("n0");
        DrawEntry("Maneuver Node in", $"{numOrbits} orbit(s)");
        DrawEntry2Button("Orbit", labelStyle, ref orbitDec, "-", ref orbitInc, "+");
        Draw2Entries("Start", "Duration", nameLabelStyle, (currentNode.Time - UT).ToString("n2"), currentNode.BurnDuration.ToString("n2"), "s");
        GUILayout.Box("", horizontalDivider);
        Draw2Entries("Starting Orbit", "New Orbit", labelStyle);
        var patch = currentNode?.ManeuverTrajectoryPatch;
        if (patch != null)
        {
            try
            {
                if (patch.eccentricity < 1)
                    nextApA = (patch.ApoapsisArl / 1000).ToString("n3");
                else
                    nextApA = "Inf";
                nextPeA = (patch.PeriapsisArl / 1000).ToString("n3");
                nextInc = patch.inclination.ToString("n3");
                nextEcc = patch.eccentricity.ToString("n3");
                nextLAN = patch.longitudeOfAscendingNode.ToString("n3");
            }
            catch (Exception e)
            {
                Logger.LogError($"drawSimpleMode: Caught Exception getting orbit info for maneuver patch: {e}");
                nextApA = "Err";
                nextPeA = "Err";
                nextInc = "Err";
                nextEcc = "Err";
                nextLAN = "Err";
            }
        }
        else
        {
            nextApA = "NaN";
            nextPeA = "NaN";
            nextInc = "NaN";
            nextEcc = "NaN";
            nextLAN = "NaN";
        }
        Draw2Entries("Ap", "Ap", nameLabelStyle, (activeVessel.Orbit.ApoapsisArl / 1000).ToString("n3"), nextApA, "km");
        Draw2Entries("Pe", "Pe", nameLabelStyle, (activeVessel.Orbit.PeriapsisArl / 1000).ToString("n3"), nextPeA, "km");
        Draw2Entries("Inc", "Inc", nameLabelStyle, activeVessel.Orbit.inclination.ToString("n3"), nextInc, "°");
        Draw2Entries("Ecc", "Ecc", nameLabelStyle, activeVessel.Orbit.eccentricity.ToString("n3"), nextEcc);
        Draw2Entries("LAN", "LAN", nameLabelStyle, activeVessel.Orbit.longitudeOfAscendingNode.ToString("n3"), nextLAN, "°");
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

    //private void DrawSectionHeader(string sectionName, string value = "") // was (string sectionName, ref bool isPopout, string value = "")
    //{
    //    GUILayout.BeginHorizontal();
    //    // Don't need popout buttons for ROC
    //    // isPopout = isPopout ? !CloseButton() : GUILayout.Button("⇖", popoutBtnStyle);

    //    GUILayout.Label($"<b>{sectionName}</b>");
    //    GUILayout.FlexibleSpace();
    //    GUILayout.Label(value, valueLabelStyle);
    //    GUILayout.Space(5);
    //    GUILayout.Label("", unitLabelStyle);
    //    GUILayout.EndHorizontal();
    //    GUILayout.Space(spacingAfterHeader);
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
    
    //private void DrawEntryButton(string entryName, ref bool button, string buttonStr, string value, string unit = "")
    //{
    //    GUILayout.BeginHorizontal();
    //    GUILayout.Label(entryName, nameLabelStyle);
    //    GUILayout.FlexibleSpace();
    //    button = GUILayout.Button(buttonStr, ctrlBtnStyle);
    //    GUILayout.Label(value, valueLabelStyle);
    //    GUILayout.Space(5);
    //    GUILayout.Label(unit, unitLabelStyle);
    //    GUILayout.EndHorizontal();
    //    GUILayout.Space(spacingAfterEntry);
    //}

    private void DrawEntry2Button(string entryName, GUIStyle entryStyle, ref bool button1, string button1Str, ref bool button2, string button2Str)
    {
        GUILayout.BeginHorizontal();
        button1 = GUILayout.Button(button1Str, ctrlBtnStyle); // GUILayout.Width(windowWidth / 9));
        GUILayout.FlexibleSpace();
        GUILayout.Label(entryName, entryStyle);
        GUILayout.FlexibleSpace();
        button2 = GUILayout.Button(button2Str, ctrlBtnStyle); // GUILayout.Width(windowWidth / 9));
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void DrawEntry4Button(string entryName, GUIStyle entryStyle, ref bool button1, string button1Str, ref bool button2, string button2Str, ref bool button3, string button3Str, ref bool button4, string button4Str)
    {
        GUILayout.BeginHorizontal();
        button1 = GUILayout.Button(button1Str, ctrlBtnStyle); // GUILayout.Width(windowWidth / 9));
        GUILayout.Space(5);
        button2 = GUILayout.Button(button2Str, ctrlBtnStyle); // GUILayout.Width(windowWidth / 9));
        GUILayout.FlexibleSpace();
        GUILayout.Label(entryName, entryStyle);
        GUILayout.FlexibleSpace();
        button3 = GUILayout.Button(button3Str, ctrlBtnStyle); // GUILayout.Width(windowWidth / 9));
        GUILayout.Space(5);
        button4 = GUILayout.Button(button4Str, ctrlBtnStyle); // GUILayout.Width(windowWidth / 9));
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void DrawEntry5Button(string entryName, GUIStyle entryStyle, ref bool button1, string button1Str, ref bool button2, string button2Str, ref bool button3, string button3Str, ref bool button4, string button4Str, ref bool button5, string button5Str)
    {
        GUILayout.BeginHorizontal();
        button1 = GUILayout.Button(button1Str, ctrlBtnStyle); // GUILayout.Width(windowWidth / 9));
        GUILayout.Space(5);
        button2 = GUILayout.Button(button2Str, ctrlBtnStyle); // GUILayout.Width(windowWidth / 9));
        GUILayout.FlexibleSpace();
        GUILayout.Label(entryName, entryStyle);
        GUILayout.FlexibleSpace();
        button3 = GUILayout.Button(button3Str, ctrlBtnStyle); // GUILayout.Width(windowWidth / 9));
        GUILayout.Space(5);
        button4 = GUILayout.Button(button4Str, ctrlBtnStyle); // GUILayout.Width(windowWidth / 9));
        GUILayout.Space(5);
        button5 = GUILayout.Button(button5Str, ctrlBtnStyle); // GUILayout.Width(windowWidth / 9));
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void DrawEntryTextField(string entryName, ref string textEntry, string unit = "")
    {
        GUILayout.BeginHorizontal();
        if (unit.Length > 0)
            GUILayout.Label($"{entryName} ({unit}): ", nameLabelStyle);
        else
            GUILayout.Label($"{entryName}: ", nameLabelStyle);
        // GUILayout.FlexibleSpace();
        GUI.SetNextControlName(entryName);
        textEntry = GUILayout.TextField(textEntry, textInputStyle);
        // GUILayout.Space(5);
        // GUILayout.Label(unit, unitLabelStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    //private void DrawSectionEnd() // was (ref bool isPopout)
    //{
    //    //if (isPopout)
    //    //{
    //    //    GUI.DragWindow(new Rect(0, 0, windowWidth, windowHeight));
    //    //    GUILayout.Space(spacingBelowPopout);
    //    //}
    //    //else
    //    //{
    //    GUILayout.Space(spacingAfterSection);
    //    //}
    //}

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

    private IPatchedOrbit GetLastOrbit()
    {
        Logger.LogInfo("GetLastOrbit");
        List<ManeuverNodeData> patchList =
            Game.SpaceSimulation.Maneuvers.GetNodesForVessel(activeVessel.SimulationObject.GlobalId);

        Logger.LogMessage($"GetLastOrbit: patchList.Count = {patchList.Count}");

        if (patchList.Count == 0)
        {
            // Logger.LogMessage($"GetLastOrbit: activeVessel.Orbit = {activeVessel.Orbit}");
            return activeVessel.Orbit;
        }
        Logger.LogMessage($"GetLastOrbit: ManeuverTrajectoryPatch = {patchList[patchList.Count - 1].ManeuverTrajectoryPatch}");
        IPatchedOrbit orbit = patchList[patchList.Count - 1].ManeuverTrajectoryPatch;

        return orbit;
    }

    private void CreateManeuverNodeAtTA(Vector3d burnVector, double TrueAnomalyRad)
    {
        // Logger.LogInfo("CreateManeuverNodeAtTA");
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
        // Logger.LogInfo("CreateManeuverNodeAtUT");
        //PatchedConicsOrbit referencedOrbit = GetLastOrbit() as PatchedConicsOrbit;
        //if (referencedOrbit == null)
        //{
        //    Logger.LogError("CreateManeuverNodeAtUT: referencedOrbit is null!");
        //    return;
        //}

        if (UT < game.UniverseModel.UniversalTime + 1) // Don't set node to now or in the past
            UT = game.UniverseModel.UniversalTime + 1;

        ManeuverNodeData nodeData = new ManeuverNodeData(activeVessel.SimulationObject.GlobalId, false, UT);

        //IPatchedOrbit orbit = referencedOrbit;
        //orbit.PatchStartTransition = PatchTransitionType.Maneuver;
        //orbit.PatchEndTransition = PatchTransitionType.Final;

        //nodeData.SetManeuverState((PatchedConicsOrbit)orbit);

        nodeData.BurnVector = burnVector;

        //Logger.LogInfo($"CreateManeuverNodeAtUT: BurnVector [{burnVector.x}, {burnVector.y}, {burnVector.z}] m/s");
        //Logger.LogInfo($"CreateManeuverNodeAtUT: BurnDuration {nodeData.BurnDuration} s");
        //Logger.LogInfo($"CreateManeuverNodeAtUT: Burn Time {nodeData.Time} s");

        AddManeuverNode(nodeData);
    }

    private void AddManeuverNode(ManeuverNodeData nodeData)
    {
        //Logger.LogInfo("AddManeuverNode");

        // Add the node to the vessel's orbit
        GameManager.Instance.Game.SpaceSimulation.Maneuvers.AddNodeToVessel(nodeData);

        // For KSP2, We want the to start burns early to make them centered on the node
        nodeData.Time -= nodeData.BurnDuration / 2;

        //Logger.LogInfo($"AddManeuverNode: BurnVector   [{nodeData.BurnVector.x}, {nodeData.BurnVector.y}, {nodeData.BurnVector.z}] m/s");
        Logger.LogInfo($"AddManeuverNode: BurnDuration {nodeData.BurnDuration} s");
        //Logger.LogInfo($"AddManeuverNode: Burn Time    {nodeData.Time}");

        // Set the currentNode  to the node we just created and added to the vessel
        currentNode = nodeData;

        StartCoroutine(UpdateNode(nodeData));

        //Logger.LogInfo("AddManeuverNode Done");
    }

    private IEnumerator UpdateNode(ManeuverNodeData nodeData)
    {
        MapCore mapCore = null;
        Game.Map.TryGetMapCore(out mapCore);
        var m3d = mapCore.map3D;
        var maneuverManager = m3d.ManeuverManager;

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

        var UT = game.UniverseModel.UniversalTime;
        if (nodeData.Time < UT + 1)
        {
            Logger.LogWarning($"UpdateNode: Attempted to set nodeData.Time to {nodeData.Time - UT} s from now.");
            nodeData.Time = UT + 1;
            Logger.LogWarning($"UpdateNode: Forcing nodeData.Time to {nodeData.Time} to prevent setting a node in the past.");
            maneuverPlanComponent.UpdateTimeOnNode(nodeData, nodeData.Time); // May not need this?
        }

        // Wait a tick for things to get created
        yield return new WaitForFixedUpdate();

        // Manage the maneuver on the map
        maneuverManager.RemoveAll();
        try { maneuverManager?.GetNodeDataForVessels(); }
        catch { Logger.LogError("UpdateNode: caught exception on call to mapCore.map3D.ManeuverManager.GetNodeDataForVessels()"); }
        try { maneuverManager.UpdateAll(); }
        catch { Logger.LogError("UpdateNode: caught exception on call to mapCore.map3D.ManeuverManager.UpdateAll()"); }
        try { maneuverManager.UpdatePositionForGizmo(nodeData.NodeID); }
        catch { Logger.LogError("UpdateNode: caught exception on call to mapCore.map3D.ManeuverManager.UpdatePositionForGizmo()"); }

        // Wait a tick for things to get created
        yield return new WaitForFixedUpdate();

        try { maneuverPlanComponent.RefreshManeuverNodeState(0); } // Occasionally getting NREs here...
        catch (NullReferenceException e) { Logger.LogError($"UpdateNode: caught NRE on call to maneuverPlanComponent.RefreshManeuverNodeState(0): {e}"); }
    }

    private void handleButtons()
    {
        if (currentNode == null)
        {
            if (addNode)
            {
                // Add an empty maneuver node
                // Logger.LogInfo("handleButtons: Adding New Node");

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
            //Logger.LogInfo("handleButtons: Pushing new burn info to node");
            //Logger.LogInfo($"handleButtons: burnParams         [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s");
            maneuverPlanComponent.UpdateChangeOnNode(currentNode, burnParams);
            Logger.LogInfo($"handleButtons: Updated BurnVector    [{currentNode.BurnVector.x}, {currentNode.BurnVector.y}, {currentNode.BurnVector.z}] m/s");
            //Logger.LogInfo($"handleButtons: BurnVector.normalized [{currentNode.BurnVector.normalized.x}, {currentNode.BurnVector.normalized.y}, {currentNode.BurnVector.normalized.z}] m/s");
            // IPatchedOrbit patchedOrbit = !currentNode.IsOnManeuverTrajectory ? (IPatchedOrbit)simObject.Orbiter.PatchedConicSolver.FindPatchContainingUT(currentNode.Time) : (IPatchedOrbit)currentNode.ManeuverTrajectoryPatch;

            StartCoroutine(UpdateNode(currentNode));

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
            var oldBurnTime = currentNode.Time;
            var timeOfNodeFromNow = oldBurnTime - UT;

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
                currentNode.Time = UT + vessel.Orbit.TimeToAp;
            }
            else if (snapToPe) // Snap the maneuver time to the next Pe
            {
                currentNode.Time = UT + vessel.Orbit.TimeToPe;
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

            //Logger.LogInfo($"handleButtons: Burn time was {oldBurnTime}, is {currentNode.Time}");
            maneuverPlanComponent.UpdateTimeOnNode(currentNode, currentNode.Time); // This may not be necessary?
            
            StartCoroutine(UpdateNode(currentNode));

        }
    }
}   