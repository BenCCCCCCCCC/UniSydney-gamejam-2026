using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EscapePauseMenuController : MonoBehaviour
{
    private const int PauseMenuSortingOrder = 999;
    private const float DimAlpha = 0.58f;

    private static EscapePauseMenuController instance;

    private Canvas pauseCanvas;
    private GameObject pauseCanvasObject;
    private GameObject fallbackEventSystemObject;
    private bool isPaused;
    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject controllerObject = new GameObject("EscapePauseMenuController");
        DontDestroyOnLoad(controllerObject);
        controllerObject.AddComponent<EscapePauseMenuController>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildPauseMenuCanvas();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    private void Update()
    {
        if (WasEscapePressedThisFrame())
        {
            TogglePauseMenu();
        }
    }

    private static bool WasEscapePressedThisFrame()
    {
        return Keyboard.current != null
            && Keyboard.current.escapeKey.wasPressedThisFrame;
    }

    private void TogglePauseMenu()
    {
        if (isPaused)
        {
            ClosePauseMenu();
        }
        else
        {
            OpenPauseMenu();
        }
    }

    private void OpenPauseMenu()
    {
        if (pauseCanvasObject == null)
        {
            BuildPauseMenuCanvas();
        }

        EnsureEventSystem();

        previousCursorLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        if (pauseCanvasObject != null)
        {
            pauseCanvasObject.SetActive(true);
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isPaused = true;
    }

    private void ClosePauseMenu()
    {
        if (pauseCanvasObject != null)
        {
            pauseCanvasObject.SetActive(false);
        }

        Time.timeScale = 1f;
        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;
        isPaused = false;
    }

    private void RestartCurrentScene()
    {
        Time.timeScale = 1f;
        RestoreCursorStateForLeavingMenu();
        isPaused = false;

        if (pauseCanvasObject != null)
        {
            pauseCanvasObject.SetActive(false);
        }

        int activeSceneBuildIndex = SceneManager.GetActiveScene().buildIndex;
        if (activeSceneBuildIndex >= 0)
        {
            SceneManager.LoadScene(activeSceneBuildIndex);
        }
        else
        {
            Debug.LogWarning("EscapePauseMenuController: current scene is not in Build Settings, restart skipped.");
        }
    }

    private void QuitGame()
    {
        Time.timeScale = 1f;
        RestoreCursorStateForLeavingMenu();
        isPaused = false;

#if UNITY_EDITOR
        Debug.Log("Quit Game");
#else
        Application.Quit();
#endif
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CleanupFallbackEventSystemIfSceneProvidesOne();

        if (!isPaused)
        {
            return;
        }

        ClosePauseMenu();
    }

    private void RestoreCursorStateForLeavingMenu()
    {
        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;
    }

    private void BuildPauseMenuCanvas()
    {
        if (pauseCanvasObject != null)
        {
            return;
        }

        pauseCanvasObject = new GameObject(
            "PauseMenuCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        DontDestroyOnLoad(pauseCanvasObject);

        pauseCanvas = pauseCanvasObject.GetComponent<Canvas>();
        pauseCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        pauseCanvas.sortingOrder = PauseMenuSortingOrder;

        CanvasScaler scaler = pauseCanvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = pauseCanvasObject.GetComponent<RectTransform>();

        CreateDimBackground(canvasRect);
        CreateButtonContainer(canvasRect);

        pauseCanvasObject.SetActive(false);
    }

    private static void CreateDimBackground(RectTransform parent)
    {
        GameObject dimObject = new GameObject(
            "DimBackground",
            typeof(RectTransform),
            typeof(Image));

        dimObject.transform.SetParent(parent, false);

        RectTransform rect = dimObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = dimObject.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, DimAlpha);
        image.raycastTarget = true;
    }

    private void CreateButtonContainer(RectTransform parent)
    {
        GameObject containerObject = new GameObject(
            "ButtonContainer",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup));

        containerObject.transform.SetParent(parent, false);

        RectTransform rect = containerObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(520f, 260f);

        VerticalLayoutGroup layout = containerObject.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 60f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateTextButton(
            "RestartButton",
            "Restart",
            containerObject.transform,
            RestartCurrentScene);

        CreateTextButton(
            "QuitButton",
            "Quit Game",
            containerObject.transform,
            QuitGame);
    }

    private static void CreateTextButton(
        string objectName,
        string label,
        Transform parent,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement));

        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(500f, 100f);

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = 500f;
        layoutElement.preferredHeight = 100f;

        Image image = buttonObject.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClick);

        GameObject textObject = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(TextMeshProUGUI));

        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = label;
        text.color = Color.white;
        text.fontSize = 60f;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        if (fallbackEventSystemObject != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject(
            "PauseMenuEventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));

        DontDestroyOnLoad(eventSystemObject);
        fallbackEventSystemObject = eventSystemObject;
    }

    private void CleanupFallbackEventSystemIfSceneProvidesOne()
    {
        if (fallbackEventSystemObject == null)
        {
            return;
        }

        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(
            FindObjectsInactive.Exclude);

        foreach (EventSystem eventSystem in eventSystems)
        {
            if (eventSystem != null && eventSystem.gameObject != fallbackEventSystemObject)
            {
                Destroy(fallbackEventSystemObject);
                fallbackEventSystemObject = null;
                return;
            }
        }
    }
}
