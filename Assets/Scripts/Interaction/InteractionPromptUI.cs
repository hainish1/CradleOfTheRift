using UnityEngine;
using TMPro;

public class InteractionPromptUI : MonoBehaviour
{
    private Camera mainCam;
    [SerializeField] private GameObject UIPanel;
    [SerializeField] private TextMeshProUGUI promptText;
    public bool isDisplayed = false;

    private void Start()
    {
        mainCam = Camera.main;
        UIPanel.SetActive(false);
    }

    private void LateUpdate()
    {
        transform.LookAt(transform.position + mainCam.transform.rotation * Vector3.forward,
            mainCam.transform.rotation * Vector3.up);
    }

    public void ShowPrompt(string prompt)
    {
        promptText.text = prompt;
        UIPanel.SetActive(true);
        isDisplayed = true;
    }

    public void HidePrompt()
    {
        UIPanel.SetActive(false);
        isDisplayed = false;
    }
}
