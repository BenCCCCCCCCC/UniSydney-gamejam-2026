using System.Collections;
using UnityEngine;

public class PlacedToolIconView : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Coroutine playCoroutine;

    public void Play(
        Sprite icon,
        Vector3 startWorldPosition,
        Vector3 endWorldPosition,
        float duration,
        Vector2 size,
        string sortingLayerName,
        int sortingOrder)
    {
        if (icon == null)
        {
            Debug.Log("PlacedToolIconView: icon is missing, skip icon animation.");
            return;
        }

        EnsureSpriteRenderer();

        spriteRenderer.sprite = icon;
        spriteRenderer.sortingLayerName = string.IsNullOrWhiteSpace(sortingLayerName) ? "Default" : sortingLayerName;
        spriteRenderer.sortingOrder = sortingOrder;

        ApplyPreservedAspectSize(icon, size);

        transform.position = startWorldPosition;

        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
        }

        playCoroutine = StartCoroutine(PlayFall(startWorldPosition, endWorldPosition, Mathf.Max(0f, duration)));
    }

    private IEnumerator PlayFall(Vector3 startWorldPosition, Vector3 endWorldPosition, float duration)
    {
        if (duration <= 0f)
        {
            transform.position = endWorldPosition;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            transform.position = Vector3.Lerp(startWorldPosition, endWorldPosition, eased);
            yield return null;
        }

        transform.position = endWorldPosition;
    }

    private void EnsureSpriteRenderer()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }

    private void ApplyPreservedAspectSize(Sprite icon, Vector2 size)
    {
        if (icon == null || icon.bounds.size.x <= 0f || icon.bounds.size.y <= 0f || size.x <= 0f || size.y <= 0f)
        {
            transform.localScale = Vector3.one;
            return;
        }

        float scaleX = size.x / icon.bounds.size.x;
        float scaleY = size.y / icon.bounds.size.y;
        float scale = Mathf.Min(scaleX, scaleY);
        transform.localScale = Vector3.one * scale;
    }
}
