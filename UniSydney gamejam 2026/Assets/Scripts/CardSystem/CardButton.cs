using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardButton : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button button;

    private CardRow card;
    private CardCraftingController controller;
    private bool selectableForCrafting;

    public void Setup(CardRow cardData, CardCraftingController owner, bool selectableForCrafting = true)
    {
        card = cardData;
        controller = owner;
        this.selectableForCrafting = selectableForCrafting;

        if (nameText != null)
        {
            nameText.text = CardDisplayNameHelper.ToEnglishName(card.CardID);
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClicked);
            button.interactable = selectableForCrafting;
        }
    }

    private void OnClicked()
    {
        if (!selectableForCrafting)
        {
            return;
        }

        controller.SelectCard(card);
    }
}
