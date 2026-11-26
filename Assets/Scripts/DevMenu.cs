#if UNITY_EDITOR
using System;
using UnityEngine;

/// <summary>
/// Меню розробника на F12. Дозволяє видати будь-яку зброю або здатність
/// у будь-який момент, не чекаючи картки апгрейду.
/// </summary>
public sealed class DevMenu : MonoBehaviour
{
    static readonly WeaponId[] Weapons = (WeaponId[])Enum.GetValues(typeof(WeaponId));
    const int Columns = 4;

    bool _open;
    int _tab;
    Vector2 _scroll;
    GUIStyle _button;
    GUIStyle _label;

    void Update()
    {
        var session = GameSession.Instance;
        if (session == null || !session.AllowsGameplay)
            return;
        if (Input.GetKeyDown(KeyCode.F12))
            _open = !_open;
    }

    void OnGUI()
    {
        if (!_open)
            return;
        var session = GameSession.Instance;
        if (session == null || !session.AllowsGameplay)
            return;

        BuildStyles();
        float width = Mathf.Min(920f, Screen.width - 40f);
        float height = Mathf.Min(620f, Screen.height - 40f);
        Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        GUI.color = new Color(0.04f, 0.05f, 0.09f, 0.96f);
        GUI.DrawTexture(panel, RuntimeTextureLibrary.Panel);
        GUI.color = Color.white;

        GUILayout.BeginArea(new Rect(panel.x + 18f, panel.y + 14f, panel.width - 36f, panel.height - 28f));
        GUILayout.Label("МЕНЮ РОЗРОБНИКА — F12", _label);
        GUILayout.Space(6f);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Зброя", _button)) _tab = 0;
        if (GUILayout.Button("Здатності", _button)) _tab = 1;
        if (GUILayout.Button("Вороги", _button)) _tab = 2;
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Закрити", _button)) _open = false;
        GUILayout.EndHorizontal();
        GUILayout.Space(8f);

        var combat = FindAnyObjectByType<PlayerCombat>();
        var stats = FindAnyObjectByType<PlayerStats>();
        _scroll = GUILayout.BeginScrollView(_scroll);
        if (_tab == 0)
        {
            GUILayout.Label("Клік — видати або підвищити рівень зброї", _label);
            for (int i = 0; i < Weapons.Length; i += Columns)
            {
                GUILayout.BeginHorizontal();
                for (int c = 0; c < Columns && i + c < Weapons.Length; c++)
                {
                    WeaponId id = Weapons[i + c];
                    if (GUILayout.Button(id.ToString(), _button))
                        combat?.DevGrantWeapon(id);
                }
                GUILayout.EndHorizontal();
            }
        }
        else if (_tab == 1)
        {
            GUILayout.Label("Клік — активувати здатність", _label);
            if (GUILayout.Button("Здатність персонажа (ПКМ)", _button))
                combat?.DevTriggerSpecial();
            if (GUILayout.Button("Дзеркало заднього виду: увімкнути", _button))
                RearViewMirror.Instance?.DevSetActive(true);
            if (GUILayout.Button("Дзеркало заднього виду: вимкнути", _button))
                RearViewMirror.Instance?.DevSetActive(false);
            if (GUILayout.Button("Часовий зсув (E): увімкнути", _button))
                FindAnyObjectByType<TimeDisplacement>()?.DevTrigger();
            if (GUILayout.Button("Часовий зсув: завершити", _button))
                FindAnyObjectByType<TimeDisplacement>()?.DevForceEnd();
            if (GUILayout.Button("+1 рівень", _button))
                stats?.AddXp(stats.Level * 10 + 10);
            if (GUILayout.Button("Відновити HP", _button))
                stats?.Heal(99f);
        }
        else
        {
            GUILayout.Label("Клік — заспавнити ворога", _label);
            var kinds = (EnemyKind[])Enum.GetValues(typeof(EnemyKind));
            for (int i = 0; i < kinds.Length; i += Columns)
            {
                GUILayout.BeginHorizontal();
                for (int c = 0; c < Columns && i + c < kinds.Length; c++)
                {
                    EnemyKind kind = kinds[i + c];
                    if (GUILayout.Button(kind.ToString(), _button))
                        SpawnEnemy(kind);
                }
                GUILayout.EndHorizontal();
            }
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    static void SpawnEnemy(EnemyKind kind)
    {
        var player = GameSession.Instance?.Player;
        if (player == null)
            return;
        Vector3 dir = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f) * Vector3.forward;
        Vector3 pos = player.transform.position + dir * 8f;
        Enemy.Spawn(pos, kind, EnemyTier.Small);
    }

    void BuildStyles()
    {
        if (_button != null)
            return;
        _button = new GUIStyle(GUI.skin.button) { fontSize = 12, fixedHeight = 28f };
        _label = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true };
    }
}
#endif
