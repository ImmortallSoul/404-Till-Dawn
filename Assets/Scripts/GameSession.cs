using UnityEngine;
using UnityEngine.SceneManagement;

public enum RunState
{
    Menu,
    Playing,
    Paused,
    LevelUp,
    Dead,
    Won
}

public class GameSession : MonoBehaviour
{
    public const float RunDuration = 20f * 60f;

    public static GameSession Instance { get; private set; }

    public static CharacterId SelectedCharacter = CharacterId.Shana;
    public static WeaponId SelectedWeapon = WeaponId.Revolver;
    public static MapType SelectedMap = MapType.Forest;
    public static int Darkness = 0;
    public static int SelectedDarkness = 0;

    public RunState State { get; private set; } = RunState.Menu;
    public float Elapsed { get; private set; }
    public float Remaining { get { return Mathf.Max(0f, RunDuration - Elapsed); } }
    public float Progress { get { return Mathf.Clamp01(Elapsed / RunDuration); } }
    public int Kills { get; private set; }
    public bool AllowsGameplay { get { return State == RunState.Playing; } }

    public string Notice { get; private set; }
    public float NoticeUntil { get; private set; }
    bool _finalMinuteMusic;
    float _nextAmbienceAt;

    public PlayerStats Player { get; private set; }

    public string MapName
    {
        get
        {
            switch (SelectedMap)
            {
                case MapType.Temple: return "Храм";
                case MapType.PumpkinPatch: return "Гарбузове поле";
                default: return "Ліс";
            }
        }
    }

    public string CharacterName { get { return CharacterCatalog.Get(SelectedCharacter).Name; } }
    public string WeaponName { get { return WeaponCatalog.Get(SelectedWeapon).Title; } }

    void Awake()
    {
        Instance = this;
        SelectedDarkness = Mathf.Clamp(PrototypeSettings.Get(PrototypeSettings.Darkness, 0), 0, 3);
        Darkness = SelectedDarkness;
        Enemy.ResetAlive();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        Time.timeScale = 1f;
    }

    public void BeginRun()
    {
        if (State != RunState.Menu)
            return;

        State = RunState.Playing;
        Darkness = Mathf.Clamp(SelectedDarkness, 0, 3);
        Elapsed = 0f;
        Kills = 0;
        _finalMinuteMusic = false;
        AudioManager.Instance?.SetState(MusicState.Run);

        var menuCamera = GameObject.Find("MenuCamera");
        if (menuCamera != null)
            Destroy(menuCamera);

        var controller = FirstPersonController.Spawn(new Vector3(0f, 0.05f, 0f));
        BindPlayer(controller.GetComponent<PlayerStats>());

        SetCursor(true);
        ShowNotice(CharacterName + " · " + WeaponName + " · " + MapName, 3.5f);
    }

    public void BindPlayer(PlayerStats player)
    {
        Player = player;
    }

    public void RegisterKill()
    {
        Kills++;
    }

    public void ShowNotice(string text, float seconds = 3f)
    {
        Notice = text;
        NoticeUntil = Time.unscaledTime + seconds;
    }

    public void BeginLevelUp()
    {
        if (State != RunState.Playing)
            return;

        State = RunState.LevelUp;
        Time.timeScale = 0f;
        SetCursor(false);
    }

    public void EndLevelUp()
    {
        if (State != RunState.LevelUp)
            return;

        State = RunState.Playing;
        Time.timeScale = 1f;
        SetCursor(true);
    }

    public void BeginPause()
    {
        if (State != RunState.Playing)
            return;

        State = RunState.Paused;
        Time.timeScale = 0f;
        SetCursor(false);
        AudioManager.Instance?.SetPaused(true);
    }

    public void EndPause()
    {
        if (State != RunState.Paused)
            return;

        State = RunState.Playing;
        Time.timeScale = 1f;
        SetCursor(true);
        AudioManager.Instance?.SetPaused(false);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        AudioManager.Instance?.SetPaused(false);
        Restart();
    }

    public void PlayerDied()
    {
        State = RunState.Dead;
        Time.timeScale = 0f;
        SetCursor(false);
        AudioManager.Instance?.SetState(MusicState.None);
        AudioManager.Instance?.Play("hurt", 0.9f, 0.85f, 0f);
    }

    void Update()
    {
        if (State == RunState.Playing)
        {
            Elapsed += Time.deltaTime;
            if (!_finalMinuteMusic && Remaining <= 60f)
            {
                _finalMinuteMusic = true;
                AudioManager.Instance?.SetState(MusicState.FinalMinute);
                ShowNotice("ОСТАННЯ ХВИЛИНА ДО СВІТАНКУ", 4f);
            }
            if (Time.time >= _nextAmbienceAt)
            {
                _nextAmbienceAt = Time.time + Random.Range(14f, 28f);
                AudioManager.Instance?.Play("extra_nature", 0.16f, Random.Range(0.9f, 1.1f), 0.25f);
            }
            if (Elapsed >= RunDuration)
                Win();
        }

        if ((State == RunState.Dead || State == RunState.Won) && Input.GetKeyDown(KeyCode.R))
            Restart();
    }

    void Win()
    {
        State = RunState.Won;
        Time.timeScale = 0f;
        SetCursor(false);
        AudioManager.Instance?.SetState(MusicState.Dawn);
        AudioManager.Instance?.Play("dawn", 0.9f, 1f, 0f);
    }

    public static void Restart()
    {
        Time.timeScale = 1f;
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex >= 0 ? scene.name : scene.path);
    }

    public static void SetCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
