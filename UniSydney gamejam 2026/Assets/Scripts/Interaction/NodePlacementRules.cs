using UnityEngine;

// Data-driven placement validation for the game jam bridge.
// Node usability comes from nodeLoadouts; valid point/tool matches come from placementResults.
public static class NodePlacementRules
{
    private const string WrongPointLine = "This tool does not belong here.";
    private const string WrongNodeLine = "This tool belongs to another story node.";

    private static CardDatabase cachedDatabase;

    public static bool TryPlaceTool(string toolCardID, PlacementPoint placementPoint)
    {
        if (placementPoint == null)
        {
            Debug.LogWarning("NodePlacementRules: placement point is null.");
            return false;
        }

        string nodeID = placementPoint.nodeID;
        string pointID = placementPoint.placePointID;

        if (!TryGetDatabase(out CardDatabase database))
        {
            Debug.LogWarning($"INVALID_NODE_TOOL: {toolCardID} is not usable in {nodeID}");
            Debug.Log($"Future line: {WrongNodeLine}");
            return false;
        }

        if (!database.IsToolAllowedInNode(nodeID, toolCardID))
        {
            Debug.Log($"INVALID_NODE_TOOL: {toolCardID} is not usable in {nodeID}");
            Debug.Log($"Future line: {WrongNodeLine}");
            return false;
        }

        if (!database.TryGetPlacementResult(nodeID, pointID, toolCardID, out PlacementResultRow result))
        {
            Debug.Log($"INVALID_PLACEMENT: {toolCardID} cannot be used on {pointID}");
            Debug.Log($"Future line: {WrongPointLine}");
            return false;
        }

        Debug.Log($"VALID_PLACEMENT: {toolCardID} on {pointID}");
        Debug.Log($"Future line: {result.ResultSummaryCN}");
        return true;
    }

    private static bool TryGetDatabase(out CardDatabase database)
    {
        if (cachedDatabase == null)
        {
            cachedDatabase = Object.FindAnyObjectByType<CardDatabase>();
        }

        if (cachedDatabase == null)
        {
            GameObject databaseObject = new GameObject("RuntimeCardDatabase");
            cachedDatabase = databaseObject.AddComponent<CardDatabase>();
        }

        database = cachedDatabase;
        return database != null && database.Data != null;
    }
}
