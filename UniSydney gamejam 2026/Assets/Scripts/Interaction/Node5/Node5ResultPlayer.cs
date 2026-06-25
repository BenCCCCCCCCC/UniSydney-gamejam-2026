using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class Node5ResultPlayer : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string nodeID = "Node5";
    [SerializeField] private string retrySceneName = "Node5";
    [SerializeField] private StoryActorAutoMove storyActor;
    [SerializeField] private CardDatabase database;

    [Header("Actors")]
    [SerializeField] private StoryActorAutoMove princeActor;
    [SerializeField] private StoryActorAutoMove dwarfActor;
    [SerializeField] private GameObject princeVisualRoot;
    [SerializeField] private GameObject dwarfVisualRoot;
    [SerializeField] private Transform dwarfStartPoint;
    [SerializeField] private Transform dwarfEndPoint;

    [Header("Prince-Dwarf Join")]
    [SerializeField] private bool dwarfFollowsPrinceWhenNotLost = true;
    [SerializeField] private Transform princeDwarfJoinPoint;
    [SerializeField] private Vector3 princeDwarfJoinOffsetFromDwarf = new Vector3(0.8f, 0f, 0f);
    [SerializeField] private float princeDwarfJoinDistance = 0.15f;
    [SerializeField] private Vector3 dwarfFollowOffsetFromPrince = new Vector3(-0.8f, 0f, 0f);

    [Header("Text UI")]
    [SerializeField] private SceneTextUIController textUI;
    [SerializeField] private Node5TextBank textBank;
    [SerializeField] private bool showTriggerFeedback = true;
    [SerializeField] private float triggerFeedbackDuration = 1.2f;
    [SerializeField] private bool useDynamicFeedbackDuration = true;
    [SerializeField] private float minFeedbackDuration = 1.8f;
    [SerializeField] private float maxFeedbackDuration = 5.5f;
    [SerializeField] private float readingWordsPerMinute = 220f;
    [SerializeField] private float feedbackPaddingSeconds = 0.8f;
    [SerializeField] private float punctuationExtraSeconds = 0.15f;

    private int totalScore;
    private bool princeCalled;
    private bool hasEnded;
    private Coroutine feedbackCoroutine;
    private bool n5P1Resolved;
    private bool princeLost;
    private bool dwarfPhaseStarted;
    private bool waitingForPrinceToReachDwarfJoin;
    private bool dwarfFollowingPrinceVisualOnly;

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
        if (point == null || point.nodeID != nodeID)
        {
            return;
        }

        PlayResult(point);
    }

    private void Start()
    {
        if (princeActor == null)
        {
            princeActor = storyActor;
        }

        if (storyActor == null)
        {
            storyActor = princeActor;
        }

        if (princeActor == null)
        {
            princeActor = FindAnyObjectByType<StoryActorAutoMove>();
            storyActor = princeActor;
        }

        if (princeVisualRoot == null && princeActor != null)
        {
            princeVisualRoot = princeActor.gameObject;
        }

        if (dwarfVisualRoot == null && dwarfActor != null)
        {
            dwarfVisualRoot = dwarfActor.gameObject;
        }

        if (dwarfActor != null && dwarfStartPoint != null && dwarfEndPoint != null)
        {
            dwarfActor.SetMovePath(dwarfStartPoint, dwarfEndPoint);
        }

        if (dwarfVisualRoot != null)
        {
            dwarfVisualRoot.SetActive(true);
        }

        SetDwarfTriggerEnabled(false);

        if (dwarfActor != null)
        {
            dwarfActor.PauseMove();
        }

        SetPrinceTriggerEnabled(true);
        waitingForPrinceToReachDwarfJoin = false;
        dwarfFollowingPrinceVisualOnly = false;

        if (database == null)
        {
            database = FindAnyObjectByType<CardDatabase>();
        }

        if (textBank == null)
        {
            textBank = FindAnyObjectByType<Node5TextBank>();
        }

        if (textUI != null)
        {
            textUI.ConfigureEndingButtons(RetryNode5, RetryNode5, HandleNextLevel);
            textUI.HideDialogue();
            textUI.HideEnding();
        }
        else
        {
            Debug.LogWarning("Node5ResultPlayer: SceneTextUIController is not assigned.");
        }
    }

    private void LateUpdate()
    {
        if (waitingForPrinceToReachDwarfJoin)
        {
            TryStartDwarfVisualFollow();
        }

        if (dwarfFollowingPrinceVisualOnly)
        {
            UpdateDwarfVisualFollowPosition();
        }
    }

    public void PlayResult(PlacementPoint point)
    {
        if (hasEnded || point == null)
        {
            return;
        }

        string placePointID = point.placePointID;

        if (placePointID == "N5_P1")
        {
            if (n5P1Resolved)
            {
                return;
            }
        }
        else if (placePointID == "N5_P2" || placePointID == "N5_P3")
        {
            if (!n5P1Resolved)
            {
                Debug.LogWarning($"NODE5_TRIGGER_IGNORED_BEFORE_N5_P1: {placePointID}");
                return;
            }
        }

        string toolCardID = string.IsNullOrWhiteSpace(point.storedToolCardID)
            ? ""
            : point.storedToolCardID;

        int delta = 0;
        string outcomeType = "InvalidPlacement";
        string summary = "";

        if (!string.IsNullOrWhiteSpace(toolCardID)
            && TryGetPlacementResult(placePointID, toolCardID, out PlacementResultRow result))
        {
            outcomeType = result.OutcomeType;
            summary = result.ResultSummaryCN;
            delta = GetDeltaFromOutcome(outcomeType);
        }
        else if (!string.IsNullOrWhiteSpace(toolCardID)
            && TryGetFallbackDelta(placePointID, toolCardID, out delta))
        {
            outcomeType = GetFallbackOutcomeType(delta);
            summary = "Node5 fallback score rule.";
        }

        totalScore += delta;

        if (placePointID == "N5_P1" && delta > 0)
        {
            princeCalled = true;
        }

        if (placePointID == "N5_P1")
        {
            n5P1Resolved = true;
            princeLost = delta < 0;
        }

        string loggedToolCardID = string.IsNullOrWhiteSpace(toolCardID)
            ? "(empty)"
            : toolCardID;

        Debug.Log($"NODE5_SCORE_RECORD: {placePointID} / {loggedToolCardID} / {outcomeType} / delta = {delta} / total = {totalScore} / {summary}");

        if (showTriggerFeedback)
        {
            if (feedbackCoroutine != null)
            {
                StopCoroutine(feedbackCoroutine);
                feedbackCoroutine = null;
            }

            feedbackCoroutine = StartCoroutine(ShowFeedbackThenHide(
                GetFeedbackMessage(placePointID, delta),
                placePointID == "N5_P1"));
        }
        else if (placePointID == "N5_P1")
        {
            StartDwarfPhase();
        }
    }

    private IEnumerator ShowFeedbackThenHide(string message, bool switchToDwarfAfter)
    {
        if (textUI == null)
        {
            Debug.LogWarning("Node5ResultPlayer: cannot show feedback because SceneTextUIController is not assigned.");
            if (switchToDwarfAfter)
            {
                StartDwarfPhase();
            }

            yield break;
        }

        StoryActorAutoMove activeActor = GetActiveActor();
        if (activeActor != null)
        {
            activeActor.PauseMove();
        }

        textUI.ShowDialogue(message);

        yield return new WaitForSeconds(GetFeedbackDuration(message));

        textUI.HideDialogue();
        feedbackCoroutine = null;

        if (hasEnded)
        {
            yield break;
        }

        if (switchToDwarfAfter)
        {
            StartDwarfPhase();
            yield break;
        }

        activeActor = GetActiveActor();
        if (activeActor != null)
        {
            activeActor.ResumeMove();
        }
    }

    private float GetFeedbackDuration(string message)
    {
        return DialogReadingTimeUtility.GetDuration(
            message,
            useDynamicFeedbackDuration,
            triggerFeedbackDuration,
            minFeedbackDuration,
            maxFeedbackDuration,
            readingWordsPerMinute,
            feedbackPaddingSeconds,
            punctuationExtraSeconds);
    }

    private bool TryGetPlacementResult(string placePointID, string toolCardID, out PlacementResultRow result)
    {
        result = null;

        if (!TryGetDatabase(out CardDatabase activeDatabase))
        {
            Debug.LogWarning("Node5ResultPlayer: CardDatabase is unavailable.");
            return false;
        }

        return activeDatabase.TryGetPlacementResult(nodeID, placePointID, toolCardID, out result);
    }

    private bool TryGetDatabase(out CardDatabase activeDatabase)
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

        activeDatabase = database;
        return activeDatabase != null && activeDatabase.Data != null;
    }

    private int GetDeltaFromOutcome(string outcomeType)
    {
        if (outcomeType == "ScorePlus")
        {
            return 1;
        }

        if (outcomeType == "ScoreMinus")
        {
            return -1;
        }

        return 0;
    }

    private bool TryGetFallbackDelta(string placePointID, string toolCardID, out int delta)
    {
        delta = 0;

        if (placePointID != "N5_P1")
        {
            return false;
        }

        if (toolCardID == "T_PETAL_PATH")
        {
            delta = 1;
            return true;
        }

        if (toolCardID == "T_BLOOMING_PATH")
        {
            delta = 0;
            return true;
        }

        if (toolCardID == "T_FAST_VINES")
        {
            delta = 1;
            return true;
        }

        return false;
    }

    private string GetFallbackOutcomeType(int delta)
    {
        if (delta > 0)
        {
            return "ScorePlus";
        }

        if (delta < 0)
        {
            return "ScoreMinus";
        }

        return "ScoreNeutral";
    }

    private void OnActorReachedEnd(StoryActorAutoMove actor)
    {
        if (hasEnded)
        {
            return;
        }

        StoryActorAutoMove activeActor = GetActiveActor();
        if (activeActor != null && actor != activeActor)
        {
            return;
        }

        hasEnded = true;

        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
            feedbackCoroutine = null;
        }

        if (textUI != null)
        {
            textUI.HideDialogue();
        }

        activeActor = GetActiveActor();
        if (activeActor != null)
        {
            activeActor.PauseMove();
        }

        GameSessionData.CurrentPhase = GameFlowPhase.Result;

        GetEnding(out string title, out string body);
        ShowEndingPanel(title, body);
    }

    private void GetEnding(out string title, out string body)
    {
        if (textBank != null)
        {
            textBank.GetEnding(totalScore, princeCalled, out title, out body);
            return;
        }

        Debug.LogWarning("Node5ResultPlayer: Node5TextBank is not assigned. Using fallback ending text.");

        if (totalScore < 0)
        {
            title = "Bad Ending";
            body = "It seems Snow White has been sealed away forever.";
            return;
        }

        if (totalScore == 0)
        {
            title = "Failed Rescue";
            body = princeCalled
                ? "The prince and the dwarfs cannot rescue Snow White."
                : "The dwarfs cannot rescue Snow White.";
            return;
        }

        if (princeCalled)
        {
            title = "Rescue Ending";
            body = "The prince and the dwarfs rescue Snow White.";
            return;
        }

        title = "Dwarfs Rescue Ending";
        body = "The dwarfs rescue Snow White.";
    }

    private string GetFeedbackMessage(string placePointID, int delta)
    {
        if (textBank != null)
        {
            return textBank.GetFeedbackMessage(placePointID, delta);
        }

        Debug.LogWarning("Node5ResultPlayer: Node5TextBank is not assigned. Using fallback feedback text.");

        if (placePointID == "N5_P1")
        {
            if (delta > 0)
            {
                return "The prince receives help and finds the way forward.";
            }

            if (delta < 0)
            {
                return "The prince gets lost.";
            }

            return "The prince follows a beautiful flower path, but it does not really help.";
        }

        if (placePointID == "N5_P2")
        {
            if (delta > 0)
            {
                return "The dwarfs receive useful help.";
            }

            if (delta < 0)
            {
                return "The dwarfs are thrown into trouble.";
            }

            return "The dwarfs notice something, but it is not enough to change the rescue.";
        }

        if (placePointID == "N5_P3")
        {
            if (delta > 0)
            {
                return "The crystal coffin begins to open.";
            }

            if (delta < 0)
            {
                return "The seal around the crystal coffin grows stronger.";
            }

            return "The crystal coffin reacts faintly, but remains closed.";
        }

        return "The magic has an unclear effect.";
    }

    private void ShowEndingPanel(string title, string body)
    {
        if (textUI == null)
        {
            Debug.LogWarning("Node5ResultPlayer: cannot show ending because SceneTextUIController is not assigned.");
            return;
        }

        textUI.ShowEnding(title, $"{body}\n\nFinal Score: {totalScore}", true);
    }

    private void StartDwarfPhase()
    {
        if (princeLost)
        {
            StartLostPrinceDwarfPhase();
            return;
        }

        ContinuePrinceWithDwarfVisualJoin();
    }

    private void StartLostPrinceDwarfPhase()
    {
        if (dwarfPhaseStarted)
        {
            return;
        }

        dwarfPhaseStarted = true;
        waitingForPrinceToReachDwarfJoin = false;
        dwarfFollowingPrinceVisualOnly = false;
        SetPrinceTriggerEnabled(false);

        if (princeActor != null)
        {
            princeActor.PauseMove();
        }

        if (princeVisualRoot != null)
        {
            princeVisualRoot.SetActive(false);
        }

        if (dwarfActor == null)
        {
            Debug.LogWarning("Node5ResultPlayer: dwarfActor is not assigned.");
            return;
        }

        if (dwarfVisualRoot == null)
        {
            dwarfVisualRoot = dwarfActor.gameObject;
        }

        dwarfVisualRoot.SetActive(true);
        dwarfActor.gameObject.tag = "StoryActor";
        SetDwarfTriggerEnabled(true);

        if (dwarfStartPoint != null && dwarfEndPoint != null)
        {
            dwarfActor.SetMovePath(dwarfStartPoint, dwarfEndPoint);
        }

        StartCoroutine(StartDwarfActorAfterActivation());
    }

    private void ContinuePrinceWithDwarfVisualJoin()
    {
        dwarfPhaseStarted = false;
        waitingForPrinceToReachDwarfJoin = dwarfFollowsPrinceWhenNotLost;
        dwarfFollowingPrinceVisualOnly = false;
        SetPrinceTriggerEnabled(true);
        SetDwarfTriggerEnabled(false);

        if (princeVisualRoot != null)
        {
            princeVisualRoot.SetActive(true);
        }

        if (dwarfVisualRoot == null && dwarfActor != null)
        {
            dwarfVisualRoot = dwarfActor.gameObject;
        }

        if (dwarfVisualRoot != null)
        {
            dwarfVisualRoot.SetActive(true);
        }

        if (dwarfActor != null)
        {
            dwarfActor.PauseMove();
        }

        if (princeActor != null)
        {
            princeActor.ResumeMove();
        }
    }

    private IEnumerator StartDwarfActorAfterActivation()
    {
        yield return null;

        if (hasEnded || dwarfActor == null)
        {
            yield break;
        }

        if (dwarfStartPoint != null)
        {
            dwarfActor.MoveToStart();
        }

        dwarfActor.ResumeMove();
    }

    private StoryActorAutoMove GetActiveActor()
    {
        if (princeLost && dwarfPhaseStarted && dwarfActor != null)
        {
            return dwarfActor;
        }

        return princeActor != null ? princeActor : storyActor;
    }

    private void TryStartDwarfVisualFollow()
    {
        if (princeActor == null)
        {
            waitingForPrinceToReachDwarfJoin = false;
            return;
        }

        if (!TryGetPrinceDwarfJoinTarget(out Vector3 joinTarget))
        {
            return;
        }

        float distanceToJoin = Vector3.Distance(princeActor.transform.position, joinTarget);
        if (distanceToJoin > princeDwarfJoinDistance)
        {
            return;
        }

        waitingForPrinceToReachDwarfJoin = false;
        dwarfFollowingPrinceVisualOnly = true;

        if (dwarfVisualRoot == null && dwarfActor != null)
        {
            dwarfVisualRoot = dwarfActor.gameObject;
        }

        if (dwarfVisualRoot != null)
        {
            dwarfVisualRoot.SetActive(true);
        }

        SetDwarfTriggerEnabled(false);

        if (dwarfActor != null)
        {
            dwarfActor.PauseMove();
        }
    }

    private bool TryGetPrinceDwarfJoinTarget(out Vector3 joinTarget)
    {
        if (princeDwarfJoinPoint != null)
        {
            joinTarget = princeDwarfJoinPoint.position;
            return true;
        }

        if (dwarfVisualRoot != null)
        {
            joinTarget = dwarfVisualRoot.transform.position + princeDwarfJoinOffsetFromDwarf;
            return true;
        }

        if (dwarfActor != null)
        {
            joinTarget = dwarfActor.transform.position + princeDwarfJoinOffsetFromDwarf;
            return true;
        }

        joinTarget = Vector3.zero;
        return false;
    }

    private void UpdateDwarfVisualFollowPosition()
    {
        if (dwarfVisualRoot == null)
        {
            return;
        }

        Transform followTarget = princeVisualRoot != null
            ? princeVisualRoot.transform
            : princeActor != null
                ? princeActor.transform
                : null;

        if (followTarget == null)
        {
            return;
        }

        dwarfVisualRoot.transform.position = followTarget.position + dwarfFollowOffsetFromPrince;
    }

    private void SetPrinceTriggerEnabled(bool enabled)
    {
        if (princeActor == null)
        {
            return;
        }

        Collider2D[] colliders = princeActor.GetComponents<Collider2D>();
        foreach (Collider2D actorCollider in colliders)
        {
            actorCollider.enabled = enabled;
        }
    }

    private void SetDwarfTriggerEnabled(bool enabled)
    {
        if (dwarfActor == null)
        {
            return;
        }

        Collider2D[] colliders = dwarfActor.GetComponents<Collider2D>();
        foreach (Collider2D actorCollider in colliders)
        {
            actorCollider.enabled = enabled;
        }
    }

    private void RetryNode5()
    {
        GameSessionData.CurrentNodeID = nodeID;
        GameSessionData.CurrentNodeSceneName = retrySceneName;
        GameSessionData.CurrentPhase = GameFlowPhase.Briefing;
        GameSessionData.ToolCardIDs.Clear();

        LoadSceneByName(retrySceneName);
    }

    private void HandleNextLevel()
    {
        Debug.Log("NODE5_NEXT_LEVEL_NOT_IMPLEMENTED");
    }

    private void LoadSceneByName(string sceneName)
    {
        Debug.Log($"Node5ResultPlayer loading scene: {sceneName}");

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
