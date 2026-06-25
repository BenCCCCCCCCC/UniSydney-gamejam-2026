using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MemoryCountdownUI : MonoBehaviour
{
    private const string Message =
        "Memorize what you see.\nLet your imagination guide the strangest combinations.";

    private RectTransform progressFill;

    public static MemoryCountdownUI Create(Canvas canvas)
    {
        if (canvas == null)
        {
            return null;
        }

        GameObject rootObject = new GameObject(
            "MemoryCountdownUI",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(MemoryCountdownUI));

        rootObject.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.2f, 0.81f);
        rootRect.anchorMax = new Vector2(0.8f, 0.97f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.SetAsLastSibling();

        CanvasGroup rootGroup = rootObject.GetComponent<CanvasGroup>();
        rootGroup.interactable = false;
        rootGroup.blocksRaycasts = false;

        CreatePanelBackground(rootRect);
        CreateMessageText(rootRect);
        RectTransform fillRect = CreateProgressBar(rootRect);

        MemoryCountdownUI countdown = rootObject.GetComponent<MemoryCountdownUI>();
        countdown.progressFill = fillRect;
        countdown.SetProgress(1f);
        return countdown;
    }

    public static Canvas CreateFallbackCanvas()
    {
        GameObject canvasObject = new GameObject(
            "MemoryCountdownFallbackCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        return canvas;
    }

    public void SetProgress(float normalizedProgress)
    {
        if (progressFill == null)
        {
            return;
        }

        Vector2 anchorMax = progressFill.anchorMax;
        anchorMax.x = Mathf.Clamp01(normalizedProgress);
        progressFill.anchorMax = anchorMax;
    }

    public void HideAndDestroy()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private static void CreatePanelBackground(RectTransform parent)
    {
        GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        Stretch(panelRect);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.035f, 0.045f, 0.065f, 0.92f);
        panelImage.raycastTarget = false;
    }

    private static void CreateMessageText(RectTransform parent)
    {
        GameObject textObject = new GameObject(
            "Message",
            typeof(RectTransform),
            typeof(TextMeshProUGUI));

        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.05f, 0.34f);
        textRect.anchorMax = new Vector2(0.95f, 0.92f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = Message;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = 30f;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
    }

    private static RectTransform CreateProgressBar(RectTransform parent)
    {
        GameObject trackObject = new GameObject(
            "ProgressTrack",
            typeof(RectTransform),
            typeof(Image));

        trackObject.transform.SetParent(parent, false);

        RectTransform trackRect = trackObject.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0.08f, 0.12f);
        trackRect.anchorMax = new Vector2(0.92f, 0.27f);
        trackRect.offsetMin = Vector2.zero;
        trackRect.offsetMax = Vector2.zero;

        Image trackImage = trackObject.GetComponent<Image>();
        trackImage.color = new Color(0.14f, 0.16f, 0.2f, 1f);
        trackImage.raycastTarget = false;

        GameObject fillObject = new GameObject(
            "ProgressFill",
            typeof(RectTransform),
            typeof(Image));

        fillObject.transform.SetParent(trackRect, false);

        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.color = new Color(0.92f, 0.68f, 0.24f, 1f);
        fillImage.raycastTarget = false;
        return fillRect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
