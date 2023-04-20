using KSP.Game;
using KSP.Map;
using KSP.Sim.impl;
using KSP.Sim.Maneuver;
using KSP.Sim;
using MNCUtilities;
using ManeuverNodeController;

namespace MNCNodeControls;

public static class MNCNodeControl
{
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
        var nodes = MNCUtility.activeVessel.SimulationObject?.FindComponent<ManeuverPlanComponent>()?.GetNodes();
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
        GameManager.Instance.Game.SpaceSimulation.Maneuvers.RemoveNodesFromVessel(MNCUtility.activeVessel.GlobalId, nodesToDelete);
    }

    public static void SpitNode(int SelectedNodeIndex)
    {
        ManeuverNodeData node = Nodes[SelectedNodeIndex];
        ManeuverNodeControllerMod.Logger.LogInfo($"Node[{SelectedNodeIndex}]");
        ManeuverNodeControllerMod.Logger.LogInfo($"BurnDuration:             {node.BurnDuration} s");
        ManeuverNodeControllerMod.Logger.LogInfo($"BurnRequiredDV:           {node.BurnRequiredDV} m/s");
        ManeuverNodeControllerMod.Logger.LogInfo($"BurnVector:               [{node.BurnVector.x}, {node.BurnVector.y}, {node.BurnVector.z}] = {node.BurnVector.magnitude} m/s");
        ManeuverNodeControllerMod.Logger.LogInfo($"CachedManeuverPatchEndUT: {node.CachedManeuverPatchEndUT} s");
        ManeuverNodeControllerMod.Logger.LogInfo($"IsOnManeuverTrajectory:   {node.IsOnManeuverTrajectory}");
        ManeuverNodeControllerMod.Logger.LogInfo($"ManeuverTrajectoryPatch:  {node.ManeuverTrajectoryPatch}");
        ManeuverNodeControllerMod.Logger.LogInfo($"NodeID:                   {node.NodeID}");
        ManeuverNodeControllerMod.Logger.LogInfo($"NodeName:                 {node.NodeName}");
        ManeuverNodeControllerMod.Logger.LogInfo($"RelatedSimID:             {node.RelatedSimID}");
        ManeuverNodeControllerMod.Logger.LogInfo($"SimTransform:             {node.SimTransform}");
    }

    public static void SpitNode(ManeuverNodeData node)
    {
        // ManeuverNodeData node = Nodes[SelectedNodeIndex];
        ManeuverNodeControllerMod.Logger.LogInfo($"Node:");
        ManeuverNodeControllerMod.Logger.LogInfo($"BurnDuration:             {node.BurnDuration} s");
        ManeuverNodeControllerMod.Logger.LogInfo($"BurnRequiredDV:           {node.BurnRequiredDV} m/s");
        ManeuverNodeControllerMod.Logger.LogInfo($"BurnVector:               [{node.BurnVector.x}, {node.BurnVector.y}, {node.BurnVector.z}] = {node.BurnVector.magnitude} m/s");
        ManeuverNodeControllerMod.Logger.LogInfo($"CachedManeuverPatchEndUT: {node.CachedManeuverPatchEndUT} s");
        ManeuverNodeControllerMod.Logger.LogInfo($"IsOnManeuverTrajectory:   {node.IsOnManeuverTrajectory}");
        ManeuverNodeControllerMod.Logger.LogInfo($"ManeuverTrajectoryPatch:  {node.ManeuverTrajectoryPatch}");
        ManeuverNodeControllerMod.Logger.LogInfo($"NodeID:                   {node.NodeID}");
        ManeuverNodeControllerMod.Logger.LogInfo($"NodeName:                 {node.NodeName}");
        ManeuverNodeControllerMod.Logger.LogInfo($"RelatedSimID:             {node.RelatedSimID}");
        ManeuverNodeControllerMod.Logger.LogInfo($"SimTransform:             {node.SimTransform}");
    }
    public static int RefreshManeuverNodes()
    {
        ManeuverPlanComponent activeVesselPlan = MNCUtility.activeVessel?.SimulationObject?.FindComponent<ManeuverPlanComponent>();
        if (activeVesselPlan != null)
        {
            Nodes = activeVesselPlan.GetNodes();
        }
        // else ManeuverNodeControllerMod.Logger.LogDebug("RefreshManeuverNodes: activeVesselPlan is null, Nodes list not updated.");

        return Nodes.Count;
    }

    private static IPatchedOrbit GetLastOrbit(bool silent = true)
    {
        // ManeuverNodeControllerMod.Logger.LogDebug("GetLastOrbit");
        //List<ManeuverNodeData> patchList =
        //    GameManager.Instance.Game.SpaceSimulation.Maneuvers.GetNodesForVessel(Utility.activeVessel.SimulationObject.GlobalId);

        if (!silent)
            ManeuverNodeControllerMod.Logger.LogDebug($"GetLastOrbit: Nodes.Count = {Nodes.Count}");

        if (Nodes.Count == 0) // There are no nodes, so use the activeVessel.Orbit
        {
            if (!silent)
                ManeuverNodeControllerMod.Logger.LogDebug($"GetLastOrbit: last orbit is activeVessel.Orbit: {MNCUtility.activeVessel.Orbit}");
            return MNCUtility.activeVessel.Orbit;
        }

        // Get the last patch in the list
        IPatchedOrbit lastOrbit = Nodes[Nodes.Count - 1].ManeuverTrajectoryPatch;
        if (!silent)
        {
            ManeuverNodeControllerMod.Logger.LogDebug($"GetLastOrbit: last orbit is patch {Nodes.Count - 1}: {lastOrbit}");
        }

        return lastOrbit;
    }

    public static void CreateManeuverNodeAtTA(Vector3d burnVector, double TrueAnomalyRad, double burnDurationOffsetFactor = -0.5)
    {
        // ManeuverNodeControllerMod.Logger.LogDebug("CreateManeuverNodeAtTA");
        PatchedConicsOrbit referencedOrbit = MNCNodeControl.GetLastOrbit(true) as PatchedConicsOrbit;
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
        // ManeuverNodeControllerMod.Logger.LogDebug("CreateManeuverNodeAtUT");
        PatchedConicsOrbit referencedOrbit = MNCNodeControl.GetLastOrbit(true) as PatchedConicsOrbit;
        if (referencedOrbit == null)
        {
            ManeuverNodeControllerMod.Logger.LogError("CreateManeuverNodeAtUT: referencedOrbit is null!");
            return;
        }

        if (UT < GameManager.Instance.Game.UniverseModel.UniversalTime + 1) // Don't set node to now or in the past
            UT = GameManager.Instance.Game.UniverseModel.UniversalTime + 1;

        // Get the patch to put this node on
        ManeuverPlanSolver maneuverPlanSolver = MNCUtility.activeVessel.Orbiter?.ManeuverPlanSolver;
        IPatchedOrbit orbit = MNCUtility.activeVessel.Orbit;
        // maneuverPlanSolver.FindPatchContainingUt(UT, maneuverPlanSolver.ManeuverTrajectory, out orbit, out int _);
        // var selectedNode = -1;
        for (int i = 0; i < Nodes.Count-1; i++)
        {
            if (UT > Nodes[i].Time && UT < Nodes[i+1].Time)
            {
                orbit = Nodes[i+1].ManeuverTrajectoryPatch;
                // selectedNode = i;
                ManeuverNodeControllerMod.Logger.LogDebug($"CreateManeuverNodeAtUT: Attaching node to Node[{i+1}]'s ManeuverTrajectoryPatch");
            }
        }
        //if (selectedNode < 0)
        //{
        //    ManeuverNodeControllerMod.Logger.LogDebug($"CreateManeuverNodeAtUT: Attaching node to activeVessle's next patch");
        //    orbit = MNCUtility.activeVessel.Orbit.NextPatch;
        //}

        //if (selectedNode < Nodes.Count - 1) // This is a test block! We shouldn't need this. If this triggers, then fix the block above
        //{
        //    ManeuverNodeControllerMod.Logger.LogDebug($"CreateManeuverNodeAtUT: Failed to select last patch");
        //    selectedNode = Nodes.Count - 1;
        //    orbit = Nodes[selectedNode].ManeuverTrajectoryPatch;
        //    ManeuverNodeControllerMod.Logger.LogDebug($"CreateManeuverNodeAtUT: Attaching node to Node[{selectedNode}]'s patch");
        //}

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
            nodeData = new ManeuverNodeData(MNCUtility.activeVessel.SimulationObject.GlobalId, false, UT);
        }
        else 
        {
            if (UT < Nodes[0].Time) // request time is before the first node
            {
                nodeData = new ManeuverNodeData(MNCUtility.activeVessel.SimulationObject.GlobalId, false, UT);
                orbit.PatchEndTransition = PatchTransitionType.Maneuver;
            }
            else if (UT > Nodes[Nodes.Count - 1].Time) // requested time is after the last node
            {
                nodeData = new ManeuverNodeData(MNCUtility.activeVessel.SimulationObject.GlobalId, true, UT);
                orbit.PatchEndTransition = PatchTransitionType.Final;
            }
            else // request time is between existing nodes
            {
                nodeData = new ManeuverNodeData(MNCUtility.activeVessel.SimulationObject.GlobalId, true, UT);
                orbit.PatchEndTransition = PatchTransitionType.Maneuver;
            }
            orbit.PatchStartTransition = PatchTransitionType.EndThrust;

            nodeData.SetManeuverState((PatchedConicsOrbit)orbit);
        }

        nodeData.BurnVector = burnVector;

        // ManeuverNodeControllerMod.Logger.LogDebug($"CreateManeuverNodeAtUT: BurnVector [{burnVector.x}, {burnVector.y}, {burnVector.z}] m/s");
        // ManeuverNodeControllerMod.Logger.LogDebug($"CreateManeuverNodeAtUT: BurnDuration {nodeData.BurnDuration} s");
        // ManeuverNodeControllerMod.Logger.LogDebug($"CreateManeuverNodeAtUT: Burn Time {nodeData.Time} s");

        MNCNodeControl.AddManeuverNode(nodeData, burnDurationOffsetFactor);
    }

    private static void AddManeuverNode(ManeuverNodeData nodeData, double burnDurationOffsetFactor)
    {
        // ManeuverNodeControllerMod.Logger.LogDebug("AddManeuverNode");

        // Add the node to the vessel's orbit
        MNCUtility.activeVessel.SimulationObject.ManeuverPlan.AddNode(nodeData, true);
        // GameManager.Instance.Game.SpaceSimulation.Maneuvers.AddNodeToVessel(nodeData);

        // MNCNodeControl.SpitNode(nodeData);

        MNCUtility.activeVessel.Orbiter.ManeuverPlanSolver.UpdateManeuverTrajectory();

        // MNCNodeControl.SpitNode(nodeData);

        // For KSP2, We want the to start burns early to make them centered on the node
        var nodeTimeAdj = nodeData.BurnDuration * burnDurationOffsetFactor;

        // ManeuverNodeControllerMod.Logger.LogDebug($"AddManeuverNode: BurnDuration {nodeData.BurnDuration} s");

        // Refersh the Utility.currentNode with what we've produced here in prep for calling UpdateNode
        MNCUtility.RefreshActiveVesselAndCurrentManeuver();

        // Update the node to put a gizmo on it
        MNCNodeControl.UpdateNode(nodeData, nodeTimeAdj);

        // ManeuverNodeControllerMod.Logger.LogDebug("AddManeuverNode Done");
    }

    public static void UpdateNode(ManeuverNodeData nodeData, double nodeTimeAdj = 0)
    {
        MapCore mapCore = null;
        GameManager.Instance.Game.Map.TryGetMapCore(out mapCore);
        var m3d = mapCore.map3D;
        var maneuverManager = m3d.ManeuverManager;
        //IGGuid simID;
        //SimulationObjectModel simObject;

        //// Get the ManeuverPlanComponent for the active vessel
        //var universeModel = GameManager.Instance.Game.UniverseModel;
        //VesselComponent vesselComponent;
        //ManeuverPlanComponent maneuverPlanComponent;
        //if (Utility.currentNode != null)
        //{
        //    simID = Utility.currentNode.RelatedSimID;
        //    simObject = universeModel.FindSimObject(simID);
        //}
        //else
        //{
        //    simObject = Utility.activeVessel?.SimulationObject;
        //}

        //if (simObject != null)
        //{
        //    maneuverPlanComponent = simObject.FindComponent<ManeuverPlanComponent>();
        //}
        //else
        //{
        //    simObject = universeModel.FindSimObject(simID);
        //    maneuverPlanComponent = simObject.FindComponent<ManeuverPlanComponent>();
        //}

        //if (nodeTimeAdj != 0)
        //{
        //    nodeData.Time += nodeTimeAdj;
        //    if (nodeData.Time < GameManager.Instance.Game.UniverseModel.UniversalTime + 1) // Don't set node in the past
        //        nodeData.Time = GameManager.Instance.Game.UniverseModel.UniversalTime + 1;
        //    maneuverPlanComponent.UpdateTimeOnNode(nodeData, nodeData.Time); // This may not be necessary?
        //}

        //try { maneuverPlanComponent.RefreshManeuverNodeState(0); }
        //catch (NullReferenceException e) { ManeuverNodeControllerMod.Logger.LogError($"UpdateNode: caught NRE on call to maneuverPlanComponent.RefreshManeuverNodeState(0): {e}"); }

        // Utility.RefreshActiveVesselAndCurrentManeuver(); // do we need this?

        if (MNCUtility.currentNode != null)
        {
            // Manage the maneuver on the map
            maneuverManager.RemoveAll();
            try { maneuverManager?.GetNodeDataForVessels(); }
            catch (Exception e) { ManeuverNodeControllerMod.Logger.LogError($"UpdateNode: caught exception on call to mapCore.map3D.ManeuverManager.GetNodeDataForVessels(): {e}"); }
            try { maneuverManager.UpdateAll(); }
            catch (Exception e) { ManeuverNodeControllerMod.Logger.LogError($"UpdateNode: caught exception on call to mapCore.map3D.ManeuverManager.UpdateAll(): {e}"); }
            try { maneuverManager.UpdatePositionForGizmo(MNCUtility.currentNode.NodeID); }
            catch (Exception e) { ManeuverNodeControllerMod.Logger.LogError($"UpdateNode: caught exception on call to mapCore.map3D.ManeuverManager.UpdatePositionForGizmo(): {e}"); }
        }
    }

    public static void AddNode(PatchedConicsOrbit orbit)
    {
        // Add an empty maneuver node
        // ManeuverNodeControllerMod.Logger.LogDebug("handleButtons: Adding New Node");

        // Define empty node data
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        double burnUT;
        if (orbit.eccentricity < 1 && MNCNodeControl.Nodes.Count == 0)
        {
            burnUT = UT + orbit.TimeToAp;
        }
        else
        {
            if (MNCNodeControl.Nodes.Count > 0)
                burnUT = MNCNodeControl.Nodes[MNCNodeControl.Nodes.Count - 1].Time + Math.Min(orbit.period/10, 600);
            else
                burnUT = UT + 30;
        }

        Vector3d burnVector;
        burnVector.x = 0;
        burnVector.y = 0;
        burnVector.z = 0;

        MNCNodeControl.CreateManeuverNodeAtUT(burnVector, burnUT, 0);
        MNCNodeControl.RefreshManeuverNodes();
    }
}
