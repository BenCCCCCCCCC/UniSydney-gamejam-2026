using UnityEngine;

public class PlacementTriggerZone : MonoBehaviour
{
    public PlacementPoint placementPoint;
    public Node1ResultPlayer resultPlayer;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered)
        {
            return;
        }

        if (!other.CompareTag("StoryActor"))
        {
            return;
        }

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
        {
            resultPlayer.PlayResult(placementPoint);
        }
    }
}