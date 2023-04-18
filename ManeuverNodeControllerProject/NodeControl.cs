using KSP.Game;
using KSP.Map;
using KSP.Sim.impl;
using KSP.Sim.Maneuver;
using KSP.Sim;
using HarmonyLib;

namespace ManeuverNodeController;

public static class NodeControl
{
    // public static int SelectedNodeIndex = 0;
    public static List<ManeuverNodeData> Nodes = new();

    //public static ManeuverNodeData getCurrentNode(ref List<ManeuverNodeData> activeNodes)
    //{
    //    activeNodes = GameManager.Instance.Game.SpaceSimulation.Maneuvers.GetNodesForVessel(GameManager.Instance.Game.ViewController.GetActiveVehicle(true).Guid);
    //    return (activeNodes.Count() > 0) ? activeNodes[0] : null;
    //}

    public static void DeleteNodes(int SelectedNodeIndex)
    {
        //var activeVesselPlan = Utility.activeVessel.SimulationObject.FindComponent<ManeuverPlanComponent>();
        //List<ManeuverNodeData> nodeData = new List<ManeuverNodeData>();
        var nodes = Utility.activeVessel.SimulationObject?.FindComponent<ManeuverPlanComponent>()?.GetNodes();
        List<ManeuverNodeData> nodesToDelete = new List<ManeuverNodeData>();

        //var nodeToDelete = activeVesselPlan.GetNodes()[SelectedNodeIndex];
        //nodeData.Add(nodeToDelete);
        // This should never happen, but better be safe
        if (SelectedNodeIndex + 1 > nodes.Count)
            SelectedNodeIndex = Math.Max(0, nodes.Count - 1);

        var nodeToDelete = nodes[SelectedNodeIndex];
        nodesToDelete.Add(nodeToDelete);

        foreach (ManeuverNodeData node in nodes)
        {
            if (!nodesToDelete.Contains(node) && (!nodeToDelete.IsOnManeuverTrajectory || nodeToDelete.Time < node.Time))
                nodesToDelete.Add(node);
        }
        GameManager.Instance.Game.SpaceSimulation.Maneuvers.RemoveNodesFromVessel(Utility.activeVessel.GlobalId, nodesToDelete);
    }

    internal static void RefreshManeuverNodes()
    {
        ManeuverPlanComponent activeVesselPlan = Utility.activeVessel?.SimulationObject?.FindComponent<ManeuverPlanComponent>();
        if (activeVesselPlan != null)
        {
            Nodes = activeVesselPlan.GetNodes();
        }
    }

    private static IPatchedOrbit GetLastOrbit(bool silent = true)
    {
        // ManeuverNodeControllerMod.Logger.LogInfo("GetLastOrbit");
        //List<ManeuverNodeData> patchList =
        //    GameManager.Instance.Game.SpaceSimulation.Maneuvers.GetNodesForVessel(Utility.activeVessel.SimulationObject.GlobalId);

        if (!silent)
            ManeuverNodeControllerMod.Logger.LogMessage($"GetLastOrbit: Nodes.Count = {Nodes.Count}");

        if (Nodes.Count == 0) // There are no nodes, so use the activeVessel.Orbit
        {
            if (!silent)
                ManeuverNodeControllerMod.Logger.LogMessage($"GetLastOrbit: last orbit is activeVessel.Orbit: {Utility.activeVessel.Orbit}");
            return Utility.activeVessel.Orbit;
        }

        // Get the last patch in the list
        IPatchedOrbit lastOrbit = Nodes[Nodes.Count - 1].ManeuverTrajectoryPatch;
        if (!silent)
        {
            ManeuverNodeControllerMod.Logger.LogMessage($"GetLastOrbit: last orbit is patch {Nodes.Count - 1}: {lastOrbit}");
        }

        return lastOrbit;
    }

    public static void CreateManeuverNodeAtTA(Vector3d burnVector, double TrueAnomalyRad, double burnDurationOffsetFactor = -0.5)
    {
        // ManeuverNodeControllerMod.Logger.LogInfo("CreateManeuverNodeAtTA");
        PatchedConicsOrbit referencedOrbit = GetLastOrbit(false) as PatchedConicsOrbit;
        if (referencedOrbit == null)
        {
            ManeuverNodeControllerMod.Logger.LogError("CreateManeuverNodeAtTA: referencedOrbit is null!");
            return;
        }

        double UT = referencedOrbit.GetUTforTrueAnomaly(TrueAnomalyRad, 0);

        CreateManeuverNodeAtUT(burnVector, UT, burnDurationOffsetFactor);
    }

