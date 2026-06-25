using UnityEngine;

public class PlacementTriggerZone : MonoBehaviour
{
    [Header("Placement")]
    public PlacementPoint placementPoint;

    [Header("Optional Result Players")]
    public Node1ResultPlayer node1ResultPlayer;
    public Node3ResultPlayer node3ResultPlayer;
    public Node5ResultPlayer node5ResultPlayer;

    [Header("Debug")]
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool hasTriggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("StoryActor"))
        {
            return;
        }

        // 只有点击 Play、进入 AutoPlay 阶段后，TriggerZone 才允许触发。
        // 这样可以避免角色开场站在 Trigger 里时提前触发空卡结果。
        if (GameSessionData.CurrentPhase != GameFlowPhase.AutoPlay)
        {
            Debug.Log($"TRIGGER_IGNORED_NOT_AUTOPLAY: {gameObject.name}, phase = {GameSessionData.CurrentPhase}");
            return;
        }

        if (triggerOnlyOnce && hasTriggered)
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

        if (nodeID == "Node5")
        {
            if (node5ResultPlayer == null)
            {
                node5ResultPlayer = FindAnyObjectByType<Node5ResultPlayer>();
            }

            if (node5ResultPlayer != null)
            {
                node5ResultPlayer.PlayResult(placementPoint);
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: Node5ResultPlayer is missing.");
            }

            return;
        }

        if (nodeID == "Node1")
        {
            if (node1ResultPlayer == null)
            {
                node1ResultPlayer = FindAnyObjectByType<Node1ResultPlayer>();
            }

            if (node1ResultPlayer != null)
            {
                node1ResultPlayer.PlayResult(placementPoint);
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: Node1ResultPlayer is missing.");
            }

            return;
        }

        Debug.LogWarning($"{gameObject.name}: no result player configured for node {nodeID}.");
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
