using UnityEngine;

public class Shockwave : MonoBehaviour
{
    [Header("Effect Parameters")] [Space]
    [SerializeField]
    [Tooltip("The object used to create the expanding shockwave effect.")] private GameObject _shockwaveEffectSphere;
    //[Tooltip("The camera impulse source to shake.")] public CinemachineImpulseSource _shockwaveCameraImpulseSource;
    [SerializeField] private float _cameraShakeIntensity;
    [SerializeField]
    [Tooltip("Distance in units from the player at which camera shake completely drops off.")] private float _cameraShakeDropoffDistance = 50;
    private Color _originalColor;

    void Start()
    {
        _shockwaveEffectSphere.SetActive(false);
        _shockwaveEffectSphere.transform.localScale = Vector3.zero;
        _originalColor = _shockwaveEffectSphere.GetComponent<Renderer>().material.color;
        //_shockwaveTimer = ShockwaveCooldown;
    }

    void Update()
    {
        
    }
}
