using System.Globalization;
using KSP.Game;
using KSP.Sim;
using KSP.Sim.impl;
using KSP.Sim.Maneuver;
using KSP.UI.Binding;
using MuMech;
using NodeManager;
using UitkForKsp2.API;
using UnityEngine;
using UnityEngine.UIElements;

namespace ManeuverNodeController;

public class MncUiController : KerbalMonoBehaviour
{
    private GameInstance game;

    private VisualElement _container;
    private bool initialized = false;

    private float largeStepDv = 25;
    private float smallStepDv = 5;
    private float largeStepTime = 25;
    private float smallStepTime = 5;

    // private long myDelay = ManeuverNodeControllerPlugin.Instance.repeatDelay.Value;
    // private long myInterval = ManeuverNodeControllerPlugin.Instance.repeatInterval.Value;

    private VisualElement NoNodesGroup;
    private VisualElement HasNodesGroup;

    private Button SnapToANtButton;//  = _container.Q<Button>("SnapToANtButton");
    private Button SnapToDNtButton;

    private Button SmallStepDvIncrementUpButton;
    private Button SmallStepDvIncrementDownButton;
    private Button LargeStepDvIncrementUpButton;
    private Button LargeStepDvIncrementDownButton;
    private Button SmallTimeStepIncrementUpButton;
    private Button SmallTimeStepIncrementDownButton;
    private Button LargeTimeStepIncrementUpButton;
    private Button LargeTimeStepIncrementDownButton;

    private TextField SmallStepDvInput;
    private TextField LargeStepDvInput;
    private TextField SmallTimeStepInput;
    private TextField LargeTimeStepInput;
    private TextField ProgradeDvInput;
    private TextField NormalDvInput;
    private TextField RadialDvInput;

    private Label NodeIndexValue;
    private Label NodeMaxIndexValue;
    private Label TotalDvValue;
    private Label DvRemainingValue;
    private Label StartTimeValue;
    private Label DurationValue;
    private Label NodeTimeValue;
    private Label OrbitsLabel;
    private Label PreviousApValue;
    private Label PreviousPeValue;
    private Label PreviousIncValue;
    private Label PreviousEccValue;
    private Label PreviousLANValue;
    private Label NextApValue;
    private Label NextPeValue;
    private Label NextIncValue;
    private Label NextEccValue;
    private Label NextLANValue;

    private Foldout PreviousNextFoldout;
    private Foldout EncounterFoldout;

    // Define a custom class to store the data for each event
    public class EventData
    {
        public VisualElement Event;
        public Label EncounterBody;
        public Label EncounterType;
        public Label EncounterInfo;
    }

    // Create a list to store all the event data objects
    private List<EventData> eventDataList = new List<EventData>();

    private int maxNumEvents = 4;

    private ManeuverNodeData thisNode = null;

    private void Start()
    {
        SetupDocument();
    }

