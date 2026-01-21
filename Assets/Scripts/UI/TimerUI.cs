using UnityEngine;
using UnityEngine.UIElements;

public class TimerUI : MonoBehaviour
{
    private float elapsedTime = 0f;
    private bool isRunning = true;
    private Label timerLabel;

    void Start()
    {
        // Initialize the UI Label
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        this.timerLabel = root.Q<Label>(name: "TimerLabel");
    }

    void Update()
    {
        if (!isRunning) return;

        // Increase time instead of decreasing it
        this.elapsedTime += Time.deltaTime;

        UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        // Format minutes and seconds for display
        int minutes = Mathf.FloorToInt(this.elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(this.elapsedTime % 60f);

        timerLabel.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // Optional: Public methods to control the timer from other scripts
    public void SetRunning(bool run) => isRunning = run;
    public void ResetTimer() => elapsedTime = 0f;
}