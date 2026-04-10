using UnityEngine;

public class GlobalSettingsManager : MonoBehaviour
{
    public static GlobalSettingsManager Instance { get; private set; }

    [Header("Wwise RTPC Names")]
    [SerializeField] private string masterVolumeRTPC  = "MasterVolume";
    [SerializeField] private string musicVolumeRTPC   = "MusicVolume";
    [SerializeField] private string sfxVolumeRTPC     = "SFXVolume";
    [SerializeField] private string ambientVolumeRTPC = "AmbientVolume";

    public SettingsService Service { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize the service once
        Service = new SettingsService(masterVolumeRTPC, musicVolumeRTPC, 
                                      sfxVolumeRTPC, ambientVolumeRTPC);
        Service.Load();
    }
}