using System;
using UnityEngine;

public class StoryActorAutoMove : MonoBehaviour
{
    public static event Action<StoryActorAutoMove> ActorReachedEnd;

    [Header("Movement Points")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float arriveDistance = 0.05f;

    private bool isMoving;
    private bool hasReachedEnd;

    public bool IsMoving => isMoving;
    public bool HasReachedEnd => hasReachedEnd;

    // 到达终点时触发，Node2_1ResultPlayer 等外部逻辑订阅此事件
    public System.Action OnReachedEnd;

    private void Start()
    {
        MoveToStart();
        PauseMove();
    }

    private void Update()
    {
        if (!isMoving)
        {
            return;
        }

        if (endPoint == null)
        {
            Debug.LogError($"{gameObject.name}: End Point is missing.");
            isMoving = false;
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            endPoint.position,
            moveSpeed * Time.deltaTime
        );

        float distanceToEnd = Vector3.Distance(transform.position, endPoint.position);

        if (distanceToEnd <= arriveDistance)
        {
            transform.position = endPoint.position;
            isMoving = false;
            hasReachedEnd = true;

            Debug.Log("Actor reached end point.");
            OnReachedEnd?.Invoke();
            ActorReachedEnd?.Invoke(this);
        }
    }

    public void ResumeMove()
    {
        if (startPoint == null)
        {
            Debug.LogError($"{gameObject.name}: Start Point is missing.");
            return;
        }

        if (endPoint == null)
        {
            Debug.LogError($"{gameObject.name}: End Point is missing.");
            return;
        }

        if (moveSpeed <= 0f)
        {
            Debug.LogError($"{gameObject.name}: Move Speed must be greater than 0.");
            return;
        }

        if (hasReachedEnd)
        {
            return;
        }

        isMoving = true;

        Debug.Log($"STORY_ACTOR_RESUME: {gameObject.name}, from {transform.position}, to {endPoint.position}, speed = {moveSpeed}");
    }

    public void StartPlay()
    {
        ResumeMove();
    }

    public void PauseMove()
    {
        isMoving = false;
    }

    public void MoveToStart()
    {
        if (startPoint == null)
        {
            return;
        }

        transform.position = startPoint.position;
        hasReachedEnd = false;
        isMoving = false;
    }

    public void StopAtEnd()
    {
        if (endPoint != null)
        {
            transform.position = endPoint.position;
        }

        hasReachedEnd = true;
        isMoving = false;
    }

    // 供转场系统动态更换路径（携带角色跨场景时使用）
    public void SetMovePath(Transform newStart, Transform newEnd)
    {
        startPoint = newStart;
        endPoint = newEnd;
        hasReachedEnd = false;
    }

    // 向下兼容旧调用（等同于 PauseMove）
    public void StopMove()
    {
        PauseMove();
    }
}