    private void Update()
    {
        // We do need things to be initialized first...
        if (!initialized)
        {
            InitializeElements();
            return;
        }

        List<ManeuverNodeData> nodes = new List<ManeuverNodeData>();
        if (NodeManagerPlugin.Instance.Nodes != null)
            nodes = NodeManagerPlugin.Instance.Nodes;

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

            PatchedConicsOrbit orbit = MncUtility.activeVessel?.Orbit;
            OrbiterComponent Orbiter = MncUtility.activeVessel?.Orbiter;
            ManeuverPlanSolver ManeuverPlanSolver = Orbiter?.ManeuverPlanSolver;
            List<PatchedConicsOrbit> PatchedConicsList = ManeuverPlanSolver?.PatchedConicsList;
            List<IPatchedOrbit> PatchList = new List<IPatchedOrbit>(ManeuverPlanSolver?.PatchedConicsList);

            if (orbit == null)
                return;

            NodeIndexValue.text = (selectedNode + 1).ToString();
            NodeMaxIndexValue.text = nodes.Count.ToString();

            if (selectedNode == 0)
                dvRemaining = (MncUtility.activeVessel.Orbiter.ManeuverPlanSolver.GetVelocityAfterFirstManeuver(out UT).vector - orbit.GetOrbitalVelocityAtUTZup(UT)).magnitude;
            else
                dvRemaining = thisNode.BurnRequiredDV;

            UT = game.UniverseModel.UniverseTime;
            TotalDvValue.text = nodes[selectedNode].BurnVector.magnitude.ToString("N2");
            DvRemainingValue.text = dvRemaining.ToString("N2");
            StartTimeValue.text = MncUtility.SecondsToTimeString(thisNode.Time - UT, false);
            DurationValue.text = MncUtility.SecondsToTimeString(thisNode.BurnDuration);
            if (thisNode.Time < UT)
                StartTimeValue.style.color = Color.red;
            else if (thisNode.Time < UT + 30)
                StartTimeValue.style.color = Color.yellow;
            else
                StartTimeValue.style.color = Color.green; // may prefer white text for no warning...

            // Update delta-v text fields
            ProgradeDvInput.value = thisNode.BurnVector.z.ToString(CultureInfo.InvariantCulture);
            NormalDvInput.value = thisNode.BurnVector.y.ToString(CultureInfo.InvariantCulture);
            RadialDvInput.value = thisNode.BurnVector.x.ToString(CultureInfo.InvariantCulture);

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
                numOrbits = (int)Math.Truncate((thisNode.Time - game.UniverseModel.UniverseTime) / game.UniverseModel.FindVesselComponent(thisNode.RelatedSimID).Orbit.period);
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
                PreviousNextFoldout.style.display = DisplayStyle.Flex;
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
                    previousApA = MncUtility.MetersToScaledDistanceString(previousOrbit.ApoapsisArl, 3);
                else
                    previousApA = "Inf";
                previousPeA = MncUtility.MetersToScaledDistanceString(previousOrbit.PeriapsisArl, 3);
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
                    nextApA = MncUtility.MetersToScaledDistanceString(nextOrbit.ApoapsisArl, 3);
                else
                    nextApA = "Inf";
                nextPeA = MncUtility.MetersToScaledDistanceString(nextOrbit.PeriapsisArl, 3);
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
                PreviousNextFoldout.style.display = DisplayStyle.None;

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
                        DisplayEvent(eventIdx++, thisPatch, ManeuverNodeControllerMod.PatchEventType.StartOfPatch);
                        eventCount++;
                    }
                    // If there's an event during the patch
                    if (((thisPatch.PatchStartTransition == PatchTransitionType.Encounter && thisPatch.PatchEndTransition == PatchTransitionType.Escape) ||
                         thisPatch.PatchEndTransition == PatchTransitionType.PartialOutOfFuel || thisPatch.PatchEndTransition == PatchTransitionType.CompletelyOutOfFuel) && eventIdx < maxNumEvents)
                    {
                        // Report encounter durring thisPatch
                        DisplayEvent(eventIdx++, thisPatch, ManeuverNodeControllerMod.PatchEventType.MidPatch);
                        eventCount++;
                    }
                    // If there's an event at the end of the patch
                    if (((thisPatch.PatchEndTransition == PatchTransitionType.Encounter && thisPatch.closestEncounterLevel != EncounterSolutionLevel.None) ||
                         thisPatch.PatchEndTransition == PatchTransitionType.Collision) && eventIdx < maxNumEvents)
                    {
                        // Report encounter at end of thisPatch
                        DisplayEvent(eventIdx++, thisPatch, ManeuverNodeControllerMod.PatchEventType.EndOfPatch);
                        eventCount++;
                    }

