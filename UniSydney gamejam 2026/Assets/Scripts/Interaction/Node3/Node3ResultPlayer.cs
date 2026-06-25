using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class Node3ResultPlayer : MonoBehaviour
{
    private const string LostInForestMessage = "Snow White gets lost in the forest.";

    private enum Node3PostFeedbackAction
    {
        ResumeNormally,
        ShortcutToDoorTrigger,
        OpenDoorThenSucceed,
        FailImmediately
    }

    [Header("Scene")]
    [SerializeField] private string nodeID = "Node3";
    [SerializeField] private string retrySceneName = "Node3_DwarfHouse";
    [SerializeField] private string nextSceneName = "Node4_1";
    [SerializeField] private bool useAsyncLoad = false;
    [SerializeField] private StoryActorAutoMove storyActor;

    [Header("Dialogue")]
    [SerializeField] private SceneTextUIController textUI;
    [SerializeField] private float messageDuration = 1.6f;
    [SerializeField] private bool useDynamicMessageDuration = true;
    [SerializeField] private float minMessageDuration = 1.8f;
    [SerializeField] private float maxMessageDuration = 5.5f;
    [SerializeField] private float readingWordsPerMinute = 220f;
    [SerializeField] private float messagePaddingSeconds = 0.7f;
    [SerializeField] private float punctuationExtraSeconds = 0.15f;

    [Header("Node3 Shortcut")]
    [SerializeField] private Transform shortcutTargetBeforeDoorTrigger;
    [SerializeField] private float shortcutMoveSpeedMultiplier = 3.5f;
    [SerializeField] private float shortcutArriveDistance = 0.05f;
    [SerializeField] private bool useAutomaticDoorTriggerFallback = true;
    [SerializeField] private float minimumShortcutMoveSpeed = 6f;
    [SerializeField] private float shortcutStopDistanceBeforeDoorAlongPath = 0.7f;
    [SerializeField] private bool shortcutToAfterPoint2Trigger = true;
    [SerializeField] private float shortcutDistanceAfterPoint2Trigger = 0.35f;
    [SerializeField] private float fallbackShortShortcutAdvanceDistance = 1.2f;

    [Header("Door Visual")]
    [SerializeField] private SpriteRenderer doorRenderer;
    [SerializeField] private Sprite closedDoorSprite;
    [SerializeField] private Sprite openDoorSprite;
    [SerializeField] private bool setClosedDoorOnStart = true;

    private bool routeSolved;
    private bool doorSolved;
    private bool hasEnded;
    private bool isLoadingNextScene;
    private CardDatabase cachedDatabase;

    private string routeFailReason = "Snow White is lost.";
    private string doorFailReason = "Snow White couldn't enter the house.";

    private Coroutine dialogueCoroutine;
    private bool isShortcutMoving;

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

        if (setClosedDoorOnStart && doorRenderer != null && closedDoorSprite != null)
        {
            doorRenderer.sprite = closedDoorSprite;
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

        if (isShortcutMoving)
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

        Node3PostFeedbackAction postFeedbackAction = GetPostFeedbackAction(placePointID, hasPlacementResult, result);
        if (ShouldShowLostMessageButContinue(placePointID, hasPlacementResult, result))
        {
            message = LostInForestMessage;
            routeFailReason = message;
            postFeedbackAction = Node3PostFeedbackAction.ResumeNormally;
        }
        else if (ShouldRoutePointFailImmediately(placePointID, hasPlacementResult, result))
        {
            message = LostInForestMessage;
            routeFailReason = message;
            postFeedbackAction = Node3PostFeedbackAction.FailImmediately;
        }

        Debug.Log($"NODE3_RESULT: {placePointID} / {toolCardID} / {(hasPlacementResult ? result.OutcomeType : "InvalidPlacement")}");

        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
        }

        dialogueCoroutine = StartCoroutine(ShowMessageThenContinue(message, postFeedbackAction));
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

    private Node3PostFeedbackAction GetPostFeedbackAction(
        string placePointID,
        bool hasPlacementResult,
        PlacementResultRow result)
    {
        if (!hasPlacementResult || result == null)
        {
            return Node3PostFeedbackAction.ResumeNormally;
        }

        if (placePointID == "N3_P1" && result.OutcomeType == "ReachDoor")
        {
            return Node3PostFeedbackAction.ShortcutToDoorTrigger;
        }

        if (placePointID == "N3_P2" && result.OutcomeType == "Fail")
        {
            return Node3PostFeedbackAction.FailImmediately;
        }

        if (placePointID == "N3_P3" && result.OutcomeType == "Success")
        {
            return Node3PostFeedbackAction.OpenDoorThenSucceed;
        }

        if (placePointID == "N3_P3" && result.OutcomeType == "Fail")
        {
            return Node3PostFeedbackAction.FailImmediately;
        }

        return Node3PostFeedbackAction.ResumeNormally;
    }

    private bool ShouldRoutePointFailImmediately(
        string placePointID,
        bool hasPlacementResult,
        PlacementResultRow result)
    {
        if (placePointID != "N3_P2")
        {
            return false;
        }

        return !hasPlacementResult
            || result == null
            || result.OutcomeType != "ReachDoor";
    }

    private bool ShouldShowLostMessageButContinue(
        string placePointID,
        bool hasPlacementResult,
        PlacementResultRow result)
    {
        if (placePointID != "N3_P1")
        {
            return false;
        }

        return !hasPlacementResult
            || result == null
            || result.OutcomeType != "ReachDoor";
    }

    private IEnumerator ShowMessageThenContinue(string message, Node3PostFeedbackAction postFeedbackAction)
    {
        if (storyActor != null)
        {
            storyActor.PauseMove();
        }

        ShowDialogue(message);

        yield return new WaitForSeconds(GetMessageDuration(message));

        HideDialogue();

        dialogueCoroutine = null;

        if (hasEnded)
        {
            yield break;
        }

        if (postFeedbackAction == Node3PostFeedbackAction.ShortcutToDoorTrigger)
        {
            yield return MoveActorToShortcutTarget();
            yield break;
        }

        if (postFeedbackAction == Node3PostFeedbackAction.OpenDoorThenSucceed)
        {
            OpenDoorVisual();
            CompleteNode3(true, "Snow White settles down in the forest.");
            yield break;
        }

        if (postFeedbackAction == Node3PostFeedbackAction.FailImmediately)
        {
            CompleteNode3(false, message);
            yield break;
        }

        if (!hasEnded && storyActor != null)
        {
            storyActor.ResumeMove();
        }
    }

    private float GetMessageDuration(string message)
    {
        return DialogReadingTimeUtility.GetDuration(
            message,
            useDynamicMessageDuration,
            messageDuration,
            minMessageDuration,
            maxMessageDuration,
            readingWordsPerMinute,
            messagePaddingSeconds,
            punctuationExtraSeconds);
    }

    private IEnumerator MoveActorToShortcutTarget()
    {
        if (storyActor == null)
        {
            yield break;
        }

        if (!TryGetShortcutTargetPosition(out Vector3 shortcutTargetPosition))
        {
            Debug.LogWarning("Node3ResultPlayer: no shortcut target or automatic N3_P3 fallback was found. Resuming normal movement.");
            storyActor.ResumeMove();
            yield break;
        }

        isShortcutMoving = true;
        storyActor.PauseMove();

        float baseSpeed = storyActor.MoveSpeed > 0f ? storyActor.MoveSpeed : 2f;
        float shortcutSpeed = Mathf.Max(minimumShortcutMoveSpeed, baseSpeed * shortcutMoveSpeedMultiplier);
        while (Vector3.Distance(storyActor.transform.position, shortcutTargetPosition) > shortcutArriveDistance)
        {
            storyActor.transform.position = Vector3.MoveTowards(
                storyActor.transform.position,
                shortcutTargetPosition,
                shortcutSpeed * Time.deltaTime);

            yield return null;
        }

        storyActor.transform.position = shortcutTargetPosition;
        isShortcutMoving = false;

        if (!hasEnded)
        {
            storyActor.ResumeMove();
        }
    }

    private bool TryGetShortcutTargetPosition(out Vector3 shortcutTargetPosition)
    {
        if (storyActor == null || storyActor.StartPoint == null || storyActor.EndPoint == null)
        {
            Debug.LogWarning("Node3ResultPlayer: storyActor, StartPoint, or EndPoint is missing. Cannot calculate shortcut along actor path.");
            shortcutTargetPosition = Vector3.zero;
            return false;
        }

        Vector3 pathStart = storyActor.StartPoint.position;
        Vector3 pathEnd = storyActor.EndPoint.position;
        Vector3 pathVector = pathEnd - pathStart;
        pathVector.z = 0f;

        float pathLength = pathVector.magnitude;
        if (pathLength <= 0.0001f)
        {
            Debug.LogWarning("Node3ResultPlayer: actor path length is too short. Cannot calculate shortcut along actor path.");
            shortcutTargetPosition = Vector3.zero;
            return false;
        }

        Vector3 pathDirection = pathVector / pathLength;
        float currentDistanceOnPath = Vector3.Dot(storyActor.transform.position - pathStart, pathDirection);
        currentDistanceOnPath = Mathf.Clamp(currentDistanceOnPath, 0f, pathLength);

        float targetDistanceOnPath;
        if (shortcutToAfterPoint2Trigger
            && TryFindPlacementPointOrTriggerPosition("N3_P2", out Vector3 point2Position))
        {
            float point2DistanceOnPath = Vector3.Dot(point2Position - pathStart, pathDirection);
            targetDistanceOnPath = point2DistanceOnPath + shortcutDistanceAfterPoint2Trigger;
        }
        else if (shortcutTargetBeforeDoorTrigger != null)
        {
            targetDistanceOnPath = Vector3.Dot(
                shortcutTargetBeforeDoorTrigger.position - pathStart,
                pathDirection);
        }
        else
        {
            targetDistanceOnPath = currentDistanceOnPath + fallbackShortShortcutAdvanceDistance;
        }

        if (TryFindPlacementPointOrTriggerPosition("N3_P3", out Vector3 point3Position))
        {
            float point3DistanceOnPath = Vector3.Dot(point3Position - pathStart, pathDirection);
            targetDistanceOnPath = Mathf.Min(
                targetDistanceOnPath,
                point3DistanceOnPath - shortcutStopDistanceBeforeDoorAlongPath);
        }

        targetDistanceOnPath = Mathf.Clamp(
            targetDistanceOnPath,
            currentDistanceOnPath + 0.1f,
            pathLength);

        shortcutTargetPosition = pathStart + pathDirection * targetDistanceOnPath;
        shortcutTargetPosition.z = storyActor.transform.position.z;
        return true;
    }

    private bool TryFindPlacementPointOrTriggerPosition(string placePointID, out Vector3 position)
    {
        PlacementTriggerZone[] triggerZones = FindObjectsByType<PlacementTriggerZone>();
        foreach (PlacementTriggerZone triggerZone in triggerZones)
        {
            PlacementPoint point = triggerZone != null ? triggerZone.placementPoint : null;
            if (IsNode3Point(point, placePointID))
            {
                position = triggerZone.transform.position;
                return true;
            }
        }

        PlacementPoint[] placementPoints = FindObjectsByType<PlacementPoint>();
        foreach (PlacementPoint point in placementPoints)
        {
            if (IsNode3Point(point, placePointID))
            {
                position = point.transform.position;
                return true;
            }
        }

        position = Vector3.zero;
        return false;
    }

    private bool IsNode3Point(PlacementPoint point, string placePointID)
    {
        return point != null
            && point.nodeID == nodeID
            && point.placePointID == placePointID;
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

        HideDialogue();

        if (!routeSolved)
        {
            CompleteNode3(false, routeFailReason);
            return;
        }

        if (!doorSolved)
        {
            CompleteNode3(false, doorFailReason);
            return;
        }

        CompleteNode3(true, "Snow White settles down in the forest.");
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

    private void OpenDoorVisual()
    {
        if (doorRenderer == null)
        {
            return;
        }

        if (openDoorSprite == null)
        {
            Debug.LogWarning("Node3ResultPlayer: openDoorSprite is not assigned.");
            return;
        }

        doorRenderer.sprite = openDoorSprite;
    }

    private void CompleteNode3(bool success, string message)
    {
        if (hasEnded)
        {
            return;
        }

        hasEnded = true;
        isShortcutMoving = false;

        if (storyActor != null)
        {
            storyActor.PauseMove();
        }

        HideDialogue();
        ShowEndingPanel(success, message);
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
        if (isLoadingNextScene)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogWarning("Node3ResultPlayer: nextSceneName is empty.");
            return;
        }

        isLoadingNextScene = true;

        GameSessionData.CurrentNodeSceneName = nextSceneName;
        GameSessionData.CurrentPhase = GameFlowPhase.Briefing;

        LoadSceneByName(nextSceneName, useAsyncLoad);
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
        LoadSceneByName(sceneName, false);
    }

    private void LoadSceneByName(string sceneName, bool asyncLoad)
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

        if (asyncLoad)
        {
            SceneManager.LoadSceneAsync(sceneName);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
