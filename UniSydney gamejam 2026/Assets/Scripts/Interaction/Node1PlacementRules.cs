// Compatibility wrapper for older Node1 bridge calls.
// The actual placement rules are now data-driven in NodePlacementRules.
public static class Node1PlacementRules
{
    public static bool TryPlaceTool(string toolCardID, PlacementPoint placementPoint)
    {
        return NodePlacementRules.TryPlaceTool(toolCardID, placementPoint);
    }
}
