using System.Collections;
using FairyTale.Core;
using UnityEngine;

/// <summary>
/// Node2_1（猎人森林路）演出结果控制器。
/// 猎人踩中放置槽时触发，根据放置的道具决定走向结局1还是继续进入Node2_2。
/// </summary>
public class Node2_1ResultPlayer : MonoBehaviour
{
    [Header("场景引用")]
    public StoryActorAutoMove hunterActor;

    [Header("结果设置")]
    public float pauseDuration = 1.5f;

    private void OnEnable()  => PlacementTriggerZone.OnToolPlaced += HandleToolPlaced;
    private void OnDisable() => PlacementTriggerZone.OnToolPlaced -= HandleToolPlaced;

    private void HandleToolPlaced(PlacementPoint point)
    {
        if (point == null || point.gameObject.scene != gameObject.scene) return;
        PlayResult(point);
    }

    private void Start()
    {
        if (hunterActor != null)
            hunterActor.OnReachedEnd += HandleActorReachedEnd;
    }

    private void HandleActorReachedEnd()
    {
        // 先取消订阅，绝对防止转场中被反复调用
        if (hunterActor != null)
            hunterActor.OnReachedEnd -= HandleActorReachedEnd;

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.PanToNextScene("Node2_2_HunterHunt");
    }

    public void PlayResult(PlacementPoint point)
    {
        if (point == null)
        {
            Debug.LogWarning("Node2_1ResultPlayer: point is null。");
            return;
        }
        StartCoroutine(PlayResultRoutine(point));
    }

    private IEnumerator PlayResultRoutine(PlacementPoint point)
    {
        if (hunterActor != null)
            hunterActor.PauseMove();

        // 道具已使用，立即从手牌 UI 中销毁这张卡
        ToolCardDragItem.ConsumeCardOnPoint(point.placePointID);

        switch (point.storedToolCardID)
        {
            case "T_HONEY_APPLE":
                // 蜜糖苹果放在路上 → 野猪冲出 → 猎人与野猪扭打滚下山坡（结局1）
                Debug.Log("Node2_1: [结局1] 蜜糖苹果触发！野猪冲出森林，与猎人扭打，二人滚下山坡。");
                yield return new WaitForSeconds(pauseDuration);
                // 猎人停在原地（结局1，不进入Node2_2）
                if (hunterActor != null)
                    hunterActor.StopMove();
                break;

            default:
                // 槽空置或放了其他道具 → 无特殊阻挡，猎人继续前进进入Node2_2
                Debug.Log($"Node2_1: 放置槽内容='{point.storedToolCardID}'，无特殊触发，猎人继续前进→Node2_2。");
                yield return new WaitForSeconds(0.3f);
                if (hunterActor != null)
                    hunterActor.ResumeMove();
                break;
        }
    }
}
