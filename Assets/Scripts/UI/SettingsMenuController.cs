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

    // ── Internal ──────────────────────────────────────────
    private VisualElement root;

    // Tabs
    private Button tabAudio;
    private Button tabVideo;
    private Button tabControls;

    // Pages
    private VisualElement pageAudio;
    private VisualElement pageVideo;
    private VisualElement pageControls;

    // Audio controls
    private Slider sliderMaster;
    private Slider sliderMusic;
    private Slider sliderSFX;
    private Slider sliderAmbient;

    private Label labelMaster;
    private Label labelMusic;
    private Label labelSFX;
    private Label labelAmbient;

    // Bottom bar
    private Button buttonApply;
    private Button buttonBack;

    // Pending values (committed on Apply)
    private float pendingMaster  = 1f;
    private float pendingMusic   = 0.75f;
    private float pendingSFX     = 1f;
    private float pendingAmbient = 0.5f;

    private const string ACTIVE_TAB_CLASS = "settings-tab--active";
    private List<(Button tab, VisualElement page)> tabPagePairs;

    public System.Action OnBackPressed;

    // ── Lifecycle ─────────────────────────────────────────

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

    // ── Setup ─────────────────────────────────────────────

    public void Initialize(VisualElement settingsRoot)
    {
        root = settingsRoot;

        tabAudio    = root.Q<Button>("TabAudio");
        tabVideo    = root.Q<Button>("TabVideo");
        tabControls = root.Q<Button>("TabControls");

        pageAudio    = root.Q<VisualElement>("PageAudio");
        pageVideo    = root.Q<VisualElement>("PageVideo");
        pageControls = root.Q<VisualElement>("PageControls");

        tabPagePairs = new List<(Button, VisualElement)>
        {
            (tabAudio,    pageAudio),
            (tabVideo,    pageVideo),
            (tabControls, pageControls),
        };

        tabAudio   .RegisterCallback<ClickEvent>(_ => SwitchToTab(tabAudio));
        tabVideo   .RegisterCallback<ClickEvent>(_ => SwitchToTab(tabVideo));
        tabControls.RegisterCallback<ClickEvent>(_ => SwitchToTab(tabControls));

        sliderMaster  = root.Q<Slider>("SliderMasterVolume");
        sliderMusic   = root.Q<Slider>("SliderMusicVolume");
        sliderSFX     = root.Q<Slider>("SliderSFXVolume");
        sliderAmbient = root.Q<Slider>("SliderAmbientVolume");

        labelMaster  = root.Q<Label>("LabelMasterVolume");
        labelMusic   = root.Q<Label>("LabelMusicVolume");
        labelSFX     = root.Q<Label>("LabelSFXVolume");
        labelAmbient = root.Q<Label>("LabelAmbientVolume");

        RegisterSliderLabel(sliderMaster,  labelMaster,  v => pendingMaster  = v);
        RegisterSliderLabel(sliderMusic,   labelMusic,   v => pendingMusic   = v);
        RegisterSliderLabel(sliderSFX,     labelSFX,     v => pendingSFX     = v);
        RegisterSliderLabel(sliderAmbient, labelAmbient, v => pendingAmbient = v);

        buttonApply = root.Q<Button>("ButtonApply");
        buttonBack  = root.Q<Button>("ButtonBack");

        buttonApply?.RegisterCallback<ClickEvent>(_ => ApplySettings());
        buttonBack ?.RegisterCallback<ClickEvent>(_ => OnBackPressed?.Invoke());

        LoadSettings();
        SwitchToTab(tabAudio);
    }

    // ── Tab Switching ─────────────────────────────────────

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

    private void ApplySettings()
    {
        PlayerPrefs.SetFloat("Vol_Master",  pendingMaster);
        PlayerPrefs.SetFloat("Vol_Music",   pendingMusic);
        PlayerPrefs.SetFloat("Vol_SFX",     pendingSFX);
        PlayerPrefs.SetFloat("Vol_Ambient", pendingAmbient);
        PlayerPrefs.Save();

        AkSoundEngine.SetRTPCValue(masterVolumeRTPC,  pendingMaster  * 100f);
        AkSoundEngine.SetRTPCValue(musicVolumeRTPC,   pendingMusic   * 100f);
        AkSoundEngine.SetRTPCValue(sfxVolumeRTPC,     pendingSFX     * 100f);
        AkSoundEngine.SetRTPCValue(ambientVolumeRTPC, pendingAmbient * 100f);
    }

    // ── Helpers ───────────────────────────────────────────

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

    private void SetSliderSilently(Slider slider, Label label, float value)
    {
        if (slider == null) return;
        slider.SetValueWithoutNotify(value);
        if (label != null) label.text = Mathf.RoundToInt(value * 100).ToString();
    }
}
