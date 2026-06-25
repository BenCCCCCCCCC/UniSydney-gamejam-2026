using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Temporary game jam bridge for showing CardBackpack tools in Node scenes.
// It reads GameSessionData and writes selected tools into PlacementPoint.
public class NodeToolHandController : MonoBehaviour
{
    private static NodeToolHandController instance;

    [Header("Tool Card Art")]
    [SerializeField] private CardArtCatalog cardArtCatalog;
    [SerializeField] private bool useResourcesArtFallback = true;
    [SerializeField] private Vector2 toolHandCardSize = new Vector2(120f, 160f);

    private RectTransform handArea;
    private TMP_Text activeToolText;
    private RectTransform p1DropSlot;
    private RectTransform p2DropSlot;
    private RectTransform p3DropSlot;

    private readonly Dictionary<string, RectTransform> dropSlotsByPointID = new();

    private string activeToolCardID;
    private bool slotsAligned = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneLoadedHandler()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        BuildRuntimeBridgeIfNeeded(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BuildRuntimeBridgeIfNeeded(scene);
    }

    private static void BuildRuntimeBridgeIfNeeded(Scene scene)
    {
        if (GameSessionData.CurrentPhase != GameFlowPhase.Placement)
        {
            return;
        }

        if (GameSessionData.ToolCardIDs.Count == 0)
        {
            return;
        }

        if (FindAnyObjectByType<PlacementPoint>() == null)
        {
            return;
        }

        if (FindAnyObjectByType<NodeToolHandController>() != null)
        {
            return;
        }

        RuntimeUiBuilder.Build();
    }

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnNewSceneLoaded;

