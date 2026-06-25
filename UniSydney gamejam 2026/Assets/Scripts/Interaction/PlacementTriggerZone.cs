using UnityEngine;

public class PlacementTriggerZone : MonoBehaviour
{
    public PlacementPoint placementPoint;

    [Header("Node1")]
    public Node1ResultPlayer resultPlayer;

    [Header("Node2_1")]
    public Node2_1ResultPlayer node2_1ResultPlayer;

    private bool hasTriggered = false;

    public void ResetTrigger()
    {
        hasTriggered = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered)
            return;

        if (!other.CompareTag("StoryActor"))
            return;

        if (placementPoint == null)
        {
            Debug.LogWarning($"{name}: PlacementPoint is not assigned.");
            return;
        }

        hasTriggered = true;

        Debug.Log(
            $"Triggered: {placementPoint.nodeID} / {placementPoint.placePointID} / Tool = {placementPoint.storedToolCardID}"
        );

        if (resultPlayer != null)
            resultPlayer.PlayResult(placementPoint);
        else if (node2_1ResultPlayer != null)
            node2_1ResultPlayer.PlayResult(placementPoint);
    }
}