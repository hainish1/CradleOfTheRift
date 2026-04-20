using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Chest : MonoBehaviour, IInteractable
{
    //[SerializeField] private string prompt = "Press E to interact";
    [SerializeField] private int price = 10;
    [SerializeField] private bool singleActivation = true;
    [SerializeField] private AudioSource audioData;
    [SerializeField] private LootTable lootTable;

    [Header("Visuals")]
    [Tooltip("The closed chest")]
    [SerializeField] private GameObject closedChestVisual;
    [Tooltip("The open chest")]
    [SerializeField] private GameObject openChestVisual;
    [Tooltip("How long the open chest stays visible before being obliterated")]
    [SerializeField] private float destroyDelay = 2f;

    [Header("Audio Settings")]
    [SerializeField] private AK.Wwise.Event OpenSound;
    [SerializeField] private AK.Wwise.Event TooExpensiveSound;
    public string InteractionPrompt => "[E] - " + price + "G";
    public bool SingleActivation => singleActivation;
    private bool canInteract = true;
    public bool Interact(Interactor interactor)
    {
        Debug.Log("Interacted with " + gameObject.name);

        if (canInteract)
        {
            // Check if the interactor has enough money

            if (interactor.GetComponent<PlayerGold>().SpendGold(price))
            {
                Debug.Log("U have money.");
                // Play sounds
                audioData = GetComponent<AudioSource>();
                audioData.Play(0);
                OpenSound.Post(gameObject);
                // Spawn items
                if (lootTable != null)
                {
                    //Instantiate(item, transform.position + Vector3.up, Quaternion.identity);
                    // grab the inventory here and pass it down
                    var inv = interactor.GetComponent<PlayerInventory>();
                    lootTable.DoDrop(inv);
                    Debug.Log("Dropped loot.");
                }
                else
                {
                    // Spawn random item perhaps
                    //Instantiate(item, transform.position + Vector3.up, Quaternion.identity);
                    Debug.Log("No loot table.");
                }
                if (closedChestVisual != null) closedChestVisual.SetActive(false);
                if (openChestVisual != null) openChestVisual.SetActive(true);

                if (SingleActivation)
                {
                    canInteract = false;
                    Destroy(gameObject, destroyDelay); // Add a Delay to allow sound to play and block subsequent interactions
                }
                return true;
            }
            else
            {
                Debug.Log("U broke.");
                TooExpensiveSound.Post(gameObject);
            }
        }

        return false;
    }
}
