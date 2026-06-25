using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HandCardHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Canvas canvas;
    private RectTransform sourceRect;
    private RectTransform overlayLayer;
    private GameObject previewObject;
    private Coroutine animationRoutine;
    private CanvasGroup sourceCanvasGroup;
    private float hoverScale = 1.18f;
    private float hoverLiftY = 60f;
    private float hoverEnterDuration = 0.12f;
    private float previewOffsetY = 80f;
    private float originalCardHoverAlpha;
    private float hoverExitDuration = 0.16f;
    private float originalCardRestoreDuration = 0.12f;
    private float hoverFadeDuration = 0.12f;
    private float originalCardRestingAlpha = 1f;
    private bool pointerInside;

    public void Configure(
        Canvas ownerCanvas,
        float scale,
        float liftY,
        float enterDuration,
        float offsetY,
        float sourceHoverAlpha,
        float exitDuration,
        float sourceRestoreDuration,
        float fadeDuration)
    {
        canvas = ownerCanvas;
        sourceRect = GetComponent<RectTransform>();
        sourceCanvasGroup = GetComponent<CanvasGroup>();
        hoverScale = Mathf.Max(0.01f, scale);
        hoverLiftY = liftY;
        hoverEnterDuration = Mathf.Max(0f, enterDuration);
        previewOffsetY = offsetY;
        originalCardHoverAlpha = Mathf.Clamp01(sourceHoverAlpha);
        hoverExitDuration = Mathf.Max(0f, exitDuration);
        originalCardRestoreDuration = Mathf.Max(0f, sourceRestoreDuration);
        hoverFadeDuration = Mathf.Max(0f, fadeDuration);

        if (sourceCanvasGroup != null && previewObject == null && animationRoutine == null)
        {
            originalCardRestingAlpha = sourceCanvasGroup.alpha;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        TransitionPreview(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        TransitionPreview(false);
    }

    public void SetHoverEnabled(bool hoverEnabled)
    {
        if (hoverEnabled)
        {
            enabled = true;
            return;
        }

        pointerInside = false;
        CleanupPreviewAndRestore();
        enabled = false;
    }

    private void OnDisable()
    {
        pointerInside = false;
        CleanupPreviewAndRestore();
    }

    private void TransitionPreview(bool showing)
    {
        if (canvas == null || sourceRect == null)
        {
            return;
        }

        StopActiveAnimation();

        if (showing)
        {
            overlayLayer = GetOrCreateOverlayLayer(canvas);
            if (overlayLayer == null)
            {
                return;
            }

            overlayLayer.SetAsLastSibling();

            if (previewObject == null)
            {
                if (sourceCanvasGroup != null)
                {
                    originalCardRestingAlpha = sourceCanvasGroup.alpha;
                }

                previewObject = CreatePreviewClone();
                if (previewObject == null)
                {
                    RestoreOriginalCardImmediately();
                    return;
                }
            }
        }
        else if (previewObject == null)
        {
            RestoreOriginalCardImmediately();
            return;
        }

        animationRoutine = StartCoroutine(AnimateTransition(showing));
    }

    private IEnumerator AnimateTransition(bool showing)
    {
        RectTransform previewRect = previewObject.GetComponent<RectTransform>();
        CanvasGroup previewGroup = previewObject.GetComponent<CanvasGroup>();
        Vector2 startPosition = previewRect.anchoredPosition;
        Vector3 startScale = previewRect.localScale;
        float startPreviewAlpha = previewGroup.alpha;
        float startSourceAlpha = sourceCanvasGroup != null
            ? sourceCanvasGroup.alpha
            : originalCardRestingAlpha;

        Vector2 restingPosition = GetOverlayPosition(sourceRect) + Vector2.up * previewOffsetY;
        Vector2 targetPosition = showing
            ? restingPosition + Vector2.up * hoverLiftY
            : restingPosition;
        Vector3 targetScale = showing
            ? Vector3.one * hoverScale
            : Vector3.one;
        float targetPreviewAlpha = showing ? 1f : 0f;
        float targetSourceAlpha = showing
            ? originalCardHoverAlpha
            : originalCardRestingAlpha;
        float motionDuration = showing ? hoverEnterDuration : hoverExitDuration;
        float sourceDuration = showing ? hoverFadeDuration : originalCardRestoreDuration;
        float totalDuration = Mathf.Max(motionDuration, hoverFadeDuration, sourceDuration);

        if (totalDuration <= 0f)
        {
            ApplyTransitionEnd(
                previewRect,
                previewGroup,
                targetPosition,
                targetScale,
                targetPreviewAlpha,
                targetSourceAlpha);
            FinishTransition(showing);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < totalDuration && previewObject != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float motionT = Smooth(GetProgress(elapsed, motionDuration));
            float fadeT = Smooth(GetProgress(elapsed, hoverFadeDuration));
            float sourceT = Smooth(GetProgress(elapsed, sourceDuration));

            previewRect.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, motionT);
            previewRect.localScale = Vector3.LerpUnclamped(startScale, targetScale, motionT);
            previewGroup.alpha = Mathf.LerpUnclamped(startPreviewAlpha, targetPreviewAlpha, fadeT);

            if (sourceCanvasGroup != null)
            {
                sourceCanvasGroup.alpha = Mathf.LerpUnclamped(startSourceAlpha, targetSourceAlpha, sourceT);
            }

            yield return null;
        }

        if (previewObject != null)
        {
            ApplyTransitionEnd(
                previewRect,
                previewGroup,
                targetPosition,
                targetScale,
                targetPreviewAlpha,
                targetSourceAlpha);
        }

        FinishTransition(showing);
    }

    private void ApplyTransitionEnd(
        RectTransform previewRect,
        CanvasGroup previewGroup,
        Vector2 targetPosition,
        Vector3 targetScale,
        float targetPreviewAlpha,
        float targetSourceAlpha)
    {
        previewRect.anchoredPosition = targetPosition;
        previewRect.localScale = targetScale;
        previewGroup.alpha = targetPreviewAlpha;

        if (sourceCanvasGroup != null)
        {
            sourceCanvasGroup.alpha = targetSourceAlpha;
        }
    }

    private void FinishTransition(bool showing)
    {
        animationRoutine = null;

        if (!showing && !pointerInside)
        {
            if (previewObject != null)
            {
                Destroy(previewObject);
                previewObject = null;
            }

            RestoreOriginalCardImmediately();
        }
    }

    private GameObject CreatePreviewClone()
    {
        GameObject clone = Instantiate(gameObject, overlayLayer, false);
        clone.name = "HandCardHoverPreview";
        clone.SetActive(true);

        RectTransform cloneRect = clone.GetComponent<RectTransform>();
        cloneRect.anchorMin = new Vector2(0.5f, 0.5f);
        cloneRect.anchorMax = new Vector2(0.5f, 0.5f);
        cloneRect.pivot = new Vector2(0.5f, 0.5f);
        cloneRect.sizeDelta = sourceRect.rect.size;
        cloneRect.anchoredPosition = GetOverlayPosition(sourceRect) + Vector2.up * previewOffsetY;
        cloneRect.localRotation = Quaternion.identity;
        cloneRect.localScale = Vector3.one;

        LayoutElement layoutElement = clone.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = true;
        }

        foreach (HandCardHoverEffect hoverEffect in clone.GetComponentsInChildren<HandCardHoverEffect>(true))
        {
            hoverEffect.enabled = false;
        }

        foreach (CardView cardView in clone.GetComponentsInChildren<CardView>(true))
        {
            cardView.enabled = false;
        }

        foreach (Selectable selectable in clone.GetComponentsInChildren<Selectable>(true))
        {
            selectable.interactable = false;
        }

        foreach (Graphic graphic in clone.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
        }

        CanvasGroup group = clone.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = clone.AddComponent<CanvasGroup>();
        }

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        return clone;
    }

    private void StopActiveAnimation()
    {
        if (animationRoutine == null)
        {
            return;
        }

        StopCoroutine(animationRoutine);
        animationRoutine = null;
    }

    private void RestoreOriginalCardImmediately()
    {
        if (sourceCanvasGroup != null)
        {
            sourceCanvasGroup.alpha = originalCardRestingAlpha;
        }
    }

    private void CleanupPreviewAndRestore()
    {
        StopActiveAnimation();

        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }

        RestoreOriginalCardImmediately();
    }

    private RectTransform GetOrCreateOverlayLayer(Canvas ownerCanvas)
    {
        RectTransform canvasRect = ownerCanvas.GetComponent<RectTransform>();
        Transform existing = canvasRect.Find("HandCardHoverLayer");
        if (existing != null && existing.TryGetComponent(out RectTransform existingRect))
        {
            existingRect.SetAsLastSibling();
            return existingRect;
        }

        GameObject layerObject = new GameObject("HandCardHoverLayer", typeof(RectTransform));
        layerObject.transform.SetParent(canvasRect, false);

        RectTransform layerRect = layerObject.GetComponent<RectTransform>();
        layerRect.anchorMin = Vector2.zero;
        layerRect.anchorMax = Vector2.one;
        layerRect.offsetMin = Vector2.zero;
        layerRect.offsetMax = Vector2.zero;
        layerRect.SetAsLastSibling();
        return layerRect;
    }

    private Vector2 GetOverlayPosition(RectTransform source)
    {
        Vector3 worldCenter = source.TransformPoint(source.rect.center);
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, worldCenter);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayLayer,
            screenPoint,
            eventCamera,
            out Vector2 localPoint);

        return localPoint;
    }

    private static float GetProgress(float elapsed, float duration)
    {
        return duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
    }

    private static float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
