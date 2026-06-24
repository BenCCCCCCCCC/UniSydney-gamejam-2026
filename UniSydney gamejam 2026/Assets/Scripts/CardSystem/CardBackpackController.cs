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
    private const string DefaultNodeID = "Node1";
    private const string DefaultTargetSceneName = "Node1_QueenCastle";
    private const string CardBackpackSceneName = "CardBackpackTest";

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
    [SerializeField] private Vector2 baseCardSize = new Vector2(118f, 168f);
    [SerializeField] private Vector2 toolHandCardSize = new Vector2(120f, 160f);

    [Header("Timing")]
    [SerializeField] private float previewSeconds = 3f;

    private readonly List<CardView> baseCardViews = new();
    private readonly List<CardView> openedCards = new();
    private readonly List<CardView> toolCards = new();

    private bool inputLocked;
    private bool isContinuing;

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
        ApplyGameSessionTarget();

        if (!HasRequiredReferences())
        {
            return;
        }

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

        ClearChildren(baseCardArea);
        ClearChildren(toolHandArea);
        baseCardViews.Clear();
        openedCards.Clear();
        toolCards.Clear();

        instructionText.text = "Memorize the base cards. They will flip soon.";

        List<CardRow> baseCards = database.GetBaseCardsForNode(currentNodeId);

        foreach (CardRow card in baseCards)
        {
            CardView view = CreateCardView(baseCardArea);
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

            StartCoroutine(first.PlayDisappear());
            StartCoroutine(second.PlayDisappear());
            yield return new WaitForSeconds(0.24f);

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
                CardArtLoader.GetSprite(outputCard.CardID, cardArtCatalog, useResourcesArtFallback),
                null,
                new Color(0.66f, 0.78f, 0.95f, 1f),
                new Color(0.16f, 0.18f, 0.25f, 1f));

            toolCards.Add(toolView);
            StartCoroutine(toolView.PlayAppear());
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

    private CardView CreateCardView(Transform parent)
    {
        CardView view;
        Vector2 cardSize = parent == toolHandArea ? toolHandCardSize : baseCardSize;

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
            grid.cellSize = new Vector2(118f, 168f);
            grid.spacing = new Vector2(14f, 14f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            RectTransform toolHandArea = CreateArea(
                "ToolHandArea",
                canvasRect,
                new Vector2(0.22f, 0.035f),
                new Vector2(0.78f, 0.215f),
                new Color(0.06f, 0.07f, 0.09f, 0.9f));

            HorizontalLayoutGroup handLayout = toolHandArea.gameObject.AddComponent<HorizontalLayoutGroup>();
            handLayout.spacing = 24f;
            handLayout.childAlignment = TextAnchor.MiddleCenter;
            handLayout.childControlWidth = true;
            handLayout.childControlHeight = true;
            handLayout.childForceExpandWidth = false;
            handLayout.childForceExpandHeight = false;

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
