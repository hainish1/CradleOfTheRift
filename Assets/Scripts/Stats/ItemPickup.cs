using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Item config")]
    [SerializeField] ItemData itemData;
    [SerializeField] bool destroyOnPickup = true;

    private Vector3 startPosition;

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
    }

    void OnTriggerEnter(Collider other)
    {
        // Check for PlayerMovement or PlayerMovementV4
        if (!other.GetComponent<PlayerMovement>() && !other.GetComponent<PlayerMovement>())
        {
            return;
        }
        var inventory = other.GetComponent<PlayerInventory>();
        if (inventory != null && itemData != null)
        {
            inventory.AddItem(itemData);
            if (destroyOnPickup)
            {
                Destroy(gameObject,0.05f);//delay destory for multiple effect
            }
        }
    }
}
