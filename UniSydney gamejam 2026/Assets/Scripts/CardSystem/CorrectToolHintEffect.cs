using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class CorrectToolHintEffect : MonoBehaviour
{
    private const float RefreshInterval = 0.2f;
    private const float PulseSpeed = 0.85f;
    private const float MinimumGlowAlpha = 0.16f;
    private const float MaximumGlowAlpha = 0.62f;
    private const float HandBounceHeight = 2f;

    private static readonly List<CorrectToolHintEffect> HandHints = new();
    private static string lastLoggedHandHintKey;

    private string toolCardID;
    private Outline outline;
    private RectTransform hintGraphicRect;
    private Vector2 hintGraphicRestingPosition;
    private bool isMergeResult;
    private bool shouldAnimate;
    private float nextRefreshTime;

    public static void ConfigureHandCard(GameObject cardObject, string cardID)
    {
        if (cardObject == null || string.IsNullOrWhiteSpace(cardID))
        {
            return;
        }

        CorrectToolHintEffect hint =
            cardObject.GetComponent<CorrectToolHintEffect>();
        if (hint == null)
        {
            hint = cardObject.AddComponent<CorrectToolHintEffect>();
        }

        hint.toolCardID = cardID;
        hint.isMergeResult = false;
        hint.EnsureOutline();
        RefreshHandHints();
    }

    public static void ConfigureMergeResult(GameObject resultObject)
    {
        if (resultObject == null)
        {
            return;
        }

        CorrectToolHintEffect hint =
            resultObject.GetComponent<CorrectToolHintEffect>();
        if (hint == null)
        {
            hint = resultObject.AddComponent<CorrectToolHintEffect>();
        }

        HandHints.Remove(hint);
        hint.isMergeResult = true;
        hint.shouldAnimate = true;
        hint.EnsureOutline();
        Debug.Log(
            $"CORRECT_TOOL_HINT_MERGE_ATTACHED: object={resultObject.name}, "
            + $"graphic={(hint.hintGraphicRect != null ? hint.hintGraphicRect.name : "(missing)")}");
    }

    private void OnEnable()
    {
        if (!isMergeResult && !HandHints.Contains(this))
        {
            HandHints.Add(this);
        }
    }

    private void OnDisable()
    {
        HandHints.Remove(this);
        ResetVisual();
        RefreshHandHints();
    }

    private void OnDestroy()
    {
        HandHints.Remove(this);
    }

    private void Update()
    {
        if (!isMergeResult && Time.unscaledTime >= nextRefreshTime)
        {
            nextRefreshTime = Time.unscaledTime + RefreshInterval;
            RefreshHandHints();
        }

        if (!shouldAnimate || outline == null)
        {
            return;
        }

        float wave = (Mathf.Sin(Time.unscaledTime * PulseSpeed * Mathf.PI * 2f)
            + 1f) * 0.5f;
        Color color = outline.effectColor;
        color.a = Mathf.Lerp(MinimumGlowAlpha, MaximumGlowAlpha, wave);
        outline.effectColor = color;

        if (!isMergeResult && hintGraphicRect != null)
        {
            float bounce = Mathf.Sin(wave * Mathf.PI) * HandBounceHeight;
            hintGraphicRect.anchoredPosition =
                hintGraphicRestingPosition + Vector2.up * bounce;
        }
    }

    private static void RefreshHandHints()
    {
        for (int i = HandHints.Count - 1; i >= 0; i--)
        {
            CorrectToolHintEffect hint = HandHints[i];
            if (hint == null)
            {
                HandHints.RemoveAt(i);
                continue;
            }

            hint.SetAnimating(false);
        }

        string nodeID = GameSessionData.CurrentNodeID;
        string nextPointID = CorrectToolHintRules.GetNextCorrectPointID(nodeID);
        if (string.IsNullOrWhiteSpace(nextPointID))
        {
            LogHandHintChange(nodeID, null, null);
            return;
        }

        foreach (CorrectToolHintEffect hint in HandHints)
        {
            if (hint == null
                || !hint.IsCurrentlyInHand()
                || !CorrectToolHintRules.IsCorrectForPoint(
                    nodeID,
                    nextPointID,
                    hint.toolCardID))
            {
                continue;
            }

            hint.SetAnimating(true);
            LogHandHintChange(nodeID, nextPointID, hint.toolCardID);
            return;
        }

        LogHandHintChange(nodeID, nextPointID, null);
    }

    private static void LogHandHintChange(
        string nodeID,
        string pointID,
        string cardID)
    {
        string key = $"{nodeID}|{pointID}|{cardID}";
        if (key == lastLoggedHandHintKey)
        {
            return;
        }

        lastLoggedHandHintKey = key;
        Debug.Log(
            $"CORRECT_TOOL_HAND_HINT: node={nodeID}, "
            + $"nextPoint={(pointID ?? "(none)")}, "
            + $"card={(cardID ?? "(none)")}");
    }

    private bool IsCurrentlyInHand()
    {
        HandCardHoverEffect hover = GetComponent<HandCardHoverEffect>();
        return isActiveAndEnabled
            && gameObject.activeInHierarchy
            && hover != null
            && hover.enabled;
    }

    private void SetAnimating(bool animate)
    {
        shouldAnimate = animate;
        if (!animate)
        {
            ResetVisual();
        }
    }

    private void EnsureOutline()
    {
        if (outline != null)
        {
            return;
        }

        Image targetImage = FindHintImage();
        if (targetImage == null)
        {
            return;
        }

        outline = targetImage.GetComponent<Outline>();
        if (outline == null)
        {
            outline = targetImage.gameObject.AddComponent<Outline>();
        }

        hintGraphicRect = targetImage.rectTransform;
        hintGraphicRestingPosition = hintGraphicRect.anchoredPosition;
        outline.effectDistance = new Vector2(2.5f, -2.5f);
        outline.useGraphicAlpha = false;
        outline.effectColor = new Color(1f, 0.93f, 0.42f, 0f);
    }

    private Image FindHintImage()
    {
        Image fallback = null;
        Image inactiveSpriteFallback = null;
        foreach (Image image in GetComponentsInChildren<Image>(true))
        {
            if (image == null)
            {
                continue;
            }

            fallback ??= image;
            if (image.sprite != null && image.gameObject.activeInHierarchy)
            {
                return image;
            }

            if (image.sprite != null)
            {
                inactiveSpriteFallback ??= image;
            }
        }

        return inactiveSpriteFallback != null
            ? inactiveSpriteFallback
            : fallback;
    }

    private void ResetVisual()
    {
        if (outline == null)
        {
            return;
        }

        Color color = outline.effectColor;
        color.a = 0f;
        outline.effectColor = color;

        if (hintGraphicRect != null)
        {
            hintGraphicRect.anchoredPosition = hintGraphicRestingPosition;
        }
    }
}
