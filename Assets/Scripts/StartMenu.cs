using System;
using UnityEngine;

/// <summary>
/// Мінімалістичне головне меню: одна центрована колонка, плоскі чіпи,
/// єдиний акцент — золотий колір обраного елемента.
/// </summary>
public class StartMenu : MonoBehaviour
{
    const float VirtualW = 1280f;
    const float VirtualH = 720f;

    static readonly Color Gold = new Color(0.91f, 0.73f, 0.42f);

    GUIStyle _title;
    GUIStyle _subtitle;
    GUIStyle _section;
    GUIStyle _chip;
    GUIStyle _chipSelected;
    GUIStyle _start;
    GUIStyle _hint;

    float _scale = -1f;
    float _ox;
    float _oy;

    void OnGUI()
    {
        var session = GameSession.Instance;
        if (session == null || session.State != RunState.Menu)
            return;

        Prepare();

        MenuVisuals.DrawMinimalBackground();

        GUI.Label(R(340f, 44f, 600f, 54f), "404 TILL DAWN", _title);
        GUI.Label(R(360f, 98f, 560f, 24f), "20 ХВИЛИН ДО СВІТАНКУ", _subtitle);
        MenuVisuals.DrawLine(R(540f, 134f, 200f, 1f), new Color(Gold.r, Gold.g, Gold.b, 0.35f));

        DrawRow("ЛОКАЦІЯ", 166f, LocationNames(), (int)GameSession.SelectedMap, 200f, 10f, i =>
        {
            GameSession.SelectedMap = (MapType)i;
            ArenaBuilder.Build(ArenaBuilder.Size, (MapType)i);
        });

        DrawRow("ГЕРОЙ", 246f, HeroNames(), HeroIndex(), 132f, 8f, i =>
        {
            GameSession.SelectedCharacter = CharacterCatalog.All[i].Id;
        });

        DrawRow("ЗБРОЯ", 326f, WeaponNames(), WeaponIndex(), 92f, 6f, i =>
        {
            GameSession.SelectedWeapon = WeaponCatalog.All[i].Id;
        });

        DrawRow("ТЕМРЯВА", 406f, DarknessNames(), GameSession.SelectedDarkness, 140f, 8f, i =>
        {
            GameSession.SelectedDarkness = i;
            PrototypeSettings.Set(PrototypeSettings.Darkness, i);
            PrototypeSettings.Save();
        });

        Rect start = R(500f, 498f, 280f, 52f);
        if (MenuVisuals.DrawMinimalButton(start, true, Gold))
        {
            Click();
            session.BeginRun();
        }
        GUI.Label(start, "ПОЧАТИ ГРУ", _start);

        GUI.Label(R(340f, 566f, 600f, 24f), "WASD · ЛКМ · ПКМ · 1–4 · Q · E · F1", _hint);
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
            fontSize = F(38),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _title.normal.textColor = new Color(0.91f, 0.92f, 0.96f);

        _subtitle = new GUIStyle(GUI.skin.label)
        {
            fontSize = F(13),
            alignment = TextAnchor.MiddleCenter
        };
        _subtitle.normal.textColor = new Color(0.55f, 0.59f, 0.68f);

        _section = new GUIStyle(GUI.skin.label)
        {
            fontSize = F(12),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        _section.normal.textColor = new Color(0.62f, 0.66f, 0.75f);

        _chip = new GUIStyle(GUI.skin.label)
        {
            fontSize = F(12),
            alignment = TextAnchor.MiddleCenter
        };
        _chip.normal.textColor = new Color(0.72f, 0.76f, 0.84f);

        _chipSelected = new GUIStyle(_chip)
        {
            fontStyle = FontStyle.Bold
        };
        _chipSelected.normal.textColor = Gold;

        _start = new GUIStyle(GUI.skin.label)
        {
            fontSize = F(18),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _start.normal.textColor = Gold;

        _hint = new GUIStyle(GUI.skin.label)
        {
            fontSize = F(11),
            alignment = TextAnchor.MiddleCenter
        };
        _hint.normal.textColor = new Color(0.45f, 0.49f, 0.57f);
    }

    Rect R(float x, float y, float w, float h)
    {
        return new Rect(_ox + x * _scale, _oy + y * _scale, w * _scale, h * _scale);
    }

    void DrawRow(string label, float y, string[] items, int selected,
        float chipWidth, float gap, Action<int> onSelect)
    {
        GUI.Label(R((VirtualW - 600f) * 0.5f, y, 600f, 20f), label, _section);

        float total = items.Length * chipWidth + (items.Length - 1) * gap;
        float x = (VirtualW - total) * 0.5f;

        for (int i = 0; i < items.Length; i++)
        {
            Rect b = R(x + i * (chipWidth + gap), y + 26f, chipWidth, 40f);
            bool isSelected = i == selected;

            if (MenuVisuals.DrawMinimalButton(b, isSelected, Gold))
            {
                Click();
                onSelect(i);
            }

            GUI.Label(b, items[i], isSelected ? _chipSelected : _chip);
        }
    }

    static string[] LocationNames()
    {
        return new[] { "Ліс", "Храм", "Гарбузове поле" };
    }

    static string[] HeroNames()
    {
        var all = CharacterCatalog.All;
        var names = new string[all.Length];
        for (int i = 0; i < all.Length; i++)
            names[i] = all[i].Name;
        return names;
    }

    static string[] WeaponNames()
    {
        var all = WeaponCatalog.All;
        var names = new string[all.Length];
        for (int i = 0; i < all.Length; i++)
            names[i] = all[i].Title;
        return names;
    }

    static string[] DarknessNames()
    {
        var names = new string[4];
        for (int i = 0; i < names.Length; i++)
            names[i] = i == 0 ? "Звичайна" : "Рівень " + i;
        return names;
    }

    static int HeroIndex()
    {
        var all = CharacterCatalog.All;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].Id == GameSession.SelectedCharacter)
                return i;
        }
        return 0;
    }

    static int WeaponIndex()
    {
        var all = WeaponCatalog.All;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].Id == GameSession.SelectedWeapon)
                return i;
        }
        return 0;
    }

    static void Click()
    {
        AudioManager.Instance?.Play("ui_click", 0.55f, UnityEngine.Random.Range(0.96f, 1.06f), 0.04f);
    }
}
