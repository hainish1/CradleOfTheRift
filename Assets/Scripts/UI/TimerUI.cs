using System;
using UnityEngine;
using UnityEngine.UIElements;
public class TimerUI : MonoBehaviour
{
    private float elapsedTime = 0f;

    // Used for pausing game
    private bool isRunning = true;
    private Label timerLabel;

    private bool hasDisplayedExtraction = false;
    private bool hasTimeEnded = false;

    private float timeToDisplayExtraction = 180f;
    private float timeEnd = 320f;

    public event Action DisplayExtraction;
    public event Action DisplayEndGame;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        string labelName = "TimerLabel";
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        this.timerLabel = root.Q<Label>(name: labelName);
    }

    void Update()
    {
        if (!isRunning) return;

        elapsedTime += Time.deltaTime;
        UpdateTimerUI();

        if (!this.hasDisplayedExtraction && this.elapsedTime >= this.timeToDisplayExtraction)
        {
            this.hasDisplayedExtraction = true;
            this.DisplayExtraction?.Invoke();
            Debug.Log("Timer hit 3 minutes");
        }
    }
    private void UpdateTimerUI()
    {
        // Format minutes and seconds
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);

        timerLabel.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
