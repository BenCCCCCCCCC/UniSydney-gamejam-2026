using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CardMergeEffectPlayer : MonoBehaviour
{
    [Header("Input Card Merge Motion")]
    [SerializeField, Min(0f)] private float inputCardMoveDuration = 0.38f;
    [SerializeField, Min(0.01f)] private float inputCardStartScale = 1f;
    [SerializeField, Min(0.01f)] private float inputCardCenterScale = 1.08f;
    [SerializeField, Min(0.01f)] private float inputCardMergeShrinkScale = 0.72f;
    [SerializeField, Min(0f)] private float inputCardCenterOffset = 42f;

    [Header("Result Card Pop")]
    [SerializeField, Min(0f)] private float resultCardPopDuration = 0.2f;
    [SerializeField, Min(0.01f)] private float resultCardStartScale = 0.78f;
    [SerializeField, Min(0.01f)] private float resultCardPeakScale = 1.12f;
    [SerializeField, Min(0.01f)] private float resultCardFinalCenterScale = 1f;
    [SerializeField, Min(0f)] private float resultCardCenterHoldDuration = 0.5f;

    [Header("Result Card To Hand")]
    [SerializeField, Min(0f)] private float resultCardMoveToHandDuration = 0.46f;
    [SerializeField, Min(0.01f)] private float resultCardHandArrivalScale = 1f;

    [Header("Fade / Rotation / Easing")]
    [SerializeField, Min(0f)] private float inputCardFadeDuration = 0.18f;
    [SerializeField] private float inputCardRotationAmount = 8f;
    [SerializeField] private AnimationCurve inputCardMoveEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve inputCardFadeEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve resultCardPopEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve resultCardMoveEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private RectTransform effectLayer;
    private Canvas canvas;

    public static CardMergeEffectPlayer CreateRuntimeFallback(RectTransform canvasRect)
    {
        if (canvasRect == null)
        {
            return null;
        }

        GameObject playerObject = new GameObject(
            "RuntimeCardMergeEffectPlayer",
            typeof(CardMergeEffectPlayer));

        CardMergeEffectPlayer player = playerObject.GetComponent<CardMergeEffectPlayer>();
        player.ConfigureForCanvas(canvasRect.GetComponent<Canvas>());
        return player;
    }

    public void ConfigureForCanvas(Canvas ownerCanvas)
    {
        canvas = ownerCanvas;
        effectLayer = GetOrCreateEffectLayer(ownerCanvas);
    }

    public IEnumerator PlayMerge(
        CardView first,
        CardView second,
        CardView resultVisualSource,
        Vector3 handTargetWorldPosition,
        bool showCorrectToolHint = false)
    {
        if (effectLayer == null && canvas != null)
        {
            effectLayer = GetOrCreateEffectLayer(canvas);
        }

        if (effectLayer == null || first == null || second == null || resultVisualSource == null)
        {
            yield break;
        }

        effectLayer.SetAsLastSibling();

        RectTransform firstRect = first.GetComponent<RectTransform>();
        RectTransform secondRect = second.GetComponent<RectTransform>();

        if (firstRect == null || secondRect == null)
        {
            yield break;
        }

        GameObject firstClone = CreateVisualClone(first, "MergeCardA");
        GameObject secondClone = CreateVisualClone(second, "MergeCardB");
        GameObject resultClone = CreateVisualClone(resultVisualSource, "MergeResultCard");

        if (firstClone == null || secondClone == null || resultClone == null)
        {
            DestroyIfPresent(firstClone);
            DestroyIfPresent(secondClone);
            DestroyIfPresent(resultClone);
            yield break;
        }

        RectTransform firstCloneRect = firstClone.GetComponent<RectTransform>();
        RectTransform secondCloneRect = secondClone.GetComponent<RectTransform>();
        RectTransform resultCloneRect = resultClone.GetComponent<RectTransform>();
        CanvasGroup firstCloneGroup = firstClone.GetComponent<CanvasGroup>();
        CanvasGroup secondCloneGroup = secondClone.GetComponent<CanvasGroup>();
        CanvasGroup resultCloneGroup = resultClone.GetComponent<CanvasGroup>();

        if (showCorrectToolHint)
        {
            CorrectToolHintEffect.ConfigureMergeResult(resultClone);
        }

        Vector3 inputStartScale = Vector3.one * inputCardStartScale;
        firstCloneRect.anchoredPosition = GetLayerPosition(firstRect);
        secondCloneRect.anchoredPosition = GetLayerPosition(secondRect);
        firstCloneRect.localScale = inputStartScale;
        secondCloneRect.localScale = inputStartScale;

        Vector2 center = effectLayer.rect.center;
        resultCloneRect.anchoredPosition = center;
        resultCloneRect.localScale = Vector3.one * resultCardStartScale;
        resultCloneGroup.alpha = 0f;

        SetOriginalVisible(first, false);
        SetOriginalVisible(second, false);

        Vector2 firstGatherPosition = center + Vector2.left * inputCardCenterOffset;
        Vector2 secondGatherPosition = center + Vector2.right * inputCardCenterOffset;

        yield return AnimateInputCardsToCenter(
            firstCloneRect,
            secondCloneRect,
            firstCloneRect.anchoredPosition,
            secondCloneRect.anchoredPosition,
            firstGatherPosition,
            secondGatherPosition);

        yield return AnimateInputCardsConsumed(
            firstCloneRect,
            secondCloneRect,
            firstCloneGroup,
            secondCloneGroup);

        firstClone.SetActive(false);
        secondClone.SetActive(false);

        yield return AnimateResultPop(resultCloneRect, resultCloneGroup);

        if (resultCardCenterHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(resultCardCenterHoldDuration);
        }

        Vector2 handTargetPosition = GetLayerPosition(handTargetWorldPosition);
        yield return AnimateResultToHand(
            resultCloneRect,
            resultCloneRect.anchoredPosition,
            handTargetPosition);

        SetOriginalVisible(first, true);
        SetOriginalVisible(second, true);

        Destroy(firstClone);
        Destroy(secondClone);
        Destroy(resultClone);
    }

    private void OnValidate()
    {
        inputCardMoveDuration = Mathf.Max(0f, inputCardMoveDuration);
        inputCardStartScale = Mathf.Max(0.01f, inputCardStartScale);
        inputCardCenterScale = Mathf.Max(0.01f, inputCardCenterScale);
        inputCardMergeShrinkScale = Mathf.Max(0.01f, inputCardMergeShrinkScale);
        inputCardCenterOffset = Mathf.Max(0f, inputCardCenterOffset);
        inputCardFadeDuration = Mathf.Max(0f, inputCardFadeDuration);

        resultCardPopDuration = Mathf.Max(0f, resultCardPopDuration);
        resultCardStartScale = Mathf.Max(0.01f, resultCardStartScale);
        resultCardPeakScale = Mathf.Max(0.01f, resultCardPeakScale);
        resultCardFinalCenterScale = Mathf.Max(0.01f, resultCardFinalCenterScale);
        resultCardCenterHoldDuration = Mathf.Max(0f, resultCardCenterHoldDuration);
        resultCardMoveToHandDuration = Mathf.Max(0f, resultCardMoveToHandDuration);
        resultCardHandArrivalScale = Mathf.Max(0.01f, resultCardHandArrivalScale);
    }

    private RectTransform GetOrCreateEffectLayer(Canvas ownerCanvas)
    {
        if (ownerCanvas == null)
        {
            return null;
        }

        RectTransform canvasRect = ownerCanvas.GetComponent<RectTransform>();
        Transform existing = canvasRect.Find("MergeEffectLayer");
        if (existing != null && existing.TryGetComponent(out RectTransform existingRect))
        {
            existingRect.SetAsLastSibling();
            return existingRect;
        }

        GameObject layerObject = new GameObject("MergeEffectLayer", typeof(RectTransform));
        layerObject.transform.SetParent(canvasRect, false);

        RectTransform layerRect = layerObject.GetComponent<RectTransform>();
        layerRect.anchorMin = Vector2.zero;
        layerRect.anchorMax = Vector2.one;
        layerRect.offsetMin = Vector2.zero;
        layerRect.offsetMax = Vector2.zero;
        layerRect.SetAsLastSibling();
        return layerRect;
    }

    private GameObject CreateVisualClone(CardView source, string cloneName)
    {
        if (source == null)
        {
            return null;
        }

        RectTransform sourceRect = source.GetComponent<RectTransform>();
        if (sourceRect == null)
        {
            return null;
        }

        GameObject clone = Instantiate(source.gameObject, effectLayer, false);
        clone.name = cloneName;
        clone.SetActive(true);

        RectTransform cloneRect = clone.GetComponent<RectTransform>();
        cloneRect.anchorMin = new Vector2(0.5f, 0.5f);
        cloneRect.anchorMax = new Vector2(0.5f, 0.5f);
        cloneRect.pivot = new Vector2(0.5f, 0.5f);
        cloneRect.sizeDelta = sourceRect.rect.size;
        cloneRect.localRotation = Quaternion.identity;
        cloneRect.localScale = Vector3.one;

        LayoutElement layoutElement = clone.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = true;
        }

        foreach (Selectable selectable in clone.GetComponentsInChildren<Selectable>(true))
        {
            selectable.interactable = false;
        }

        foreach (Graphic graphic in clone.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
        }

        CardView clonedView = clone.GetComponent<CardView>();
        if (clonedView != null)
        {
            clonedView.enabled = false;
        }

        CanvasGroup canvasGroup = clone.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = clone.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        return clone;
    }

    private Vector2 GetLayerPosition(RectTransform source)
    {
        Vector3 worldCenter = source.TransformPoint(source.rect.center);
        return GetLayerPosition(worldCenter);
    }

    private Vector2 GetLayerPosition(Vector3 worldPosition)
    {
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, worldPosition);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            effectLayer,
            screenPoint,
            eventCamera,
            out Vector2 localPoint);

        return localPoint;
    }

    private IEnumerator AnimateInputCardsToCenter(
        RectTransform first,
        RectTransform second,
        Vector2 firstStart,
        Vector2 secondStart,
        Vector2 firstEnd,
        Vector2 secondEnd)
    {
        Vector3 startScale = Vector3.one * inputCardStartScale;
        Vector3 centerScale = Vector3.one * inputCardCenterScale;

        if (inputCardMoveDuration <= 0f)
        {
            first.anchoredPosition = firstEnd;
            second.anchoredPosition = secondEnd;
            first.localScale = centerScale;
            second.localScale = centerScale;
            first.localRotation = Quaternion.Euler(0f, 0f, inputCardRotationAmount);
            second.localRotation = Quaternion.Euler(0f, 0f, -inputCardRotationAmount);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < inputCardMoveDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Evaluate(inputCardMoveEasing, elapsed / inputCardMoveDuration);
            first.anchoredPosition = Vector2.LerpUnclamped(firstStart, firstEnd, t);
            second.anchoredPosition = Vector2.LerpUnclamped(secondStart, secondEnd, t);
            first.localScale = Vector3.LerpUnclamped(startScale, centerScale, t);
            second.localScale = Vector3.LerpUnclamped(startScale, centerScale, t);
            first.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(0f, inputCardRotationAmount, t));
            second.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(0f, -inputCardRotationAmount, t));
            yield return null;
        }

        first.anchoredPosition = firstEnd;
        second.anchoredPosition = secondEnd;
        first.localScale = centerScale;
        second.localScale = centerScale;
    }

    private IEnumerator AnimateInputCardsConsumed(
        RectTransform first,
        RectTransform second,
        CanvasGroup firstGroup,
        CanvasGroup secondGroup)
    {
        Vector2 firstStartPosition = first.anchoredPosition;
        Vector2 secondStartPosition = second.anchoredPosition;
        Vector2 mergePosition = Vector2.Lerp(firstStartPosition, secondStartPosition, 0.5f);
        Vector3 centerScale = Vector3.one * inputCardCenterScale;
        Vector3 shrinkScale = Vector3.one * inputCardMergeShrinkScale;

        if (inputCardFadeDuration <= 0f)
        {
            first.anchoredPosition = mergePosition;
            second.anchoredPosition = mergePosition;
            first.localScale = shrinkScale;
            second.localScale = shrinkScale;
            firstGroup.alpha = 0f;
            secondGroup.alpha = 0f;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < inputCardFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Evaluate(inputCardFadeEasing, elapsed / inputCardFadeDuration);
            first.anchoredPosition = Vector2.LerpUnclamped(firstStartPosition, mergePosition, t);
            second.anchoredPosition = Vector2.LerpUnclamped(secondStartPosition, mergePosition, t);
            first.localScale = Vector3.LerpUnclamped(centerScale, shrinkScale, t);
            second.localScale = Vector3.LerpUnclamped(centerScale, shrinkScale, t);
            first.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(inputCardRotationAmount, 0f, t));
            second.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(-inputCardRotationAmount, 0f, t));
            firstGroup.alpha = 1f - t;
            secondGroup.alpha = 1f - t;
            yield return null;
        }

        firstGroup.alpha = 0f;
        secondGroup.alpha = 0f;
    }

    private IEnumerator AnimateResultPop(RectTransform result, CanvasGroup resultGroup)
    {
        Vector3 startScale = Vector3.one * resultCardStartScale;
        Vector3 peakScale = Vector3.one * resultCardPeakScale;
        Vector3 finalScale = Vector3.one * resultCardFinalCenterScale;

        if (resultCardPopDuration <= 0f)
        {
            result.localScale = finalScale;
            resultGroup.alpha = 1f;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < resultCardPopDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / resultCardPopDuration);
            float t = Evaluate(resultCardPopEasing, normalizedTime);

            if (t < 0.5f)
            {
                result.localScale = Vector3.LerpUnclamped(startScale, peakScale, t * 2f);
            }
            else
            {
                result.localScale = Vector3.LerpUnclamped(peakScale, finalScale, (t - 0.5f) * 2f);
            }

            resultGroup.alpha = t;
            yield return null;
        }

        result.localScale = finalScale;
        resultGroup.alpha = 1f;
    }

    private IEnumerator AnimateResultToHand(RectTransform target, Vector2 start, Vector2 end)
    {
        Vector3 startScale = Vector3.one * resultCardFinalCenterScale;
        Vector3 endScale = Vector3.one * resultCardHandArrivalScale;

        if (resultCardMoveToHandDuration <= 0f)
        {
            target.anchoredPosition = end;
            target.localScale = endScale;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < resultCardMoveToHandDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Evaluate(resultCardMoveEasing, elapsed / resultCardMoveToHandDuration);
            target.anchoredPosition = Vector2.LerpUnclamped(start, end, t);
            target.localScale = Vector3.LerpUnclamped(startScale, endScale, t);
            yield return null;
        }

        target.anchoredPosition = end;
        target.localScale = endScale;
    }

    private static float Evaluate(AnimationCurve curve, float normalizedTime)
    {
        float t = Mathf.Clamp01(normalizedTime);
        return curve == null || curve.length == 0
            ? t * t * (3f - 2f * t)
            : curve.Evaluate(t);
    }

    private static void SetOriginalVisible(CardView view, bool visible)
    {
        if (view == null)
        {
            return;
        }

        CanvasGroup group = view.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = visible ? 1f : 0f;
        }
    }

    private static void DestroyIfPresent(GameObject target)
    {
        if (target != null)
        {
            Destroy(target);
        }
    }
}
