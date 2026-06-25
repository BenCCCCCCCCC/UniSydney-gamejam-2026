using System;
using System.Collections.Generic;
using UnityEngine;

// Scene setup:
// 1. Create > Flip The Script > Tool Icon Catalog, then bind ToolCardID -> icon Sprite in Inspector.
// 2. Add this component to a scene object such as NodeIconPresenter.
// 3. Fill placementBindings with placePointID, PlacementPoint, and an iconTarget transform.
// 4. Add NodeIconPresenter.PlayPlacedToolIcons to the Play Button OnClick list.
public class PlacedToolIconPresenter : MonoBehaviour
{
    [Header("Icon Data")]
    [SerializeField] private ToolIconCatalog toolIconCatalog;

    [Header("Scene Targets")]
    [SerializeField] private Transform iconWorldParent;
    [SerializeField] private List<PlacementIconBinding> placementBindings = new();

    [Header("Animation")]
    [SerializeField] private float fallHeight = 1.5f;
    [SerializeField] private float fallDuration = 0.35f;
    [SerializeField] private Vector3 worldOffset;
    [SerializeField] private Vector2 iconSize = new Vector2(1f, 1f);

    [Header("Rendering")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 50;

    [Header("Optional UI Cleanup")]
    [SerializeField] private bool hidePlacedCardUIAfterSpawn;
    [SerializeField] private bool clearOldIconsBeforePlay = true;

    private readonly List<GameObject> spawnedIcons = new();

    public void PlayPlacedToolIcons()
    {
        if (clearOldIconsBeforePlay)
        {
            ClearSpawnedIcons();
        }

        foreach (PlacementIconBinding binding in placementBindings)
        {
            if (binding == null)
            {
                continue;
            }

            string pointID = ResolvePointID(binding);

            if (binding.PlacementPoint == null)
            {
                Debug.Log($"PLACEMENT_POINT_MISSING: {pointID}");
                continue;
            }

            if (!TryReadToolCardID(binding.PlacementPoint, out string toolCardID))
            {
                Debug.Log($"PLACED_TOOL_ID_UNREADABLE: {pointID}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(toolCardID))
            {
                continue;
            }

            if (toolIconCatalog == null || !toolIconCatalog.TryGetIcon(toolCardID, out Sprite icon))
            {
                Debug.Log($"CARD_ICON_MISSING: {toolCardID}");
                continue;
            }

            Transform target = binding.IconTarget != null ? binding.IconTarget : binding.PlacementPoint.transform;
            Vector3 endPosition = target.position + worldOffset;
            Vector3 startPosition = endPosition + Vector3.up * fallHeight;

            GameObject iconObject = new GameObject($"PlacedIcon_{pointID}_{toolCardID}", typeof(SpriteRenderer), typeof(PlacedToolIconView));
            iconObject.transform.SetParent(iconWorldParent != null ? iconWorldParent : transform, true);

            spawnedIcons.Add(iconObject);

            PlacedToolIconView view = iconObject.GetComponent<PlacedToolIconView>();
            view.Play(icon, startPosition, endPosition, fallDuration, iconSize, sortingLayerName, sortingOrder);

            if (hidePlacedCardUIAfterSpawn && binding.PlacedCardUIObjectToHide != null)
            {
                binding.PlacedCardUIObjectToHide.SetActive(false);
            }
        }
    }

    public void ClearSpawnedIcons()
    {
        for (int i = spawnedIcons.Count - 1; i >= 0; i--)
        {
            GameObject iconObject = spawnedIcons[i];

            if (iconObject != null)
            {
                Destroy(iconObject);
            }
        }

        spawnedIcons.Clear();
    }

    private static bool TryReadToolCardID(PlacementPoint placementPoint, out string toolCardID)
    {
        toolCardID = null;

        if (placementPoint == null)
        {
            return false;
        }

        toolCardID = placementPoint.storedToolCardID;
        return true;
    }

    private static string ResolvePointID(PlacementIconBinding binding)
    {
        if (binding == null)
        {
            return "(null binding)";
        }

        if (!string.IsNullOrWhiteSpace(binding.PlacePointID))
        {
            return binding.PlacePointID;
        }

        if (binding.PlacementPoint != null && !string.IsNullOrWhiteSpace(binding.PlacementPoint.placePointID))
        {
            return binding.PlacementPoint.placePointID;
        }

        return "(unknown point)";
    }
}

[Serializable]
public class PlacementIconBinding
{
    [SerializeField] private string placePointID;
    [SerializeField] private PlacementPoint placementPoint;
    [SerializeField] private Transform iconTarget;
    [SerializeField] private GameObject placedCardUIObjectToHide;

    public string PlacePointID => placePointID;
    public PlacementPoint PlacementPoint => placementPoint;
    public Transform IconTarget => iconTarget;
    public GameObject PlacedCardUIObjectToHide => placedCardUIObjectToHide;
}
