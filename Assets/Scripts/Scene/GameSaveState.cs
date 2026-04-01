using UnityEngine;

/// <summary>
/// Persistent save state using PlayerPrefs.
/// track whether the player has completed the tutorial
/// </summary>
public static class GameSaveState
{
    private const string TutorialCompleteKey = "TutorialComplete";

    public static bool HasCompletedTutorial
    {
        get => PlayerPrefs.GetInt(TutorialCompleteKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(TutorialCompleteKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Resets all save state (useful for testing or a "reset progress" button).
    /// </summary>
    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(TutorialCompleteKey);
        PlayerPrefs.Save();
    }
}
