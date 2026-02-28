using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Item config")]
    [SerializeField] ItemData itemData;
    [SerializeField] bool destroyOnPickup = true;

    [Header("Tooltip")]
    [Tooltip("How close the player needs to be for tooltip to show")]
    [SerializeField] private float tooltipRange = 20f;

    private Vector3 startPosition;
    private Transform playerTransform;
    private bool isShowingTooltip;

    void Start()
    {
        startPosition = transform.position;

        // Set visual based on rarity
        if (itemData != null)
        {
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = itemData.rarityColor;
            }
        }

        // get player transform
        var player = GameObject.FindWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }


    // check distance from player and show or hide tooltip
    void Update()
    {
        if (playerTransform == null || itemData == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        if (dist <= tooltipRange && !isShowingTooltip)
        {
            isShowingTooltip = true;
            if (ItemPickupTooltipUI.Instance != null)
                ItemPickupTooltipUI.Instance.Show(itemData, transform);
        }
        else if (dist > tooltipRange && isShowingTooltip)
        {
            isShowingTooltip = false;
            if (ItemPickupTooltipUI.Instance != null)
                ItemPickupTooltipUI.Instance.Hide();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check for PlayerMovement 
        if (!other.GetComponent<PlayerMovement>())
        {
            return;
        }
        var inventory = other.GetComponent<PlayerInventory>();
        if (inventory != null && itemData != null)
        {
            // hide tooltip when pickup
            if (isShowingTooltip && ItemPickupTooltipUI.Instance != null)
            {
                ItemPickupTooltipUI.Instance.Hide();
                isShowingTooltip = false;
            }

            inventory.AddItem(itemData);

            // show pickup banner after pickupin up item
            if (ItemPickupBannerUI.Instance != null)
                ItemPickupBannerUI.Instance.Show(itemData);

            if (destroyOnPickup)
            {
                Destroy(gameObject, 0.05f);
            }
        }
    }

    void OnDestroy()
    {
        // hide if item destroyed
        if (isShowingTooltip && ItemPickupTooltipUI.Instance != null)
        {
            ItemPickupTooltipUI.Instance.Hide();
            isShowingTooltip = false;
        }
    }

    public ItemData ItemData => itemData;
}