    public static void CreateManeuverNodeAtUT(Vector3d burnVector, double UT, double burnDurationOffsetFactor = -0.5)
    {
        ManeuverNodeControllerMod.Logger.LogInfo("CreateManeuverNodeAtUT");
        PatchedConicsOrbit referencedOrbit = GetLastOrbit(false) as PatchedConicsOrbit;
        if (referencedOrbit == null)
        {
            ManeuverNodeControllerMod.Logger.LogError("CreateManeuverNodeAtUT: referencedOrbit is null!");
            return;
        }

        if (UT < GameManager.Instance.Game.UniverseModel.UniversalTime + 1) // Don't set node to now or in the past
            UT = GameManager.Instance.Game.UniverseModel.UniversalTime + 1;

        // Get the patch to put this node on
        ManeuverPlanSolver maneuverPlanSolver = Utility.activeVessel.Orbiter?.ManeuverPlanSolver;
        IPatchedOrbit orbit = Utility.activeVessel.Orbit;
        // maneuverPlanSolver.FindPatchContainingUt(UT, maneuverPlanSolver.ManeuverTrajectory, out orbit, out int _);
        for (int i = 0; i < Nodes.Count; i++)
        {
            if (UT > Nodes[i].Time) orbit = Nodes[i].ManeuverTrajectoryPatch;
        }

        // IPatchedOrbit orbit = referencedOrbit;
        // orbit.PatchStartTransition = PatchTransitionType.Maneuver;
        // orbit.PatchEndTransition = PatchTransitionType.Final;
        //Initial,
        //Final,
        //Encounter,
        //Escape,
        //Maneuver,
        //Collision,
        //EndThrust,
        //PartialOutOfFuel,
        //CompletelyOutOfFuel,

        // Build the node data
        ManeuverNodeData nodeData;
        if (Nodes.Count == 0) // There are no nodes
        {
            nodeData = new ManeuverNodeData(Utility.activeVessel.SimulationObject.GlobalId, false, UT);
            orbit.PatchEndTransition = PatchTransitionType.Maneuver;
        }
        else if (UT < Nodes[0].Time) // request time is before the first node
        {
            nodeData = new ManeuverNodeData(Utility.activeVessel.SimulationObject.GlobalId, false, UT);
            orbit.PatchEndTransition = PatchTransitionType.Maneuver;
        }
        else if (UT > Nodes[Nodes.Count - 1].Time) // requested time is after the last node
        {
            nodeData = new ManeuverNodeData(Utility.activeVessel.SimulationObject.GlobalId, true, UT);
            orbit.PatchEndTransition = PatchTransitionType.Final;
        }
        else // request time is between existing nodes
        {
            nodeData = new ManeuverNodeData(Utility.activeVessel.SimulationObject.GlobalId, true, UT);
            orbit.PatchEndTransition = PatchTransitionType.Maneuver;
        }
        orbit.PatchStartTransition = PatchTransitionType.EndThrust;

        nodeData.SetManeuverState((PatchedConicsOrbit)orbit);

        nodeData.BurnVector = burnVector;

        // ManeuverNodeControllerMod.Logger.LogInfo($"CreateManeuverNodeAtUT: BurnVector [{burnVector.x}, {burnVector.y}, {burnVector.z}] m/s");
        // ManeuverNodeControllerMod.Logger.LogInfo($"CreateManeuverNodeAtUT: BurnDuration {nodeData.BurnDuration} s");
        // ManeuverNodeControllerMod.Logger.LogInfo($"CreateManeuverNodeAtUT: Burn Time {nodeData.Time} s");

        AddManeuverNode(nodeData, burnDurationOffsetFactor);
    }

