using UnityEngine;

public class PlacementPoint : MonoBehaviour
{
    [Header("Point Identity")]
    public string nodeID = "Node1";
    public string placePointID = "N1_P1";

    [Header("Temporary Tool Data")]
    public string storedToolCardID = "";

    public bool HasTool()
    {
        return !string.IsNullOrEmpty(storedToolCardID);
    }
}