using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Node3PlacementPlayController : MonoBehaviour
{
    private const int MaxPlacementSlots = 4;
    private const float PlacementSlotScale = 0.8f;
    private static readonly Vector2 NativePlacementSlotSize = new Vector2(378f, 528f);
    private static readonly float[] SlotRotations = { -8f, 5f, 12f, -4f };
    private static readonly Vector2[] FourSlotPositions =
    {
        new Vector2(0.25f, 0.58f),
        new Vector2(0.42f, 0.52f),
        new Vector2(0.58f, 0.61f),
        new Vector2(0.75f, 0.54f)
    };
    private const string PlacementSlotResourcesPath = "Art/UI/bg_add_card";
#if UNITY_EDITOR
    private const string PlacementSlotAssetPath = "Assets/Art/UI/bg_add_card.png";
#endif

    [Header("Node")]
    [SerializeField] private string nodeID = "Node3";

    [Header("Scene References")]
    [SerializeField] private StoryActorAutoMove storyActor;
    [SerializeField] private PlacementPoint[] requiredPlacementPoints;

    [Header("Card Art")]
    [SerializeField] private CardArtCatalog cardArtCatalog;
    [SerializeField] private bool useResourcesArtFallback = true;

    [Header("Central Slots")]
    [SerializeField] private Vector2 slotPanelAnchor = new Vector2(0.5f, 0.56f);
    [SerializeField] private Vector2 slotPanelSize = new Vector2(820f, 260f);
    [SerializeField] private Vector2 slotSize = new Vector2(210f, 240f);
    [SerializeField] private float slotSpacing = 34f;
    [SerializeField] private Sprite placementSlotSprite;

    [Header("Tool Hand")]
    [SerializeField] private Vector2 handPanelAnchor = new Vector2(0.5f, 0.20f);
    [SerializeField] private Vector2 handPanelSize = new Vector2(1000f, 180f);
    [SerializeField] private Vector2 handCardSize = new Vector2(210f, 294f);
    [SerializeField] private float handCardSpacing = 18f;

    [Header("Runtime Play Button")]
    [SerializeField] private bool createRuntimePlayButton = true;
    [SerializeField] private Button playButton;
    [SerializeField] private TMP_Text playButtonText;

    [Header("Button Position")]
    [SerializeField] private Vector2 buttonAnchor = new Vector2(0.88f, 0.24f);
    [SerializeField] private Vector2 buttonSize = new Vector2(220f, 70f);

    [Header("Deferred Show")]
    [Tooltip("false = 构建后立即隐藏，等外部调用 ShowPlacementUI() 再显示（用于 Node2_2 等先旁白后放牌的场景）")]
    [SerializeField] private bool showUIImmediately = true;

    [Header("Actor Start Delay")]
    [SerializeField] private float actorStartDelaySeconds = 2.5f;
    [SerializeField] private bool delayInitialActorStart = true;

    private GameObject tableCanvasObject;
    private GameObject playButtonCanvasObject;
    private RectTransform canvasRect;
    private RectTransform handPanel;
    private RectTransform[] slotRects;
    private Node3CentralToolCardDragItem[] placedCards;
    private CardDatabase runtimeCardDatabase;
    private HandCardPresentationSettings handPresentationSettings;
    private bool loggedMissingPlacementSlotSprite;

    private bool hasStartedPlay;
    public bool HasStartedPlay => hasStartedPlay;


    private IEnumerator Start()
    {
        if (storyActor != null)
        {
            storyActor.PauseMove();
        }

        ApplySceneConfig(FindAnyObjectByType<NodeToolHandSceneConfig>());

        if (GameSessionData.CurrentPhase != GameFlowPhase.Placement
            && GameSessionData.CurrentPhase != GameFlowPhase.AutoPlay)
        {
            HidePlayButton();
            yield break;
        }

        EnsureRequiredPlacementPoints();

        yield return null;
        yield return null;

        HideLegacyRuntimeToolHandUI();
        ClearAllPlacementPointTools();

        BuildPlacementTable();

        if (createRuntimePlayButton && playButton == null)
        {
            BuildRuntimePlayButton();
            // showUIImmediately=false 时，Play 按钮 canvas 也延迟显示，和 tableCanvasObject 保持一致
            if (!showUIImmediately && playButtonCanvasObject != null)
                playButtonCanvasObject.SetActive(false);
        }

        RefreshPlayButtonState();
    }

    private void ApplySceneConfig(NodeToolHandSceneConfig config)
    {
        if (config == null)
        {
            return;
        }

        if (config.CardArtCatalog != null)
        {
            cardArtCatalog = config.CardArtCatalog;
        }

        useResourcesArtFallback = config.UseResourcesArtFallback;
    }

    private void Update()
    {
        if (GameSessionData.CurrentPhase != GameFlowPhase.Placement)
        {
            return;
        }

        if (hasStartedPlay)
        {
            return;
        }

        RefreshPlayButtonState();
    }

    private void EnsureRequiredPlacementPoints()
    {
        if (requiredPlacementPoints != null && requiredPlacementPoints.Length > 0)
        {
            requiredPlacementPoints = LimitPlacementPoints(requiredPlacementPoints);
            return;
        }

        PlacementPoint[] allPoints = FindObjectsByType<PlacementPoint>();

        List<PlacementPoint> nodePoints = new();

        foreach (PlacementPoint point in allPoints)
        {
            if (point != null && point.nodeID == nodeID)
            {
                nodePoints.Add(point);
            }
        }

        nodePoints.Sort((a, b) =>
        {
            string aID = a != null ? a.placePointID : string.Empty;
            string bID = b != null ? b.placePointID : string.Empty;
            return string.CompareOrdinal(aID, bID);
        });

        requiredPlacementPoints = LimitPlacementPoints(nodePoints.ToArray());
    }

    private PlacementPoint[] LimitPlacementPoints(PlacementPoint[] source)
    {
        if (source == null || source.Length == 0)
        {
            return System.Array.Empty<PlacementPoint>();
        }

        List<PlacementPoint> limitedPoints = new(MaxPlacementSlots);
        foreach (PlacementPoint point in source)
        {
            if (point == null)
            {
                continue;
            }

            limitedPoints.Add(point);
            if (limitedPoints.Count >= MaxPlacementSlots)
            {
                break;
            }
        }

        if (source.Length > MaxPlacementSlots)
        {
            Debug.LogWarning(
                $"Node3PlacementPlayController: Node 3 supports at most {MaxPlacementSlots} placement slots. "
                + "Only the first four valid placement points will be used.");
        }

        return limitedPoints.ToArray();
    }

    private void ClearAllPlacementPointTools()
    {
        if (requiredPlacementPoints == null)
        {
            return;
        }

        foreach (PlacementPoint point in requiredPlacementPoints)
        {
            if (point != null)
            {
                point.SetTool(string.Empty);
            }
        }
    }

private void BuildPlacementTable()
    {
        if (tableCanvasObject != null)
        {
            Destroy(tableCanvasObject);
        }

        EnsureEventSystem();

        tableCanvasObject = new GameObject(
            "Node3ManualPlacementTableCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        // 确保 canvas 在组件所在的场景里，而不是当前 active scene
        // （转场期间 active scene 可能是旧场景，卸载时会一并销毁此 canvas）
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(
            tableCanvasObject, gameObject.scene);

        Canvas canvas = tableCanvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = tableCanvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasRect = tableCanvasObject.GetComponent<RectTransform>();

        BuildSlotPanel();
        BuildHandPanel();

        if (!showUIImmediately)
            tableCanvasObject.SetActive(false);
    }

    public void ShowPlacementUI()
    {
        if (tableCanvasObject != null)
            tableCanvasObject.SetActive(true);
        if (playButtonCanvasObject != null)
            playButtonCanvasObject.SetActive(true);
    }

    private void BuildSlotPanel()
    {
        GameObject panelObject = new GameObject(
            "CentralPlacementSlots",
            typeof(RectTransform));

        panelObject.transform.SetParent(canvasRect, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        int requestedSlotCount = requiredPlacementPoints != null
            ? requiredPlacementPoints.Length
            : 0;

        int slotCount = requestedSlotCount > 0
            ? Mathf.Min(requestedSlotCount, MaxPlacementSlots)
            : 3;

        slotRects = new RectTransform[slotCount];
        placedCards = new Node3CentralToolCardDragItem[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            CreateSlot(panelObject.transform, i, slotCount);
        }
    }

    private void CreateSlot(Transform parent, int slotIndex, int slotCount)
    {
        GameObject slotObject = new GameObject(
            $"Point{slotIndex + 1}_CardSlot",
            typeof(RectTransform),
            typeof(Image),
            typeof(Node3CentralCardSlot));

        slotObject.transform.SetParent(parent, false);

        RectTransform rect = slotObject.GetComponent<RectTransform>();
        Vector2 normalizedPosition = GetNormalizedSlotPosition(slotIndex, slotCount);
        rect.anchorMin = normalizedPosition;
        rect.anchorMax = normalizedPosition;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = NativePlacementSlotSize * PlacementSlotScale;
        rect.localRotation = Quaternion.Euler(0f, 0f, GetSlotRotation(slotIndex));
        slotRects[slotIndex] = rect;

        Image image = slotObject.GetComponent<Image>();
        image.sprite = GetPlacementSlotSprite();
        image.preserveAspect = true;
        image.raycastTarget = true;

        if (image.sprite != null)
        {
            image.color = Color.white;
            image.SetNativeSize();
            rect.sizeDelta *= PlacementSlotScale;
        }
        else
        {
            image.color = Color.clear;
            rect.sizeDelta = NativePlacementSlotSize * PlacementSlotScale;
        }

        Node3CentralCardSlot slot = slotObject.GetComponent<Node3CentralCardSlot>();
        slot.Setup(this, slotIndex);
    }

    private Vector2 GetNormalizedSlotPosition(int slotIndex, int slotCount)
    {
        if (slotCount >= MaxPlacementSlots)
        {
            return FourSlotPositions[Mathf.Clamp(slotIndex, 0, FourSlotPositions.Length - 1)];
        }

        if (slotCount == 3)
        {
            Vector2[] positions =
            {
                new Vector2(0.30f, 0.58f),
                new Vector2(0.50f, 0.52f),
                new Vector2(0.70f, 0.60f)
            };
            return positions[Mathf.Clamp(slotIndex, 0, positions.Length - 1)];
        }

        if (slotCount == 2)
        {
            return slotIndex == 0
                ? new Vector2(0.38f, 0.57f)
                : new Vector2(0.62f, 0.53f);
        }

        return new Vector2(0.5f, 0.56f);
    }

    private static float GetSlotRotation(int slotIndex)
    {
        return SlotRotations[slotIndex % SlotRotations.Length];
    }

    private Sprite GetPlacementSlotSprite()
    {
        if (placementSlotSprite != null)
        {
            return placementSlotSprite;
        }

        placementSlotSprite = Resources.Load<Sprite>(PlacementSlotResourcesPath);

#if UNITY_EDITOR
        if (placementSlotSprite == null)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(PlacementSlotAssetPath);
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    placementSlotSprite = sprite;
                    break;
                }
            }
        }
#endif

        if (placementSlotSprite == null && !loggedMissingPlacementSlotSprite)
        {
            Debug.LogWarning(
                "Node3PlacementPlayController: bg_add_card could not be loaded. "
                + "Assign Assets/Art/UI/bg_add_card.png to Placement Slot Sprite "
                + "or provide it at Resources/Art/UI/bg_add_card.");
            loggedMissingPlacementSlotSprite = true;
        }

        return placementSlotSprite;
    }

    private void BuildHandPanel()
    {
        GameObject handObject = new GameObject(
            "CraftedToolHand",
            typeof(RectTransform),
            typeof(Image),
            typeof(HorizontalLayoutGroup));

        handObject.transform.SetParent(canvasRect, false);

        Image image = handObject.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = false;

        handPanel = handObject.GetComponent<RectTransform>();
        handPanel.anchorMin = new Vector2(handPanelAnchor.x, 0f);
        handPanel.anchorMax = new Vector2(handPanelAnchor.x, 0f);
        handPanel.pivot = new Vector2(0.5f, 0f);
        handPanel.sizeDelta = handPanelSize;
        handPanel.anchoredPosition = Vector2.zero;

        handPresentationSettings = HandCardPresentationApplier.ResolveSettings(
            HandCardPresentationApplier.GetCurrentOrDefaults());
        HandCardPresentationApplier.ApplyHandArea(handPanel, handPresentationSettings);

        Debug.Log($"NODE3_MANUAL_HAND_TOOLS: {string.Join(", ", GameSessionData.ToolCardIDs)}");

        foreach (string toolCardID in GameSessionData.ToolCardIDs)
        {
            CreateHandCard(toolCardID);
        }
    }

    private void CreateHandCard(string toolCardID)
    {
        Sprite sprite = CardArtLoader.GetSprite(toolCardID, cardArtCatalog, useResourcesArtFallback);
        CardRow card = GetToolCard(toolCardID);
        if (card == null)
        {
            return;
        }

        Canvas canvas = tableCanvasObject.GetComponent<Canvas>();
        CardView cardView = HandCardViewFactory.Create(
            handPanel,
            card,
            null,
            sprite,
            handPresentationSettings,
            canvas);

        if (cardView == null)
        {
            return;
        }

        cardView.gameObject.name = $"ToolCard_{toolCardID}";
        Node3CentralToolCardDragItem dragItem =
            cardView.gameObject.AddComponent<Node3CentralToolCardDragItem>();
        dragItem.Setup(this, toolCardID, canvas, handPanel);
    }

    private CardRow GetToolCard(string toolCardID)
    {
        CardDatabase database = GetRuntimeCardDatabase();
        CardRow card = null;
        bool found = database != null && database.TryGetCard(toolCardID, out card);

        if (!found)
        {
            Debug.LogWarning($"Node3PlacementPlayController: card data missing for {toolCardID}.");
            return null;
        }

        return card;
    }

    private CardDatabase GetRuntimeCardDatabase()
    {
        if (runtimeCardDatabase != null)
        {
            return runtimeCardDatabase;
        }

        GameObject databaseObject = new GameObject("RuntimeNode3CardDatabase");
        databaseObject.transform.SetParent(transform, false);
        runtimeCardDatabase = databaseObject.AddComponent<CardDatabase>();
        return runtimeCardDatabase;
    }

    public void TryPlaceCardInSlot(Node3CentralToolCardDragItem card, int slotIndex)
    {
        if (card == null)
        {
            return;
        }

        if (slotIndex < 0 || slotIndex >= slotRects.Length)
        {
            card.ReturnToHand();
            RestoreHandCardPresentation(card);
            return;
        }

        if (requiredPlacementPoints == null || slotIndex >= requiredPlacementPoints.Length || requiredPlacementPoints[slotIndex] == null)
        {
            card.ReturnToHand();
            RestoreHandCardPresentation(card);
            return;
        }

        int oldSlotIndex = card.CurrentSlotIndex;

        if (oldSlotIndex >= 0 && oldSlotIndex < placedCards.Length && placedCards[oldSlotIndex] == card)
        {
            placedCards[oldSlotIndex] = null;

            if (oldSlotIndex < requiredPlacementPoints.Length && requiredPlacementPoints[oldSlotIndex] != null)
            {
                requiredPlacementPoints[oldSlotIndex].SetTool(string.Empty);
            }
        }

        if (placedCards[slotIndex] != null && placedCards[slotIndex] != card)
        {
            Node3CentralToolCardDragItem oldCard = placedCards[slotIndex];
            oldCard.ReturnToHand();
            RestoreHandCardPresentation(oldCard);
        }

        placedCards[slotIndex] = card;

        PlacementPoint point = requiredPlacementPoints[slotIndex];
        bool isValidPlacement = NodePlacementRules.TryPlaceTool(card.ToolCardID, point);
        point.SetTool(card.ToolCardID);

        SetPlacedCardHoverEnabled(card, false);
        card.PlaceInSlot(slotIndex, slotRects[slotIndex]);

        Debug.Log($"NODE3_MANUAL_PLACE: {card.ToolCardID} -> {point.placePointID}, valid = {isValidPlacement}");

        RefreshPlayButtonState();
    }

    public void ReturnCardToHand(Node3CentralToolCardDragItem card)
    {
        if (card == null)
        {
            return;
        }

        int oldSlotIndex = card.CurrentSlotIndex;

        if (oldSlotIndex >= 0 && oldSlotIndex < placedCards.Length && placedCards[oldSlotIndex] == card)
        {
            placedCards[oldSlotIndex] = null;

            if (oldSlotIndex < requiredPlacementPoints.Length && requiredPlacementPoints[oldSlotIndex] != null)
            {
                requiredPlacementPoints[oldSlotIndex].SetTool(string.Empty);
            }
        }

        card.ReturnToHand();
        RestoreHandCardPresentation(card);
        RefreshPlayButtonState();
    }

    private void RestoreHandCardPresentation(Node3CentralToolCardDragItem card)
    {
        if (card == null || tableCanvasObject == null)
        {
            return;
        }

        HandCardPresentationApplier.ApplyHandCard(
            card.gameObject,
            tableCanvasObject.GetComponent<Canvas>(),
            handPresentationSettings);
    }

    private static void SetPlacedCardHoverEnabled(
        Node3CentralToolCardDragItem card,
        bool hoverEnabled)
    {
        if (card == null)
        {
            return;
        }

        HandCardHoverEffect hoverEffect = card.GetComponent<HandCardHoverEffect>();
        if (hoverEffect != null)
        {
            hoverEffect.SetHoverEnabled(hoverEnabled);
        }
    }

    private void RefreshPlayButtonState()
    {
        if (playButton == null)
        {
            return;
        }

        int filledCount = GetFilledPointCount();
        int totalCount = GetRequiredPointCount();
        bool allFilled = totalCount > 0 && filledCount >= totalCount;

        playButton.gameObject.SetActive(true);
        playButton.interactable = allFilled && !hasStartedPlay;

        if (playButtonText != null)
        {
            playButtonText.text = allFilled
                ? "Play"
                : $"Need Tools {filledCount}/{totalCount}";
        }
    }

    private int GetRequiredPointCount()
    {
        if (requiredPlacementPoints == null)
        {
            return 0;
        }

        int count = 0;

        foreach (PlacementPoint point in requiredPlacementPoints)
        {
            if (point != null)
            {
                count++;
            }
        }

        return count;
    }

    private int GetFilledPointCount()
    {
        if (requiredPlacementPoints == null)
        {
            return 0;
        }

        int count = 0;

        foreach (PlacementPoint point in requiredPlacementPoints)
        {
            if (point != null && point.HasTool())
            {
                count++;
            }
        }

        return count;
    }

    private bool AreAllRequiredPointsFilled()
    {
        if (requiredPlacementPoints == null || requiredPlacementPoints.Length == 0)
        {
            return false;
        }

        foreach (PlacementPoint point in requiredPlacementPoints)
        {
            if (point == null || !point.HasTool())
            {
                return false;
            }
        }

        return true;
    }

    public void StartNodePlay()
    {
        if (hasStartedPlay)
        {
            return;
        }

        if (!AreAllRequiredPointsFilled())
        {
            Debug.LogWarning("Node3PlacementPlayController: cannot play because not all placement points have tools.");
            RefreshPlayButtonState();
            return;
        }

        hasStartedPlay = true;
        GameSessionData.CurrentPhase = GameFlowPhase.AutoPlay;

        Debug.Log("NODE3_PLAY_STARTED");

        foreach (PlacementPoint point in requiredPlacementPoints)
        {
            if (point != null)
            {
                Debug.Log($"NODE3_CARD_SLOT: {point.placePointID} = {point.storedToolCardID}");
            }
        }

        HidePlayButton();

        if (tableCanvasObject != null)
        {
            tableCanvasObject.SetActive(false);
        }

        StartCoroutine(StartActorAfterDelay());
    }

    private IEnumerator StartActorAfterDelay()
    {
        if (delayInitialActorStart && actorStartDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(actorStartDelaySeconds);
        }

        if (storyActor != null)
        {
            storyActor.ResumeMove();
        }
        else
        {
            Debug.LogWarning("Node3PlacementPlayController: storyActor is not assigned.");
        }
    }

    private void HidePlayButton()
    {
        if (playButton != null)
        {
            playButton.gameObject.SetActive(false);
        }
    }

