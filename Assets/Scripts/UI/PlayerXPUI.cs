using UnityEngine;
using UnityEngine.UIElements;

public class PlayerXPUI : MonoBehaviour
{
    private Label levelLabel;
    private Label levelUpHint;
    private Label centerLevelUpHint;
    private Label xpLabel;
    private ProgressBar xpBar;
    private bool _isPulsing = false;
    private IVisualElementScheduledItem _pulseSchedule;

    void Start()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        levelLabel  = root.Q<Label>("LevelLabel");
        levelUpHint = root.Q<Label>("LevelUpHint");
        centerLevelUpHint = root.Q<Label>("CenterLevelUpHint");
        xpLabel     = root.Q<Label>("XPLabel");
        xpBar       = root.Q<ProgressBar>("XPBar");

        PlayerXP xp = PlayerXP.Instance;
        if (xp == null)
        {
            Debug.LogWarning("PlayerXP.instance not found.");
            return;
        }

        xp.XPChanged        += OnXPChanged;
        xp.LeveledUp        += OnLeveledUp;
        xp.LevelUpAvailable += OnLevelUpAvailable;

        Refresh(xp.CurrentXP, xp.XPToLevelUp, xp.CurrentLevel);
    }

    void OnDestroy()
    {
        if (PlayerXP.Instance != null)
        {
            PlayerXP.Instance.XPChanged        -= OnXPChanged;
            PlayerXP.Instance.LeveledUp        -= OnLeveledUp;
            PlayerXP.Instance.LevelUpAvailable -= OnLevelUpAvailable;
        }
    }

    private void OnXPChanged(int currentXP, int xpToLevelUp)
    {
        Refresh(currentXP, xpToLevelUp, PlayerXP.Instance.CurrentLevel);
    }

    private void OnLevelUpAvailable()
    {
        var xp = PlayerXP.Instance;
        Refresh(xp.CurrentXP, xp.XPToLevelUp, xp.CurrentLevel);
    }

    private void OnLeveledUp(int newLevel)
    {
        Refresh(PlayerXP.Instance.CurrentXP, PlayerXP.Instance.XPToLevelUp, newLevel);
    }

    private void Refresh(int currentXP, int xpToLevelUp, int level)
    {
        if (levelLabel != null)
            levelLabel.text = $"Lv {level}";

        if (xpBar != null)
        {
            xpBar.highValue = xpToLevelUp;
            xpBar.value     = currentXP;
            xpBar.title     = $"{currentXP} / {xpToLevelUp} XP";
        }

        bool showHint = PlayerXP.Instance != null && PlayerXP.Instance.IsLevelUpReady;
        UpdateHintVisibility(showHint);
    }

    private void UpdateHintVisibility(bool show)
    {
        if (levelUpHint == null) return;

        if (show)
        {
            bool needsShow = (levelUpHint != null && levelUpHint.style.display != DisplayStyle.Flex) ||
                             (centerLevelUpHint != null && centerLevelUpHint.style.display != DisplayStyle.Flex);

            if (needsShow)
            {

                // Make the element part of layout
                if (levelUpHint != null)
                    levelUpHint.style.display = DisplayStyle.Flex;

                if (centerLevelUpHint != null)
                    centerLevelUpHint.style.display = DisplayStyle.Flex;

                // Grab whichever element isn't null to schedule the event
                var schedulerElement = levelUpHint ?? centerLevelUpHint;


                // Trigger fade-in on the next frame so the transition fires
                // (USS transitions don't fire if class is added the same frame
                // the element becomes visible)
                schedulerElement.schedule.Execute(() =>
                {
                    levelUpHint?.AddToClassList("hint-visible");
                    centerLevelUpHint?.AddToClassList("hint-visible");
                    StartPulse();
                }).StartingIn(20);
            }
        }
        else
        {
            // Only hide if we aren't immediately showing it again for a multi-level up
            if (PlayerXP.Instance != null && PlayerXP.Instance.IsLevelUpReady) 
                return;
            
            StopPulse();
            // Fade out, then hide from layout once the transition completes
            levelUpHint?.RemoveFromClassList("hint-visible");
            levelUpHint?.RemoveFromClassList("hint-pulse-dim");

            centerLevelUpHint?.RemoveFromClassList("hint-visible");
            centerLevelUpHint?.RemoveFromClassList("hint-pulse-dim");

            var schedulerElement = levelUpHint ?? centerLevelUpHint;

            schedulerElement.schedule.Execute(() =>
            {
                // Double check we haven't become ready again during the fade-out
                if (levelLabel != null && levelUpHint.ClassListContains("hint-visible")) 
                    return;
                    
                if (levelUpHint != null)
                    levelUpHint.style.display = DisplayStyle.None;
                if (centerLevelUpHint != null)
                    centerLevelUpHint.style.display = DisplayStyle.None;
                    
            }).StartingIn(450); // slightly longer than the 0.4s transition
        }
    }

    /// <summary>
    /// Alternates hint-pulse-dim on/off every ~950 ms to create a slow,
    /// breathing pulse. The USS transition-duration (0.9s) handles the
    /// smooth fade between the two opacity values.
    /// </summary>
    private void StartPulse()
    {
        if (_isPulsing) return;
        _isPulsing = true;

        var schedulerElement = levelUpHint ?? centerLevelUpHint;
        if (schedulerElement == null) return;

        _pulseSchedule = schedulerElement.schedule.Execute(() =>
        {
            TogglePulseClass(levelUpHint);
            TogglePulseClass(centerLevelUpHint);
        }).Every(950);
    }

    private void TogglePulseClass(Label element)
    {
        if (element == null) return;

        if (element.ClassListContains("hint-pulse-dim"))
            element.RemoveFromClassList("hint-pulse-dim");
        else
            element.AddToClassList("hint-pulse-dim");
    }

    private void StopPulse()
    {
        _pulseSchedule?.Pause();
        _pulseSchedule = null;
        _isPulsing = false;
    }
}
