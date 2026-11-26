using UnityEngine;

/// <summary>
/// Пауза на F1: налаштування гучності, чутливості, інверсії Y, темряви
/// та вихід у головне меню з підтвердженням.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    GUIStyle _title;
    GUIStyle _section;
    GUIStyle _label;
    GUIStyle _value;
    GUIStyle _hint;
    GUIStyle _buttonText;

    bool _confirmExit;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        var session = GameSession.Instance;
        if (session == null)
            return;

        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (session.State == RunState.Playing)
            {
                _confirmExit = false;
                session.BeginPause();
                AudioManager.Instance?.Play("extra_ui", 0.55f, 1f, 0f);
            }
            else if (session.State == RunState.Paused)
            {
                Close();
            }
        }

        if (session.State != RunState.Paused)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_confirmExit)
                _confirmExit = false;
            else
                Close();
        }
    }

    void Close()
    {
        _confirmExit = false;
        var session = GameSession.Instance;
        if (session != null)
            session.EndPause();
        AudioManager.Instance?.Play("extra_ui", 0.45f, 0.92f, 0f);
    }

    void BuildStyles()
    {
        if (_title != null)
            return;

        _title = new GUIStyle(GUI.skin.label)
        {
            fontSize = 34,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        _title.normal.textColor = new Color(1f, 0.82f, 0.42f);

        _section = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold
        };
        _section.normal.textColor = new Color(0.62f, 0.82f, 1f);

        _label = new GUIStyle(GUI.skin.label) { fontSize = 14 };
        _label.normal.textColor = new Color(0.86f, 0.88f, 0.94f);

        _value = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleRight
        };
        _value.normal.textColor = new Color(1f, 0.9f, 0.62f);

        _hint = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            wordWrap = true
        };
        _hint.normal.textColor = new Color(0.74f, 0.78f, 0.86f);

        _buttonText = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };
        _buttonText.normal.textColor = Color.white;
    }

    void OnGUI()
    {
        var session = GameSession.Instance;
        if (session == null || session.State != RunState.Paused)
            return;

        BuildStyles();
        MenuVisuals.Ensure();

        GUI.color = new Color(0f, 0f, 0f, 0.84f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float width = Mathf.Min(680f, Screen.width - 40f);
        float height = Mathf.Min(660f, Screen.height - 30f);
        Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        MenuVisuals.DrawPanel(panel, new Color(0.35f, 0.65f, 1f));

        GUI.Label(new Rect(panel.x, panel.y + 16f, panel.width, 42f), "ПАУЗА", _title);
        GUI.Label(new Rect(panel.x, panel.y + 56f, panel.width, 22f),
            "F1 — продовжити · Esc — закрити", _hint);

        float x = panel.x + 32f;
        float w = panel.width - 64f;
        float y = panel.y + 92f;

        Section(x, ref y, w, "ГУЧНІСТЬ");
        AudioManager audio = AudioManager.Instance;
        if (audio != null)
        {
            y = Slider(x, ref y, w, "Ефекти", audio.SfxVolume, v =>
            {
                audio.SetVolumes(v, audio.UiVolume, audio.MusicVolume);
            }, v => Mathf.RoundToInt(v * 100f) + "%");

            y = Slider(x, ref y, w, "Інтерфейс", audio.UiVolume, v =>
            {
                audio.SetVolumes(audio.SfxVolume, v, audio.MusicVolume);
            }, v => Mathf.RoundToInt(v * 100f) + "%");

            y = Slider(x, ref y, w, "Музика", audio.MusicVolume, v =>
            {
                audio.SetVolumes(audio.SfxVolume, audio.UiVolume, v);
            }, v => Mathf.RoundToInt(v * 100f) + "%");
        }

        y += 6f;
        Section(x, ref y, w, "КЕРУВАННЯ");
        var fps = FindAnyObjectByType<FirstPersonController>();
        y = Slider(x, ref y, w, "Чутливість миші", fps != null ? fps.MouseSensitivity : 2.2f, v =>
        {
            if (fps != null)
                fps.MouseSensitivity = v;
            PrototypeSettings.Set(PrototypeSettings.MouseSensitivity, v);
            PrototypeSettings.Save();
        }, v => v.ToString("0.0"), 0.2f, 6f);

        if (MenuVisuals.DrawButton(new Rect(x, y, w, 32f), fps != null && fps.InvertY,
                new Color(0.35f, 0.65f, 1f)))
        {
            bool invert = fps == null || !fps.InvertY;
            if (fps != null)
                fps.InvertY = invert;
            PrototypeSettings.Set(PrototypeSettings.InvertY, invert);
            PrototypeSettings.Save();
            AudioManager.Instance?.Play("ui_click", 0.75f, 1f, 0f);
        }
        GUI.Label(new Rect(x + 12f, y, w - 24f, 32f),
            "Інверсія осі Y: " + (fps != null && fps.InvertY ? "УВІМКНЕНО" : "ВИМКНЕНО"), _label);
        y += 40f;

        y += 6f;
        Section(x, ref y, w, "СКЛАДНІСТЬ");
        y = Slider(x, ref y, w, "Темрява (з наступного запуску)", GameSession.SelectedDarkness,
            v =>
            {
                GameSession.SelectedDarkness = Mathf.Clamp(Mathf.RoundToInt(v), 0, 3);
                PrototypeSettings.Set(PrototypeSettings.Darkness, GameSession.SelectedDarkness);
                PrototypeSettings.Save();
            }, v => GameSession.SelectedDarkness.ToString());

        y += 6f;
        Section(x, ref y, w, "ПІДКАЗКИ");
        GUI.Label(new Rect(x, y, w, 112f),
            "WASD — рух · Миша — огляд · ЛКМ — стріляти (автовогонь)\n" +
            "1–4 / колесо — вибір слота зброї · R — рерол карт на рівні\n" +
            "Q — дзеркало заднього виду · E — зсув часу (після апгрейду)\n" +
            "F1 — пауза та налаштування · Esc — звільнити курсор",
            _hint);
        y += 112f;

        if (!_confirmExit)
        {
            if (GUI.Button(new Rect(x, panel.yMax - 92f, w, 34f), "Вийти в головне меню", _buttonText))
            {
                _confirmExit = true;
                AudioManager.Instance?.Play("extra_ui", 0.55f, 1f, 0f);
            }
            if (GUI.Button(new Rect(x, panel.yMax - 50f, w, 34f), "Продовжити гру", _buttonText))
                Close();
        }
        else
        {
            GUI.Label(new Rect(x, panel.yMax - 128f, w, 28f),
                "Точно вийти? Поточний забіг не буде збережено.", _value);
            if (GUI.Button(new Rect(x, panel.yMax - 92f, w * 0.48f, 34f), "Так, вийти", _buttonText))
                session.ReturnToMenu();
            if (GUI.Button(new Rect(x + w * 0.52f, panel.yMax - 92f, w * 0.48f, 34f), "Скасувати", _buttonText))
            {
                _confirmExit = false;
                AudioManager.Instance?.Play("extra_ui", 0.45f, 0.92f, 0f);
            }
        }
    }

    void Section(float x, ref float y, float w, string text)
    {
        GUI.Label(new Rect(x, y, w, 24f), text, _section);
        y += 26f;
    }

    float Slider(float x, ref float y, float w, string label, float value,
        System.Action<float> changed, System.Func<float, string> format,
        float min = 0f, float max = 1f)
    {
        GUI.Label(new Rect(x, y, w * 0.62f, 22f), label, _label);
        GUI.Label(new Rect(x + w * 0.62f, y, w * 0.38f, 22f), format(value), _value);
        y += 22f;

        float next = GUI.HorizontalSlider(new Rect(x, y, w, 20f), value, min, max);
        y += 28f;
        if (Mathf.Abs(next - value) > 0.0001f)
            changed(next);
        return y;
    }

    float Slider(float x, ref float y, float w, string label, int value,
        System.Action<float> changed, System.Func<float, string> format)
    {
        GUI.Label(new Rect(x, y, w * 0.62f, 22f), label, _label);
        GUI.Label(new Rect(x + w * 0.62f, y, w * 0.38f, 22f), format(value), _value);
        y += 22f;

        float next = GUI.HorizontalSlider(new Rect(x, y, w, 20f), value, 0f, 3f);
        y += 28f;
        if (Mathf.Abs(next - value) > 0.0001f)
            changed(next);
        return y;
    }
}