    private static void AddManeuverNode(ManeuverNodeData nodeData, double burnDurationOffsetFactor)
    {
        // ManeuverNodeControllerMod.Logger.LogInfo("AddManeuverNode");

        // Add the node to the vessel's orbit
        GameManager.Instance.Game.SpaceSimulation.Maneuvers.AddNodeToVessel(nodeData);

        Utility.activeVessel.Orbiter.ManeuverPlanSolver.UpdateManeuverTrajectory();

        // For KSP2, We want the to start burns early to make them centered on the node
        var nodeTimeAdj = nodeData.BurnDuration * burnDurationOffsetFactor;

        ManeuverNodeControllerMod.Logger.LogDebug($"AddManeuverNode: BurnDuration {nodeData.BurnDuration} s");

        // Refersh the Utility.currentNode with what we've produced here in prep for calling UpdateNode
        Utility.RefreshActiveVesselAndCurrentManeuver();

        // FIX ME!!!
        UpdateNode(nodeData, nodeTimeAdj);

        // ManeuverNodeControllerMod.Logger.LogInfo("AddManeuverNode Done");
    }

    public static void UpdateNode(ManeuverNodeData nodeData, double nodeTimeAdj = 0)
    {
        MapCore mapCore = null;
        GameManager.Instance.Game.Map.TryGetMapCore(out mapCore);
        var m3d = mapCore.map3D;
        var maneuverManager = m3d.ManeuverManager;
        IGGuid simID;
        SimulationObjectModel simObject;

        // Get the ManeuverPlanComponent for the active vessel
        var universeModel = GameManager.Instance.Game.UniverseModel;
        VesselComponent vesselComponent;
        ManeuverPlanComponent maneuverPlanComponent;
        if (Utility.currentNode != null)
        {
            simID = Utility.currentNode.RelatedSimID;
            simObject = universeModel.FindSimObject(simID);
        }
        else
        {
            simObject = Utility.activeVessel?.SimulationObject;
        }

        if (simObject != null)
        {
            maneuverPlanComponent = simObject.FindComponent<ManeuverPlanComponent>();
        }
        else
        {
            simObject = universeModel.FindSimObject(simID);
            maneuverPlanComponent = simObject.FindComponent<ManeuverPlanComponent>();
        }

        if (nodeTimeAdj != 0)
        {
            nodeData.Time += nodeTimeAdj;
            if (nodeData.Time < GameManager.Instance.Game.UniverseModel.UniversalTime + 1) // Don't set node in the past
                nodeData.Time = GameManager.Instance.Game.UniverseModel.UniversalTime + 1;
            maneuverPlanComponent.UpdateTimeOnNode(nodeData, nodeData.Time); // This may not be necessary?
        }

        try { maneuverPlanComponent.RefreshManeuverNodeState(0); }
        catch (NullReferenceException e) { ManeuverNodeControllerMod.Logger.LogError($"UpdateNode: caught NRE on call to maneuverPlanComponent.RefreshManeuverNodeState(0): {e}"); }

        Utility.RefreshActiveVesselAndCurrentManeuver(); // do we need this?

        if (Utility.currentNode != null)
        {
            // Manage the maneuver on the map
            maneuverManager.RemoveAll();
            try { maneuverManager?.GetNodeDataForVessels(); }
            catch (Exception e) { ManeuverNodeControllerMod.Logger.LogError($"UpdateNode: caught exception on call to mapCore.map3D.ManeuverManager.GetNodeDataForVessels(): {e}"); }
            try { maneuverManager.UpdateAll(); }
            catch (Exception e) { ManeuverNodeControllerMod.Logger.LogError($"UpdateNode: caught exception on call to mapCore.map3D.ManeuverManager.UpdateAll(): {e}"); }
            try { maneuverManager.UpdatePositionForGizmo(Utility.currentNode.NodeID); }
            catch (Exception e) { ManeuverNodeControllerMod.Logger.LogError($"UpdateNode: caught exception on call to mapCore.map3D.ManeuverManager.UpdatePositionForGizmo(): {e}"); }
        }
    }

    public static void AddNode(PatchedConicsOrbit orbit)
    {
        // Add an empty maneuver node
        // ManeuverNodeControllerMod.Logger.LogInfo("handleButtons: Adding New Node");

        // Define empty node data
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        double burnUT;
        if (orbit.eccentricity < 1 && NodeControl.Nodes.Count == 0)
        {
            burnUT = UT + orbit.TimeToAp;
        }
        else
        {
            if (NodeControl.Nodes.Count > 0)
                burnUT = NodeControl.Nodes[NodeControl.Nodes.Count - 1].Time + Math.Min(orbit.period/10, 600);
            else
                burnUT = UT + 30;
        }

        Vector3d burnVector;
        burnVector.x = 0;
        burnVector.y = 0;
        burnVector.z = 0;

        CreateManeuverNodeAtUT(burnVector, burnUT, 0);
    }
}
