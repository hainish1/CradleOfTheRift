using UnityEngine;

public class AUDIO_PlayerSounds : MonoBehaviour
{
    public AK.Wwise.Event meleeEvent;
    public AK.Wwise.Event shootEvent;
    public AK.Wwise.Event dashEvent;

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayMelee()
    {
        meleeEvent.Post(gameObject);
    }

    public void PlayShoot()
    {
        shootEvent.Post(gameObject);
    }

    public void PlayDash()
    {
        dashEvent.Post(gameObject);
    }
}
