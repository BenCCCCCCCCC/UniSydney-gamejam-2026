using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class Node1SimpleFlowController : MonoBehaviour
{
    [Header("Node")]
    [SerializeField] private string nodeID = "Node1";
    [SerializeField] private string nodeSceneName = "Node1_QueenCastle";
    [SerializeField] private string cardBackpackSceneName = "CardBackpackTest";

    [Header("Briefing")]
    [SerializeField] private string briefingText = "The Queen becomes jealous of Snow White and starts to see her as a threat.";
    [SerializeField] private float briefingDuration = 2.4f;

    private GameObject canvasObject;
    private GameObject briefingPanelObject;
    private TMP_Text briefingTMP;

    private IEnumerator Start()
    {
        // 如果是从卡牌系统 Continue 回来的 Placement 阶段，就不要再次播放旁白、不要再次跳卡牌系统。
        if (GameSessionData.CurrentPhase == GameFlowPhase.Placement ||
            GameSessionData.CurrentPhase == GameFlowPhase.AutoPlay ||
            GameSessionData.CurrentPhase == GameFlowPhase.Result)
        {
            yield break;
        }

        GameSessionData.StartNode(nodeID, nodeSceneName);
        GameSessionData.CardBackpackSceneName = cardBackpackSceneName;
        GameSessionData.CurrentPhase = GameFlowPhase.Briefing;

        BuildBriefingUI();
        ShowBriefing(briefingText);

        yield return new WaitForSeconds(briefingDuration);

        HideBriefing();

        GameSessionData.CurrentPhase = GameFlowPhase.CardCrafting;

        Debug.Log($"Node1SimpleFlowController: loading scene {cardBackpackSceneName}");

        LoadSceneByName(cardBackpackSceneName);
    }

    private void BuildBriefingUI()
    {
        EnsureEventSystem();

        if (canvasObject != null)
        {
            return;
        }

        canvasObject = new GameObject(
            "Node1BriefingCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();

        briefingPanelObject = new GameObject(
            "BriefingPanel",
            typeof(RectTransform),
            typeof(Image));

        briefingPanelObject.transform.SetParent(canvasRect, false);

        Image image = briefingPanelObject.GetComponent<Image>();
        image.color = new Color(0.04f, 0.045f, 0.06f, 0.92f);

        RectTransform panelRect = briefingPanelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.18f);
        panelRect.anchorMax = new Vector2(0.5f, 0.18f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1060f, 150f);
        panelRect.anchoredPosition = Vector2.zero;

        GameObject textObject = new GameObject(
            "BriefingText",
            typeof(RectTransform),
            typeof(TextMeshProUGUI));

        textObject.transform.SetParent(briefingPanelObject.transform, false);

        briefingTMP = textObject.GetComponent<TMP_Text>();
        briefingTMP.alignment = TextAlignmentOptions.Center;
        briefingTMP.color = Color.white;
        briefingTMP.fontSize = 34f;
        briefingTMP.textWrappingMode = TextWrappingModes.Normal;
        briefingTMP.enableAutoSizing = true;
        briefingTMP.fontSizeMin = 18f;
        briefingTMP.fontSizeMax = 34f;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(40f, 20f);
        textRect.offsetMax = new Vector2(-40f, -20f);
    }

    private void ShowBriefing(string message)
    {
        if (briefingPanelObject == null || briefingTMP == null)
        {
            return;
        }

        briefingPanelObject.SetActive(true);
        briefingTMP.text = message;
    }

    private void HideBriefing()
    {
        if (briefingPanelObject != null)
        {
            briefingPanelObject.SetActive(false);
        }
    }

    private void LoadSceneByName(string sceneName)
    {
        Debug.Log($"Node1SimpleFlowController loading scene: {sceneName}");

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

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));

        eventSystemObject.SetActive(true);
    }
}