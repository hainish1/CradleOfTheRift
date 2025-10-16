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
                // Spawn items
                if (lootTable != null)
                {
                    //Instantiate(item, transform.position + Vector3.up, Quaternion.identity);
                    lootTable.DoDrop();
                    Debug.Log("Dropped loot.");
                }
                else
                {
                    // Spawn random item perhaps
                    //Instantiate(item, transform.position + Vector3.up, Quaternion.identity);
                    Debug.Log("No loot table.");
                }
                if (SingleActivation)
                {
                    canInteract = false;
                    Destroy(gameObject, 1f); // Add a Delay to allow sound to play and block subsequent interactions
                }   
                return true;
            }
            else
            {
                Debug.Log("U broke.");
            }
        }

        return false;
    }
}
