using UnityEngine;

public class AUDIO_GlobalAudioPlayer : MonoBehaviour
{
    public static AUDIO_GlobalAudioPlayer Instance { get; private set; }


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
}
