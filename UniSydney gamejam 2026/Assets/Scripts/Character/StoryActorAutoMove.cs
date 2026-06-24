using UnityEngine;

public class StoryActorAutoMove : MonoBehaviour
{
    [Header("Movement Points")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("Movement Settings")]
    public float moveSpeed = 2f;

    private bool isPlaying = false;

    private void Start()
    {
        if (startPoint != null)
        {
            transform.position = startPoint.position;
        }
    }

    private void Update()
    {
        if (!isPlaying || endPoint == null)
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
        }
    }

    public void StartPlay()
    {
        if (startPoint != null)
        {
            transform.position = startPoint.position;
        }

        isPlaying = true;
        Debug.Log("Play started.");
    }
}