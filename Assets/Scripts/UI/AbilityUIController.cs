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
        public VisualElement slotElement;
        public Label chargeLabel;
        public List<VisualElement> cooldownOverlays = new List<VisualElement>();
    }

    private List<AbilitySlot> abilitySlots = new List<AbilitySlot>();

    private List<AbilityInfo> abilities = new();
    private VisualElement abilityBar;


    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        abilityBar = root.Q<VisualElement>("AbilityBar");

        playerManager = FindFirstObjectByType<PlayerManager>();
        if (playerManager == null)
        {
            Debug.LogError("PlayerManager not found!");
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
        {
            Debug.LogError("PlayerMovement not found!");
        }

        AbilityInfo dashAbility = new AbilityInfo
        {
            abilityName = "Dash",
            key = KeyCode.LeftShift,
            icon = this.images[0], 
            maxCharges = playerStats.DashCharges,
            currentCharges = playerStats.DashCharges,
            getCooldown = () => playerStats.DashCooldown
        };
        this.abilities.Add(dashAbility);
        CreateAbility(dashAbility);

        AbilityInfo flyAbility = new AbilityInfo
        {
            abilityName = "Fly",
            key = KeyCode.LeftAlt,
            icon = this.images.Count > 1 ? this.images[1] : null,
            maxCharges = 1,
            currentCharges = 1,
            getCooldown = () => 0f
        };
        flyAbilityIndex = this.abilities.Count;
        this.abilities.Add(flyAbility);
        CreateAbility(flyAbility);

        AbilityInfo shockwaveAbility = new AbilityInfo
        {
            abilityName = "Shockwave",
            key = KeyCode.X,
            icon = this.images.Count > 2 ? this.images[2] : null,
            maxCharges = 1,
            currentCharges = 1,
            getCooldown = () => playerStats.ShockwaveCooldown
        };
        this.abilities.Add(shockwaveAbility);
        CreateAbility(shockwaveAbility);

        AbilityInfo rangedAbility = new AbilityInfo
        {
            abilityName = "Ranged",
            key = KeyCode.Mouse1,
            icon = this.images.Count > 3 ? this.images[3] : null,
            maxCharges = playerStats.FireCharges,
            currentCharges = playerStats.FireCharges,
            getCooldown = () => playerStats.FireChargeCooldown
        };
        this.abilities.Add(rangedAbility);
        CreateAbility(rangedAbility);
    }
    
    string GetKeyDisplayName(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Mouse0: return "LMB";
            case KeyCode.Mouse1: return "RMB";
            case KeyCode.Mouse2: return "MMB";
            case KeyCode.LeftShift: return "Shift";
            case KeyCode.LeftAlt: return "Alt";
            case KeyCode.LeftControl: return "Ctrl";
            case KeyCode.Space: return "Space";
            default: return key.ToString();
        }
    }

    void CreateAbility(AbilityInfo ability)
    {
        var slot = abilitySlotAsset.Instantiate();
        var chargeLabel = slot.Q<Label>("ChargeLabel");

        // Initialize ability
        chargeLabel.text = ability.currentCharges.ToString();
        slot.Q<Label>("KeyLabel").text = GetKeyDisplayName(ability.key);

        // Initialize icon
        var iconElement = slot.Q<VisualElement>("AbilityIcon");
        iconElement.style.backgroundImage = new StyleBackground(ability.icon);
        iconElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
        iconElement.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
        iconElement.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
        iconElement.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);

        // Prepare overlays
        var overlayContainer = slot.Q<VisualElement>("AbilityIconContainer");
        List<VisualElement> overlays = new List<VisualElement>();

        for (int i = 0; i < ability.maxCharges; i++)
        {
            VisualElement overlayInstance = new VisualElement();
            overlayInstance.name = "CooldownOverlay" + i;

            // Match the styling of your original overlay
            overlayInstance.style.position = Position.Absolute;
            overlayInstance.style.top = 0;
            overlayInstance.style.left = 0;
            overlayInstance.style.width = Length.Percent(100);
            overlayInstance.style.height = Length.Percent(100);
            overlayInstance.style.backgroundColor = new Color(0, 0, 0, 0.5f); // semi-transparent black
            overlayInstance.style.opacity = 0;

            overlayContainer.Add(overlayInstance);
            overlays.Add(overlayInstance);
        }

        // Add the slot to the ability bar
        abilityBar.Add(slot);

        // Track the slot
        abilitySlots.Add(new AbilitySlot
        {
            slotElement = slot,
            chargeLabel = chargeLabel,
            cooldownOverlays = overlays
        });
    }

    void Update()
    {
        UpdateFlightAbilityUI();

        for (int i = 0; i < abilities.Count; i++)
        {
            if (i == flyAbilityIndex) continue;

            var ability = abilities[i];
            
            if (ability.key == KeyCode.Mouse0 && Input.GetMouseButtonDown(0))
            {
                OnAbilityPressed(i);
            }
            else if (ability.key == KeyCode.Mouse1 && Input.GetMouseButtonDown(1))
            {
                OnAbilityPressed(i);
            }
            else if (ability.key == KeyCode.Mouse2 && Input.GetMouseButtonDown(2))
            {
                OnAbilityPressed(i);
            }
            else if (ability.key != KeyCode.Mouse0 && ability.key != KeyCode.Mouse1 && ability.key != KeyCode.Mouse2)
            {
                if (Input.GetKeyDown(ability.key))
                {
                    OnAbilityPressed(i);
                }
            }
        }
    }

    void UpdateFlightAbilityUI()
    {
        if (flyAbilityIndex < 0 || playerMovement == null || playerStats == null) return;

        var slot = abilitySlots[flyAbilityIndex];
        var overlay = slot.cooldownOverlays[0];

        float maxEnergy = playerStats.FlightMaxEnergy;
        float currentEnergy = playerMovement.GetCurrentFlightEnergy();
        float energyRatio = maxEnergy > 0 ? currentEnergy / maxEnergy : 0f;

        if (energyRatio >= 1f)
        {
            overlay.style.opacity = 0;
            overlay.style.height = Length.Percent(0);
        }
        else
        {
            overlay.style.opacity = 1;
            overlay.style.height = Length.Percent((1f - energyRatio) * 100f);
        }
    }

    public void OnAbilityPressed(int abilityIndex)
{
    var ability = abilities[abilityIndex];
    var slot = abilitySlots[abilityIndex];

    if (ability.currentCharges <= 0)
        return;

    // Spend a charge
    ability.currentCharges--;
    slot.chargeLabel.text = ability.currentCharges.ToString();

    // Queue a cooldown
    ability.pendingCooldowns++;

    // If no cooldown is currently running, start it
    if (!ability.isCooldownRunning)
        StartCoroutine(ProcessCooldownQueue(ability, slot));
}

