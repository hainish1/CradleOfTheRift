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
    [SerializeField] private GameObject item;
    public string InteractionPrompt => "[E] - " + price + "G";
    public bool SingleActivation => singleActivation;
    private bool canInteract = true;
    public bool Interact(Interactor interactor)
    {
        Debug.Log("Interacted with " + gameObject.name);

        if (canInteract)
        {
            // Check if the interactor has enough money

            // if (interactor.GetComponent<PlayerMoney>() >= price)
            // {
            //`     interactor.GetComponent<PlayerMoney>() -= price;
            // };
            // Disable further interactions

            // Play sounds
            audioData = GetComponent<AudioSource>();
            audioData.Play(0);
            // Spawn items
            if (item != null)
            {
                Instantiate(item, transform.position + Vector3.up, Quaternion.identity);
            }
            else
            {
                // Spawn random item perhaps
                //Instantiate(item, transform.position + Vector3.up, Quaternion.identity);
            }
            Destroy(gameObject, 1f); // Add a Delay to allow sound to play and block subsequent interactions
            return true;
        }

        if (SingleActivation)
        {
            canInteract = false; // I really dont know how to make this prettier but this will do
        }

        return false;
    }
}
