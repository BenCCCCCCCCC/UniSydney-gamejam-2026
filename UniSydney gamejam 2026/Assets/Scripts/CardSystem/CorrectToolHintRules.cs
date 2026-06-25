using System;
using System.Collections.Generic;
using UnityEngine;

public static class CorrectToolHintRules
{
    private static CardDatabase cachedDatabase;
    private static bool warnedMissingData;

    public static bool IsCorrectSolutionTool(string nodeID, string toolCardID)
    {
        if (!TryGetData(out CardSystemData data)
            || string.IsNullOrWhiteSpace(nodeID)
            || string.IsNullOrWhiteSpace(toolCardID))
        {
            return false;
        }

        foreach (PlacementResultRow row in data.placementResults)
        {
            if (row != null
                && row.NodeID == nodeID
                && row.ToolCardID == toolCardID
                && IsPositiveOutcome(row.OutcomeType))
            {
                Debug.Log(
                    $"CORRECT_TOOL_HINT_RULE: node={nodeID}, tool={toolCardID}, "
                    + $"point={row.PlacePointID}, outcome={row.OutcomeType}, correct=true");
                return true;
            }
        }

        Debug.Log(
            $"CORRECT_TOOL_HINT_RULE: node={nodeID}, tool={toolCardID}, correct=false");
        return false;
    }

    public static string GetNextCorrectPointID(string nodeID)
    {
        if (!TryGetData(out CardSystemData data)
            || string.IsNullOrWhiteSpace(nodeID))
        {
            return null;
        }

        List<string> orderedPointIDs = GetOrderedPositivePointIDs(data, nodeID);
        PlacementPoint[] scenePoints =
            UnityEngine.Object.FindObjectsByType<PlacementPoint>(
                FindObjectsInactive.Include);

        foreach (string pointID in orderedPointIDs)
        {
            PlacementPoint scenePoint = FindPoint(scenePoints, nodeID, pointID);
            if (scenePoint == null || !IsPointCorrectlySatisfied(data, scenePoint))
            {
                return pointID;
            }
        }

        return null;
    }

    public static bool IsCorrectForPoint(
        string nodeID,
        string pointID,
        string toolCardID)
    {
        if (!TryGetData(out CardSystemData data)
            || string.IsNullOrWhiteSpace(nodeID)
            || string.IsNullOrWhiteSpace(pointID)
            || string.IsNullOrWhiteSpace(toolCardID))
        {
            return false;
        }

        foreach (PlacementResultRow row in data.placementResults)
        {
            if (row != null
                && row.NodeID == nodeID
                && row.PlacePointID == pointID
                && row.ToolCardID == toolCardID
                && IsPositiveOutcome(row.OutcomeType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPositiveOutcome(string outcomeType)
    {
        return outcomeType == "Success"
            || outcomeType == "SuccessFunny"
            || outcomeType == "ReachDoor"
            || outcomeType == "ScorePlus";
    }

    private static bool TryGetData(out CardSystemData data)
    {
        if (cachedDatabase == null)
        {
            cachedDatabase = UnityEngine.Object.FindAnyObjectByType<CardDatabase>();
        }

        if (cachedDatabase == null)
        {
            GameObject databaseObject =
                new GameObject("RuntimeCorrectToolHintDatabase");
            cachedDatabase = databaseObject.AddComponent<CardDatabase>();
        }

        data = cachedDatabase != null ? cachedDatabase.Data : null;
        bool available = data != null && data.placementResults != null;
        if (!available && !warnedMissingData)
        {
            Debug.LogWarning(
                "CORRECT_TOOL_HINT_RULE: CardDatabase placementResults are unavailable.");
            warnedMissingData = true;
        }

        return available;
    }

    private static List<string> GetOrderedPositivePointIDs(
        CardSystemData data,
        string nodeID)
    {
        var pointIDs = new List<string>();

        foreach (PlacementResultRow row in data.placementResults)
        {
            if (row == null
                || row.NodeID != nodeID
                || !IsPositiveOutcome(row.OutcomeType)
                || string.IsNullOrWhiteSpace(row.PlacePointID)
                || pointIDs.Contains(row.PlacePointID))
            {
                continue;
            }

            pointIDs.Add(row.PlacePointID);
        }

        pointIDs.Sort(StringComparer.Ordinal);
        return pointIDs;
    }

    private static PlacementPoint FindPoint(
        PlacementPoint[] points,
        string nodeID,
        string pointID)
    {
        foreach (PlacementPoint point in points)
        {
            if (point != null
                && point.nodeID == nodeID
                && point.placePointID == pointID)
            {
                return point;
            }
        }

        return null;
    }

    private static bool IsPointCorrectlySatisfied(
        CardSystemData data,
        PlacementPoint point)
    {
        if (point == null || string.IsNullOrWhiteSpace(point.storedToolCardID))
        {
            return false;
        }

        foreach (PlacementResultRow row in data.placementResults)
        {
            if (row != null
                && row.NodeID == point.nodeID
                && row.PlacePointID == point.placePointID
                && row.ToolCardID == point.storedToolCardID
                && IsPositiveOutcome(row.OutcomeType))
            {
                return true;
            }
        }

        return false;
    }
}
