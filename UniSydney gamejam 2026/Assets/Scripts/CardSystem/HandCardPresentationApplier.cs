using UnityEngine;
using UnityEngine.UI;

public enum HandCardVerticalCropAlignment
{
    ShowUpperPart,
    ShowCenterPart,
    ShowLowerPart
}

public struct HandCardPresentationSettings
{
    public Vector2 CardSize;
    public float Overlap;
    public HandCardVerticalCropAlignment CropAlignment;
    public float HoverScale;
    public float HoverLiftY;
    public float HoverEnterDuration;
    public float HoverPreviewOffsetY;
    public float OriginalCardHoverAlpha;
    public float HoverExitDuration;
    public float OriginalCardRestoreDuration;
    public float HoverFadeDuration;

    public static HandCardPresentationSettings CreateDefaults()
    {
        return new HandCardPresentationSettings
        {
            CardSize = HandCardPresentationApplier.DefaultCardSize,
            Overlap = 70f,
            CropAlignment = HandCardVerticalCropAlignment.ShowUpperPart,
            HoverScale = 1.18f,
            HoverLiftY = 60f,
            HoverEnterDuration = 0.12f,
            HoverPreviewOffsetY = 80f,
            OriginalCardHoverAlpha = 0f,
            HoverExitDuration = 0.16f,
            OriginalCardRestoreDuration = 0.12f,
            HoverFadeDuration = 0.12f
        };
    }
}

public static class HandCardPresentationApplier
{
    public static readonly Vector2 DefaultCardSize = new Vector2(210f, 294f);

    private static readonly Vector2 LegacyCardSize120x160 = new Vector2(120f, 160f);
    private static readonly Vector2 LegacyCardSize150x150 = new Vector2(150f, 150f);

    private static HandCardPresentationSettings currentSettings =
        HandCardPresentationSettings.CreateDefaults();

    private static bool hasPublishedSettings;
    private static bool loggedDefaultSettings;

    public static void PublishSettings(HandCardPresentationSettings settings)
    {
        currentSettings = ResolveSettings(settings);
        hasPublishedSettings = true;
        Debug.Log($"HAND_CARD_PRESENTATION_SIZE: shared size = {currentSettings.CardSize.x:0}x{currentSettings.CardSize.y:0}");
    }

    public static HandCardPresentationSettings GetCurrentOrDefaults()
    {
        if (hasPublishedSettings)
        {
            return currentSettings;
        }

        HandCardPresentationSettings defaults =
            HandCardPresentationSettings.CreateDefaults();

        if (!loggedDefaultSettings)
        {
            Debug.Log($"HAND_CARD_PRESENTATION_SIZE: using code defaults {defaults.CardSize.x:0}x{defaults.CardSize.y:0}");
            loggedDefaultSettings = true;
        }

        return defaults;
    }

    public static HandCardPresentationSettings ResolveSettings(
        HandCardPresentationSettings settings)
    {
        return Sanitize(settings);
    }

    public static Vector2 ResolveCardSize(Vector2 requestedSize)
    {
        if (requestedSize.x <= 0f
            || requestedSize.y <= 0f
            || Approximately(requestedSize, LegacyCardSize120x160)
            || Approximately(requestedSize, LegacyCardSize150x150))
        {
            return DefaultCardSize;
        }

        return requestedSize;
    }

    public static void ApplyHandArea(
        RectTransform handArea,
        HandCardPresentationSettings settings)
    {
        if (handArea == null)
        {
            return;
        }

        settings = Sanitize(settings);
        MakeViewportTransparent(handArea);
        AnchorViewportToBottom(handArea);

        HorizontalLayoutGroup layout = handArea.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
        {
            layout = handArea.gameObject.AddComponent<HorizontalLayoutGroup>();
        }

        layout.spacing = -settings.Overlap;
        layout.childAlignment = GetTextAnchor(settings.CropAlignment);
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        if (handArea.GetComponent<RectMask2D>() == null)
        {
            handArea.gameObject.AddComponent<RectMask2D>();
        }
    }

    private static void MakeViewportTransparent(RectTransform handArea)
    {
        Image viewportImage = handArea.GetComponent<Image>();
        if (viewportImage == null)
        {
            return;
        }

        Color color = viewportImage.color;
        color.a = 0f;
        viewportImage.color = color;
        viewportImage.raycastTarget = false;
    }

    private static void AnchorViewportToBottom(RectTransform handArea)
    {
        float anchorHeight = handArea.anchorMax.y - handArea.anchorMin.y;

        if (Mathf.Abs(anchorHeight) > 0.0001f)
        {
            Vector2 anchorMin = handArea.anchorMin;
            Vector2 anchorMax = handArea.anchorMax;
            anchorMin.y = 0f;
            anchorMax.y = Mathf.Clamp01(anchorHeight);
            handArea.anchorMin = anchorMin;
            handArea.anchorMax = anchorMax;
            handArea.offsetMin = new Vector2(handArea.offsetMin.x, 0f);
            handArea.offsetMax = new Vector2(handArea.offsetMax.x, 0f);
            return;
        }

        Vector2 fixedAnchorMin = handArea.anchorMin;
        Vector2 fixedAnchorMax = handArea.anchorMax;
        fixedAnchorMin.y = 0f;
        fixedAnchorMax.y = 0f;
        handArea.anchorMin = fixedAnchorMin;
        handArea.anchorMax = fixedAnchorMax;
        handArea.pivot = new Vector2(handArea.pivot.x, 0f);
        handArea.anchoredPosition = new Vector2(handArea.anchoredPosition.x, 0f);
    }

