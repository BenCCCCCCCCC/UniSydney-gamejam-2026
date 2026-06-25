using UnityEngine;

/// <summary>
/// Anticipation pulse for the spawned world-space tool/item icon only.
/// It never attaches to hand cards or placed CardView UI.
/// </summary>
public sealed class PlacedToolItemPulseEffect : MonoBehaviour
{
    private const float DefaultAnticipationDistance = 1.75f;
    private const float DefaultPulseDuration = 0.5f;
    private const float PeakScaleMultiplier = 1.1f;

    private PlacementPoint placementPoint;
    private Transform actor;
    private Vector3 triggerWorldPosition;
    private Vector3 baseScale = Vector3.one;
    private float anticipationDistance = DefaultAnticipationDistance;
    private float pulseDuration = DefaultPulseDuration;
    private float pulseElapsed;
    private bool isPulsing;
    private bool hasActivated;

    public void Configure(
        PlacementPoint point,
        Vector3 fallbackTriggerPosition,
        Vector3 stableBaseScale)
    {
        placementPoint = point;
        triggerWorldPosition = ResolveTriggerPosition(point, fallbackTriggerPosition);
        baseScale = stableBaseScale;
        transform.localScale = baseScale;

        actor = FindStoryActor();
    }

    private void OnEnable()
    {
        PlacementTriggerZone.OnToolPlaced += HandleToolActivated;
    }

    private void OnDisable()
    {
        PlacementTriggerZone.OnToolPlaced -= HandleToolActivated;
        StopPulseAndReset();
    }

    private void Update()
    {
        if (hasActivated)
        {
            return;
        }

        if (actor == null)
        {
            actor = FindStoryActor();
            if (actor == null)
            {
                return;
            }
        }

        if (!isPulsing)
        {
            float distance = Vector2.Distance(actor.position, triggerWorldPosition);
            if (distance > anticipationDistance)
            {
                return;
            }

            isPulsing = true;
            pulseElapsed = 0f;
        }

        pulseElapsed += Time.deltaTime;
        float cycle = pulseDuration > 0f
            ? Mathf.Repeat(pulseElapsed, pulseDuration) / pulseDuration
            : 0f;

        // 1.0 -> 1.1 -> 1.0, equivalent to 1.2 -> 1.32 -> 1.2.
        float pulse = Mathf.Sin(cycle * Mathf.PI);
        transform.localScale =
            baseScale * Mathf.Lerp(1f, PeakScaleMultiplier, pulse);
    }

    private void HandleToolActivated(PlacementPoint activatedPoint)
    {
        if (activatedPoint != placementPoint)
        {
            return;
        }

        hasActivated = true;
        StopPulseAndReset();
    }

    private void StopPulseAndReset()
    {
        isPulsing = false;
        pulseElapsed = 0f;
        transform.localScale = baseScale;
    }

    private static Transform FindStoryActor()
    {
        GameObject actorObject = GameObject.FindGameObjectWithTag("StoryActor");
        return actorObject != null ? actorObject.transform : null;
    }

    private static Vector3 ResolveTriggerPosition(
        PlacementPoint point,
        Vector3 fallbackPosition)
    {
        if (point == null)
        {
            return fallbackPosition;
        }

        PlacementTriggerZone[] zones =
            FindObjectsByType<PlacementTriggerZone>(FindObjectsInactive.Include);

        foreach (PlacementTriggerZone zone in zones)
        {
            if (zone == null || zone.placementPoint != point)
            {
                continue;
            }

            Collider2D triggerCollider = zone.GetComponent<Collider2D>();
            return triggerCollider != null
                ? triggerCollider.bounds.center
                : zone.transform.position;
        }

        return fallbackPosition;
    }
}
