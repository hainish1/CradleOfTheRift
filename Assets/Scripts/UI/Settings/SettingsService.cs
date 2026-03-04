using System.IO;
using UnityEngine;

/// <summary>
/// Handles all settings business logic: loading from disk, saving to disk,
/// applying values to Wwise, and reverting to the open-time snapshot.
/// </summary>
public class SettingsService
{
    private readonly string masterVolumeRTPC;
    private readonly string musicVolumeRTPC;
    private readonly string sfxVolumeRTPC;
    private readonly string ambientVolumeRTPC;

    private static string SavePath =>
        Path.Combine(Application.persistentDataPath, "settings.json");

   // The live settings object. Page controllers mutate this on slider change.
    public SettingsData Current { get; private set; } = new SettingsData();

    // Captured once in Load(). RevertToSnapshot() restores to this state.
    private SettingsData snapshot;

    public SettingsService(string masterRTPC, string musicRTPC,
                           string sfxRTPC,   string ambientRTPC)
    {
        masterVolumeRTPC  = masterRTPC;
        musicVolumeRTPC   = musicRTPC;
        sfxVolumeRTPC     = sfxRTPC;
        ambientVolumeRTPC = ambientRTPC;
    }

    /// <summary>
    /// Reads settings from disk (or writes defaults if none exist), takes an
    /// open-time snapshot for revert, then applies values to Wwise immediately.
    /// </summary>
    public void Load()
    {
        if (File.Exists(SavePath)) {
            Current = JsonUtility.FromJson<SettingsData>(File.ReadAllText(SavePath));
            // Rebuild the runtime binding-overrides dictionary from the
            // serialized parallel lists (JsonUtility can't handle Dictionary).
            Current.BuildOverrideDictionary();
        }
        else
        {
            Debug.LogWarning("SettingsService: No saved settings found, using defaults.");
            Current = new SettingsData();
            Current.BuildOverrideDictionary();
            Save();
        }

        // Snapshot the state at open-time so Revert can return here
        TakeSnapshot();
        Apply(Current);
    }

    /// <summary>
    /// Serializes Current to disk. Called by SettingsMenuController.OnDisable
    /// so changes are persisted when the menu closes.
    /// </summary>
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

    /// <summary>
    /// Restores Current to the snapshot taken when the menu was opened,
    /// discarding any unsaved changes made this session.
    /// </summary>
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

    // Unused method, reverts all settings to default
    public void RevertToDefaults() => Apply(new SettingsData());
}