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

    public void StartGame()
    {
        if (string.IsNullOrWhiteSpace(startSceneName))
        {
            Debug.LogWarning("MainMenuController: startSceneName is empty.");
            return;
        }

        if (disableButtonsAfterStart)
        {
            SetButtonsInteractable(false);

            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.blocksRaycasts = false;
                menuCanvasGroup.interactable = false;
            }
        }

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
