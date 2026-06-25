using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class CameraFocusTrigger : MonoBehaviour
{
    [SerializeField, Min(0f)] private float triggerHoldTime = 1.1f;
    [SerializeField] private Transform focusPoint;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallForCurrentScene()
    {
        InstallOnPlacementTriggers();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallOnPlacementTriggers();
    }

    private static void InstallOnPlacementTriggers()
    {
        PlacementTriggerZone[] placementTriggers =
            FindObjectsByType<PlacementTriggerZone>(FindObjectsInactive.Exclude);

        foreach (PlacementTriggerZone placementTrigger in placementTriggers)
        {
            if (placementTrigger == null
                || placementTrigger.GetComponent<CameraFocusTrigger>() != null)
            {
                continue;
            }

            placementTrigger.gameObject.AddComponent<CameraFocusTrigger>();
        }
    }

    private void OnValidate()
    {
        triggerHoldTime = Mathf.Max(0f, triggerHoldTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsGameplayActor(other))
        {
            return;
        }

        SmoothCameraFollow cameraFollow = GetCameraFollow();
        if (cameraFollow == null)
        {
            return;
        }

        Vector3 focusWorldPosition = focusPoint != null
            ? focusPoint.position
            : transform.position;
        cameraFollow.BeginFocus(focusWorldPosition, triggerHoldTime);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsGameplayActor(other))
        {
            return;
        }

        GetCameraFollow()?.EndFocus();
    }

    private static SmoothCameraFollow GetCameraFollow()
    {
        Camera mainCamera = Camera.main;
        return mainCamera != null
            ? mainCamera.GetComponent<SmoothCameraFollow>()
            : null;
    }

    private static bool IsGameplayActor(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        if (other.GetComponentInParent<StoryActorAutoMove>() != null)
        {
            return true;
        }

        return HasTag(other.gameObject, "Player")
            || HasTag(other.gameObject, "StoryActor");
    }

    private static bool HasTag(GameObject target, string tagName)
    {
        try
        {
            return target.CompareTag(tagName);
        }
        catch (UnityException)
        {
            return false;
        }
    }
}
