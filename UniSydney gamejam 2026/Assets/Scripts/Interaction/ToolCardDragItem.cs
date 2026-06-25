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

        // 只遍历 PlacementPoint，用 ToolSlotVisual 世界坐标做 snap 目标
        // 避免 TriggerZone 和 PlacementPoint 父节点产生重复候选干扰
        PlacementPoint[] placementPoints = FindObjectsByType<PlacementPoint>();
        foreach (PlacementPoint point in placementPoints)
        {
            if (point == null)
            {
                continue;
            }

            Vector3 snapWorldPos = FindToolSlotVisualPosition(point);
            PlacementCandidate candidate = CreateCandidateFromWorldPoint(camera, point, snapWorldPos);
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

    private static Vector3 FindToolSlotVisualPosition(PlacementPoint point)
    {
        for (int i = 0; i < point.transform.childCount; i++)
        {
            Transform child = point.transform.GetChild(i);
            if (child.name.Contains("ToolSlotVisual"))
            {
                return child.position;
            }
        }
        return point.transform.position;
    }

    private static PlacementCandidate CreateCandidateFromWorldPoint(Camera camera, PlacementPoint point, Vector3 worldPosition)
    {
        return new PlacementCandidate
        {
            point = point,
            collider = null,
            screenRect = PointToScreenRect(camera, worldPosition, 200f)
        };
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
        // 用 ViewportPoint → Screen 坐标，与 GetCardScreenRect() 的 Screen 坐标系保持一致
        // 避免 camera.pixelWidth vs Screen.width 不一致（编辑器缩放/DPI）导致 snap 偏移
        Vector3 viewportPoint = camera.WorldToViewportPoint(worldPosition);
        float screenX = viewportPoint.x * Screen.width;
        float screenY = viewportPoint.y * Screen.height;
        float halfSize = size * 0.5f;
        return Rect.MinMaxRect(screenX - halfSize, screenY - halfSize, screenX + halfSize, screenY + halfSize);
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
