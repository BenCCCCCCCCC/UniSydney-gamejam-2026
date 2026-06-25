using System.Collections;
using UnityEngine;

/// <summary>
/// Node2_2（猎人追公主）演出结果控制器。
/// 按「槽位 + 道具」双维度判定结局：
///
///   N2_P2 槽（第一个槽）：
///     T_HALLUCINATION_MUSHROOM → 特殊结局：迷幻蘑菇令猎人产生幻觉，与野猪"互送"出森林
///     其它道具                 → 通用：道具没什么用，猎人继续追
///
///   N2_P3 槽（第二个槽）：待实现
/// </summary>
public class Node2_2ResultPlayer : MonoBehaviour
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

    public void PlayResult(PlacementPoint point)
    {
        if (point == null)
        {
            Debug.LogWarning("Node2_2ResultPlayer: point is null。");
            return;
        }

        if (hunterActor == null)
            hunterActor = FindAnyObjectByType<StoryActorAutoMove>();

        StartCoroutine(PlayResultRoutine(point));
    }

    private IEnumerator PlayResultRoutine(PlacementPoint point)
    {
        if (hunterActor != null)
            hunterActor.PauseMove();

        // 道具已使用，立即从手牌 UI 中销毁这张卡
        ToolCardDragItem.ConsumeCardOnPoint(point.placePointID);

        string slot = point.placePointID ?? string.Empty;
        string tool = point.storedToolCardID ?? string.Empty;

        switch (slot)
        {
            case "N2_P2":
                yield return StartCoroutine(PlayP2Routine(tool));
                break;

            case "N2_P3":
                // 第二个槽待实现
                Debug.Log($"Node2_2: N2_P3 触发，道具='{tool}'（逻辑待实现）。");
                yield return new WaitForSeconds(0.3f);
                if (hunterActor != null)
                    hunterActor.ResumeMove();
                break;

            default:
                Debug.Log($"Node2_2: 未知槽位='{slot}'，道具='{tool}'，猎人继续。");
                yield return new WaitForSeconds(0.3f);
                if (hunterActor != null)
                    hunterActor.ResumeMove();
                break;
        }
    }

    // ── N2_P2 槽的结局逻辑 ──────────────────────────────────────────────────
    private IEnumerator PlayP2Routine(string tool)
    {
        switch (tool)
        {
            case "T_HALLUCINATION_MUSHROOM":
                // 迷幻蘑菇：猎人产生幻觉，误把野猪当同伴，与野猪一起离开森林
                Debug.Log("Node2_2 [N2_P2 · 特殊结局] 迷幻蘑菇！" +
                          "猎人嗅到孢子后产生幻觉，将冲出的野猪误认为同伴，" +
                          "两人（一人一猪）手拉手离开了森林。公主趁机逃脱。");
                yield return new WaitForSeconds(pauseDuration);
                // TODO: 播放猎人+野猪离场动画
                if (hunterActor != null)
                    hunterActor.StopMove();
                break;

            default:
                // 其它道具：通用"没什么用"反应
                string toolLabel = string.IsNullOrEmpty(tool) ? "（空）" : tool;
                Debug.Log($"Node2_2 [N2_P2 · 通用] 道具「{toolLabel}」放在这里没什么用，猎人无视后继续前进。");
                yield return new WaitForSeconds(pauseDuration);
                if (hunterActor != null)
                    hunterActor.ResumeMove();
                break;
        }
    }
}
