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

    [Header("Speaker Portrait")]
    [SerializeField] private bool showDadPortraitForBriefing = true;
    [SerializeField] private bool showDadPortraitForDialogue = true;
    [SerializeField] private Image dadPortraitImage;
    [SerializeField] private Sprite dadPortraitSprite;
    [SerializeField] private GameObject dadPortraitRoot;

    [Header("Ending")]
    [SerializeField] private GameObject endingPanel;
    [SerializeField] private TMP_Text endingTitleText;
    [SerializeField] private TMP_Text endingBodyText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button tryAnotherWayButton;
    [SerializeField] private Button nextLevelButton;

    private bool hasWarnedMissingDadPortraitSprite;

    public void HideAll()
    {
        HideBriefing();
        HideDialogue();
        HideEnding();
        HideDadPortrait();
    }

    public void ShowBriefing()
    {
        SetActiveOrWarn(briefingPanel, true, nameof(briefingPanel));

        if (showDadPortraitForBriefing)
        {
            ShowDadPortrait();
        }
    }

    public void HideBriefing()
    {
        SetActiveIfPresent(briefingPanel, false);
        HideDadPortrait();
    }

    public void ShowDialogue(string message)
    {
        SetTextOrWarn(dialogueText, message, nameof(dialogueText));
        SetActiveOrWarn(dialoguePanel, true, nameof(dialoguePanel));

        if (showDadPortraitForDialogue)
        {
            ShowDadPortrait();
        }
    }

    public void HideDialogue()
    {
        SetActiveIfPresent(dialoguePanel, false);
        HideDadPortrait();
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

    public void ShowFinalEnding(string title, string body)
    {
        SetTextOrWarn(endingTitleText, title, nameof(endingTitleText));
        SetTextOrWarn(endingBodyText, body, nameof(endingBodyText));

        SetActiveIfPresent(retryButton != null ? retryButton.gameObject : null, false);
        SetActiveIfPresent(tryAnotherWayButton != null ? tryAnotherWayButton.gameObject : null, true);
        SetActiveIfPresent(nextLevelButton != null ? nextLevelButton.gameObject : null, true);

        SetActiveOrWarn(endingPanel, true, nameof(endingPanel));
    }

    public void ConfigureFinalEndingButtons(UnityAction tryAnotherWayAction, UnityAction mainMenuAction)
    {
        ConfigureButton(retryButton, null, nameof(retryButton));
        ConfigureButton(tryAnotherWayButton, tryAnotherWayAction, nameof(tryAnotherWayButton));
        ConfigureButton(nextLevelButton, mainMenuAction, nameof(nextLevelButton));

        SetButtonLabel(tryAnotherWayButton, "Try Another Way");
        SetButtonLabel(nextLevelButton, "Main Menu");
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

    private void SetButtonLabel(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.text = label;
            return;
        }

        Text legacyText = button.GetComponentInChildren<Text>(true);
        if (legacyText != null)
        {
            legacyText.text = label;
        }
    }

    private void ShowDadPortrait()
    {
        if (dadPortraitRoot != null)
        {
            dadPortraitRoot.SetActive(true);
        }

        if (dadPortraitImage == null)
        {
            return;
        }

        if (dadPortraitSprite != null)
        {
            dadPortraitImage.sprite = dadPortraitSprite;
        }
        else if (!hasWarnedMissingDadPortraitSprite)
        {
            Debug.LogWarning($"{nameof(SceneTextUIController)} on {name}: Dad portrait sprite is missing.");
            hasWarnedMissingDadPortraitSprite = true;
        }

        dadPortraitImage.gameObject.SetActive(true);
    }

    private void HideDadPortrait()
    {
        if (dadPortraitRoot != null)
        {
            dadPortraitRoot.SetActive(false);
        }

        if (dadPortraitImage != null)
        {
            dadPortraitImage.gameObject.SetActive(false);
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
