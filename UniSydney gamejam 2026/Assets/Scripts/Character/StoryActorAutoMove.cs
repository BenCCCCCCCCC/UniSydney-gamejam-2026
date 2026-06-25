using UnityEngine;

public class StoryActorAutoMove : MonoBehaviour
{
    [Header("Movement Points")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float arriveDistance = 0.05f;

    private bool isMoving;

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

            Debug.Log("Actor reached end point.");
            OnReachedEnd?.Invoke();
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

        isMoving = true;

        Debug.Log($"STORY_ACTOR_RESUME: {gameObject.name}, from {transform.position}, to {endPoint.position}, speed = {moveSpeed}");
    }

    // Compatibility method for older test scripts like TemporaryPlayStarter.
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
    }

    public void StopAtEnd()
    {
        if (endPoint != null)
        {
            transform.position = endPoint.position;
        }

        isMoving = false;
    }
}