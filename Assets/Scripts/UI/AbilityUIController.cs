using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class AbilityUIController : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset abilitySlotAsset;

    [System.Serializable]
    public class AbilityInfo
    {
        public string abilityName;
        public string key;
        public Texture2D icon; // Assign directly in Inspector
        public int maxCharges;
        [HideInInspector] public int currentCharges; // track at runtime
    }


    [System.Serializable]
    public class AbilitySlot
    {
        public VisualElement slotElement;
        public Label chargeLabel;
        public VisualElement cooldownOverlay;
    }

    private List<AbilitySlot> abilitySlots = new List<AbilitySlot>();



    [SerializeField] private AbilityInfo[] abilities;

    private VisualElement abilityBar;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        abilityBar = root.Q<VisualElement>("AbilityBar");

        foreach (var ability in abilities)
        {
            CreateAbility(ability);
        }
    }

    void CreateAbility(AbilityInfo ability)
    {
        var slot = abilitySlotAsset.Instantiate();
        var chargeLabel = slot.Q<Label>("ChargeLabel");
        var overlay = slot.Q<VisualElement>("CooldownOverlay");

        // Initialize
        ability.currentCharges = ability.maxCharges;
        chargeLabel.text = ability.currentCharges.ToString();
        slot.Q<Label>("KeyLabel").text = ability.key;

        var iconElement = slot.Q<VisualElement>("AbilityIcon");
        iconElement.style.backgroundImage = new StyleBackground(ability.icon);
        iconElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
        iconElement.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
        iconElement.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
        iconElement.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);

        overlay.style.opacity = 0;

        abilityBar.Add(slot);

        // Track slot
        abilitySlots.Add(new AbilitySlot
        {
            slotElement = slot,
            chargeLabel = chargeLabel,
            cooldownOverlay = overlay
        });
    }

    public IEnumerator StartCooldown(VisualElement overlay, AbilityInfo ability, Label chargeLabel, float cooldownTime)
    {
        overlay.style.opacity = 1;
        overlay.style.height = Length.Percent(100); // full cover

        float elapsed = 0f;
        while (elapsed < cooldownTime)
        {
            elapsed += Time.deltaTime;
            float fill = Mathf.Lerp(100, 0, elapsed / cooldownTime);
            overlay.style.height = Length.Percent(fill); // shrink from top to bottom
            yield return null;
        }

        overlay.style.opacity = 0;

        // Refill one charge
        if (ability.currentCharges < ability.maxCharges)
        {
            ability.currentCharges++;
            chargeLabel.text = ability.currentCharges.ToString();
        }
    }


    public void OnAbilityPressed(int abilityIndex)
    {
        var ability = abilities[abilityIndex];
        var slot = abilitySlots[abilityIndex];

        if (ability.currentCharges <= 0)
        {
            Debug.Log($"{ability.abilityName} has no charges left!");
            return; // cannot use ability
        }

        // Reduce charges
        ability.currentCharges--;
        slot.chargeLabel.text = ability.currentCharges.ToString();

        // Start cooldown overlay
        float cooldownDuration = 5f; // Example cooldown duration
        StartCoroutine(StartCooldown(slot.cooldownOverlay, ability, slot.chargeLabel, cooldownDuration));
        Debug.Log($"Ability {ability.abilityName} pressed. Charges left: {ability.currentCharges}");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            OnAbilityPressed(0);
    }
}
