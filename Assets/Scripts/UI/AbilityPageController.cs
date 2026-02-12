using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;

public class AbilityPageController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private AbilityData abilityQ;
    [SerializeField] private AbilityData abilityW;
    [SerializeField] private AbilityData abilityE;
    [SerializeField] private AbilityData abilityR;

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RenderTexture videoTexture;

    private Label titleLabel;
    private Label descLabel;
    private Image videoDisplay;

    // We no longer search the whole root; we receive our specific page
    public void Initialize(VisualElement pageRoot)
    {
        titleLabel = pageRoot.Q<Label>("AbilityTitle");
        descLabel = pageRoot.Q<Label>("AbilityDescription");
        videoDisplay = pageRoot.Q<Image>("AbilityVideoPlayer");

        // Link the texture
        if(videoDisplay != null) videoDisplay.image = videoTexture;

        SetupButton(pageRoot.Q<Button>("BtnQ"), abilityQ);
        SetupButton(pageRoot.Q<Button>("BtnW"), abilityW);
        SetupButton(pageRoot.Q<Button>("BtnE"), abilityE);
        SetupButton(pageRoot.Q<Button>("BtnR"), abilityR);
    }

    // Called by the Main Menu when the user clicks the "Abilities" tab
    public void OnShow()
    {
        // Reset to Q every time we open the tab? Or keep last state?
        // Let's reset to Q to be safe and ensure video starts.
        DisplayAbility(abilityQ);
    }

    private void SetupButton(Button btn, AbilityData data)
    {
        if (btn == null || data == null) return;
        btn.style.backgroundImage = new StyleBackground(data.icon);
        btn.RegisterCallback<ClickEvent>(evt => DisplayAbility(data));
    }

    private void DisplayAbility(AbilityData data)
    {
        if (data == null) return;

        titleLabel.text = data.abilityName;
        descLabel.text = data.description;

        // Video Logic (With the 'Prepare' fix included)
        videoDisplay.style.visibility = Visibility.Hidden;
        videoPlayer.Stop();
        videoPlayer.clip = data.previewClip;
        videoPlayer.targetTexture = videoTexture;

        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        source.prepareCompleted -= OnVideoPrepared;
        source.Play();
        videoDisplay.style.visibility = Visibility.Visible;
    }
}