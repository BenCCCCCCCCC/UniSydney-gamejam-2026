using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Node4 开场流程：显示旁白 → 切换到翻牌场景。
/// </summary>
public class Node4SimpleFlowController : MonoBehaviour
{
    private static Material safeSpriteMaterial;

    [Header("节点设置")]
    [SerializeField] private string nodeID = "Node4";
    [SerializeField] private string nodeSceneName = "Node4_1";
    [SerializeField] private string cardBackpackSceneName = "CardBackpackTest";

    [Header("旁白")]
    [SerializeField] private string briefingLine = "（第四幕旁白内容，在 Inspector 中修改）";
    [SerializeField] private float readingSeconds = 4f;

    private IEnumerator Start()
    {
        EnsureSceneSpriteRenderersUseValidMaterials();

        GameSessionData.CurrentNodeID = nodeID;
        GameSessionData.CurrentNodeSceneName = nodeSceneName;
        GameSessionData.CardBackpackSceneName = cardBackpackSceneName;

        if (GameSessionData.CurrentPhase == GameFlowPhase.Placement)
        {
            Debug.Log("Node4SimpleFlowController: entered Placement phase.");
            yield break;
        }

        GameSessionData.CurrentPhase = GameFlowPhase.Briefing;

        ShowDialogueBubble(briefingLine);

        yield return new WaitForSeconds(readingSeconds);

        yield return new WaitForEndOfFrame();
        CaptureCardBackpackBackgroundSnapshot();

        GameSessionData.CurrentPhase = GameFlowPhase.CardCrafting;
        LoadSceneByName(cardBackpackSceneName);
    }

    private void CaptureCardBackpackBackgroundSnapshot()
    {
        Texture2D snapshot = ScreenCapture.CaptureScreenshotAsTexture();

        if (snapshot == null || snapshot.width <= 0 || snapshot.height <= 0)
        {
            Debug.LogWarning("Node4SimpleFlowController: background snapshot capture failed; CardBackpack will use a dim fallback background.");
            GameSessionData.ClearCardBackpackBackgroundSnapshot();
            return;
        }

        GameSessionData.SetCardBackpackBackgroundSnapshot(snapshot);
    }

    private void EnsureSceneSpriteRenderersUseValidMaterials()
    {
        SpriteRenderer[] spriteRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        Material fallbackMaterial = null;
        int fixedCount = 0;

        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            if (spriteRenderer == null || !HasBrokenMaterial(spriteRenderer.sharedMaterial))
            {
                continue;
            }

            fallbackMaterial ??= GetSafeSpriteMaterial();
            if (fallbackMaterial == null)
            {
                Debug.LogWarning("Node4SimpleFlowController: could not find a safe sprite shader for broken SpriteRenderer materials.");
                return;
            }

            spriteRenderer.sharedMaterial = fallbackMaterial;
            fixedCount++;
        }

        if (fixedCount > 0)
        {
            Debug.Log($"Node4SimpleFlowController: replaced {fixedCount} broken SpriteRenderer material(s) with a safe sprite material.");
        }
    }

    private static bool HasBrokenMaterial(Material material)
    {
        return material == null
            || material.shader == null
            || !material.shader.isSupported
            || material.shader.name == "Hidden/InternalErrorShader";
    }

    private static Material GetSafeSpriteMaterial()
    {
        if (safeSpriteMaterial != null)
        {
            return safeSpriteMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            return null;
        }

        safeSpriteMaterial = new Material(shader)
        {
            name = "Node4_RuntimeSafeSpriteMaterial"
        };

        return safeSpriteMaterial;
    }

    private void ShowDialogueBubble(string line)
    {
        var dialogueCanvas = new GameObject(
            "Node4DialogueCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = dialogueCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = dialogueCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = dialogueCanvas.GetComponent<RectTransform>();

        GameObject bubbleObject = new GameObject("DialogueBubble", typeof(RectTransform), typeof(Image));
        bubbleObject.transform.SetParent(canvasRect, false);
        bubbleObject.GetComponent<Image>().color = new Color(0.08f, 0.07f, 0.06f, 0.88f);

        RectTransform bubbleRect = bubbleObject.GetComponent<RectTransform>();
        bubbleRect.anchorMin = new Vector2(0.12f, 0.72f);
        bubbleRect.anchorMax = new Vector2(0.88f, 0.92f);
        bubbleRect.offsetMin = Vector2.zero;
        bubbleRect.offsetMax = Vector2.zero;

        GameObject textObject = new GameObject("DialogueText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(bubbleRect, false);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = line;
        text.fontSize = 34f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.04f, 0.08f);
        textRect.anchorMax = new Vector2(0.96f, 0.92f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void LoadSceneByName(string sceneName)
    {
        Debug.Log($"Node4SimpleFlowController: loading scene {sceneName}");

#if UNITY_EDITOR
        string scenePath = $"Assets/Scenes/{sceneName}.unity";
        if (SceneUtility.GetBuildIndexByScenePath(scenePath) < 0)
        {
            EditorSceneManager.LoadSceneInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        SceneManager.LoadScene(sceneName);
    }
}
