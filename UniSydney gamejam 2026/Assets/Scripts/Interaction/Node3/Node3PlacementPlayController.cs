using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class Node3PlacementPlayController : MonoBehaviour
{
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

    private GameObject tableCanvasObject;
    private GameObject playButtonCanvasObject;
    private RectTransform canvasRect;
    private RectTransform handPanel;
    private RectTransform[] slotRects;
    private Node3CentralToolCardDragItem[] placedCards;
    private CardDatabase runtimeCardDatabase;
    private HandCardPresentationSettings handPresentationSettings;

    private bool hasStartedPlay;

    private IEnumerator Start()
    {
        if (storyActor != null)
        {
            storyActor.PauseMove();
        }

        ApplySceneConfig(FindAnyObjectByType<NodeToolHandSceneConfig>());

        if (GameSessionData.CurrentPhase != GameFlowPhase.Placement)
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

        requiredPlacementPoints = nodePoints.ToArray();
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

        Canvas canvas = tableCanvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = tableCanvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasRect = tableCanvasObject.GetComponent<RectTransform>();

        BuildSlotPanel();
        BuildHandPanel();
    }

    private void BuildSlotPanel()
    {
        GameObject panelObject = new GameObject(
            "CentralPlacementSlots",
            typeof(RectTransform),
            typeof(Image),
            typeof(HorizontalLayoutGroup));

        panelObject.transform.SetParent(canvasRect, false);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.04f, 0.05f, 0.06f, 0.45f);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = slotPanelAnchor;
        panelRect.anchorMax = slotPanelAnchor;
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = slotPanelSize;
        panelRect.anchoredPosition = Vector2.zero;

        HorizontalLayoutGroup layout = panelObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = slotSpacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        int slotCount = Mathf.Max(3, requiredPlacementPoints != null ? requiredPlacementPoints.Length : 3);

        slotRects = new RectTransform[slotCount];
        placedCards = new Node3CentralToolCardDragItem[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            string pointID = i < requiredPlacementPoints.Length && requiredPlacementPoints[i] != null
                ? requiredPlacementPoints[i].placePointID
                : $"N3_P{i + 1}";

            CreateSlot(panelObject.transform, i, pointID);
        }
    }

    private void CreateSlot(Transform parent, int slotIndex, string pointID)
    {
        GameObject slotObject = new GameObject(
            $"Point{slotIndex + 1}_CardSlot",
            typeof(RectTransform),
            typeof(Image),
            typeof(LayoutElement),
            typeof(Node3CentralCardSlot));

        slotObject.transform.SetParent(parent, false);

        RectTransform rect = slotObject.GetComponent<RectTransform>();
        rect.sizeDelta = slotSize;
        slotRects[slotIndex] = rect;

        LayoutElement layoutElement = slotObject.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = slotSize.x;
        layoutElement.preferredHeight = slotSize.y;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;

        Image image = slotObject.GetComponent<Image>();
        image.color = new Color(0.16f, 0.18f, 0.22f, 0.72f);

        Node3CentralCardSlot slot = slotObject.GetComponent<Node3CentralCardSlot>();
        slot.Setup(this, slotIndex);

        TMP_Text label = CreateText(
            "PointLabel",
            slotObject.transform,
            $"Point {slotIndex + 1}\n{pointID}\nDrop card here",
            new Vector2(0.5f, 0.5f),
            new Vector2(slotSize.x - 20f, slotSize.y - 20f),
            22f);

        label.color = new Color(1f, 0.92f, 0.62f, 1f);
        label.fontStyle = FontStyles.Bold;
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
        point.SetTool(card.ToolCardID);

        card.PlaceInSlot(slotIndex, slotRects[slotIndex]);

        Debug.Log($"NODE3_MANUAL_PLACE: {card.ToolCardID} -> {point.placePointID}");

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
