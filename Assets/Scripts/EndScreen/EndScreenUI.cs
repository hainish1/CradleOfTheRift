using UnityEngine;
using UnityEngine.UIElements;

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
    }

    private void OnLoseScreen()
    {        
        this.activeScreen = Instantiate(loseScreen);
    }
}