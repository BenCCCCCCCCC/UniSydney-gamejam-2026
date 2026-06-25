using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Runtime bridge for Play buttons created by scene controllers.
// Add this to a scene object, assign PlacedToolIconPresenter if possible,
// and it will append icon playback to the runtime Play Button without changing existing listeners.
public class RuntimePlayButtonIconHook : MonoBehaviour
{
    [SerializeField] private PlacedToolIconPresenter iconPresenter;
    [SerializeField] private Button playButtonOverride;
    [SerializeField] private string playButtonNameContains = "Play";
    [SerializeField] private float bindDelaySeconds = 0.1f;
    [SerializeField] private bool logBinding = true;

    private Button boundButton;
    private bool hasBound;

    private void Start()
    {
        StartCoroutine(BindAfterRuntimeUiIsReady());
    }

    private IEnumerator BindAfterRuntimeUiIsReady()
    {
        if (bindDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(bindDelaySeconds);
        }
        else
        {
            yield return null;
        }

        BindPlayButton();
    }

    private void BindPlayButton()
    {
        if (hasBound)
        {
            return;
        }

        if (iconPresenter == null)
        {
            iconPresenter = FindAnyObjectByType<PlacedToolIconPresenter>();
        }

        if (iconPresenter == null)
        {
            Debug.LogWarning("PLACED_TOOL_ICON_PRESENTER_NOT_FOUND");
            return;
        }

        Button button = playButtonOverride != null ? playButtonOverride : FindRuntimePlayButton();

        if (button == null)
        {
            Debug.LogWarning("PLAY_BUTTON_NOT_FOUND_FOR_ICON_HOOK");
            return;
        }

        boundButton = button;
        boundButton.onClick.AddListener(HandlePlayButtonClicked);
        hasBound = true;

        if (logBinding)
        {
            Debug.Log($"PLAY_BUTTON_ICON_HOOK_BOUND: {boundButton.name}");
        }
    }

    private Button FindRuntimePlayButton()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Exclude);

        foreach (Button button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(playButtonNameContains)
                || button.name.IndexOf(playButtonNameContains, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return button;
            }
        }

        return null;
    }

    private void HandlePlayButtonClicked()
    {
        if (iconPresenter == null)
        {
            Debug.LogWarning("PLACED_TOOL_ICON_PRESENTER_NOT_FOUND");
            return;
        }

        iconPresenter.PlayPlacedToolIcons();
    }

    private void OnDestroy()
    {
        if (boundButton != null)
        {
            boundButton.onClick.RemoveListener(HandlePlayButtonClicked);
        }
    }
}
