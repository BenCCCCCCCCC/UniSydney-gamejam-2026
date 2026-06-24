using UnityEngine;
using UnityEngine.EventSystems;

// Temporary game jam bridge: drag a runtime tool-card UI item onto a Node placement point.
public class ToolCardDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private string toolCardID;
    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector2 originalAnchoredPosition;

    public void Setup(string cardID)
    {
        toolCardID = cardID;
        rectTransform = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();

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
            PlacementPoint placementPoint = FindPlacementPointUnderPointer(eventData);
            if (placementPoint != null)
            {
                placementPoint.SetTool(toolCardID);
                Debug.Log($"Placed {toolCardID} on {placementPoint.placePointID}");
            }
        }

        ReturnToHand();
    }

    private PlacementPoint FindPlacementPointUnderPointer(PointerEventData eventData)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            camera = FindAnyObjectByType<Camera>();
        }

        if (camera == null)
        {
            Debug.LogWarning("ToolCardDragItem: no camera found for placement hit test.");
            return null;
        }

        Vector3 worldPosition = camera.ScreenToWorldPoint(eventData.position);
        Vector2 worldPoint = new Vector2(worldPosition.x, worldPosition.y);
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);

        foreach (Collider2D hit in hits)
        {
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
        }

        return null;
    }

    private void ReturnToHand()
    {
        if (originalParent != null)
        {
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(originalSiblingIndex);
        }

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalAnchoredPosition;
        }

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }
    }
}
