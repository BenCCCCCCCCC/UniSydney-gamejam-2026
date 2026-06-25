using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SceneTextUIController : MonoBehaviour
{
    [Header("Briefing")]
    [SerializeField] private GameObject briefingPanel;
    [SerializeField] private TMP_Text briefingText;

    [Header("Dialogue")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Ending")]
    [SerializeField] private GameObject endingPanel;
    [SerializeField] private TMP_Text endingTitleText;
    [SerializeField] private TMP_Text endingBodyText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button tryAnotherWayButton;
    [SerializeField] private Button nextLevelButton;

    public void HideAll()
    {
        HideBriefing();
        HideDialogue();
        HideEnding();
    }

    public void ShowBriefing()
    {
        SetActiveOrWarn(briefingPanel, true, nameof(briefingPanel));
    }

    public void HideBriefing()
    {
        SetActiveIfPresent(briefingPanel, false);
    }

    public void ShowDialogue(string message)
    {
        SetTextOrWarn(dialogueText, message, nameof(dialogueText));
        SetActiveOrWarn(dialoguePanel, true, nameof(dialoguePanel));
    }

    public void HideDialogue()
    {
        SetActiveIfPresent(dialoguePanel, false);
    }

    public void ShowEnding(string title, string body, bool success)
    {
        SetTextOrWarn(endingTitleText, title, nameof(endingTitleText));
        SetTextOrWarn(endingBodyText, body, nameof(endingBodyText));

        SetActiveIfPresent(retryButton != null ? retryButton.gameObject : null, !success);
        SetActiveIfPresent(tryAnotherWayButton != null ? tryAnotherWayButton.gameObject : null, success);
        SetActiveIfPresent(nextLevelButton != null ? nextLevelButton.gameObject : null, success);

        SetActiveOrWarn(endingPanel, true, nameof(endingPanel));
    }

    public void HideEnding()
    {
        SetActiveIfPresent(endingPanel, false);
    }

    public void ConfigureEndingButtons(UnityAction retryAction, UnityAction tryAnotherWayAction, UnityAction nextLevelAction)
    {
        ConfigureButton(retryButton, retryAction, nameof(retryButton));
        ConfigureButton(tryAnotherWayButton, tryAnotherWayAction, nameof(tryAnotherWayButton));
        ConfigureButton(nextLevelButton, nextLevelAction, nameof(nextLevelButton));
    }

    private void ConfigureButton(Button button, UnityAction action, string fieldName)
    {
        if (button == null)
        {
            Debug.LogWarning($"{nameof(SceneTextUIController)} on {name}: {fieldName} is missing.");
            return;
        }

        button.onClick.RemoveAllListeners();

        if (action != null)
        {
            button.onClick.AddListener(action);
        }
    }

    private void SetTextOrWarn(TMP_Text text, string value, string fieldName)
    {
        if (text == null)
        {
            Debug.LogWarning($"{nameof(SceneTextUIController)} on {name}: {fieldName} is missing.");
            return;
        }

        text.text = value;
    }

    private void SetActiveOrWarn(GameObject target, bool active, string fieldName)
    {
        if (target == null)
        {
            Debug.LogWarning($"{nameof(SceneTextUIController)} on {name}: {fieldName} is missing.");
            return;
        }

        target.SetActive(active);
    }

    private void SetActiveIfPresent(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
