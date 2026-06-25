using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class Node5SimpleFlowController : MonoBehaviour
{
    [Header("Node")]
    [SerializeField] private string nodeID = "Node5";
    [SerializeField] private string nodeSceneName = "Node5";
    [SerializeField] private string cardBackpackSceneName = "CardBackpackTest";

    [Header("Briefing")]
    [SerializeField] private SceneTextUIController textUI;
    [SerializeField] private float briefingDuration = 3.2f;

    private IEnumerator Start()
    {
        if (GameSessionData.CurrentPhase == GameFlowPhase.Placement ||
            GameSessionData.CurrentPhase == GameFlowPhase.AutoPlay ||
            GameSessionData.CurrentPhase == GameFlowPhase.Result)
        {
            yield break;
        }

        GameSessionData.StartNode(nodeID, nodeSceneName);
        GameSessionData.CardBackpackSceneName = cardBackpackSceneName;
        GameSessionData.CurrentPhase = GameFlowPhase.Briefing;

        if (textUI != null)
        {
            textUI.HideAll();
            textUI.ShowBriefing();
        }
        else
        {
            Debug.LogWarning("Node5SimpleFlowController: SceneTextUIController is not assigned.");
        }

        yield return new WaitForSeconds(briefingDuration);

        if (textUI != null)
        {
            textUI.HideBriefing();
        }

        yield return new WaitForEndOfFrame();
        GameSessionData.SetCardBackpackBackgroundSnapshot(
            ScreenCapture.CaptureScreenshotAsTexture());

        GameSessionData.CurrentPhase = GameFlowPhase.CardCrafting;

        Debug.Log($"Node5SimpleFlowController: loading scene {cardBackpackSceneName}");

        LoadSceneByName(cardBackpackSceneName);
    }

    private void LoadSceneByName(string sceneName)
    {
        Debug.Log($"Node5SimpleFlowController loading scene: {sceneName}");

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
