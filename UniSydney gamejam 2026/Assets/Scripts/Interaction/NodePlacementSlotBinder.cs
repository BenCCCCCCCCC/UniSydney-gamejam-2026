using System;
using System.Collections.Generic;
using UnityEngine;

public class NodePlacementSlotBinder : MonoBehaviour
{
    private static NodePlacementSlotBinder activeBinder;

    [SerializeField] private List<NodePlacementSlotBinding> bindings = new();

    private readonly Dictionary<string, RectTransform> slotsByPointID = new();

    private void Awake()
    {
        activeBinder = this;
        RebuildLookup();
    }

    private void OnEnable()
    {
        activeBinder = this;
        RebuildLookup();
    }

    public static RectTransform GetDropSlotForPoint(string placePointID)
    {
        NodePlacementSlotBinder binder = GetActiveBinder();
        if (binder == null)
        {
            return null;
        }

        return binder.GetSlot(placePointID);
    }

    public static bool HasCompleteNode1Binding()
    {
        NodePlacementSlotBinder binder = GetActiveBinder();
        return binder != null
            && binder.GetSlot("N1_P1") != null
            && binder.GetSlot("N1_P2") != null
            && binder.GetSlot("N1_P3") != null;
    }

    private static NodePlacementSlotBinder GetActiveBinder()
    {
        if (activeBinder != null)
        {
            return activeBinder;
        }

        activeBinder = FindAnyObjectByType<NodePlacementSlotBinder>();
        if (activeBinder != null)
        {
            activeBinder.RebuildLookup();
        }

        return activeBinder;
    }

    private RectTransform GetSlot(string placePointID)
    {
        if (slotsByPointID.TryGetValue(placePointID, out RectTransform slot) && slot != null)
        {
            return slot;
        }

        return null;
    }

    private void RebuildLookup()
    {
        slotsByPointID.Clear();

        foreach (NodePlacementSlotBinding binding in bindings)
        {
            if (binding == null || string.IsNullOrWhiteSpace(binding.PlacePointID) || binding.DropSlot == null)
            {
                continue;
            }

            slotsByPointID[binding.PlacePointID] = binding.DropSlot;
            Debug.Log($"DROP_SLOT_BOUND: {binding.PlacePointID} -> {binding.DropSlot.name}");
        }
    }
}

[Serializable]
public class NodePlacementSlotBinding
{
    [SerializeField] private string placePointID;
    [SerializeField] private RectTransform dropSlot;

    public string PlacePointID => placePointID;
    public RectTransform DropSlot => dropSlot;
}
