using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class Node3SimpleFlowController : MonoBehaviour
{
    [Header("Node Settings")]
    [SerializeField] private string nodeID = "Node3";
    [SerializeField] private string nodeSceneName = "Node3_DwarfHouse";
    [SerializeField] private string cardBackpackSceneName = "CardBackpackTest";

    [Header("Briefing")]
    [SerializeField] private string briefingLine = "白雪公主在森林里迷了路，必须找到小矮人的屋子。";
    [SerializeField] private float readingSeconds = 4f;

    private GameObject dialogueCanvas;

    private IEnumerator Start()
    {
        GameSessionData.CurrentNodeID = nodeID;
        GameSessionData.CurrentNodeSceneName = nodeSceneName;
        GameSessionData.CardBackpackSceneName = cardBackpackSceneName;

        if (GameSessionData.CurrentPhase == GameFlowPhase.Placement)
        {
            Debug.Log("Node3SimpleFlowController: entered Placement phase.");
            yield break;
        }

        GameSessionData.CurrentPhase = GameFlowPhase.Briefing;

        ShowDialogueBubble(briefingLine);

        yield return new WaitForSeconds(readingSeconds);

        GameSessionData.CurrentPhase = GameFlowPhase.CardCrafting;
        LoadSceneByName(cardBackpackSceneName);
    }

    private void ShowDialogueBubble(string line)
    {
        dialogueCanvas = new GameObject(
            "Node3DialogueCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = dialogueCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = dialogueCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = dialogueCanvas.GetComponent<RectTransform>();

        GameObject bubbleObject = new GameObject(
            "DialogueBubble",
            typeof(RectTransform),
            typeof(Image));

        bubbleObject.transform.SetParent(canvasRect, false);

        Image bubbleImage = bubbleObject.GetComponent<Image>();
        bubbleImage.color = new Color(0.08f, 0.07f, 0.06f, 0.88f);

        RectTransform bubbleRect = bubbleObject.GetComponent<RectTransform>();
        bubbleRect.anchorMin = new Vector2(0.12f, 0.72f);
        bubbleRect.anchorMax = new Vector2(0.88f, 0.92f);
        bubbleRect.offsetMin = Vector2.zero;
        bubbleRect.offsetMax = Vector2.zero;

        GameObject textObject = new GameObject(
            "DialogueText",
            typeof(RectTransform),
            typeof(TextMeshProUGUI));

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
        Debug.Log($"Node3SimpleFlowController: loading scene {sceneName}");

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