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
            button.Setup(card, this);
        }
    }

    public void SelectCard(CardRow card)
    {
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

    private void CombineSelectedCards()
    {
        if (selectedA == null || selectedB == null)
        {
            resultText.text = "先选择两张基础牌。";
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
            resultText.text = $"“{selectedA.CardNameCN}”和“{selectedB.CardNameCN}”好像编不出故事。";
            return;
        }

        resultText.text = $"合成成功：{outputCard.CardNameCN}\n{recipe.ResultSummaryCN}";

        CardButton toolButton = Instantiate(cardButtonPrefab, backpackPanel);
        toolButton.Setup(outputCard, this);

        selectedA = null;
        selectedB = null;
        RefreshSelectedUI();
    }

    private void RefreshSelectedUI()
    {
        selectedAText.text = selectedA == null ? "Slot A: 空" : $"Slot A: {selectedA.CardNameCN}";
        selectedBText.text = selectedB == null ? "Slot B: 空" : $"Slot B: {selectedB.CardNameCN}";
    }

    private void ClearChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }
}
