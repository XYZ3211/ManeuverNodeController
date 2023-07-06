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

  public SimulationObjectModel currentTarget;

  private GameInstance game;

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

    game = GameManager.Instance.Game;
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
        thisNode = nodes[selectedNode];
        double dvRemaining, UT;
        int numOrbits;
        string nextApA, nextPeA, nextInc, nextEcc, nextLAN, previousApA, previousPeA, previousInc, previousEcc, previousLAN;

        PatchedConicsOrbit orbit = MNCUtility.activeVessel?.Orbit;
        OrbiterComponent Orbiter = MNCUtility.activeVessel?.Orbiter;
        ManeuverPlanSolver ManeuverPlanSolver = Orbiter?.ManeuverPlanSolver;
        List<PatchedConicsOrbit> PatchedConicsList = ManeuverPlanSolver?.PatchedConicsList;

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
        numOrbits = (int)Math.Truncate((thisNode.Time - game.UniverseModel.UniversalTime) / game.UniverseModel.FindVesselComponent(thisNode.RelatedSimID).Orbit.period);

        NodeTimeValue.text = numOrbits.ToString("n0");
        if (numOrbits == 1) OrbitsLabel.text = "orbit";
        else OrbitsLabel.text = "orbits";

        // ManeuverPlanComponent activeVesselPlan = Utility.activeVessel?.SimulationObject?.FindComponent<ManeuverPlanComponent>();
        // var nodes = activeVesselPlan?.GetNodes();


        // Get the patch info and index for the patch that contains this node
        // ManeuverPlanSolver.FindPatchContainingUt(NodeManagerPlugin.Instance.Nodes[SelectedNodeIndex].Time, PatchedConicsList, out var patch, out var patchIndex);
        var nodeTime = thisNode.Time;
        PatchedConicsOrbit patch = null;
        int patchIdx = 0;
        if (nodeTime < PatchedConicsList[0].StartUT)
        {
          patchIdx = (PatchedConicsList[0].PatchEndTransition == PatchTransitionType.Encounter) ? 1 : 0;
          patch = PatchedConicsList[patchIdx];
        }
        else
        {
          for (int i = 0; i < PatchedConicsList.Count - 1; i++)
          {
            if (PatchedConicsList[i].StartUT < nodeTime && nodeTime <= PatchedConicsList[i + 1].StartUT)
            {
              patchIdx = (PatchedConicsList[i + 1].PatchEndTransition == PatchTransitionType.Encounter && i < PatchedConicsList.Count - 2) ? i + 2 : i + 1;
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

        if (selectedNode == 0) // One or more nodes, and the selected node is the first
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

        PreviousApValue.text = previousApA + " km";
        PreviousPeValue.text = previousPeA + " km";
        PreviousIncValue.text = previousInc + "°";
        PreviousEccValue.text = previousEcc;
        PreviousLANValue.text = previousLAN + "°";

        NextApValue.text = nextApA + " km";
        NextPeValue.text = nextPeA + " km";
        NextIncValue.text = nextInc + "°";
        NextEccValue.text = nextEcc;
        NextLANValue.text = nextLAN + "°";

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

    //document.rootVisualElement.Query<TextField>().ForEach(textField =>
    //{
    //  textField.RegisterCallback<FocusInEvent>(_ => GameManager.Instance?.Game?.Input.Disable());
    //  textField.RegisterCallback<FocusOutEvent>(_ => GameManager.Instance?.Game?.Input.Enable());

    //  textField.RegisterValueChangedCallback((evt) =>
    //  {
    //    ManeuverNodeControllerMod.Logger.LogDebug($"TryParse attempt for {textField.name}. Tooltip = {textField.tooltip}");
    //    if (float.TryParse(evt.newValue, out _))
    //    {
    //      textField.RemoveFromClassList("unity-text-field-invalid");
    //      ManeuverNodeControllerMod.Logger.LogDebug($"TryParse success for {textField.name}, nValue = '{evt.newValue}': Removed unity-text-field-invalid from class list");
    //    }
    //    else
    //    {
    //      textField.AddToClassList("unity-text-field-invalid");
    //      ManeuverNodeControllerMod.Logger.LogDebug($"TryParse failure for {textField.name}, nValue = '{evt.newValue}': Added unity-text-field-invalid to class list");
    //      ManeuverNodeControllerMod.Logger.LogDebug($"document.rootVisualElement.transform.position.z = {document.rootVisualElement.transform.position.z}");
    //    }
    //  });

    //  textField.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
    //  textField.RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());
    //  textField.RegisterCallback<PointerMoveEvent>(evt => evt.StopPropagation());
    //});
  }

  public void InitializeElements()
  {
    game = GameManager.Instance.Game;
    bool pass;

    ManeuverNodeControllerMod.Logger.LogInfo($"MNC: Starting UITK GUI Initialization. initialized is set to {initialized}");

    // Set up variables to be able to access UITK GUI panel groups quickly (Queries are expensive) 
    NoNodesGroup      = _container.Q<VisualElement>("NoNodesGroup");
    HasNodesGroup     = _container.Q<VisualElement>("HasNodesGroup");

    ManeuverNodeControllerMod.Logger.LogInfo($"MNC: Panel groups initialized. initialized is set to {initialized}");

    // Set up variables to be able to access UITK GUI Buttons quickly (Queries are expensive) 
    SnapToANtButton   = _container.Q<Button>("SnapToANtButton");
    SnapToDNtButton   = _container.Q<Button>("SnapToDNtButton");

    ManeuverNodeControllerMod.Logger.LogInfo($"MNC: SnapTo buttons initialized. initialized is set to {initialized}");

    // Set up variables to be able to access UITK GUI labels quickly (Queries are expensive) 
    NodeIndexValue    = _container.Q<Label>("NodeIndexValue");
    NodeMaxIndexValue = _container.Q<Label>("NodeMaxIndexValue");
    TotalDvValue      = _container.Q<Label>("TotalDvValue");
    DvRemainingValue  = _container.Q<Label>("DvRemainingValue");
    StartTimeValue    = _container.Q<Label>("StartTimeValue");
    DurationValue     = _container.Q<Label>("DurationValue");
    ProgradeDvValue   = _container.Q<Label>("ProgradeDvValue");
    NormalDvValue     = _container.Q<Label>("NormalDvValue");
    RadialDvValue     = _container.Q<Label>("RadialDvValue");
    NodeTimeValue     = _container.Q<Label>("NodeTimeValue");
    OrbitsLabel       = _container.Q<Label>("OrbitsLabel");
    PreviousApValue   = _container.Q<Label>("PreviousApValue");
    PreviousPeValue   = _container.Q<Label>("PreviousPeValue");
    PreviousIncValue  = _container.Q<Label>("PreviousIncValue");
    PreviousEccValue  = _container.Q<Label>("PreviousEccValue");
    PreviousLANValue  = _container.Q<Label>("PreviousLANValue");
    NextApValue       = _container.Q<Label>("NextApValue");
    NextPeValue       = _container.Q<Label>("NextPeValue");
    NextIncValue      = _container.Q<Label>("NextIncValue");
    NextEccValue      = _container.Q<Label>("NextEccValue");
    NextLANValue      = _container.Q<Label>("NextLANValue");

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
    _container.Q<TextField>("AbsoluteDvInput").RegisterValueChangedCallback((evt) => {
      if (float.TryParse(evt.newValue, out float newFloat))
      {
        _container.Q<TextField>("AbsoluteDvInput").style.color = Color.white;
        absDvValue = newFloat;
      }
      else
      {
        _container.Q<TextField>("AbsoluteDvInput").style.color = Color.red;
      }
    });
    _container.Q<TextField>("AbsoluteDvInput").value = absDvValue.ToString();
    _container.Q<TextField>("AbsoluteDvInput").RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
    _container.Q<TextField>("AbsoluteDvInput").RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());
    _container.Q<TextField>("AbsoluteDvInput").RegisterCallback<PointerMoveEvent>(evt => evt.StopPropagation());

    // pass = float.TryParse(_container.Q<TextField>("SmallStepDvInput").value, out smallStepDv);
    _container.Q<TextField>("SmallStepDvInput").RegisterValueChangedCallback((evt) => {
      if (float.TryParse(evt.newValue, out float newFloat))
      {
        _container.Q<TextField>("SmallStepDvInput").style.color = Color.white;
        smallStepDv = newFloat;
      }
      else
      {
        _container.Q<TextField>("SmallStepDvInput").style.color = Color.red;
      }
    });
    _container.Q<TextField>("SmallStepDvInput").value = smallStepDv.ToString();
    _container.Q<TextField>("SmallStepDvInput").RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
    _container.Q<TextField>("SmallStepDvInput").RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());
    _container.Q<TextField>("SmallStepDvInput").RegisterCallback<PointerMoveEvent>(evt => evt.StopPropagation());

    // pass = float.TryParse(_container.Q<TextField>("LargeStepDvInput").value, out largeStepDv);
    _container.Q<TextField>("LargeStepDvInput").RegisterValueChangedCallback((evt) => {
      if (float.TryParse(evt.newValue, out float newFloat))
      {
        _container.Q<TextField>("LargeStepDvInput").style.color = Color.white;
        largeStepDv = newFloat;
      }
      else
      {
        _container.Q<TextField>("LargeStepDvInput").style.color = Color.red;
      }
    });
    _container.Q<TextField>("LargeStepDvInput").value = largeStepDv.ToString();
    _container.Q<TextField>("LargeStepDvInput").RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
    _container.Q<TextField>("LargeStepDvInput").RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());
    _container.Q<TextField>("LargeStepDvInput").RegisterCallback<PointerMoveEvent>(evt => evt.StopPropagation());

    // pass = float.TryParse(_container.Q<TextField>("SmallTimeStepInput").value, out smallStepTime);
    _container.Q<TextField>("SmallTimeStepInput").RegisterValueChangedCallback((evt) => {
      if (float.TryParse(evt.newValue, out float newFloat))
      {
        _container.Q<TextField>("SmallTimeStepInput").style.color = Color.white;
        smallStepTime = newFloat;
      }
      else
      {
        _container.Q<TextField>("SmallTimeStepInput").style.color = Color.red;
      }
    });
    _container.Q<TextField>("SmallTimeStepInput").value = smallStepTime.ToString();
    _container.Q<TextField>("SmallTimeStepInput").RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
    _container.Q<TextField>("SmallTimeStepInput").RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());
    _container.Q<TextField>("SmallTimeStepInput").RegisterCallback<PointerMoveEvent>(evt => evt.StopPropagation());

    // pass = float.TryParse(_container.Q<TextField>("LargeTimeStepInput").value, out largeStepTime);
    _container.Q<TextField>("LargeTimeStepInput").RegisterValueChangedCallback((evt) => {
      if (float.TryParse(evt.newValue, out float newFloat))
      {
        _container.Q<TextField>("LargeTimeStepInput").style.color = Color.white;
        largeStepTime = newFloat;
      }
      else
      {
        _container.Q<TextField>("LargeTimeStepInput").style.color = Color.red;
      }
    });
    _container.Q<TextField>("LargeTimeStepInput").value = largeStepTime.ToString();
    _container.Q<TextField>("LargeTimeStepInput").RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
    _container.Q<TextField>("LargeTimeStepInput").RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());
    _container.Q<TextField>("LargeTimeStepInput").RegisterCallback<PointerMoveEvent>(evt => evt.StopPropagation());

    _container.Q<Button>("LargeProgradeDecreaseButton").clicked += () => IncrementPrograde(-largeStepDv);
    _container.Q<Button>("SmallProgradeDecreaseButton").clicked += () => IncrementPrograde(-smallStepDv);
    _container.Q<Button>("SmallProgradeIncreaseButton").clicked += () => IncrementPrograde(smallStepDv);
    _container.Q<Button>("LargeProgradeIncreaseButton").clicked += () => IncrementPrograde(largeStepDv);
    _container.Q<Button>("AbsoluteProgradeButton").clicked      += () => SetPrograde(absDvValue);

    _container.Q<Button>("LargeNormalDecreaseButton").clicked += () => IncrementNormal(-largeStepDv);
    _container.Q<Button>("SmallNormalDecreaseButton").clicked += () => IncrementNormal(-smallStepDv);
    _container.Q<Button>("SmallNormalIncreaseButton").clicked += () => IncrementNormal(smallStepDv);
    _container.Q<Button>("LargeNormalIncreaseButton").clicked += () => IncrementNormal(largeStepDv);
    _container.Q<Button>("AbsoluteNormalButton").clicked      += () => SetNormal(absDvValue);

    _container.Q<Button>("LargeRadialDecreaseButton").clicked += () => IncrementRadial(-largeStepDv);
    _container.Q<Button>("SmallRadialDecreaseButton").clicked += () => IncrementRadial(-smallStepDv);
    _container.Q<Button>("SmallRadialIncreaseButton").clicked += () => IncrementRadial(smallStepDv);
    _container.Q<Button>("LargeRadialIncreaseButton").clicked += () => IncrementRadial(largeStepDv);
    _container.Q<Button>("AbsoluteRadialButton").clicked      += () => SetRadial(absDvValue);

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
      StartCoroutine(NodeManagerPlugin.Instance.RefreshNodes());
    }
    else
    {
      StartCoroutine(NodeManagerPlugin.Instance.RefreshNodes());
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
    IncrementTime(amount * (float) MNCUtility.activeVessel.Orbit.period);
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