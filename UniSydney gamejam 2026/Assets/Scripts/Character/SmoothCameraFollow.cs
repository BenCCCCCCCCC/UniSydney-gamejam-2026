using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class SmoothCameraFollow : MonoBehaviour
{
    private static bool warnedAboutMissingRuntimeTarget;
    private static bool warnedAboutMissingMainCamera;

    [Header("Follow Target")]
    [SerializeField] private Transform target;

    [Header("Follow Motion")]
    [SerializeField, Min(0.01f)] private float smoothTime = 0.3f;
    [SerializeField, Min(0f)] private float maxAnchorOffset = 0.3f;
    [SerializeField, Min(0f)] private float targetFollowInfluence = 0.04f;

    [Header("Cinematic Sway")]
    [SerializeField] private Vector2 swayAmount = new Vector2(0.05f, 0.03f);
    [SerializeField, Min(0f)] private float swaySpeed = 0.6f;

    [Header("Focus / Zoom")]
    [SerializeField, Min(0f)] private float zoomAmount = 0.65f;
    [SerializeField, Min(0.01f)] private float zoomTime = 0.3f;
    [SerializeField, Min(0f)] private float triggerHoldTime = 1.1f;

    private Camera controlledCamera;
    private Transform trackedTarget;
    private Vector3 anchorPosition;
    private Vector3 initialTargetPosition;
    private Vector3 smoothVelocity;
    private Vector3 previousTargetPosition;
    private Vector2 focusAnchorOffset;
    private float normalOrthographicSize;
    private float normalFieldOfView;
    private float zoomVelocity;
    private float movementBlend;
    private float movementBlendVelocity;
    private float focusEndTime;
    private bool hasFollowState;
    private bool focusActive;
    private bool warnedAboutMissingTarget;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallForCurrentScene()
    {
        InstallIfGameplayCameraAvailable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallIfGameplayCameraAvailable();
    }

    private static void InstallIfGameplayCameraAvailable()
    {
        Transform followTarget = FindGameplayTarget();
        if (followTarget == null)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!warnedAboutMissingRuntimeTarget
                && activeScene.IsValid()
                && activeScene.name.StartsWith("Node", System.StringComparison.Ordinal))
            {
                Debug.LogWarning(
                    $"SmoothCameraFollow: no player or StoryActor target was found in {activeScene.name}.");
                warnedAboutMissingRuntimeTarget = true;
            }

            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            if (!warnedAboutMissingMainCamera)
            {
                Debug.LogWarning("SmoothCameraFollow: gameplay target found, but Camera.main is missing.");
                warnedAboutMissingMainCamera = true;
            }

            return;
        }

        SmoothCameraFollow follow = mainCamera.GetComponent<SmoothCameraFollow>();
        if (follow == null)
        {
            follow = mainCamera.gameObject.AddComponent<SmoothCameraFollow>();
        }

        follow.SetTargetIfMissing(followTarget);
    }

    private void Awake()
    {
        controlledCamera = GetComponent<Camera>();
        anchorPosition = transform.position;
        CacheNormalZoom();
    }

    private void OnEnable()
    {
        if (controlledCamera == null)
        {
            controlledCamera = GetComponent<Camera>();
            CacheNormalZoom();
        }

        if (target == null)
        {
            target = FindGameplayTarget();
        }

        TryInitializeFollowState();
    }

    private void OnValidate()
    {
        smoothTime = Mathf.Max(0.01f, smoothTime);
        zoomTime = Mathf.Max(0.01f, zoomTime);
        swaySpeed = Mathf.Max(0f, swaySpeed);
        zoomAmount = Mathf.Max(0f, zoomAmount);
        triggerHoldTime = Mathf.Max(0f, triggerHoldTime);
        maxAnchorOffset = Mathf.Max(0f, maxAnchorOffset);
        targetFollowInfluence = Mathf.Max(0f, targetFollowInfluence);
        swayAmount.x = Mathf.Max(0f, swayAmount.x);
        swayAmount.y = Mathf.Max(0f, swayAmount.y);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            target = FindGameplayTarget();
            if (target == null)
            {
                WarnAboutMissingTarget();
                return;
            }
        }

        if (!hasFollowState || trackedTarget != target)
        {
            TryInitializeFollowState();
            return;
        }

        if (focusActive && Time.unscaledTime >= focusEndTime)
        {
            EndFocus();
        }

        Vector3 targetPosition = target.position;
        Vector3 targetMovement = targetPosition - previousTargetPosition;
        previousTargetPosition = targetPosition;

        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 targetVelocity = targetMovement / deltaTime;
        float targetMovementBlend = focusActive || targetVelocity.sqrMagnitude < 0.0001f
            ? 0f
            : 1f;
        movementBlend = Mathf.SmoothDamp(
            movementBlend,
            targetMovementBlend,
            ref movementBlendVelocity,
            0.2f,
            Mathf.Infinity,
            Time.deltaTime);

        float swayPhase = Time.time * swaySpeed * Mathf.PI * 2f;
        Vector2 swayOffset = new Vector2(
            Mathf.Sin(swayPhase) * swayAmount.x,
            Mathf.Sin(swayPhase * 0.73f + 0.8f) * swayAmount.y) * movementBlend;

        Vector2 targetOffset = new Vector2(
            targetPosition.x - initialTargetPosition.x,
            targetPosition.y - initialTargetPosition.y) * targetFollowInfluence;
        Vector2 requestedOffset = focusActive
            ? focusAnchorOffset
            : targetOffset + swayOffset;
        Vector2 clampedOffset = Vector2.ClampMagnitude(requestedOffset, maxAnchorOffset);
        Vector3 desiredPosition = anchorPosition
            + new Vector3(clampedOffset.x, clampedOffset.y, 0f);

        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref smoothVelocity,
            focusActive ? smoothTime * 1.35f : smoothTime,
            Mathf.Infinity,
            Time.deltaTime);
        transform.position = ClampToAnchor(smoothedPosition);

        UpdateZoom();
    }

    public void BeginFocus(Vector3 worldPosition, float holdDuration = -1f)
    {
        if (!hasFollowState)
        {
            return;
        }

        Vector2 focusDirection = new Vector2(
            worldPosition.x - initialTargetPosition.x,
            worldPosition.y - initialTargetPosition.y);
        focusAnchorOffset = focusDirection.sqrMagnitude > 0.0001f
            ? focusDirection.normalized * maxAnchorOffset
            : Vector2.zero;
        focusEndTime = Time.unscaledTime
            + (holdDuration >= 0f ? holdDuration : triggerHoldTime);
        focusActive = true;
    }

    public void EndFocus()
    {
        focusActive = false;
        focusEndTime = 0f;
    }

    private void SetTargetIfMissing(Transform followTarget)
    {
        if (target != null || followTarget == null)
        {
            return;
        }

        target = followTarget;
        TryInitializeFollowState();
    }

    private void TryInitializeFollowState()
    {
        if (target == null)
        {
            WarnAboutMissingTarget();
            return;
        }

        trackedTarget = target;
        initialTargetPosition = target.position;
        previousTargetPosition = target.position;
        smoothVelocity = Vector3.zero;
        movementBlend = 0f;
        movementBlendVelocity = 0f;
        hasFollowState = true;
        warnedAboutMissingTarget = false;
    }

    private void CacheNormalZoom()
    {
        if (controlledCamera == null)
        {
            return;
        }

        normalOrthographicSize = controlledCamera.orthographicSize;
        normalFieldOfView = controlledCamera.fieldOfView;
    }

    private void UpdateZoom()
    {
        if (controlledCamera == null)
        {
            return;
        }

        if (controlledCamera.orthographic)
        {
            float targetSize = focusActive
                ? Mathf.Max(0.01f, normalOrthographicSize - zoomAmount)
                : normalOrthographicSize;
            controlledCamera.orthographicSize = Mathf.SmoothDamp(
                controlledCamera.orthographicSize,
                targetSize,
                ref zoomVelocity,
                zoomTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
        }
        else
        {
            float targetFov = focusActive
                ? Mathf.Max(1f, normalFieldOfView - zoomAmount)
                : normalFieldOfView;
            controlledCamera.fieldOfView = Mathf.SmoothDamp(
                controlledCamera.fieldOfView,
                targetFov,
                ref zoomVelocity,
                zoomTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
        }
    }

    private Vector3 ClampToAnchor(Vector3 position)
    {
        Vector2 offset = new Vector2(
            position.x - anchorPosition.x,
            position.y - anchorPosition.y);
        offset = Vector2.ClampMagnitude(offset, maxAnchorOffset);
        return new Vector3(
            anchorPosition.x + offset.x,
            anchorPosition.y + offset.y,
            anchorPosition.z);
    }

    private static Transform FindGameplayTarget()
    {
        Transform taggedTarget = FindTaggedTarget("Player");
        if (taggedTarget != null)
        {
            return taggedTarget;
        }

        string[] fallbackNames = { "Player", "StoryActor", "SnowWhite_Placeholder", "Queen_Placeholder" };
        foreach (string fallbackName in fallbackNames)
        {
            GameObject candidate = GameObject.Find(fallbackName);
            if (candidate != null
                && candidate.GetComponentInParent<StoryActorAutoMove>() != null)
            {
                return candidate.transform;
            }
        }

        StoryActorAutoMove actor = FindAnyObjectByType<StoryActorAutoMove>();
        if (actor != null && actor.gameObject.activeInHierarchy)
        {
            return actor.transform;
        }

        taggedTarget = FindTaggedTarget("StoryActor");
        if (taggedTarget != null)
        {
            return taggedTarget;
        }

        return null;
    }

    private static Transform FindTaggedTarget(string tagName)
    {
        try
        {
            GameObject taggedObject = GameObject.FindGameObjectWithTag(tagName);
            return taggedObject != null ? taggedObject.transform : null;
        }
        catch (UnityException)
        {
            return null;
        }
    }

    private void WarnAboutMissingTarget()
    {
        if (warnedAboutMissingTarget)
        {
            return;
        }

        Debug.LogWarning(
            "SmoothCameraFollow: no gameplay follow target was found. Camera follow is paused.",
            this);
        warnedAboutMissingTarget = true;
    }
}
