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
            Debug.LogError("CardDatabase: cardSystemJson is missing.");
            return;
        }

        Data = JsonUtility.FromJson<CardSystemData>(cardSystemJson.text);

        if (Data == null)
        {
            Debug.LogError("CardDatabase: failed to parse JSON.");
            return;
        }

        cardsById.Clear();
        recipesByPairKey.Clear();

        foreach (var card in Data.cards)
        {
            if (!string.IsNullOrEmpty(card.CardID))
            {
                cardsById[card.CardID] = card;
            }
        }

        foreach (var recipe in Data.recipes)
        {
            if (!string.IsNullOrEmpty(recipe.PairKey))
            {
                recipesByPairKey[recipe.PairKey] = recipe;
            }
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

    public List<CardRow> GetBaseCardsForNode(string nodeId)
    {
        var result = new List<CardRow>();

        foreach (var loadout in Data.nodeLoadouts)
        {
            if (loadout.NodeID != nodeId) continue;

            string[] ids = loadout.AvailableBaseCardIDs.Split(',');

            foreach (string rawId in ids)
            {
                string id = rawId.Trim();

                if (cardsById.TryGetValue(id, out CardRow card))
                {
                    result.Add(card);
                }
            }

            break;
        }

        return result;
    }

    private string MakePairKey(string cardAId, string cardBId)
    {
        return string.CompareOrdinal(cardAId, cardBId) < 0
            ? $"{cardAId}+{cardBId}"
            : $"{cardBId}+{cardAId}";
    }
}