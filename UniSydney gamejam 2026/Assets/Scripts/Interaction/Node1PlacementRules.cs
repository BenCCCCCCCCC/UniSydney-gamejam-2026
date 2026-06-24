using UnityEngine;

// Temporary Node1 placement validation for the game jam bridge.
// This only judges and logs; placement storage/visuals are handled by drag/click callers.
public static class Node1PlacementRules
{
    private const string WrongPointLine = "孩子：这个道具好像不是放在这里的。";
    private const string WrongNodeLine = "孩子：这个道具应该属于别的故事片段。";

    public static bool IsNode1Tool(string toolCardID)
    {
        return toolCardID == "T_SPOTLIGHT_MIRROR"
            || toolCardID == "T_BROADCAST_BIRD"
            || toolCardID == "T_BOUNCY_CROWN"
            || toolCardID == "T_BEAUTY_RANKING"
            || toolCardID == "T_PAPER_CROWN_DOLL";
    }

    public static bool IsValidPlacement(string toolCardID, string placePointID, out string futureLine)
    {
        futureLine = string.Empty;

        if (toolCardID == "T_SPOTLIGHT_MIRROR" && placePointID == "N1_P1")
        {
            futureLine = "王后：为什么光都照在白雪公主身上？";
            return true;
        }

        if (toolCardID == "T_BROADCAST_BIRD" && placePointID == "N1_P2")
        {
            futureLine = "小鸟：号外号外，白雪公主最漂亮！";
            return true;
        }

        if (toolCardID == "T_BOUNCY_CROWN" && placePointID == "N1_P3")
        {
            futureLine = "王后：我的王冠为什么往白雪那边跳？";
            return true;
        }

        if (toolCardID == "T_BEAUTY_RANKING" && placePointID == "N1_P3")
        {
            futureLine = "孩子：排行榜说白雪第一，王后第二。";
            return true;
        }

        if (toolCardID == "T_PAPER_CROWN_DOLL" && placePointID == "N1_P2")
        {
            futureLine = "王后：她已经有自己的皇冠替身了？";
            return true;
        }

        return false;
    }

    public static bool TryPlaceTool(string toolCardID, PlacementPoint placementPoint)
    {
        if (placementPoint == null)
        {
            Debug.LogWarning("Node1PlacementRules: placement point is null.");
            return false;
        }

        if (!IsNode1Tool(toolCardID))
        {
            Debug.Log($"INVALID_NODE_TOOL: {toolCardID} is not usable in Node1");
            Debug.Log($"Future line: {WrongNodeLine}");
            return false;
        }

        if (!IsValidPlacement(toolCardID, placementPoint.placePointID, out string futureLine))
        {
            Debug.Log($"INVALID_PLACEMENT: {toolCardID} cannot be used on {placementPoint.placePointID}");
            Debug.Log($"Future line: {WrongPointLine}");
            return false;
        }

        Debug.Log($"VALID_PLACEMENT: {toolCardID} on {placementPoint.placePointID}");
        Debug.Log($"Future line: {futureLine}");
        return true;
    }
}
