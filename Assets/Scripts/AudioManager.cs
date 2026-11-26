using System.Collections.Generic;
using UnityEngine;

public enum MusicState
{
    None,
    Menu,
    Run,
    FinalMinute,
    Dawn
}

/// <summary>
/// Простий аудіо-менеджер із трьома групами: музика, ефекти та UI.
/// Використовує готові CC0-кліпи Kenney з Assets/Resources/Audio.
/// </summary>
public sealed class AudioManager : MonoBehaviour
{
    const int SfxVoices = 14;
    const int UiVoices = 4;

    static AudioManager _instance;

    readonly Dictionary<string, AudioClip[]> _clips = new Dictionary<string, AudioClip[]>();
    readonly Dictionary<string, float> _lastPlayed = new Dictionary<string, float>();
    readonly List<AudioSource> _sfx = new List<AudioSource>();
    readonly List<AudioSource> _ui = new List<AudioSource>();
    AudioSource _loop;

    AudioSource _musicA;
    AudioSource _musicB;
    bool _musicOnA = true;
    MusicState _state = MusicState.None;
    float _nextMusicAt;
    int _musicStep;
    int _sfxCursor;
    bool _paused;

    public float SfxVolume = 0.82f;
    public float UiVolume = 0.7f;
    public float MusicVolume = 0.42f;

    public static AudioManager Instance { get { return _instance; } }
    public MusicState State { get { return _state; } }

