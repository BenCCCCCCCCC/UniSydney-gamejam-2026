using UnityEngine;

// Temporary game jam bridge: click a PlacementPoint to assign the active tool.
public class PlacementPointClickBridge : MonoBehaviour
{
    [SerializeField] private PlacementPoint placementPoint;

    public PlacementPoint PlacementPoint => placementPoint;

    private void Awake()
    {
        if (placementPoint == null)
        {
            placementPoint = GetComponent<PlacementPoint>();
        }
    }

    public void SetPlacementPoint(PlacementPoint point)
    {
        placementPoint = point;
    }

    private void OnMouseDown()
    {
        NodeToolHandController.TryPlaceActiveTool(placementPoint);
    }
}
