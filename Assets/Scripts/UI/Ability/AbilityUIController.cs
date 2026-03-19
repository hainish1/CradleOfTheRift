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

    [SerializeField]
    private List<Texture2D> images;

    private int flyAbilityIndex = -1;

    [System.Serializable]
    public class AbilitySlot
    {
        public VisualElement         slotElement;
        public Label                 chargeLabel;
        public DiamondAbilityElement diamond;
    }

    private List<AbilitySlot> abilitySlots = new List<AbilitySlot>();
    private List<AbilityInfo> abilities    = new List<AbilityInfo>();
    private VisualElement     abilityBar;
    private void UpdateChargeLabel(Label label, AbilityInfo ability)
    {
        label.text = ability.showCharges ? ability.currentCharges.ToString() : "";
        label.EnableInClassList("hidden", !ability.showCharges);
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
            Debug.LogError("Player manager not found");
            return;
        }

        playerStats = playerManager.Stats;
        if (playerStats == null)
        {
            Debug.LogError("PlayerManager.Stats is null!");
            return;
        }

        playerMovement = playerManager.GetComponent<PlayerMovement>();
        if (playerMovement == null)
            Debug.LogError("PlayerMovement not found!");

        // ---- Dash ---------------------------------------------------- //
        AbilityInfo dashAbility = new AbilityInfo
        {
            abilityName    = "Dash",
            key            = KeyCode.LeftShift,
            icon           = images[0],
            maxCharges     = playerStats.DashCharges,
            currentCharges = playerStats.DashCharges,
            getCooldown    = () => playerStats.DashCooldown
        };
        abilities.Add(dashAbility);
        CreateAbility(dashAbility);
        if (playerMovement != null)
            playerMovement.DashCooldownStarted += OnDashCooldownStarted;

        // ---- Fly ----------------------------------------------------- //
        AbilityInfo flyAbility = new AbilityInfo
        {
            abilityName    = "Fly",
            key            = KeyCode.F,
            icon           = images.Count > 1 ? images[1] : null,
            maxCharges     = 1,
            currentCharges = 1,
            getCooldown    = () => 0f,
            showCharges = false
        };
        flyAbilityIndex = abilities.Count;
        abilities.Add(flyAbility);
        CreateAbility(flyAbility, fillFromTop: true);

        // ---- Shockwave ----------------------------------------------- //
        AbilityInfo shockwaveAbility = new AbilityInfo
        {
            abilityName    = "Shockwave",
            key            = KeyCode.X,
            icon           = images.Count > 2 ? images[2] : null,
            maxCharges     = 1,
            currentCharges = 1,
            getCooldown    = () => playerStats.ShockwaveCooldown,
            showCharges = false
        };
        abilities.Add(shockwaveAbility);
        CreateAbility(shockwaveAbility);

        // ---- Ranged -------------------------------------------------- //
        AbilityInfo rangedAbility = new AbilityInfo
        {
            abilityName    = "Ranged",
            key            = KeyCode.Mouse1,
            icon           = images.Count > 3 ? images[3] : null,
            maxCharges     = playerStats.FireCharges,
            currentCharges = playerStats.FireCharges,
            getCooldown    = () => playerStats.FireChargeCooldown
        };
        abilities.Add(rangedAbility);
        CreateAbility(rangedAbility);
    }

    string GetKeyDisplayName(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Mouse0:      return "LMB";
            case KeyCode.Mouse1:      return "RMB";
            case KeyCode.Mouse2:      return "MMB";
            case KeyCode.LeftShift:   return "Shift";
            case KeyCode.LeftAlt:     return "Alt";
            case KeyCode.LeftControl: return "Ctrl";
            case KeyCode.Space:       return "Space";
            default:                  return key.ToString();
        }
    }

    void CreateAbility(AbilityInfo ability, bool fillFromTop = false)
    {
        var slot        = abilitySlotAsset.Instantiate();
        var chargeLabel = slot.Q<Label>("ChargeLabel");
        slot.Q<Label>("KeyLabel").text = GetKeyDisplayName(ability.key);
        UpdateChargeLabel(chargeLabel, ability);

        var diamond = slot.Q<DiamondAbilityElement>("DiamondSlot");

        // Configure the diamond
        diamond.Icon         = ability.icon;
        diamond.CooldownT    = 0f;
        diamond.FillFromTop  = fillFromTop;

        abilityBar.Add(slot);

        abilitySlots.Add(new AbilitySlot
        {
            slotElement = slot,
            chargeLabel = chargeLabel,
            diamond     = diamond
        });
    }

    void Update()
    {
        // Guard: don't run until Start has fully populated both lists.
        if (abilities.Count == 0 || abilitySlots.Count != abilities.Count) return;

        UpdateFlightAbilityUI();

        // Sync dash charge count from movement component
        if (playerMovement != null)
        {
            abilities[0].currentCharges      = playerMovement.CurrentDashCharges;
            UpdateChargeLabel(abilitySlots[0].chargeLabel, abilities[0]);
        }

        // Input polling for all non-fly abilities
        for (int i = 0; i < abilities.Count; i++)
        {
            if (i == flyAbilityIndex) continue;

            var ability = abilities[i];
            bool pressed = ability.key switch
            {
                KeyCode.Mouse0 => Input.GetMouseButtonDown(0),
                KeyCode.Mouse1 => Input.GetMouseButtonDown(1),
                KeyCode.Mouse2 => Input.GetMouseButtonDown(2),
                _              => Input.GetKeyDown(ability.key)
            };

            if (pressed) OnAbilityPressed(i);
        }
    }

    void OnDashCooldownStarted(float duration)
    {
        if (abilities.Count == 0) return;
        var ability = abilities[0];
        var slot    = abilitySlots[0];
        ability.pendingCooldowns++;
        if (!ability.isCooldownRunning)
            StartCoroutine(ProcessCooldownQueue(ability, slot));
    }

    void OnDestroy()
    {
        if (playerMovement != null)
            playerMovement.DashCooldownStarted -= OnDashCooldownStarted;
    }

    void UpdateFlightAbilityUI()
    {
        if (flyAbilityIndex < 0 || playerMovement == null) return;

        var slot = abilitySlots[flyAbilityIndex];
        // FlightEnergyRatio: 1 = full, 0 = empty.
        // CooldownT:         0 = no overlay (full), 1 = fully covered (empty).
        slot.diamond.CooldownT = 1f - playerMovement.FlightEnergyRatio;
    }

    public void OnAbilityPressed(int abilityIndex)
    {
        if (abilityIndex == 0) return; // dash handled via event

        var ability = abilities[abilityIndex];
        var slot    = abilitySlots[abilityIndex];

        if (ability.currentCharges <= 0) return;

        ability.currentCharges--;
        UpdateChargeLabel(slot.chargeLabel, ability);

        ability.pendingCooldowns++;
        if (!ability.isCooldownRunning)
            StartCoroutine(ProcessCooldownQueue(ability, slot));
    }

    private IEnumerator ProcessCooldownQueue(AbilityInfo ability, AbilitySlot slot)
    {
        ability.isCooldownRunning = true;

        while (ability.pendingCooldowns > 0)
        {
            if (slot.diamond.CooldownT < 0.001f)
            {
                yield return StartCoroutine(
                    StartCooldown(slot.diamond, ability, slot.chargeLabel, ability.getCooldown()));
            }
            ability.pendingCooldowns--;
        }

        ability.isCooldownRunning = false;
    }

    public IEnumerator StartCooldown(
        DiamondAbilityElement diamond,
        AbilityInfo ability,
        Label chargeLabel,
        float cooldownTime)
    {
        diamond.CooldownT = 1f;

        float elapsed = 0f;
        while (elapsed < cooldownTime)
        {
            elapsed          += Time.deltaTime;
            diamond.CooldownT = Mathf.Lerp(1f, 0f, elapsed / cooldownTime);
            yield return null;
        }

        diamond.CooldownT = 0f;

        ability.currentCharges++;
        UpdateChargeLabel(chargeLabel, ability);
    }
}

[System.Serializable]
public class AbilityInfo
{
    public string      abilityName;
    public KeyCode     key;
    public Texture2D   icon;
    public int         maxCharges;
    public int         currentCharges;
    public Func<float> getCooldown;
    public float       CooldownRemaining => getCooldown();
    public bool showCharges = true;

    [HideInInspector] public int  pendingCooldowns  = 0;
    [HideInInspector] public bool isCooldownRunning = false;
}
