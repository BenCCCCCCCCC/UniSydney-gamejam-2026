using System.Collections;
using TMPro;
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
    [SerializeField] private bool useDynamicReadingSeconds = true;
    [SerializeField] private float minReadingSeconds = 1.8f;
    [SerializeField] private float maxReadingSeconds = 5.5f;
    [SerializeField] private float readingWordsPerMinute = 220f;
    [SerializeField] private float readingPaddingSeconds = 0.7f;
    [SerializeField] private float punctuationExtraSeconds = 0.15f;
    [SerializeField, TextArea] private string briefingTextOverrideForDuration;

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

        yield return new WaitForSeconds(GetBriefingDuration());

        if (textUI != null)
        {
            textUI.HideBriefing();
        }

        yield return new WaitForEndOfFrame();
        GameSessionData.SetCardBackpackBackgroundSnapshot(
            ScreenCapture.CaptureScreenshotAsTexture());

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

    private float GetBriefingDuration()
    {
        string briefingText = GetBriefingTextForDuration();
        return DialogReadingTimeUtility.GetDuration(
            briefingText,
            useDynamicReadingSeconds,
            readingSeconds,
            minReadingSeconds,
            maxReadingSeconds,
            readingWordsPerMinute,
            readingPaddingSeconds,
            punctuationExtraSeconds);
    }

    private string GetBriefingTextForDuration()
    {
        if (!string.IsNullOrWhiteSpace(briefingTextOverrideForDuration))
        {
            return briefingTextOverrideForDuration;
        }

        if (textUI == null)
        {
            return "";
        }

        TMP_Text[] texts = textUI.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text != null && text.name.Contains("Briefing"))
            {
                return text.text;
            }
        }

        return "";
    }
}
