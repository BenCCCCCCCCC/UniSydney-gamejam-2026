using UnityEngine;

public class Node5TextBank : MonoBehaviour
{
    [Header("N5_P1: Prince route point")]
    [SerializeField] private string princeRoutePlus = "The prince receives help and finds the way forward.";
    [SerializeField] private string princeRouteNeutral = "The prince is unsure where to go.";
    [SerializeField] private string princeRouteMinus = "The prince gets lost.";

    [Header("N5_P2: Dwarfs rescue point")]
    [SerializeField] private string dwarfsRescuePlus = "The dwarfs receive useful help.";
    [SerializeField] private string dwarfsRescueNeutral = "The dwarfs are not sure how to help.";
    [SerializeField] private string dwarfsRescueMinus = "The dwarfs are thrown into trouble.";

    [Header("N5_P3: Crystal coffin / seal point")]
    [SerializeField] private string crystalCoffinPlus = "The crystal coffin begins to open.";
    [SerializeField] private string crystalCoffinNeutral = "The crystal coffin does not react.";
    [SerializeField] private string crystalCoffinMinus = "The seal around the crystal coffin grows stronger.";

    [Header("Endings")]
    [SerializeField] private string badEndingTitle = "Bad Ending";
    [SerializeField] private string badEndingBody = "It seems Snow White has been sealed away forever.";
    [SerializeField] private string failedRescueTitle = "Failed Rescue";
    [SerializeField] private string failedRescueWithPrinceBody = "The prince and the dwarfs cannot rescue Snow White.";
    [SerializeField] private string failedRescueDwarfsBody = "The dwarfs cannot rescue Snow White.";
    [SerializeField] private string rescueEndingTitle = "Rescue Ending";
    [SerializeField] private string rescueEndingBody = "The prince and the dwarfs rescue Snow White.";
    [SerializeField] private string dwarfsRescueEndingTitle = "Dwarfs Rescue Ending";
    [SerializeField] private string dwarfsRescueEndingBody = "The dwarfs rescue Snow White.";

    public string GetFeedbackMessage(string placePointID, int delta)
    {
        if (placePointID == "N5_P1")
        {
            string message = GetByDelta(delta, princeRoutePlus, princeRouteNeutral, princeRouteMinus);
            if (delta == 0 && ContainsFlowerSpecificFallback(message))
            {
                return "The prince is unsure where to go.";
            }

            return message;
        }

        if (placePointID == "N5_P2")
        {
            return GetByDelta(delta, dwarfsRescuePlus, dwarfsRescueNeutral, dwarfsRescueMinus);
        }

        if (placePointID == "N5_P3")
        {
            return GetByDelta(delta, crystalCoffinPlus, crystalCoffinNeutral, crystalCoffinMinus);
        }

        return GetByDelta(delta, princeRoutePlus, princeRouteNeutral, princeRouteMinus);
    }

    public void GetEnding(int totalScore, bool princeCalled, out string title, out string body)
    {
        if (totalScore < 0)
        {
            title = badEndingTitle;
            body = badEndingBody;
            return;
        }

        if (totalScore == 0)
        {
            title = failedRescueTitle;
            body = princeCalled ? failedRescueWithPrinceBody : failedRescueDwarfsBody;
            return;
        }

        if (princeCalled)
        {
            title = rescueEndingTitle;
            body = rescueEndingBody;
            return;
        }

        title = dwarfsRescueEndingTitle;
        body = dwarfsRescueEndingBody;
    }

    private string GetByDelta(int delta, string plus, string neutral, string minus)
    {
        if (delta > 0)
        {
            return plus;
        }

        if (delta < 0)
        {
            return minus;
        }

        return neutral;
    }

    private static bool ContainsFlowerSpecificFallback(string feedback)
    {
        if (string.IsNullOrWhiteSpace(feedback))
        {
            return false;
        }

        return feedback.Contains("flower")
            || feedback.Contains("Flower")
            || feedback.Contains("Blooming Path")
            || feedback.Contains("Petal Path")
            || feedback.Contains("beautiful");
    }
}
