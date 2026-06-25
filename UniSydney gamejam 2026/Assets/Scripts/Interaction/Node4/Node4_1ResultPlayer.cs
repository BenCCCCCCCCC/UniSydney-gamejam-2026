using System.Collections;
using FairyTale.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public enum Node4_1EffectType
{
    InvalidPlacement,
    PoisonApple,    // T_POISON_APPLE → 成功结局：Snow 晕倒，被放入棺材
}

/// <summary>
/// Node4_1 触发结果控制器（占位模板，基于 Node2_1ResultPlayer）。
/// 在 Inspector 中配置旁白文字；在 GetEffectType / PlayResult 中实现具体道具判定。
/// </summary>
public class Node4_1ResultPlayer : MonoBehaviour
{
    [Header("场景引用")]
    public StoryActorAutoMove actorAutoMove;
    [SerializeField] private string retrySceneName = "Node4_1";
    [SerializeField] private string nextSceneName = "Node5";
    [SerializeField] private bool useAsyncLoad = false;
    [SerializeField] private bool disableNextButtonAfterClick = true;

    [Header("对话设置")]
    [SerializeField] private float messageDuration = 3f;
    [SerializeField] [TextArea(2, 4)] private string msgInvalid = "Nothing seems to happen. Snow continues walking.";
    [SerializeField] [TextArea(2, 4)] private string msgPoisonApple = "Snow took a bite of the apple... and slowly collapsed to the ground.";

    [Header("结局面板 — 成功")]
    [SerializeField] [TextArea(2, 4)] private string endingPoisonApple =
        "The dwarves found Snow White lying still and, believing she was dead, lovingly placed her in a glass coffin deep in the forest.\n\nGood Ending";

    private GameObject canvasObject;
    private GameObject dialoguePanelObject;
    private TMP_Text dialogueText;
    private Button nextButton;

    private Coroutine dialogueCoroutine;
    private bool hasEnded;
    private bool isLoadingNextScene;

    private void OnEnable()  => PlacementTriggerZone.OnToolPlaced += HandleToolPlaced;
    private void OnDisable() => PlacementTriggerZone.OnToolPlaced -= HandleToolPlaced;

    private void Start()
    {
        if (actorAutoMove == null)
            actorAutoMove = FindAnyObjectByType<StoryActorAutoMove>();

        BuildRuntimeUI();
        HideDialogue();

        if (actorAutoMove != null)
            actorAutoMove.OnReachedEnd += HandleActorReachedEnd;
    }

    // ── 触发入口 ─────────────────────────────────────────────────────────────

    private void HandleToolPlaced(PlacementPoint point)
    {
        if (point == null || point.gameObject.scene != gameObject.scene) return;
        if (hasEnded) return;
        PlayResult(point);
    }

    public void PlayResult(PlacementPoint point)
    {
        if (point == null) return;
        if (actorAutoMove == null)
            actorAutoMove = FindAnyObjectByType<StoryActorAutoMove>();

        Node4_1EffectType effectType = GetEffectType(point.placePointID, point.storedToolCardID);
        string message = GetMessage(effectType);

        if (dialogueCoroutine != null) StopCoroutine(dialogueCoroutine);

        if (effectType == Node4_1EffectType.PoisonApple)
            dialogueCoroutine = StartCoroutine(ShowMessageThenEnd(message, endingPoisonApple));
        else
            dialogueCoroutine = StartCoroutine(ShowMessageThenContinue(message));
    }

    // ── 效果分类 ──────────────────────────────────────────────────────────────

    private Node4_1EffectType GetEffectType(string placePointID, string toolCardID)
    {
        if (placePointID == "N4_P1" && toolCardID == "T_POISON_APPLE")
            return Node4_1EffectType.PoisonApple;
        return Node4_1EffectType.InvalidPlacement;
    }

    private string GetMessage(Node4_1EffectType effectType)
    {
        return effectType switch
        {
            Node4_1EffectType.PoisonApple => msgPoisonApple,
            _ => msgInvalid
        };
    }

    // ── 协程：旁白 → 结局 ────────────────────────────────────────────────────

    private IEnumerator ShowMessageThenEnd(string message, string ending)
    {
        hasEnded = true;
        if (actorAutoMove != null) actorAutoMove.PauseMove();

        ShowDialogue(message);
        yield return new WaitForSeconds(messageDuration);
        HideDialogue();

        if (actorAutoMove != null) actorAutoMove.StopMove();
        ShowEndingPanel(ending);
    }

    // ── 协程：旁白 → 恢复行走 ────────────────────────────────────────────────

    private IEnumerator ShowMessageThenContinue(string message)
    {
        if (actorAutoMove != null) actorAutoMove.PauseMove();

        ShowDialogue(message);
        yield return new WaitForSeconds(messageDuration);
        HideDialogue();

        if (actorAutoMove != null) actorAutoMove.ResumeMove();
    }

