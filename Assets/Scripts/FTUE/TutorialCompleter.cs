using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialCompleter : MonoBehaviour
{
    [SerializeField] private string mainSceneName = "Main";

    /// <summary>
    /// mark the tutorial as complete and transition to the main game scene
    /// </summary>
    public void CompleteTutorial()
    {
        GameSaveState.HasCompletedTutorial = true;
        SceneManager.LoadScene(mainSceneName);
    }
}
