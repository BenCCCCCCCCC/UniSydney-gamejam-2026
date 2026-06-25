using System.Collections;
using FairyTale.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Node2_2 流程控制：
///   1. 转场完成后猎人先走到 pausePoint（停顿点）
///   2. 到达后弹出旁白，玩家可放置手牌
///   3. 按 Space 关闭旁白，猎人继续走向 actorEnd
/// </summary>
public class Node2_2FlowController : MonoBehaviour
{
    [Header("路径点（在 Inspector 里拖入）")]
    [Tooltip("猎人走到此处后停下并弹出旁白")]
    public Transform pausePoint;
    [Tooltip("猎人最终目标（Node2_2 的 N2_ActorEnd）")]
    public Transform actorEnd;

    [Header("旁白设置")]
    [SerializeField] private string briefingLine = "猎人追入深林，公主命悬一线……";
    [SerializeField] private float readingSeconds = 3f;

    private GameObject dialogueCanvas;
    private bool waitingForSpace;
    private StoryActorAutoMove hunterActor;
    private Node3PlacementPlayController placementCtrl;


    private void Awake()
    {
        // Awake 先于所有 Start() 运行，确保 Phase 在 PlacedToolIconAutoPlayTrigger.Start() 前重置
        // 否则后者在 AutoPlay 阶段看到 Phase 就会立刻触发（此时卡槽尚无卡片）
        GameSessionData.CurrentPhase = GameFlowPhase.Placement;
    }

    private void Start()
    {
        // 告诉转场管理器：步骤7完成后调用 OnActorReady，并跳过自动 StartPlay
        SceneTransitionManager.SkipActorAutoStartAfterPan = true;
        SceneTransitionManager.OnActorReadyInNewScene = OnActorReady;
    }

    // 转场管理器步骤7（MoveToStart）完成后触发
    private void OnActorReady()
    {
        hunterActor = FindAnyObjectByType<StoryActorAutoMove>();
        if (hunterActor == null) return;

        if (pausePoint != null && actorEnd != null)
        {
            // 让猎人先走到停顿点
            var startTf = hunterActor.transform; // 当前位置即 ActorStart（MoveToStart 已执行）
            // 用 SetMovePath 把终点改为 pausePoint，走到那里就停
            // startPoint 保持不变（ActorStart），只改 endPoint
            hunterActor.SetMovePath(hunterActor.transform, pausePoint);
            hunterActor.OnReachedEnd += HandleReachedPausePoint;
            hunterActor.StartPlay();
        }
        else
        {
            // 没配停顿点则直接弹出旁白
            hunterActor.PauseMove();
            ShowDialogueBubble();
            waitingForSpace = true;
        }
    }

    // 猎人走到停顿点后触发
    private void HandleReachedPausePoint()
    {
        hunterActor.OnReachedEnd -= HandleReachedPausePoint;
        ShowDialogueBubble();
        StartCoroutine(AutoDismissBriefing());
    }

    // 旁白定时消失，消失后等待玩家按 Space
private IEnumerator AutoDismissBriefing()
    {
        yield return new WaitForSeconds(readingSeconds);
        HideDialogueBubble();
        placementCtrl = FindAnyObjectByType<Node3PlacementPlayController>();
        placementCtrl?.ShowPlacementUI();
        waitingForSpace = true;
    }

    private void Update()
    {
        if (!waitingForSpace) return;
        if (Keyboard.current == null) return;

        bool spacePressed = Keyboard.current.spaceKey.wasPressedThisFrame;
        bool playClicked = placementCtrl != null && placementCtrl.HasStartedPlay;

        if (!spacePressed && !playClicked) return;

        waitingForSpace = false;

        // N2_P4 有蜜糖苹果时直接触发坏结局，猎人不启动
        if (CheckHoneyBaitOnP4())
        {
            enabled = false;
            return;
        }

        if (hunterActor != null)
        {
            if (actorEnd != null)
                hunterActor.SetMovePath(hunterActor.transform, actorEnd);

            // 猎人开始第二段行走，通知 ResultPlayer 可以监听终点事件了
            FindAnyObjectByType<Node2_2ResultPlayer>()?.EnableEndTrigger();

            hunterActor.StartPlay();
        }

        enabled = false;
    }

    private bool CheckHoneyBaitOnP4()
    {
        foreach (var point in FindObjectsByType<PlacementPoint>(FindObjectsInactive.Exclude))
        {
            if (point.placePointID == "N2_P4" && point.storedToolCardID == "T_HONEY_BAIT")
            {
                var resultPlayer = FindAnyObjectByType<Node2_2ResultPlayer>();
                resultPlayer?.PlayResult(point);
                return true;
            }
        }
        return false;
    }

    private void ShowDialogueBubble()
    {
        dialogueCanvas = new GameObject(
            "Node2_2DialogueCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = dialogueCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = dialogueCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = dialogueCanvas.GetComponent<RectTransform>();

        GameObject bubbleObject = new GameObject("DialogueBubble", typeof(RectTransform), typeof(Image));
        bubbleObject.transform.SetParent(canvasRect, false);

        Image bubbleImage = bubbleObject.GetComponent<Image>();
        bubbleImage.color = new Color(0.08f, 0.07f, 0.06f, 0.88f);

        RectTransform bubbleRect = bubbleObject.GetComponent<RectTransform>();
        bubbleRect.anchorMin = new Vector2(0.12f, 0.72f);
        bubbleRect.anchorMax = new Vector2(0.88f, 0.92f);
        bubbleRect.offsetMin = Vector2.zero;
        bubbleRect.offsetMax = Vector2.zero;

        GameObject textObject = new GameObject("DialogueText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(bubbleRect, false);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = briefingLine;
        text.fontSize = 34f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.04f, 0.08f);
        textRect.anchorMax = new Vector2(0.96f, 0.92f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void HideDialogueBubble()
    {
        if (dialogueCanvas != null)
            Destroy(dialogueCanvas);
    }
}
