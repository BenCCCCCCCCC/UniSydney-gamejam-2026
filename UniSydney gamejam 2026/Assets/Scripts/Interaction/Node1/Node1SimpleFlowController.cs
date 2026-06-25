using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField] private SceneTextUIController textUI;
    [SerializeField] private float briefingDuration = 2.4f;

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
            Debug.LogWarning("Node1SimpleFlowController: SceneTextUIController is not assigned.");
        }

        yield return new WaitForSeconds(briefingDuration);

        if (textUI != null)
        {
            textUI.HideBriefing();
        }

        GameSessionData.CurrentPhase = GameFlowPhase.CardCrafting;

        Debug.Log($"Node1SimpleFlowController: loading scene {cardBackpackSceneName}");

        LoadSceneByName(cardBackpackSceneName);
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
}