    public static void ApplyHandCard(
        GameObject cardObject,
        Canvas canvas,
        HandCardPresentationSettings settings)
    {
        if (cardObject == null)
        {
            return;
        }

        settings = Sanitize(settings);

        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        if (cardRect != null)
        {
            cardRect.sizeDelta = settings.CardSize;
            cardRect.localScale = Vector3.one;
        }

        LayoutElement layoutElement = cardObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = cardObject.AddComponent<LayoutElement>();
        }

        layoutElement.preferredWidth = settings.CardSize.x;
        layoutElement.preferredHeight = settings.CardSize.y;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;

        foreach (Image image in cardObject.GetComponentsInChildren<Image>(true))
        {
            image.preserveAspect = true;
        }

        if (canvas == null)
        {
            return;
        }

        HandCardHoverEffect hoverEffect = cardObject.GetComponent<HandCardHoverEffect>();
        if (hoverEffect == null)
        {
            hoverEffect = cardObject.AddComponent<HandCardHoverEffect>();
        }

        hoverEffect.Configure(
            canvas,
            settings.HoverScale,
            settings.HoverLiftY,
            settings.HoverEnterDuration,
            settings.HoverPreviewOffsetY,
            settings.OriginalCardHoverAlpha,
            settings.HoverExitDuration,
            settings.OriginalCardRestoreDuration,
            settings.HoverFadeDuration);
    }

    public static Vector3 CalculatePendingCardWorldPosition(
        RectTransform handArea,
        int finalCardCount,
        HandCardPresentationSettings settings)
    {
        if (handArea == null)
        {
            return Vector3.zero;
        }

        settings = Sanitize(settings);

        HorizontalLayoutGroup layout = handArea.GetComponent<HorizontalLayoutGroup>();
        RectOffset padding = layout != null ? layout.padding : new RectOffset();
        float spacing = layout != null ? layout.spacing : -settings.Overlap;
        Rect handRect = handArea.rect;
        int safeCardCount = Mathf.Max(1, finalCardCount);
        float totalCardsWidth = safeCardCount * settings.CardSize.x;
        float totalSpacingWidth = Mathf.Max(0, safeCardCount - 1) * spacing;
        float contentWidth = totalCardsWidth + totalSpacingWidth;
        float innerWidth = Mathf.Max(0f, handRect.width - padding.horizontal);
        float firstCardLeft = handRect.xMin + padding.left + (innerWidth - contentWidth) * 0.5f;
        float targetX = firstCardLeft
            + (safeCardCount - 1) * (settings.CardSize.x + spacing)
            + settings.CardSize.x * 0.5f;
        float targetY = GetVerticalCardCenter(handRect, padding, settings);

        return handArea.TransformPoint(new Vector3(targetX, targetY, 0f));
    }

    private static float GetVerticalCardCenter(
        Rect handRect,
        RectOffset padding,
        HandCardPresentationSettings settings)
    {
        return settings.CropAlignment switch
        {
            HandCardVerticalCropAlignment.ShowUpperPart =>
                handRect.yMax - padding.top - settings.CardSize.y * 0.5f,
            HandCardVerticalCropAlignment.ShowLowerPart =>
                handRect.yMin + padding.bottom + settings.CardSize.y * 0.5f,
            _ => handRect.center.y + (padding.bottom - padding.top) * 0.5f
        };
    }

    private static TextAnchor GetTextAnchor(HandCardVerticalCropAlignment alignment)
    {
        return alignment switch
        {
            HandCardVerticalCropAlignment.ShowUpperPart => TextAnchor.UpperCenter,
            HandCardVerticalCropAlignment.ShowLowerPart => TextAnchor.LowerCenter,
            _ => TextAnchor.MiddleCenter
        };
    }

    private static HandCardPresentationSettings Sanitize(
        HandCardPresentationSettings settings)
    {
        settings.CardSize = ResolveCardSize(settings.CardSize);
        settings.Overlap = Mathf.Max(0f, settings.Overlap);
        settings.HoverScale = Mathf.Max(0.01f, settings.HoverScale);
        settings.HoverEnterDuration = Mathf.Max(0f, settings.HoverEnterDuration);
        settings.OriginalCardHoverAlpha = Mathf.Clamp01(settings.OriginalCardHoverAlpha);
        settings.HoverExitDuration = Mathf.Max(0f, settings.HoverExitDuration);
        settings.OriginalCardRestoreDuration = Mathf.Max(0f, settings.OriginalCardRestoreDuration);
        settings.HoverFadeDuration = Mathf.Max(0f, settings.HoverFadeDuration);
        return settings;
    }

    private static bool Approximately(Vector2 value, Vector2 expected)
    {
        return Mathf.Abs(value.x - expected.x) < 0.01f
            && Mathf.Abs(value.y - expected.y) < 0.01f;
    }
}
