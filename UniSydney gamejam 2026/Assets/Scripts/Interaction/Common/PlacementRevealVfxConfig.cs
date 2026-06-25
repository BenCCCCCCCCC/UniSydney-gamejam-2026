using UnityEngine;

[CreateAssetMenu(menuName = "Flip The Script/Placement Reveal VFX Config")]
public class PlacementRevealVfxConfig : ScriptableObject
{
    [Header("Smoke")]
    [SerializeField] private Sprite[] smokeFrames;
    [SerializeField] private float smokeFrameRate = 12f;
    [SerializeField] private float smokeScaleStart = 0.4f;
    [SerializeField] private float smokeScaleEnd = 1.5f;
    [SerializeField] private float smokeDuration = 0.35f;
    [SerializeField] private Vector2 smokeSize = new Vector2(1f, 1f);
    [SerializeField] private int smokeSortingOrder = 120;

    [Header("Placed Card")]
    [SerializeField] private float cardShrinkDuration = 0.18f;
    [SerializeField] private float cardFadeDuration = 0.18f;
    [SerializeField] private bool hidePlacedCardAfterEffect = true;

    [Header("Icon")]
    [SerializeField] private float iconSpawnDelay = 0.22f;
    [SerializeField] private float iconFallHeight = 1.2f;
    [SerializeField] private float iconFallDuration = 0.35f;
    [SerializeField] private int iconSortingOrder = 130;

    [Header("Rendering")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private bool usePlaceholderWhenNoSmokeFrames = true;

    public Sprite[] SmokeFrames => smokeFrames;
    public float CardShrinkDuration => cardShrinkDuration;
    public float CardFadeDuration => cardFadeDuration;
    public float SmokeFrameRate => smokeFrameRate;
    public float SmokeScaleStart => smokeScaleStart;
    public float SmokeScaleEnd => smokeScaleEnd;
    public float SmokeDuration => smokeDuration;
    public float IconSpawnDelay => iconSpawnDelay;
    public float IconFallHeight => iconFallHeight;
    public float IconFallDuration => iconFallDuration;
    public Vector2 SmokeSize => smokeSize;
    public int SmokeSortingOrder => smokeSortingOrder;
    public int IconSortingOrder => iconSortingOrder;
    public string SortingLayerName => string.IsNullOrWhiteSpace(sortingLayerName) ? "Default" : sortingLayerName;
    public bool HidePlacedCardAfterEffect => hidePlacedCardAfterEffect;
    public bool UsePlaceholderWhenNoSmokeFrames => usePlaceholderWhenNoSmokeFrames;

    private void OnValidate()
    {
        cardShrinkDuration = Mathf.Max(0f, cardShrinkDuration);
        cardFadeDuration = Mathf.Max(0f, cardFadeDuration);
        smokeFrameRate = Mathf.Max(1f, smokeFrameRate);
        smokeScaleStart = Mathf.Max(0.01f, smokeScaleStart);
        smokeScaleEnd = Mathf.Max(0.01f, smokeScaleEnd);
        smokeDuration = Mathf.Max(0f, smokeDuration);
        iconSpawnDelay = Mathf.Max(0f, iconSpawnDelay);
        iconFallHeight = Mathf.Max(0f, iconFallHeight);
        iconFallDuration = Mathf.Max(0f, iconFallDuration);
        smokeSize = new Vector2(Mathf.Max(0.01f, smokeSize.x), Mathf.Max(0.01f, smokeSize.y));
    }
}