private void BuildRuntimePlayButton()
    {
        EnsureEventSystem();

        playButtonCanvasObject = new GameObject(
            "Node3PlayButtonCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        // 与 tableCanvasObject 相同：强制移入组件所在场景，防止转场卸载时被销毁
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(
            playButtonCanvasObject, gameObject.scene);

        Canvas canvas = playButtonCanvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = playButtonCanvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = playButtonCanvasObject.GetComponent<RectTransform>();

        GameObject buttonObject = new GameObject(
            "Node3PlayButton",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button));

        buttonObject.transform.SetParent(canvasRect, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.22f, 0.46f, 0.32f, 0.95f);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = buttonAnchor;
        buttonRect.anchorMax = buttonAnchor;
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = buttonSize;
        buttonRect.anchoredPosition = Vector2.zero;

        playButton = buttonObject.GetComponent<Button>();
        playButton.onClick.RemoveAllListeners();
        playButton.onClick.AddListener(StartNodePlay);

        GameObject textObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(TextMeshProUGUI));

        textObject.transform.SetParent(buttonObject.transform, false);

        playButtonText = textObject.GetComponent<TMP_Text>();
        playButtonText.alignment = TextAlignmentOptions.Center;
        playButtonText.color = Color.white;
        playButtonText.fontSize = 28f;
        playButtonText.fontStyle = FontStyles.Bold;
        playButtonText.textWrappingMode = TextWrappingModes.Normal;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void HideLegacyRuntimeToolHandUI()
    {
        GameObject oldCanvas = GameObject.Find("NodeToolHandCanvas");

        if (oldCanvas != null)
        {
            oldCanvas.SetActive(false);
            Debug.Log("Node3PlacementPlayController: hidden NodeToolHandCanvas for manual central table.");
        }
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
        tmp.fontSizeMin = 10f;
        tmp.fontSizeMax = fontSize;

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        return tmp;
    }

    private Image CreateImage(string name, Transform parent, Vector2 anchor, Vector2 size)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        return image;
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
