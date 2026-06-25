using System.Collections;
using UnityEngine;

public class PlacedToolRevealEffectView : MonoBehaviour
{
    public void Play(
        GameObject placedCardUIObject,
        Sprite icon,
        Vector3 startWorldPosition,
        Vector3 endWorldPosition,
        Vector2 iconSize,
        PlacementRevealVfxConfig config,
        Transform parent,
        string placePointID,
        string toolCardID)
    {
        if (config == null)
        {
            Debug.Log("PlacedToolRevealEffectView: Reveal VFX config is missing.");
            return;
        }

        transform.SetParent(parent, true);
        StartCoroutine(PlayRoutine(placedCardUIObject, icon, startWorldPosition, endWorldPosition, iconSize, config, placePointID, toolCardID));
    }

    private IEnumerator PlayRoutine(
        GameObject placedCardUIObject,
        Sprite icon,
        Vector3 startWorldPosition,
        Vector3 endWorldPosition,
        Vector2 iconSize,
        PlacementRevealVfxConfig config,
        string placePointID,
        string toolCardID)
    {
        Coroutine cardCoroutine = null;

        if (placedCardUIObject != null)
        {
            cardCoroutine = StartCoroutine(PlayCardShrinkAndFade(placedCardUIObject, config));
        }
        else
        {
            Debug.Log($"CARD_SHRINK_SKIPPED_NO_UI: {placePointID}");
        }

        GameObject smokeObject = CreateSmokeObject(config, endWorldPosition, placePointID);
        Coroutine smokeCoroutine = smokeObject != null ? StartCoroutine(PlaySmoke(smokeObject, config)) : null;

        if (config.IconSpawnDelay > 0f)
        {
            yield return new WaitForSeconds(config.IconSpawnDelay);
        }

        if (icon == null)
        {
            Debug.Log($"PlacedToolRevealEffectView: icon is missing for {toolCardID}, skip icon animation.");
        }
        else
        {
            GameObject iconObject = new GameObject($"RevealedIcon_{placePointID}_{toolCardID}", typeof(SpriteRenderer), typeof(PlacedToolIconView));
            iconObject.transform.SetParent(transform, true);

            PlacedToolIconView iconView = iconObject.GetComponent<PlacedToolIconView>();
            iconView.Play(icon, startWorldPosition, endWorldPosition, config.IconFallDuration, iconSize, config.SortingLayerName, config.IconSortingOrder);
            Debug.Log($"PLACED_ICON_SPAWNED: {toolCardID} at {placePointID}");
        }

        if (cardCoroutine != null)
        {
            yield return cardCoroutine;
        }

        if (smokeCoroutine != null)
        {
            yield return smokeCoroutine;
        }

        if (smokeObject != null)
        {
            Destroy(smokeObject);
        }
    }

    private IEnumerator PlayCardShrinkAndFade(GameObject placedCardUIObject, PlacementRevealVfxConfig config)
    {
        CanvasGroup canvasGroup = placedCardUIObject.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = placedCardUIObject.AddComponent<CanvasGroup>();
        }

        Transform cardTransform = placedCardUIObject.transform;
        Vector3 startScale = cardTransform.localScale;
        Vector3 endScale = Vector3.zero;
        float duration = Mathf.Max(config.CardShrinkDuration, config.CardFadeDuration);

        if (duration <= 0f)
        {
            cardTransform.localScale = endScale;
            canvasGroup.alpha = 0f;
            ApplyPlacedCardEndState(placedCardUIObject, config);
            yield break;
        }

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float shrinkT = config.CardShrinkDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / config.CardShrinkDuration);
            float fadeT = config.CardFadeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / config.CardFadeDuration);
            cardTransform.localScale = Vector3.Lerp(startScale, endScale, shrinkT);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, fadeT);
            yield return null;
        }

        cardTransform.localScale = endScale;
        canvasGroup.alpha = 0f;
        ApplyPlacedCardEndState(placedCardUIObject, config);
    }

    private static void ApplyPlacedCardEndState(GameObject placedCardUIObject, PlacementRevealVfxConfig config)
    {
        if (config.HidePlacedCardAfterEffect)
        {
            placedCardUIObject.SetActive(false);
        }
    }

    private GameObject CreateSmokeObject(PlacementRevealVfxConfig config, Vector3 endWorldPosition, string placePointID)
    {
        Sprite[] smokeFrames = config.SmokeFrames;

        if (smokeFrames == null || smokeFrames.Length == 0 || smokeFrames[0] == null)
        {
            Debug.Log($"SMOKE_SKIPPED_NO_FRAMES: {placePointID}");
            return null;
        }

        GameObject smokeObject = new GameObject($"RevealSmoke_{placePointID}", typeof(SpriteRenderer));
        smokeObject.transform.SetParent(transform, true);
        smokeObject.transform.position = endWorldPosition;

        SpriteRenderer smokeRenderer = smokeObject.GetComponent<SpriteRenderer>();
        smokeRenderer.sprite = smokeFrames[0];
        smokeRenderer.sortingLayerName = config.SortingLayerName;
        smokeRenderer.sortingOrder = config.SmokeSortingOrder;

        ApplyPreservedAspectSize(smokeObject.transform, smokeFrames[0], config.SmokeSize, config.SmokeScaleStart);
        return smokeObject;
    }

    private IEnumerator PlaySmoke(GameObject smokeObject, PlacementRevealVfxConfig config)
    {
        SpriteRenderer smokeRenderer = smokeObject.GetComponent<SpriteRenderer>();
        Sprite[] smokeFrames = config.SmokeFrames;
        float duration = config.SmokeDuration;

        if (duration <= 0f)
        {
            ApplyPreservedAspectSize(smokeObject.transform, smokeRenderer.sprite, config.SmokeSize, config.SmokeScaleEnd);
            yield break;
        }

        float elapsed = 0f;
        float secondsPerFrame = 1f / config.SmokeFrameRate;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (smokeFrames != null && smokeFrames.Length > 1)
            {
                int frameIndex = Mathf.Clamp(Mathf.FloorToInt(elapsed / secondsPerFrame), 0, smokeFrames.Length - 1);

                if (smokeFrames[frameIndex] != null)
                {
                    smokeRenderer.sprite = smokeFrames[frameIndex];
                }
            }

            float scale = Mathf.Lerp(config.SmokeScaleStart, config.SmokeScaleEnd, t);
            ApplyPreservedAspectSize(smokeObject.transform, smokeRenderer.sprite, config.SmokeSize, scale);
            yield return null;
        }

        ApplyPreservedAspectSize(smokeObject.transform, smokeRenderer.sprite, config.SmokeSize, config.SmokeScaleEnd);
    }

    private static void ApplyPreservedAspectSize(Transform target, Sprite sprite, Vector2 size, float multiplier)
    {
        if (target == null || sprite == null || sprite.bounds.size.x <= 0f || sprite.bounds.size.y <= 0f)
        {
            return;
        }

        float scaleX = size.x / sprite.bounds.size.x;
        float scaleY = size.y / sprite.bounds.size.y;
        float scale = Mathf.Min(scaleX, scaleY) * Mathf.Max(0.01f, multiplier);
        target.localScale = Vector3.one * scale;
    }
}
