using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using KSP.Game;
using KSP.Map;
using KSP.Messages;
using KSP.Sim;
using KSP.Sim.impl;
using KSP.Sim.Maneuver;
using KSP.UI.Binding;
using MNCUtilities;
using MuMech;
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
using static ManeuverNodeController.ManeuverNodeControllerMod;

namespace ManeuverNodeController;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
[BepInDependency(UitkForKsp2Plugin.ModGuid, UitkForKsp2Plugin.ModVer)]
[BepInDependency(NodeManagerPlugin.ModGuid, NodeManagerPlugin.ModVer)]
public class ManeuverNodeControllerMod : BaseSpaceWarpPlugin
{
    public static ManeuverNodeControllerMod Instance { get; set; }

    // These are useful in case some other mod wants to add a dependency to this one
    public const string ModGuid = MyPluginInfo.PLUGIN_GUID;
    public const string ModName = MyPluginInfo.PLUGIN_NAME;
    public const string ModVer = MyPluginInfo.PLUGIN_VERSION;

    // Control game input state while user has clicked into a TextField.
    // private bool gameInputState = true;
    // public List<String> inputFields = new List<String>();

    // GUI stuff
    static bool loaded = false;
    private bool interfaceEnabled = false;
    private bool GUIenabled = true;

    private ConfigEntry<KeyboardShortcut> _keybind;
    private ConfigEntry<KeyboardShortcut> _keybind2;
    public ConfigEntry<bool> previousNextEnable;
    public ConfigEntry<bool> postNodeEventLookahead;

    public enum PatchEventType
    {
        StartOfPatch,
        MidPatch,
        EndOfPatch
    }

    public SimulationObjectModel currentTarget;

    // private GameInstance game;

    internal int SelectedNodeIndex = 0;

    MncUiController controller;

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

        var mncUxml = AssetManager.GetAsset<VisualTreeAsset>($"{Info.Metadata.GUID}/mnc_ui/mnc_ui.uxml");
        var mncWindow = Window.CreateFromUxml(mncUxml, "Maneuver Node Controller Main Window", transform, true);
        UnityEngine.Object.DontDestroyOnLoad(mncWindow);
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

        GameManager.Instance.Game.Messages.Subscribe<ManeuverRemovedMessage>(msg =>
        {
            var message = (ManeuverRemovedMessage)msg;
            OnManeuverRemovedMessage(message);
        });

        Logger.LogInfo("Loaded");
        if (loaded)
        {
            Destroy(this);
        }
        loaded = true;

        gameObject.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(gameObject);

        // Register Flight AppBar button
        Appbar.RegisterAppButton(
            "Maneuver Node Cont.",
            ToolbarFlightButtonID,
            AssetManager.GetAsset<Texture2D>($"{Info.Metadata.GUID}/images/icon.png"),
            ToggleButton);

        // Register all Harmony patches in the project
        Harmony.CreateAndPatchAll(typeof(ManeuverNodeControllerMod).Assembly);

