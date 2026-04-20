using UnityEngine;

public class AUDIO_PostOnCreate : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField]
    private AK.Wwise.Event _event;
    

    void Awake()
    {
        if(_event != null)
            if (_event.IsValid())
                _event.Post(gameObject);
    }
}
