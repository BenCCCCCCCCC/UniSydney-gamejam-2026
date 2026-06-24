using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Temporary game jam bridge for showing CardBackpack tools in Node1.
// It reads GameSessionData and writes selected tools into PlacementPoint.
public class NodeToolHandController : MonoBehaviour
{
    private static NodeToolHandController instance;

    private RectTransform handArea;
    private TMP_Text activeToolText;
    private string activeToolCardID;

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
        if (scene.name != "Node1_QueenCastle")
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
        ClearPlacementPointTestValues();
        AttachPlacementPointClickBridges();
        BuildToolButtons();
    }

    public void ConfigureRuntime(RectTransform runtimeHandArea, TMP_Text runtimeActiveToolText)
    {
        handArea = runtimeHandArea;
        activeToolText = runtimeActiveToolText;
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

        Node1PlacementRules.TryPlaceTool(activeToolCardID, placementPoint);
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

    private static string FormatToolIDs(System.Collections.Generic.List<string> toolCardIDs)
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
        rect.sizeDelta = new Vector2(150f, 64f);

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = 150f;
        layoutElement.preferredHeight = 64f;
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
            new Vector2(140f, 54f),
            18f);
        label.color = Color.white;
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

            GameObject canvasObject = new GameObject("NodeToolHandCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

            RectTransform handArea = CreateHandArea(canvasRect);

            GameObject controllerObject = new GameObject("NodeToolHandController");
            NodeToolHandController controller = controllerObject.AddComponent<NodeToolHandController>();
            controller.ConfigureRuntime(handArea, activeToolText);
        }

        private static RectTransform CreateHandArea(Transform parent)
        {
            GameObject areaObject = new GameObject("NodeToolHandArea", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
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
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.SetActive(true);
        }
    }
}
