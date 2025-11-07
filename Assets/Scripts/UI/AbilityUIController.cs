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
    }

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
        slot.Q<Label>("KeyLabel").text = ability.key;
        slot.Q<Label>("ChargeLabel").text = ability.maxCharges.ToString();

        var iconElement = slot.Q<VisualElement>("AbilityIcon");
        iconElement.style.backgroundImage = new StyleBackground(ability.icon);
        iconElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
        iconElement.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
        iconElement.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
        iconElement.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);

        slot.Q<VisualElement>("CooldownOverlay").style.opacity = 0;

        abilityBar.Add(slot);
    }
}
