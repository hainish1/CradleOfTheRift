using UnityEngine;

public class AUDIO_PostEvent : MonoBehaviour
{
    public AK.Wwise.Event postEvent;

    public void PlaySound()
    {
        postEvent.Post(gameObject);
    }
}
