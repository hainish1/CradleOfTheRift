using System.IO;
using UnityEngine;

public class SettingsService
{
    private readonly string masterVolumeRTPC;
    private readonly string musicVolumeRTPC;
    private readonly string sfxVolumeRTPC;
    private readonly string ambientVolumeRTPC;

    private static string SavePath =>
        Path.Combine(Application.persistentDataPath, "settings.json");

    public SettingsData Current { get; private set; } = new SettingsData();
    private SettingsData snapshot;

    public SettingsService(string masterRTPC, string musicRTPC,
                           string sfxRTPC,   string ambientRTPC)
    {
        masterVolumeRTPC  = masterRTPC;
        musicVolumeRTPC   = musicRTPC;
        sfxVolumeRTPC     = sfxRTPC;
        ambientVolumeRTPC = ambientRTPC;
    }

    public void Load()
    {
        if (File.Exists(SavePath))
            Current = JsonUtility.FromJson<SettingsData>(File.ReadAllText(SavePath));
        else
        {
            Debug.LogWarning("SettingsService: No saved settings found, using defaults.");
            Current = new SettingsData();
            Save();
        }

        // Snapshot the state at open-time so Revert can return here
        TakeSnapshot();
        Apply(Current);
    }

    public void RevertToSnapshot()
    {
        Apply(new SettingsData
        {
            masterVolume  = snapshot.masterVolume,
            musicVolume   = snapshot.musicVolume,
            sfxVolume     = snapshot.sfxVolume,
            ambientVolume = snapshot.ambientVolume,
        });
    }

    private void TakeSnapshot()
    {
        snapshot = new SettingsData
        {
            masterVolume  = Current.masterVolume,
            musicVolume   = Current.musicVolume,
            sfxVolume     = Current.sfxVolume,
            ambientVolume = Current.ambientVolume,
        };
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(Current, prettyPrint: true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"SettingsService: Saved to {SavePath}\n{File.ReadAllText(SavePath)}");
        Debug.Log("SettingsService: Applied audio settings to JSON and Wwise.\n" +
                  $"Master={Current.masterVolume}, Music={Current.musicVolume}, SFX={Current.sfxVolume}, Ambient={Current.ambientVolume}");
    }

    /// <summary>
    /// Stores data as Current and pushes all values to Wwise immediately.
    /// Called on every slider change for real-time audio feedback.
    /// </summary>
    public void Apply(SettingsData data)
    {
        Current = data;

        AkUnitySoundEngine.SetRTPCValue(masterVolumeRTPC,  data.masterVolume);
        AkUnitySoundEngine.SetRTPCValue(musicVolumeRTPC,   data.musicVolume);
        AkUnitySoundEngine.SetRTPCValue(sfxVolumeRTPC,     data.sfxVolume);
        AkUnitySoundEngine.SetRTPCValue(ambientVolumeRTPC, data.ambientVolume);
    }

    public void RevertToDefaults() => Apply(new SettingsData());
}