using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// States from the CC0 KayKit Character Animations pack, driven on a legacy
/// Animation component so imported FBX clips work without editor-side import
/// settings. Aliases keep gameplay code independent from exact clip names.
/// </summary>
public sealed class CharacterAnimationController : MonoBehaviour
{
    const string ClipFolder = "Animations/Rig_Medium";
    const string FabClipFolder = "Animations/FabSkeleton";

    static AnimationClip[] _clips;
    static bool _clipsLoaded;
    static string _loadedFolder;
    static readonly string[] AliasNames =
    {
        "Spawn", "Idle", "Walk", "Run", "Attack", "Hit", "Death", "Reload", "Shoot", "Magic",
        "RollForward", "RollBackward", "RollLeft", "RollRight"
    };

    Animation _animation;
    readonly Dictionary<string, string> _aliases = new Dictionary<string, string>();
    string _current;

    public string CurrentState { get { return _current; } }

    public static CharacterAnimationController Attach(GameObject root, string profile = "melee")
    {
        if (root == null)
            return null;
        // Готові тварини (Dragon/Bat/Butterfly) мають власні матеріали й рух задає
        // SummonManager — накладати KayKit-кліпи на їхні кістики не потрібно.
        if (string.Equals(profile, "none", System.StringComparison.OrdinalIgnoreCase))
            return null;

        bool fab = string.Equals(profile, "fab", System.StringComparison.OrdinalIgnoreCase);
        EnsureClips(fab);
        var animation = root.GetComponent<Animation>();
        if (animation == null)
            animation = root.AddComponent<Animation>();

        var controller = root.GetComponent<CharacterAnimationController>();
        if (controller == null)
            controller = root.AddComponent<CharacterAnimationController>();
        controller._animation = animation;
        controller._aliases.Clear();
        controller._current = null;

        if (fab)
        {
            // Fab Stylized Low Poly Skeleton: власні кліпи з тією ж самою
            // ієрархією кісток, без спроби застосувати KayKit-анімації.
            controller.Map("Spawn", "Skeleton_spawn", false);
            controller.Map("Idle", "Skeleton_idle", true);
            controller.Map("Walk", "Skeleton_walk_forward", true);
            controller.Map("Run", "Skeleton_run_forward", true);
            controller.Map("Hit", "Skeleton_take_damage", false);
            controller.Map("Death", "Skeleton_death", false);
            controller.Map("Reload", "Skeleton_throw_projectiles", false);
            controller.Map("Attack", "Skeleton_slash01", false);
            controller.Map("Shoot", "Skeleton_throw_projectiles", false);
            controller.Map("Magic", "Skeleton_slash02", false);
        }
        else
        {
            controller.Map("Spawn", "Spawn_Ground", false);
            controller.Map("Idle", "Idle_A", true);
            controller.Map("Walk", "Walking_A", true);
            controller.Map("Run", "Running_A", true);
            controller.Map("Hit", "Hit_A", false);
            controller.Map("Death", "Death_A", false);
            controller.Map("Reload", "Ranged_1H_Reload", false);

            // CC0 KayKit MovementAdvanced: чотири напрямки перекату.
            controller.Map("RollForward", "Dodge_Forward", false);
            controller.Map("RollBackward", "Dodge_Backward", false);
            controller.Map("RollLeft", "Dodge_Left", false);
            controller.Map("RollRight", "Dodge_Right", false);

            if (profile == "ranged")
                controller.Map("Attack", "Ranged_1H_Shoot", false);
            else if (profile == "magic")
                controller.Map("Attack", "Ranged_Magic_Summon", false);
            else
                controller.Map("Attack", "Melee_Unarmed_Attack_Punch_A", false);

            controller.Map("Shoot", "Ranged_1H_Shoot", false);
            controller.Map("Magic", "Ranged_Magic_Spellcasting", false);
        }

        controller.SetState("Spawn", 0f);
        return controller;
    }

    static void EnsureClips(bool fab = false)
    {
        string folder = fab ? FabClipFolder : ClipFolder;
        if (_clipsLoaded && _loadedFolder == folder)
            return;
        _clipsLoaded = true;
        _loadedFolder = folder;
        _clips = Resources.LoadAll<AnimationClip>(folder);
        var names = new List<string>();
        for (int i = 0; i < _clips.Length; i++)
            names.Add(_clips[i].name);
        Debug.Log("[Animations] Rig_Medium clips=" + _clips.Length + ": " + string.Join(", ", names.ToArray()));
    }

    public static int LoadedClipCount
    {
        get { EnsureClips(); return _clips == null ? 0 : _clips.Length; }
    }

    void Map(string alias, string preferredClip, bool loop)
    {
        var clip = FindClip(preferredClip);
        if (clip == null)
            return;

        clip.legacy = true;
        clip.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
        _animation.AddClip(clip, alias);
        _aliases[alias] = alias;
    }

    static AnimationClip FindClip(string preferred)
    {
        if (_clips == null)
            return null;

        for (int i = 0; i < _clips.Length; i++)
        {
            if (string.Equals(_clips[i].name, preferred, System.StringComparison.OrdinalIgnoreCase))
                return _clips[i];
        }

        string token = preferred.Length > 6 ? preferred.Substring(0, 6) : preferred;
        for (int i = 0; i < _clips.Length; i++)
        {
            if (_clips[i].name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return _clips[i];
        }
        return null;
    }

    /// <summary>
    /// Spawn -> Idle -> Walk/Run -> Attack/Hit -> Death. Missing clips are ignored
    /// and the previous state keeps playing, so the game never stalls.
    /// </summary>
    public void SetState(string state, float fade = 0.16f)
    {
        SetState(state, fade, 1f);
    }

    public void SetState(string state, float fade, float speed)
    {
        if (_animation == null || string.IsNullOrEmpty(state) || state == _current)
            return;

        string alias = ResolveAlias(state);
        if (string.IsNullOrEmpty(alias))
            return;

        _current = alias;
        var clipState = _animation[alias];
        if (clipState != null)
            clipState.speed = Mathf.Max(0.01f, speed);

        if (fade <= 0f)
            _animation.Play(alias, PlayMode.StopSameLayer);
        else
            _animation.CrossFade(alias, fade);
    }

    /// <summary>Тривалість кліпу в секундах — використовується для синхронізації перекату.</summary>
    public float ClipLength(string state)
    {
        string alias = ResolveAlias(state);
        if (string.IsNullOrEmpty(alias) || _animation == null)
            return 0f;
        var clip = _animation.GetClip(alias);
        return clip != null ? clip.length : 0f;
    }

    string ResolveAlias(string state)
    {
        string alias;
        if (_aliases.TryGetValue(state, out alias))
            return alias;

        for (int i = 0; i < AliasNames.Length; i++)
        {
            if (string.Equals(AliasNames[i], state, System.StringComparison.OrdinalIgnoreCase))
            {
                alias = AliasNames[i];
                return _aliases.ContainsKey(alias) ? alias : null;
            }
        }
        return null;
    }
}
