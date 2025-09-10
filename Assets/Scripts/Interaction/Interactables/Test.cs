using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour, IInteractable
{
    [SerializeField] private string prompt = "Press E to interact";
    public string InteractionPrompt => prompt;
    public bool Interact(Interactor interactor)
    {
        Debug.Log("Interacted with " + gameObject.name);
        return true;
    }
}
