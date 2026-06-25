using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string startSceneName;
    [SerializeField] private bool useAsyncLoad = false;

    [Header("Input")]
    [SerializeField] private bool disableButtonsAfterStart = true;
    [SerializeField] private Button startButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private CanvasGroup menuCanvasGroup;

    [Header("Tutorial Before Start")]
    [SerializeField] private GameObject tutorialRoot;
    [SerializeField] private Image tutorialImage;
    [SerializeField] private Sprite tutorialSprite;
    [SerializeField] private float tutorialDuration = 6f;
    [SerializeField] private bool showTutorialBeforeStart = true;

    private bool isStarting;

    private void Awake()
    {
        if (tutorialRoot != null)
        {
            tutorialRoot.SetActive(false);
        }
    }

    public void StartGame()
    {
        if (isStarting)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(startSceneName))
        {
            Debug.LogWarning("MainMenuController: startSceneName is empty.");
            return;
        }

        isStarting = true;

        if (disableButtonsAfterStart)
        {
            SetButtonsInteractable(false);

            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.blocksRaycasts = false;
                menuCanvasGroup.interactable = false;
            }
        }

        if (showTutorialBeforeStart)
        {
            StartCoroutine(ShowTutorialThenLoad());
            return;
        }

        LoadStartScene();
    }

    private IEnumerator ShowTutorialThenLoad()
    {
        ShowTutorial();

        yield return new WaitForSeconds(tutorialDuration);

        LoadStartScene();
    }

    private void ShowTutorial()
    {
        if (tutorialImage == null && tutorialSprite != null)
        {
            CreateRuntimeTutorialOverlay();
        }

        if (tutorialRoot == null && tutorialImage != null)
        {
            tutorialRoot = tutorialImage.gameObject;
        }

        if (tutorialRoot != null)
        {
            tutorialRoot.SetActive(true);
        }

        if (tutorialImage != null && tutorialSprite != null)
        {
            tutorialImage.sprite = tutorialSprite;
            tutorialImage.enabled = true;
            return;
        }

        Debug.LogWarning("MainMenuController: Tutorial image or sprite is not assigned.");
    }

    private void CreateRuntimeTutorialOverlay()
    {
        GameObject canvasObject = new GameObject(
            "TutorialOverlay",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject imageObject = new GameObject("TutorialImage", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);

        tutorialImage = imageObject.GetComponent<Image>();
        tutorialImage.color = Color.white;
        tutorialImage.preserveAspect = true;
        tutorialImage.raycastTarget = false;

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        tutorialRoot = canvasObject;
    }

    private void LoadStartScene()
    {
        if (useAsyncLoad)
        {
            SceneManager.LoadSceneAsync(startSceneName);
            return;
        }

        SceneManager.LoadScene(startSceneName);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        Debug.Log("Quit requested.");
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void SetButtonsInteractable(bool interactable)
    {
        if (startButton != null)
        {
            startButton.interactable = interactable;
        }

        if (exitButton != null)
        {
            exitButton.interactable = interactable;
        }
    }
}
