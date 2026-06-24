using System.Collections;
using UnityEngine;

public class Node1ResultPlayer : MonoBehaviour
{
    [Header("Scene References")]
    public StoryActorAutoMove storyActor;
    public Transform queenPlaceholder;

    [Header("Result Settings")]
    public float resultDuration = 1.2f;

    public void PlayResult(PlacementPoint point)
    {
        if (point == null)
        {
            Debug.LogWarning("Node1ResultPlayer: point is null.");
            return;
        }

        StartCoroutine(PlayResultRoutine(point));
    }

    private IEnumerator PlayResultRoutine(PlacementPoint point)
    {
        if (storyActor != null)
        {
            storyActor.PauseMove();
        }

        string key = $"{point.nodeID}_{point.placePointID}_{point.storedToolCardID}";

        switch (key)
        {
            case "Node1_N1_P1_T_SPOTLIGHT_MIRROR":
                Debug.Log("Result: 聚光魔镜触发，王后注意到白雪更美。");
                yield return ShakeQueen();
                break;

            case "Node1_N1_P2_T_BROADCAST_BIRD":
                Debug.Log("Result: 广播鸟触发，窗外开始广播白雪更美。");
                yield return ShakeQueen();
                break;

            case "Node1_N1_P3_T_BOUNCY_CROWN":
                Debug.Log("Result: 弹跳王冠触发，王后觉得主角地位被抢。");
                yield return ShakeQueen();
                break;

            default:
                Debug.Log($"Result: {key} 没有特殊结果，播放中性效果。");
                yield return new WaitForSeconds(resultDuration);
                break;
        }

        if (storyActor != null)
        {
            storyActor.ResumeMove();
        }
    }

    private IEnumerator ShakeQueen()
    {
        if (queenPlaceholder == null)
        {
            yield return new WaitForSeconds(resultDuration);
            yield break;
        }

        Vector3 originalPosition = queenPlaceholder.position;
        float timer = 0f;

        while (timer < resultDuration)
        {
            float offsetX = Mathf.Sin(timer * 40f) * 0.08f;
            queenPlaceholder.position = originalPosition + new Vector3(offsetX, 0f, 0f);

            timer += Time.deltaTime;
            yield return null;
        }

        queenPlaceholder.position = originalPosition;
    }
}