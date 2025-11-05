using System;
using UnityEngine;
using UnityEngine.UIElements;
public class TimerUI : MonoBehaviour
{
    private float elapsedTime = 0f;
    private float remainingTime = 300f;
    private float timeToDisplayExtraction = 120f;

    // Used for pausing game
    private bool isRunning = true;
    private Label timerLabel;
    private bool hasDisplayedExtraction = false;
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

        this.remainingTime -= Time.deltaTime;

        if (this.remainingTime < 0f)
        {
            this.remainingTime = 0f;
        }
        
        UpdateTimerUI();

        if (!this.hasDisplayedExtraction && this.remainingTime <= this.timeToDisplayExtraction)
        {
            this.hasDisplayedExtraction = true;
            this.DisplayExtraction?.Invoke();
            Debug.Log("Timer hit 2 minutes");
            timerLabel.style.color = Color.red;
        }

        if (this.remainingTime <= 0f)
        {
            this.isRunning = false;
            this.DisplayEndGame?.Invoke();
            Debug.Log("Timer hit 0 minutes");
        }
    }
    private void UpdateTimerUI()
    {
        // Format minutes and seconds
        int minutes = Mathf.FloorToInt(this.remainingTime / 60f);
        int seconds = Mathf.FloorToInt(this.remainingTime % 60f);

        timerLabel.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        UpdateColor();
    }

    private void UpdateColor()
    {
        float colorEndTime = 0f;  

        Color customRed;
        ColorUtility.TryParseHtmlString("#660000", out customRed);

        if (this.remainingTime <= this.timeToDisplayExtraction)
        {
            float t = Mathf.Clamp01(1f - ((this.remainingTime - colorEndTime) / (this.timeToDisplayExtraction - colorEndTime)));
            timerLabel.style.color = Color.Lerp(Color.white, customRed, t);
        }
        else
        {
            timerLabel.style.color = Color.white;
        }
    }
}