    public static void Ensure()
    {
        if (_instance != null)
            return;
        var go = new GameObject("AudioManager");
        go.AddComponent<AudioManager>();
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        SfxVolume = Mathf.Clamp01(PrototypeSettings.Get(PrototypeSettings.SfxVolume, 0.82f));
        UiVolume = Mathf.Clamp01(PrototypeSettings.Get(PrototypeSettings.UiVolume, 0.7f));
        MusicVolume = Mathf.Clamp01(PrototypeSettings.Get(PrototypeSettings.MusicVolume, 0.42f));
        LoadClips();
        BuildVoices();
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    public void SetVolumes(float sfx, float ui, float music)
    {
        SfxVolume = Mathf.Clamp01(sfx);
        UiVolume = Mathf.Clamp01(ui);
        MusicVolume = Mathf.Clamp01(music);
        PrototypeSettings.Set(PrototypeSettings.SfxVolume, SfxVolume);
        PrototypeSettings.Set(PrototypeSettings.UiVolume, UiVolume);
        PrototypeSettings.Set(PrototypeSettings.MusicVolume, MusicVolume);
        PrototypeSettings.Save();
        ApplyVolumes();
    }

    public void SetPaused(bool paused)
    {
        _paused = paused;
        ApplyVolumes();
    }

    void ApplyVolumes()
    {
        for (int i = 0; i < _sfx.Count; i++)
            _sfx[i].volume = SfxVolume;
        for (int i = 0; i < _ui.Count; i++)
            _ui[i].volume = UiVolume;
        if (_loop != null)
            _loop.volume = SfxVolume;
        if (_musicA != null)
            _musicA.volume = MusicVolume * (_paused ? 0.35f : 1f);
        if (_musicB != null)
            _musicB.volume = MusicVolume * (_paused ? 0.35f : 1f);
    }

    void LoadClips()
    {
        var sfx = Resources.LoadAll<AudioClip>("Audio/Sfx");
        var music = Resources.LoadAll<AudioClip>("Audio/Music");

        AddGroup("shot", sfx, "shot_", "shot_retro_");
        AddGroup("reload_start", sfx, "reload_start_");
        AddGroup("reload_end", sfx, "reload_end_");
        AddGroup("enemy_hit", sfx, "enemy_hit_");
        AddGroup("enemy_death", sfx, "enemy_death_");
        AddGroup("xp", sfx, "xp_");
        AddGroup("card_select", sfx, "card_select");
        AddGroup("summon_spawn", sfx, "summon_spawn_");
        AddGroup("summon_attack", sfx, "summon_attack");
        AddGroup("boss_spawn", sfx, "boss_spawn");
        AddGroup("footstep", sfx, "footstep_");
        AddGroup("hurt", sfx, "hurt");
        AddGroup("heal", sfx, "heal");
        AddGroup("phased", sfx, "phased");
        AddGroup("ui_click", sfx, "ui_click");
        AddGroup("ui_back", sfx, "ui_back");

        // Кожен ствол має власний CC0-звук, а не один спільний "shot".
        // М'які варіанти з HomkaPlay обрані замість різкіших Kenney-кліпів:
        // ті самі назви груп, але префікс _soft_ ігнорує старі .ogg.
        AddGroup("gun_revolver", sfx, "gun_revolver_soft_");
        AddGroup("gun_shotgun", sfx, "gun_shotgun_soft_");
        AddGroup("gun_smg", sfx, "gun_smg_soft_");
        AddGroup("gun_crossbow", sfx, "gun_crossbow_soft_");
        AddGroup("gun_bat", sfx, "gun_bat_soft_");
        AddGroup("gun_grenade", sfx, "gun_grenade_soft_");
        AddGroup("gun_staff", sfx, "gun_staff_soft_");
        AddGroup("gun_knife", sfx, "gun_knife_soft_");
        AddGroup("gun_water", sfx, "gun_water_soft_");
        AddGroup("gun_sword", sfx, "gun_sword_soft_");
        AddGroup("flame_loop", sfx, "flame_loop");

        // Додаткові безкоштовні кліпи HomkaPlay — окремі групи, без заміни
        // жодного з 11 фірмових звуків зброї.
        AddGroup("extra_ui", sfx, "extra_ui_");
        AddGroup("extra_magic", sfx, "extra_magic_");
        AddGroup("extra_nature", sfx, "extra_nature_");
        AddGroup("extra_step", sfx, "extra_step_");

        AddGroup("level_up", music, "dawn_2", "final_1");
        AddGroup("dawn", music, "dawn_0", "dawn_1");
        AddGroup("music_menu", music, "menu_");
        AddGroup("music_run", music, "run_");
        AddGroup("music_final", music, "final_");
        AddGroup("music_dawn", music, "dawn_0", "dawn_1", "dawn_2");

        Debug.Log("[AudioManager] clips loaded: sfx=" + sfx.Length + " music=" + music.Length +
                  " groups=" + _clips.Count);
    }

    static void AddGroup(string name, AudioClip[] source, params string[] prefixes)
    {
        var matches = new List<AudioClip>();
        for (int i = 0; i < source.Length; i++)
        {
            var clip = source[i];
            if (clip == null)
                continue;
            for (int p = 0; p < prefixes.Length; p++)
            {
                if (clip.name.StartsWith(prefixes[p], System.StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(clip);
                    break;
                }
            }
        }
        if (matches.Count > 0)
            matches.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        // The array is cached: repeated cues never allocate.
        if (!name.StartsWith("music_", System.StringComparison.Ordinal))
        {
            for (int i = 0; i < matches.Count; i++)
                Debug.Log("[AudioManager] cue " + name + " <- " + matches[i].name);
        }
        // ReSharper disable once CollectionNeverQueried.Local
        _instance._clips[name] = matches.Count == 0 ? new AudioClip[0] : matches.ToArray();
    }

    void BuildVoices()
    {
        _musicA = CreateVoice("MusicA", MusicVolume, true);
        _musicB = CreateVoice("MusicB", MusicVolume, true);

        for (int i = 0; i < SfxVoices; i++)
            _sfx.Add(CreateVoice("Sfx_" + i, SfxVolume, false));
        for (int i = 0; i < UiVoices; i++)
            _ui.Add(CreateVoice("Ui_" + i, UiVolume, false));
        _loop = CreateVoice("Loop", SfxVolume, true);
    }

    static AudioSource CreateVoice(string name, float volume, bool loopable)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_instance.transform, false);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loopable;
        source.spatialBlend = 0f;
        source.volume = volume;
        source.priority = loopable ? 200 : 100;
        return source;
    }