        previousNextEnable = Config.Bind<bool>("Features Section", "Previous / Next Orbit Display", true, "Enable/Disable the display of the PRevious Obrit / Next Orbit information block");
        postNodeEventLookahead = Config.Bind<bool>("Features Section", "Post-Node Event Lookahead", true, "Enable/Disable the display of the Post-Node Event Lookahead information block");

    }

    private void OnManeuverRemovedMessage(MessageCenterMessage message)
    {
        // Update the lsit of nodes to capture the effect of the node deletion
        var nodeCount = NodeManagerPlugin.Instance.RefreshManeuverNodes();
        if (nodeCount == 0)
            SelectedNodeIndex = 0;
        if (SelectedNodeIndex + 1 > nodeCount && nodeCount > 0)
            SelectedNodeIndex = nodeCount - 1;
    }

    public void ToggleButton(bool toggle)
    {
        interfaceEnabled = toggle;
        GameObject.Find(ToolbarFlightButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(interfaceEnabled);
        controller.SetEnabled(toggle);
    }

    public void LaunchMNC()
    {
        ToggleButton(true);
    }

    void Awake()
    {
        // windowRect = new Rect((Screen.width * 0.7f) - (windowWidth / 2), (Screen.height / 2) - (windowHeight / 2), 0, 0);
    }

    void Update()
    {
        if ((_keybind != null && _keybind.Value.IsDown()) || (_keybind2 != null && _keybind2.Value.IsDown()))
        {
            ToggleButton(!interfaceEnabled);
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

        MNCUtility.RefreshActiveVesselAndCurrentManeuver();

        currentTarget = MNCUtility.activeVessel?.TargetObject;
        //if (MNCUtility.activeVessel != null)
        //  orbit = MNCUtility.activeVessel.Orbit;
    }
}

public class MncUiController : KerbalMonoBehaviour
{
    private GameInstance game;

    private VisualElement _container;
    private bool initialized = false;

    private float largeStepDv = 25;
    private float smallStepDv = 5;
    private float absDvValue = 0;
    private float largeStepTime = 25;
    private float smallStepTime = 5;

    VisualElement NoNodesGroup;
    VisualElement HasNodesGroup;

    Button SnapToANtButton;//  = _container.Q<Button>("SnapToANtButton");
    Button SnapToDNtButton;

    Label NodeIndexValue;
    Label NodeMaxIndexValue;
    Label TotalDvValue;
    Label DvRemainingValue;
    Label StartTimeValue;
    Label DurationValue;
    Label ProgradeDvValue;
    Label NormalDvValue;
    Label RadialDvValue;
    Label NodeTimeValue;
    Label OrbitsLabel;
    Label PreviousApValue;
    Label PreviousPeValue;
    Label PreviousIncValue;
    Label PreviousEccValue;
    Label PreviousLANValue;
    Label NextApValue;
    Label NextPeValue;
    Label NextIncValue;
    Label NextEccValue;
    Label NextLANValue;

    VisualElement PreviousNextGroup;
    VisualElement EncounterGroup;

    // Define a custom class to store the data for each event
    public class EventData
    {
        public VisualElement Event;
        public Label EncounterBody;
        public Label EncounterType;
        public Label EncounterInfo;
    }

    // Create a list to store all the event data objects
    List<EventData> eventDataList = new List<EventData>();

    int maxNumEvents = 4;

    ManeuverNodeData thisNode = null;

    private void Start()
    {
        SetupDocument();
    }

    private void Update()
    {
        // We do need things to be initialized first...
        if (initialized)
        {
            List<ManeuverNodeData> nodes = NodeManagerPlugin.Instance.Nodes;

            // If we've got nodes...
            if (nodes.Count > 0)
            {
                HasNodesGroup.style.display = DisplayStyle.Flex;
                NoNodesGroup.style.display = DisplayStyle.None;

                int selectedNode = ManeuverNodeControllerMod.Instance.SelectedNodeIndex;
                if (selectedNode >= nodes.Count)
                {
                    selectedNode = nodes.Count - 1;
                    ManeuverNodeControllerMod.Instance.SelectedNodeIndex = selectedNode;
                }
                thisNode = nodes[selectedNode];
                double dvRemaining, UT;
                int numOrbits;
                string nextApA, nextPeA, nextInc, nextEcc, nextLAN, nextBody, nextLevel, previousApA, previousPeA, previousInc, previousEcc, previousLAN, previousBody, previousLevel;
                string previousStart, nextStart, previousEnd, nextEnd;

                PatchedConicsOrbit orbit = MNCUtility.activeVessel?.Orbit;
                OrbiterComponent Orbiter = MNCUtility.activeVessel?.Orbiter;
                ManeuverPlanSolver ManeuverPlanSolver = Orbiter?.ManeuverPlanSolver;
                List<PatchedConicsOrbit> PatchedConicsList = ManeuverPlanSolver?.PatchedConicsList;
                List<IPatchedOrbit> PatchList = new List<IPatchedOrbit>(ManeuverPlanSolver?.PatchedConicsList);

                if (orbit == null)
                    return;

                NodeIndexValue.text = (selectedNode + 1).ToString();
                NodeMaxIndexValue.text = nodes.Count.ToString();

                if (selectedNode == 0)
                    dvRemaining = (MNCUtility.activeVessel.Orbiter.ManeuverPlanSolver.GetVelocityAfterFirstManeuver(out UT).vector - orbit.GetOrbitalVelocityAtUTZup(UT)).magnitude;
                else
                    dvRemaining = thisNode.BurnRequiredDV;

                UT = game.UniverseModel.UniversalTime;
                TotalDvValue.text = nodes[selectedNode].BurnVector.magnitude.ToString("N2");
                DvRemainingValue.text = dvRemaining.ToString("N2");
                StartTimeValue.text = MNCUtility.SecondsToTimeString(thisNode.Time - UT, false);
                DurationValue.text = MNCUtility.SecondsToTimeString(thisNode.BurnDuration);
                if (thisNode.Time < UT)
                    StartTimeValue.style.color = Color.red;
                else if (thisNode.Time < UT + 30)
                    StartTimeValue.style.color = Color.yellow;
                else
                    StartTimeValue.style.color = Color.green; // may prefer white text for no warning...

                ProgradeDvValue.text = thisNode.BurnVector.z.ToString("N2");
                NormalDvValue.text = thisNode.BurnVector.y.ToString("N2");
                RadialDvValue.text = thisNode.BurnVector.x.ToString("N2");

                // If we've got a target
                if (ManeuverNodeControllerMod.Instance.currentTarget != null)
                { // If that target is orbiting the same body the active vessel is
                    if (ManeuverNodeControllerMod.Instance.currentTarget.Orbit.referenceBody == orbit.referenceBody)
                    { // Allow SnapTo ANt and DNt
                        SnapToANtButton.style.display = DisplayStyle.Flex;
                        SnapToDNtButton.style.display = DisplayStyle.Flex;
                    }
                    else
                    { // Do not Allow Snap To ANt or DNt
                        SnapToANtButton.style.display = DisplayStyle.None;
                        SnapToDNtButton.style.display = DisplayStyle.None;
                    }
                }
                else
                { // Do not Allow Snap To ANt or DNt
                    SnapToANtButton.style.display = DisplayStyle.None;
                    SnapToDNtButton.style.display = DisplayStyle.None;
                }

                // numOrbits = Math.Truncate((thisNode.Time - UT) / MNCUtility.activeVessel.Orbit.period);
                if (game.UniverseModel.FindVesselComponent(thisNode.RelatedSimID) != null)
                {
                    numOrbits = (int)Math.Truncate((thisNode.Time - game.UniverseModel.UniversalTime) / game.UniverseModel.FindVesselComponent(thisNode.RelatedSimID).Orbit.period);
                    NodeTimeValue.text = numOrbits.ToString("n0");
                    if (numOrbits == 1) OrbitsLabel.text = "orbit";
                    else OrbitsLabel.text = "orbits";
                }
                else
                {
                    NodeTimeValue.text = "NaN";
                    OrbitsLabel.text = "orbits";
                }

                // NodeTimeValue.text = numOrbits.ToString("n0");
                //if (numOrbits == 1) OrbitsLabel.text = "orbit";
                //else OrbitsLabel.text = "orbits";

                // ManeuverPlanComponent activeVesselPlan = Utility.activeVessel?.SimulationObject?.FindComponent<ManeuverPlanComponent>();
                // var nodes = activeVesselPlan?.GetNodes();


                // Get the patch info and index for the patch that contains this node
                PatchedConicsOrbit patch = null;
                var nodeTime = thisNode.Time;
                try
                {
                    ManeuverPlanSolver.FindPatchContainingUt(nodeTime, PatchList, out var thisPatch, out var patchIndex);
                }
                catch (Exception ex) { ManeuverNodeControllerMod.Logger.LogWarning($"Supporessed exception: {ex}"); }
                int nodePatchIdx = 0;
                if (nodeTime < PatchedConicsList[0].StartUT)
                {
                    nodePatchIdx = 0; // (PatchedConicsList[0].PatchEndTransition == PatchTransitionType.Encounter) ? 1 : 0;
                    patch = PatchedConicsList[nodePatchIdx];
                }
                else
                {
                    for (int i = 0; i < PatchedConicsList.Count - 1; i++)
                    {
                        if (PatchedConicsList[i].StartUT < nodeTime && nodeTime <= PatchedConicsList[i + 1].StartUT)
                        {
                            nodePatchIdx = i + 1; // (PatchedConicsList[i + 1].PatchEndTransition == PatchTransitionType.Encounter && i < PatchedConicsList.Count - 2) ? i + 2 : i + 1;
                            patch = PatchedConicsList[nodePatchIdx];
                            break;
                        }
                    }
                }
                if (patch == null)
                {
                    patch = PatchedConicsList.Last();
                    nodePatchIdx = PatchedConicsList.Count - 1;
                }

                if (ManeuverNodeControllerMod.Instance.previousNextEnable.Value)
                {
                    PreviousNextGroup.style.display = DisplayStyle.Flex;
                    PatchedConicsOrbit previousOrbit = null;
                    PatchedConicsOrbit nextOrbit = patch;
                    if (nodePatchIdx == 0)
                    {
                        previousOrbit = orbit;
                    }
                    else
                    {
                        previousOrbit = PatchedConicsList[nodePatchIdx - 1];
                    }

                    previousBody = "None";
                    previousLevel = "N/A";
                    nextBody = "None";
                    nextLevel = "N/A";

                    // Populate the previous orbit info
                    if (previousOrbit.eccentricity < 1)
                        previousApA = MNCUtility.MetersToScaledDistanceString(previousOrbit.ApoapsisArl, 3);
                    else
                        previousApA = "Inf";
                    previousPeA = MNCUtility.MetersToScaledDistanceString(previousOrbit.PeriapsisArl, 3);
                    previousInc = previousOrbit.inclination.ToString("n3");
                    previousEcc = previousOrbit.eccentricity.ToString("n3");
                    previousLAN = previousOrbit.longitudeOfAscendingNode.ToString("n3");
                    if (previousOrbit.closestEncounterBody != null)
                    {
                        previousBody = previousOrbit.closestEncounterBody.Name;
                        previousLevel = previousOrbit.closestEncounterLevel.ToString();
                    }

                    previousStart = previousOrbit.PatchStartTransition.ToString();
                    previousEnd = previousOrbit.PatchEndTransition.ToString();

                    // Populate the next orbit info
                    if (nextOrbit.eccentricity < 1)
                        nextApA = MNCUtility.MetersToScaledDistanceString(nextOrbit.ApoapsisArl, 3);
                    else
                        nextApA = "Inf";
                    nextPeA = MNCUtility.MetersToScaledDistanceString(nextOrbit.PeriapsisArl, 3);
                    nextInc = nextOrbit.inclination.ToString("n3");
                    nextEcc = nextOrbit.eccentricity.ToString("n3");
                    nextLAN = nextOrbit.longitudeOfAscendingNode.ToString("n3");

                    if (nextOrbit.closestEncounterBody != null)
                    {
                        nextBody = nextOrbit.closestEncounterBody.Name;
                        nextLevel = nextOrbit.closestEncounterLevel.ToString();

                    }
                    else if (nextOrbit.NextPatch != null && selectedNode + 1 == nodes.Count) // There's another patch and we're at the last node
                    {
                        var nextPatch = patch.NextPatch as PatchedConicsOrbit;
                        if (nextPatch.closestEncounterBody != null)
                        {
                            nextBody = nextPatch.closestEncounterBody.Name;
                            nextLevel = nextPatch.closestEncounterLevel.ToString();
                        }
                    }
                    nextStart = nextOrbit.PatchStartTransition.ToString();
                    nextEnd = nextOrbit.PatchEndTransition.ToString();

                    PreviousApValue.text = previousApA;
                    PreviousPeValue.text = previousPeA;
                    PreviousIncValue.text = previousInc + "°";
                    PreviousEccValue.text = previousEcc;
                    PreviousLANValue.text = previousLAN + "°";

                    NextApValue.text = nextApA;
                    NextPeValue.text = nextPeA;
                    NextIncValue.text = nextInc + "°";
                    NextEccValue.text = nextEcc;
                    NextLANValue.text = nextLAN + "°";
                }
                else
                    PreviousNextGroup.style.display = DisplayStyle.None;

                if (ManeuverNodeControllerMod.Instance.postNodeEventLookahead.Value)
                {
                    // Display Event Lookahead
                    int eventIdx = 0;
                    int eventCount = 0;
                    for (int patchIdx = nodePatchIdx; patchIdx < PatchedConicsList.Count; patchIdx++)
                    {
                        // Grap the current patch in the list we're processing
                        var thisPatch = PatchedConicsList[patchIdx];
                        // if we've found the end of the list of active patches or hit the max humber of reportable events
                        if (!thisPatch.ActivePatch || eventIdx > 3)
                            break;
                        // If there's an event at the start of the patch
                        if ((thisPatch.PatchStartTransition == PatchTransitionType.PartialOutOfFuel || thisPatch.PatchStartTransition == PatchTransitionType.CompletelyOutOfFuel ||
                            thisPatch.PatchStartTransition == PatchTransitionType.Escape) && eventIdx < maxNumEvents)
                        {
                            // Report encounter at start of thisPatch
                            DisplayEvent(eventIdx++, thisPatch, PatchEventType.StartOfPatch);
                            eventCount++;
                        }
                        // If there's an event during the patch
                        if (((thisPatch.PatchStartTransition == PatchTransitionType.Encounter && thisPatch.PatchEndTransition == PatchTransitionType.Escape) ||
                            thisPatch.PatchEndTransition == PatchTransitionType.PartialOutOfFuel || thisPatch.PatchEndTransition == PatchTransitionType.CompletelyOutOfFuel) && eventIdx < maxNumEvents)
                        {
                            // Report encounter durring thisPatch
                            DisplayEvent(eventIdx++, thisPatch, PatchEventType.MidPatch);
                            eventCount++;
                        }
                        // If there's an event at the end of the patch
                        if (((thisPatch.PatchEndTransition == PatchTransitionType.Encounter && thisPatch.closestEncounterLevel != EncounterSolutionLevel.None) ||
                            thisPatch.PatchEndTransition == PatchTransitionType.Collision) && eventIdx < maxNumEvents)
                        {
                            // Report encounter at end of thisPatch
                            DisplayEvent(eventIdx++, thisPatch, PatchEventType.EndOfPatch);
                            eventCount++;
                        }

                        // If we've found at least one event to display and the event group display is switched off, then switch it on
                        if (eventCount > 0 && EncounterGroup.style.display == DisplayStyle.None)
                            EncounterGroup.style.display = DisplayStyle.Flex;
                    }
                    // If there are no events to display and the event group's display is switched on, then switch it off
                    if (eventCount == 0)
                        EncounterGroup.style.display = DisplayStyle.None;
                    if (eventCount < maxNumEvents)
                    {
                        for (int idx = maxNumEvents; idx > eventCount; idx--)
                            eventDataList[idx - 1].Event.style.display = DisplayStyle.None;
                    }

                }
                else
                    EncounterGroup.style.display = DisplayStyle.None;
            }
            else
            {
                HasNodesGroup.style.display = DisplayStyle.None;
                NoNodesGroup.style.display = DisplayStyle.Flex;
            }
        }
        else
        {
            InitializeElements();
        }
    }

    // string lastBody = "N/A";

    void DisplayEvent(int idx, PatchedConicsOrbit thisPatch, PatchEventType eventType = PatchEventType.StartOfPatch)
    {
        eventDataList[idx].Event.style.display = DisplayStyle.Flex;

        //if (thisPatch.closestEncounterBody != null)
        //{
        //    eventDataList[idx].EncounterBody.text = thisPatch.closestEncounterBody.Name;
        //    // lastBody = thisPatch.closestEncounterBody.Name;
        //}
        //else
        //    eventDataList[idx].EncounterBody.text = thisPatch.referenceBody.Name;

        eventDataList[idx].EncounterType.RemoveFromClassList("unity-label-invalid");

        if (eventType == PatchEventType.StartOfPatch)
        {
            // (thisPatch.PatchStartTransition == PatchTransitionType.PartialOutOfFuel || thisPatch.PatchStartTransition == PatchTransitionType.CompletelyOutOfFuel ||
            //  thisPatch.PatchStartTransition == PatchTransitionType.Escape)
            if (thisPatch.PatchStartTransition == PatchTransitionType.Escape)
            {
                eventDataList[idx].EncounterType.text = "SOI Exit @";
                eventDataList[idx].EncounterBody.text = thisPatch.PreviousPatch.referenceBody.Name;
                eventDataList[idx].EncounterInfo.text = MNCUtility.SecondsToTimeString(thisPatch.StartUT - Game.UniverseModel.UniversalTime);
            }
            else if (thisPatch.PatchStartTransition == PatchTransitionType.PartialOutOfFuel) // Does this ever occur?
            {
                eventDataList[idx].EncounterType.text = "Partial Out of Fuel @";
                eventDataList[idx].EncounterBody.text = thisPatch.PreviousPatch.referenceBody.Name;
                eventDataList[idx].EncounterInfo.text = MNCUtility.SecondsToTimeString(thisPatch.StartUT - Game.UniverseModel.UniversalTime);
            }
            else if (thisPatch.PatchStartTransition == PatchTransitionType.CompletelyOutOfFuel) // Does this ever occur?
            {
                eventDataList[idx].EncounterType.text = "Out of Fuel @";
                eventDataList[idx].EncounterBody.text = thisPatch.PreviousPatch.referenceBody.Name;
                eventDataList[idx].EncounterInfo.text = MNCUtility.SecondsToTimeString(thisPatch.StartUT - Game.UniverseModel.UniversalTime);
            }
            //else if (thisPatch.PatchStartTransition == PatchTransitionType.Encounter)
            //{
            //    // do what?
            //}
        }
        else if (eventType == PatchEventType.MidPatch)
        {
            // ((thisPatch.PatchStartTransition == PatchTransitionType.Encounter && thisPatch.PatchEndTransition == PatchTransitionType.Escape) ||
            //   thisPatch.PatchEndTransition == PatchTransitionType.PartialOutOfFuel || thisPatch.PatchEndTransition == PatchTransitionType.CompletelyOutOfFuel)
            if (thisPatch.PatchEndTransition == PatchTransitionType.Escape)
            {
                eventDataList[idx].EncounterType.text = "Fly By @";
                eventDataList[idx].EncounterBody.text = thisPatch.referenceBody.Name;
                eventDataList[idx].EncounterInfo.text = "Pe: " + MNCUtility.MetersToScaledDistanceString(thisPatch.PeriapsisArl, 3) + $", inc: {thisPatch.inclination:N1}°";
            }
            else if (thisPatch.PatchEndTransition == PatchTransitionType.PartialOutOfFuel) // Does this ever occur?
            {
                eventDataList[idx].EncounterType.text = "Partial Out of Fuel @";
                eventDataList[idx].EncounterBody.text = thisPatch.referenceBody.Name;
                eventDataList[idx].EncounterInfo.text = MNCUtility.SecondsToTimeString(thisPatch.EndUT - Game.UniverseModel.UniversalTime);
            }
            else if (thisPatch.PatchEndTransition == PatchTransitionType.CompletelyOutOfFuel) // Does this ever occur?
            {
                eventDataList[idx].EncounterType.text = "Out of Fuel @";
                eventDataList[idx].EncounterBody.text = thisPatch.referenceBody.Name;
                eventDataList[idx].EncounterInfo.text = MNCUtility.SecondsToTimeString(thisPatch.EndUT - Game.UniverseModel.UniversalTime);
            }
        }
        else if (eventType == PatchEventType.EndOfPatch)
        {
            // ((thisPatch.PatchEndTransition == PatchTransitionType.Encounter && thisPatch.closestEncounterLevel != EncounterSolutionLevel.None) ||
            //   thisPatch.PatchEndTransition == PatchTransitionType.Collision)
            if (thisPatch.PatchEndTransition == PatchTransitionType.Collision)
            {
                eventDataList[idx].EncounterType.AddToClassList("unity-label-invalid");
                eventDataList[idx].EncounterBody.text = thisPatch.referenceBody.Name;
                eventDataList[idx].EncounterType.text = "Collision @";
                eventDataList[idx].EncounterInfo.text = MNCUtility.SecondsToTimeString(thisPatch.EndUT - Game.UniverseModel.UniversalTime);
            }
            else if (thisPatch.closestEncounterLevel != EncounterSolutionLevel.None)
            {
                switch (thisPatch.closestEncounterLevel)
                {
                    case EncounterSolutionLevel.OrbitIntersect:
                        eventDataList[idx].EncounterType.text = "Orbit Intersect @";
                        eventDataList[idx].EncounterInfo.text = MNCUtility.SecondsToTimeString(thisPatch.EndUT - Game.UniverseModel.UniversalTime);
                        break;
                    case EncounterSolutionLevel.SoiIntersect1:
                        if (thisPatch.NextPatch.referenceBody.Mass >= thisPatch.referenceBody.Mass)
                        {
                            eventDataList[idx].EncounterType.text = "SOI Exit @";
                            eventDataList[idx].EncounterBody.text = thisPatch.referenceBody.Name;
                        }
                        else
                        {
                            eventDataList[idx].EncounterType.text = "SOI Entry @";
                            eventDataList[idx].EncounterBody.text = thisPatch.NextPatch.referenceBody.Name;
                        }
                        eventDataList[idx].EncounterInfo.text = MNCUtility.SecondsToTimeString(thisPatch.EndUT - Game.UniverseModel.UniversalTime);
                        break;
                    case EncounterSolutionLevel.SoiIntersect2:
                        if (thisPatch.NextPatch.referenceBody.Mass >= thisPatch.referenceBody.Mass)
                        {
                            eventDataList[idx].EncounterType.text = "SOI Exit @";
                            eventDataList[idx].EncounterBody.text = thisPatch.referenceBody.Name;
                        }
                        else
                        {
                            eventDataList[idx].EncounterType.text = "SOI Entry @";
                            eventDataList[idx].EncounterBody.text = thisPatch.NextPatch.referenceBody.Name;
                        }
                        eventDataList[idx].EncounterInfo.text = MNCUtility.SecondsToTimeString(thisPatch.StartUT - Game.UniverseModel.UniversalTime);
                        break;
                    default:
                        break;
                }
            }

        }

        //else
        //    eventDataList[idx].Event.style.display = DisplayStyle.None;
    }
    public void SetEnabled(bool newState)
    {
        if (newState) _container.style.display = DisplayStyle.Flex;
        else _container.style.display = DisplayStyle.None;
        GameObject.Find(ManeuverNodeControllerMod.ToolbarFlightButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(newState);
        // ManeuverNodeControllerMod.Instance.ToggleButton(newState);

        //interfaceEnabled = newState;
        //GameObject.Find(ToolbarFlightButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(interfaceEnabled);
    }

    public void SetupDocument()
    {
        var document = GetComponent<UIDocument>();
        if (document.TryGetComponent<DocumentLocalization>(out var localization))
        {
            localization.Localize();
        }
        else
        {
            document.EnableLocalization();
        }

        _container = document.rootVisualElement;
        _container[0].transform.position = new Vector2(800, 50);
        _container[0].CenterByDefault();
        _container.style.display = DisplayStyle.None;

        document.rootVisualElement.Query<TextField>().ForEach(textField =>
        {
            textField.RegisterCallback<FocusInEvent>(_ => GameManager.Instance?.Game?.Input.Disable());
            textField.RegisterCallback<FocusOutEvent>(_ => GameManager.Instance?.Game?.Input.Enable());

            textField.RegisterValueChangedCallback((evt) =>
        {
            ManeuverNodeControllerMod.Logger.LogDebug($"TryParse attempt for {textField.name}. Tooltip = {textField.tooltip}");
            if (float.TryParse(evt.newValue, out _))
            {
                textField.RemoveFromClassList("unity-text-field-invalid");
                ManeuverNodeControllerMod.Logger.LogDebug($"TryParse success for {textField.name}, nValue = '{evt.newValue}': Removed unity-text-field-invalid from class list");
            }
            else
            {
                textField.AddToClassList("unity-text-field-invalid");
                ManeuverNodeControllerMod.Logger.LogDebug($"TryParse failure for {textField.name}, nValue = '{evt.newValue}': Added unity-text-field-invalid to class list");
                ManeuverNodeControllerMod.Logger.LogDebug($"document.rootVisualElement.transform.position.z = {document.rootVisualElement.transform.position.z}");
            }
        });

            textField.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
            textField.RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());
            textField.RegisterCallback<PointerMoveEvent>(evt => evt.StopPropagation());
        });
    }

    public void InitializeElements()
    {
        game = GameManager.Instance.Game;
        bool pass;

        ManeuverNodeControllerMod.Logger.LogInfo($"MNC: Starting UITK GUI Initialization. initialized is set to {initialized}");

        // Set up variables to be able to access UITK GUI panel groups quickly (Queries are expensive) 
        NoNodesGroup = _container.Q<VisualElement>("NoNodesGroup");
        HasNodesGroup = _container.Q<VisualElement>("HasNodesGroup");

        ManeuverNodeControllerMod.Logger.LogInfo($"MNC: Panel groups initialized. initialized is set to {initialized}");

        // Set up variables to be able to access UITK GUI Buttons quickly (Queries are expensive) 
        SnapToANtButton = _container.Q<Button>("SnapToANtButton");
        SnapToDNtButton = _container.Q<Button>("SnapToDNtButton");

        ManeuverNodeControllerMod.Logger.LogInfo($"MNC: SnapTo buttons initialized. initialized is set to {initialized}");

        // Set up variables to be able to access UITK GUI labels quickly (Queries are expensive) 
        NodeIndexValue = _container.Q<Label>("NodeIndexValue");
        NodeMaxIndexValue = _container.Q<Label>("NodeMaxIndexValue");
        TotalDvValue = _container.Q<Label>("TotalDvValue");
        DvRemainingValue = _container.Q<Label>("DvRemainingValue");
        StartTimeValue = _container.Q<Label>("StartTimeValue");
        DurationValue = _container.Q<Label>("DurationValue");
        ProgradeDvValue = _container.Q<Label>("ProgradeDvValue");
        NormalDvValue = _container.Q<Label>("NormalDvValue");
        RadialDvValue = _container.Q<Label>("RadialDvValue");
        NodeTimeValue = _container.Q<Label>("NodeTimeValue");
        OrbitsLabel = _container.Q<Label>("OrbitsLabel");
        PreviousApValue = _container.Q<Label>("PreviousApValue");
        PreviousPeValue = _container.Q<Label>("PreviousPeValue");
        PreviousIncValue = _container.Q<Label>("PreviousIncValue");
        PreviousEccValue = _container.Q<Label>("PreviousEccValue");
        PreviousLANValue = _container.Q<Label>("PreviousLANValue");
        NextApValue = _container.Q<Label>("NextApValue");
        NextPeValue = _container.Q<Label>("NextPeValue");
        NextIncValue = _container.Q<Label>("NextIncValue");
        NextEccValue = _container.Q<Label>("NextEccValue");
        NextLANValue = _container.Q<Label>("NextLANValue");
        PreviousNextGroup = _container.Q<VisualElement>("PreviousNextGroup");
        EncounterGroup = _container.Q<VisualElement>("EncounterGroup");

        //EncounterInfo.Add(EncounterInfo4);

        // Loop through each event and initialize the data objects
        for (int i = 1; i <= maxNumEvents; i++)
        {
            EventData eventData = new EventData();
            eventData.Event = _container.Q<VisualElement>("Event" + i);
            eventData.EncounterBody = _container.Q<Label>("EncounterBody" + i);
            eventData.EncounterType = _container.Q<Label>("EncounterType" + i);
            eventData.EncounterInfo = _container.Q<Label>("EncounterInfo" + i);

            // Add the data object to the list
            eventDataList.Add(eventData);
        }

        ManeuverNodeControllerMod.Logger.LogInfo($"MNC: SnapTo labels initialized. initialized is set to {initialized}");

        _container.Q<Button>("CloseButton").clicked += () => ManeuverNodeControllerMod.Instance.ToggleButton(false);

        _container.Q<Button>("AddFirstNodeButton").clicked += AddManeuverNode;

        _container.Q<Button>("DecreaseNodeIndexButton").clicked += PreviousManeuverNode;
        _container.Q<Button>("IncreaseNodeIndexButton").clicked += NextManeuverNode;

        _container.Q<Button>("DelNodeButton").clicked += DelManeuverNode;
        _container.Q<Button>("CheckNodeButton").clicked += CheckManeuverNode;
#if DEBUG
        _container.Q<Button>("CheckNodeButton").style.display = DisplayStyle.Flex;
#endif
        _container.Q<Button>("AddNodeButton").clicked += AddManeuverNode;

        // pass = float.TryParse(_container.Q<TextField>("AbsoluteDvInput").value, out absDvValue);
        _container.Q<TextField>("AbsoluteDvInput").RegisterValueChangedCallback((evt) =>
        {
            if (float.TryParse(evt.newValue, out float newFloat))
            {
                absDvValue = newFloat;
            }
        });
        _container.Q<TextField>("AbsoluteDvInput").value = absDvValue.ToString();

        // pass = float.TryParse(_container.Q<TextField>("SmallStepDvInput").value, out smallStepDv);
        _container.Q<TextField>("SmallStepDvInput").RegisterValueChangedCallback((evt) =>
        {
            if (float.TryParse(evt.newValue, out float newFloat))
            {
                smallStepDv = newFloat;
            }
        });
        _container.Q<TextField>("SmallStepDvInput").value = smallStepDv.ToString();

        // pass = float.TryParse(_container.Q<TextField>("LargeStepDvInput").value, out largeStepDv);
        _container.Q<TextField>("LargeStepDvInput").RegisterValueChangedCallback((evt) =>
        {
            if (float.TryParse(evt.newValue, out float newFloat))
            {
                largeStepDv = newFloat;
            }
        });
        _container.Q<TextField>("LargeStepDvInput").value = largeStepDv.ToString();

        // pass = float.TryParse(_container.Q<TextField>("SmallTimeStepInput").value, out smallStepTime);
        _container.Q<TextField>("SmallTimeStepInput").RegisterValueChangedCallback((evt) =>
        {
            if (float.TryParse(evt.newValue, out float newFloat))
            {
                smallStepTime = newFloat;
            }
        });
        _container.Q<TextField>("SmallTimeStepInput").value = smallStepTime.ToString();

        // pass = float.TryParse(_container.Q<TextField>("LargeTimeStepInput").value, out largeStepTime);
        _container.Q<TextField>("LargeTimeStepInput").RegisterValueChangedCallback((evt) =>
        {
            if (float.TryParse(evt.newValue, out float newFloat))
            {
                largeStepTime = newFloat;
            }
        });
        _container.Q<TextField>("LargeTimeStepInput").value = largeStepTime.ToString();

        _container.Q<Button>("LargeProgradeDecreaseButton").clicked += () => IncrementPrograde(-largeStepDv);
        _container.Q<Button>("SmallProgradeDecreaseButton").clicked += () => IncrementPrograde(-smallStepDv);
        _container.Q<Button>("SmallProgradeIncreaseButton").clicked += () => IncrementPrograde(smallStepDv);
        _container.Q<Button>("LargeProgradeIncreaseButton").clicked += () => IncrementPrograde(largeStepDv);
        _container.Q<Button>("AbsoluteProgradeButton").clicked += () => SetPrograde(absDvValue);

        _container.Q<Button>("LargeNormalDecreaseButton").clicked += () => IncrementNormal(-largeStepDv);
        _container.Q<Button>("SmallNormalDecreaseButton").clicked += () => IncrementNormal(-smallStepDv);
        _container.Q<Button>("SmallNormalIncreaseButton").clicked += () => IncrementNormal(smallStepDv);
        _container.Q<Button>("LargeNormalIncreaseButton").clicked += () => IncrementNormal(largeStepDv);
        _container.Q<Button>("AbsoluteNormalButton").clicked += () => SetNormal(absDvValue);

        _container.Q<Button>("LargeRadialDecreaseButton").clicked += () => IncrementRadial(-largeStepDv);
        _container.Q<Button>("SmallRadialDecreaseButton").clicked += () => IncrementRadial(-smallStepDv);
        _container.Q<Button>("SmallRadialIncreaseButton").clicked += () => IncrementRadial(smallStepDv);
        _container.Q<Button>("LargeRadialIncreaseButton").clicked += () => IncrementRadial(largeStepDv);
        _container.Q<Button>("AbsoluteRadialButton").clicked += () => SetRadial(absDvValue);

        _container.Q<Button>("SnapToApButton").clicked += SnapToAp;
        _container.Q<Button>("SnapToPeButton").clicked += SnapToPe;
        _container.Q<Button>("SnapToANeButton").clicked += SnapToANe;
        _container.Q<Button>("SnapToDNeButton").clicked += SnapToDNe;
        _container.Q<Button>("SnapToANtButton").clicked += SnapToANt;
        _container.Q<Button>("SnapToDNtButton").clicked += SnapToDNt;

        _container.Q<Button>("LargeTimeDecreaseButton").clicked += () => IncrementTime(-largeStepTime);
        _container.Q<Button>("SmallTimeDecreaseButton").clicked += () => IncrementTime(-smallStepTime);
        _container.Q<Button>("SmallTimeIncreaseButton").clicked += () => IncrementTime(smallStepTime);
        _container.Q<Button>("LargeTimeIncreaseButton").clicked += () => IncrementTime(largeStepTime);

        _container.Q<Button>("DecreaseOrbitButton").clicked += () => IncrementOrbit(-1);
        _container.Q<Button>("IncreaseOrbitButton").clicked += () => IncrementOrbit(1);

        initialized = true;
        ManeuverNodeControllerMod.Logger.LogInfo($"MNC: UITK GUI Initialized. initialized set to {initialized}");
    }

    public void BindFunctions()
    {

    }

    public void SetDefaults()
    {

    }

    public void UnbindFunctions()
    {

    }

    void NextManeuverNode()
    {
        // increment if possible, or wrap around
        if (ManeuverNodeControllerMod.Instance.SelectedNodeIndex + 1 < NodeManagerPlugin.Instance.Nodes.Count)
        {
            ManeuverNodeControllerMod.Instance.SelectedNodeIndex++;
            thisNode = NodeManagerPlugin.Instance.Nodes[ManeuverNodeControllerMod.Instance.SelectedNodeIndex];
        }
    }

    void PreviousManeuverNode()
    {
        // Decrement if possible, or wrap around
        if (ManeuverNodeControllerMod.Instance.SelectedNodeIndex > 0)
        {
            ManeuverNodeControllerMod.Instance.SelectedNodeIndex--;
            thisNode = NodeManagerPlugin.Instance.Nodes[ManeuverNodeControllerMod.Instance.SelectedNodeIndex];
        }
    }

    void AddManeuverNode()
    {
        // Combined process from original to account for either first node or adding a node in general
        bool firstNode = NodeManagerPlugin.Instance.Nodes.Count == 0;
        bool pass;

        if (firstNode)
        {
            pass = NodeManagerPlugin.Instance.AddNode();
            ManeuverNodeControllerMod.Instance.SelectedNodeIndex = 0;
            NodeManagerPlugin.Instance.SpitNode(ManeuverNodeControllerMod.Instance.SelectedNodeIndex);
            // StartCoroutine(NodeManagerPlugin.Instance.RefreshNodes());
        }
        else
        {
            // StartCoroutine(NodeManagerPlugin.Instance.RefreshNodes());
            pass = NodeManagerPlugin.Instance.AddNode();
            ManeuverNodeControllerMod.Instance.SelectedNodeIndex = NodeManagerPlugin.Instance.Nodes.Count - 1;
            NodeManagerPlugin.Instance.SpitNode(ManeuverNodeControllerMod.Instance.SelectedNodeIndex);
        }

    }

    void DelManeuverNode()
    {
        // Identical process to original
        NodeManagerPlugin.Instance.DeleteNodes(ManeuverNodeControllerMod.Instance.SelectedNodeIndex);
        if (ManeuverNodeControllerMod.Instance.SelectedNodeIndex > 0)
            ManeuverNodeControllerMod.Instance.SelectedNodeIndex--;
        else
            ManeuverNodeControllerMod.Instance.SelectedNodeIndex = 0;
    }

    void CheckManeuverNode()
    {
        NodeManagerPlugin.Instance.SpitNode(ManeuverNodeControllerMod.Instance.SelectedNodeIndex);
        OrbiterComponent Orbiter = MNCUtility.activeVessel?.Orbiter;
        ManeuverPlanSolver ManeuverPlanSolver = Orbiter?.ManeuverPlanSolver;
        List<PatchedConicsOrbit> patch = ManeuverPlanSolver?.PatchedConicsList;

        int nodePatchIdx = 0;
        double nodeTime = NodeManagerPlugin.Instance.Nodes[ManeuverNodeControllerMod.Instance.SelectedNodeIndex].Time;
        if (nodeTime < patch[0].StartUT)
        {
            nodePatchIdx = 0; // (PatchedConicsList[0].PatchEndTransition == PatchTransitionType.Encounter) ? 1 : 0;
        }
        else
        {
            for (int i = 0; i < patch.Count - 1; i++)
            {
                if (patch[i].StartUT < nodeTime && nodeTime <= patch[i + 1].StartUT)
                {
                    nodePatchIdx = i + 1; // (PatchedConicsList[i + 1].PatchEndTransition == PatchTransitionType.Encounter && i < PatchedConicsList.Count - 2) ? i + 2 : i + 1;
                    break;
                }
            }
        }

        ManeuverNodeControllerMod.Logger.LogInfo($"Node Patch {nodePatchIdx}");
        int eventCount = 0;
        for (int i = 0; i < patch.Count; i++)
        {
            var thisPatch = patch[i];
            if (!thisPatch.ActivePatch)
                break;
            if (thisPatch.closestEncounterBody != null)
                ManeuverNodeControllerMod.Logger.LogInfo($"Patch {i}: Active {thisPatch.ActivePatch}, SOI Encounter {thisPatch.UniversalTimeAtSoiEncounter}, Start {thisPatch.PatchStartTransition} @ {thisPatch.StartUT:N3}, End {thisPatch.PatchEndTransition} @ {thisPatch.EndUT:N3}, referenceBody: {thisPatch.referenceBody.Name}, Encounter: {thisPatch.closestEncounterBody.Name} ({thisPatch.closestEncounterLevel})");
            else
                ManeuverNodeControllerMod.Logger.LogInfo($"Patch {i}: Active {thisPatch.ActivePatch}, SOI Encounter {thisPatch.UniversalTimeAtSoiEncounter}, Start {thisPatch.PatchStartTransition} @ {thisPatch.StartUT:N3}, End {thisPatch.PatchEndTransition} @ {thisPatch.EndUT:N3}, referenceBody: {thisPatch.referenceBody.Name}");

            // If there's an event at the start of the patch
            if (thisPatch.PatchStartTransition == PatchTransitionType.PartialOutOfFuel || thisPatch.PatchStartTransition == PatchTransitionType.CompletelyOutOfFuel ||
                thisPatch.PatchStartTransition == PatchTransitionType.Escape)
            {
                // Report encounter at start of thisPatch
                ManeuverNodeControllerMod.Logger.LogInfo($"Patch {i}: Has event at start of patch");
                eventCount++;
            }
            // If there's an event during the patch
            if ((thisPatch.PatchStartTransition == PatchTransitionType.Encounter && thisPatch.PatchEndTransition == PatchTransitionType.Escape) ||
                thisPatch.PatchEndTransition == PatchTransitionType.PartialOutOfFuel || thisPatch.PatchEndTransition == PatchTransitionType.CompletelyOutOfFuel)
            {
                // Report encounter durring thisPatch
                ManeuverNodeControllerMod.Logger.LogInfo($"Patch {i}: Has mid-patch event");
                eventCount++;
            }
            // If there's an event at the end of the patch
            if ((thisPatch.PatchEndTransition == PatchTransitionType.Encounter && thisPatch.closestEncounterLevel != EncounterSolutionLevel.None) ||
                thisPatch.PatchEndTransition == PatchTransitionType.Collision)
            {
                // Report encounter at end of thisPatch
                ManeuverNodeControllerMod.Logger.LogInfo($"Patch {i}: Has event at end of patch");
                eventCount++;
            }
        }
        ManeuverNodeControllerMod.Logger.LogInfo($"Events Detected {eventCount}");
    }

    void IncrementPrograde(float amount)
    {
        Vector3d burnParams = Vector3d.zero;
        burnParams.z += amount;
        ManeuverNodeControllerMod.Logger.LogDebug($"IncrementPrograde: amount = {amount} BurnParams =[{burnParams.x:n3}, {burnParams.y:n3}, {burnParams.z:n3}]");
        ApplyChange(0, burnParams);
    }

    void IncrementNormal(float amount)
    {
        Vector3d burnParams = Vector3d.zero;
        burnParams.y += amount;
        ManeuverNodeControllerMod.Logger.LogDebug($"IncrementNormal: amount = {amount} BurnParams =[{burnParams.x:n3}, {burnParams.y:n3}, {burnParams.z:n3}]");
        ApplyChange(0, burnParams);
    }

    void IncrementRadial(float amount)
    {
        Vector3d burnParams = Vector3d.zero;
        burnParams.x += amount;
        ManeuverNodeControllerMod.Logger.LogDebug($"IncrementRadial: amount = {amount} BurnParams =[{burnParams.x:n3}, {burnParams.y:n3}, {burnParams.z:n3}]");
        ApplyChange(0, burnParams);
    }

    void SetPrograde(float amount)
    {
        Vector3d burnParams = Vector3d.zero;
        burnParams.z = amount - thisNode.BurnVector.z;
        ManeuverNodeControllerMod.Logger.LogDebug($"SetPrograde: amount = {amount} BurnParams =[{burnParams.x:n3}, {burnParams.y:n3}, {burnParams.z:n3}]");
        ApplyChange(0, burnParams);
    }

    void SetNormal(float amount)
    {
        Vector3d burnParams = Vector3d.zero;
        burnParams.y = amount - thisNode.BurnVector.y;
        ManeuverNodeControllerMod.Logger.LogDebug($"SetNormal: amount = {amount} BurnParams =[{burnParams.x:n3}, {burnParams.y:n3}, {burnParams.z:n3}]");
        ApplyChange(0, burnParams);
    }

    void SetRadial(float amount)
    {
        Vector3d burnParams = Vector3d.zero;
        burnParams.x = amount - thisNode.BurnVector.x;
        ManeuverNodeControllerMod.Logger.LogDebug($"SetRadial: amount = {amount} BurnParams =[{burnParams.x:n3}, {burnParams.y:n3}, {burnParams.z:n3}]");
        ApplyChange(0, burnParams);
    }

    void IncrementTime(float amount)
    {
        ManeuverNodeControllerMod.Logger.LogDebug($"IncrementTime: amount = {amount}");
        ApplyChange(thisNode.Time + amount, Vector3d.zero);
    }

    void IncrementOrbit(float amount)
    {
        ManeuverNodeControllerMod.Logger.LogDebug($"IncrementOrbit: amount = {amount}");
        IncrementTime(amount * (float)MNCUtility.activeVessel.Orbit.period);
    }

    void SnapToAp()
    {
        ApplyChange(game.UniverseModel.UniversalTime + MNCUtility.activeVessel.Orbit.TimeToAp, Vector3d.zero);
    }

    void SnapToPe()
    {
        ApplyChange(game.UniverseModel.UniversalTime + MNCUtility.activeVessel.Orbit.TimeToPe, Vector3d.zero);
    }

    void SnapToANe()
    {
        ApplyChange(MNCUtility.activeVessel.Orbit.TimeOfAscendingNodeEquatorial(game.UniverseModel.UniversalTime), Vector3d.zero);
    }

    void SnapToDNe()
    {
        ApplyChange(MNCUtility.activeVessel.Orbit.TimeOfDescendingNodeEquatorial(game.UniverseModel.UniversalTime), Vector3d.zero);
    }

    void SnapToANt()
    {
        ApplyChange(MNCUtility.activeVessel.Orbit.TimeOfAscendingNode(MNCUtility.currentTarget.Orbit, game.UniverseModel.UniversalTime), Vector3d.zero);
    }

    void SnapToDNt()
    {
        ApplyChange(MNCUtility.activeVessel.Orbit.TimeOfDescendingNode(MNCUtility.currentTarget.Orbit, game.UniverseModel.UniversalTime), Vector3d.zero);
    }

    void ApplyChange(double nodeTime, Vector3d burnParams)
    {
        // Get some objects and info we need
        GameManager.Instance.Game.Map.TryGetMapCore(out MapCore mapCore);
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

        // ManeuverNodeData thisNode = NodeManagerPlugin.Instance.Nodes[ManeuverNodeControllerMod.Instance.SelectedNodeIndex];
        VesselComponent vessel = game.UniverseModel.FindVesselComponent(thisNode.RelatedSimID);
        // var target = vessel?.TargetObject;
        double UT = game.UniverseModel.UniversalTime;

        if (nodeTime > 0)
        {
            // double oldBurnTime = thisNode.Time;
            // double timeOfNodeFromNow = oldBurnTime - UT;

            double minTime = UT + Math.Max(smallStepTime, 5);
            double maxTime = UT - 1;
            if (ManeuverNodeControllerMod.Instance.SelectedNodeIndex > 0)
                minTime = NodeManagerPlugin.Instance.Nodes[ManeuverNodeControllerMod.Instance.SelectedNodeIndex - 1].Time + Math.Max(smallStepTime, 5);
            if (ManeuverNodeControllerMod.Instance.SelectedNodeIndex < NodeManagerPlugin.Instance.Nodes.Count - 1)
                maxTime = NodeManagerPlugin.Instance.Nodes[ManeuverNodeControllerMod.Instance.SelectedNodeIndex + 1].Time - Math.Max(smallStepTime, 5);

            if (nodeTime < minTime) // Not allowed to move the node prior to anopther node
            {
                nodeTime = minTime;
                ManeuverNodeControllerMod.Logger.LogDebug($"Limiting nodeTime to no less than {(nodeTime - UT):F3} from now.");
            }
            if (maxTime > minTime && nodeTime > maxTime) // Not allowed to move the node ahead of a later node
            {
                nodeTime = maxTime;
                ManeuverNodeControllerMod.Logger.LogDebug($"Limiting nodeTime to no more than {(nodeTime - UT):F3} from now.");
            }

            // Push the update to the node
            maneuverPlanComponent.UpdateTimeOnNode(thisNode, nodeTime);

            ManeuverNodeControllerMod.Logger.LogDebug($"nodeTime after adjust  : {(nodeTime - UT):F3} from now.");
        }
        if (burnParams.magnitude != 0)
        {
            // Push the update to the node
            maneuverPlanComponent.UpdateChangeOnNode(thisNode, burnParams);

            ManeuverNodeControllerMod.Logger.LogDebug($"ApplyChange: Updated BurnVector    [{thisNode.BurnVector.x:F3}, {thisNode.BurnVector.y:F3}, {thisNode.BurnVector.z:F3}] m/s");

        }

        // Call RefreshNodes to update the nodes in a way that allows the game to catch up with the updates
        StartCoroutine(NodeManagerPlugin.Instance.RefreshNodes());
    }
}