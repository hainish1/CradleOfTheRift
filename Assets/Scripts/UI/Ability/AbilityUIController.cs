using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class AbilityUIController : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset abilitySlotAsset;
    [SerializeField] private PlayerManager playerManager;
    private Stats playerStats;
    private PlayerMovement playerMovement;

    [SerializeField] private List<Texture2D> images;

    private int flyAbilityIndex = -1;
    private int shockwaveAbilityIndex = -1;
    private int rangedAbilityIndex = -1;

    [System.Serializable]
    public class AbilitySlot
    {
        public VisualElement slotElement;
        public Label chargeLabel;
        public Label cooldownLabel;
        public DiamondAbilityElement diamond;
    }

    private List<AbilitySlot> abilitySlots = new List<AbilitySlot>();
    private List<AbilityInfo> abilities = new List<AbilityInfo>();
    private VisualElement abilityBar;

    private void UpdateChargeLabel(Label label, AbilityInfo ability)
    {
        label.text = ability.showCharges ? ability.currentCharges.ToString() : "";
        label.EnableInClassList("hidden", !ability.showCharges);
    }

    private string FormatCooldown(float seconds)
    {
        if (seconds <= 0f) return "";
        return Mathf.CeilToInt(seconds).ToString();
    }

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        abilityBar = root.Q<VisualElement>("AbilityBar");

        if (playerManager == null)
            playerManager = PlayerLocator.FindPlayerComponent<PlayerManager>();
        if (playerManager == null)
            playerManager = FindFirstObjectByType<PlayerManager>();
        if (playerManager == null)
        {
            Debug.LogError("AbilityUIController: Player manager not found.");
            return;
        }

        playerStats = playerManager.Stats;
        if (playerStats == null) { Debug.LogError("PlayerManager.Stats is null!"); return; }

        playerMovement = playerManager.GetComponent<PlayerMovement>();
        if (playerMovement == null) Debug.LogError("PlayerMovement not found!");

        // Remove placeholder abilities from playerUI.uxml
        abilityBar.Clear();

        // ---- Dash -------------------------------------------------------- //
        var dashAbility = new AbilityInfo
        {
            abilityName = "Dash",
            key = KeyCode.LeftShift,
            icon = images[0],
            maxCharges = playerStats.DashCharges,
            currentCharges = playerStats.DashCharges,
            getCooldown = () => playerStats.DashCooldown
        };
        abilities.Add(dashAbility);
        CreateAbility(dashAbility);

        // ---- Fly --------------------------------------------------------- //
        var flyAbility = new AbilityInfo
        {
            abilityName = "Fly",
            key = KeyCode.F,
            icon = images.Count > 1 ? images[1] : null,
            maxCharges = 1,
            currentCharges = 1,
            getCooldown = () => 0f,
            showCharges = false
        };
        flyAbilityIndex = abilities.Count;
        abilities.Add(flyAbility);
        CreateAbility(flyAbility, fillFromTop: true);

        // ---- Shockwave --------------------------------------------------- //
        var shockwaveAbility = new AbilityInfo
        {
            abilityName = "Shockwave",
            key = KeyCode.X,
            icon = images.Count > 2 ? images[2] : null,
            maxCharges = 1,
            currentCharges = 1,
            getCooldown = () => playerStats.ShockwaveCooldown,
            showCharges = false
        };
        shockwaveAbilityIndex = abilities.Count;
        abilities.Add(shockwaveAbility);
        CreateAbility(shockwaveAbility);

        // ---- Ranged ------------------------------------------------------ //
        var rangedAbility = new AbilityInfo
        {
            abilityName = "Ranged",
            key = KeyCode.Mouse1,
            icon = images.Count > 3 ? images[3] : null,
            maxCharges = playerStats.FireCharges,
            currentCharges = playerStats.FireCharges,
            getCooldown = () => playerStats.FireChargeCooldown
        };
        rangedAbilityIndex = abilities.Count;
        abilities.Add(rangedAbility);
        CreateAbility(rangedAbility);
    }

    private void OnEnable()
    {
        // Dash
        PlayerMovement.OnDashChargeSpent += HandleDashChargeSpent;
        PlayerMovement.OnDashChargeRestored += HandleDashChargeRestored;

        // Ranged
        PlayerShooter.OnFireChargeSpent += HandleFireChargeSpent;
        PlayerShooter.OnFireChargeRestored += HandleFireChargeRestored;

        // Shockwave / GroundSlam (share the same UI slot)
        PlayerShockwave.OnShockwaveUsed += HandleShockwaveUsed;
        PlayerGroundSlam.OnGroundSlamUsed += HandleShockwaveUsed;
    }

    private void OnDisable()
    {
        PlayerMovement.OnDashChargeSpent -= HandleDashChargeSpent;
        PlayerMovement.OnDashChargeRestored -= HandleDashChargeRestored;

        PlayerShooter.OnFireChargeSpent -= HandleFireChargeSpent;
        PlayerShooter.OnFireChargeRestored -= HandleFireChargeRestored;

        PlayerShockwave.OnShockwaveUsed -= HandleShockwaveUsed;
        PlayerGroundSlam.OnGroundSlamUsed -= HandleShockwaveUsed;
    }

    void Update()
    {
        if (abilities.Count == 0 || abilitySlots.Count != abilities.Count) return;
        UpdateFlightAbilityUI();
    }

    void CreateAbility(AbilityInfo ability, bool fillFromTop = false)
    {
        var slot = abilitySlotAsset.Instantiate();
        var chargeLabel = slot.Q<Label>("ChargeLabel");
        var cooldownLabel = slot.Q<Label>("CooldownLabel");
        slot.Q<Label>("KeyLabel").text = GetKeyDisplayName(ability.key);
        UpdateChargeLabel(chargeLabel, ability);

        var diamond = slot.Q<DiamondAbilityElement>("DiamondSlot");
        diamond.Icon = ability.icon;
        diamond.CooldownT = 0f;
        diamond.FillFromTop = fillFromTop;

        abilityBar.Add(slot);

        abilitySlots.Add(new AbilitySlot
        {
            slotElement = slot,
            chargeLabel = chargeLabel,
            cooldownLabel = cooldownLabel,
            diamond = diamond
        });
    }

    // Dash: a charge was spent; start a cooldown animation for one charge slot.
    private void HandleDashChargeSpent(int current, int max)
    {
        if (abilities.Count == 0) return;
        var ability = abilities[0];
        var slot = abilitySlots[0];

        ability.currentCharges = current;
        UpdateChargeLabel(slot.chargeLabel, ability);

        ability.pendingCooldowns++;
        if (!ability.isCooldownRunning)
            StartCoroutine(ProcessCooldownQueue(ability, slot));
    }

    // Dash: a charge was restored by the regen coroutine in PlayerMovement.
    // Advance the cooldown animation to completion immediately so the UI stays in sync.
    private void HandleDashChargeRestored(int current, int max)
    {
        if (abilities.Count == 0) return;
        var ability = abilities[0];
        var slot = abilitySlots[0];

        ability.currentCharges = current;
        UpdateChargeLabel(slot.chargeLabel, ability);
    }

    // Ranged:  a fire charge was spent.
    private void HandleFireChargeSpent(int current, int max)
    {
        if (rangedAbilityIndex < 0 || rangedAbilityIndex >= abilities.Count) return;
        var ability = abilities[rangedAbilityIndex];
        var slot = abilitySlots[rangedAbilityIndex];

        ability.currentCharges = current;
        UpdateChargeLabel(slot.chargeLabel, ability);

        ability.pendingCooldowns++;
        if (!ability.isCooldownRunning)
            StartCoroutine(ProcessCooldownQueue(ability, slot));
    }

    // Ranged: a fire charge was restored.
    private void HandleFireChargeRestored(int current, int max)
    {
        if (rangedAbilityIndex < 0 || rangedAbilityIndex >= abilities.Count) return;
        var ability = abilities[rangedAbilityIndex];
        var slot = abilitySlots[rangedAbilityIndex];

        ability.currentCharges = current;
        UpdateChargeLabel(slot.chargeLabel, ability);
    }

    // Shockwave: ability was used; start a single cooldown.
    private void HandleShockwaveUsed()
    {
        if (shockwaveAbilityIndex < 0 || shockwaveAbilityIndex >= abilities.Count) return;
        var ability = abilities[shockwaveAbilityIndex];
        var slot = abilitySlots[shockwaveAbilityIndex];

        ability.currentCharges = 0;
        ability.pendingCooldowns++;
        if (!ability.isCooldownRunning)
            StartCoroutine(ProcessCooldownQueue(ability, slot));
    }

    void UpdateFlightAbilityUI()
    {
        if (flyAbilityIndex < 0 || playerMovement == null) return;
        // CooldownT 0 = full energy (no overlay), 1 = empty (full overlay).
        abilitySlots[flyAbilityIndex].diamond.CooldownT = 1f - playerMovement.FlightEnergyRatio;
    }

    private IEnumerator ProcessCooldownQueue(AbilityInfo ability, AbilitySlot slot)
    {
        ability.isCooldownRunning = true;

        while (ability.pendingCooldowns > 0)
        {
            if (slot.diamond.CooldownT < 0.001f)
            {
                yield return StartCoroutine(
                    StartCooldown(slot.diamond, slot.cooldownLabel, ability, slot.chargeLabel,
                                  ability.getCooldown()));
            }
            ability.pendingCooldowns--;
        }

        ability.isCooldownRunning = false;
    }

    public IEnumerator StartCooldown(
        DiamondAbilityElement diamond,
        Label cooldownLabel,
        AbilityInfo ability,
        Label chargeLabel,
        float cooldownTime)
    {
        diamond.CooldownT = 1f;
        cooldownLabel.text = FormatCooldown(cooldownTime);

        float elapsed = 0f;
        while (elapsed < cooldownTime)
        {
            elapsed += Time.deltaTime;
            float remaining = Mathf.Max(0f, cooldownTime - elapsed);
            diamond.CooldownT = Mathf.Lerp(1f, 0f, elapsed / cooldownTime);
            cooldownLabel.text = FormatCooldown(remaining);
            yield return null;
        }

        diamond.CooldownT = 0f;
        cooldownLabel.text = "";

        // Charge label is updated by the Restored event, but update here too
        // for shockwave which has no Restored event.
        if (ability == abilities[shockwaveAbilityIndex])
        {
            ability.currentCharges = 1;
            UpdateChargeLabel(chargeLabel, ability);
        }
    }

    string GetKeyDisplayName(KeyCode key)
    {
        return key switch
        {
            KeyCode.Mouse0 => "LMB",
            KeyCode.Mouse1 => "RMB",
            KeyCode.Mouse2 => "MMB",
            KeyCode.LeftShift => "Shift",
            KeyCode.LeftAlt => "Alt",
            KeyCode.LeftControl => "Ctrl",
            KeyCode.Space => "Space",
            _ => key.ToString()
        };
    }
}

[System.Serializable]
public class AbilityInfo
{
    public string abilityName;
    public KeyCode key;
    public Texture2D icon;
    public int maxCharges;
    public int currentCharges;
    public Func<float> getCooldown;
    public float CooldownRemaining => getCooldown();
    public bool showCharges = true;

    [HideInInspector] public int  pendingCooldowns  = 0;
    [HideInInspector] public bool isCooldownRunning = false;
}
