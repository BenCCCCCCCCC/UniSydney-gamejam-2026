using System.Collections.Generic;
using UnityEngine;

// Data-driven placement validation for the game jam bridge.
// Node usability comes from nodeLoadouts; valid point/tool matches come from placementResults.
public static class NodePlacementRules
{
    private const string DataResourcePath = "Data/FlipTheScript_CardSystem_Data";
    private const string WrongPointLine = "孩子：这个道具好像不是放在这里的。";
    private const string WrongNodeLine = "孩子：这个道具好像属于另一个故事片段。";

    private static CardSystemData data;
    private static readonly Dictionary<string, HashSet<string>> usableToolsByNode = new();

    public static bool TryPlaceTool(string toolCardID, PlacementPoint placementPoint)
    {
        if (placementPoint == null)
        {
            Debug.LogWarning("NodePlacementRules: placement point is null.");
            return false;
        }

        string nodeID = placementPoint.nodeID;
        string pointID = placementPoint.placePointID;

        if (!EnsureLoaded())
        {
            Debug.LogWarning($"INVALID_NODE_TOOL: {toolCardID} is not usable in {nodeID}");
            Debug.Log($"Future line: {WrongNodeLine}");
            return false;
        }

        if (!IsUsableInNode(nodeID, toolCardID))
        {
            Debug.Log($"INVALID_NODE_TOOL: {toolCardID} is not usable in {nodeID}");
            Debug.Log($"Future line: {WrongNodeLine}");
            return false;
        }

        if (!TryGetPlacementResult(nodeID, pointID, toolCardID, out PlacementResultRow result))
        {
            Debug.Log($"INVALID_PLACEMENT: {toolCardID} cannot be used on {pointID}");
            Debug.Log($"Future line: {WrongPointLine}");
            return false;
        }

        Debug.Log($"VALID_PLACEMENT: {toolCardID} on {pointID}");
        Debug.Log($"Future line: {result.ResultSummaryCN}");
        return true;
    }

    private static bool IsUsableInNode(string nodeID, string toolCardID)
    {
        if (string.IsNullOrWhiteSpace(nodeID) || string.IsNullOrWhiteSpace(toolCardID))
        {
            return false;
        }

        return usableToolsByNode.TryGetValue(nodeID, out HashSet<string> tools)
            && tools.Contains(toolCardID);
    }

    private static bool TryGetPlacementResult(string nodeID, string pointID, string toolCardID, out PlacementResultRow result)
    {
        result = null;

        if (data == null || data.placementResults == null)
        {
            return false;
        }

        foreach (PlacementResultRow row in data.placementResults)
        {
            if (row == null)
            {
                continue;
            }

            if (row.NodeID == nodeID && row.PlacePointID == pointID && row.ToolCardID == toolCardID)
            {
                result = row;
                return true;
            }
        }

        return false;
    }

    private static bool EnsureLoaded()
    {
        if (data != null)
        {
            return true;
        }

        TextAsset json = Resources.Load<TextAsset>(DataResourcePath);
        if (json == null)
        {
            Debug.LogError($"NodePlacementRules: failed to load {DataResourcePath}.");
            return false;
        }

        data = JsonUtility.FromJson<CardSystemData>(json.text);
        if (data == null || data.nodeLoadouts == null)
        {
            Debug.LogError("NodePlacementRules: failed to parse card system data.");
            return false;
        }

        usableToolsByNode.Clear();

        foreach (NodeLoadoutRow loadout in data.nodeLoadouts)
        {
            if (loadout == null || string.IsNullOrWhiteSpace(loadout.NodeID))
            {
                continue;
            }

            var tools = new HashSet<string>();
            AddToolIDs(tools, loadout.CoreToolCardIDs);
            AddToolIDs(tools, loadout.OptionalToolCardIDs);
            usableToolsByNode[loadout.NodeID] = tools;
        }

        return true;
    }

    private static void AddToolIDs(HashSet<string> target, string csv)
    {
        if (target == null || string.IsNullOrWhiteSpace(csv))
        {
            return;
        }

        string[] ids = csv.Split(',');
        foreach (string rawId in ids)
        {
            string id = rawId.Trim();
            if (!string.IsNullOrWhiteSpace(id))
            {
                target.Add(id);
            }
        }
    }
}