                    // If we've found at least one event to display and the event group display is switched off, then switch it on
                    if (eventCount > 0 && EncounterFoldout.style.display == DisplayStyle.None)
                        EncounterFoldout.style.display = DisplayStyle.Flex;
                }
                // If there are no events to display and the event group's display is switched on, then switch it off
                if (eventCount == 0)
                    EncounterFoldout.style.display = DisplayStyle.None;
                if (eventCount < maxNumEvents)
                {
                    for (int idx = maxNumEvents; idx > eventCount; idx--)
                        eventDataList[idx - 1].Event.style.display = DisplayStyle.None;
                }

            }
            else
                EncounterFoldout.style.display = DisplayStyle.None;
        }
        else
        {
            HasNodesGroup.style.display = DisplayStyle.None;
            NoNodesGroup.style.display = DisplayStyle.Flex;
        }
    }

    // string lastBody = "N/A";

    private void DisplayEvent(int idx, PatchedConicsOrbit thisPatch, ManeuverNodeControllerMod.PatchEventType eventType = ManeuverNodeControllerMod.PatchEventType.StartOfPatch)
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

        if (eventType == ManeuverNodeControllerMod.PatchEventType.StartOfPatch)
        {
            // (thisPatch.PatchStartTransition == PatchTransitionType.PartialOutOfFuel || thisPatch.PatchStartTransition == PatchTransitionType.CompletelyOutOfFuel ||
            //  thisPatch.PatchStartTransition == PatchTransitionType.Escape)
            if (thisPatch.PatchStartTransition == PatchTransitionType.Escape)
            {
                eventDataList[idx].EncounterType.text = "SOI Exit @";
                eventDataList[idx].EncounterBody.text = thisPatch.PreviousPatch.referenceBody.Name;
                eventDataList[idx].EncounterInfo.text = MncUtility.SecondsToTimeString(thisPatch.StartUT - Game.UniverseModel.UniverseTime);
            }
            else if (thisPatch.PatchStartTransition == PatchTransitionType.PartialOutOfFuel) // Does this ever occur?
            {
                eventDataList[idx].EncounterType.text = "Partial Out of Fuel @";
                eventDataList[idx].EncounterBody.text = thisPatch.PreviousPatch.referenceBody.Name;
                eventDataList[idx].EncounterInfo.text = MncUtility.SecondsToTimeString(thisPatch.StartUT - Game.UniverseModel.UniverseTime);
            }
            else if (thisPatch.PatchStartTransition == PatchTransitionType.CompletelyOutOfFuel) // Does this ever occur?
            {
                eventDataList[idx].EncounterType.text = "Out of Fuel @";
                eventDataList[idx].EncounterBody.text = thisPatch.PreviousPatch.referenceBody.Name;
                eventDataList[idx].EncounterInfo.text = MncUtility.SecondsToTimeString(thisPatch.StartUT - Game.UniverseModel.UniverseTime);
            }
            //else if (thisPatch.PatchStartTransition == PatchTransitionType.Encounter)
            //{
            //    // do what?
            //}
        }
        else if (eventType == ManeuverNodeControllerMod.PatchEventType.MidPatch)
        {
            // ((thisPatch.PatchStartTransition == PatchTransitionType.Encounter && thisPatch.PatchEndTransition == PatchTransitionType.Escape) ||
            //   thisPatch.PatchEndTransition == PatchTransitionType.PartialOutOfFuel || thisPatch.PatchEndTransition == PatchTransitionType.CompletelyOutOfFuel)
            if (thisPatch.PatchEndTransition == PatchTransitionType.Escape)
            {
                eventDataList[idx].EncounterType.text = "Fly By @";
                eventDataList[idx].EncounterBody.text = thisPatch.referenceBody.Name;
                eventDataList[idx].EncounterInfo.text = "Pe: " + MncUtility.MetersToScaledDistanceString(thisPatch.PeriapsisArl, 3) + $", inc: {thisPatch.inclination:N1}°";
            }
            else if (thisPatch.PatchEndTransition == PatchTransitionType.PartialOutOfFuel) // Does this ever occur?
            {
                eventDataList[idx].EncounterType.text = "Partial Out of Fuel @";
                eventDataList[idx].EncounterBody.text = thisPatch.referenceBody.Name;
                eventDataList[idx].EncounterInfo.text = MncUtility.SecondsToTimeString(thisPatch.EndUT - Game.UniverseModel.UniverseTime);
            }
            else if (thisPatch.PatchEndTransition == PatchTransitionType.CompletelyOutOfFuel) // Does this ever occur?
            {
                eventDataList[idx].EncounterType.text = "Out of Fuel @";
                eventDataList[idx].EncounterBody.text = thisPatch.referenceBody.Name;
                eventDataList[idx].EncounterInfo.text = MncUtility.SecondsToTimeString(thisPatch.EndUT - Game.UniverseModel.UniverseTime);
            }
        }
        else if (eventType == ManeuverNodeControllerMod.PatchEventType.EndOfPatch)
        {
            // ((thisPatch.PatchEndTransition == PatchTransitionType.Encounter && thisPatch.closestEncounterLevel != EncounterSolutionLevel.None) ||
            //   thisPatch.PatchEndTransition == PatchTransitionType.Collision)
            if (thisPatch.PatchEndTransition == PatchTransitionType.Collision)
            {
                eventDataList[idx].EncounterType.AddToClassList("unity-label-invalid");
                eventDataList[idx].EncounterBody.text = thisPatch.referenceBody.Name;
                eventDataList[idx].EncounterType.text = "Collision @";
                eventDataList[idx].EncounterInfo.text = MncUtility.SecondsToTimeString(thisPatch.EndUT - Game.UniverseModel.UniverseTime);
            }
            else if (thisPatch.closestEncounterLevel != EncounterSolutionLevel.None)
            {
                switch (thisPatch.closestEncounterLevel)
                {
                    case EncounterSolutionLevel.OrbitIntersect:
                        eventDataList[idx].EncounterType.text = "Orbit Intersect @";
                        eventDataList[idx].EncounterInfo.text = MncUtility.SecondsToTimeString(thisPatch.EndUT - Game.UniverseModel.UniverseTime);
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
                        eventDataList[idx].EncounterInfo.text = MncUtility.SecondsToTimeString(thisPatch.EndUT - Game.UniverseModel.UniverseTime);
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
                        eventDataList[idx].EncounterInfo.text = MncUtility.SecondsToTimeString(thisPatch.StartUT - Game.UniverseModel.UniverseTime);
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
        _container.style.display = newState ? DisplayStyle.Flex : DisplayStyle.None;
        GameObject.Find(ManeuverNodeControllerMod.ToolbarFlightButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(newState);
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
        SmallStepDvIncrementUpButton   = _container.Q<Button>("SmallStepDvIncrementUpButton");
        SmallStepDvIncrementDownButton = _container.Q<Button>("SmallStepDvIncrementDownButton");
        LargeStepDvIncrementUpButton   = _container.Q<Button>("LargeStepDvIncrementUpButton");
        LargeStepDvIncrementDownButton = _container.Q<Button>("LargeStepDvIncrementDownButton");

        SmallTimeStepIncrementUpButton   = _container.Q<Button>("SmallTimeStepIncrementUpButton");
        SmallTimeStepIncrementDownButton = _container.Q<Button>("SmallTimeStepIncrementDownButton");
        LargeTimeStepIncrementUpButton   = _container.Q<Button>("LargeTimeStepIncrementUpButton");
        LargeTimeStepIncrementDownButton = _container.Q<Button>("LargeTimeStepIncrementDownButton");

        SmallStepDvInput = _container.Q<TextField>("SmallStepDvInput");
        LargeStepDvInput = _container.Q<TextField>("LargeStepDvInput");
        SmallTimeStepInput = _container.Q<TextField>("SmallTimeStepInput");
        LargeTimeStepInput = _container.Q<TextField>("LargeTimeStepInput");

        ProgradeDvInput = _container.Q<TextField>("ProgradeDvInput");
        NormalDvInput = _container.Q<TextField>("NormalDvInput");
        RadialDvInput = _container.Q<TextField>("RadialDvInput");

        ManeuverNodeControllerMod.Logger.LogInfo($"MNC: SnapTo buttons initialized. initialized is set to {initialized}");

        // Set up variables to be able to access UITK GUI labels quickly (Queries are expensive)
        NodeIndexValue = _container.Q<Label>("NodeIndexValue");
        NodeMaxIndexValue = _container.Q<Label>("NodeMaxIndexValue");
        TotalDvValue = _container.Q<Label>("TotalDvValue");
        DvRemainingValue = _container.Q<Label>("DvRemainingValue");
        StartTimeValue = _container.Q<Label>("StartTimeValue");
        DurationValue = _container.Q<Label>("DurationValue");
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
        PreviousNextFoldout = _container.Q<Foldout>("PreviousNextFoldout");
        EncounterFoldout = _container.Q<Foldout>("EncounterFoldout");

        //EncounterInfo.Add(EncounterInfo4);

        // Loop through each event and initialize the data objects
        for (int i = 1; i <= maxNumEvents; i++)
        {
            EventData eventData = new EventData
            {
                Event = _container.Q<VisualElement>("Event" + i),
                EncounterBody = _container.Q<Label>("EncounterBody" + i),
                EncounterType = _container.Q<Label>("EncounterType" + i),
                EncounterInfo = _container.Q<Label>("EncounterInfo" + i)
            };

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

        // pass = float.TryParse(_container.Q<TextField>("SmallStepDvInput").value, out smallStepDv);
        SmallStepDvInput.RegisterValueChangedCallback((evt) =>
        {
            if (float.TryParse(evt.newValue, out float newFloat))
            {
                smallStepDv = newFloat;
            }
        });
        SmallStepDvInput.value = smallStepDv.ToString();
        SmallStepDvIncrementUpButton.clicked += () => { smallStepDv *= 10.0f; SmallStepDvInput.value = smallStepDv.ToString(); };
        SmallStepDvIncrementDownButton.clicked += () => { smallStepDv *= 0.1f; SmallStepDvInput.value = smallStepDv.ToString(); };

        // pass = float.TryParse(_container.Q<TextField>("LargeStepDvInput").value, out largeStepDv);
        LargeStepDvInput.RegisterValueChangedCallback((evt) =>
        {
            if (float.TryParse(evt.newValue, out float newFloat))
            {
                largeStepDv = newFloat;
            }
        });
        LargeStepDvInput.value = largeStepDv.ToString();
        LargeStepDvIncrementUpButton.clicked += () => { largeStepDv *= 10.0f; LargeStepDvInput.value = largeStepDv.ToString(); };
        LargeStepDvIncrementDownButton.clicked += () => { largeStepDv *= 0.1f; LargeStepDvInput.value = largeStepDv.ToString(); };

        // pass = float.TryParse(_container.Q<TextField>("SmallTimeStepInput").value, out smallStepTime);
        SmallTimeStepInput.RegisterValueChangedCallback((evt) =>
        {
            if (float.TryParse(evt.newValue, out float newFloat))
            {
                smallStepTime = newFloat;
            }
        });
        SmallTimeStepInput.value = smallStepTime.ToString();
        SmallTimeStepIncrementUpButton.clicked += () => { smallStepTime *= 10.0f; SmallTimeStepInput.value = smallStepTime.ToString(); };
        SmallTimeStepIncrementDownButton.clicked += () => { smallStepTime *= 0.1f; SmallTimeStepInput.value = smallStepTime.ToString(); };

        // pass = float.TryParse(_container.Q<TextField>("LargeTimeStepInput").value, out largeStepTime);
        LargeTimeStepInput.RegisterValueChangedCallback((evt) =>
        {
            if (float.TryParse(evt.newValue, out float newFloat))
            {
                largeStepTime = newFloat;
            }
        });
        LargeTimeStepInput.value = largeStepTime.ToString();
        LargeTimeStepIncrementUpButton.clicked += () => { largeStepTime *= 10.0f; LargeTimeStepInput.value = largeStepTime.ToString(); };
        LargeTimeStepIncrementDownButton.clicked += () => { largeStepTime *= 0.1f; LargeTimeStepInput.value = largeStepTime.ToString(); };

        ProgradeDvInput.RegisterValueChangedCallback((evt) =>
        {
            if (float.TryParse(evt.newValue, out float newProgradeDv))
            {
                SetPrograde(newProgradeDv);
            }
        });

        NormalDvInput.RegisterValueChangedCallback((evt) =>
        {
            if (float.TryParse(evt.newValue, out float newNormalDv))
            {
                SetNormal(newNormalDv);
            }
        });

        RadialDvInput.RegisterValueChangedCallback((evt) =>
        {
            if (float.TryParse(evt.newValue, out float newRadialDv))
            {
                SetRadial(newRadialDv);
            }
        });

        _container.Q<RepeatButton>("LargeProgradeDecreaseButton").SetAction(() => IncrementPrograde(-largeStepDv), ManeuverNodeControllerMod.Instance.repeatDelay.Value, ManeuverNodeControllerMod.Instance.repeatInterval.Value);
        _container.Q<RepeatButton>("SmallProgradeDecreaseButton").SetAction(() => IncrementPrograde(-smallStepDv), ManeuverNodeControllerMod.Instance.repeatDelay.Value, ManeuverNodeControllerMod.Instance.repeatInterval.Value);
        _container.Q<RepeatButton>("SmallProgradeIncreaseButton").SetAction(() => IncrementPrograde(smallStepDv), ManeuverNodeControllerMod.Instance.repeatDelay.Value, ManeuverNodeControllerMod.Instance.repeatInterval.Value);
        _container.Q<RepeatButton>("LargeProgradeIncreaseButton").SetAction(() => IncrementPrograde(largeStepDv), ManeuverNodeControllerMod.Instance.repeatDelay.Value, ManeuverNodeControllerMod.Instance.repeatInterval.Value);

        _container.Q<RepeatButton>("LargeNormalDecreaseButton").SetAction(() => IncrementNormal(-largeStepDv), ManeuverNodeControllerMod.Instance.repeatDelay.Value, ManeuverNodeControllerMod.Instance.repeatInterval.Value);
        _container.Q<RepeatButton>("SmallNormalDecreaseButton").SetAction(() => IncrementNormal(-smallStepDv), ManeuverNodeControllerMod.Instance.repeatDelay.Value, ManeuverNodeControllerMod.Instance.repeatInterval.Value);
        _container.Q<RepeatButton>("SmallNormalIncreaseButton").SetAction(() => IncrementNormal(smallStepDv), ManeuverNodeControllerMod.Instance.repeatDelay.Value, ManeuverNodeControllerMod.Instance.repeatInterval.Value);
        _container.Q<RepeatButton>("LargeNormalIncreaseButton").SetAction(() => IncrementNormal(largeStepDv), ManeuverNodeControllerMod.Instance.repeatDelay.Value, ManeuverNodeControllerMod.Instance.repeatInterval.Value);

        _container.Q<RepeatButton>("LargeRadialDecreaseButton").SetAction(() => IncrementRadial(-largeStepDv), ManeuverNodeControllerMod.Instance.repeatDelay.Value, ManeuverNodeControllerMod.Instance.repeatInterval.Value);
        _container.Q<RepeatButton>("SmallRadialDecreaseButton").SetAction(() => IncrementRadial(-smallStepDv), ManeuverNodeControllerMod.Instance.repeatDelay.Value, ManeuverNodeControllerMod.Instance.repeatInterval.Value);
        _container.Q<RepeatButton>("SmallRadialIncreaseButton").SetAction(() => IncrementRadial(smallStepDv), ManeuverNodeControllerMod.Instance.repeatDelay.Value, ManeuverNodeControllerMod.Instance.repeatInterval.Value);
        _container.Q<RepeatButton>("LargeRadialIncreaseButton").SetAction(() => IncrementRadial(largeStepDv), ManeuverNodeControllerMod.Instance.repeatDelay.Value, ManeuverNodeControllerMod.Instance.repeatInterval.Value);

        _container.Q<Button>("SnapToApButton").clicked += SnapToAp;
        _container.Q<Button>("SnapToPeButton").clicked += SnapToPe;
        _container.Q<Button>("SnapToANeButton").clicked += SnapToANe;
        _container.Q<Button>("SnapToDNeButton").clicked += SnapToDNe;
        _container.Q<Button>("SnapToANtButton").clicked += SnapToANt;
        _container.Q<Button>("SnapToDNtButton").clicked += SnapToDNt;

        _container.Q<RepeatButton>("LargeTimeDecreaseButton").SetAction(() => IncrementTime(-largeStepTime), ManeuverNodeControllerMod.Instance.repeatDelay.Value, ManeuverNodeControllerMod.Instance.repeatInterval.Value);
        _container.Q<RepeatButton>("SmallTimeDecreaseButton").SetAction(() => IncrementTime(-smallStepTime), ManeuverNodeControllerMod.Instance.repeatDelay.Value, ManeuverNodeControllerMod.Instance.repeatInterval.Value);
        _container.Q<RepeatButton>("SmallTimeIncreaseButton").SetAction(() => IncrementTime(smallStepTime), ManeuverNodeControllerMod.Instance.repeatDelay.Value, ManeuverNodeControllerMod.Instance.repeatInterval.Value);
        _container.Q<RepeatButton>("LargeTimeIncreaseButton").SetAction(() => IncrementTime(largeStepTime), ManeuverNodeControllerMod.Instance.repeatDelay.Value, ManeuverNodeControllerMod.Instance.repeatInterval.Value);

        _container.Q<RepeatButton>("DecreaseOrbitButton").SetAction(() => IncrementOrbit(-1), ManeuverNodeControllerMod.Instance.repeatDelay.Value, ManeuverNodeControllerMod.Instance.repeatInterval.Value);
        _container.Q<RepeatButton>("IncreaseOrbitButton").SetAction(() => IncrementOrbit(1), ManeuverNodeControllerMod.Instance.repeatDelay.Value, ManeuverNodeControllerMod.Instance.repeatInterval.Value);

        initialized = true;
        ManeuverNodeControllerMod.Logger.LogInfo($"MNC: UITK GUI Initialized. initialized set to {initialized}");
    }

    private void NextManeuverNode()
    {
        // increment if possible, or wrap around
        if (ManeuverNodeControllerMod.Instance.SelectedNodeIndex + 1 < NodeManagerPlugin.Instance.Nodes.Count)
        {
            ManeuverNodeControllerMod.Instance.SelectedNodeIndex++;
            thisNode = NodeManagerPlugin.Instance.Nodes[ManeuverNodeControllerMod.Instance.SelectedNodeIndex];
        }
    }

    private void PreviousManeuverNode()
    {
        // Decrement if possible, or wrap around
        if (ManeuverNodeControllerMod.Instance.SelectedNodeIndex > 0)
        {
            ManeuverNodeControllerMod.Instance.SelectedNodeIndex--;
            thisNode = NodeManagerPlugin.Instance.Nodes[ManeuverNodeControllerMod.Instance.SelectedNodeIndex];
        }
    }

    private void AddManeuverNode()
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
        if (pass) ManeuverNodeControllerMod.Instance.forceOpen = false;
    }

    private void DelManeuverNode()
    {
        // Identical process to original
        NodeManagerPlugin.Instance.DeleteNodes(ManeuverNodeControllerMod.Instance.SelectedNodeIndex);
        if (ManeuverNodeControllerMod.Instance.SelectedNodeIndex > 0)
            ManeuverNodeControllerMod.Instance.SelectedNodeIndex--;
        else
            ManeuverNodeControllerMod.Instance.SelectedNodeIndex = 0;
    }

    private void CheckManeuverNode()
    {
        NodeManagerPlugin.Instance.SpitNode(ManeuverNodeControllerMod.Instance.SelectedNodeIndex);
        OrbiterComponent Orbiter = MncUtility.activeVessel?.Orbiter;
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
        double NextClosestApproachDist = -1;
        for (int i = 0; i < patch.Count; i++)
        {
            var thisPatch = patch[i];
            if (!thisPatch.ActivePatch)
                break;
            if (thisPatch.closestEncounterBody != null)
                ManeuverNodeControllerMod.Logger.LogInfo($"Patch {i}: Active {thisPatch.ActivePatch}, SOI Encounter {thisPatch.UniversalTimeAtSoiEncounter}, Start {thisPatch.PatchStartTransition} @ {thisPatch.StartUT:N3}, End {thisPatch.PatchEndTransition} @ {thisPatch.EndUT:N3}, referenceBody: {thisPatch.referenceBody.Name}, Encounter: {thisPatch.closestEncounterBody.Name} ({thisPatch.closestEncounterLevel})");
            else
                ManeuverNodeControllerMod.Logger.LogInfo($"Patch {i}: Active {thisPatch.ActivePatch}, SOI Encounter {thisPatch.UniversalTimeAtSoiEncounter}, Start {thisPatch.PatchStartTransition} @ {thisPatch.StartUT:N3}, End {thisPatch.PatchEndTransition} @ {thisPatch.EndUT:N3}, referenceBody: {thisPatch.referenceBody.Name}");
            var NextClosestApproachTime = thisPatch.NextClosestApproachTime(MncUtility.currentTarget.Orbit as PatchedConicsOrbit, thisPatch.StartUT);
            if (NextClosestApproachTime > thisPatch.StartUT)
            {
                PatchedConicsOrbit o = thisPatch;
                NextClosestApproachDist = (o.GetRelativePositionAtUTZup(NextClosestApproachTime) - MncUtility.currentTarget.Orbit.GetRelativePositionAtUTZup(NextClosestApproachTime)).magnitude;
            }
            ManeuverNodeControllerMod.Logger.LogInfo($"Patch {i}: TrueAnomalyFirstEncounterPriOrbit {thisPatch.TrueAnomalyFirstEncounterPriOrbit}");
            ManeuverNodeControllerMod.Logger.LogInfo($"Patch {i}: TrueAnomalyFirstEncounterSecOrbit {thisPatch.TrueAnomalyFirstEncounterSecOrbit}");
            ManeuverNodeControllerMod.Logger.LogInfo($"Patch {i}: TrueAnomalySecEncounterPriOrbit   {thisPatch.TrueAnomalySecEncounterPriOrbit}");
            ManeuverNodeControllerMod.Logger.LogInfo($"Patch {i}: TrueAnomalySecEncounterSecOrbit   {thisPatch.TrueAnomalySecEncounterSecOrbit}");
            ManeuverNodeControllerMod.Logger.LogInfo($"Patch {i}: closestTgtApprUT                  {thisPatch.closestTgtApprUT}");
            ManeuverNodeControllerMod.Logger.LogInfo($"Patch {i}: ClosestApproachDistance           {thisPatch.ClosestApproachDistance}");
            ManeuverNodeControllerMod.Logger.LogInfo($"Patch {i}: numClosePoints                    {thisPatch.numClosePoints}");
            ManeuverNodeControllerMod.Logger.LogInfo($"Patch {i}: UniversalTimeAtClosestApproach    {thisPatch.UniversalTimeAtClosestApproach}");
            ManeuverNodeControllerMod.Logger.LogInfo($"Patch {i}: UniversalTimeAtSoiEncounter       {thisPatch.UniversalTimeAtSoiEncounter}");
            ManeuverNodeControllerMod.Logger.LogInfo($"Patch {i}: timeToTransition1                 {thisPatch.timeToTransition1}");
            ManeuverNodeControllerMod.Logger.LogInfo($"Patch {i}: timeToTransition2                 {thisPatch.timeToTransition2}");
            ManeuverNodeControllerMod.Logger.LogInfo($"Patch {i}: NextClosestApproachTime           {NextClosestApproachTime:N3} = {MncUtility.SecondsToTimeString(NextClosestApproachTime)}");
            ManeuverNodeControllerMod.Logger.LogInfo($"Patch {i}: NextClosestApproachDist           {NextClosestApproachDist:N3}");
            ManeuverNodeControllerMod.Logger.LogInfo($"Patch {i}: findClosestPoints                 {thisPatch.findClosestPoints}");
            ManeuverNodeControllerMod.Logger.LogInfo($"Patch {i}: ClEctr1                           {thisPatch.ClEctr1}");
            ManeuverNodeControllerMod.Logger.LogInfo($"Patch {i}: ClEctr2                           {thisPatch.ClEctr2}");
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

    private void IncrementPrograde(float amount)
    {
        Vector3d burnParams = Vector3d.zero;
        burnParams.z += amount;
        ManeuverNodeControllerMod.Logger.LogDebug($"IncrementPrograde: amount = {amount} BurnParams =[{burnParams.x:n3}, {burnParams.y:n3}, {burnParams.z:n3}]");
        ApplyChange(0, burnParams);
    }

    private void IncrementNormal(float amount)
    {
        Vector3d burnParams = Vector3d.zero;
        burnParams.y += amount;
        ManeuverNodeControllerMod.Logger.LogDebug($"IncrementNormal: amount = {amount} BurnParams =[{burnParams.x:n3}, {burnParams.y:n3}, {burnParams.z:n3}]");
        ApplyChange(0, burnParams);
    }

    private void IncrementRadial(float amount)
    {
        Vector3d burnParams = Vector3d.zero;
        burnParams.x += amount;
        ManeuverNodeControllerMod.Logger.LogDebug($"IncrementRadial: amount = {amount} BurnParams =[{burnParams.x:n3}, {burnParams.y:n3}, {burnParams.z:n3}]");
        ApplyChange(0, burnParams);
    }

    private void SetPrograde(float amount)
    {
        Vector3d burnParams = Vector3d.zero;
        burnParams.z = amount - thisNode.BurnVector.z;
        ManeuverNodeControllerMod.Logger.LogDebug($"SetPrograde: amount = {amount} BurnParams =[{burnParams.x:n3}, {burnParams.y:n3}, {burnParams.z:n3}]");
        ApplyChange(0, burnParams);
    }

    private void SetNormal(float amount)
    {
        Vector3d burnParams = Vector3d.zero;
        burnParams.y = amount - thisNode.BurnVector.y;
        ManeuverNodeControllerMod.Logger.LogDebug($"SetNormal: amount = {amount} BurnParams =[{burnParams.x:n3}, {burnParams.y:n3}, {burnParams.z:n3}]");
        ApplyChange(0, burnParams);
    }

    private void SetRadial(float amount)
    {
        Vector3d burnParams = Vector3d.zero;
        burnParams.x = amount - thisNode.BurnVector.x;
        ManeuverNodeControllerMod.Logger.LogDebug($"SetRadial: amount = {amount} BurnParams =[{burnParams.x:n3}, {burnParams.y:n3}, {burnParams.z:n3}]");
        ApplyChange(0, burnParams);
    }

    private void IncrementTime(float amount)
    {
        ManeuverNodeControllerMod.Logger.LogDebug($"IncrementTime: amount = {amount}");
        ApplyChange(thisNode.Time + amount, Vector3d.zero);
    }

    private void IncrementOrbit(float amount)
    {
        ManeuverNodeControllerMod.Logger.LogDebug($"IncrementOrbit: amount = {amount}");
        IncrementTime(amount * (float)MncUtility.activeVessel.Orbit.period);
    }

    private void SnapToAp()
    {
        ApplyChange(game.UniverseModel.UniverseTime + MncUtility.activeVessel.Orbit.TimeToAp, Vector3d.zero);
    }

    private void SnapToPe()
    {
        ApplyChange(game.UniverseModel.UniverseTime + MncUtility.activeVessel.Orbit.TimeToPe, Vector3d.zero);
    }

    private void SnapToANe()
    {
        ApplyChange(MncUtility.activeVessel.Orbit.TimeOfAscendingNodeEquatorial(game.UniverseModel.UniverseTime), Vector3d.zero);
    }

    private void SnapToDNe()
    {
        ApplyChange(MncUtility.activeVessel.Orbit.TimeOfDescendingNodeEquatorial(game.UniverseModel.UniverseTime), Vector3d.zero);
    }

    private void SnapToANt()
    {
        ApplyChange(MncUtility.activeVessel.Orbit.TimeOfAscendingNode(MncUtility.currentTarget.Orbit, game.UniverseModel.UniverseTime), Vector3d.zero);
    }

    private void SnapToDNt()
    {
        ApplyChange(MncUtility.activeVessel.Orbit.TimeOfDescendingNode(MncUtility.currentTarget.Orbit, game.UniverseModel.UniverseTime), Vector3d.zero);
    }

    private void ApplyChange(double nodeTime, Vector3d burnParams)
    {
        // Get the ManeuverPlanComponent for the active vessel
        ManeuverPlanComponent maneuverPlanComponent = null;
        if (MncUtility.activeVessel != null)
        {
            var simObject = MncUtility.activeVessel?.SimulationObject;
            maneuverPlanComponent = simObject?.FindComponent<ManeuverPlanComponent>();
        }

        double ut = game.UniverseModel.UniverseTime;

        if (nodeTime > 0)
        {
            double minTime = ut + Math.Max(smallStepTime, 5);
            double maxTime = ut - 1;
            if (ManeuverNodeControllerMod.Instance.SelectedNodeIndex > 0)
                minTime = NodeManagerPlugin.Instance.Nodes[ManeuverNodeControllerMod.Instance.SelectedNodeIndex - 1].Time + Math.Max(smallStepTime, 5);
            if (ManeuverNodeControllerMod.Instance.SelectedNodeIndex < NodeManagerPlugin.Instance.Nodes.Count - 1)
                maxTime = NodeManagerPlugin.Instance.Nodes[ManeuverNodeControllerMod.Instance.SelectedNodeIndex + 1].Time - Math.Max(smallStepTime, 5);

            if (nodeTime < minTime) // Not allowed to move the node prior to another node
            {
                nodeTime = minTime;
                ManeuverNodeControllerMod.Logger.LogDebug($"Limiting nodeTime to no less than {(nodeTime - ut):F3} from now.");
            }
            if (maxTime > minTime && nodeTime > maxTime) // Not allowed to move the node ahead of a later node
            {
                nodeTime = maxTime;
                ManeuverNodeControllerMod.Logger.LogDebug($"Limiting nodeTime to no more than {(nodeTime - ut):F3} from now.");
            }

            // Push the update to the node
            maneuverPlanComponent.UpdateTimeOnNode(thisNode, nodeTime);

            ManeuverNodeControllerMod.Logger.LogDebug($"nodeTime after adjust  : {(nodeTime - ut):F3} from now.");
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