private IEnumerator ProcessCooldownQueue(AbilityInfo ability, AbilitySlot slot)
{
    ability.isCooldownRunning = true;

    while (ability.pendingCooldowns > 0)
    {
        // Find the first inactive overlay
        var overlay = slot.cooldownOverlays.Find(o => o.style.opacity.value == 0);
        if (overlay != null)
        {
            yield return StartCoroutine(StartCooldown(overlay, ability, slot.chargeLabel, ability.getCooldown()));
        }

        ability.pendingCooldowns--;
    }

    ability.isCooldownRunning = false;
}

public IEnumerator StartCooldown(VisualElement overlay, AbilityInfo ability, Label chargeLabel, float cooldownTime)
{
    overlay.style.opacity = 1;
    overlay.style.height = Length.Percent(100);

    float elapsed = 0f;
    while (elapsed < cooldownTime)
    {
        elapsed += Time.deltaTime;
        overlay.style.height = Length.Percent(Mathf.Lerp(100, 0, elapsed / cooldownTime));
        yield return null;
    }

    overlay.style.opacity = 0;

    // Refill a charge
    ability.currentCharges++;
    chargeLabel.text = ability.currentCharges.ToString();
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


    [HideInInspector] public int pendingCooldowns = 0;
    [HideInInspector] public bool isCooldownRunning = false;
}