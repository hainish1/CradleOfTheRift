using UnityEngine;
using UnityEngine.UIElements;

public class DifficultyUI : MonoBehaviour
{
    [SerializeField] private DifficultyScaler difficultyScaler;
    
    private VisualElement difficultyBarFill;
    private Label difficultyLabel;

    void Start()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        
        // Query your elements
        difficultyBarFill = root.Q<VisualElement>("DifficultyBarFill");
        difficultyLabel = root.Q<Label>("DifficultyLabel");

        // Subscribe to the scaler
        if (difficultyScaler != null)
        {
            difficultyScaler.OnDifficultyUIUpdate += UpdateUI;
        }
    }

    private void UpdateUI(float fillPercentage, string difficultyText)
    {
        // Update the width of the bar (assumes the parent is the bounds)
        difficultyBarFill.style.width = new Length(fillPercentage * 100f, LengthUnit.Percent);
        
        // Update the text
        difficultyLabel.text = difficultyText;
    }

    private void OnDestroy()
    {
        if (difficultyScaler != null)
        {
            difficultyScaler.OnDifficultyUIUpdate -= UpdateUI;
        }
    }
}