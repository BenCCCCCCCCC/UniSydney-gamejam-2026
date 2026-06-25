using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ToolIconCatalog", menuName = "Flip The Script/Tool Icon Catalog")]
public class ToolIconCatalog : ScriptableObject
{
    [SerializeField] private List<ToolIconEntry> entries = new();

    public Sprite GetIcon(string toolCardID)
    {
        TryGetIcon(toolCardID, out Sprite icon);
        return icon;
    }

    public bool TryGetIcon(string toolCardID, out Sprite icon)
    {
        icon = null;

        if (string.IsNullOrWhiteSpace(toolCardID))
        {
            return false;
        }

        foreach (ToolIconEntry entry in entries)
        {
            if (entry != null && entry.ToolCardID == toolCardID && entry.IconSprite != null)
            {
                icon = entry.IconSprite;
                return true;
            }
        }

        return false;
    }
}

[Serializable]
public class ToolIconEntry
{
    [SerializeField] private string toolCardID;
    [SerializeField] private Sprite iconSprite;

    public string ToolCardID => toolCardID;
    public Sprite IconSprite => iconSprite;
}
