using UnityEngine;

// Temporary game jam config for the runtime Node tool hand UI.
// Put this on a scene object when the generated hand area needs Inspector-tuned sizing.
public class NodeToolHandSceneConfig : MonoBehaviour
{
    [Header("Tool Card Art")]
    [SerializeField] private CardArtCatalog cardArtCatalog;
    [SerializeField] private bool useResourcesArtFallback = true;

    [Header("Tool Hand Layout")]
    [SerializeField] private Vector2 toolHandCardSize = new Vector2(210f, 294f);
    [SerializeField] private float toolHandSpacing = 24f;
    [SerializeField] private Vector2 handAreaAnchorMin = new Vector2(0.18f, 0.025f);
    [SerializeField] private Vector2 handAreaAnchorMax = new Vector2(0.82f, 0.22f);

    [Header("Placement Slot Layout")]
    [SerializeField] private bool autoAlignDropSlots = true;
    [SerializeField] private Vector2 placementDropSlotSize = new Vector2(170f, 220f);
    [SerializeField] private Vector2 placedCardSize = new Vector2(120f, 160f);
    [SerializeField] private float placedCardScale = 1f;
    [SerializeField] private bool placedCardPreserveAspect = true;

    [Header("Active Tool Text")]
    [SerializeField] private Vector2 activeToolTextAnchor = new Vector2(0.5f, 0.17f);
    [SerializeField] private Vector2 activeToolTextSize = new Vector2(900f, 44f);
    [SerializeField] private float activeToolTextFontSize = 24f;

    public CardArtCatalog CardArtCatalog => cardArtCatalog;
    public bool UseResourcesArtFallback => useResourcesArtFallback;
    public Vector2 ToolHandCardSize => HandCardPresentationApplier.ResolveCardSize(toolHandCardSize);
    public float ToolHandSpacing => toolHandSpacing;
    public Vector2 HandAreaAnchorMin => handAreaAnchorMin;
    public Vector2 HandAreaAnchorMax => handAreaAnchorMax;
    public bool AutoAlignDropSlots => autoAlignDropSlots;
    public Vector2 PlacementDropSlotSize => placementDropSlotSize;
    public Vector2 PlacedCardSize => placedCardSize;
    public float PlacedCardScale => placedCardScale;
    public bool PlacedCardPreserveAspect => placedCardPreserveAspect;
    public Vector2 ActiveToolTextAnchor => activeToolTextAnchor;
    public Vector2 ActiveToolTextSize => activeToolTextSize;
    public float ActiveToolTextFontSize => activeToolTextFontSize;

    private void OnValidate()
    {
        toolHandCardSize.x = Mathf.Max(1f, toolHandCardSize.x);
        toolHandCardSize.y = Mathf.Max(1f, toolHandCardSize.y);
        toolHandSpacing = Mathf.Max(0f, toolHandSpacing);
        placementDropSlotSize.x = Mathf.Max(1f, placementDropSlotSize.x);
        placementDropSlotSize.y = Mathf.Max(1f, placementDropSlotSize.y);
        placedCardSize.x = Mathf.Max(1f, placedCardSize.x);
        placedCardSize.y = Mathf.Max(1f, placedCardSize.y);
        placedCardScale = Mathf.Max(0.01f, placedCardScale);
        activeToolTextSize.x = Mathf.Max(1f, activeToolTextSize.x);
        activeToolTextSize.y = Mathf.Max(1f, activeToolTextSize.y);
        activeToolTextFontSize = Mathf.Max(1f, activeToolTextFontSize);
    }
}
