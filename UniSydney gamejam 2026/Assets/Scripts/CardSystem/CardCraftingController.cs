using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardCraftingController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CardDatabase database;
    [SerializeField] private string currentNodeId = "Node2";

    [Header("UI")]
    [SerializeField] private Transform baseCardPanel;
    [SerializeField] private Transform backpackPanel;
    [SerializeField] private CardButton cardButtonPrefab;
    [SerializeField] private TMP_Text selectedAText;
    [SerializeField] private TMP_Text selectedBText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button combineButton;

    private CardRow selectedA;
    private CardRow selectedB;

    private void Start()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        combineButton.onClick.AddListener(CombineSelectedCards);
        RefreshBaseCards();
        RefreshSelectedUI();
    }

    private bool HasRequiredReferences()
    {
        bool hasAllReferences = true;

        if (database == null)
        {
            Debug.LogError("CardCraftingController: database is missing.");
            hasAllReferences = false;
        }

        if (combineButton == null)
        {
            Debug.LogError("CardCraftingController: combineButton is missing.");
            hasAllReferences = false;
        }

        if (baseCardPanel == null)
        {
            Debug.LogError("CardCraftingController: baseCardPanel is missing.");
            hasAllReferences = false;
        }

        if (backpackPanel == null)
        {
            Debug.LogError("CardCraftingController: backpackPanel is missing.");
            hasAllReferences = false;
        }

        if (cardButtonPrefab == null)
        {
            Debug.LogError("CardCraftingController: cardButtonPrefab is missing.");
            hasAllReferences = false;
        }

        if (selectedAText == null)
        {
            Debug.LogError("CardCraftingController: selectedAText is missing.");
            hasAllReferences = false;
        }

        if (selectedBText == null)
        {
            Debug.LogError("CardCraftingController: selectedBText is missing.");
            hasAllReferences = false;
        }

        if (resultText == null)
        {
            Debug.LogError("CardCraftingController: resultText is missing.");
            hasAllReferences = false;
        }

        return hasAllReferences;
    }

    private void RefreshBaseCards()
    {
        ClearChildren(baseCardPanel);

        var baseCards = database.GetBaseCardsForNode(currentNodeId);

        foreach (var card in baseCards)
        {
            CardButton button = Instantiate(cardButtonPrefab, baseCardPanel);
            button.Setup(card, this, true);
        }
    }

    public void SelectCard(CardRow card)
    {
        if (!CanSelectForCrafting(card))
        {
            return;
        }

        if (selectedA == null)
        {
            selectedA = card;
        }
        else if (selectedB == null)
        {
            selectedB = card;
        }
        else
        {
            selectedA = card;
            selectedB = null;
        }

        RefreshSelectedUI();
    }

    private bool CanSelectForCrafting(CardRow card)
    {
        return card != null
            && card.IsBasicCard
            && !card.IsCraftedTool
            && !string.IsNullOrWhiteSpace(card.CardID)
            && card.CardID.StartsWith("B_", System.StringComparison.Ordinal);
    }

    private void CombineSelectedCards()
    {
        if (selectedA == null || selectedB == null)
        {
            resultText.text = "Select two base cards first.";
            return;
        }

        bool success = database.TryCombine(
            selectedA.CardID,
            selectedB.CardID,
            out CardRow outputCard,
            out RecipeRow recipe
        );

        if (!success)
        {
            string selectedAName = CardDisplayNameHelper.ToEnglishName(selectedA.CardID);
            string selectedBName = CardDisplayNameHelper.ToEnglishName(selectedB.CardID);
            resultText.text = $"No recipe for {selectedAName} + {selectedBName}.";
            return;
        }

        string outputName = CardDisplayNameHelper.ToEnglishName(outputCard.CardID);
        resultText.text = $"Crafted: {outputName}\nID: {outputCard.CardID}";

        CardButton toolButton = Instantiate(cardButtonPrefab, backpackPanel);
        toolButton.Setup(outputCard, this, false);

        selectedA = null;
        selectedB = null;
        RefreshSelectedUI();
    }

    private void RefreshSelectedUI()
    {
        selectedAText.text = selectedA == null ? "Selected A: Empty" : $"Selected A: {CardDisplayNameHelper.ToEnglishName(selectedA.CardID)}";
        selectedBText.text = selectedB == null ? "Selected B: Empty" : $"Selected B: {CardDisplayNameHelper.ToEnglishName(selectedB.CardID)}";
    }

    private void ClearChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }
}