    // ── Actor 到达终点 ────────────────────────────────────────────────────────

    private void HandleActorReachedEnd()
    {
        if (actorAutoMove != null) actorAutoMove.OnReachedEnd -= HandleActorReachedEnd;
        if (hasEnded) return;
        // TODO: 配置转场目标场景名
        SceneTransitionManager.Instance?.PanToNextScene("Node4_2");
    }

    // ── 结局面板 ─────────────────────────────────────────────────────────────

    private void ShowEndingPanel(string ending)
    {
        BuildRuntimeUI();
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();

        GameObject panel = new GameObject("EndingPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasRect, false);
        panel.GetComponent<Image>().color = new Color(0.04f, 0.045f, 0.06f, 0.96f);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot     = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(900f, 420f);
        panelRect.anchoredPosition = Vector2.zero;

        CreateText("EndingText", panel.transform,
            ending,
            new Vector2(0.5f, 0.66f), new Vector2(780f, 190f), 38f);

        Button retryBtn = CreateButton("RetryButton", panel.transform,
            "Try Another Way", new Vector2(0.37f, 0.22f), new Vector2(260f, 76f));
        retryBtn.onClick.AddListener(RetryNode4_1);

        Button nextBtn = CreateButton("NextButton", panel.transform,
            "Next Level", new Vector2(0.67f, 0.22f), new Vector2(220f, 76f));
        nextButton = nextBtn;
        nextBtn.onClick.AddListener(HandleNextLevel);
    }

    private void RetryNode4_1()
    {
        GameSessionData.CurrentNodeID = "Node4";
        GameSessionData.CurrentNodeSceneName = retrySceneName;
        GameSessionData.CurrentPhase = GameFlowPhase.Briefing;
        GameSessionData.ToolCardIDs.Clear();
        LoadScene(retrySceneName);
    }

    private void HandleNextLevel()
    {
        if (isLoadingNextScene)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogWarning("Node4_1ResultPlayer: nextSceneName is empty.");
            return;
        }

        isLoadingNextScene = true;

        if (disableNextButtonAfterClick && nextButton != null)
        {
            nextButton.interactable = false;
        }

        GameSessionData.CurrentNodeSceneName = nextSceneName;
        GameSessionData.CurrentPhase = GameFlowPhase.Briefing;

        LoadScene(nextSceneName, useAsyncLoad);
    }

    private void LoadScene(string sceneName)
    {
        LoadScene(sceneName, false);
    }

    private void LoadScene(string sceneName, bool asyncLoad)
    {
#if UNITY_EDITOR
        string path = $"Assets/Scenes/{sceneName}.unity";
        if (SceneUtility.GetBuildIndexByScenePath(path) < 0)
        {
            EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
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

    // ── 运行时 UI ─────────────────────────────────────────────────────────────

    private void BuildRuntimeUI()
    {
        EnsureEventSystem();
        if (canvasObject != null) return;

        canvasObject = new GameObject(
            "Node4_1ResultCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();

        dialoguePanelObject = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image));
        dialoguePanelObject.transform.SetParent(canvasRect, false);
        dialoguePanelObject.GetComponent<Image>().color = new Color(0.05f, 0.055f, 0.07f, 0.92f);

        RectTransform panelRect = dialoguePanelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.16f);
        panelRect.anchorMax = new Vector2(0.5f, 0.16f);
        panelRect.pivot     = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(980f, 130f);
        panelRect.anchoredPosition = Vector2.zero;

        GameObject textObject = new GameObject("DialogueText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(dialoguePanelObject.transform, false);

        dialogueText = textObject.GetComponent<TMP_Text>();
        dialogueText.alignment = TextAlignmentOptions.Center;
        dialogueText.color = Color.white;
        dialogueText.fontSize = 34f;
        dialogueText.textWrappingMode = TextWrappingModes.Normal;
        dialogueText.enableAutoSizing = true;
        dialogueText.fontSizeMin = 18f;
        dialogueText.fontSizeMax = 34f;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(40f, 20f);
        textRect.offsetMax = new Vector2(-40f, -20f);
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
            dialoguePanelObject.SetActive(false);
    }

    private TMP_Text CreateText(string name, Transform parent, string text,
        Vector2 anchor, Vector2 size, float fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TMP_Text tmp = go.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontSize = fontSize;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 18f;
        tmp.fontSizeMax = fontSize;
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = anchor; r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = size; r.anchoredPosition = Vector2.zero;
        return tmp;
    }

    private Button CreateButton(string name, Transform parent, string label,
        Vector2 anchor, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.18f, 0.35f, 0.52f, 0.98f);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = anchor; r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = size; r.anchoredPosition = Vector2.zero;
        TMP_Text txt = CreateText("Label", go.transform, label,
            new Vector2(0.5f, 0.5f), size, 28f);
        txt.fontStyle = FontStyles.Bold;
        return go.GetComponent<Button>();
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        DontDestroyOnLoad(go);
    }
}
