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

    [System.Serializable]
    private class NodeContinueRequirement
    {
        public string nodeID = "Node4";
        [Min(0)] public int minimumCraftedTools = 1;
    }

    private const string DefaultNodeID = "Node1";
    private const string DefaultTargetSceneName = "Node1_QueenCastle";
    private const string CardBackpackSceneName = "CardBackpackTest";
    private const string DefaultInstruction =
        "Memorize the cards, then create the right tools.";
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
    [SerializeField] private Vector2 toolHandCardSize = new Vector2(210f, 294f);

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

    [Header("Continue Requirement")]
    [SerializeField, Min(0)] private int minimumCraftedToolsToContinue = 3;
    [SerializeField] private bool requireMinimumCraftedToolsToContinue = true;
    [SerializeField] private List<NodeContinueRequirement> nodeRequirementOverrides = new()
    {
        new NodeContinueRequirement { nodeID = "Node4", minimumCraftedTools = 1 }
    };

    [Header("Scene Transition")]
    [SerializeField] private bool useSceneTransitionOverlay = true;
    [SerializeField] private Color transitionColor = Color.black;
    [SerializeField] private float transitionFadeInSeconds = 0.08f;
    [SerializeField] private float transitionFadeOutSeconds = 0.18f;
    [SerializeField] private int transitionWaitFramesAfterLoad = 2;

    private readonly List<CardView> baseCardViews = new();
    private readonly List<CardView> roundBaseCardViews = new();
    private readonly List<CardView> openedCards = new();
    private readonly List<CardView> toolCards = new();

    private RectTransform autoBaseCardArea;
    private CardMergeEffectPlayer mergeEffectPlayer;
    private MemoryCountdownUI memoryCountdownUI;
    private Canvas memoryCountdownFallbackCanvas;
    private HandCardPresentationSettings handPresentationSettings;
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

        ApplySharedHandPresentation();
        LayoutRebuilder.ForceRebuildLayoutImmediate(toolHandArea);
    }

    private void OnDisable()
    {
        CleanupMemoryCountdown();
        GameSessionData.ClearCardBackpackBackgroundSnapshot();
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
        Debug.Log($"CardBackpackController continue requirement: node={currentNodeId}, requiredTools={GetRequiredCraftedToolCount()}");

        if (!HasRequiredReferences())
        {
            return;
        }

        ConfigureTopInstructionText();
        ApplySharedHandPresentation();

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
        toolHandCardSize = HandCardPresentationApplier.ResolveCardSize(config.ToolHandCardSize);
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
        CleanupRoundBaseCardViews();
        baseCardViews.Clear();
        openedCards.Clear();
        toolCards.Clear();

        instructionText.text = DefaultInstruction;

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
            roundBaseCardViews.Add(view);
        }

        yield return RunMemoryCountdown();

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
        instructionText.text = GetContinueRequirementMessage();
        UpdateContinueButtonState();
    }

    private IEnumerator RunMemoryCountdown()
    {
        CleanupMemoryCountdown();

        Canvas runtimeCanvas = GetRuntimeCanvas();
        if (runtimeCanvas == null)
        {
            memoryCountdownFallbackCanvas = MemoryCountdownUI.CreateFallbackCanvas();
            runtimeCanvas = memoryCountdownFallbackCanvas;
            Debug.LogWarning("CardBackpackController: no parent Canvas was available; using a temporary countdown fallback Canvas.");
        }

        memoryCountdownUI = MemoryCountdownUI.Create(runtimeCanvas);
        HideMemoryCountdownPromptVisuals();

        if (previewSeconds <= 0f)
        {
            memoryCountdownUI?.SetProgress(0f);
            CleanupMemoryCountdown();
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < previewSeconds)
        {
            elapsed += Time.deltaTime;
            float remaining = 1f - Mathf.Clamp01(elapsed / previewSeconds);
            memoryCountdownUI?.SetProgress(remaining);
            yield return null;
        }

        memoryCountdownUI?.SetProgress(0f);
        CleanupMemoryCountdown();
    }

    private void HideMemoryCountdownPromptVisuals()
    {
        if (memoryCountdownUI == null)
        {
            return;
        }

        Transform panel = memoryCountdownUI.transform.Find("Panel");
        if (panel != null)
        {
            panel.gameObject.SetActive(false);
        }

        Transform message = memoryCountdownUI.transform.Find("Message");
        if (message != null)
        {
            message.gameObject.SetActive(false);
        }
    }

    private Canvas GetRuntimeCanvas()
    {
        Canvas canvas = baseCardArea != null
            ? baseCardArea.GetComponentInParent<Canvas>()
            : null;

        if (canvas == null && toolHandArea != null)
        {
            canvas = toolHandArea.GetComponentInParent<Canvas>();
        }

        if (canvas == null && instructionText != null)
        {
            canvas = instructionText.GetComponentInParent<Canvas>();
        }

        if (canvas == null && continueButton != null)
        {
            canvas = continueButton.GetComponentInParent<Canvas>();
        }

        if (canvas == null)
        {
            canvas = FindAnyObjectByType<Canvas>();
        }

        return canvas;
    }

    private void CleanupMemoryCountdown()
    {
        if (memoryCountdownUI != null)
        {
            memoryCountdownUI.HideAndDestroy();
            memoryCountdownUI = null;
        }

        if (memoryCountdownFallbackCanvas != null)
        {
            Destroy(memoryCountdownFallbackCanvas.gameObject);
            memoryCountdownFallbackCanvas = null;
        }
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
            CardView resultVisualSource = HandCardViewFactory.Create(
                canvas.transform,
                outputCard,
                cardBackSprite,
                toolSprite,
                handPresentationSettings,
                canvas,
                cardViewPrefab: cardViewPrefab,
                enableCorrectToolHint: false);

            CanvasGroup sourceCanvasGroup = resultVisualSource.GetComponent<CanvasGroup>();
            sourceCanvasGroup.alpha = 0f;
            sourceCanvasGroup.interactable = false;
            sourceCanvasGroup.blocksRaycasts = false;

            Vector3 handTargetWorldPosition = CalculatePendingHandCardWorldPosition();
            bool isCorrectHintTool =
                CorrectToolHintRules.IsCorrectSolutionTool(
                    currentNodeId,
                    outputCard.CardID);
            Debug.Log(
                $"CORRECT_TOOL_MERGE_CHECK: node={currentNodeId}, "
                + $"result={outputCard.CardID}, correct={isCorrectHintTool}");

            if (effectPlayer != null)
            {
                yield return effectPlayer.PlayMerge(
                    first,
                    second,
                    resultVisualSource,
                    handTargetWorldPosition,
                    isCorrectHintTool);
            }

            Destroy(resultVisualSource.gameObject);

            baseCardViews.Remove(first);
            baseCardViews.Remove(second);
            PreserveConsumedBaseCardSlot(first);
            PreserveConsumedBaseCardSlot(second);

            CardView toolView = HandCardViewFactory.Create(
                toolHandArea,
                outputCard,
                cardBackSprite,
                toolSprite,
                handPresentationSettings,
                canvas,
                cardViewPrefab: cardViewPrefab);
            toolCards.Add(toolView);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(toolHandArea);

            instructionText.text = $"Crafted: {outputName}. {GetContinueRequirementMessage()}";
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
            instructionText.text = HasEnoughCraftedToolsToContinue()
                ? "No more valid pairs. Continue when ready."
                : $"No more valid pairs. {GetContinueRequirementMessage()}";
        }
        else if (!HasEnoughCraftedToolsToContinue())
        {
            instructionText.text = GetContinueRequirementMessage();
        }

        UpdateContinueButtonState();
    }

    private void ConfigureTopInstructionText()
    {
        if (instructionText == null)
        {
            return;
        }

        instructionText.textWrappingMode = TextWrappingModes.NoWrap;
        instructionText.overflowMode = TextOverflowModes.Ellipsis;
        instructionText.enableAutoSizing = true;
        instructionText.fontSizeMax = Mathf.Min(30f, instructionText.fontSize);
        instructionText.fontSizeMin = 18f;

        RectTransform rect = instructionText.rectTransform;
        Vector2 anchorMin = rect.anchorMin;
        Vector2 anchorMax = rect.anchorMax;
        anchorMin.y = 0.965f;
        anchorMax.y = 0.965f;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(rect.pivot.x, 0.5f);
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 0f);
        rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, 1120f), 44f);
    }

    private void PreserveConsumedBaseCardSlot(CardView view)
    {
        if (view == null)
        {
            return;
        }

        view.SetClickable(false);

        CanvasGroup group = view.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = view.gameObject.AddComponent<CanvasGroup>();
        }

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void CleanupRoundBaseCardViews()
    {
        foreach (CardView view in roundBaseCardViews)
        {
            if (view == null)
            {
                continue;
            }

            view.gameObject.SetActive(false);

            bool isInAutoLayout = autoBaseCardArea != null
                && view.transform.IsChildOf(autoBaseCardArea);

            if (isInAutoLayout)
            {
                continue;
            }

            Destroy(view.gameObject);
        }

        roundBaseCardViews.Clear();
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
        continueButton.interactable =
            !inputLocked
            && !isContinuing
            && HasEnoughCraftedToolsToContinue();
    }

    private int GetCraftedToolCount()
    {
        int count = 0;

        foreach (CardView toolCard in toolCards)
        {
            if (toolCard == null || toolCard.Card == null || string.IsNullOrWhiteSpace(toolCard.Card.CardID))
            {
                continue;
            }

            count++;
        }

        return count;
    }

    private bool HasEnoughCraftedToolsToContinue()
    {
        return GetCraftedToolCount() >= GetRequiredCraftedToolCount();
    }

    private string GetContinueRequirementMessage()
    {
        int requiredCount = GetRequiredCraftedToolCount();
        int craftedCount = GetCraftedToolCount();
        int remaining = Mathf.Max(0, requiredCount - craftedCount);

        if (!requireMinimumCraftedToolsToContinue || remaining <= 0)
        {
            return "Ready to continue.";
        }

        if (remaining == 1)
        {
            return $"Craft {remaining} more tool to continue. ({craftedCount}/{requiredCount})";
        }

        return $"Craft {remaining} more tools to continue. ({craftedCount}/{requiredCount})";
    }

    private int GetRequiredCraftedToolCount()
    {
        if (!requireMinimumCraftedToolsToContinue)
        {
            return 0;
        }

        if (nodeRequirementOverrides != null)
        {
            foreach (NodeContinueRequirement requirement in nodeRequirementOverrides)
            {
                if (requirement == null || string.IsNullOrWhiteSpace(requirement.nodeID))
                {
                    continue;
                }

                if (string.Equals(requirement.nodeID, currentNodeId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return Mathf.Max(0, requirement.minimumCraftedTools);
                }
            }
        }

        return Mathf.Max(0, minimumCraftedToolsToContinue);
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

    private void ApplySharedHandPresentation()
    {
        if (toolHandArea == null)
        {
            return;
        }

        handPresentationSettings = CreateHandPresentationSettings();
        HandCardPresentationApplier.PublishSettings(handPresentationSettings);
        HandCardPresentationApplier.ApplyHandArea(toolHandArea, handPresentationSettings);

        Canvas canvas = toolHandArea.GetComponentInParent<Canvas>();
        foreach (CardView handCard in toolCards)
        {
            if (handCard != null)
            {
                HandCardPresentationApplier.ApplyHandCard(
                    handCard.gameObject,
                    canvas,
                    handPresentationSettings);
            }
        }
    }

    private HandCardPresentationSettings CreateHandPresentationSettings()
    {
        HandCardPresentationSettings settings = new HandCardPresentationSettings
        {
            CardSize = HandCardPresentationApplier.ResolveCardSize(toolHandCardSize),
            Overlap = handCardOverlap,
            CropAlignment = handCardCropAlignment switch
            {
                HandCardCropAlignment.ShowUpperPart => HandCardVerticalCropAlignment.ShowUpperPart,
                HandCardCropAlignment.ShowLowerPart => HandCardVerticalCropAlignment.ShowLowerPart,
                _ => HandCardVerticalCropAlignment.ShowCenterPart
            },
            HoverScale = handCardHoverScale,
            HoverLiftY = handCardHoverLiftY,
            HoverEnterDuration = handCardHoverAnimationDuration,
            HoverPreviewOffsetY = handCardHoverPreviewOffsetY,
            OriginalCardHoverAlpha = originalCardHoverAlpha,
            HoverExitDuration = handCardHoverExitDuration,
            OriginalCardRestoreDuration = originalCardRestoreDuration,
            HoverFadeDuration = handCardHoverFadeDuration
        };

        return HandCardPresentationApplier.ResolveSettings(settings);
    }

    private Vector3 CalculatePendingHandCardWorldPosition()
    {
        return HandCardPresentationApplier.CalculatePendingCardWorldPosition(
            toolHandArea,
            toolCards.Count + 1,
            handPresentationSettings);
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

        if (!HasEnoughCraftedToolsToContinue())
        {
            string message = GetContinueRequirementMessage();
            Debug.Log($"Continue blocked: {message}");
            if (instructionText != null)
            {
                instructionText.text = message;
            }

            UpdateContinueButtonState();
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
            if (useSceneTransitionOverlay)
            {
                LoadTargetSceneWithTransition();
            }
            else
            {
                LoadTargetScene();
            }
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

    private void LoadTargetSceneWithTransition()
    {
        Debug.Log($"Loading scene with transition cover: {targetSceneName}");

#if UNITY_EDITOR
        string scenePath = $"Assets/Scenes/{targetSceneName}.unity";
        bool needsEditorFallback = SceneUtility.GetBuildIndexByScenePath(scenePath) < 0;
#else
        string scenePath = null;
        bool needsEditorFallback = false;
#endif

        SceneTransitionOverlay.LoadSceneCovered(
            targetSceneName,
            scenePath,
            needsEditorFallback,
            transitionColor,
            transitionFadeInSeconds,
            transitionFadeOutSeconds,
            transitionWaitFramesAfterLoad);
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

            Texture2D backgroundSnapshot = GameSessionData.CardBackpackBackgroundSnapshot;
            RectTransform snapshotLayer = null;
            if (backgroundSnapshot != null)
            {
                snapshotLayer = CreateStretchRawImage("GameplaySnapshotBackground", canvasRect, backgroundSnapshot);
            }

            Color dimColor = new Color(0f, 0f, 0f, 0.5f);
            RectTransform dimOverlay = CreateStretchImage("BackgroundDimOverlay", canvasRect, dimColor);
            RectTransform uiLayer = CreateStretchContainer("CardBackpackUILayer", canvasRect);
            ApplyBackgroundLayerOrder(snapshotLayer, dimOverlay, uiLayer, dimColor);

            TMP_Text instructionText = CreateText(
                "InstructionText",
                uiLayer,
                DefaultInstruction,
                new Vector2(0.5f, 0.965f),
                new Vector2(1120f, 44f),
                30f);

            RectTransform baseCardArea = CreateArea(
                "BaseCardArea",
                uiLayer,
                new Vector2(0.06f, 0.25f),
                new Vector2(0.94f, 0.82f),
                Color.clear);

            GridLayoutGroup grid = baseCardArea.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = DefaultBaseCardSize;
            grid.spacing = DefaultBaseCardSpacing;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = DefaultMaxBaseCardsPerRow;
            grid.padding = new RectOffset(16, 16, 16, 16);

            RectTransform toolHandArea = CreateArea(
                "ToolHandArea",
                uiLayer,
                new Vector2(0.22f, 0f),
                new Vector2(0.78f, 0.18f),
                Color.clear);

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
                uiLayer,
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

        private static void ApplyBackgroundLayerOrder(
            RectTransform snapshotLayer,
            RectTransform dimOverlay,
            RectTransform uiLayer,
            Color dimColor)
        {
            if (snapshotLayer != null)
            {
                snapshotLayer.SetAsFirstSibling();
                dimOverlay.SetSiblingIndex(1);
            }
            else
            {
                dimOverlay.SetAsFirstSibling();
            }

            uiLayer.SetAsLastSibling();

            Debug.Log(
                $"CardBackpack background layers: snapshotSibling={(snapshotLayer != null ? snapshotLayer.GetSiblingIndex() : -1)}, dimSibling={dimOverlay.GetSiblingIndex()}, uiSibling={uiLayer.GetSiblingIndex()}, dimColor={dimColor}");
        }

        private static RectTransform CreateStretchContainer(string name, Transform parent)
        {
            GameObject containerObject = new GameObject(name, typeof(RectTransform));
            containerObject.transform.SetParent(parent, false);

            RectTransform rect = containerObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return rect;
        }

        private static RectTransform CreateStretchImage(string name, Transform parent, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.GetComponent<Image>();
            image.material = null;
            image.color = color;
            image.raycastTarget = false;

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return rect;
        }

        private static RectTransform CreateStretchRawImage(
            string name,
            Transform parent,
            Texture texture)
        {
            if (texture == null || texture.width <= 0 || texture.height <= 0)
            {
                Debug.LogWarning($"CardBackpackController: {name} texture is invalid; skipping snapshot background layer.");
                return null;
            }

            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(parent, false);

            RawImage image = imageObject.GetComponent<RawImage>();
            image.material = null;
            image.texture = texture;
            image.color = Color.white;
            image.raycastTarget = false;

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
