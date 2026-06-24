using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visuals")]
    [SerializeField] private Image backImage;
    [SerializeField] private Image frontImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text backText;

    [Header("Input")]
    [SerializeField] private Button button;

    public CardRow Card { get; private set; }
    public bool IsFaceUp { get; private set; }
    public bool IsRemoved { get; private set; }

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Action<CardView> onClicked;
    private bool clickable;
    private bool hasBackSprite;
    private Vector3 normalScale = Vector3.one;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }
    }

    public void Setup(
        CardRow card,
        bool faceUp,
        bool clickable,
        Action<CardView> clicked,
        Sprite cardBackSprite,
        Sprite frontSprite,
        Sprite iconSprite,
        Color frontColor,
        Color backColor)
    {
        Card = card;
        onClicked = clicked;
        IsRemoved = false;

        EnsureRuntimeUi();
        ApplySprites(cardBackSprite, frontSprite, iconSprite, frontColor, backColor);
        SetClickable(clickable);
        SetFaceUp(faceUp);
    }

    public void SetFaceUp(bool faceUp)
    {
        IsFaceUp = faceUp;

        if (frontImage != null)
        {
            frontImage.gameObject.SetActive(faceUp);
        }

        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(faceUp && iconImage.sprite != null);
        }

        if (nameText != null)
        {
            nameText.gameObject.SetActive(faceUp);
            nameText.text = CardDisplayNameHelper.ToEnglishName(Card?.CardID);
        }

        if (backImage != null)
        {
            backImage.gameObject.SetActive(!faceUp);
        }

        if (backText != null)
        {
            backText.gameObject.SetActive(!faceUp && !hasBackSprite);
            backText.text = "?";
        }
    }

    public void SetClickable(bool value)
    {
        clickable = value;

        if (button != null)
        {
            button.interactable = value;
        }
    }

    public IEnumerator Shake()
    {
        if (rectTransform == null)
        {
            yield break;
        }

        Vector2 start = rectTransform.anchoredPosition;
        const float duration = 0.32f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = Mathf.Sin(elapsed * 70f) * 8f;
            rectTransform.anchoredPosition = start + new Vector2(x, 0f);
            yield return null;
        }

        rectTransform.anchoredPosition = start;
    }

    public IEnumerator PlayDisappear()
    {
        SetClickable(false);
        IsRemoved = true;

        float elapsed = 0f;
        const float duration = 0.22f;
        Vector3 startScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            canvasGroup.alpha = 1f - t;
            yield return null;
        }

        gameObject.SetActive(false);
    }

    public IEnumerator PlayAppear()
    {
        float elapsed = 0f;
        const float duration = 0.18f;
        canvasGroup.alpha = 0f;
        transform.localScale = Vector3.one * 0.8f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = t;
            transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, t);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        transform.localScale = Vector3.one;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (clickable && !IsRemoved)
        {
            transform.localScale = normalScale * 1.08f;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!IsRemoved)
        {
            transform.localScale = normalScale;
        }
    }

    private void HandleClick()
    {
        if (!clickable || IsRemoved)
        {
            return;
        }

        onClicked?.Invoke(this);
    }

    private void ApplySprites(Sprite cardBackSprite, Sprite frontSprite, Sprite iconSprite, Color frontColor, Color backColor)
    {
        if (backImage != null)
        {
            backImage.sprite = cardBackSprite;
            backImage.color = cardBackSprite == null ? backColor : Color.white;
            hasBackSprite = cardBackSprite != null;
        }

        if (frontImage != null)
        {
            frontImage.sprite = frontSprite;
            frontImage.color = frontSprite == null ? frontColor : Color.white;
        }

        if (iconImage != null)
        {
            iconImage.sprite = iconSprite;
            iconImage.color = iconSprite == null ? new Color(1f, 1f, 1f, 0f) : Color.white;
        }
    }

    private void EnsureRuntimeUi()
    {
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
            button.onClick.AddListener(HandleClick);
        }

        if (backImage == null)
        {
            backImage = CreateChildImage("BackImage");
        }

        if (frontImage == null)
        {
            frontImage = CreateChildImage("FrontImage");
        }

        if (iconImage == null)
        {
            iconImage = CreateChildImage("IconImage");
            RectTransform iconRect = iconImage.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0.56f);
            iconRect.anchorMax = new Vector2(0.5f, 0.56f);
            iconRect.sizeDelta = new Vector2(56f, 56f);
        }

        if (nameText == null)
        {
            GameObject textObject = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(transform, false);
            nameText = textObject.GetComponent<TextMeshProUGUI>();
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = new Color(0.11f, 0.08f, 0.05f, 1f);
            nameText.textWrappingMode = TextWrappingModes.Normal;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 10f;
            nameText.fontSizeMax = 18f;

            RectTransform textRect = nameText.rectTransform;
            textRect.anchorMin = new Vector2(0.08f, 0.12f);
            textRect.anchorMax = new Vector2(0.92f, 0.45f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        if (backText == null)
        {
            GameObject backTextObject = new GameObject("BackText", typeof(RectTransform), typeof(TextMeshProUGUI));
            backTextObject.transform.SetParent(transform, false);
            backText = backTextObject.GetComponent<TextMeshProUGUI>();
            backText.text = "?";
            backText.alignment = TextAlignmentOptions.Center;
            backText.color = new Color(0.88f, 0.9f, 1f, 1f);
            backText.fontStyle = FontStyles.Bold;
            backText.enableAutoSizing = true;
            backText.fontSizeMin = 24f;
            backText.fontSizeMax = 48f;

            RectTransform backTextRect = backText.rectTransform;
            backTextRect.anchorMin = new Vector2(0.1f, 0.1f);
            backTextRect.anchorMax = new Vector2(0.9f, 0.9f);
            backTextRect.offsetMin = Vector2.zero;
            backTextRect.offsetMax = Vector2.zero;
        }
    }

    private Image CreateChildImage(string objectName)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(transform, false);

        Image image = imageObject.GetComponent<Image>();
        RectTransform imageRect = image.rectTransform;
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        return image;
    }
}
