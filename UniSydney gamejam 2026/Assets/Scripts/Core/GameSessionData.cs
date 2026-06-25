using System.Collections.Generic;
using UnityEngine;

public enum GameFlowPhase
{
    Briefing,
    CardCrafting,
    Placement,
    AutoPlay,
    Result
}

// Temporary game jam bridge data between CardBackpack and Node scenes.
public static class GameSessionData
{
    public static string CurrentNodeID { get; set; } = "Node1";
    public static string CurrentNodeSceneName { get; set; } = "Node1_QueenCastle";
    public static string CardBackpackSceneName { get; set; } = "CardBackpackTest";

    public static GameFlowPhase CurrentPhase { get; set; } = GameFlowPhase.Briefing;

    public static readonly List<string> ToolCardIDs = new();
    public static Texture2D CardBackpackBackgroundSnapshot { get; private set; }

    public static void SetCardBackpackBackgroundSnapshot(Texture2D snapshot)
    {
        if (CardBackpackBackgroundSnapshot == snapshot)
        {
            return;
        }

        ClearCardBackpackBackgroundSnapshot();
        CardBackpackBackgroundSnapshot = snapshot;
    }

    public static void ClearCardBackpackBackgroundSnapshot()
    {
        if (CardBackpackBackgroundSnapshot == null)
        {
            return;
        }

        Object.Destroy(CardBackpackBackgroundSnapshot);
        CardBackpackBackgroundSnapshot = null;
    }

    public static void StartNode(string nodeID, string sceneName)
    {
        CurrentNodeID = nodeID;
        CurrentNodeSceneName = sceneName;
        CurrentPhase = GameFlowPhase.Briefing;
        ToolCardIDs.Clear();
    }

    public static void EnterPlacementWithTools(IEnumerable<string> toolCardIDs)
    {
        SetToolCardIDs(toolCardIDs);
        CurrentPhase = GameFlowPhase.Placement;
    }

    public static void SetToolCardIDs(IEnumerable<string> toolCardIDs)
    {
        ToolCardIDs.Clear();

        if (toolCardIDs == null)
        {
            return;
        }

        foreach (string toolCardID in toolCardIDs)
        {
            if (!string.IsNullOrWhiteSpace(toolCardID))
            {
                ToolCardIDs.Add(toolCardID);
            }
        }
    }
}
