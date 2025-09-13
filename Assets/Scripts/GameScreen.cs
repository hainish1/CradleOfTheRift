using UnityEngine;
using UnityEngine.UIElements;

public class GameScreen : MonoBehaviour
{

    private VisualElement winScreen;
    private VisualElement loseScreen;


    void Awake()
    {
        string winScreenName = "WinScreen";
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        this.winScreen = root.Q<VisualElement>(className: winScreenName);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
