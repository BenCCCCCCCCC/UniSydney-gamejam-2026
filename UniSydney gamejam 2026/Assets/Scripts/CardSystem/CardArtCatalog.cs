using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardArtCatalog", menuName = "Flip The Script/Card Art Catalog")]
public class CardArtCatalog : ScriptableObject
{
    [SerializeField] private List<CardArtEntry> entries = new();

    public bool TryGetSprite(string cardID, out Sprite sprite)
    {
        sprite = null;

        if (string.IsNullOrWhiteSpace(cardID))
        {
            return false;
        }

        foreach (CardArtEntry entry in entries)
        {
            if (entry != null && entry.CardID == cardID && entry.Sprite != null)
            {
                sprite = entry.Sprite;
                return true;
            }
        }

        return false;
    }
}

[Serializable]
public class CardArtEntry
{
    [SerializeField] private string cardID;
    [SerializeField] private Sprite sprite;

    public string CardID => cardID;
    public Sprite Sprite => sprite;
}
