using UnityEngine;
using UnityEngine.InputSystem;

public class TemporaryPlayStarter : MonoBehaviour
{
    public StoryActorAutoMove actor;

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (actor == null)
            {
                Debug.LogWarning("TemporaryPlayStarter: Actor is not assigned.");
                return;
            }

            actor.StartPlay();
        }
    }
}