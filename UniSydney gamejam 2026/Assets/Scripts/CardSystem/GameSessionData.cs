using System.Collections.Generic;

// Temporary game jam bridge data between CardBackpack and Node scenes.
// This is intentionally simple and only lives for the current play session.
public static class GameSessionData
{
    public static string CurrentNodeID { get; set; } = "Node1";

    public static readonly List<string> ToolCardIDs = new();

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
