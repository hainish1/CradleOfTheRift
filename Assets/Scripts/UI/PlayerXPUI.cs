using UnityEngine;
using UnityEngine.UIElements;

public class PlayerXPUI : MonoBehaviour
{
    private Label levelLabel;
    private Label levelUpHint;
    private Label xpLabel;
    private ProgressBar xpBar;

    void Start()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        levelLabel = root.Q<Label>("LevelLabel");
        levelUpHint = root.Q<Label>("LevelUpHint");
        xpLabel = root.Q<Label>("XPLabel");
        xpBar = root.Q<ProgressBar>("XPBar");

        PlayerXP xp = PlayerXP.Instance;
        if (xp == null)
        {
            Debug.LogWarning("PlayerXP.instance not found.");
            return;
        }

        // subscribe
        xp.XPChanged += OnXPChanged;
        xp.LeveledUp += OnLeveledUp;
        xp.LevelUpAvailable += OnLevelUpAvailable;

        // initialize
        Refresh(xp.CurrentXP, xp.XPToLevelUp, xp.CurrentLevel);
    }

    void OnDestroy()
    {
        if (PlayerXP.Instance != null)
        {
            PlayerXP.Instance.XPChanged -= OnXPChanged;
            PlayerXP.Instance.LeveledUp -= OnLeveledUp;
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
        {
            levelLabel.text = $"Lv {level}";
        }

        if (levelUpHint != null)
        {
            bool showHint = PlayerXP.Instance != null && PlayerXP.Instance.IsLevelUpReady;
            levelUpHint.style.display = showHint ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (xpLabel != null)
            xpLabel.text = $"{currentXP} / {xpToLevelUp} XP";

        if (xpBar != null)
        {
            xpBar.highValue = xpToLevelUp;
            xpBar.value = currentXP;
        }
    }
}