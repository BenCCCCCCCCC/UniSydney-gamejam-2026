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

public class CardBackpackController : MonoBehaviour
{
    private enum HandCardCropAlignment
    {
        ShowUpperPart,
        ShowCenterPart,
        ShowLowerPart
    }

    private const string DefaultNodeID = "Node1";
    private const string DefaultTargetSceneName = "Node1_QueenCastle";
    private const string CardBackpackSceneName = "CardBackpackTest";
    private static readonly Vector2 DefaultBaseCardSize = new Vector2(145f, 205f);
    private static readonly Vector2 DefaultBaseCardSpacing = new Vector2(18f, 18f);
    private const int DefaultMaxBaseCardsPerRow = 8;

    [Header("Data")]
    [SerializeField] private CardDatabase database;
    [SerializeField] private string currentNodeId = DefaultNodeID;
    [SerializeField] private bool loadTargetSceneOnContinue = true;
    [SerializeField] private string targetSceneName = DefaultTargetSceneName;

    [Header("UI")]
    [SerializeField] private RectTransform baseCardArea;
    [SerializeField] private RectTransform toolHandArea;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private Button continueButton;
    [SerializeField] private CardView cardViewPrefab;

    [Header("Art Placeholders")]
    [SerializeField] private CardArtCatalog cardArtCatalog;
    [SerializeField] private bool useResourcesArtFallback = true;
    [SerializeField] private Sprite cardBackSprite;
    [SerializeField] private Sprite defaultBaseFrontSprite;
    [SerializeField] private Sprite defaultToolFrontSprite;

    [Header("Card Sizes")]
    [SerializeField] private Vector2 baseCardSize = DefaultBaseCardSize;
    [SerializeField] private Vector2 baseCardSpacing = DefaultBaseCardSpacing;
    [SerializeField] private int maxBaseCardsPerRow = DefaultMaxBaseCardsPerRow;
    [SerializeField] private RectOffset baseCardAreaPadding;
    [SerializeField] private bool useManualBaseCardSlots;
    [SerializeField] private Vector2 toolHandCardSize = new Vector2(120f, 160f);

    [Header("Tool Hand Crop")]
    [SerializeField] private HandCardCropAlignment handCardCropAlignment = HandCardCropAlignment.ShowUpperPart;

    [Header("Tool Hand Polish")]
    [SerializeField, Min(0f)] private float handCardOverlap = 70f;
    [SerializeField, Min(0.01f)] private float handCardHoverScale = 1.18f;
    [SerializeField] private float handCardHoverLiftY = 60f;
    [SerializeField, Min(0f)] private float handCardHoverAnimationDuration = 0.12f;
    [SerializeField] private float handCardHoverPreviewOffsetY = 80f;
    [SerializeField, Range(0f, 1f)] private float originalCardHoverAlpha = 0f;
    [SerializeField, Min(0f)] private float handCardHoverExitDuration = 0.16f;
    [SerializeField, Min(0f)] private float originalCardRestoreDuration = 0.12f;
    [SerializeField, Min(0f)] private float handCardHoverFadeDuration = 0.12f;

    [Header("Timing")]
    [SerializeField] private float previewSeconds = 3f;

    private readonly List<CardView> baseCardViews = new();
    private readonly List<CardView> openedCards = new();
    private readonly List<CardView> toolCards = new();

    private RectTransform autoBaseCardArea;
    private CardMergeEffectPlayer mergeEffectPlayer;
    private bool inputLocked;
    private bool isContinuing;

    private void Awake()
    {
        EnsureBaseCardAreaPadding();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying || toolHandArea == null)
        {
            return;
        }

        ConfigureToolHandClipping();
        RefreshHandCardHoverSettings();
        LayoutRebuilder.ForceRebuildLayoutImmediate(toolHandArea);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneLoadedHandler()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        BuildRuntimeTestSceneIfNeeded(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BuildRuntimeTestSceneIfNeeded(scene);
    }

    private static void BuildRuntimeTestSceneIfNeeded(Scene scene)
    {
        if (scene.name != CardBackpackSceneName)
        {
            return;
        }

        if (FindAnyObjectByType<CardBackpackController>() != null)
        {
            return;
        }

        RuntimeUiBuilder.Build();
    }

