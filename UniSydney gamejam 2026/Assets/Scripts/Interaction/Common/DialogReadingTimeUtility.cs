using System;
using UnityEngine;

public static class DialogReadingTimeUtility
{
    public static float GetDuration(
        string message,
        bool useDynamic,
        float fallbackDuration,
        float minDuration,
        float maxDuration,
        float wordsPerMinute,
        float paddingSeconds,
        float punctuationExtraSeconds)
    {
        if (!useDynamic)
        {
            return fallbackDuration;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return fallbackDuration;
        }

        if (wordsPerMinute <= 0f)
        {
            return fallbackDuration;
        }

        int wordCount = CountWords(message);
        float wordsPerSecond = wordsPerMinute / 60f;
        float duration = wordCount / wordsPerSecond + paddingSeconds;

        string trimmed = message.Trim();
        if (trimmed.EndsWith("!") || trimmed.EndsWith("?") || trimmed.EndsWith(".") || trimmed.EndsWith("\""))
        {
            duration += punctuationExtraSeconds;
        }

        return Mathf.Clamp(duration, minDuration, maxDuration);
    }

    private static int CountWords(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return 0;
        }

        string[] parts = message.Split(
            new[] { ' ', '\n', '\t', '\r' },
            StringSplitOptions.RemoveEmptyEntries);

        return parts.Length;
    }
}
