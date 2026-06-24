using UnityEngine;

public class PlacementTriggerZone : MonoBehaviour
{
    public string nodeID = "Node1";
    public string placePointID = "N1_P1";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("StoryActor"))
        {
            return;
        }

        Debug.Log($"Triggered: {nodeID} / {placePointID}");
    }
}