    private void Start()
    {
        EnsureBaseCardAreaPadding();
        ApplyGameSessionTarget();

        if (!HasRequiredReferences())
        {
            return;
        }

        ConfigureToolHandClipping();

        continueButton.gameObject.SetActive(true);
        continueButton.interactable = false;
        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(OnContinueClicked);

        StartCoroutine(BeginRound());
    }

    public void ConfigureRuntime(
        CardDatabase runtimeDatabase,
        RectTransform runtimeBaseCardArea,
        RectTransform runtimeToolHandArea,
        TMP_Text runtimeInstructionText,
        Button runtimeContinueButton)
    {
        database = runtimeDatabase;
        baseCardArea = runtimeBaseCardArea;
        toolHandArea = runtimeToolHandArea;
        instructionText = runtimeInstructionText;
        continueButton = runtimeContinueButton;
    }

    public void ApplySceneConfig(CardBackpackSceneConfig config)
    {
        if (config == null)
        {
            return;
        }

        cardArtCatalog = config.CardArtCatalog;
        cardBackSprite = config.CardBackSprite;
        useResourcesArtFallback = config.UseResourcesArtFallback;
        baseCardSize = config.BaseCardSize;
        baseCardSpacing = config.BaseCardSpacing;
        maxBaseCardsPerRow = Mathf.Max(1, config.MaxBaseCardsPerRow);
        baseCardAreaPadding = CopyPadding(config.BaseCardAreaPadding);
        toolHandCardSize = config.ToolHandCardSize;
        previewSeconds = Mathf.Max(0f, config.PreviewSeconds);
        useManualBaseCardSlots = config.UseManualBaseCardSlots;

        Debug.Log("CardBackpackController: applied CardBackpackSceneConfig.");
    }

    private void ApplyGameSessionTarget()
    {
        if (!string.IsNullOrWhiteSpace(GameSessionData.CurrentNodeID))
        {
            currentNodeId = GameSessionData.CurrentNodeID;
        }

        if (!string.IsNullOrWhiteSpace(GameSessionData.CurrentNodeSceneName))
        {
            targetSceneName = GameSessionData.CurrentNodeSceneName;
        }

        Debug.Log($"CardBackpackController target: node = {currentNodeId}, scene = {targetSceneName}");
    }

    private IEnumerator BeginRound()
    {
        inputLocked = true;
        UpdateContinueButtonState();

        ClearChildren(toolHandArea);
        baseCardViews.Clear();
        openedCards.Clear();
        toolCards.Clear();

        instructionText.text = "Memorize the base cards. They will flip soon.";

        List<CardRow> baseCards = database.GetBaseCardsForNode(currentNodeId);
        BaseCardSlotBinder slotBinder = useManualBaseCardSlots ? FindAnyObjectByType<BaseCardSlotBinder>() : null;
        if (useManualBaseCardSlots && slotBinder == null)
        {
            Debug.LogWarning("CardBackpackController: useManualBaseCardSlots is true, but no BaseCardSlotBinder was found. Using automatic layout.");
        }

        bool useManualSlots = useManualBaseCardSlots && slotBinder != null;
        List<CardRow> autoLayoutCards = GetAutoLayoutBaseCards(baseCards, slotBinder, useManualSlots);
        PrepareBaseCardLayout(autoLayoutCards.Count);

        foreach (CardRow card in baseCards)
        {
            bool placedInManualSlot = TryGetManualBaseCardParent(card, slotBinder, useManualSlots, out Transform parent);
            CardView view = CreateCardView(parent, baseCardSize);
            if (placedInManualSlot)
            {
                ApplyManualSlotLayout(view, parent as RectTransform);
            }

            view.Setup(
                card,
                true,
                false,
                OnBaseCardClicked,
                cardBackSprite,
                CardArtLoader.GetSprite(card.CardID, cardArtCatalog, useResourcesArtFallback),
                null,
                new Color(0.92f, 0.82f, 0.62f, 1f),
                new Color(0.16f, 0.18f, 0.25f, 1f));

            baseCardViews.Add(view);
        }

        yield return new WaitForSeconds(previewSeconds);

        foreach (CardView view in baseCardViews)
        {
            if (view == null || view.IsRemoved)
            {
                continue;
            }

            view.SetFaceUp(false);
            view.SetClickable(true);
        }

        inputLocked = false;
        instructionText.text = "Flip two base cards to craft a tool. Continue anytime if you have enough tools.";
        UpdateContinueButtonState();
    }

