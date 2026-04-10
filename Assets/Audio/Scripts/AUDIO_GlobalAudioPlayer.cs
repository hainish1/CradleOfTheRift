using UnityEngine;

public class AUDIO_GlobalAudioPlayer : MonoBehaviour
{
    public static AUDIO_GlobalAudioPlayer Instance { get; private set; }
    [Header("Global Sound Effects")]
    [SerializeField] private AK.Wwise.Event itemPickup;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // If the Instance is already created, delete this one!
        if (Instance != null && Instance != this){
            Destroy(this);
        }
        // If it isn't created yet, create it.
        else
        {
            Instance = this;
        }
    }

    /// <summary>
    /// Posts the passed in Wwise Event.
    /// This is very useful for playing
    /// sfx AFTER an object is destroyed.
    /// Wwise doesn't like posting events
    /// from destroyed objects :(
    /// </summary>
    /// <param name="sound">The Wwise Event to post.</param>
    public void PlaySound(AK.Wwise.Event sound)
    {
        if (sound.IsValid())
        {
            // gameObject is this global object.
            sound.Post(Instance.gameObject);
        }
    }

    // This is an insanely goofy work around
    // I just don't want to set a variable for every single item.
    // Can you blame me though? There are like 100s of items.
    // AND I've already tried triggering an event by name...
    // AND THAT DOESN'T WORK CONSISTENTLY!!!
    public void PlayPickupItem()
    {
        if (itemPickup.IsValid())
        {
            itemPickup.Post(Instance.gameObject);
        }
    }
}
