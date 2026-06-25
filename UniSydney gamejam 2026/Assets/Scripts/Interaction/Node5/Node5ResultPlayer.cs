using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    private int totalScore;
    private bool princeCalled;
    private bool hasEnded;

    private GameObject canvasObject;
    private GameObject endingPanelObject;
    private TMP_Text endingText;

    private void OnEnable()
    {
        StoryActorAutoMove.ActorReachedEnd += OnActorReachedEnd;
    }

    private void OnDisable()
    {
        StoryActorAutoMove.ActorReachedEnd -= OnActorReachedEnd;
    }

    private void Start()
    {
        if (storyActor == null)
        {
            storyActor = FindAnyObjectByType<StoryActorAutoMove>();
        }

        if (database == null)
        {
            database = FindAnyObjectByType<CardDatabase>();
        }

        BuildRuntimeUI();
        HideEndingPanel();
    }

    public void PlayResult(PlacementPoint point)
    {
        if (hasEnded || point == null)
        {
            return;
        }

        string placePointID = point.placePointID;
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

        string loggedToolCardID = string.IsNullOrWhiteSpace(toolCardID)
            ? "(empty)"
            : toolCardID;

        Debug.Log($"NODE5_SCORE_RECORD: {placePointID} / {loggedToolCardID} / {outcomeType} / delta = {delta} / total = {totalScore} / {summary}");
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

        if (storyActor != null && actor != storyActor)
        {
            return;
        }

        hasEnded = true;

        if (storyActor != null)
        {
            storyActor.PauseMove();
        }

        GameSessionData.CurrentPhase = GameFlowPhase.Result;

        GetEnding(out string title, out string body);
        ShowEndingPanel(title, body);
    }

    private void GetEnding(out string title, out string body)
    {
        if (totalScore < 0)
        {
            title = "Bad Ending";
            body = "Snow White remains sealed in the crystal coffin forever.";
            return;
        }

        if (totalScore == 0)
        {
            title = "Hundred-Year Ending";
            body = "A hundred years later, Snow White wakes up by herself.";
            return;
        }

        if (princeCalled)
        {
            title = "Prince Ending";
            body = "The prince reaches the crystal coffin, and Snow White wakes up.";
            return;
        }

        title = "Dwarfs Rescue Ending";
        body = "The dwarfs rescue Snow White from the crystal coffin.";
    }

    private void BuildRuntimeUI()
    {
        EnsureEventSystem();

        if (canvasObject != null)
        {
            return;
        }

        canvasObject = new GameObject(
            "Node5ResultCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void ShowEndingPanel(string title, string body)
    {
        BuildRuntimeUI();
        HideEndingPanel();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();

        endingPanelObject = CreatePanel(
            "EndingPanel",
            canvasRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(980f, 460f),
            new Color(0.04f, 0.045f, 0.06f, 0.96f));

        string finalText = $"{title}\n\n{body}\n\nFinal Score: {totalScore}";

        endingText = CreateText(
            "EndingText",
            endingPanelObject.transform,
            finalText,
            new Vector2(0.5f, 0.66f),
            new Vector2(840f, 230f),
            38f);

        Button tryAnotherWayButton = CreateButton(
            "TryAnotherWayButton",
            endingPanelObject.transform,
            "Try Another Way",
            new Vector2(0.36f, 0.2f),
            new Vector2(270f, 76f));

        tryAnotherWayButton.onClick.AddListener(RetryNode5);

        Button nextButton = CreateButton(
            "NextLevelButton",
            endingPanelObject.transform,
            "Next Level",
            new Vector2(0.66f, 0.2f),
            new Vector2(220f, 76f));

        nextButton.onClick.AddListener(() =>
        {
            Debug.Log("NODE5_NEXT_LEVEL_NOT_IMPLEMENTED");
        });
    }

    private void HideEndingPanel()
    {
        if (endingPanelObject != null)
        {
            Destroy(endingPanelObject);
            endingPanelObject = null;
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

    private GameObject CreatePanel(string name, Transform parent, Vector2 anchor, Vector2 size, Color color)
    {
        GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        Image image = panelObject.GetComponent<Image>();
        image.color = color;

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        return panelObject;
    }

    private TMP_Text CreateText(string name, Transform parent, string text, Vector2 anchor, Vector2 size, float fontSize)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TMP_Text tmp = textObject.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontSize = fontSize;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 18f;
        tmp.fontSizeMax = fontSize;

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        return tmp;
    }

    private Button CreateButton(string name, Transform parent, string label, Vector2 anchor, Vector2 size)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.18f, 0.35f, 0.52f, 0.98f);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        Button button = buttonObject.GetComponent<Button>();

        TMP_Text text = CreateText(
            "Label",
            buttonObject.transform,
            label,
            new Vector2(0.5f, 0.5f),
            size,
            28f);

        text.fontStyle = FontStyles.Bold;

        return button;
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));

        eventSystemObject.SetActive(true);
    }
}
