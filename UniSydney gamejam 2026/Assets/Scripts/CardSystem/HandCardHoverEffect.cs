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
    private float hoverScale = 1.18f;
    private float hoverLiftY = 60f;
    private float animationDuration = 0.12f;
    private float previewOffsetY = 80f;

    public void Configure(
        Canvas ownerCanvas,
        float scale,
        float liftY,
        float duration,
        float offsetY)
    {
        canvas = ownerCanvas;
        sourceRect = GetComponent<RectTransform>();
        hoverScale = Mathf.Max(0.01f, scale);
        hoverLiftY = liftY;
        animationDuration = Mathf.Max(0f, duration);
        previewOffsetY = offsetY;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowPreview();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HidePreview();
    }

    private void OnDisable()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }
    }

    private void ShowPreview()
    {
        if (canvas == null || sourceRect == null)
        {
            return;
        }

        overlayLayer = GetOrCreateOverlayLayer(canvas);
        if (overlayLayer == null)
        {
            return;
        }

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
        }

        if (previewObject != null)
        {
            Destroy(previewObject);
        }

        previewObject = CreatePreviewClone();
        if (previewObject == null)
        {
            return;
        }

        animationRoutine = StartCoroutine(AnimatePreviewIn());
    }

    private void HidePreview()
    {
        if (previewObject == null)
        {
            return;
        }

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
        }

        animationRoutine = StartCoroutine(AnimatePreviewOut());
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

    private IEnumerator AnimatePreviewIn()
    {
        RectTransform previewRect = previewObject.GetComponent<RectTransform>();
        CanvasGroup group = previewObject.GetComponent<CanvasGroup>();
        Vector2 startPosition = previewRect.anchoredPosition;
        Vector2 endPosition = startPosition + Vector2.up * hoverLiftY;

        if (animationDuration <= 0f)
        {
            previewRect.anchoredPosition = endPosition;
            previewRect.localScale = Vector3.one * hoverScale;
            group.alpha = 1f;
            animationRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < animationDuration && previewObject != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Smooth(Mathf.Clamp01(elapsed / animationDuration));
            previewRect.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, t);
            previewRect.localScale = Vector3.LerpUnclamped(Vector3.one, Vector3.one * hoverScale, t);
            group.alpha = t;
            yield return null;
        }

        if (previewObject != null)
        {
            previewRect.anchoredPosition = endPosition;
            previewRect.localScale = Vector3.one * hoverScale;
            group.alpha = 1f;
        }

        animationRoutine = null;
    }

    private IEnumerator AnimatePreviewOut()
    {
        RectTransform previewRect = previewObject.GetComponent<RectTransform>();
        CanvasGroup group = previewObject.GetComponent<CanvasGroup>();
        Vector2 startPosition = previewRect.anchoredPosition;
        Vector2 endPosition = startPosition + Vector2.down * hoverLiftY * 0.35f;
        Vector3 startScale = previewRect.localScale;
        float startAlpha = group.alpha;

        if (animationDuration <= 0f)
        {
            DestroyPreview();
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < animationDuration && previewObject != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Smooth(Mathf.Clamp01(elapsed / animationDuration));
            previewRect.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, t);
            previewRect.localScale = Vector3.LerpUnclamped(startScale, Vector3.one, t);
            group.alpha = Mathf.LerpUnclamped(startAlpha, 0f, t);
            yield return null;
        }

        DestroyPreview();
    }

    private void DestroyPreview()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }

        animationRoutine = null;
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

    private static float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
