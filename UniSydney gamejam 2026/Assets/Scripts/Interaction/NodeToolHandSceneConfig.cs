using UnityEngine;

// Temporary game jam config for the runtime Node tool hand UI.
// Put this on a scene object when the generated hand area needs Inspector-tuned sizing.
public class NodeToolHandSceneConfig : MonoBehaviour
{
    [Header("Tool Card Art")]
    [SerializeField] private CardArtCatalog cardArtCatalog;
    [SerializeField] private bool useResourcesArtFallback = true;

    [Header("Tool Hand Layout")]
    [SerializeField] private Vector2 toolHandCardSize = new Vector2(120f, 160f);
    [SerializeField] private float toolHandSpacing = 24f;
    [SerializeField] private Vector2 handAreaAnchorMin = new Vector2(0.18f, 0.025f);
    [SerializeField] private Vector2 handAreaAnchorMax = new Vector2(0.82f, 0.22f);

    public CardArtCatalog CardArtCatalog => cardArtCatalog;
    public bool UseResourcesArtFallback => useResourcesArtFallback;
    public Vector2 ToolHandCardSize => toolHandCardSize;
    public float ToolHandSpacing => toolHandSpacing;
    public Vector2 HandAreaAnchorMin => handAreaAnchorMin;
    public Vector2 HandAreaAnchorMax => handAreaAnchorMax;

    private void OnValidate()
    {
        toolHandCardSize.x = Mathf.Max(1f, toolHandCardSize.x);
        toolHandCardSize.y = Mathf.Max(1f, toolHandCardSize.y);
        toolHandSpacing = Mathf.Max(0f, toolHandSpacing);
    }
}
