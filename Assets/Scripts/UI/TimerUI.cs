using System;
using UnityEngine;
using UnityEngine.UIElements;

public class TimerUI : MonoBehaviour
{
    private float elapsedTime = 0f;
    private bool isRunning = true;
    private Label timerLabel;
    private float timeToDisplayExtraction = 180f;
    public static event Action DisplayExtractionBeam;
    private bool hasDisplayedExtraction = false;

    void Start()
    {
        // Initialize the UI Label
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        this.timerLabel = root.Q<Label>(name: "TimerLabel");
    }

    void Update()
    {
        if (!isRunning) return;

        this.elapsedTime += Time.deltaTime;

        if (!this.hasDisplayedExtraction && elapsedTime >= this.timeToDisplayExtraction)
        {
            this.hasDisplayedExtraction = true;
            DisplayExtractionBeam?.Invoke();
        }

        UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        // Format minutes and seconds for display
        int minutes = Mathf.FloorToInt(this.elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(this.elapsedTime % 60f);

        timerLabel.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    public void SetRunning(bool run) => isRunning = run;
    public void ResetTimer() => elapsedTime = 0f;
}