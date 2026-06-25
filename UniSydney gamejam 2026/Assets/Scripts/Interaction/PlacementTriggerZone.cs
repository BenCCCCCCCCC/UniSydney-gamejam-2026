using UnityEngine;

public class PlacementTriggerZone : MonoBehaviour
{
    [Header("Placement")]
    public PlacementPoint placementPoint;

    [Header("Optional Result Players")]
    public Node1ResultPlayer node1ResultPlayer;
    public Node3ResultPlayer node3ResultPlayer;

    [Header("Debug")]
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool hasTriggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnlyOnce && hasTriggered)
        {
            return;
        }

        if (!other.CompareTag("StoryActor"))
        {
            return;
        }

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
            if (node3ResultPlayer == null)
            {
                node3ResultPlayer = FindAnyObjectByType<Node3ResultPlayer>();
            }

            if (node3ResultPlayer != null)
            {
                node3ResultPlayer.PlayResult(placementPoint);
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: Node3ResultPlayer is missing.");
            }

            return;
        }

        if (node1ResultPlayer == null)
        {
            node1ResultPlayer = FindAnyObjectByType<Node1ResultPlayer>();
        }

        if (node1ResultPlayer != null)
        {
            node1ResultPlayer.PlayResult(placementPoint);
        }
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}