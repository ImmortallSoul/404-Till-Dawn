using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Малі службові пункти меню для прототипу: швидкий доступ до кредитів
/// та перевірка наявності основних CC0-ресурсів без запуску гри.
/// </summary>
public static class PrototypeMenu
{
    [MenuItem("Tools/Show Asset Credits")]
    static void ShowCredits()
    {
        string path = Path.Combine(Application.dataPath, "Credits", "ASSET_CREDITS.md");
        if (File.Exists(path))
            EditorUtility.RevealInFinder(path);
        else
            Debug.LogWarning("[PrototypeMenu] ASSET_CREDITS.md not found.");
    }

    [MenuItem("Tools/Check Prototype Assets")]
    static void CheckAssets()
    {
        int sfx = Resources.LoadAll<AudioClip>("Audio/Sfx").Length;
        int particles = Resources.LoadAll<Texture2D>("Vfx/Particles").Length;
        int weapons = Resources.LoadAll<GameObject>("Weapons").Length;
        int hands = Resources.Load<GameObject>("ViewModels/Hands/WradArms/WradArms") != null ? 1 : 0;
        Debug.Log("[PrototypeMenu] weapons=" + weapons + " sfx=" + sfx +
                  " particles=" + particles + " handPack=" + hands);
    }
}
