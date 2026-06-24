using System;
using System.Collections.Generic;

[Serializable]
public class CardSystemData
{
    public string version;
    public CardRules rules;
    public List<CardRow> cards;
    public List<RecipeRow> recipes;
    public List<NodeLoadoutRow> nodeLoadouts;
    public List<PlacementResultRow> placementResults;
}

[Serializable]
public class CardRules
{
    public bool uniquePairOutput;
    public bool pairOrderMatters;
    public string pairKeyRule;
}

[Serializable]
public class CardRow
{
    public string CardID;
    public string CardNameCN;
    public string CardType;
    public string RecipeID;
    public string DesignedForNode;
    public string Tags;
    public string DescriptionCN;
    public string IconHintCN;
    public bool IsBasicCard;
    public bool IsCraftedTool;
}

[Serializable]
public class RecipeRow
{
    public string RecipeID;
    public string InputAID;
    public string InputANameCN;
    public string InputBID;
    public string InputBNameCN;
    public string PairKey;
    public string OutputCardID;
    public string OutputCardNameCN;
    public string DesignedForNode;
    public string RecommendedPoint;
    public string ResultSummaryCN;
    public bool PairOrderMatters;
    public bool IsGlobalUniquePair;
}

[Serializable]
public class NodeLoadoutRow
{
    public string NodeID;
    public string NodeNameCN;
    public string StoryBridgeCN;
    public string AvailableBaseCardIDs;
    public string CoreToolCardIDs;
    public string OptionalToolCardIDs;
    public int RequiredSuccessCount;
    public string NotesCN;
}

[Serializable]
public class PlacementResultRow
{
    public string NodeID;
    public string PlacePointID;
    public string PlacePointNameCN;
    public string ToolCardID;
    public string OutcomeType;
    public string ResultID;
    public string ResultSummaryCN;
    public string NextState;
}
