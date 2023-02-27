using UnityEngine;
using KSP.Game;
using KSP.Sim.impl;
using KSP.Sim.Maneuver;
using SpaceWarp.API.Mods;

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
        private string timeStepString = "5";
        private double smallStep, bigStep, timeStep;

        private bool pInc1, pInc2, pDec1, pDec2, nInc1, nInc2, nDec1, nDec2, rInc1, rInc2, rDec1, rDec2, timeInc, timeDec;
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
            }
        }

        void OnGUI()
        {
            if (interfaceEnabled)
            {
                windowRect = GUILayout.Window(
                    GUIUtility.GetControlID(FocusType.Passive),
                    windowRect,
                    FillWindow,
                    "Maneuver Node Controller",
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
                GUILayout.Label($"Total Maneuver dV (m/s): {currentNode.BurnRequiredDV.ToString("n2")}");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label($"Prograde dV (m/s): {currentNode.BurnVector.z.ToString("n2")}");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label($"Normal dV (m/s): {currentNode.BurnVector.y.ToString("n2")}");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label($"Radial dV (m/s): {currentNode.BurnVector.x.ToString("n2")}");
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
                GameManager.Instance.Game.UniverseModel.FindVesselComponent(nodeData.RelatedSimID)?.SimulationObject.FindComponent<ManeuverPlanComponent>().UpdateChangeOnNode(nodeData, burnParams);
                GameManager.Instance.Game.UniverseModel.FindVesselComponent(nodeData.RelatedSimID)?.SimulationObject.FindComponent<ManeuverPlanComponent>().RefreshManeuverNodeState(0);
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
            GUILayout.Label("Time Step (seconds): ", GUILayout.Width(windowWidth / 2));
            timeStepString = GUILayout.TextField(timeStepString);
            double.TryParse(timeStepString, out timeStep);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            timeDec = GUILayout.Button("<", GUILayout.Width(windowWidth / 7));
            GUILayout.FlexibleSpace();
            GUILayout.Label("Time", labelStyle);
            GUILayout.FlexibleSpace();
            timeInc = GUILayout.Button(">", GUILayout.Width(windowWidth / 7));
            GUILayout.EndHorizontal();

            handleButtons();
        }

        private void handleButtons()
        {
            if (pInc1 || pInc2 || pDec1 || pDec2 || nInc1 || nInc2 || nDec1 || nDec2 || rInc1 || rInc2 || rDec1 || rDec2 || timeDec || timeInc)
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
                    else if (timeDec)
                    {
                        currentNode.Time -= timeStep;
                    }
                    else if (timeInc)
                    {
                        currentNode.Time += timeStep;
                    }

                    game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID)?.SimulationObject.FindComponent<ManeuverPlanComponent>().UpdateChangeOnNode(currentNode, burnParams);
                    game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID)?.SimulationObject.FindComponent<ManeuverPlanComponent>().RefreshManeuverNodeState(0);
                }
            }
        }
    }
}