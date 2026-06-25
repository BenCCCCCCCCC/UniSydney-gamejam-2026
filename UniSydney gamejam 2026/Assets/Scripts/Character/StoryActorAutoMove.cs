using UnityEngine;

public class StoryActorAutoMove : MonoBehaviour
{
    [Header("Movement Points")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("Movement Settings")]
    public float moveSpeed = 2f;

    public System.Action OnReachedEnd;

    private bool isPlaying = false;
    private bool isPaused = false;

    private void Start()
    {
        if (startPoint != null)
        {
            transform.position = startPoint.position;
        }
    }

    private void Update()
    {
        if (!isPlaying || isPaused || endPoint == null)
        {
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            endPoint.position,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, endPoint.position) < 0.05f)
        {
            isPlaying = false;
            Debug.Log("Actor reached end point.");
            OnReachedEnd?.Invoke();
        }
    }

    public void StartPlay()
    {
        if (startPoint != null)
        {
            transform.position = startPoint.position;
        }

        // 重置场景内所有触发区，确保重播时每个槽都能再次触发
        foreach (var zone in FindObjectsByType<PlacementTriggerZone>())
        {
            zone.ResetTrigger();
        }

        isPlaying = true;
        isPaused = false;
        Debug.Log("Play started.");
    }

    public void PauseMove()
    {
        isPaused = true;
    }

    public void ResumeMove()
    {
        isPaused = false;
    }

    public void StopMove()
    {
        isPlaying = false;
        isPaused = false;
    }
}