using System;
using System.Collections.Generic;
using UnityEngine;

// Optional game jam helper: bind specific base CardIDs to hand-placed UI slots.
public class BaseCardSlotBinder : MonoBehaviour
{
    [SerializeField] private List<BaseCardSlotBinding> bindings = new();

    private readonly Dictionary<string, RectTransform> slotsByCardID = new();

    private void Awake()
    {
        RebuildLookup();
    }

    private void OnEnable()
    {
        RebuildLookup();
    }

    public bool TryGetSlot(string cardID, out RectTransform slot)
    {
        if (slotsByCardID.Count == 0)
        {
            RebuildLookup();
        }

        return slotsByCardID.TryGetValue(cardID, out slot) && slot != null;
    }

    private void RebuildLookup()
    {
        slotsByCardID.Clear();

        foreach (BaseCardSlotBinding binding in bindings)
        {
            if (binding == null || string.IsNullOrWhiteSpace(binding.CardID) || binding.Slot == null)
            {
                continue;
            }

            slotsByCardID[binding.CardID] = binding.Slot;
        }
    }
}

[Serializable]
public class BaseCardSlotBinding
{
    [SerializeField] private string cardID;
    [SerializeField] private RectTransform slot;

    public string CardID => cardID;
    public RectTransform Slot => slot;
}
