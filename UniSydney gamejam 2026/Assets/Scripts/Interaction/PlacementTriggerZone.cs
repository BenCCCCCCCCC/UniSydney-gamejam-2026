using UnityEngine;

public class PlacementTriggerZone : MonoBehaviour
{
    [Header("Placement")]
    public PlacementPoint placementPoint;

    [Header("Optional Result Player")]
    public Node1ResultPlayer resultPlayer;

    [Header("Debug")]
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool hasTriggered;

    public void ResetTrigger()
    {
        hasTriggered = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnlyOnce && hasTriggered)
        {
            return;

        if (!other.CompareTag("StoryActor"))
            return;

        hasTriggered = true;

        if (placementPoint == null)
        {
            Debug.LogWarning($"{gameObject.name}: PlacementPoint is missing.");
            return;
        }

        string nodeID = placementPoint.nodeID;
        string placePointID = placementPoint.placePointID;
        string toolCardID = string.IsNullOrWhiteSpace(placementPoint.storedToolCardID)
            ? "(empty)"
            : placementPoint.storedToolCardID;

        Debug.Log($"TRIGGER_HIT: {nodeID} / {placePointID} / ToolCardID = {toolCardID}");

        if (nodeID == "Node3")
        {
            Debug.Log($"NODE3_TRIGGER_CARD: {placePointID} touched, card = {toolCardID}");
        }

        if (resultPlayer != null)
            resultPlayer.PlayResult(placementPoint);
        else if (node2_1ResultPlayer != null)
            node2_1ResultPlayer.PlayResult(placementPoint);
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}