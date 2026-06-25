using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class SceneTransitionOverlay : MonoBehaviour
{
    private const int OverlaySortingOrder = 32767;

    private Image overlayImage;

    public static void LoadSceneCovered(
        string targetSceneName,
        string editorScenePath,
        bool needsEditorFallback,
        Color overlayColor,
        float fadeInSeconds,
        float fadeOutSeconds,
        int waitFramesAfterLoad)
    {
        SceneTransitionOverlay existingOverlay = FindAnyObjectByType<SceneTransitionOverlay>();
        if (existingOverlay != null)
        {
            Destroy(existingOverlay.gameObject);
        }

        GameObject overlayObject = new GameObject(
            "SceneTransitionOverlay",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(SceneTransitionOverlay));

        DontDestroyOnLoad(overlayObject);

        Canvas canvas = overlayObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = OverlaySortingOrder;

        CanvasScaler scaler = overlayObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        SceneTransitionOverlay overlay = overlayObject.GetComponent<SceneTransitionOverlay>();
        overlay.CreateOverlayImage(overlayObject.transform, overlayColor);
        overlay.StartCoroutine(overlay.LoadSceneCoveredRoutine(
            targetSceneName,
            editorScenePath,
            needsEditorFallback,
            overlayColor,
            fadeInSeconds,
            fadeOutSeconds,
            waitFramesAfterLoad));
    }

    private void CreateOverlayImage(Transform parent, Color overlayColor)
    {
        GameObject imageObject = new GameObject("TransitionCover", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        overlayImage = imageObject.GetComponent<Image>();
        overlayImage.color = WithAlpha(overlayColor, 0f);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private IEnumerator LoadSceneCoveredRoutine(
        string targetSceneName,
        string editorScenePath,
        bool needsEditorFallback,
        Color overlayColor,
        float fadeInSeconds,
        float fadeOutSeconds,
        int waitFramesAfterLoad)
    {
        yield return FadeOverlay(overlayColor, 0f, 1f, fadeInSeconds);

#if UNITY_EDITOR
        if (needsEditorFallback)
        {
            EditorSceneManager.LoadSceneInPlayMode(
                editorScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
        }
        else
#endif
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(targetSceneName);
            if (loadOperation != null)
            {
                while (!loadOperation.isDone)
                {
                    yield return null;
                }
            }
        }

        for (int i = 0; i < waitFramesAfterLoad; i++)
        {
            yield return null;
        }

        yield return FadeOverlay(overlayColor, 1f, 0f, fadeOutSeconds);

        Destroy(gameObject);
    }

    private IEnumerator FadeOverlay(Color overlayColor, float fromAlpha, float toAlpha, float duration)
    {
        if (overlayImage == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            overlayImage.color = WithAlpha(overlayColor, toAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            overlayImage.color = WithAlpha(overlayColor, Mathf.Lerp(fromAlpha, toAlpha, t));
            yield return null;
        }

        overlayImage.color = WithAlpha(overlayColor, toAlpha);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
