using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Temporary game jam bridge: drag a runtime tool-card UI item onto a Node placement point.
public class ToolCardDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private static readonly Dictionary<string, ToolCardDragItem> PlacedCardsByPointID = new();

    [SerializeField] private float snapScreenRadiusPixels = 260f;

    private string toolCardID;
    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector2 originalAnchoredPosition;
    private Transform handParent;
    private int handSiblingIndex;
    private Vector2 handAnchoredPosition;
    private bool hasHandHome;
    private string placedPointID;

    public void Setup(string cardID)
    {
        toolCardID = cardID;
        rectTransform = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        RememberHandHome();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (string.IsNullOrWhiteSpace(toolCardID))
        {
            return;
        }

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }

        Debug.Log($"DRAG_START: {toolCardID}");
        NodeToolHandController.SetActiveTool(toolCardID);

        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalAnchoredPosition = rectTransform.anchoredPosition;

        if (rootCanvas != null)
        {
            transform.SetParent(rootCanvas.transform, true);
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.85f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (string.IsNullOrWhiteSpace(toolCardID) || rectTransform == null)
        {
            return;
        }

        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!string.IsNullOrWhiteSpace(toolCardID))
        {
            PlacementPoint placementPoint = FindPlacementPointForDroppedCard();
            if (placementPoint == null)
            {
                Debug.LogWarning($"NO_PLACEMENT_POINT: {toolCardID} returned to hand.");
                ReturnToHand();
            }
            else
            {
                Node1PlacementRules.TryPlaceTool(toolCardID, placementPoint);
                placementPoint.SetTool(toolCardID);
                PlaceOnPoint(placementPoint);
            }
        }
    }

    private PlacementPoint FindPlacementPointForDroppedCard()
    {
        Camera camera = FindSceneCamera();

        if (camera == null)
        {
            Debug.LogWarning("ToolCardDragItem: no camera found for placement hit test.");
            return null;
        }

        Rect cardScreenRect = GetCardScreenRect();
        Vector2 cardCenterScreen = cardScreenRect.center;
        Debug.Log($"DRAG_END: {toolCardID}, cardCenterScreen = {FormatScreenPosition(cardCenterScreen)}");

        return FindNearestPlacementPoint(camera, cardCenterScreen, cardScreenRect);
    }

    private PlacementPoint FindNearestPlacementPoint(Camera camera, Vector2 cardCenterScreen, Rect cardScreenRect)
    {
        PlacementCandidate nearest = default;
        PlacementCandidate nearestOverlapping = default;
        bool hasNearest = false;
        bool hasOverlap = false;
        float nearestDistance = float.MaxValue;
        float nearestOverlapDistance = float.MaxValue;

        PlacementTriggerZone[] triggerZones = FindObjectsByType<PlacementTriggerZone>();
        foreach (PlacementTriggerZone zone in triggerZones)
        {
            if (zone == null || zone.placementPoint == null)
            {
                continue;
            }

            Collider2D zoneCollider = zone.GetComponentInChildren<Collider2D>();
            PlacementCandidate candidate = CreateCandidate(camera, zone.placementPoint, zoneCollider, zone.transform.position);
            ConsiderCandidate(cardCenterScreen, cardScreenRect, candidate, ref nearest, ref nearestDistance, ref hasNearest, ref nearestOverlapping, ref nearestOverlapDistance, ref hasOverlap);
        }

        PlacementPoint[] placementPoints = FindObjectsByType<PlacementPoint>();
        foreach (PlacementPoint point in placementPoints)
        {
            if (point == null)
            {
                continue;
            }

            Collider2D pointCollider = point.GetComponentInChildren<Collider2D>();
            PlacementCandidate candidate = CreateCandidate(camera, point, pointCollider, point.transform.position);
            ConsiderCandidate(cardCenterScreen, cardScreenRect, candidate, ref nearest, ref nearestDistance, ref hasNearest, ref nearestOverlapping, ref nearestOverlapDistance, ref hasOverlap);
        }

        PlacementCandidate selected = hasOverlap ? nearestOverlapping : nearest;
        float selectedDistance = hasOverlap ? nearestOverlapDistance : nearestDistance;

        if (hasNearest)
        {
            Debug.Log($"nearest point = {nearest.point.placePointID}, distance = {nearestDistance:0.0}");
            Debug.Log($"point screen rect = {FormatRect(nearest.screenRect)}");
            Debug.Log($"card screen rect = {FormatRect(cardScreenRect)}");
        }

        if (hasOverlap || (hasNearest && selectedDistance <= snapScreenRadiusPixels))
        {
            Debug.Log($"PLACEMENT_SNAP: {selected.point.placePointID}");
            return selected.point;
        }

        return null;
    }

    private void ConsiderCandidate(
        Vector2 cardCenterScreen,
        Rect cardScreenRect,
        PlacementCandidate candidate,
        ref PlacementCandidate nearest,
        ref float nearestDistance,
        ref bool hasNearest,
        ref PlacementCandidate nearestOverlapping,
        ref float nearestOverlapDistance,
        ref bool hasOverlap)
    {
        float distance = Vector2.Distance(cardCenterScreen, candidate.screenRect.center);
        bool overlaps = cardScreenRect.Overlaps(candidate.screenRect, true);

        if (!hasNearest || distance < nearestDistance)
        {
            nearest = candidate;
            nearestDistance = distance;
            hasNearest = true;
        }

        if (overlaps && (!hasOverlap || distance < nearestOverlapDistance))
        {
            nearestOverlapping = candidate;
            nearestOverlapDistance = distance;
            hasOverlap = true;
        }
    }

    private static PlacementCandidate CreateCandidate(Camera camera, PlacementPoint point, Collider2D collider, Vector3 fallbackWorldPosition)
    {
        Vector3 centerWorldPosition = collider != null ? collider.bounds.center : fallbackWorldPosition;
        Rect screenRect = collider != null
            ? BoundsToScreenRect(camera, collider.bounds)
            : PointToScreenRect(camera, centerWorldPosition, 160f);

        return new PlacementCandidate
        {
            point = point,
            collider = collider,
            screenRect = screenRect
        };
    }

    private Rect GetCardScreenRect()
    {
        Vector3[] worldCorners = new Vector3[4];
        rectTransform.GetWorldCorners(worldCorners);

        Camera uiCamera = GetUiCamera();
        Vector2 firstPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCorners[0]);
        float minX = firstPoint.x;
        float maxX = firstPoint.x;
        float minY = firstPoint.y;
        float maxY = firstPoint.y;

        for (int i = 1; i < worldCorners.Length; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCorners[i]);
            minX = Mathf.Min(minX, screenPoint.x);
            maxX = Mathf.Max(maxX, screenPoint.x);
            minY = Mathf.Min(minY, screenPoint.y);
            maxY = Mathf.Max(maxY, screenPoint.y);
        }

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private Camera GetUiCamera()
    {
        if (rootCanvas == null || rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return rootCanvas.worldCamera;
    }

    private static Rect BoundsToScreenRect(Camera camera, Bounds bounds)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector3[] corners =
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, max.z)
        };

        Vector3 firstPoint = camera.WorldToScreenPoint(corners[0]);
        float minX = firstPoint.x;
        float maxX = firstPoint.x;
        float minY = firstPoint.y;
        float maxY = firstPoint.y;

        for (int i = 1; i < corners.Length; i++)
        {
            Vector3 screenPoint = camera.WorldToScreenPoint(corners[i]);
            minX = Mathf.Min(minX, screenPoint.x);
            maxX = Mathf.Max(maxX, screenPoint.x);
            minY = Mathf.Min(minY, screenPoint.y);
            maxY = Mathf.Max(maxY, screenPoint.y);
        }

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private static Rect PointToScreenRect(Camera camera, Vector3 worldPosition, float size)
    {
        Vector3 screenPoint = camera.WorldToScreenPoint(worldPosition);
        float halfSize = size * 0.5f;
        return Rect.MinMaxRect(screenPoint.x - halfSize, screenPoint.y - halfSize, screenPoint.x + halfSize, screenPoint.y + halfSize);
    }

    private static PlacementPoint GetPlacementPointFromCollider(Collider2D hit)
    {
        PlacementTriggerZone triggerZone = hit.GetComponentInParent<PlacementTriggerZone>();
        if (triggerZone != null && triggerZone.placementPoint != null)
        {
            return triggerZone.placementPoint;
        }

        PlacementPoint point = hit.GetComponentInParent<PlacementPoint>();
        if (point != null)
        {
            return point;
        }

        PlacementPointClickBridge bridge = hit.GetComponentInParent<PlacementPointClickBridge>();
        if (bridge != null && bridge.PlacementPoint != null)
        {
            return bridge.PlacementPoint;
        }

        return null;
    }

    private static Camera FindSceneCamera()
    {
        Camera camera = Camera.main;
        if (camera != null)
        {
            return camera;
        }

        return FindAnyObjectByType<Camera>();
    }

    private static string FormatScreenPosition(Vector2 screenPosition)
    {
        return $"<{screenPosition.x:0.0}, {screenPosition.y:0.0}>";
    }

    private static string FormatRect(Rect rect)
    {
        return $"<x:{rect.xMin:0.0}-{rect.xMax:0.0}, y:{rect.yMin:0.0}-{rect.yMax:0.0}>";
    }

    private struct PlacementCandidate
    {
        public PlacementPoint point;
        public Collider2D collider;
        public Rect screenRect;
    }

    private void PlaceOnPoint(PlacementPoint placementPoint)
    {
        RectTransform slot = NodeToolHandController.GetDropSlotForPoint(placementPoint.placePointID);
        if (slot == null || rectTransform == null)
        {
            Debug.LogWarning($"ToolCardDragItem: missing DropSlotUI for {placementPoint.placePointID}, returning to hand.");
            ReturnToHand();
            return;
        }

        if (!string.IsNullOrWhiteSpace(placedPointID) && PlacedCardsByPointID.TryGetValue(placedPointID, out ToolCardDragItem previousSelf) && previousSelf == this)
        {
            PlacedCardsByPointID.Remove(placedPointID);
        }

        if (PlacedCardsByPointID.TryGetValue(placementPoint.placePointID, out ToolCardDragItem oldCard) && oldCard != null && oldCard != this)
        {
            Debug.Log($"REPLACED_PLACED_TOOL: old={oldCard.toolCardID}, new={toolCardID}, point={placementPoint.placePointID}");
            oldCard.ReturnToHand();
        }

        PlacedCardsByPointID[placementPoint.placePointID] = this;
        placedPointID = placementPoint.placePointID;

        transform.SetParent(slot, false);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(150f, 64f);
        transform.localScale = Vector3.one * 0.8f;
        Debug.Log($"PLACE_ANCHOR: point={placementPoint.placePointID}, visual={NodeToolHandController.GetDropSlotNameForPoint(placementPoint.placePointID)}");
        Debug.Log($"CARD_PLACED_VISUAL: {toolCardID} on {placementPoint.placePointID}");

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }
    }

    private void ReturnToHand()
    {
        if (!string.IsNullOrWhiteSpace(placedPointID) && PlacedCardsByPointID.TryGetValue(placedPointID, out ToolCardDragItem placedCard) && placedCard == this)
        {
            PlacedCardsByPointID.Remove(placedPointID);
            placedPointID = string.Empty;
        }

        Transform targetParent = hasHandHome ? handParent : originalParent;
        int targetSiblingIndex = hasHandHome ? handSiblingIndex : originalSiblingIndex;
        Vector2 targetAnchoredPosition = hasHandHome ? handAnchoredPosition : originalAnchoredPosition;

        if (targetParent != null)
        {
            transform.SetParent(targetParent, false);
            transform.SetSiblingIndex(targetSiblingIndex);
        }

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = targetAnchoredPosition;
        }

        transform.localScale = Vector3.one;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }
    }

    private void RememberHandHome()
    {
        if (rectTransform == null)
        {
            return;
        }

        handParent = transform.parent;
        handSiblingIndex = transform.GetSiblingIndex();
        handAnchoredPosition = rectTransform.anchoredPosition;
        hasHandHome = handParent != null;
    }
}
