using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BookVisualLayerRuntime : MonoBehaviour
{
    private const string FirstGameplayScene = "Node1_QueenCastle";
    private const string BookSpriteName = "bg_book_0";
    private const string RuntimeVisualName = "Runtime_bg_book_0";
    private const int DefaultSortingOrder = 5;
#if UNITY_EDITOR
    private const string BookAssetPath = "Assets/Art/UI/bg_book.png";
#endif

    private static BookVisualLayerRuntime instance;
    private static Sprite cachedSprite;
    private static Vector3 cachedCameraOffset = new Vector3(0f, 0f, 10f);
    private static Quaternion cachedRotation = Quaternion.identity;
    private static Vector3 cachedScale = Vector3.one;
    private static Color cachedColor = Color.white;
    private static string cachedParentName = "Background";
    private static int cachedLayer;
    private static int cachedSortingLayerId;
    private static int cachedSortingOrder = DefaultSortingOrder;
    private static Material cachedMaterial;
    private static bool hasCapturedSetup;
    private static bool warnedMissingSprite;

    private GameObject currentVisual;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (instance == null)
        {
            GameObject helperObject = new GameObject(nameof(BookVisualLayerRuntime));
            DontDestroyOnLoad(helperObject);
            instance = helperObject.AddComponent<BookVisualLayerRuntime>();
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        instance.ApplyToScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        instance?.ApplyToScene(scene);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    private void ApplyToScene(Scene scene)
    {
        ClearCurrentVisual();

        if (!IsGameplayScene(scene.name))
        {
            return;
        }

        SpriteRenderer existingBook = FindBookRenderer();
        if (existingBook != null)
        {
            CaptureSetup(existingBook);
            return;
        }

        if (scene.name == FirstGameplayScene)
        {
            TryLoadSpriteFallback();
            return;
        }

        if (cachedSprite == null)
        {
            TryLoadSpriteFallback();
        }

        if (cachedSprite == null)
        {
            WarnMissingSprite(scene.name);
            return;
        }

        CreateVisualForCurrentScene(scene);
    }

    private static bool IsGameplayScene(string sceneName)
    {
        return !string.IsNullOrWhiteSpace(sceneName)
            && sceneName.StartsWith("Node", System.StringComparison.Ordinal);
    }

    private static SpriteRenderer FindBookRenderer()
    {
        SpriteRenderer[] renderers =
            FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || renderer.gameObject.name == RuntimeVisualName)
            {
                continue;
            }

            if ((renderer.sprite != null && renderer.sprite.name == BookSpriteName)
                || renderer.gameObject.name == BookSpriteName)
            {
                return renderer;
            }
        }

        return null;
    }

    private static void CaptureSetup(SpriteRenderer source)
    {
        cachedSprite = source.sprite;
        cachedRotation = source.transform.rotation;
        cachedScale = source.transform.lossyScale;
        cachedColor = source.color;
        cachedParentName = source.transform.parent != null
            ? source.transform.parent.name
            : "Background";
        cachedLayer = source.gameObject.layer;
        cachedSortingLayerId = source.sortingLayerID;
        cachedSortingOrder = ResolveVisualSortingOrder(source.sortingOrder);
        cachedMaterial = source.sharedMaterial;

        Camera camera = Camera.main;
        cachedCameraOffset = camera != null
            ? source.transform.position - camera.transform.position
            : source.transform.position;

        hasCapturedSetup = true;
        warnedMissingSprite = false;

        Debug.Log(
            $"BookVisualLayerRuntime: captured {BookSpriteName} from {source.gameObject.scene.name}; "
            + $"position={source.transform.position}, scale={cachedScale}, "
            + $"sortingOrder={cachedSortingOrder}.");
    }

    private void CreateVisualForCurrentScene(Scene scene)
    {
        GameObject visualObject = new GameObject(RuntimeVisualName, typeof(SpriteRenderer));
        SceneManager.MoveGameObjectToScene(visualObject, scene);
        visualObject.layer = cachedLayer;

        GameObject parentObject = GameObject.Find(cachedParentName);
        if (parentObject == null)
        {
            parentObject = GameObject.Find("Background");
        }

        if (parentObject != null && parentObject.scene == scene)
        {
            visualObject.transform.SetParent(parentObject.transform, false);
        }

        Camera camera = Camera.main;
        Vector3 visualPosition = hasCapturedSetup && camera != null
            ? camera.transform.position + cachedCameraOffset
            : GetDefaultPosition(camera);

        Transform visualTransform = visualObject.transform;
        visualTransform.position = visualPosition;
        visualTransform.rotation = cachedRotation;
        visualTransform.localScale = cachedScale;

        SpriteRenderer renderer = visualObject.GetComponent<SpriteRenderer>();
        renderer.sprite = cachedSprite;
        renderer.color = cachedColor;
        renderer.sortingLayerID = cachedSortingLayerId;
        renderer.sortingOrder = cachedSortingOrder;
        renderer.sharedMaterial = cachedMaterial;
        renderer.maskInteraction = SpriteMaskInteraction.None;

        currentVisual = visualObject;
    }

    private static Vector3 GetDefaultPosition(Camera camera)
    {
        if (camera == null)
        {
            return Vector3.zero;
        }

        return new Vector3(
            camera.transform.position.x,
            camera.transform.position.y,
            0f);
    }

    private static int ResolveVisualSortingOrder(int sourceOrder)
    {
        // Gameplay backgrounds use order 0 and middle scenery begins around 10.
        // Keep the book between them even if its source renderer was accidentally moved.
        return sourceOrder > 0 && sourceOrder < 10
            ? sourceOrder
            : DefaultSortingOrder;
    }

    private static void TryLoadSpriteFallback()
    {
        if (cachedSprite != null)
        {
            return;
        }

        Sprite[] loadedSprites = Resources.FindObjectsOfTypeAll<Sprite>();
        foreach (Sprite sprite in loadedSprites)
        {
            if (sprite != null && sprite.name == BookSpriteName)
            {
                cachedSprite = sprite;
                break;
            }
        }

#if UNITY_EDITOR
        if (cachedSprite == null)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(BookAssetPath);
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite && sprite.name == BookSpriteName)
                {
                    cachedSprite = sprite;
                    break;
                }
            }
        }
#endif
    }

    private static void WarnMissingSprite(string sceneName)
    {
        if (warnedMissingSprite)
        {
            return;
        }

        Debug.LogWarning(
            $"BookVisualLayerRuntime: {BookSpriteName} is unavailable in {sceneName}. "
            + "Play from the first gameplay scene so its setup can be captured.");
        warnedMissingSprite = true;
    }

    private void ClearCurrentVisual()
    {
        if (currentVisual == null)
        {
            return;
        }

        Destroy(currentVisual);
        currentVisual = null;
    }
}
