using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] Image iconImage;
    [SerializeField] Image backgroundImage;
    [SerializeField] TextMeshProUGUI stackCountText;
    [SerializeField] Button button; // for hover purpose

    private ItemData itemData;
    private int stackCount;

    public void Initialize(ItemData item, int count, Color rarityColor)
    {
        itemData = item;
        stackCount = count;
        // Set the icon of that ting
        if (iconImage != null && item.icon != null)
        {
            iconImage.sprite = item.icon;
            iconImage.color = Color.white;
        }

        // Set background color based on rarity of item
        if (backgroundImage != null)
        {
            backgroundImage.color = rarityColor;
        }

        // Set count of the stack
        UpdateStack(count);
        // for tooltip thing
        if (button != null)
        {
            button.onClick.AddListener(ShowTooltip);
        }
    }

    public void UpdateStack(int newCount)
    {
        stackCount = newCount;
        if (stackCountText != null)
        {
            if (stackCount > 1)
            {
                stackCountText.text = stackCount.ToString();
                stackCountText.gameObject.SetActive(true);
            }
            else
            {
                stackCountText.gameObject.SetActive(false);

            }
        }
    }

    public void ShowTooltip()
    {
        // debuging for now but can show like properly also, idk
        Debug.Log($"{itemData.itemName}: {itemData.description}");
    }
}
