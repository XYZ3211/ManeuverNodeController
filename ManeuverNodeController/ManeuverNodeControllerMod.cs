using UnityEngine;
using KSP.Game;
using KSP.Sim.impl;
using KSP.Sim.Maneuver;
using SpaceWarp.API;
using SpaceWarp.API.Mods;
using KSP.UI.Binding;

namespace ManeuverNodeController
{
    [MainMod]
    public class ManeuverNodeControllerMod : Mod
    {
        static bool loaded = false;
        private bool interfaceEnabled = false;
        private Rect windowRect;
        private int windowWidth = 500;
        private int windowHeight = 700;

        private string progradeString = "0";
        private string normalString = "0";
        private string radialString = "0";
        private string smallStepString = "5";
        private string bigStepString = "100";
        private string timeSmallStepString = "5";
        private string timeLargeStepString = "25";
        private double smallStep, bigStep, timeSmallStep, timeLargeStep;

        private bool pInc1, pInc2, pDec1, pDec2, nInc1, nInc2, nDec1, nDec2, rInc1, rInc2, rDec1, rDec2, timeInc1, timeInc2, timeDec1, timeDec2, orbitInc, orbitDec;
        private bool advancedMode;

        private ManeuverNodeData currentNode = null;
        List<ManeuverNodeData> activeNodes;
        private Vector3d burnParams;

        private GUIStyle errorStyle, warnStyle, progradeStyle, normalStyle, radialStyle, labelStyle;
        private GameInstance game;
        private GUIStyle horizontalDivider = new GUIStyle();

        public override void OnInitialized()
        {
            Logger.Info("Loaded");
            if (loaded)
            {
                Destroy(this);
            }
            loaded = true;

            gameObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(gameObject);

            SpaceWarpManager.RegisterAppButton(
                "Maneuver Node Cont.",
                "BTN-ManeuverNodeController",
                SpaceWarpManager.LoadIcon(),
                ToggleButton);
        }

        void ToggleButton(bool toggle)
        {
            interfaceEnabled = toggle;
            GameObject.Find("BTN-MNC")?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(toggle);
        }

        void Awake()
        {
            windowRect = new Rect((Screen.width * 0.7f) - (windowWidth / 2), (Screen.height / 2) - (windowHeight / 2), 0, 0);
        }

