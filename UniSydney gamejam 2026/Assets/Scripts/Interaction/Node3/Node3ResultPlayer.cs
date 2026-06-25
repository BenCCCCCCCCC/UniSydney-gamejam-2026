using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class Node3ResultPlayer : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string nodeID = "Node3";
    [SerializeField] private string retrySceneName = "Node3_DwarfHouse";
    [SerializeField] private StoryActorAutoMove storyActor;

    [Header("Dialogue")]
    [SerializeField] private SceneTextUIController textUI;
    [SerializeField] private float messageDuration = 1.6f;

    private bool routeSolved;
    private bool doorSolved;
    private bool hasEnded;
    private CardDatabase cachedDatabase;

    private string routeFailReason = "Snow White is lost.";
    private string doorFailReason = "Snow White couldn't enter the house.";

    private Coroutine dialogueCoroutine;

    private void OnEnable()
    {
        StoryActorAutoMove.ActorReachedEnd += OnActorReachedEnd;
        PlacementTriggerZone.OnToolPlaced += HandleToolPlaced;
    }

    private void OnDisable()
    {
        StoryActorAutoMove.ActorReachedEnd -= OnActorReachedEnd;
        PlacementTriggerZone.OnToolPlaced -= HandleToolPlaced;
    }

    private void HandleToolPlaced(PlacementPoint point)
    {
        if (point == null || point.nodeID != nodeID) return;
        PlayResult(point);
    }

    private void Start()
    {
        if (storyActor == null)
        {
            storyActor = FindAnyObjectByType<StoryActorAutoMove>();
        }

        if (textUI != null)
        {
            textUI.ConfigureEndingButtons(RetryNode3, RetryNode3, HandleNextLevel);
            textUI.HideDialogue();
            textUI.HideEnding();
        }
        else
        {
            Debug.LogWarning("Node3ResultPlayer: SceneTextUIController is not assigned.");
        }
    }

    public void PlayResult(PlacementPoint point)
    {
        if (!IsRunningInRetryScene())
        {
            return;
        }

        if (hasEnded)
        {
            return;
        }

        if (point == null)
        {
            return;
        }

        string placePointID = point.placePointID;
        string toolCardID = string.IsNullOrWhiteSpace(point.storedToolCardID)
            ? "(empty)"
            : point.storedToolCardID;

        bool hasPlacementResult = TryGetPlacementResult(placePointID, toolCardID, out PlacementResultRow result);
        if (hasPlacementResult)
        {
            ApplyEffect(result, placePointID);
        }

        string message = hasPlacementResult
            ? result.ResultSummaryCN
            : "What's that for?";

        Debug.Log($"NODE3_RESULT: {placePointID} / {toolCardID} / {(hasPlacementResult ? result.OutcomeType : "InvalidPlacement")}");

        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
        }

        dialogueCoroutine = StartCoroutine(ShowMessageThenContinue(message));
    }

    private void ApplyEffect(PlacementResultRow result, string placePointID)
    {
        if (result == null)
        {
            return;
        }

        if (result.OutcomeType == "ReachDoor")
        {
            routeSolved = true;
            return;
        }

        if (result.OutcomeType == "Success")
        {
            if (placePointID == "N3_P3")
            {
                doorSolved = true;
            }
            else
            {
                routeSolved = true;
            }

            return;
        }

        if (result.OutcomeType == "Fail")
        {
            if (placePointID == "N3_P1" || placePointID == "N3_P2")
            {
                routeFailReason = result.ResultSummaryCN;
            }

            if (placePointID == "N3_P3")
            {
                doorFailReason = result.ResultSummaryCN;
            }
        }
    }

    private IEnumerator ShowMessageThenContinue(string message)
    {
        if (storyActor != null)
        {
            storyActor.PauseMove();
        }

        ShowDialogue(message);

        yield return new WaitForSeconds(messageDuration);

        HideDialogue();

        if (!hasEnded && storyActor != null)
        {
            storyActor.ResumeMove();
        }
    }

    private void OnActorReachedEnd(StoryActorAutoMove actor)
    {
        if (!IsRunningInRetryScene())
        {
            return;
        }

        if (hasEnded)
        {
            return;
        }

        if (storyActor != null && actor != storyActor)
        {
            return;
        }

        hasEnded = true;

        if (storyActor != null)
        {
            storyActor.PauseMove();
        }

        HideDialogue();

        if (!routeSolved)
        {
            ShowEndingPanel(false, routeFailReason);
            return;
        }

        if (!doorSolved)
        {
            ShowEndingPanel(false, doorFailReason);
            return;
        }

        ShowEndingPanel(true, "Snow White settles down in the forest.");
    }

    private void ShowDialogue(string message)
    {
        if (textUI == null)
        {
            Debug.LogWarning("Node3ResultPlayer: cannot show dialogue because SceneTextUIController is not assigned.");
            return;
        }

        textUI.ShowDialogue(message);
    }

    private void HideDialogue()
    {
        if (textUI != null)
        {
            textUI.HideDialogue();
        }
    }

    private void ShowEndingPanel(bool success, string message)
    {
        if (textUI == null)
        {
            Debug.LogWarning("Node3ResultPlayer: cannot show ending because SceneTextUIController is not assigned.");
            return;
        }

        string title = success ? "Success" : "Failed";
        textUI.ShowEnding(title, message, success);
    }

    private void RetryNode3()
    {
        GameSessionData.CurrentNodeID = nodeID;
        GameSessionData.CurrentNodeSceneName = retrySceneName;
        GameSessionData.CurrentPhase = GameFlowPhase.Briefing;
        GameSessionData.ToolCardIDs.Clear();

        LoadSceneByName(retrySceneName);
    }

    private void HandleNextLevel()
    {
        Debug.Log("Next Level button clicked. Not implemented yet.");
    }

    private bool IsRunningInRetryScene()
    {
        if (string.IsNullOrWhiteSpace(retrySceneName))
        {
            return true;
        }

        return SceneManager.GetActiveScene().name == retrySceneName;
    }

    private bool TryGetPlacementResult(string placePointID, string toolCardID, out PlacementResultRow result)
    {
        result = null;

        if (!TryGetDatabase(out CardDatabase database))
        {
            Debug.LogWarning("Node3ResultPlayer: CardDatabase is unavailable.");
            return false;
        }

        return database.TryGetPlacementResult(nodeID, placePointID, toolCardID, out result);
    }

    private bool TryGetDatabase(out CardDatabase database)
    {
        if (cachedDatabase == null)
        {
            cachedDatabase = FindAnyObjectByType<CardDatabase>();
        }

        if (cachedDatabase == null)
        {
            GameObject databaseObject = new GameObject("RuntimeCardDatabase");
            cachedDatabase = databaseObject.AddComponent<CardDatabase>();
        }

        database = cachedDatabase;
        return database != null && database.Data != null;
    }

    private void LoadSceneByName(string sceneName)
    {
        Debug.Log($"Node3ResultPlayer loading scene: {sceneName}");

#if UNITY_EDITOR
        string scenePath = $"Assets/Scenes/{sceneName}.unity";

        if (SceneUtility.GetBuildIndexByScenePath(scenePath) < 0)
        {
            EditorSceneManager.LoadSceneInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif

        SceneManager.LoadScene(sceneName);
    }
}
