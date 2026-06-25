using UnityEngine;
using UnityEngine.EventSystems;

public class Node3CentralToolCardDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string ToolCardID => toolCardID;
    public int CurrentSlotIndex { get; private set; } = -1;

    private Node3PlacementPlayController controller;
    private string toolCardID;
    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;

    private Transform handParent;
    private int handSiblingIndex;
    private Vector2 handAnchoredPosition;

    private bool wasDroppedIntoSlot;

    public void Setup(Node3PlacementPlayController owner, string cardID, Canvas canvas, RectTransform handPanel)
    {
        controller = owner;
        toolCardID = cardID;
        rootCanvas = canvas;
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        handParent = handPanel;
        handSiblingIndex = transform.GetSiblingIndex();
        handAnchoredPosition = rectTransform.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        wasDroppedIntoSlot = false;

        if (string.IsNullOrWhiteSpace(toolCardID))
        {
            return;
        }

        Debug.Log($"NODE3_DRAG_START: {toolCardID}");

        if (rootCanvas != null)
        {
            transform.SetParent(rootCanvas.transform, true);
        }

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.85f;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!wasDroppedIntoSlot)
        {
            controller.ReturnCardToHand(this);
        }

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }
    }

    public void PlaceInSlot(int slotIndex, RectTransform slotRect)
    {
        CurrentSlotIndex = slotIndex;
        wasDroppedIntoSlot = true;

        transform.SetParent(slotRect, false);

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = slotRect.sizeDelta * 0.86f;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }
    }

    public void ReturnToHand()
    {
        CurrentSlotIndex = -1;
        wasDroppedIntoSlot = false;

        if (handParent != null)
        {
            transform.SetParent(handParent, false);
            transform.SetSiblingIndex(handSiblingIndex);
        }

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = handAnchoredPosition;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
        }

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }
    }
}
