using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class Node1ResultPlayer : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string nodeID = "Node1";
    [SerializeField] private string retrySceneName = "Node1_QueenCastle";
    [SerializeField] private string nextSceneName = "Node2_1_HunterHunt";
    [SerializeField] private StoryActorAutoMove storyActor;

    [Header("Dialogue")]
    [SerializeField] private SceneTextUIController textUI;
    [SerializeField] private float messageDuration = 1.6f;

    private bool queenProvoked;
    private bool hasEnded;
    private CardDatabase cachedDatabase;
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
            textUI.ConfigureEndingButtons(RetryNode1, RetryNode1, HandleNextLevel);
            textUI.HideDialogue();
            textUI.HideEnding();
        }
        else
        {
            Debug.LogWarning("Node1ResultPlayer: SceneTextUIController is not assigned.");
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
            queenProvoked = true;
        }

        string message = hasPlacementResult
            ? result.ResultSummaryCN
            : "What's that for?";

        Debug.Log($"NODE1_RESULT: {placePointID} / {toolCardID} / {(hasPlacementResult ? result.OutcomeType : "InvalidPlacement")}");

        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
        }

        dialogueCoroutine = StartCoroutine(ShowMessageThenContinue(message));
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

        if (!queenProvoked)
        {
            ShowEndingPanel(false, "The Queen never notices Snow White.");
            return;
        }

        ShowEndingPanel(true, "The Queen decides to send the Hunter after Snow White.");
    }

    private void ShowDialogue(string message)
    {
        if (textUI == null)
        {
            Debug.LogWarning("Node1ResultPlayer: cannot show dialogue because SceneTextUIController is not assigned.");
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
            Debug.LogWarning("Node1ResultPlayer: cannot show ending because SceneTextUIController is not assigned.");
            return;
        }

        string title = success ? "Success" : "Failed";
        textUI.ShowEnding(title, message, success);
    }

    private void RetryNode1()
    {
        GameSessionData.CurrentNodeID = nodeID;
        GameSessionData.CurrentNodeSceneName = retrySceneName;
        GameSessionData.CurrentPhase = GameFlowPhase.Briefing;
        GameSessionData.ToolCardIDs.Clear();

        LoadSceneByName(retrySceneName);
    }

    private void HandleNextLevel()
    {
        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogWarning("Node1ResultPlayer: nextSceneName is empty.");
            return;
        }

        GameSessionData.CurrentNodeSceneName = nextSceneName;
        GameSessionData.CurrentPhase = GameFlowPhase.Briefing;

        LoadSceneByName(nextSceneName);
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
            Debug.LogWarning("Node1ResultPlayer: CardDatabase is unavailable.");
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
        Debug.Log($"Node1ResultPlayer loading scene: {sceneName}");

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
