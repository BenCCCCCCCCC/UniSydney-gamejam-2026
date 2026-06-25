using UnityEngine;

/// <summary>
/// 放置触发区：角色走入时广播 OnToolPlaced 事件。
/// 各节点的 ResultPlayer 自行订阅该事件并按场景过滤，
/// 无需在此处添加任何节点特定字段——共享文件永远不需要再改。
/// </summary>
public class PlacementTriggerZone : MonoBehaviour
{
    [Header("Placement")]
    public PlacementPoint placementPoint;

    [Header("Debug")]
    [SerializeField] private bool triggerOnlyOnce = true;

    // 任何节点的 ResultPlayer 订阅此事件即可接收触发通知
    public static event System.Action<PlacementPoint> OnToolPlaced;

    private bool hasTriggered;

    public void ResetTrigger() => hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnlyOnce && hasTriggered) return;
        if (!other.CompareTag("StoryActor")) return;

        hasTriggered = true;

        if (placementPoint == null)
        {
            Debug.LogWarning($"{gameObject.name}: PlacementPoint is missing.");
            return;
        }

        string toolCardID = string.IsNullOrWhiteSpace(placementPoint.storedToolCardID)
            ? "(empty)" : placementPoint.storedToolCardID;
        Debug.Log($"TRIGGER_HIT: {placementPoint.nodeID} / {placementPoint.placePointID} / ToolCardID = {toolCardID}");

        OnToolPlaced?.Invoke(placementPoint);
    }
}
