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

public enum Node3EffectType
{
    ShortcutMove,
    GuidePath,
    DoorOpen,
    NegativeObstacle,
    InvalidPlacement
}

public class Node3ResultPlayer : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string nodeID = "Node3";
    [SerializeField] private string retrySceneName = "Node3_DwarfHouse";
    [SerializeField] private StoryActorAutoMove storyActor;

    [Header("Dialogue")]
    [SerializeField] private float messageDuration = 1.6f;

    private readonly HashSet<string> shortcutMoveCards = new HashSet<string>
    {
        "T_LEAF_GLIDER",
        "T_FLYING_CLOAK",
        "T_BOUNCY_MUSHROOM",
        "T_LAUNCH_BOARD",
        "T_ELASTIC_ROPE"
    };

    private readonly HashSet<string> guidePathCards = new HashSet<string>
    {
        "T_GUIDE_BIRD",
        "T_TALKING_SIGN",
        "T_GLOWING_SIGN",
        "T_PETAL_PATH",
        "T_GLOWING_FLOWER_PATH",
        "T_BLOOMING_PATH",
        "T_BROADCAST_BIRD"
    };

    private readonly HashSet<string> negativePathCards = new HashSet<string>
    {
        "T_SPINNING_SIGN",
        "T_SPORE_FOG"
    };

    private readonly HashSet<string> doorOpenCards = new HashSet<string>
    {
        "T_FLOWER_KEY",
        "T_BIRD_DOORBELL",
        "T_WIND_CHIME_SIGNAL",
        "T_UNLOCK_SET",
        "T_LOCKPICK_TOOLS",
        "T_WELCOME_CURTAIN",
        "T_RESCUE_BEACON"
    };

    private readonly HashSet<string> negativeDoorCards = new HashSet<string>
    {
        "T_BARRED_DOOR"
    };

    private bool routeSolved;
    private bool doorSolved;
    private bool hasEnded;

    private string routeFailReason = "Snow White is lost.";
    private string doorFailReason = "Snow White couldn't enter the house.";

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

        Node3EffectType effectType = GetEffectType(placePointID, toolCardID);
        ApplyEffect(effectType, placePointID, toolCardID);

        string message = GetMessage(effectType, placePointID, toolCardID);

        Debug.Log($"NODE3_RESULT: {placePointID} / {toolCardID} / {effectType}");

        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
        }

        dialogueCoroutine = StartCoroutine(ShowMessageThenContinue(message));
    }

    private Node3EffectType GetEffectType(string placePointID, string toolCardID)
    {
        if (placePointID == "N3_P1")
        {
            if (shortcutMoveCards.Contains(toolCardID))
            {
                return Node3EffectType.ShortcutMove;
            }

            return Node3EffectType.InvalidPlacement;
        }

        if (placePointID == "N3_P2")
        {
            if (guidePathCards.Contains(toolCardID))
            {
                return Node3EffectType.GuidePath;
            }

            if (negativePathCards.Contains(toolCardID))
            {
                return Node3EffectType.NegativeObstacle;
            }

            return Node3EffectType.InvalidPlacement;
        }

        if (placePointID == "N3_P3")
        {
            if (doorOpenCards.Contains(toolCardID))
            {
                return Node3EffectType.DoorOpen;
            }

            if (negativeDoorCards.Contains(toolCardID))
            {
                return Node3EffectType.NegativeObstacle;
            }

            return Node3EffectType.InvalidPlacement;
        }

        return Node3EffectType.InvalidPlacement;
    }

    private void ApplyEffect(Node3EffectType effectType, string placePointID, string toolCardID)
    {
        if (effectType == Node3EffectType.ShortcutMove)
        {
            routeSolved = true;
            return;
        }

        if (effectType == Node3EffectType.GuidePath)
        {
            routeSolved = true;
            return;
        }

        if (effectType == Node3EffectType.DoorOpen)
        {
            doorSolved = true;
            return;
        }

        if (effectType == Node3EffectType.NegativeObstacle)
        {
            if (placePointID == "N3_P2")
            {
                routeFailReason = "Snow White is lost.";
            }

            if (placePointID == "N3_P3")
            {
                doorFailReason = "The door is blocked. Snow White couldn't enter the house.";
            }
        }
    }

    private string GetMessage(Node3EffectType effectType, string placePointID, string toolCardID)
    {
        if (effectType == Node3EffectType.ShortcutMove)
        {
            return "Snow White is moved to the dwarfs' house.";
        }

        if (effectType == Node3EffectType.GuidePath)
        {
            return "Snow White finds the way.";
        }

        if (effectType == Node3EffectType.DoorOpen)
        {
            return "The door opens. Snow White enters the house.";
        }

        if (effectType == Node3EffectType.NegativeObstacle)
        {
            if (placePointID == "N3_P2")
            {
                return "Snow White is lost.";
            }

            if (placePointID == "N3_P3")
            {
                return "The door is blocked.";
            }

            return "Something went wrong.";
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

    private void BuildRuntimeUI()
    {
        EnsureEventSystem();

        if (canvasObject != null)
        {
            return;
        }

        canvasObject = new GameObject(
            "Node3ResultCanvas",
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

            tryAnotherWayButton.onClick.AddListener(RetryNode3);

            Button nextButton = CreateButton(
                "NextLevelButton",
                endingPanelObject.transform,
                "Next Level",
                new Vector2(0.67f, 0.22f),
                new Vector2(220f, 76f));

            nextButton.onClick.AddListener(() =>
            {
                Debug.Log("Next Level button clicked. Not implemented yet.");
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

            retryButton.onClick.AddListener(RetryNode3);
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

    private void RetryNode3()
    {
        GameSessionData.CurrentNodeID = nodeID;
        GameSessionData.CurrentNodeSceneName = retrySceneName;
        GameSessionData.CurrentPhase = GameFlowPhase.Briefing;
        GameSessionData.ToolCardIDs.Clear();

        LoadSceneByName(retrySceneName);
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