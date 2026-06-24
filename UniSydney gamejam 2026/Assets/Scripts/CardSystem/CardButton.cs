using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardButton : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button button;

    private CardRow card;
    private CardCraftingController controller;

    public void Setup(CardRow cardData, CardCraftingController owner)
    {
        card = cardData;
        controller = owner;

        if (nameText != null)
        {
            nameText.text = card.CardNameCN;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClicked);
        }
    }

    private void OnClicked()
    {
        controller.SelectCard(card);
    }
}