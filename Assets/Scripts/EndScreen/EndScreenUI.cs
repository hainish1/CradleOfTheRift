using UnityEngine;
using UnityEngine.UIElements;

public class EndScreenUI : MonoBehaviour
{
    [SerializeField]
    private ExtractionZone extractionZone;
    [SerializeField]
    private GameObject winScreen;
    [SerializeField]
    private GameObject loseScreen;

    private GameObject activeScreen;

    void OnEnable()
    {
        this.extractionZone.WinScreen += OnWinScreen;
    }

    void OnDisable()
    {
        this.extractionZone.WinScreen -= OnWinScreen;
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