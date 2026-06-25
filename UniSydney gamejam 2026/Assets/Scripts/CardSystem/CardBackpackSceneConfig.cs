using UnityEngine;

// Scene-level knobs for the runtime-built CardBackpackTest UI.
public class CardBackpackSceneConfig : MonoBehaviour
{
    [Header("Art")]
    [SerializeField] private CardArtCatalog cardArtCatalog;
    [SerializeField] private Sprite cardBackSprite;
    [SerializeField] private bool useResourcesArtFallback = true;

    [Header("Base Card Layout")]
    [SerializeField] private Vector2 baseCardSize = new Vector2(145f, 205f);
    [SerializeField] private Vector2 baseCardSpacing = new Vector2(18f, 18f);
    [SerializeField] private int maxBaseCardsPerRow = 8;
    [SerializeField] private RectOffset baseCardAreaPadding;

    [Header("Tool Hand Layout")]
    [SerializeField] private Vector2 toolHandCardSize = new Vector2(120f, 160f);

    [Header("Timing")]
    [SerializeField] private float previewSeconds = 3f;

    [Header("Manual Base Slots")]
    [SerializeField] private bool useManualBaseCardSlots;

    public CardArtCatalog CardArtCatalog => cardArtCatalog;
    public Sprite CardBackSprite => cardBackSprite;
    public bool UseResourcesArtFallback => useResourcesArtFallback;
    public Vector2 BaseCardSize => baseCardSize;
    public Vector2 BaseCardSpacing => baseCardSpacing;
    public int MaxBaseCardsPerRow => maxBaseCardsPerRow;
    public RectOffset BaseCardAreaPadding => GetSafeBaseCardAreaPadding();
    public Vector2 ToolHandCardSize => toolHandCardSize;
    public float PreviewSeconds => previewSeconds;
    public bool UseManualBaseCardSlots => useManualBaseCardSlots;

    private void Reset()
    {
        EnsureBaseCardAreaPadding();
    }

    private void OnValidate()
    {
        EnsureBaseCardAreaPadding();
        maxBaseCardsPerRow = Mathf.Max(1, maxBaseCardsPerRow);
        previewSeconds = Mathf.Max(0f, previewSeconds);
    }

    private RectOffset GetSafeBaseCardAreaPadding()
    {
        EnsureBaseCardAreaPadding();
        return baseCardAreaPadding;
    }

    private void EnsureBaseCardAreaPadding()
    {
        if (baseCardAreaPadding == null)
        {
            baseCardAreaPadding = new RectOffset(16, 16, 16, 16);
        }
    }
}