        ClearPlacementPointTestValues();
        AttachPlacementPointClickBridges();
        BuildDropSlots();
        // 不在 Start 里对齐——等 LateUpdate 首帧（CanvasScaler 已在 Update 运行后）再对齐
        BuildToolButtons();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnNewSceneLoaded;
    }

    // 新场景加载后重新扫描当前场景的放置点，更新 drop slot 映射和对齐
    private void OnNewSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 清除上一场景遗留的已放置卡牌，防止它们被带入新场景
        ToolCardDragItem.ConsumeAllPlacedCards();
        EnsureEventSystem(); // EventSystem 可能随旧场景卸载而消失，在此补建
        AttachPlacementPointClickBridges();
        BuildDropSlots();
        slotsAligned = false; // 触发 LateUpdate 重新对齐
    }

    // 确保场景中始终有 EventSystem；若新建则跨场景存活
    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        DontDestroyOnLoad(go);
    }

    private void LateUpdate()
    {
        if (!slotsAligned)
        {
            AlignDropSlotsToPlacementPoints();
            slotsAligned = true;
        }
    }

    public void ConfigureRuntime(
        RectTransform runtimeHandArea,
        TMP_Text runtimeActiveToolText,
        RectTransform runtimeP1DropSlot,
        RectTransform runtimeP2DropSlot,
        RectTransform runtimeP3DropSlot)
    {
        handArea = runtimeHandArea;
        activeToolText = runtimeActiveToolText;
        p1DropSlot = runtimeP1DropSlot;
        p2DropSlot = runtimeP2DropSlot;
        p3DropSlot = runtimeP3DropSlot;

        BuildDropSlots();
    }

    public static void TryPlaceActiveTool(PlacementPoint placementPoint)
    {
        if (instance == null)
        {
            Debug.LogWarning("NodeToolHandController: no active controller in scene.");
            return;
        }

        instance.PlaceActiveTool(placementPoint);
    }

    public static void SetActiveTool(string toolCardID)
    {
        if (instance == null)
        {
            Debug.LogWarning("NodeToolHandController: no active controller in scene.");
            return;
        }

        instance.SelectTool(toolCardID);
        Debug.Log($"Active Tool set by drag: {toolCardID}");
    }

    public static RectTransform GetDropSlotForPoint(string placePointID)
    {
        if (instance == null)
        {
            Debug.LogWarning("NodeToolHandController: no active controller in scene.");
            return null;
        }

        if (instance.dropSlotsByPointID.TryGetValue(placePointID, out RectTransform slot))
        {
            return slot;
        }

        return null;
    }

    public static string GetDropSlotNameForPoint(string placePointID)
    {
        if (string.IsNullOrWhiteSpace(placePointID))
        {
            return "(none)";
        }

        if (placePointID.EndsWith("_P1", System.StringComparison.Ordinal))
        {
            return "P1_DropSlotUI";
        }

        if (placePointID.EndsWith("_P2", System.StringComparison.Ordinal))
        {
            return "P2_DropSlotUI";
        }

        if (placePointID.EndsWith("_P3", System.StringComparison.Ordinal))
        {
            return "P3_DropSlotUI";
        }

        return "(none)";
    }

    private void SelectTool(string toolCardID)
    {
        activeToolCardID = toolCardID;
        UpdateActiveToolText();

        Debug.Log($"NodeToolHandController: active tool = {toolCardID}");
    }

    private void PlaceActiveTool(PlacementPoint placementPoint)
    {
        if (placementPoint == null)
        {
            Debug.LogWarning("NodeToolHandController: clicked placement point is null.");
            return;
        }

        if (string.IsNullOrWhiteSpace(activeToolCardID))
        {
            Debug.LogWarning($"NodeToolHandController: no active tool selected for {placementPoint.placePointID}.");
            return;
        }

        if (placementPoint.nodeID == "Node1")
        {
            Node1PlacementRules.TryPlaceTool(activeToolCardID, placementPoint);
        }
        else
        {
            Debug.Log($"PLACE_TEST: {activeToolCardID} on {placementPoint.nodeID}/{placementPoint.placePointID}");
        }

        placementPoint.SetTool(activeToolCardID);
        UpdateActiveToolText();
    }

    private void ClearPlacementPointTestValues()
    {
        PlacementPoint[] placementPoints = FindObjectsByType<PlacementPoint>();

        foreach (PlacementPoint point in placementPoints)
        {
            point.SetTool(string.Empty);
        }

        Debug.Log($"NodeToolHandController: cleared {placementPoints.Length} placement point test tool values.");
    }

    private void AttachPlacementPointClickBridges()
    {
        PlacementPoint[] placementPoints = FindObjectsByType<PlacementPoint>();

        foreach (PlacementPoint point in placementPoints)
        {
            PlacementPointClickBridge bridge = point.GetComponent<PlacementPointClickBridge>();

            if (bridge == null)
            {
                bridge = point.gameObject.AddComponent<PlacementPointClickBridge>();
            }

            bridge.SetPlacementPoint(point);

            Collider2D existingCollider = point.GetComponent<Collider2D>();

            if (existingCollider == null)
            {
                BoxCollider2D clickCollider = point.gameObject.AddComponent<BoxCollider2D>();
                clickCollider.isTrigger = true;
                clickCollider.size = new Vector2(3f, 3f);

                Debug.Log($"Added placement collider for {point.placePointID} size = {clickCollider.size}");
            }
            else
            {
                Debug.Log($"Existing placement collider for {point.placePointID} bounds size = {existingCollider.bounds.size}");
            }
        }
    }

    private void BuildDropSlots()
    {
        dropSlotsByPointID.Clear();

        PlacementPoint[] placementPoints = FindObjectsByType<PlacementPoint>();

        foreach (PlacementPoint point in placementPoints)
        {
            if (point == null || string.IsNullOrWhiteSpace(point.placePointID))
            {
                continue;
            }

            if (point.placePointID.EndsWith("_P1", System.StringComparison.Ordinal) && p1DropSlot != null)
            {
                dropSlotsByPointID[point.placePointID] = p1DropSlot;
            }
            else if (point.placePointID.EndsWith("_P2", System.StringComparison.Ordinal) && p2DropSlot != null)
            {
                dropSlotsByPointID[point.placePointID] = p2DropSlot;
            }
            else if (point.placePointID.EndsWith("_P3", System.StringComparison.Ordinal) && p3DropSlot != null)
            {
                dropSlotsByPointID[point.placePointID] = p3DropSlot;
            }
        }

        Debug.Log($"NodeToolHandController: built {dropSlotsByPointID.Count} drop slot mappings.");
    }

    private void AlignDropSlotsToPlacementPoints()
    {
        Canvas canvas = p1DropSlot != null ? p1DropSlot.GetComponentInParent<Canvas>() : null;
        RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        Camera sceneCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();

        if (canvas == null || canvasRect == null || sceneCamera == null)
        {
            Debug.LogWarning("NodeToolHandController: cannot align drop slots because canvas or camera is missing.");
            return;
        }

        PlacementPoint[] placementPoints = FindObjectsByType<PlacementPoint>();

        foreach (PlacementPoint point in placementPoints)
        {
            if (point == null || !dropSlotsByPointID.TryGetValue(point.placePointID, out RectTransform slot) || slot == null)
            {
                continue;
            }

            Vector3 worldPosition = GetPlacementWorldCenter(point);

            // WorldToViewportPoint 返回 [0,1] 归一化坐标，再乘 Screen 尺寸得到屏幕坐标
            // 避免 camera.pixelWidth vs Screen.width 不一致（如编辑器缩放、DPI 缩放）导致偏移
            Vector3 viewportPoint = sceneCamera.WorldToViewportPoint(worldPosition);
            Vector2 screenPosition = new Vector2(viewportPoint.x * Screen.width, viewportPoint.y * Screen.height);

            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, uiCamera, out Vector2 localPoint))
            {
                slot.anchorMin = new Vector2(0.5f, 0.5f);
                slot.anchorMax = new Vector2(0.5f, 0.5f);
                slot.pivot = new Vector2(0.5f, 0.5f);
                slot.anchoredPosition = localPoint;

                Debug.Log(
                    $"DROP_SLOT_ALIGNED: {point.placePointID} " +
                    $"world={FormatVector3(worldPosition)} " +
                    $"screen={FormatVector2(screenPosition)} " +
                    $"local={FormatVector2(localPoint)}"
                );
            }
        }
    }

    private static Vector3 GetPlacementWorldCenter(PlacementPoint point)
    {
        // 优先用 ToolSlotVisual 的世界位置，让 UI 放置区和玩家看到的槽位指示器对齐
        Transform slotVisual = FindToolSlotVisual(point);
        if (slotVisual != null)
        {
            return slotVisual.position;
        }

        Collider2D triggerCollider = FindTriggerZoneCollider(point);

        if (triggerCollider != null)
        {
            return triggerCollider.bounds.center;
        }

        Collider2D pointCollider = point.GetComponentInChildren<Collider2D>();

        if (pointCollider != null)
        {
            return pointCollider.bounds.center;
        }

        return point.transform.position;
    }

    private static Transform FindToolSlotVisual(PlacementPoint point)
    {
        for (int i = 0; i < point.transform.childCount; i++)
        {
            Transform child = point.transform.GetChild(i);
            if (child.name.Contains("ToolSlotVisual"))
            {
                return child;
            }
        }
        return null;
    }

    private static Collider2D FindTriggerZoneCollider(PlacementPoint point)
    {
        PlacementTriggerZone[] triggerZones = FindObjectsByType<PlacementTriggerZone>();

        foreach (PlacementTriggerZone zone in triggerZones)
        {
            if (zone != null && zone.placementPoint == point)
            {
                Collider2D collider = zone.GetComponentInChildren<Collider2D>();

                if (collider != null)
                {
                    return collider;
                }
            }
        }

        return null;
    }

    private static string FormatVector2(Vector2 value)
    {
        return $"<{value.x:0.0}, {value.y:0.0}>";
    }

    private static string FormatVector3(Vector3 value)
    {
        return $"<{value.x:0.00}, {value.y:0.00}, {value.z:0.00}>";
    }

    private void BuildToolButtons()
    {
        if (handArea == null || activeToolText == null)
        {
            Debug.LogWarning("NodeToolHandController: runtime UI is missing.");
            return;
        }

        ClearChildren(handArea);

        Debug.Log($"NodeToolHandController loaded tool IDs: {FormatToolIDs(GameSessionData.ToolCardIDs)}");

        if (GameSessionData.ToolCardIDs.Count == 0)
        {
            Debug.LogWarning("NodeToolHandController: GameSessionData.ToolCardIDs is empty.");
        }

        foreach (string toolCardID in GameSessionData.ToolCardIDs)
        {
            CreateToolButton(toolCardID);
        }

        UpdateActiveToolText();
    }

    private static string FormatToolIDs(List<string> toolCardIDs)
    {
        if (toolCardIDs == null || toolCardIDs.Count == 0)
        {
            return "(none)";
        }

        return string.Join(", ", toolCardIDs);
    }

    private void CreateToolButton(string toolCardID)
    {
        GameObject buttonObject = new GameObject(
            toolCardID,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(CanvasGroup),
            typeof(LayoutElement),
            typeof(ToolCardDragItem));

        buttonObject.transform.SetParent(handArea, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = toolHandCardSize;

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = toolHandCardSize.x;
        layoutElement.preferredHeight = toolHandCardSize.y;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.22f, 0.36f, 0.55f, 0.95f);

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(() => SelectTool(toolCardID));

        ToolCardDragItem dragItem = buttonObject.GetComponent<ToolCardDragItem>();
        dragItem.Setup(toolCardID);

        TMP_Text label = CreateText(
            "Label",
            buttonObject.transform,
            CardDisplayNameHelper.ToEnglishName(toolCardID),
            new Vector2(0.5f, 0.5f),
            new Vector2(Mathf.Max(40f, toolHandCardSize.x - 10f), Mathf.Max(30f, toolHandCardSize.y - 10f)),
            18f);

        label.color = Color.white;

        Sprite toolSprite = CardArtLoader.GetSprite(toolCardID, cardArtCatalog, useResourcesArtFallback);
        if (toolSprite != null)
        {
            image.sprite = toolSprite;
            image.color = Color.white;
            image.preserveAspect = false;
            label.gameObject.SetActive(false);
        }
    }

    private void UpdateActiveToolText()
    {
        if (activeToolText == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(activeToolCardID))
        {
            activeToolText.text = "Active Tool: None";
            return;
        }

        activeToolText.text = $"Active Tool: {CardDisplayNameHelper.ToEnglishName(activeToolCardID)} ({activeToolCardID})";
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, Vector2 anchor, Vector2 size, float fontSize)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TMP_Text tmp = textObject.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontSize = fontSize;
        tmp.textWrappingMode = TextWrappingModes.Normal;

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        return tmp;
    }

    private static class RuntimeUiBuilder
    {
        public static void Build()
        {
            EnsureEventSystem();

            GameObject canvasObject = new GameObject(
                "NodeToolHandCanvas",
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

            TMP_Text activeToolText = CreateText(
                "ActiveToolText",
                canvasRect,
                "Active Tool: None",
                new Vector2(0.5f, 0.17f),
                new Vector2(900f, 44f),
                24f);

            RectTransform p1DropSlot = CreateDropSlot(canvasRect, "P1_DropSlotUI");
            RectTransform p2DropSlot = CreateDropSlot(canvasRect, "P2_DropSlotUI");
            RectTransform p3DropSlot = CreateDropSlot(canvasRect, "P3_DropSlotUI");
            RectTransform handArea = CreateHandArea(canvasRect);

            // Canvas 和 Controller 跨场景存活，转场时手牌持续显示
            DontDestroyOnLoad(canvasObject);

            GameObject controllerObject = new GameObject("NodeToolHandController");
            DontDestroyOnLoad(controllerObject);
            NodeToolHandController controller = controllerObject.AddComponent<NodeToolHandController>();
            controller.ConfigureRuntime(handArea, activeToolText, p1DropSlot, p2DropSlot, p3DropSlot);
        }

        private static RectTransform CreateDropSlot(Transform parent, string name)
        {
            GameObject slotObject = new GameObject(name, typeof(RectTransform));
            slotObject.transform.SetParent(parent, false);

            RectTransform rect = slotObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(170f, 84f);
            rect.anchoredPosition = Vector2.zero;

            return rect;
        }

        private static RectTransform CreateHandArea(Transform parent)
        {
            GameObject areaObject = new GameObject(
                "NodeToolHandArea",
                typeof(RectTransform),
                typeof(Image),
                typeof(HorizontalLayoutGroup));

            areaObject.transform.SetParent(parent, false);

            Image image = areaObject.GetComponent<Image>();
            image.color = new Color(0.04f, 0.05f, 0.07f, 0.82f);

            RectTransform rect = areaObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.22f, 0.025f);
            rect.anchorMax = new Vector2(0.78f, 0.14f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup layout = areaObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return rect;
        }

        private static void EnsureEventSystem()
        {
            NodeToolHandController.EnsureEventSystem();
        }
    }
}
