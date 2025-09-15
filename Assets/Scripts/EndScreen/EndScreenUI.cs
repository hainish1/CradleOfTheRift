using UnityEngine;
using UnityEngine.UIElements;

public class EndScreenUI : MonoBehaviour
{
    [SerializeField]
    private ExtractionZone extractionZone;
    private VisualElement winScreen;
    private VisualElement loseScreen;
    private VisualElement hud;

    void Awake()
    {
        string hudName = "HUD";
        string winScreenName = "WinScreen";
        string loseScreen = "LoseScreen";

        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        this.hud = root.Q<VisualElement>(name: hudName);
        this.winScreen = root.Q<VisualElement>(name: winScreenName);
        this.loseScreen = root.Q<VisualElement>(name: loseScreen);

        this.winScreen.style.display = DisplayStyle.None;
        this.loseScreen.style.display = DisplayStyle.None;
    }

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
        this.hud.style.display = DisplayStyle.None;
        this.winScreen.style.display = DisplayStyle.Flex;
    }

    private void OnLoseScreen()
    {        
        this.hud.style.display = DisplayStyle.None;
        this.loseScreen.style.display = DisplayStyle.Flex;
    }
}