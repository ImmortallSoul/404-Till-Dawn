using UnityEngine;

public class StartMenu : MonoBehaviour
{
    const float VirtualW = 1280f;
    const float VirtualH = 720f;

    static readonly Color Gold = new Color(1f, 0.79f, 0.42f);
    static readonly Color Ice = new Color(0.55f, 0.78f, 1f);

    GUIStyle _title;
    GUIStyle _subtitle;
    GUIStyle _section;
    GUIStyle _text;
    GUIStyle _small;
    GUIStyle _hint;
    GUIStyle _rowName;
    GUIStyle _cardLetter;
    GUIStyle _cardName;
    GUIStyle _start;

    float _scale = -1f;
    float _ox;
    float _oy;

    void OnGUI()
    {
        var session = GameSession.Instance;
        if (session == null || session.State != RunState.Menu)
            return;

        Prepare();
        float t = Time.realtimeSinceStartup;

        MenuVisuals.DrawBackground(t);
        DrawHeader(t);

        DrawLocationPanel(R(54f, 138f, 330f, 420f));
        DrawHeroPanel(R(402f, 138f, 476f, 420f));
        DrawWeaponPanel(R(896f, 138f, 330f, 420f));
        DrawFooter(session, t);
    }

    void Prepare()
    {
        float s = Mathf.Clamp(Mathf.Min(Screen.width / VirtualW, Screen.height / VirtualH), 0.5f, 2.2f);
        if (Mathf.Abs(s - _scale) > 0.001f)
        {
            _scale = s;
            BuildStyles();
        }

        _ox = (Screen.width - VirtualW * _scale) * 0.5f;
        _oy = (Screen.height - VirtualH * _scale) * 0.5f;

        MenuVisuals.Ensure();
    }

