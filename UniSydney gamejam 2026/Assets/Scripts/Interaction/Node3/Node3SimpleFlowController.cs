using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField] private SceneTextUIController textUI;
    [SerializeField] private float readingSeconds = 4f;

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

        if (textUI != null)
        {
            textUI.HideAll();
            textUI.ShowBriefing();
        }
        else
        {
            Debug.LogWarning("Node3SimpleFlowController: SceneTextUIController is not assigned.");
        }

        yield return new WaitForSeconds(readingSeconds);

        if (textUI != null)
        {
            textUI.HideBriefing();
        }

        GameSessionData.CurrentPhase = GameFlowPhase.CardCrafting;
        LoadSceneByName(cardBackpackSceneName);
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
