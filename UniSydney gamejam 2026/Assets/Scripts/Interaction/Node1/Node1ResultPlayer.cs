using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public enum Node1EffectType
{
    OutsideRumor,
    CrownJealousy,
    MirrorReveal,
    InvalidPlacement
}

public class Node1ResultPlayer : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string nodeID = "Node1";
    [SerializeField] private string retrySceneName = "Node1_QueenCastle";
    [SerializeField] private StoryActorAutoMove storyActor;

    [Header("Dialogue")]
    [SerializeField] private float messageDuration = 1.6f;

    private readonly HashSet<string> outsideRumorCards = new HashSet<string>
    {
        "T_BROADCAST_BIRD",
        "T_PETAL_PATH",
        "T_GLOWING_FLOWER_PATH"
    };

    private readonly HashSet<string> crownJealousyCards = new HashSet<string>
    {
        "T_BOUNCY_CROWN",
        "T_PAPER_CROWN_DOLL"
    };

    private readonly HashSet<string> mirrorRevealCards = new HashSet<string>
    {
        "T_SPOTLIGHT_MIRROR",
        "T_BEAUTY_RANKING"
    };

    private bool queenProvoked;
    private bool hasEnded;

    private GameObject canvasObject;
    private GameObject dialoguePanelObject;
    private TMP_Text dialogueText;

    private GameObject endingPanelObject;
    private TMP_Text endingText;

    private Coroutine dialogueCoroutine;

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

        BuildRuntimeUI();
        HideDialogue();
        HideEndingPanel();
    }

    public void PlayResult(PlacementPoint point)
    {
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

        Node1EffectType effectType = GetEffectType(placePointID, toolCardID);
        ApplyEffect(effectType);

        string message = GetMessage(effectType);

        Debug.Log($"NODE1_RESULT: {placePointID} / {toolCardID} / {effectType}");

        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
        }

        dialogueCoroutine = StartCoroutine(ShowMessageThenContinue(message));
    }

    private Node1EffectType GetEffectType(string placePointID, string toolCardID)
    {
        if (placePointID == "N1_P1")
        {
            if (outsideRumorCards.Contains(toolCardID))
            {
                return Node1EffectType.OutsideRumor;
            }

            return Node1EffectType.InvalidPlacement;
        }

        if (placePointID == "N1_P2")
        {
            if (crownJealousyCards.Contains(toolCardID))
            {
                return Node1EffectType.CrownJealousy;
            }

            return Node1EffectType.InvalidPlacement;
        }

        if (placePointID == "N1_P3")
        {
            if (mirrorRevealCards.Contains(toolCardID))
            {
                return Node1EffectType.MirrorReveal;
            }

            return Node1EffectType.InvalidPlacement;
        }

        return Node1EffectType.InvalidPlacement;
    }

    private void ApplyEffect(Node1EffectType effectType)
    {
        if (effectType == Node1EffectType.InvalidPlacement)
        {
            return;
        }

        queenProvoked = true;
    }

    private string GetMessage(Node1EffectType effectType)
    {
        if (effectType == Node1EffectType.OutsideRumor)
        {
            return "Everyone is talking about Snow White.";
        }

        if (effectType == Node1EffectType.CrownJealousy)
        {
            return "The Queen feels her place being taken.";
        }

        if (effectType == Node1EffectType.MirrorReveal)
        {
            return "The mirror reveals Snow White's beauty.";
        }

        return "What's that for?";
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

    private void BuildRuntimeUI()
    {
        EnsureEventSystem();

        if (canvasObject != null)
        {
            return;
        }

        canvasObject = new GameObject(
            "Node1ResultCanvas",
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

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();

        dialoguePanelObject = CreatePanel(
            "DialoguePanel",
            canvasRect,
            new Vector2(0.5f, 0.16f),
            new Vector2(980f, 130f),
            new Color(0.05f, 0.055f, 0.07f, 0.92f));

        dialogueText = CreateText(
            "DialogueText",
            dialoguePanelObject.transform,
            "",
            new Vector2(0.5f, 0.5f),
            new Vector2(900f, 90f),
            34f);
    }

    private void ShowDialogue(string message)
    {
        BuildRuntimeUI();

        dialoguePanelObject.SetActive(true);
        dialogueText.text = message;
    }

    private void HideDialogue()
    {
        if (dialoguePanelObject != null)
        {
            dialoguePanelObject.SetActive(false);
        }
    }

    private void ShowEndingPanel(bool success, string message)
    {
        BuildRuntimeUI();

        HideEndingPanel();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();

        endingPanelObject = CreatePanel(
            "EndingPanel",
            canvasRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(900f, 420f),
            new Color(0.04f, 0.045f, 0.06f, 0.96f));

        string title = success ? "Success" : "Failed";
        string body = $"{title}\n\n{message}";

        endingText = CreateText(
            "EndingText",
            endingPanelObject.transform,
            body,
            new Vector2(0.5f, 0.66f),
            new Vector2(780f, 190f),
            38f);

        if (success)
        {
            Button tryAnotherWayButton = CreateButton(
                "TryAnotherWayButton",
                endingPanelObject.transform,
                "Try Another Way",
                new Vector2(0.37f, 0.22f),
                new Vector2(260f, 76f));

            tryAnotherWayButton.onClick.AddListener(RetryNode1);

            Button nextButton = CreateButton(
                "NextLevelButton",
                endingPanelObject.transform,
                "Next Level",
                new Vector2(0.67f, 0.22f),
                new Vector2(220f, 76f));

            nextButton.onClick.AddListener(() =>
            {
                Debug.Log("Node1 Next Level button clicked. Not implemented yet.");
            });
        }
        else
        {
            Button retryButton = CreateButton(
                "RetryButton",
                endingPanelObject.transform,
                "Retry",
                new Vector2(0.5f, 0.22f),
                new Vector2(220f, 76f));

            retryButton.onClick.AddListener(RetryNode1);
        }
    }

    private void HideEndingPanel()
    {
        if (endingPanelObject != null)
        {
            Destroy(endingPanelObject);
            endingPanelObject = null;
        }
    }

    private void RetryNode1()
    {
        GameSessionData.CurrentNodeID = nodeID;
        GameSessionData.CurrentNodeSceneName = retrySceneName;
        GameSessionData.CurrentPhase = GameFlowPhase.Briefing;
        GameSessionData.ToolCardIDs.Clear();

        LoadSceneByName(retrySceneName);
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