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

public enum Node2_2EffectType
{
    HallucinationMushroomOnP2,  // N2_P2 + T_HALLUCINATION_MUSHROOM → 成功结局
    LeafCloakOnP3,              // N2_P3 + T_LEAF_CLOAK → 成功结局
    HoneyBaitOnP4,              // N2_P4 + T_HONEY_BAIT → 坏结局
    InvalidPlacement            // 空槽或不匹配道具
}

/// <summary>
/// Node2_2（猎人追公主）触发结果控制器。
/// N2_P2 + T_HALLUCINATION_MUSHROOM → 旁白 → 猎人停止 → 成功结局面板。
/// 其他情况 → 旁白 → 恢复行走。
/// </summary>
public class Node2_2ResultPlayer : MonoBehaviour
{
    [Header("场景引用")]
    public StoryActorAutoMove hunterActor;
    [SerializeField] private string retrySceneName = "Node2_1_HunterHunt";

    [Header("对话设置")]
    [SerializeField] private float messageDuration = 2f;
    [SerializeField] [TextArea(2, 4)] private string msgHallucinationMushroom =
        "The hunter started hallucinating, mistaking the wild boar for the Queen!";
    [SerializeField] [TextArea(2, 4)] private string msgLeafCloak =
        "Disguised by the leaf cloak, the wild boar was mistaken for Snow by the hunter!";
    [SerializeField] [TextArea(2, 4)] private string msgHoneyBait =
        "The wild boar was attracted by the honey scent and charged toward Snow!";
    [SerializeField] [TextArea(2, 4)] private string msgInvalid = "Nothing useful here. The hunter keeps moving.";

    [Header("成功结局面板")]
    [SerializeField] [TextArea(2, 4)] private string goodEndingMessageP2 =
        "The hunter escorted the \"wild boar\" out of the forest.\nSnow White successfully escaped!";
    [SerializeField] [TextArea(2, 4)] private string goodEndingMessageP3 =
        "After a fierce struggle, the hunter caught the \"wild boar\" and left the forest.\nSnow escaped in confusion!";

    [Header("坏结局面板")]
    [SerializeField] [TextArea(2, 4)] private string badEndingMessage =
        "Snow was knocked out by the wild boar and couldn't escape the hunter.";

    [Header("到达终点结局")]
    [SerializeField] [TextArea(2, 4)] private string msgReachedEnd =
        "The hunter, fixated on Snow, was tripped by a passing wild boar!";
    [SerializeField] [TextArea(2, 4)] private string endEndingMessage =
        "The hunter was knocked out and couldn't get up. Snow successfully escaped!";

    private GameObject canvasObject;
    private GameObject dialoguePanelObject;
    private TMP_Text dialogueText;

    private Coroutine dialogueCoroutine;
    private bool hasEnded;
    private bool endTriggerEnabled;

    private void OnEnable()
    {
        PlacementTriggerZone.OnToolPlaced += HandleToolPlaced;
        StoryActorAutoMove.ActorReachedEnd += HandleActorReachedEnd;
    }

    private void OnDisable()
    {
        PlacementTriggerZone.OnToolPlaced -= HandleToolPlaced;
        StoryActorAutoMove.ActorReachedEnd -= HandleActorReachedEnd;
    }

    private void Start()
    {
        if (hunterActor == null)
            hunterActor = FindAnyObjectByType<StoryActorAutoMove>();

        BuildRuntimeUI();
        HideDialogue();
    }

    // ── 终点触发 ─────────────────────────────────────────────────────────────

    /// <summary>由 Node2_2FlowController 在猎人开始第二段行走时调用，开启终点监听。</summary>
    public void EnableEndTrigger() => endTriggerEnabled = true;

    private void HandleActorReachedEnd(StoryActorAutoMove actor)
    {
        if (!endTriggerEnabled) return;
        if (hasEnded) return;
        if (actor.gameObject.scene != gameObject.scene) return;
        if (dialogueCoroutine != null) StopCoroutine(dialogueCoroutine);
        dialogueCoroutine = StartCoroutine(ShowMessageThenEnd(msgReachedEnd, endEndingMessage));
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
        // 猎人经由转场平移到达，Start() 时可能还不存在，这里补找
        if (hunterActor == null)
            hunterActor = FindAnyObjectByType<StoryActorAutoMove>();

        Node2_2EffectType effectType = GetEffectType(point.placePointID, point.storedToolCardID);
        string message = GetMessage(effectType);

        if (dialogueCoroutine != null) StopCoroutine(dialogueCoroutine);

        if (effectType == Node2_2EffectType.HallucinationMushroomOnP2)
            dialogueCoroutine = StartCoroutine(ShowMessageThenEnd(message, goodEndingMessageP2));
        else if (effectType == Node2_2EffectType.LeafCloakOnP3)
            dialogueCoroutine = StartCoroutine(ShowMessageThenEnd(message, goodEndingMessageP3));
        else if (effectType == Node2_2EffectType.HoneyBaitOnP4)
            dialogueCoroutine = StartCoroutine(ShowMessageThenEnd(message, null));
        else
            dialogueCoroutine = StartCoroutine(ShowMessageThenContinue(message));
    }

