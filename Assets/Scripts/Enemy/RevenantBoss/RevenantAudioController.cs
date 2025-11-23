using UnityEngine;

public class RevenantAudioController : MonoBehaviour
{
    [SerializeField] private AudioClip fireProjectileSound;
    [SerializeField] private AudioClip fireAOEProjectileSound;
    [SerializeField] private AudioClip attackIndicatorSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip ambientSound;            // No sound for this yet
    private AudioSource attackAudioSource;
    private AudioSource ambientAudioSource;

    void Awake()
    {
        attackAudioSource = GetComponent<AudioSource>();
        ambientAudioSource = GetComponent<AudioSource>();
    }

    public void PlayFireProjectileSound()
    {
        if (attackAudioSource != null && fireProjectileSound != null)
        {
            attackAudioSource.Stop();
            attackAudioSource.PlayOneShot(fireProjectileSound);
        }
    }

    public void PlayFireAOEProjectileSound()
    {
        if (attackAudioSource != null && fireAOEProjectileSound != null)
        {
            attackAudioSource.Stop();
            attackAudioSource.PlayOneShot(fireAOEProjectileSound);
        }
    }

    public void PlayAttackIndicatorSound()
    {
        if (attackAudioSource != null && attackIndicatorSound != null)
        {
            attackAudioSource.Stop();
            attackAudioSource.PlayOneShot(attackIndicatorSound);
        }
    }

    public void PlayDeathSound()
    {
        if (ambientAudioSource != null && deathSound != null)
        {
            ambientAudioSource.Stop();
            ambientAudioSource.PlayOneShot(deathSound);
        }
    }
}
