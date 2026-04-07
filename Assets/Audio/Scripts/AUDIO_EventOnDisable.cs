using UnityEngine;

public class AUDIO_EventOnDisable : MonoBehaviour
{
    [SerializeField]
    private AK.Wwise.Event audioEvent;
    void OnDisable()
    {
        if (audioEvent is null) return;
        audioEvent.Post(gameObject);
    }
}
