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

public enum Node2_1EffectType
{
    HoneyAppleTrap,     // T_HONEY_BAIT：蜜香诱来野猪，触发成功结局
    InvalidPlacement    // 空槽或与 N2_P1 不匹配的道具
}

/// <summary>
/// Node2_1（猎人森林路）触发结果控制器。
/// T_HONEY_BAIT 放入 N2_P1 → 旁白 → 猎人停止 → 成功结局面板。
/// 其他情况 → 旁白 → 恢复行走 → 到终点转场 Node2_2。
/// </summary>
public class Node2_1ResultPlayer : MonoBehaviour
{
    [Header("场景引用")]
    public StoryActorAutoMove hunterActor;
    [SerializeField] private string retrySceneName = "Node2_1_HunterHunt";
    [SerializeField] private string nextSceneName = "Node3_DwarfHouse";

    [Header("对话设置")]
    [SerializeField] private float messageDuration = 2f;
    [SerializeField] [TextArea(2, 4)] private string msgHoneyApple =
        "The wild boar charged out, following the scent of honey!\nThe hunter was knocked away! Snow narrowly escaped a disaster.";
    [SerializeField] [TextArea(2, 4)] private string msgInvalid = "槽里什么也没有，猎人径直走过。";

    [Header("结局面板")]
    [SerializeField] [TextArea(2, 4)] private string endingMessage =
        "Snow White successfully escaped the forest!";

    private GameObject canvasObject;
    private GameObject dialoguePanelObject;
    private TMP_Text dialogueText;
    private GameObject endingPanelObject;

    private Coroutine dialogueCoroutine;
    private bool hasEnded;

    private void OnEnable()  => PlacementTriggerZone.OnToolPlaced += HandleToolPlaced;
    private void OnDisable() => PlacementTriggerZone.OnToolPlaced -= HandleToolPlaced;

    private void Start()
    {
        if (hunterActor == null)
            hunterActor = FindAnyObjectByType<StoryActorAutoMove>();

        BuildRuntimeUI();
        HideDialogue();

        if (hunterActor != null)
            hunterActor.OnReachedEnd += HandleActorReachedEnd;
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

        Node2_1EffectType effectType = GetEffectType(point.placePointID, point.storedToolCardID);
        string message = GetMessage(effectType);

        if (dialogueCoroutine != null) StopCoroutine(dialogueCoroutine);

        if (effectType == Node2_1EffectType.HoneyAppleTrap)
            dialogueCoroutine = StartCoroutine(ShowMessageThenEnd(message));
        else
            dialogueCoroutine = StartCoroutine(ShowMessageThenContinue(message));
    }

    // ── 效果分类 ─────────────────────────────────────────────────────────────

    private Node2_1EffectType GetEffectType(string placePointID, string toolCardID)
    {
        if (placePointID == "N2_P1" && toolCardID == "T_HONEY_BAIT")
            return Node2_1EffectType.HoneyAppleTrap;

        return Node2_1EffectType.InvalidPlacement;
    }

    private string GetMessage(Node2_1EffectType effectType)
    {
        return effectType switch
        {
            Node2_1EffectType.HoneyAppleTrap => msgHoneyApple,
            _                                => msgInvalid
        };
    }

    // ── 协程：旁白 → 结局 ────────────────────────────────────────────────────

    private IEnumerator ShowMessageThenEnd(string message)
    {
        hasEnded = true;
        if (hunterActor != null) hunterActor.PauseMove();

        ShowDialogue(message);
        yield return new WaitForSeconds(messageDuration);
        HideDialogue();

        if (hunterActor != null) hunterActor.StopMove();
        ShowEndingPanel();
    }

    // ── 协程：旁白 → 恢复行走 ────────────────────────────────────────────────

    private IEnumerator ShowMessageThenContinue(string message)
    {
        if (hunterActor != null) hunterActor.PauseMove();

        ShowDialogue(message);
        yield return new WaitForSeconds(messageDuration);
        HideDialogue();

        if (hunterActor != null) hunterActor.ResumeMove();
    }

    // ── Actor 到达终点 → 转场（未触发成功结局时）────────────────────────────────

    private void HandleActorReachedEnd()
    {
        if (hunterActor != null) hunterActor.OnReachedEnd -= HandleActorReachedEnd;
        if (hasEnded) return;
        SceneTransitionManager.Instance?.PanToNextScene("Node2_2_HunterHunt");
    }

    // ── 结局面板 ─────────────────────────────────────────────────────────────

    private void ShowEndingPanel()
    {
        BuildRuntimeUI();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();

        endingPanelObject = new GameObject("EndingPanel", typeof(RectTransform), typeof(Image));
        endingPanelObject.transform.SetParent(canvasRect, false);
        endingPanelObject.GetComponent<Image>().color = new Color(0.04f, 0.045f, 0.06f, 0.96f);

        RectTransform panelRect = endingPanelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot     = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(900f, 420f);
        panelRect.anchoredPosition = Vector2.zero;

        // 标题 + 结局文字
        CreateText("EndingText", endingPanelObject.transform,
            "Success\n\n" + endingMessage,
            new Vector2(0.5f, 0.66f), new Vector2(780f, 190f), 38f);

        // 重试按钮
        Button retryBtn = CreateButton("RetryButton", endingPanelObject.transform,
            "Try Another Way", new Vector2(0.37f, 0.22f), new Vector2(260f, 76f));
        retryBtn.onClick.AddListener(RetryNode2_1);

        Button nextBtn = CreateButton("NextButton", endingPanelObject.transform,
            "Next Level", new Vector2(0.67f, 0.22f), new Vector2(220f, 76f));
        nextBtn.onClick.AddListener(HandleNextLevel);
    }

    private void RetryNode2_1()
    {
        GameSessionData.CurrentNodeID = "Node2";
        GameSessionData.CurrentNodeSceneName = retrySceneName;
        GameSessionData.CurrentPhase = GameFlowPhase.Briefing;
        GameSessionData.ToolCardIDs.Clear();
        LoadScene(retrySceneName);
    }

    private void HandleNextLevel()
    {
        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogWarning("Node2_1ResultPlayer: nextSceneName is empty.");
            return;
        }

        GameSessionData.CurrentNodeSceneName = nextSceneName;
        GameSessionData.CurrentPhase = GameFlowPhase.Briefing;

        LoadScene(nextSceneName);
    }

    private void LoadScene(string sceneName)
    {
#if UNITY_EDITOR
        string path = $"Assets/Scenes/{sceneName}.unity";
        if (UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath(path) < 0)
        {
            EditorSceneManager.LoadSceneInPlayMode(path,
                new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        SceneManager.LoadScene(sceneName);
    }

    // ── 运行时 UI ─────────────────────────────────────────────────────────────

    private void BuildRuntimeUI()
    {
        EnsureEventSystem();
        if (canvasObject != null) return;

        canvasObject = new GameObject(
            "Node2_1ResultCanvas",
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
