using System;
using UnityEngine;
using UnityEngine.UI;

public static class HandCardViewFactory
{
    private static readonly Color DefaultFrontColor =
        new Color(0.66f, 0.78f, 0.95f, 1f);

    private static readonly Color DefaultBackColor =
        new Color(0.16f, 0.18f, 0.25f, 1f);

    public static CardView Create(
        Transform parent,
        CardRow card,
        Sprite cardBackSprite,
        Sprite frontSprite,
        HandCardPresentationSettings settings,
        Canvas canvas,
        Action<CardView> onClicked = null,
        bool clickable = false,
        CardView cardViewPrefab = null,
        bool enableCorrectToolHint = true)
    {
        if (parent == null || card == null)
        {
            return null;
        }

        settings = HandCardPresentationApplier.ResolveSettings(settings);

        CardView view;

        if (cardViewPrefab != null)
        {
            view = UnityEngine.Object.Instantiate(cardViewPrefab, parent);
        }
        else
        {
            GameObject cardObject = new GameObject(
                "CardView",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(CanvasGroup),
                typeof(CardView));

            cardObject.transform.SetParent(parent, false);

            Image hitImage = cardObject.GetComponent<Image>();
            hitImage.color = new Color(1f, 1f, 1f, 0f);

            view = cardObject.GetComponent<CardView>();
        }

        view.Setup(
            card,
            true,
            clickable,
            onClicked,
            cardBackSprite,
            frontSprite,
            null,
            DefaultFrontColor,
            DefaultBackColor);

        HandCardPresentationApplier.ApplyHandCard(
            view.gameObject,
            canvas,
            settings);
        if (enableCorrectToolHint)
        {
            CorrectToolHintEffect.ConfigureHandCard(view.gameObject, card.CardID);
            Debug.Log(
                $"CORRECT_TOOL_HAND_ATTACHED: card={card.CardID}, object={view.gameObject.name}");
        }

        return view;
    }
}