    // ── 效果分类 ─────────────────────────────────────────────────────────────

    private Node2_2EffectType GetEffectType(string placePointID, string toolCardID)
    {
        if (placePointID == "N2_P2" && toolCardID == "T_HALLUCINATION_MUSHROOM")
            return Node2_2EffectType.HallucinationMushroomOnP2;

        if (placePointID == "N2_P3" && toolCardID == "T_LEAF_CLOAK")
            return Node2_2EffectType.LeafCloakOnP3;

        if (placePointID == "N2_P4" && toolCardID == "T_HONEY_BAIT")
            return Node2_2EffectType.HoneyBaitOnP4;

        return Node2_2EffectType.InvalidPlacement;
    }

    private string GetMessage(Node2_2EffectType effectType)
    {
        return effectType switch
        {
            Node2_2EffectType.HallucinationMushroomOnP2 => msgHallucinationMushroom,
            Node2_2EffectType.LeafCloakOnP3             => msgLeafCloak,
            Node2_2EffectType.HoneyBaitOnP4             => msgHoneyBait,
            _                                           => msgInvalid
        };
    }

    // ── 协程：旁白 → 结局 ────────────────────────────────────────────────────

    // endingText == null 代表坏结局
    private IEnumerator ShowMessageThenEnd(string message, string endingText)
    {
        hasEnded = true;
        if (hunterActor != null) hunterActor.StopMove();

        ShowDialogue(message);
        yield return new WaitForSeconds(messageDuration);
        HideDialogue();

        ShowEndingPanel(endingText);
    }

    // ── 协程：旁白 → 恢复行走 ────────────────────────────────────────────────

    private IEnumerator ShowMessageThenContinue(string message)
    {
        if (hunterActor != null) hunterActor.PauseMove();

        ShowDialogue(message);
        yield return new WaitForSeconds(messageDuration);
        HideDialogue();

        if (hasEnded)
        {
            if (hunterActor != null) hunterActor.StopMove();
            yield break;
        }
        if (hunterActor != null) hunterActor.ResumeMove();
    }

    // ── 结局面板 ─────────────────────────────────────────────────────────────

    // endingText == null 代表坏结局
    private void ShowEndingPanel(string endingText)
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

        bool good = endingText != null;

        CreateText("EndingText", panel.transform,
            good ? "Success\n\n" + endingText : "Failed\n\n" + badEndingMessage,
            new Vector2(0.5f, 0.66f), new Vector2(780f, 190f), 38f);

        if (good)
        {
            Button retryBtn = CreateButton("RetryButton", panel.transform,
                "Try Another Way", new Vector2(0.37f, 0.22f), new Vector2(260f, 76f));
            retryBtn.onClick.AddListener(RetryNode2);

            Button nextBtn = CreateButton("NextButton", panel.transform,
                "Next Level", new Vector2(0.67f, 0.22f), new Vector2(220f, 76f));
            nextBtn.onClick.AddListener(() =>
                Debug.Log("Node2_2 Next Level clicked. Next scene not yet configured."));
        }
        else
        {
            Button retryBtn = CreateButton("RetryButton", panel.transform,
                "Try Again", new Vector2(0.5f, 0.22f), new Vector2(220f, 76f));
            retryBtn.onClick.AddListener(RetryNode2);
        }
    }

    private void RetryNode2()
    {
        GameSessionData.CurrentNodeID = "Node2";
        GameSessionData.CurrentNodeSceneName = retrySceneName;
        GameSessionData.CurrentPhase = GameFlowPhase.Briefing;
        GameSessionData.ToolCardIDs.Clear();
        LoadScene(retrySceneName);
    }

    private void LoadScene(string sceneName)
    {
#if UNITY_EDITOR
        string path = $"Assets/Scenes/{sceneName}.unity";
        if (SceneUtility.GetBuildIndexByScenePath(path) < 0)
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
            "Node2_2ResultCanvas",
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
