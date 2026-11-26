using UnityEngine;

/// <summary>
/// Налаштування, які зберігаються між запусками. Використовуються паузою,
/// аудіоменеджером і first-person контролером.
/// </summary>
public static class PrototypeSettings
{
    public const string SfxVolume = "proto_sfx_volume";
    public const string UiVolume = "proto_ui_volume";
    public const string MusicVolume = "proto_music_volume";
    public const string MouseSensitivity = "proto_mouse_sensitivity";
    public const string InvertY = "proto_invert_y";
    public const string Darkness = "proto_darkness";

    public static float Get(string key, float fallback)
    {
        return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetFloat(key) : fallback;
    }

    public static int Get(string key, int fallback)
    {
        return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : fallback;
    }

    public static bool Get(string key, bool fallback)
    {
        return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) != 0 : fallback;
    }

    public static void Set(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
    }

    public static void Set(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
    }

    public static void Set(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }
}
