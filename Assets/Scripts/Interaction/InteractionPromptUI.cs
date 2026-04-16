using UnityEngine;
using TMPro;

public class InteractionPromptUI : MonoBehaviour
{
    private Camera mainCam;
    [SerializeField] private GameObject UIPanel;
    [SerializeField] private TextMeshProUGUI promptText;
    [Tooltip("How far above the prompt floats")]
    [SerializeField] private float worldHeightOffset = 1.75f;
    public bool isDisplayed = false;

    private Transform anchor;
    private float currentHeightOffset;

    private void Start()
    {
        mainCam = Camera.main;
        UIPanel.SetActive(false);
    }

    private void LateUpdate()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        if (anchor != null)
        {
            transform.position = anchor.position + Vector3.up * currentHeightOffset;
        }

        transform.LookAt(transform.position + mainCam.transform.rotation * Vector3.forward,
            mainCam.transform.rotation * Vector3.up);
    }

    public void ShowPrompt(string prompt, Transform anchor = null, float? heightOffsetOverride = null)
    {
        this.anchor = anchor;
        this.currentHeightOffset = heightOffsetOverride ?? worldHeightOffset;
        promptText.text = prompt;
        UIPanel.SetActive(true);
        isDisplayed = true;
    }

    public void HidePrompt()
    {
        anchor = null;
        UIPanel.SetActive(false);
        isDisplayed = false;
    }
}
