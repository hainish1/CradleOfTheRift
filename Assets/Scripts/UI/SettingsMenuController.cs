using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;

public class SettingsMenuController : MonoBehaviour
{
    [Header("Wwise RTPC Names")]
    [SerializeField] private string masterVolumeRTPC  = "MasterVolume";
    [SerializeField] private string musicVolumeRTPC   = "MusicVolume";
    [SerializeField] private string sfxVolumeRTPC     = "SFXVolume";
    [SerializeField] private string ambientVolumeRTPC = "AmbientVolume";

    // ── Tab animation config ──────────────────────────────────────────
    private const float BORDER_MAX  = 4f;   // px when active
    private const float BORDER_MIN  = 0f;   // px when inactive
    private const float ANIM_SPEED  = 30f;  // higher = faster (units per second)
    // ─────────────────────────────────────────────────────────────────

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

    // Staging variables to hold pending slider values until "Apply" is pressed
    private SettingsData pending = new SettingsData();
    private static string SettingsPath => Path.Combine(Application.persistentDataPath, "settings.json");

    // USS class applied to whichever tab button is currently selected
    private const string ACTIVE_TAB_CLASS = "settings-tab--active";
    
    // Pairs each tab button with its corresponding page for easy iteration
    private List<(Button tab, VisualElement page)> tabPagePairs;

    // Per-frame border animation
    private Button[] animTabs;
    private float[]  borderWidths;
    private Button   activeTab;

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

    private void Update()
    {
        if (animTabs == null) return;

        for (int i = 0; i < animTabs.Length; i++)
        {
            float target  = (animTabs[i] == activeTab) ? BORDER_MAX : BORDER_MIN;
            float current = borderWidths[i];

            if (Mathf.Approximately(current, target)) continue;

            float next = Mathf.MoveTowards(current, target, ANIM_SPEED * Time.deltaTime);
            borderWidths[i] = next;
            animTabs[i].style.borderBottomWidth = next;
        }
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

        // Animation arrays and order must match tabPagePairs
        animTabs     = new[] { tabAudio, tabVideo, tabControls };
        borderWidths = new float[3];

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
        RegisterSliderLabel(sliderMaster,  labelMaster,  v => pending.masterVolume  = v);
        RegisterSliderLabel(sliderMusic,   labelMusic,   v => pending.musicVolume   = v);
        RegisterSliderLabel(sliderSFX,     labelSFX,     v => pending.sfxVolume     = v);
        RegisterSliderLabel(sliderAmbient, labelAmbient, v => pending.ambientVolume = v);

        // Wire bottom bar buttons
        buttonApply = root.Q<Button>("ButtonApply");
        buttonBack  = root.Q<Button>("ButtonBack");

        buttonApply?.RegisterCallback<ClickEvent>(_ => ApplySettings());
        buttonBack ?.RegisterCallback<ClickEvent>(_ => OnBackPressed?.Invoke());

        LoadSettings();
        SwitchToTab(tabAudio, snap: true);
    }

    // ── Tab Switching ─────────────────────────────────────

    /// <summary>
    /// Activates the target tab's page and marks it with the active CSS class;
    /// all other tabs and pages are hidden/deactivated.
    /// </summary>
    private void SwitchToTab(Button targetTab, bool snap = false)
    {
        activeTab = targetTab;

        for (int i = 0; i < animTabs.Length; i++)
        {
            bool isTarget = animTabs[i] == targetTab;

            if (isTarget) animTabs[i].AddToClassList(ACTIVE_TAB_CLASS);
            else          animTabs[i].RemoveFromClassList(ACTIVE_TAB_CLASS);

            if (snap)
            {
                float snapValue = isTarget ? BORDER_MAX : BORDER_MIN;
                borderWidths[i] = snapValue;
                animTabs[i].style.borderBottomWidth = snapValue;
            }
        }

        foreach (var (tab, page) in tabPagePairs)
            page.style.display = (tab == targetTab) ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // ── Settings Persistence ──────────────────────────────

    /// <summary>
    /// Loads saved volume values from PlayerPrefs and silently updates
    /// the sliders and labels without triggering change callbacks.
    /// </summary>
    private void LoadSettings()
    {
        if(File.Exists(SettingsPath))
        {
            string json = File.ReadAllText(SettingsPath);
            pending = JsonUtility.FromJson<SettingsData>(json);
        }
        else
        {
            Debug.LogWarning("SettingsMenuController: No saved settings found, using defaults.");
            pending = new SettingsData(); // Defaults defined in SettingsData class
            SaveToDisk();
        }

        // (convert int back to float)
        SetSliderSilently(sliderMaster,  labelMaster,  pending.masterVolume);
        SetSliderSilently(sliderMusic,   labelMusic,   pending.musicVolume);
        SetSliderSilently(sliderSFX,     labelSFX,     pending.sfxVolume);
        SetSliderSilently(sliderAmbient, labelAmbient, pending.ambientVolume);
    }


    /// <summary>
    /// Commits all pending volume values to PlayerPrefs and pushes them
    /// to Wwise as 0–100 RTPC values.
    /// </summary>
    private void ApplySettings()
    {
        SaveToDisk();

        // Wwise RTPCs expect a 0–100 scale
        AkSoundEngine.SetRTPCValue(masterVolumeRTPC,  pending.masterVolume);
        AkSoundEngine.SetRTPCValue(musicVolumeRTPC,   pending.musicVolume);
        AkSoundEngine.SetRTPCValue(sfxVolumeRTPC,     pending.sfxVolume);
        AkSoundEngine.SetRTPCValue(ambientVolumeRTPC, pending.ambientVolume);

        Debug.Log($"Settings saved to: {SettingsPath}\n{File.ReadAllText(SettingsPath)}");
        Debug.Log("SettingsMenuController: Applied audio settings to PlayerPrefs and Wwise.\n" +
                  $"Master={pending.masterVolume}, Music={pending.musicVolume}, SFX={pending.sfxVolume}, Ambient={pending.ambientVolume}");
    }

    // ── Helpers ───────────────────────────────────────────

    /// <summary>
    /// Serializes the pending settings to JSON and writes them to disk at a known path.
    /// </summary>
    private void SaveToDisk()
    {
        string json = JsonUtility.ToJson(pending, prettyPrint: true);
        File.WriteAllText(SettingsPath, json);
    }

    /// <summary>
    /// Subscribes to a slider's value-changed event to keep its label in sync
    /// and forward the new value to the provided staging callback.
    /// </summary>
    private void RegisterSliderLabel(Slider slider, Label label, System.Action<int> onChanged)
    {
        if (slider == null) return;
        slider.RegisterValueChangedCallback(evt =>
        {
            int v = (int)evt.newValue; // Snap to nearest step
            slider.SetValueWithoutNotify(v); // Update slider to snapped value 

            onChanged(v);
            if (label != null) label.text = v.ToString();
        });
    }

    /// <summary>
    /// Sets a slider's value and updates its label without firing value-changed callbacks,
    /// used during initial load to avoid dirtying pending state.
    /// </summary>
    private void SetSliderSilently(Slider slider, Label label, int value)
    {
        if (slider == null) return;
        slider.SetValueWithoutNotify(value);
        if (label != null) label.text = value.ToString();
    }
}
