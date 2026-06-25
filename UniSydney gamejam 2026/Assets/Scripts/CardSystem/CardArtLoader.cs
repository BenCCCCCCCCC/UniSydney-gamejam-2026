using UnityEngine;

public class CardArtLoader : MonoBehaviour
{
    private static CardArtLoader activeLoader;

    [SerializeField] private CardArtCatalog catalog;

    private void Awake()
    {
        activeLoader = this;
    }

    private void OnEnable()
    {
        activeLoader = this;
    }

    public static Sprite GetSprite(string cardID, CardArtCatalog catalog, bool useResourcesFallback)
    {
        if (string.IsNullOrWhiteSpace(cardID))
        {
            Debug.LogWarning("CARD_ART_MISSING: empty CardID, using text fallback");
            return null;
        }

        CardArtCatalog resolvedCatalog = catalog != null ? catalog : activeLoader != null ? activeLoader.catalog : null;

        if (resolvedCatalog != null && resolvedCatalog.TryGetSprite(cardID, out Sprite catalogSprite))
        {
            Debug.Log($"CARD_ART_FOUND: {cardID}");
            return catalogSprite;
        }

        if (useResourcesFallback)
        {
            Sprite resourcesSprite = Resources.Load<Sprite>($"Art/item/{cardID}");
            if (resourcesSprite != null)
            {
                Debug.Log($"CARD_ART_FOUND: {cardID}");
                return resourcesSprite;
            }
        }

        Debug.Log($"CARD_ART_MISSING: {cardID}, using text fallback");
        return null;
    }
}
