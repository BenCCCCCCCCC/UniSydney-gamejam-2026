using UnityEngine;

public class StoryActorFrameAnimator : MonoBehaviour
{
    private enum AnimationState
    {
        Idle,
        Walk
    }

    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private StoryActorAutoMove movementSource;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private float idleFrameRate = 4f;
    [SerializeField] private float walkFrameRate = 8f;
    [SerializeField] private float movementThreshold = 0.001f;
    [SerializeField] private bool useUnscaledTime;
    [SerializeField] private bool playIdleWhenNoFrames = true;
    [SerializeField] private bool logMissingFrames;

    private Vector3 lastPosition;
    private AnimationState currentState = AnimationState.Idle;
    private AnimationState forcedState;
    private bool hasForcedState;
    private int frameIndex;
    private float frameTimer;
    private bool loggedMissingIdleFrames;
    private bool loggedMissingWalkFrames;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (movementSource == null)
        {
            movementSource = GetComponent<StoryActorAutoMove>();
        }

        if (movementSource == null)
        {
            movementSource = GetComponentInParent<StoryActorAutoMove>();
        }

        lastPosition = transform.position;
        SetState(AnimationState.Idle);
        ApplyCurrentFrame();
    }

    private void Update()
    {
        AnimationState nextState = hasForcedState ? forcedState : GetMovementState();
        SetState(nextState);
        AdvanceFrame();
        lastPosition = transform.position;
    }

    public void ForceIdle()
    {
        forcedState = AnimationState.Idle;
        hasForcedState = true;
        SetState(AnimationState.Idle);
        ApplyCurrentFrame();
    }

    public void ForceWalk()
    {
        forcedState = AnimationState.Walk;
        hasForcedState = true;
        SetState(AnimationState.Walk);
        ApplyCurrentFrame();
    }

    public void ClearForcedState()
    {
        hasForcedState = false;
    }

    private AnimationState GetMovementState()
    {
        if (movementSource != null)
        {
            return movementSource.IsMoving ? AnimationState.Walk : AnimationState.Idle;
        }

        float movedDistance = Vector3.Distance(transform.position, lastPosition);
        return movedDistance > movementThreshold ? AnimationState.Walk : AnimationState.Idle;
    }

    private void SetState(AnimationState nextState)
    {
        if (currentState == nextState)
        {
            return;
        }

        currentState = nextState;
        frameIndex = 0;
        frameTimer = 0f;
        ApplyCurrentFrame();
    }

    private void AdvanceFrame()
    {
        Sprite[] activeFrames = GetActiveFrames();
        if (targetRenderer == null || activeFrames == null || activeFrames.Length == 0)
        {
            LogMissingFramesOnce();
            return;
        }

        float frameRate = currentState == AnimationState.Walk ? walkFrameRate : idleFrameRate;
        if (frameRate <= 0f)
        {
            ApplyCurrentFrame();
            return;
        }

        frameTimer += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float frameDuration = 1f / frameRate;

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex = (frameIndex + 1) % activeFrames.Length;
        }

        ApplyCurrentFrame();
    }

    private void ApplyCurrentFrame()
    {
        Sprite[] activeFrames = GetActiveFrames();
        if (targetRenderer == null || activeFrames == null || activeFrames.Length == 0)
        {
            LogMissingFramesOnce();
            return;
        }

        frameIndex = Mathf.Clamp(frameIndex, 0, activeFrames.Length - 1);
        targetRenderer.sprite = activeFrames[frameIndex];
    }

    private Sprite[] GetActiveFrames()
    {
        if (currentState == AnimationState.Walk)
        {
            if (walkFrames != null && walkFrames.Length > 0)
            {
                return walkFrames;
            }

            if (playIdleWhenNoFrames && idleFrames != null && idleFrames.Length > 0)
            {
                return idleFrames;
            }

            return walkFrames;
        }

        return idleFrames;
    }

    private void LogMissingFramesOnce()
    {
        if (!logMissingFrames)
        {
            return;
        }

        if (currentState == AnimationState.Walk)
        {
            if (loggedMissingWalkFrames)
            {
                return;
            }

            loggedMissingWalkFrames = true;
            Debug.LogWarning($"{name}: StoryActorFrameAnimator has no walk frames assigned.", this);
            return;
        }

        if (loggedMissingIdleFrames)
        {
            return;
        }

        loggedMissingIdleFrames = true;
        Debug.LogWarning($"{name}: StoryActorFrameAnimator has no idle frames assigned.", this);
    }
}