    public void Play(string cue, float volume = 1f, float pitch = 1f, float minGap = 0f)
    {
        if (_instance == null || _clips.Count == 0)
            return;

        AudioClip[] group;
        if (!_clips.TryGetValue(cue, out group) || group.Length == 0)
            return;

        float now = Time.unscaledTime;
        float last;
        if (_lastPlayed.TryGetValue(cue, out last) && now - last < minGap)
            return;

        var clip = group[Random.Range(0, group.Length)];
        bool isUi = cue == "ui_click" || cue == "ui_back" || cue == "card_select";
        var pool = isUi ? _ui : _sfx;
        var source = NextVoice(pool, isUi ? UiVolume : SfxVolume);
        if (source == null)
            return;

        source.volume = (isUi ? UiVolume : SfxVolume) * Mathf.Clamp01(volume);
        source.pitch = Mathf.Clamp(pitch, 0.5f, 1.8f);
        source.PlayOneShot(clip, 1f);
        _lastPlayed[cue] = now;
    }

    /// <summary>
    /// Безперервний звук для струї вогню. Не перезапускає кліп, якщо він уже грає.
    /// </summary>
    public void SetLoop(string cue, bool enabled, float volume = 1f)
    {
        if (_loop == null || _clips.Count == 0)
            return;

        if (!enabled)
        {
            if (_loop.isPlaying)
                _loop.Stop();
            return;
        }

        AudioClip[] group;
        if (!_clips.TryGetValue(cue, out group) || group.Length == 0)
            return;

        var clip = group[0];
        if (_loop.clip != clip || !_loop.isPlaying)
        {
            _loop.clip = clip;
            _loop.loop = true;
            _loop.Play();
        }
        _loop.volume = SfxVolume * Mathf.Clamp01(volume);
    }

    static AudioSource NextVoice(List<AudioSource> pool, float volume)
    {
        if (pool.Count == 0)
            return null;

        int start = _instance._sfxCursor++;
        for (int i = 0; i < pool.Count; i++)
        {
            var source = pool[(start + i) % pool.Count];
            if (!source.isPlaying)
                return source;
        }
        return pool[start % pool.Count];
    }

    public void SetState(MusicState state)
    {
        if (_state == state)
            return;

        _state = state;
        _musicStep = 0;
        _nextMusicAt = Time.unscaledTime + 0.35f;
        if (_musicA != null) _musicA.Stop();
        if (_musicB != null) _musicB.Stop();

        Debug.Log("[AudioManager] music state = " + state);
    }

    void Update()
    {
        if (_state == MusicState.None || _musicA == null)
            return;
        if (Time.unscaledTime < _nextMusicAt)
            return;

        string group;
        float gap;
        switch (_state)
        {
            case MusicState.Menu:
                group = "music_menu"; gap = 7.5f; break;
            case MusicState.FinalMinute:
                group = "music_final"; gap = 4.8f; break;
            case MusicState.Dawn:
                group = "music_dawn"; gap = 6.5f; break;
            case MusicState.Run:
            default:
                group = "music_run"; gap = 8.5f; break;
        }

        AudioClip[] clips;
        if (!_clips.TryGetValue(group, out clips) || clips.Length == 0)
        {
            _nextMusicAt = Time.unscaledTime + 6f;
            return;
        }

        var source = _musicOnA ? _musicA : _musicB;
        _musicOnA = !_musicOnA;
        source.clip = clips[_musicStep % clips.Length];
        source.volume = MusicVolume;
        source.pitch = _state == MusicState.FinalMinute ? 1.08f : 1f;
        source.Play();
        _musicStep++;
        _nextMusicAt = Time.unscaledTime + gap;
    }
}
