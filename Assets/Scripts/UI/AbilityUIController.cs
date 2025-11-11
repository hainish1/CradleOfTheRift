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
        public Texture2D icon;
        public int maxCharges;
        [HideInInspector] public int currentCharges;
        [HideInInspector] public int pendingCooldowns = 0; // how many cooldowns are waiting
        [HideInInspector] public bool isCooldownRunning = false; // is overlay animating
    }


    [System.Serializable]
    public class AbilitySlot
    {
        public VisualElement slotElement;
        public Label chargeLabel;
        public List<VisualElement> cooldownOverlays = new List<VisualElement>();
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

        // Initialize ability
        ability.currentCharges = ability.maxCharges;
        chargeLabel.text = ability.currentCharges.ToString();
        slot.Q<Label>("KeyLabel").text = ability.key;

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




    public void OnAbilityPressed(int abilityIndex)
    {
        var ability = abilities[abilityIndex];
        var slot = abilitySlots[abilityIndex];

        if (ability.currentCharges <= 0)
            return;

        ability.currentCharges--;
        slot.chargeLabel.text = ability.currentCharges.ToString();

        // Find the first inactive overlay
        var overlay = slot.cooldownOverlays.Find(o => o.style.opacity.value == 0);
        if (overlay != null)
            StartCoroutine(StartCooldown(overlay, ability, slot.chargeLabel, 5f));
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
            OnAbilityPressed(0);
    }
}