        void Update()
        {
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.N))
            {
                interfaceEnabled = !interfaceEnabled;
                Logger.Info("UI toggled with hotkey");
            }
        }

        void OnGUI()
        {
            if (interfaceEnabled)
            {
                GUI.skin = SpaceWarpManager.Skin;

                windowRect = GUILayout.Window(
                    GUIUtility.GetControlID(FocusType.Passive),
                    windowRect,
                    FillWindow,
                    "<color=#696DFF>// MANEUVER NODE CONTROLLER</color>",
                    GUILayout.Height(0),
                    GUILayout.Width(350));
            }

        }

        private void FillWindow(int windowID)
        {
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

            GUILayout.BeginVertical();

            if (currentNode == null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("The active vessel has no maneuver nodes.", errorStyle);
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Total Maneuver dV (m/s): ");
                GUILayout.FlexibleSpace();
                GUILayout.Label(currentNode.BurnRequiredDV.ToString("n2"));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label($"Prograde dV (m/s): ");
                GUILayout.FlexibleSpace();
                GUILayout.Label(currentNode.BurnVector.z.ToString("n2"));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label($"Normal dV (m/s): ");
                GUILayout.FlexibleSpace();
                GUILayout.Label(currentNode.BurnVector.y.ToString("n2"));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label($"Radial dV (m/s): ");
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

            }

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, 10000, 500));
        }

        private void drawAdvancedMode()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Prograde dV (m/s): ", GUILayout.Width(windowWidth / 2));
            progradeString = GUILayout.TextField(progradeString, progradeStyle);
            double.TryParse(progradeString, out burnParams.z);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Normal dV (m/s): ", GUILayout.Width(windowWidth / 2));
            normalString = GUILayout.TextField(normalString, normalStyle);
            double.TryParse(normalString, out burnParams.y);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Radial dV (m/s): ", GUILayout.Width(windowWidth / 2));
            radialString = GUILayout.TextField(radialString, radialStyle);
            double.TryParse(radialString, out burnParams.x);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Apply Changes to Node"))
            {
                ManeuverNodeData nodeData = GameManager.Instance.Game.SpaceSimulation.Maneuvers.GetNodesForVessel(GameManager.Instance.Game.ViewController.GetActiveVehicle(true).Guid)[0];
                //nodeData.BurnVector = burnParams;
                game.UniverseModel.FindVesselComponent(nodeData.RelatedSimID)?.SimulationObject.FindComponent<ManeuverPlanComponent>().UpdateChangeOnNode(nodeData, burnParams);
                game.UniverseModel.FindVesselComponent(nodeData.RelatedSimID)?.SimulationObject.FindComponent<ManeuverPlanComponent>().RefreshManeuverNodeState(0);
                Logger.Info(nodeData.ToString());
            }
        }

        private void drawSimpleMode()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Small Step dV (m/s): ", GUILayout.Width(windowWidth / 2));
            smallStepString = GUILayout.TextField(smallStepString);
            double.TryParse(smallStepString, out smallStep);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Large Step dV (m/s): ", GUILayout.Width(windowWidth / 2));
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
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            nDec2 = GUILayout.Button("<<", GUILayout.Width(windowWidth / 9));
            nDec1 = GUILayout.Button("<", GUILayout.Width(windowWidth / 9));
            GUILayout.FlexibleSpace();
            GUILayout.Label("Normal", normalStyle);
            GUILayout.FlexibleSpace();
            nInc1 = GUILayout.Button(">", GUILayout.Width(windowWidth / 9));
            nInc2 = GUILayout.Button(">>", GUILayout.Width(windowWidth / 9));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            rDec2 = GUILayout.Button("<<", GUILayout.Width(windowWidth / 9));
            rDec1 = GUILayout.Button("<", GUILayout.Width(windowWidth / 9));
            GUILayout.FlexibleSpace();
            GUILayout.Label("Radial", radialStyle);
            GUILayout.FlexibleSpace();
            rInc1 = GUILayout.Button(">", GUILayout.Width(windowWidth / 9));
            rInc2 = GUILayout.Button(">>", GUILayout.Width(windowWidth / 9));
            GUILayout.EndHorizontal();

            GUILayout.Box("", horizontalDivider);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Small Time Step (seconds): ", GUILayout.Width(windowWidth / 2));
            timeSmallStepString = GUILayout.TextField(timeSmallStepString);
            double.TryParse(timeSmallStepString, out timeSmallStep);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Large Time Step (seconds): ", GUILayout.Width(windowWidth / 2));
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
            GUILayout.Label($"{((currentNode.Time - game.UniverseModel.UniversalTime) / game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID).Orbit.period).ToString("n0")} orbit(s) ");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            orbitDec = GUILayout.Button("-", GUILayout.Width(windowWidth / 7));
            GUILayout.FlexibleSpace();
            GUILayout.Label("Orbit", labelStyle);
            GUILayout.FlexibleSpace();
            orbitInc = GUILayout.Button("+", GUILayout.Width(windowWidth / 7));
            GUILayout.EndHorizontal();

            handleButtons();
        }

        private void handleButtons()
        {
            if (pInc1 || pInc2 || pDec1 || pDec2 || nInc1 || nInc2 || nDec1 || nDec2 || rInc1 || rInc2 || rDec1 || rDec2 || timeDec1 || timeDec2 || timeInc1 || timeInc2 || orbitDec || orbitInc)
            {
                if (currentNode != null)
                {
                    burnParams = Vector3d.zero;
                    if (pInc1)
                    {
                        burnParams.z += smallStep;
                    }
                    else if (pInc2)
                    {
                        burnParams.z += bigStep;
                    }
                    else if (nInc1)
                    {
                        burnParams.y += smallStep;
                    }
                    else if (nInc2)
                    {
                        burnParams.y += bigStep;
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
                        if (game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID).Orbit.period < (currentNode.Time- game.UniverseModel.UniversalTime))
                        {
                            currentNode.Time -= game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID).Orbit.period;
                        }
                    }
                    else if (orbitInc)
                    {
                        currentNode.Time += game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID).Orbit.period;
                    }

                    game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID)?.SimulationObject.FindComponent<ManeuverPlanComponent>().UpdateChangeOnNode(currentNode, burnParams);
                    game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID)?.SimulationObject.FindComponent<ManeuverPlanComponent>().RefreshManeuverNodeState(0);
                }
            }
        }
    }
}