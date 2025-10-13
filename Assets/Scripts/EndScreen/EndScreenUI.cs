using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;


public class EndScreenUI : MonoBehaviour
{
    [SerializeField]
    private ExtractionZone extractionZone;

    [SerializeField]
    private PlayerHealth playerHealth;
    [SerializeField]
    private GameObject winScreen;
    [SerializeField]
    private GameObject loseScreen;

    private GameObject activeScreen;

    void OnEnable()
    {
        this.extractionZone.WinScreen += OnWinScreen;
        this.playerHealth.LoseScreen += OnLoseScreen;
    }

    void OnDisable()
    {
        this.extractionZone.WinScreen -= OnWinScreen;
        this.playerHealth.LoseScreen -= OnLoseScreen;

    }

    private void OnWinScreen()
    {
        this.activeScreen = Instantiate(winScreen);

        // go back to Start scene
        StartCoroutine(LoadSceneAfterDelay("Jared", 5f)); // 5 second delay
    }

    private void OnLoseScreen()
    {
        this.activeScreen = Instantiate(loseScreen);

        // go back to Start scene
        StartCoroutine(LoadSceneAfterDelay("Jared", 5f)); // 5 second delay
    }
    
    private IEnumerator LoadSceneAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }

}