    void BuildStyles()
    {
        int F(float size) { return Mathf.Max(8, Mathf.RoundToInt(size * _scale)); }

        _title = new GUIStyle(GUI.skin.label)
        {
            fontSize = F(42),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _title.normal.textColor = new Color(1f, 0.85f, 0.58f);

        _subtitle = new GUIStyle(GUI.skin.label)
        {
            fontSize = F(15),
            alignment = TextAnchor.MiddleCenter
        };
        _subtitle.normal.textColor = new Color(0.68f, 0.73f, 0.86f);

        _section = new GUIStyle(GUI.skin.label)
        {
            fontSize = F(14),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        _section.normal.textColor = Gold;

        _text = new GUIStyle(GUI.skin.label)
        {
            fontSize = F(13),
            alignment = TextAnchor.UpperLeft,
            wordWrap = true
        };
        _text.normal.textColor = new Color(0.82f, 0.85f, 0.93f);

        _small = new GUIStyle(GUI.skin.label)
        {
            fontSize = F(11),
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true
        };
        _small.normal.textColor = new Color(0.6f, 0.66f, 0.78f);

        _hint = new GUIStyle(GUI.skin.label)
        {
            fontSize = F(12),
            alignment = TextAnchor.MiddleCenter
        };
        _hint.normal.textColor = new Color(0.56f, 0.62f, 0.75f);

        _rowName = new GUIStyle(GUI.skin.label)
        {
            fontSize = F(15),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        _rowName.normal.textColor = Color.white;

        _cardLetter = new GUIStyle(GUI.skin.label)
        {
            fontSize = F(21),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _cardLetter.normal.textColor = Color.white;

        _cardName = new GUIStyle(GUI.skin.label)
        {
            fontSize = F(12),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        _cardName.normal.textColor = new Color(0.92f, 0.94f, 1f);

        _start = new GUIStyle(GUI.skin.label)
        {
            fontSize = F(24),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _start.normal.textColor = new Color(1f, 0.91f, 0.66f);
    }

    Rect R(float x, float y, float w, float h)
    {
        return new Rect(_ox + x * _scale, _oy + y * _scale, w * _scale, h * _scale);
    }

    Rect C(Rect parent, float x, float y, float w, float h)
    {
        return new Rect(parent.x + x * _scale, parent.y + y * _scale, w * _scale, h * _scale);
    }

    void DrawHeader(float t)
    {
        float pulse = MenuVisuals.Pulse(t, 1.6f);
        MenuVisuals.DrawGlow(R(390f, 4f, 500f, 118f), new Color(1f, 0.62f, 0.2f), 0.18f + pulse * 0.1f);

        GUI.Label(R(190f, 14f, 900f, 58f), "20 ХВИЛИН ДО СВІТАНКУ", _title);
        GUI.Label(R(240f, 72f, 800f, 26f), "Оберни героя, зброю та місце. Світанок настане через 20 хвилин.", _subtitle);

        Color line = new Color(Gold.r, Gold.g, Gold.b, 0.45f);
        MenuVisuals.DrawLine(R(250f, 112f, 300f, 1f), line);
        MenuVisuals.DrawLine(R(730f, 112f, 300f, 1f), line);
        MenuVisuals.DrawGlow(R(632f, 102f, 16f, 20f), Gold, 0.75f + pulse * 0.2f);
    }

    void DrawLocationPanel(Rect panel)
    {
        Color accent = new Color(0.4f, 0.8f, 0.55f);
        MenuVisuals.DrawPanel(panel, accent);
        Section(panel, "ЛОКАЦІЯ", accent);

        string[] names = { "Ліс", "Храм", "Гарбузове поле" };
        string[] descriptions =
        {
            "Туманний ліс. Рідкі дерева, камені й тиша між рядами.",
            "Давній храм. Колони, вівтарі й жовте світло руїн.",
            "Гарбузове поле. Могили, сухі дерева й помаранчевий туман."
        };
        Color[] colors =
        {
            new Color(0.35f, 0.85f, 0.45f),
            new Color(1f, 0.78f, 0.28f),
            new Color(1f, 0.45f, 0.16f)
        };

        for (int i = 0; i < names.Length; i++)
        {
            var map = (MapType)i;
            bool selected = GameSession.SelectedMap == map;
            Rect b = C(panel, 18f, 48f + i * 56f, 294f, 48f);

            if (MenuVisuals.DrawButton(b, selected, colors[i]))
            {
                Click();
                GameSession.SelectedMap = map;
                ArenaBuilder.Build(ArenaBuilder.Size, map);
            }

            MenuVisuals.DrawGlow(C(b, 10f, 8f, 32f, 32f), colors[i], selected ? 0.8f : 0.45f);
            GUI.Label(C(b, 52f, 6f, 232f, 36f), names[i], _rowName);
        }

        GUI.Label(C(panel, 20f, 218f, 290f, 44f), descriptions[(int)GameSession.SelectedMap], _small);
        MenuVisuals.DrawLine(C(panel, 18f, 270f, 294f, 1f), new Color(1f, 1f, 1f, 0.1f));

        Section(panel, "ТЕМРЯВА", Ice, 278f);

        for (int i = 0; i <= 3; i++)
        {
            int col = i % 2;
            int row = i / 2;
            bool selected = GameSession.SelectedDarkness == i;
            Rect b = C(panel, 18f + col * 151f, 312f + row * 42f, 143f, 34f);
            string label = i == 0 ? "Звичайна" : "Рівень " + i;

            if (MenuVisuals.DrawButton(b, selected, Ice))
            {
                Click();
                GameSession.SelectedDarkness = i;
                PrototypeSettings.Set(PrototypeSettings.Darkness, i);
                PrototypeSettings.Save();
            }

            GUI.Label(C(b, 10f, 0f, 123f, 34f), label, _small);
        }

        GUI.Label(C(panel, 20f, 396f, 290f, 20f), "Більше темряви — більше ворогів і XP", _small);
    }

    void DrawHeroPanel(Rect panel)
    {
        Color accent = new Color(0.75f, 0.5f, 1f);
        MenuVisuals.DrawPanel(panel, accent);
        Section(panel, "ГЕРОЙ", accent);

        var all = CharacterCatalog.All;
        float cardW = 82f;
        float gap = 8f;
        float startX = 17f;

        for (int i = 0; i < all.Length; i++)
        {
            var hero = all[i];
            bool selected = GameSession.SelectedCharacter == hero.Id;
            Rect b = C(panel, startX + i * (cardW + gap), 48f, cardW, 94f);

            if (MenuVisuals.DrawButton(b, selected, hero.Color))
            {
                Click();
                GameSession.SelectedCharacter = hero.Id;
            }

            MenuVisuals.DrawGlow(C(b, 24f, 8f, 34f, 34f), hero.Color, selected ? 0.9f : 0.5f);
            GUI.Label(C(b, 24f, 8f, 34f, 34f), hero.Name.Substring(0, 1), _cardLetter);
            GUI.Label(C(b, 5f, 46f, cardW - 10f, 36f), hero.Name, _cardName);
        }

        var current = CharacterCatalog.Get(GameSession.SelectedCharacter);

        GUI.Label(C(panel, 18f, 152f, 440f, 20f), "ПАСИВКА:", _small);
        GUI.Label(C(panel, 18f, 172f, 440f, 60f), current.Description, _text);
        MenuVisuals.DrawLine(C(panel, 18f, 234f, 440f, 1f), new Color(1f, 1f, 1f, 0.1f));

        GUI.Label(C(panel, 18f, 242f, 440f, 24f), "ЗДІБНІСТЬ (ПКМ): " + current.SpecialName, _section);
        GUI.Label(C(panel, 18f, 268f, 440f, 48f), current.SpecialDescription, _text);

        MenuVisuals.DrawLine(C(panel, 18f, 318f, 440f, 8f), new Color(0.06f, 0.07f, 0.12f, 0.9f));
        MenuVisuals.DrawLine(C(panel, 18f, 318f, 440f * Mathf.Clamp01(current.MaxHp / 7f), 8f), current.Color);

        GUI.Label(C(panel, 18f, 332f, 200f, 22f), "HP: " + current.MaxHp.ToString("0"), _small);
        GUI.Label(C(panel, 240f, 332f, 218f, 22f), "Стиль: " + HeroStyle(current.Id), _small);

        GUI.Label(C(panel, 18f, 360f, 440f, 46f),
            "Порада: вивчай картки дзеркала й часового зсуву — вони відкривають нові можливості.", _small);
    }

    void DrawWeaponPanel(Rect panel)
    {
        Color accent = new Color(1f, 0.68f, 0.24f);
        MenuVisuals.DrawPanel(panel, accent);
        Section(panel, "СТАРТОВА ЗБРОЯ", accent);

        GUI.Label(C(panel, 18f, 40f, 294f, 22f), "Усі отримані зброї стріляють разом", _small);

        var all = WeaponCatalog.All;
        float cardW = 140f;
        float cardH = 38f;

        for (int i = 0; i < all.Length; i++)
        {
            int col = i % 2;
            int row = i / 2;
            bool selected = GameSession.SelectedWeapon == all[i].Id;
            Color c = WeaponColor(i);
            Rect b = C(panel, 18f + col * 148f, 72f + row * 46f, cardW, cardH);

            if (MenuVisuals.DrawButton(b, selected, c))
            {
                Click();
                GameSession.SelectedWeapon = all[i].Id;
            }

            MenuVisuals.DrawGlow(C(b, 8f, 10f, 18f, 18f), c, selected ? 0.85f : 0.42f);
            GUI.Label(C(b, 32f, 0f, cardW - 38f, cardH), all[i].Title, _small);
        }

        int index = IndexOfWeapon(GameSession.SelectedWeapon);
        GUI.Label(C(panel, 18f, 354f, 294f, 20f), all[index].Title.ToUpper(), _section);
        GUI.Label(C(panel, 18f, 376f, 294f, 38f), all[index].BaseDescription, _small);
    }

    void DrawFooter(GameSession session, float t)
    {
        Rect panel = R(54f, 576f, 1172f, 124f);
        MenuVisuals.DrawPanel(panel, Gold);

        string summary = session.MapName + "  ·  " + session.CharacterName + "  ·  " + session.WeaponName;
        GUI.Label(C(panel, 20f, 14f, 330f, 26f), "ОБРАНИЙ ВИБІР", _section);
        GUI.Label(C(panel, 20f, 40f, 330f, 22f), summary, _small);

        Rect start = C(panel, 386f, 10f, 400f, 58f);
        MenuVisuals.DrawGlow(start, Gold, 0.16f + MenuVisuals.Pulse(t, 2.2f) * 0.16f);
        if (MenuVisuals.DrawButton(start, true, Gold))
        {
            Click();
            session.BeginRun();
        }
        GUI.Label(start, "ПОЧАТИ ГРУ", _start);

        GUI.Label(C(panel, 20f, 80f, 1132f, 18f),
            "WASD — рух   ·   ЛКМ — стрілянина   ·   ПКМ — здатність   ·   1–4 — підсилення   ·   R — перемішування карток", _hint);
        GUI.Label(C(panel, 20f, 100f, 1132f, 18f),
            "Скіли вивчаються картками: Q — дзеркало заднього виду   ·   E — часовий зсув", _hint);

        GUI.Label(C(panel, 830f, 14f, 320f, 26f), "00:00  →  20:00", _section);
        GUI.Label(C(panel, 830f, 40f, 320f, 22f), "Витримай до світанку", _small);
    }

    static void Click()
    {
        AudioManager.Instance?.Play("ui_click", 0.55f, Random.Range(0.96f, 1.06f), 0.04f);
    }

    void Section(Rect panel, string text, Color accent, float y = 12f)
    {
        GUI.Label(C(panel, 18f, y, 260f, 24f), text, _section);
        MenuVisuals.DrawLine(C(panel, 18f, y + 25f, 34f, 2f), accent);
    }

    static string HeroStyle(CharacterId id)
    {
        switch (id)
        {
            case CharacterId.Diamond: return "Танк";
            case CharacterId.Scarlett: return "Полум'я";
            case CharacterId.Spark: return "Блискавка";
            case CharacterId.Lilith: return "Призив";
            default: return "Швидкість";
        }
    }

    static Color WeaponColor(int index)
    {
        Color[] palette =
        {
            new Color(1f, 0.82f, 0.4f),
            new Color(1f, 0.5f, 0.25f),
            new Color(0.5f, 0.9f, 0.5f),
            new Color(0.5f, 0.75f, 1f),
            new Color(0.85f, 0.4f, 1f),
            new Color(1f, 0.4f, 0.15f),
            new Color(0.6f, 1f, 0.4f),
            new Color(0.4f, 0.85f, 1f),
            new Color(0.8f, 0.9f, 1f),
            new Color(0.4f, 1f, 0.6f),
            new Color(0.6f, 0.75f, 1f)
        };
        return palette[index % palette.Length];
    }

    static int IndexOfWeapon(WeaponId id)
    {
        for (int i = 0; i < WeaponCatalog.All.Length; i++)
        {
            if (WeaponCatalog.All[i].Id == id)
                return i;
        }
        return 0;
    }
}
