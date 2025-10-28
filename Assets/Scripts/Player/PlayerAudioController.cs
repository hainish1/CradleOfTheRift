    using UnityEngine;

    public class PlayerAudioController : MonoBehaviour
    {
        [SerializeField] private AudioClip fireProjectileSound;
        [SerializeField] private AudioClip meleeSound;
        [SerializeField] private AudioClip slamSound;
        [SerializeField] private AudioClip dashSound;
        private AudioSource audioSource;

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        public void PlayAttackSound()
        {
            if (audioSource != null && fireProjectileSound != null)
            {
                audioSource.Stop();
                audioSource.PlayOneShot(fireProjectileSound);
            }
        }

        public void PlayMeleeSound()
        {
            if (audioSource != null && meleeSound != null)
            {
                audioSource.Stop();
                audioSource.PlayOneShot(meleeSound);
            }
        }

        public void PlaySlamSound()
        {
            if (audioSource != null && slamSound != null)
            {
                audioSource.Stop();
                audioSource.PlayOneShot(slamSound);
            }
        }
        
        public void PlayDashSound()
        {
            if (audioSource != null && dashSound != null)
            {
                audioSource.Stop();
                audioSource.PlayOneShot(dashSound);
            }
        }

    }