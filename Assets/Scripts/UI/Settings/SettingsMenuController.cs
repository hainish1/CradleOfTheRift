using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SettingsMenuController : MonoBehaviour
{
    [Header("Sound Effects")]
    [SerializeField] private AK.Wwise.Event clickSFX;

    // ── Tab animation config ──────────────────────────────────────────
    private const float BORDER_MAX = 4f; // px when active
    private const float BORDER_MIN = 0f; // px when inactive
    private const float ANIM_SPEED = 30f;

    private SettingsService service;
    private AudioPageController audioCtrl;
    private VideoPageController videoCtrl;
    private ControlsPageController controlsCtrl;

    private VisualElement root;

    // Tab buttons in the top navigation bar
    private Button tabAudio, tabVideo, tabControls;

    // Content pages shown/hidden when switching tabs
    private VisualElement pageAudio, pageVideo, pageControls;

    // Bottom bar
    private Button buttonRevert, buttonResetTutorial, buttonBack;

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
            Debug.LogError("SettingsMenuController: No UIDocument found!");
            return;
        }

        Initialize(document.rootVisualElement);
    }

    private void OnDisable()
    {
        service?.Save();
    }

    private void Update()
    {
        if (animTabs == null) return;

        for (int i = 0; i < animTabs.Length; i++)
        {
            float target  = (animTabs[i] == activeTab) ? BORDER_MAX : BORDER_MIN;
            float current = borderWidths[i];
            if (Mathf.Approximately(current, target)) continue;

            float next = Mathf.MoveTowards(current, target, ANIM_SPEED * Time.unscaledDeltaTime);
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

        // Create the service
        service = GlobalSettingsManager.Instance.Service;

        // Bind the click sfx to the root.
        // Hopefully through event propogation,
        // this will make the click sfx play for every click!
        root.RegisterCallback<ClickEvent>(_ => AUDIO_GlobalAudioPlayer.Instance.PlaySound(clickSFX));

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

        // Initialise page controllers
        audioCtrl    = new AudioPageController(service);
        videoCtrl    = new VideoPageController(service);
        controlsCtrl = new ControlsPageController(service);

        // Initialize respective pages, passing each its own page root
        audioCtrl   .Initialize(pageAudio);
        videoCtrl   .Initialize(pageVideo);
        controlsCtrl.Initialize(pageControls);

        // Wire bottom bar button
        buttonRevert        = root.Q<Button>("ButtonRevert");
        buttonResetTutorial = root.Q<Button>("ButtonResetTutorial");
        buttonBack          = root.Q<Button>("ButtonBack");

        buttonRevert?.RegisterCallback<ClickEvent>(_ =>
        {
            service.RevertToSnapshot();
            RefreshAllPages();
        });
        buttonResetTutorial?.RegisterCallback<ClickEvent>(_ =>
        {
            GameSaveState.ResetAll();
            buttonResetTutorial.text = "Tutorial Reset!";
            buttonResetTutorial.SetEnabled(false);
        });
        buttonBack?.RegisterCallback<ClickEvent>(_ => AUDIO_GlobalAudioPlayer.Instance.PlaySound(clickSFX));
        buttonBack?.RegisterCallback<ClickEvent>(_ => OnBackPressed?.Invoke());
        
        RefreshAllPages();

        SwitchToTab(tabAudio, snap: true);
    }

    private void RefreshAllPages()
    {
        audioCtrl   .Refresh(service.Current);
        videoCtrl   .Refresh(service.Current);
        controlsCtrl.Refresh(service.Current);
    }

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
}