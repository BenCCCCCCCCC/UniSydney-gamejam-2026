using UnityEngine;
using UnityEngine.EventSystems;

public class Node3CentralCardSlot : MonoBehaviour, IDropHandler
{
    private Node3PlacementPlayController controller;
    private int slotIndex;

    public void Setup(Node3PlacementPlayController owner, int index)
    {
        controller = owner;
        slotIndex = index;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
        {
            return;
        }

        Node3CentralToolCardDragItem card = eventData.pointerDrag.GetComponent<Node3CentralToolCardDragItem>();

        if (card == null)
        {
            return;
        }

        controller.TryPlaceCardInSlot(card, slotIndex);
    }
}