    private void OnBaseCardClicked(CardView view)
    {
        if (inputLocked || view == null || view.IsRemoved || view.IsFaceUp)
        {
            return;
        }

        if (!baseCardViews.Contains(view))
        {
            return;
        }

        if (openedCards.Count >= 2)
        {
            return;
        }

        view.SetFaceUp(true);
        view.SetClickable(false);
        openedCards.Add(view);

        if (openedCards.Count == 2)
        {
            StartCoroutine(ResolveOpenedPair());
        }
    }

    private IEnumerator ResolveOpenedPair()
    {
        inputLocked = true;
        UpdateContinueButtonState();

        SetRemainingBaseCardsClickable(false);
        yield return new WaitForSeconds(0.15f);

        CardView first = openedCards[0];
        CardView second = openedCards[1];

        bool success = database.TryCombine(
            first.Card.CardID,
            second.Card.CardID,
            out CardRow outputCard,
            out RecipeRow recipe);

        if (success)
        {
            string outputName = CardDisplayNameHelper.ToEnglishName(outputCard.CardID);
            instructionText.text = $"Crafted: {outputName}";

            Sprite toolSprite = CardArtLoader.GetSprite(outputCard.CardID, cardArtCatalog, useResourcesArtFallback);
            CardMergeEffectPlayer effectPlayer = GetOrCreateMergeEffectPlayer();
            Canvas canvas = toolHandArea.GetComponentInParent<Canvas>();
            CardView resultVisualSource = CreateCardView(canvas.transform, toolHandCardSize);
            resultVisualSource.Setup(
                outputCard,
                true,
                false,
                null,
                cardBackSprite,
                toolSprite,
                null,
                new Color(0.66f, 0.78f, 0.95f, 1f),
                new Color(0.16f, 0.18f, 0.25f, 1f));

            CanvasGroup sourceCanvasGroup = resultVisualSource.GetComponent<CanvasGroup>();
            sourceCanvasGroup.alpha = 0f;
            sourceCanvasGroup.interactable = false;
            sourceCanvasGroup.blocksRaycasts = false;

            Vector3 handTargetWorldPosition = CalculatePendingHandCardWorldPosition();
            if (effectPlayer != null)
            {
                yield return effectPlayer.PlayMerge(
                    first,
                    second,
                    resultVisualSource,
                    handTargetWorldPosition);
            }

            Destroy(resultVisualSource.gameObject);

            baseCardViews.Remove(first);
            baseCardViews.Remove(second);
            Destroy(first.gameObject);
            Destroy(second.gameObject);

            CardView toolView = CreateCardView(toolHandArea);
            toolView.Setup(
                outputCard,
                true,
                false,
                null,
                cardBackSprite,
                toolSprite,
                null,
                new Color(0.66f, 0.78f, 0.95f, 1f),
                new Color(0.16f, 0.18f, 0.25f, 1f));

            ConfigureHandCardHover(toolView);
            toolCards.Add(toolView);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(toolHandArea);
        }
        else
        {
            string firstName = CardDisplayNameHelper.ToEnglishName(first.Card.CardID);
            string secondName = CardDisplayNameHelper.ToEnglishName(second.Card.CardID);
            instructionText.text = $"No recipe: {firstName} + {secondName}";

            StartCoroutine(first.Shake());
            StartCoroutine(second.Shake());
            yield return new WaitForSeconds(0.34f);

            first.SetFaceUp(false);
            second.SetFaceUp(false);
        }

        openedCards.Clear();
        inputLocked = false;
        SetRemainingBaseCardsClickable(true);

        if (!HasAnyRemainingRecipe())
        {
            instructionText.text = "No more valid pairs. Continue when ready.";
        }

        UpdateContinueButtonState();
    }

