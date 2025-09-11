using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Chest : MonoBehaviour, IInteractable
{
    [SerializeField] private string prompt = "Press E to interact";
    [SerializeField] private int price = 100;
    [SerializeField] private bool singleActivation = true;
    [SerializeField] private AudioSource audioData;
    [SerializeField] private GameObject item;
    public string InteractionPrompt => prompt;
    public bool Interact(Interactor interactor)
    {
        Debug.Log("Interacted with " + gameObject.name);
        if (singleActivation)
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

            singleActivation = false;
            return true;
        }

        return false;
    }
}
