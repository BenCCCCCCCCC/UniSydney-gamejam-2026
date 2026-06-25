using UnityEngine;

/// <summary>
/// Node2（Node2_1 + Node2_2）放置规则验证。
/// 有效道具：T_HONEY_APPLE（蜜糖苹果）/ T_MAGIC_CLOAK（迷幻斗篷）/ T_HALLUCINOGEN_WINE（致幻酒）
/// </summary>
public static class Node2PlacementRules
{
    private const string WrongPointLine = "这个道具好像不该放在这里。";
    private const string WrongNodeLine = "这个道具属于别的故事片段。";

    public static bool IsNode2Tool(string toolCardID)
    {
        return toolCardID == "T_HONEY_APPLE"
            || toolCardID == "T_MAGIC_CLOAK"
            || toolCardID == "T_HALLUCINOGEN_WINE";
    }

    public static bool IsValidPlacement(string toolCardID, string placePointID, out string futureLine)
    {
        futureLine = string.Empty;

        // Node2_1
        if (toolCardID == "T_HONEY_APPLE" && placePointID == "N2_P1")
        {
            futureLine = "野猪：那是我的苹果！";
            return true;
        }

        // Node2_2
        if (toolCardID == "T_MAGIC_CLOAK" && placePointID == "N2_P2")
        {
            futureLine = "猎人：前面那个不是公主，是……野猪？";
            return true;
        }

        if (toolCardID == "T_HALLUCINOGEN_WINE" && placePointID == "N2_P3")
        {
            futureLine = "猎人：我怎么看什么都是树皮？";
            return true;
        }

        return false;
    }

    public static bool TryPlaceTool(string toolCardID, PlacementPoint placementPoint)
    {
        if (placementPoint == null)
        {
            Debug.LogWarning("Node2PlacementRules: placement point is null。");
            return false;
        }

        if (!IsNode2Tool(toolCardID))
        {
            Debug.Log($"INVALID_NODE_TOOL: {toolCardID} 不适用于Node2。");
            Debug.Log($"提示：{WrongNodeLine}");
            return false;
        }

        if (!IsValidPlacement(toolCardID, placementPoint.placePointID, out string futureLine))
        {
            Debug.Log($"INVALID_PLACEMENT: {toolCardID} 不能放在 {placementPoint.placePointID}。");
            Debug.Log($"提示：{WrongPointLine}");
            return false;
        }

        Debug.Log($"VALID_PLACEMENT: {toolCardID} → {placementPoint.placePointID}");
        Debug.Log($"预告台词：{futureLine}");
        placementPoint.SetTool(toolCardID);
        return true;
    }
}