    private void SetRemainingBaseCardsClickable(bool clickable)
    {
        foreach (CardView view in baseCardViews)
        {
            if (view == null || view.IsRemoved || view.IsFaceUp)
            {
                continue;
            }

            view.SetClickable(clickable);
        }
    }

    private bool HasAnyRemainingRecipe()
    {
        for (int i = 0; i < baseCardViews.Count; i++)
        {
            CardView first = baseCardViews[i];

            if (first == null || first.IsRemoved)
            {
                continue;
            }

            for (int j = i + 1; j < baseCardViews.Count; j++)
            {
                CardView second = baseCardViews[j];

                if (second == null || second.IsRemoved)
                {
                    continue;
                }

                if (database.TryCombine(first.Card.CardID, second.Card.CardID, out _, out _))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void UpdateContinueButtonState()
    {
        if (continueButton == null)
        {
            return;
        }

        continueButton.gameObject.SetActive(true);
        continueButton.interactable = !inputLocked && !isContinuing;
    }

    private List<CardRow> GetAutoLayoutBaseCards(List<CardRow> baseCards, BaseCardSlotBinder slotBinder, bool useManualSlots)
    {
        var autoCards = new List<CardRow>();

        foreach (CardRow card in baseCards)
        {
            if (card == null)
            {
                continue;
            }

            if (useManualSlots && slotBinder.TryGetSlot(card.CardID, out _))
            {
                continue;
            }

            autoCards.Add(card);
        }

        return autoCards;
    }

    private bool TryGetManualBaseCardParent(CardRow card, BaseCardSlotBinder slotBinder, bool useManualSlots, out Transform parent)
    {
        parent = autoBaseCardArea != null ? autoBaseCardArea : baseCardArea;

        if (!useManualSlots)
        {
            return false;
        }

        if (slotBinder.TryGetSlot(card.CardID, out RectTransform slot))
        {
            parent = slot;
            return true;
        }

        Debug.LogWarning($"BaseCardSlotBinder: no slot bound for {card.CardID}; using automatic fallback layout.");
        return false;
    }

    private void PrepareBaseCardLayout(int autoLayoutCardCount)
    {
        GridLayoutGroup baseGrid = baseCardArea.GetComponent<GridLayoutGroup>();
        if (baseGrid != null)
        {
            baseGrid.enabled = false;
        }

        autoBaseCardArea = GetOrCreateAutoBaseCardArea();
        ClearChildren(autoBaseCardArea);
        ConfigureGridLayout(autoBaseCardArea, autoLayoutCardCount);
    }

    private RectTransform GetOrCreateAutoBaseCardArea()
    {
        Transform existing = baseCardArea.Find("AutoBaseCardLayoutArea");
        if (existing != null && existing.TryGetComponent(out RectTransform existingRect))
        {
            return existingRect;
        }

        GameObject areaObject = new GameObject("AutoBaseCardLayoutArea", typeof(RectTransform));
        areaObject.transform.SetParent(baseCardArea, false);

        RectTransform rect = areaObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return rect;
    }

    private void ConfigureGridLayout(RectTransform area, int cardCount)
    {
        GridLayoutGroup grid = area.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = area.gameObject.AddComponent<GridLayoutGroup>();
        }

        grid.enabled = true;
        grid.cellSize = baseCardSize;
        grid.spacing = baseCardSpacing;
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, Mathf.Min(Mathf.Max(1, maxBaseCardsPerRow), Mathf.Max(1, cardCount)));
        grid.padding = GetBaseCardAreaPadding();
    }

    private RectOffset GetBaseCardAreaPadding()
    {
        EnsureBaseCardAreaPadding();
        return baseCardAreaPadding;
    }

    private void EnsureBaseCardAreaPadding()
    {
        if (baseCardAreaPadding == null)
        {
            baseCardAreaPadding = new RectOffset(16, 16, 16, 16);
        }
    }

    private static RectOffset CopyPadding(RectOffset source)
    {
        if (source == null)
        {
            return new RectOffset(16, 16, 16, 16);
        }

        return new RectOffset(source.left, source.right, source.top, source.bottom);
    }

    private void ApplyManualSlotLayout(CardView view, RectTransform slot)
    {
        if (view == null || slot == null)
        {
            return;
        }

        RectTransform rect = view.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = baseCardSize;
    }

    private CardView CreateCardView(Transform parent)
    {
        Vector2 cardSize = parent == toolHandArea ? toolHandCardSize : baseCardSize;
        return CreateCardView(parent, cardSize);
    }

    private CardView CreateCardView(Transform parent, Vector2 cardSize)
    {
        CardView view;

        if (cardViewPrefab != null)
        {
            view = Instantiate(cardViewPrefab, parent);
            ApplyCardLayoutSize(view.gameObject, cardSize);
        }
        else
        {
            GameObject cardObject = new GameObject(
                "CardView",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(CanvasGroup),
                typeof(CardView));

            cardObject.transform.SetParent(parent, false);
            ApplyCardLayoutSize(cardObject, cardSize);

            Image hitImage = cardObject.GetComponent<Image>();
            hitImage.color = new Color(1f, 1f, 1f, 0f);

            view = cardObject.GetComponent<CardView>();
        }

        return view;
    }

    private void ApplyCardLayoutSize(GameObject cardObject, Vector2 size)
    {
        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        cardRect.sizeDelta = size;

        LayoutElement layoutElement = cardObject.GetComponent<LayoutElement>();

        if (layoutElement == null)
        {
            layoutElement = cardObject.AddComponent<LayoutElement>();
        }

        layoutElement.preferredWidth = size.x;
        layoutElement.preferredHeight = size.y;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;
    }

    private void ConfigureToolHandClipping()
    {
        if (toolHandArea == null)
        {
            return;
        }

        HorizontalLayoutGroup handLayout = toolHandArea.GetComponent<HorizontalLayoutGroup>();
        if (handLayout != null)
        {
            handLayout.spacing = -handCardOverlap;
            handLayout.childControlWidth = false;
            handLayout.childControlHeight = false;
            handLayout.childForceExpandWidth = false;
            handLayout.childForceExpandHeight = false;
            handLayout.childAlignment = GetHandCardLayoutAlignment();
        }

        if (toolHandArea.GetComponent<RectMask2D>() == null)
        {
            toolHandArea.gameObject.AddComponent<RectMask2D>();
        }
    }

    private void ConfigureHandCardHover(CardView handCard)
    {
        if (handCard == null)
        {
            return;
        }

        Canvas canvas = toolHandArea.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        HandCardHoverEffect hoverEffect = handCard.GetComponent<HandCardHoverEffect>();
        if (hoverEffect == null)
        {
            hoverEffect = handCard.gameObject.AddComponent<HandCardHoverEffect>();
        }

        hoverEffect.Configure(
            canvas,
            handCardHoverScale,
            handCardHoverLiftY,
            handCardHoverAnimationDuration,
            handCardHoverPreviewOffsetY,
            originalCardHoverAlpha,
            handCardHoverExitDuration,
            originalCardRestoreDuration,
            handCardHoverFadeDuration);
    }

    private void RefreshHandCardHoverSettings()
    {
        foreach (CardView handCard in toolCards)
        {
            if (handCard != null)
            {
                ConfigureHandCardHover(handCard);
            }
        }
    }

    private TextAnchor GetHandCardLayoutAlignment()
    {
        return handCardCropAlignment switch
        {
            HandCardCropAlignment.ShowUpperPart => TextAnchor.UpperCenter,
            HandCardCropAlignment.ShowLowerPart => TextAnchor.LowerCenter,
            _ => TextAnchor.MiddleCenter
        };
    }

    private Vector3 CalculatePendingHandCardWorldPosition()
    {
        HorizontalLayoutGroup handLayout = toolHandArea.GetComponent<HorizontalLayoutGroup>();
        Rect handRect = toolHandArea.rect;
        RectOffset padding = handLayout != null
            ? handLayout.padding
            : new RectOffset();
        float spacing = handLayout != null ? handLayout.spacing : 0f;
        int finalCardCount = toolCards.Count + 1;
        float totalCardsWidth = finalCardCount * toolHandCardSize.x;
        float totalSpacingWidth = Mathf.Max(0, finalCardCount - 1) * spacing;
        float contentWidth = totalCardsWidth + totalSpacingWidth;
        float innerWidth = Mathf.Max(0f, handRect.width - padding.horizontal);
        float firstCardLeft = handRect.xMin + padding.left + (innerWidth - contentWidth) * 0.5f;
        float targetX = firstCardLeft
            + (finalCardCount - 1) * (toolHandCardSize.x + spacing)
            + toolHandCardSize.x * 0.5f;

        float targetY = handCardCropAlignment switch
        {
            HandCardCropAlignment.ShowUpperPart => handRect.yMax - padding.top - toolHandCardSize.y * 0.5f,
            HandCardCropAlignment.ShowLowerPart => handRect.yMin + padding.bottom + toolHandCardSize.y * 0.5f,
            _ => handRect.center.y + (padding.bottom - padding.top) * 0.5f
        };

        return toolHandArea.TransformPoint(new Vector3(targetX, targetY, 0f));
    }

    private CardMergeEffectPlayer GetOrCreateMergeEffectPlayer()
    {
        if (mergeEffectPlayer != null)
        {
            return mergeEffectPlayer;
        }

        Canvas canvas = baseCardArea != null ? baseCardArea.GetComponentInParent<Canvas>() : null;
        if (canvas == null)
        {
            Debug.LogWarning("CardBackpackController: cannot create merge effect layer because no parent Canvas was found.");
            return null;
        }

        mergeEffectPlayer = FindAnyObjectByType<CardMergeEffectPlayer>(FindObjectsInactive.Include);
        if (mergeEffectPlayer != null)
        {
            mergeEffectPlayer.ConfigureForCanvas(canvas);
            return mergeEffectPlayer;
        }

        mergeEffectPlayer = CardMergeEffectPlayer.CreateRuntimeFallback(canvas.GetComponent<RectTransform>());
        return mergeEffectPlayer;
    }

    private bool HasRequiredReferences()
    {
        bool hasAllReferences = true;

        if (database == null)
        {
            Debug.LogError("CardBackpackController: database is missing.");
            hasAllReferences = false;
        }

        if (baseCardArea == null)
        {
            Debug.LogError("CardBackpackController: baseCardArea is missing.");
            hasAllReferences = false;
        }

        if (toolHandArea == null)
        {
            Debug.LogError("CardBackpackController: toolHandArea is missing.");
            hasAllReferences = false;
        }

        if (instructionText == null)
        {
            Debug.LogError("CardBackpackController: instructionText is missing.");
            hasAllReferences = false;
        }

        if (continueButton == null)
        {
            Debug.LogError("CardBackpackController: continueButton is missing.");
            hasAllReferences = false;
        }

        return hasAllReferences;
    }

    private void OnContinueClicked()
    {
        if (inputLocked || isContinuing)
        {
            return;
        }

        isContinuing = true;
        UpdateContinueButtonState();

        List<string> toolCardIDs = GetToolCardIDs();

        Debug.Log($"Continue clicked. Crafted tools count = {toolCardIDs.Count}");
        Debug.Log($"Saving tool IDs: {FormatToolIDs(toolCardIDs)}");

        GameSessionData.CurrentNodeID = currentNodeId;
        GameSessionData.CurrentNodeSceneName = targetSceneName;
        GameSessionData.EnterPlacementWithTools(toolCardIDs);

        Debug.Log($"CardBackpackController: continue with {toolCardIDs.Count} crafted tools.");
        instructionText.text = $"Continuing with {toolCardIDs.Count} crafted tools.";

        if (loadTargetSceneOnContinue && !string.IsNullOrWhiteSpace(targetSceneName))
        {
            LoadTargetScene();
        }
    }

    public List<string> GetToolCardIDs()
    {
        var result = new List<string>();

        foreach (CardView toolCard in toolCards)
        {
            if (toolCard == null || toolCard.Card == null || string.IsNullOrWhiteSpace(toolCard.Card.CardID))
            {
                continue;
            }

            result.Add(toolCard.Card.CardID);
        }

        return result;
    }

    private void LoadTargetScene()
    {
        Debug.Log($"Loading scene: {targetSceneName}");

#if UNITY_EDITOR
        string scenePath = $"Assets/Scenes/{targetSceneName}.unity";

        if (SceneUtility.GetBuildIndexByScenePath(scenePath) < 0)
        {
            EditorSceneManager.LoadSceneInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif

        SceneManager.LoadScene(targetSceneName);
    }

    private static string FormatToolIDs(List<string> toolCardIDs)
    {
        if (toolCardIDs == null || toolCardIDs.Count == 0)
        {
            return "(none)";
        }

        return string.Join(", ", toolCardIDs);
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    private static class RuntimeUiBuilder
    {
        public static void Build()
        {
            EnsureEventSystem();

            GameObject canvasObject = new GameObject(
                "Canvas",
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

            CreateStretchImage("DarkBackground", canvasRect, new Color(0.055f, 0.06f, 0.08f, 1f));

            TMP_Text instructionText = CreateText(
                "InstructionText",
                canvasRect,
                "Memorize the base cards. They will flip soon.",
                new Vector2(0.5f, 0.94f),
                new Vector2(1120f, 58f),
                30f);

            RectTransform baseCardArea = CreateArea(
                "BaseCardArea",
                canvasRect,
                new Vector2(0.06f, 0.25f),
                new Vector2(0.94f, 0.82f),
                new Color(0.12f, 0.13f, 0.17f, 0.78f));

            GridLayoutGroup grid = baseCardArea.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = DefaultBaseCardSize;
            grid.spacing = DefaultBaseCardSpacing;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = DefaultMaxBaseCardsPerRow;
            grid.padding = new RectOffset(16, 16, 16, 16);

            RectTransform toolHandArea = CreateArea(
                "ToolHandArea",
                canvasRect,
                new Vector2(0.22f, 0.035f),
                new Vector2(0.78f, 0.215f),
                new Color(0.06f, 0.07f, 0.09f, 0.9f));

            HorizontalLayoutGroup handLayout = toolHandArea.gameObject.AddComponent<HorizontalLayoutGroup>();
            handLayout.spacing = -70f;
            handLayout.childAlignment = TextAnchor.UpperCenter;
            handLayout.childControlWidth = false;
            handLayout.childControlHeight = false;
            handLayout.childForceExpandWidth = false;
            handLayout.childForceExpandHeight = false;
            toolHandArea.gameObject.AddComponent<RectMask2D>();

            Button continueButton = CreateButton(
                "ContinueButton",
                canvasRect,
                "Continue",
                new Vector2(0.88f, 0.12f),
                new Vector2(230f, 72f));

            continueButton.gameObject.SetActive(true);

            GameObject controllerObject = new GameObject("CardBackpackController");
            CardDatabase database = controllerObject.AddComponent<CardDatabase>();
            CardBackpackController controller = controllerObject.AddComponent<CardBackpackController>();
            controller.ConfigureRuntime(database, baseCardArea, toolHandArea, instructionText, continueButton);

            CardBackpackSceneConfig config = FindAnyObjectByType<CardBackpackSceneConfig>();
            if (config != null)
            {
                controller.ApplySceneConfig(config);
            }
        }

        private static void EnsureEventSystem()
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

        private static RectTransform CreateStretchImage(string name, Transform parent, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.GetComponent<Image>();
            image.color = color;

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return rect;
        }

        private static RectTransform CreateArea(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject areaObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            areaObject.transform.SetParent(parent, false);

            Image image = areaObject.GetComponent<Image>();
            image.color = color;

            RectTransform rect = areaObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return rect;
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

        private static Button CreateButton(string name, Transform parent, string label, Vector2 anchor, Vector2 size)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.24f, 0.44f, 0.58f, 1f);

            Button button = buttonObject.GetComponent<Button>();

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;

            TMP_Text text = CreateText(
                "Label",
                buttonObject.transform,
                label,
                new Vector2(0.5f, 0.5f),
                size,
                26f);

            text.color = Color.white;

            return button;
        }
    }
}
