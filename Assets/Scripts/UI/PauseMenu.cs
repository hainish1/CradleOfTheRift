using UnityEngine;
using UnityEngine.UIElements;
public class PauseMenu : MonoBehaviour
{
    private UIDocument document;
    private Button button;

    private void Awake()
    {
        document = GetComponent<UIDocument>();
        button = document.rootVisualElement.Q("ButtonStartGame") as Button;
        button.RegisterCallback<ClickEvent>(OnStartGameClick);
    }

    private void OnDisable()
    {
        button.UnregisterCallback<ClickEvent>(OnStartGameClick);
    }

    private void OnStartGameClick(ClickEvent evt)
    {
        Debug.Log("You Pressed the Start Button");
    }
}
