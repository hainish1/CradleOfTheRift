using System;

[Serializable]
public class SettingsData
{
    public int masterVolume  = 100;
    public int musicVolume   = 75;
    public int sfxVolume     = 100;
    public int ambientVolume = 50;

    public bool Equals(SettingsData other)
    {
        if (other == null) return false;
        return masterVolume  == other.masterVolume  &&
               musicVolume   == other.musicVolume   &&
               sfxVolume     == other.sfxVolume     &&
               ambientVolume == other.ambientVolume;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(masterVolume, musicVolume, sfxVolume, ambientVolume);
    }
}