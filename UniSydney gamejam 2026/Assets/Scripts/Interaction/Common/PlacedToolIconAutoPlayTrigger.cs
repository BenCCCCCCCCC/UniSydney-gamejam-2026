using System.Collections;
using UnityEngine;

// Optional scene helper: watches GameSessionData.CurrentPhase and plays placed tool icons
// when the node flow moves into AutoPlay. It does not touch the runtime Play Button.
public class PlacedToolIconAutoPlayTrigger : MonoBehaviour
{
    [SerializeField] private PlacedToolIconPresenter iconPresenter;
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private float triggerDelaySeconds = 0f;
    [SerializeField] private bool autoFindPresenter = true;

    private GameFlowPhase previousPhase;
    private bool hasTriggered;
    private bool isTriggerPending;

    private void Start()
    {
        previousPhase = GameSessionData.CurrentPhase;

        if (GameSessionData.CurrentPhase == GameFlowPhase.AutoPlay)
        {
            RequestTrigger();
        }
    }

    private void Update()
    {
        GameFlowPhase currentPhase = GameSessionData.CurrentPhase;

        if (previousPhase != GameFlowPhase.AutoPlay && currentPhase == GameFlowPhase.AutoPlay)
        {
            RequestTrigger();
        }

        previousPhase = currentPhase;
    }

    private void RequestTrigger()
    {
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        if (isTriggerPending)
        {
            return;
        }

        if (triggerDelaySeconds > 0f)
        {
            StartCoroutine(TriggerAfterDelay());
            return;
        }

        TriggerIconPresenter();
    }

    private IEnumerator TriggerAfterDelay()
    {
        isTriggerPending = true;
        yield return new WaitForSeconds(triggerDelaySeconds);
        isTriggerPending = false;

        TriggerIconPresenter();
    }

    private void TriggerIconPresenter()
    {
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        if (iconPresenter == null && autoFindPresenter)
        {
            iconPresenter = FindAnyObjectByType<PlacedToolIconPresenter>();
        }

        if (iconPresenter == null)
        {
            Debug.Log("PLACED_TOOL_ICON_PRESENTER_NOT_FOUND");
            return;
        }

        hasTriggered = true;
        iconPresenter.PlayPlacedToolIcons();
    }
}
