using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class SettingsMenuController : MonoBehaviour
{
    [Header("Wwise RTPC Names")]
    [SerializeField] private string masterVolumeRTPC  = "MasterVolume";
    [SerializeField] private string musicVolumeRTPC   = "MusicVolume";
    [SerializeField] private string sfxVolumeRTPC     = "SFXVolume";
    [SerializeField] private string ambientVolumeRTPC = "AmbientVolume";

    private VisualElement root;

    // Tab buttons in the top navigation bar
    private Button tabAudio;
    private Button tabVideo;
    private Button tabControls;

    // Content pages shown/hidden when switching tabs
    private VisualElement pageAudio;
    private VisualElement pageVideo;
    private VisualElement pageControls;

    // Audio volume sliders (0–1 range)
    private Slider sliderMaster;
    private Slider sliderMusic;
    private Slider sliderSFX;
    private Slider sliderAmbient;

    // Numeric labels that display each slider's current value as a percentage
    private Label labelMaster;
    private Label labelMusic;
    private Label labelSFX;
    private Label labelAmbient;

    // Bottom bar
    private Button buttonApply;
    private Button buttonBack;

    // Staged volume values — written to PlayerPrefs/Wwise only when Apply is pressed
    private float pendingMaster  = 1f;
    private float pendingMusic   = 0.75f;
    private float pendingSFX     = 1f;
    private float pendingAmbient = 0.5f;

    // USS class applied to whichever tab button is currently selected
    private const string ACTIVE_TAB_CLASS = "settings-tab--active";
    
    // Pairs each tab button with its corresponding page for easy iteration
    private List<(Button tab, VisualElement page)> tabPagePairs;

    public System.Action OnBackPressed;


    private void OnEnable()
    {
        var document = GetComponent<UIDocument>();
        if (document == null)
        {
            Debug.LogError("SettingsMenuController: No UIDocument found on this GameObject!");
            return;
        }

        Initialize(document.rootVisualElement);
    }


    /// <summary>
    /// Queries all UI elements from the provided root, registers callbacks,
    /// loads saved settings, and defaults to the Audio tab.
    /// </summary>
    public void Initialize(VisualElement settingsRoot)
    {
        root = settingsRoot;

        // Query tab buttons
        tabAudio    = root.Q<Button>("TabAudio");
        tabVideo    = root.Q<Button>("TabVideo");
        tabControls = root.Q<Button>("TabControls");

        // Query page containers
        pageAudio    = root.Q<VisualElement>("PageAudio");
        pageVideo    = root.Q<VisualElement>("PageVideo");
        pageControls = root.Q<VisualElement>("PageControls");

        // Build pairs list so SwitchToTab can iterate without a chain of if/else
        tabPagePairs = new List<(Button, VisualElement)>
        {
            (tabAudio,    pageAudio),
            (tabVideo,    pageVideo),
            (tabControls, pageControls),
        };

        // Wire tab click events
        tabAudio   .RegisterCallback<ClickEvent>(_ => SwitchToTab(tabAudio));
        tabVideo   .RegisterCallback<ClickEvent>(_ => SwitchToTab(tabVideo));
        tabControls.RegisterCallback<ClickEvent>(_ => SwitchToTab(tabControls));

        // Query audio sliders and their paired display labels
        sliderMaster  = root.Q<Slider>("SliderMasterVolume");
        sliderMusic   = root.Q<Slider>("SliderMusicVolume");
        sliderSFX     = root.Q<Slider>("SliderSFXVolume");
        sliderAmbient = root.Q<Slider>("SliderAmbientVolume");

        labelMaster  = root.Q<Label>("LabelMasterVolume");
        labelMusic   = root.Q<Label>("LabelMusicVolume");
        labelSFX     = root.Q<Label>("LabelSFXVolume");
        labelAmbient = root.Q<Label>("LabelAmbientVolume");

        // Register each slider to update its label and stage the pending value
        RegisterSliderLabel(sliderMaster,  labelMaster,  v => pendingMaster  = v);
        RegisterSliderLabel(sliderMusic,   labelMusic,   v => pendingMusic   = v);
        RegisterSliderLabel(sliderSFX,     labelSFX,     v => pendingSFX     = v);
        RegisterSliderLabel(sliderAmbient, labelAmbient, v => pendingAmbient = v);

        // Wire bottom bar buttons
        buttonApply = root.Q<Button>("ButtonApply");
        buttonBack  = root.Q<Button>("ButtonBack");

        buttonApply?.RegisterCallback<ClickEvent>(_ => ApplySettings());
        buttonBack ?.RegisterCallback<ClickEvent>(_ => OnBackPressed?.Invoke());

        LoadSettings();
        SwitchToTab(tabAudio);
    }

    // ── Tab Switching ─────────────────────────────────────

    /// <summary>
    /// Activates the target tab's page and marks it with the active CSS class;
    /// all other tabs and pages are hidden/deactivated.
    /// </summary>
    private void SwitchToTab(Button targetTab)
    {
        foreach (var (tab, page) in tabPagePairs)
        {
            bool isTarget = tab == targetTab;
            if (isTarget) tab.AddToClassList(ACTIVE_TAB_CLASS);
            else          tab.RemoveFromClassList(ACTIVE_TAB_CLASS);
            page.style.display = isTarget ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    // ── Settings Persistence ──────────────────────────────

    /// <summary>
    /// Loads saved volume values from PlayerPrefs and silently updates
    /// the sliders and labels without triggering change callbacks.
    /// </summary>
    private void LoadSettings()
    {
        pendingMaster  = PlayerPrefs.GetFloat("Vol_Master",  1f);
        pendingMusic   = PlayerPrefs.GetFloat("Vol_Music",   0.75f);
        pendingSFX     = PlayerPrefs.GetFloat("Vol_SFX",     1f);
        pendingAmbient = PlayerPrefs.GetFloat("Vol_Ambient", 0.5f);

        SetSliderSilently(sliderMaster,  labelMaster,  pendingMaster);
        SetSliderSilently(sliderMusic,   labelMusic,   pendingMusic);
        SetSliderSilently(sliderSFX,     labelSFX,     pendingSFX);
        SetSliderSilently(sliderAmbient, labelAmbient, pendingAmbient);
    }


    /// <summary>
    /// Commits all pending volume values to PlayerPrefs and pushes them
    /// to Wwise as 0–100 RTPC values.
    /// </summary>
    private void ApplySettings()
    {
        PlayerPrefs.SetFloat("Vol_Master",  pendingMaster);
        PlayerPrefs.SetFloat("Vol_Music",   pendingMusic);
        PlayerPrefs.SetFloat("Vol_SFX",     pendingSFX);
        PlayerPrefs.SetFloat("Vol_Ambient", pendingAmbient);
        PlayerPrefs.Save();

        // Wwise RTPCs expect a 0–100 scale; sliders store normalised 0–1 values
        AkSoundEngine.SetRTPCValue(masterVolumeRTPC,  pendingMaster  * 100f);
        AkSoundEngine.SetRTPCValue(musicVolumeRTPC,   pendingMusic   * 100f);
        AkSoundEngine.SetRTPCValue(sfxVolumeRTPC,     pendingSFX     * 100f);
        AkSoundEngine.SetRTPCValue(ambientVolumeRTPC, pendingAmbient * 100f);
    }

    // ── Helpers ───────────────────────────────────────────

    /// <summary>
    /// Subscribes to a slider's value-changed event to keep its label in sync
    /// and forward the new value to the provided staging callback.
    /// </summary>
    private void RegisterSliderLabel(Slider slider, Label label, System.Action<float> onChanged)
    {
        if (slider == null) return;
        slider.RegisterValueChangedCallback(evt =>
        {
            float v = evt.newValue;
            onChanged(v);
            if (label != null) label.text = Mathf.RoundToInt(v * 100).ToString();
        });
    }

    /// <summary>
    /// Sets a slider's value and updates its label without firing value-changed callbacks,
    /// used during initial load to avoid dirtying pending state.
    /// </summary>
    private void SetSliderSilently(Slider slider, Label label, float value)
    {
        if (slider == null) return;
        slider.SetValueWithoutNotify(value);
        if (label != null) label.text = Mathf.RoundToInt(value * 100).ToString();
    }
}
