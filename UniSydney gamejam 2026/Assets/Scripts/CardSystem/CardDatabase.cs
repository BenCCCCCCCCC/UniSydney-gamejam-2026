using System.Collections.Generic;
using UnityEngine;

public class CardDatabase : MonoBehaviour
{
    [Header("Put FlipTheScript_CardSystem_Data.json here")]
    [SerializeField] private TextAsset cardSystemJson;

    public CardSystemData Data { get; private set; }

    private readonly Dictionary<string, CardRow> cardsById = new();
    private readonly Dictionary<string, RecipeRow> recipesByPairKey = new();

    private void Awake()
    {
        Load();
    }

    private void Load()
    {
        if (cardSystemJson == null)
        {
            cardSystemJson = Resources.Load<TextAsset>("Data/FlipTheScript_CardSystem_Data");

            if (cardSystemJson == null)
            {
                Debug.LogError("CardDatabase: cardSystemJson is missing.");
                return;
            }
        }

        Data = JsonUtility.FromJson<CardSystemData>(cardSystemJson.text);

        if (Data == null)
        {
            Debug.LogError("CardDatabase: failed to parse JSON.");
            return;
        }

        if (Data.cards == null)
        {
            Debug.LogError("CardDatabase: JSON is missing cards.");
            return;
        }

        if (Data.recipes == null)
        {
            Debug.LogError("CardDatabase: JSON is missing recipes.");
            return;
        }

        if (Data.nodeLoadouts == null)
        {
            Debug.LogError("CardDatabase: JSON is missing nodeLoadouts.");
            return;
        }

        cardsById.Clear();
        recipesByPairKey.Clear();

        foreach (var card in Data.cards)
        {
            if (card == null)
            {
                Debug.LogWarning("CardDatabase: skipped null card row.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(card.CardID))
            {
                Debug.LogWarning("CardDatabase: skipped card with empty CardID.");
                continue;
            }

            if (cardsById.ContainsKey(card.CardID))
            {
                Debug.LogWarning($"CardDatabase: duplicate CardID found, overwriting previous card: {card.CardID}");
            }

            cardsById[card.CardID] = card;
        }

        foreach (var recipe in Data.recipes)
        {
            if (recipe == null)
            {
                Debug.LogWarning("CardDatabase: skipped null recipe row.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(recipe.PairKey))
            {
                Debug.LogWarning($"CardDatabase: skipped recipe with empty PairKey: {recipe.RecipeID}");
                continue;
            }

            if (recipesByPairKey.ContainsKey(recipe.PairKey))
            {
                Debug.LogError($"CardDatabase: duplicate PairKey found, keeping first recipe and skipping duplicate: {recipe.PairKey}");
                continue;
            }

            recipesByPairKey[recipe.PairKey] = recipe;
        }

        foreach (var loadout in Data.nodeLoadouts)
        {
            if (loadout == null)
            {
                Debug.LogWarning("CardDatabase: skipped null node loadout row.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(loadout.NodeID))
            {
                Debug.LogWarning("CardDatabase: node loadout has empty NodeID.");
            }

            if (string.IsNullOrWhiteSpace(loadout.AvailableBaseCardIDs))
            {
                Debug.LogWarning($"CardDatabase: node loadout has empty AvailableBaseCardIDs: {loadout.NodeID}");
            }

            ValidateLoadoutToolList(loadout, "CoreToolCardIDs", loadout.CoreToolCardIDs);
            ValidateLoadoutToolList(loadout, "OptionalToolCardIDs", loadout.OptionalToolCardIDs);
        }

        Debug.Log($"CardDatabase loaded. Cards: {cardsById.Count}, Recipes: {recipesByPairKey.Count}");
    }

    public bool TryGetCard(string cardId, out CardRow card)
    {
        return cardsById.TryGetValue(cardId, out card);
    }

    public bool TryCombine(string cardAId, string cardBId, out CardRow outputCard, out RecipeRow recipe)
    {
        outputCard = null;
        recipe = null;

        // Crafting is keyed only by stable CardID values, never by Chinese display names.
        if (string.IsNullOrWhiteSpace(cardAId) || string.IsNullOrWhiteSpace(cardBId))
        {
            return false;
        }

        string pairKey = MakePairKey(cardAId, cardBId);

        if (!recipesByPairKey.TryGetValue(pairKey, out recipe))
        {
            return false;
        }

        if (!cardsById.TryGetValue(recipe.OutputCardID, out outputCard))
        {
            Debug.LogWarning($"Recipe found but output card missing: {recipe.OutputCardID}");
            return false;
        }

        return true;
    }

    public bool IsToolAllowedInNode(string nodeId, string toolCardID)
    {
        if (string.IsNullOrWhiteSpace(nodeId) || string.IsNullOrWhiteSpace(toolCardID))
        {
            return false;
        }

        if (Data == null || Data.nodeLoadouts == null)
        {
            Debug.LogError("CardDatabase: cannot check node tool list before data is loaded.");
            return false;
        }

        foreach (NodeLoadoutRow loadout in Data.nodeLoadouts)
        {
            if (loadout == null || loadout.NodeID != nodeId)
            {
                continue;
            }

            return ContainsCsvID(loadout.CoreToolCardIDs, toolCardID)
                || ContainsCsvID(loadout.OptionalToolCardIDs, toolCardID);
        }

        return false;
    }

    public bool TryGetPlacementResult(
        string nodeId,
        string placePointID,
        string toolCardID,
        out PlacementResultRow result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(nodeId)
            || string.IsNullOrWhiteSpace(placePointID)
            || string.IsNullOrWhiteSpace(toolCardID))
        {
            return false;
        }

        if (Data == null || Data.placementResults == null)
        {
            Debug.LogError("CardDatabase: cannot check placement results before data is loaded.");
            return false;
        }

        foreach (PlacementResultRow row in Data.placementResults)
        {
            if (row == null)
            {
                continue;
            }

            if (row.NodeID == nodeId
                && row.PlacePointID == placePointID
                && row.ToolCardID == toolCardID)
            {
                result = row;
                return true;
            }
        }

        return false;
    }

    public List<CardRow> GetBaseCardsForNode(string nodeId)
    {
        var result = new List<CardRow>();

        if (string.IsNullOrWhiteSpace(nodeId))
        {
            Debug.LogWarning("CardDatabase: cannot get base cards for an empty nodeId.");
            return result;
        }

        if (Data == null || Data.nodeLoadouts == null)
        {
            Debug.LogError("CardDatabase: cannot get base cards before data is loaded.");
            return result;
        }

        foreach (var loadout in Data.nodeLoadouts)
        {
            if (loadout == null)
            {
                Debug.LogWarning("CardDatabase: skipped null node loadout row.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(loadout.NodeID))
            {
                Debug.LogWarning("CardDatabase: skipped node loadout with empty NodeID.");
                continue;
            }

            if (loadout.NodeID != nodeId) continue;

            if (string.IsNullOrWhiteSpace(loadout.AvailableBaseCardIDs))
            {
                Debug.LogWarning($"CardDatabase: node loadout has no base cards: {nodeId}");
                return result;
            }

            string[] ids = loadout.AvailableBaseCardIDs.Split(',');

            foreach (string rawId in ids)
            {
                string id = rawId.Trim();

                if (string.IsNullOrWhiteSpace(id))
                {
                    Debug.LogWarning($"CardDatabase: skipped empty base card id in node loadout: {nodeId}");
                    continue;
                }

                if (cardsById.TryGetValue(id, out CardRow card))
                {
                    if (!IsBasicCardForCrafting(card))
                    {
                        Debug.LogWarning($"CardDatabase: node loadout skipped non-basic card id: {id}");
                        continue;
                    }

                    result.Add(card);
                }
                else
                {
                    Debug.LogWarning($"CardDatabase: node loadout references missing card id: {id}");
                }
            }

            break;
        }

        return result;
    }

    private bool IsBasicCardForCrafting(CardRow card)
    {
        return card != null
            && card.IsBasicCard
            && !card.IsCraftedTool
            && !string.IsNullOrWhiteSpace(card.CardID)
            && card.CardID.StartsWith("B_", System.StringComparison.Ordinal);
    }

    private bool ContainsCsvID(string csv, string targetID)
    {
        if (string.IsNullOrWhiteSpace(csv) || string.IsNullOrWhiteSpace(targetID))
        {
            return false;
        }

        string[] ids = csv.Split(',');

        foreach (string rawId in ids)
        {
            if (rawId.Trim() == targetID)
            {
                return true;
            }
        }

        return false;
    }

    private void ValidateLoadoutToolList(NodeLoadoutRow loadout, string fieldName, string toolCardIDs)
    {
        if (loadout == null || string.IsNullOrWhiteSpace(loadout.NodeID) || string.IsNullOrWhiteSpace(toolCardIDs))
        {
            return;
        }

        string[] ids = toolCardIDs.Split(',');

        foreach (string rawId in ids)
        {
            string id = rawId.Trim();

            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            if (!cardsById.TryGetValue(id, out CardRow card))
            {
                Debug.LogWarning($"{loadout.NodeID} {fieldName} contains {id}, but that card does not exist.");
                continue;
            }

            if (card.DesignedForNode != loadout.NodeID)
            {
                Debug.LogWarning($"{loadout.NodeID} {fieldName} contains {id}, but it is DesignedForNode {card.DesignedForNode}.");
            }
        }
    }

    private string MakePairKey(string cardAId, string cardBId)
    {
        // PairKey is order-independent: sort the two CardIDs before joining them.
        // This keeps B_APPLE + B_POTION the same recipe as B_POTION + B_APPLE.
        return string.CompareOrdinal(cardAId, cardBId) < 0
            ? $"{cardAId}+{cardBId}"
            : $"{cardBId}+{cardAId}";
    }
}
