using UnityEngine;

public class SettingsBootstrapper : MonoBehaviour
{
    [Header("Wwise RTPC Names")]
    [SerializeField] private string masterVolumeRTPC  = "MasterVolume";
    [SerializeField] private string musicVolumeRTPC   = "MusicVolume";
    [SerializeField] private string sfxVolumeRTPC     = "SFXVolume";
    [SerializeField] private string ambientVolumeRTPC = "AmbientVolume";

    private void Start()
    {
        // Create a temporary service to load the JSON and push values to Wwise at game start
        SettingsService initService = new SettingsService(
            masterVolumeRTPC, 
            musicVolumeRTPC, 
            sfxVolumeRTPC, 
            ambientVolumeRTPC
        );
        
        initService.Load();
    }
}