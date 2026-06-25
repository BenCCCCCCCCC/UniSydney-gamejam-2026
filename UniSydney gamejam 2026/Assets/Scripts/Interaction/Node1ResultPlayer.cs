using System.Collections;
using UnityEngine;

public class Node1ResultPlayer : MonoBehaviour
{
    [Header("Scene References")]
    public StoryActorAutoMove storyActor;
    public Transform queenPlaceholder;

    [Header("Result Settings")]
    public float resultDuration = 1.2f;

    private CardDatabase database;

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

        if (TryGetDatabase(out CardDatabase resultDatabase)
            && resultDatabase.TryGetPlacementResult(
                point.nodeID,
                point.placePointID,
                point.storedToolCardID,
                out PlacementResultRow result))
        {
            Debug.Log($"Result: {result.ResultSummaryCN}");
            Debug.Log($"OutcomeType: {result.OutcomeType}, NextState: {result.NextState}");
            yield return ShakeQueen();
        }
        else
        {
            yield return PlayLegacyResult(point);
        }

        if (storyActor != null)
        {
            storyActor.ResumeMove();
        }
    }

    private IEnumerator PlayLegacyResult(PlacementPoint point)
    {
        string key = $"{point.nodeID}_{point.placePointID}_{point.storedToolCardID}";

        switch (key)
        {
            // T_SPOTLIGHT_MIRROR is the old ID. T_MAGIC_MIRROR is the current JSON ID.
            case "Node1_N1_P1_T_SPOTLIGHT_MIRROR":
            case "Node1_N1_P1_T_MAGIC_MIRROR":
                Debug.Log("Result: Magic Mirror triggered; queen notices Snow White is more beautiful.");
                yield return ShakeQueen();
                break;

            case "Node1_N1_P2_T_BROADCAST_BIRD":
                Debug.Log("Result: Broadcast Bird triggered; the news spreads from the window.");
                yield return ShakeQueen();
                break;

            case "Node1_N1_P3_T_BOUNCY_CROWN":
                Debug.Log("Result: Bouncy Crown triggered; queen feels her main-character status being stolen.");
                yield return ShakeQueen();
                break;

            default:
                Debug.Log($"Result: {key} has no special result; playing neutral effect.");
                yield return new WaitForSeconds(resultDuration);
                break;
        }
    }

    private bool TryGetDatabase(out CardDatabase resultDatabase)
    {
        if (database == null)
        {
            database = FindAnyObjectByType<CardDatabase>();
        }

        if (database == null)
        {
            GameObject databaseObject = new GameObject("RuntimeCardDatabase");
            database = databaseObject.AddComponent<CardDatabase>();
        }

        resultDatabase = database;
        return resultDatabase != null && resultDatabase.Data != null;
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
