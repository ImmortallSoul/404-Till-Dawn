using System.Collections.Generic;
using UnityEngine;

public class GameHud : MonoBehaviour
{
    static float _lastShot;
    static float _lastHit;
    static float _lastKill;
    static float _bloom;

    GUIStyle _timer;
    GUIStyle _label;
    GUIStyle _small;
    GUIStyle _center;
    GUIStyle _weapon;
    Texture2D _panel;
    Texture2D _glow;
    Texture2D _vignette;

    public static void NotifyShot()
    {
        _lastShot = Time.unscaledTime;
        _bloom = Mathf.Min(1f, _bloom + 0.32f);
    }

    public static void NotifyHit(bool critical, bool killed)
    {
        _lastHit = Time.unscaledTime;
        _bloom = Mathf.Min(1f, _bloom + (critical ? 0.18f : 0.09f));
        if (killed)
            _lastKill = _lastHit;
    }

    void BuildStyles()
    {
        if (_timer != null)
            return;

        _panel = RuntimeTextureLibrary.Panel;
        _glow = RuntimeTextureLibrary.Glow;
        _vignette = BuildVignette();

        _timer = new GUIStyle(GUI.skin.label)
        {
            fontSize = 38,
            alignment = TextAnchor.UpperCenter,
            fontStyle = FontStyle.Bold
        };
        _timer.normal.textColor = Color.white;

        _label = new GUIStyle(GUI.skin.label) { fontSize = 16 };
        _label.normal.textColor = Color.white;

        _small = new GUIStyle(GUI.skin.label) { fontSize = 12 };
        _small.normal.textColor = new Color(0.8f, 0.82f, 0.88f);

        _center = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };
        _center.normal.textColor = Color.white;

        _weapon = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
        _weapon.normal.textColor = new Color(0.95f, 0.92f, 0.82f);
    }

    void OnGUI()
    {
        var session = GameSession.Instance;
        if (session == null || session.State == RunState.Menu)
            return;

        BuildStyles();
        _bloom = Mathf.MoveTowards(_bloom, 0f, Time.unscaledDeltaTime * 2.2f);

        var player = session.Player;
        if (player != null && player.IsPhased)
            DrawPhaseOverlay();
        if (player != null && player.DamageFlash > 0.01f)
            DrawDamageOverlay(player);

        bool dawn = session.Remaining <= 90f;
        _timer.normal.textColor = dawn
            ? new Color(1f, 0.35f + (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f)) * 0.25f, 0.3f)
            : Color.white;

        int total = Mathf.CeilToInt(session.Remaining);
        GUI.Label(new Rect(0f, 12f, Screen.width, 44f), string.Format("{0:00}:{1:00}", total / 60, total % 60), _timer);

        if (player != null)
        {
            DrawBar(18f, 18f, 240f, 18f, player.Hp01, new Color(0.78f, 0.14f, 0.18f),
                "HP " + player.Hp.ToString("0.0") + " / " + player.MaxHp.ToString("0.0"));

            float xp01 = player.XpToNext <= 0 ? 0f : (float)player.Xp / player.XpToNext;
            DrawBar(18f, 44f, 240f, 13f, xp01, new Color(0.25f, 0.75f, 0.95f), "LV " + player.Level);

            GUI.Label(new Rect(18f, 64f, 280f, 20f),
                "Убито " + session.Kills + "   Кристалів " + player.Gems + "   Загроз " + Enemy.Alive, _small);
            GUI.Label(new Rect(18f, 84f, 300f, 20f),
                session.CharacterName + " · " + session.MapName + " · Темрява " + GameSession.Darkness, _small);
        }

        DrawBossBar();
        DrawWeapons(player);
        DrawRearView(player);
        DrawTimeDisplacement(player);
        DrawSpecial(player);
        DrawOffscreenThreats(player);
        DrawCrosshair();

        if (!string.IsNullOrEmpty(session.Notice) && Time.unscaledTime < session.NoticeUntil)
            GUI.Label(new Rect(0f, Screen.height * 0.2f, Screen.width, 42f), session.Notice, _center);

        if (session.State == RunState.Dead)
            Overlay("ТИ ПАВ ДО СВІТАНКУ\nR — почати заново");
        else if (session.State == RunState.Won)
            Overlay("СВІТАНОК. ТИ ВИЖИВ 20 ХВИЛИН\nR — ще раз");
    }

    static Texture2D BuildVignette()
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "DamageVignette",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.SmoothStep(0.35f, 1.05f, d);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    void DrawDamageOverlay(PlayerStats player)
    {
        float flash = Mathf.Clamp01(player.DamageFlash);
        GUI.color = new Color(0.72f, 0.05f, 0.07f, flash * 0.72f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _vignette);

        if (player.DamageDirection.sqrMagnitude < 0.01f)
        {
            GUI.color = Color.white;
            return;
        }

        var cam = Camera.main;
        if (cam == null)
        {
            GUI.color = Color.white;
            return;
        }

        Vector3 local = cam.transform.InverseTransformDirection(player.DamageDirection);
        float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
        float radius = Mathf.Min(Screen.width, Screen.height) * 0.36f;
        float cx = Screen.width * 0.5f + Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
        float cy = Screen.height * 0.5f - Mathf.Cos(angle * Mathf.Deg2Rad) * radius;

        Matrix4x4 old = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle + 180f, new Vector2(cx, cy));
        GUI.color = new Color(1f, 0.16f, 0.14f, flash);
        GUI.Label(new Rect(cx - 14f, cy - 18f, 28f, 36f), "▲", _center);
        GUI.matrix = old;
        GUI.color = Color.white;
    }

    void DrawBossBar()
    {
        var boss = Enemy.PrimaryBoss;
        if (boss == null)
            return;

        float w = Mathf.Min(520f, Screen.width - 120f);
        float x = (Screen.width - w) * 0.5f;
        float y = 66f;

        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(new Rect(x, y, w, 16f), Texture2D.whiteTexture);
        GUI.color = new Color(0.85f, 0.12f, 0.2f);
        GUI.DrawTexture(new Rect(x, y, w * boss.Health01, 16f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(x, y - 20f, w, 20f), boss.Title, _center);

        if (boss.IsTelegraphing)
        {
            float a = 0.18f + boss.Telegraph01 * 0.28f;
            GUI.color = new Color(1f, 0.2f, 0.12f, a);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, 5f), _glow, ScaleMode.StretchToFill);
            GUI.DrawTexture(new Rect(0f, Screen.height - 5f, Screen.width, 5f), _glow, ScaleMode.StretchToFill);
            GUI.DrawTexture(new Rect(0f, 0f, 5f, Screen.height), _glow, ScaleMode.StretchToFill);
            GUI.DrawTexture(new Rect(Screen.width - 5f, 0f, 5f, Screen.height), _glow, ScaleMode.StretchToFill);
            GUI.color = Color.white;
        }
    }

    void DrawWeapons(PlayerStats player)
    {
        if (player == null)
            return;
        var combat = player.GetComponent<PlayerCombat>();
        if (combat == null || combat.Weapons.Count == 0)
            return;

        float y = Screen.height - 26f - combat.Weapons.Count * 26f;
        for (int i = 0; i < combat.Weapons.Count; i++)
        {
            var weapon = combat.Weapons[i];
            string ammo = weapon.IsReloading ? "ПЕРЕЗАРЯДКА" : weapon.Ammo + "/" + weapon.MaxAmmo;
            bool active = combat.ActiveWeaponIndex == i;

            if (active)
            {
                GUI.color = new Color(0.2f, 0.55f, 0.8f, 0.28f);
                GUI.DrawTexture(new Rect(12f, y - 2f, 340f, 24f), _panel, ScaleMode.StretchToFill);
                GUI.color = Color.white;
            }

            GUI.Label(new Rect(18f, y, 330f, 20f),
                (active ? "▸ " : "  ") + (i + 1) + "  " + weapon.DisplayName + "   " + ammo, _weapon);

            float barW = 120f;
            float bx = 18f;
            float by = y + 19f;
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(new Rect(bx, by, barW, 3f), Texture2D.whiteTexture);
            GUI.color = weapon.IsReloading ? new Color(1f, 0.55f, 0.2f) : new Color(0.7f, 0.85f, 1f);
            float fill = weapon.IsReloading ? weapon.ReloadProgress : (weapon.MaxAmmo <= 0 ? 0f : (float)weapon.Ammo / weapon.MaxAmmo);
            GUI.DrawTexture(new Rect(bx, by, barW * Mathf.Clamp01(fill), 3f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            y += 26f;
        }
    }

    void DrawRearView(PlayerStats player)
    {
        var mirror = RearViewMirror.Instance;
        if (player == null || !player.RearViewMirror || mirror == null || mirror.Texture == null || !mirror.IsActive)
            return;

        float w = Mathf.Min(360f, Screen.width * 0.26f);
        float h = w / mirror.Aspect;
        float x = Screen.width - w - 18f;
        float y = 62f;
        var rect = new Rect(x, y, w, h);

        GUI.color = new Color(0f, 0f, 0f, 0.78f);
        GUI.DrawTexture(new Rect(x - 4f, y - 4f, w + 8f, h + 8f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.DrawTexture(rect, mirror.Texture, ScaleMode.ScaleToFit, false);

        GUI.color = new Color(0.45f, 0.85f, 1f, 0.9f);
        GUI.DrawTexture(new Rect(x, y, w, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y + h - 2f, w, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y, 2f, h), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x + w - 2f, y, 2f, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        var style = new GUIStyle(_small) { alignment = TextAnchor.MiddleRight };
        style.normal.textColor = new Color(0.55f, 0.88f, 1f);
        GUI.Label(new Rect(x, y + h + 4f, w, 18f), "ДЗЕРКАЛО ЗАДНЬОГО ВИДУ · Q", style);
    }

    void DrawPhaseOverlay()
    {
        GUI.color = new Color(0.22f, 0.14f, 0.5f, 0.26f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

        GUI.color = new Color(0.6f, 0.85f, 1f, 0.75f);
        for (int y = 0; y < Screen.height; y += 6)
            GUI.DrawTexture(new Rect(0f, y, Screen.width, 1f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        var style = new GUIStyle(_center) { fontSize = 20 };
        style.normal.textColor = new Color(0.8f, 0.75f, 1f);
        GUI.Label(new Rect(0f, Screen.height * 0.13f, Screen.width, 30f),
            "ПАРАЛЕЛЬНИЙ ВИМІР · E — ПОВЕРНУТИСЯ", style);
    }

    void DrawTimeDisplacement(PlayerStats player)
    {
        var shift = player.GetComponent<TimeDisplacement>();
        if (shift == null || !player.TimeDisplacement)
            return;

        float w = 210f;
        float x = Screen.width - w - 18f;
        float y = Screen.height - 82f;

        GUI.Label(new Rect(x, y - 20f, w, 18f), "E — ЧАСОВИЙ ЗСУВ", _small);
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(x, y, w, 12f), Texture2D.whiteTexture);

        float fill = shift.IsActive ? shift.Active01 : shift.Cooldown01;
        GUI.color = shift.IsActive ? new Color(0.75f, 0.45f, 1f) : new Color(0.45f, 0.6f, 0.95f);
        GUI.DrawTexture(new Rect(x, y, w * fill, 12f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        string text = shift.IsActive ? shift.Remaining.ToString("0.0") + "с"
                    : shift.CooldownRemaining > 0f ? shift.CooldownRemaining.ToString("0.0") + "с" : "ГОТОВИЙ";
        var mini = new GUIStyle(_small) { alignment = TextAnchor.MiddleRight };
        mini.normal.textColor = Color.white;
        GUI.Label(new Rect(x, y - 1f, w, 14f), text, mini);
    }

    void DrawSpecial(PlayerStats player)
    {
        if (player == null)
            return;
        var combat = player.GetComponent<PlayerCombat>();
        if (combat == null)
            return;

        float w = 210f;
        float x = Screen.width - w - 18f;
        float y = Screen.height - 52f;

        string skill = CharacterCatalog.Get(GameSession.SelectedCharacter).SpecialName;
        GUI.Label(new Rect(x, y - 22f, w, 20f), "ПКМ — " + skill, _small);
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(x, y, w, 14f), Texture2D.whiteTexture);
        GUI.color = combat.SpecialRemaining <= 0f ? new Color(0.45f, 0.95f, 0.6f) : new Color(0.5f, 0.65f, 0.95f);
        GUI.DrawTexture(new Rect(x, y, w * combat.Special01, 14f), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void DrawOffscreenThreats(PlayerStats player)
    {
        if (player == null || !GameSession.Instance.AllowsGameplay)
            return;

        var camera = Camera.main;
        if (camera == null)
            return;

        var threats = Enemy.NearestEnemies(player.transform.position, 12, 52f);
        int drawn = 0;
        for (int i = 0; i < threats.Count && drawn < 6; i++)
        {
            var enemy = threats[i];
            if (enemy == null)
                continue;

            Vector3 world = enemy.transform.position + Vector3.up * 1.1f;
            Vector3 local = camera.transform.InverseTransformPoint(world);
            Vector2 dir = new Vector2(local.x, local.y);
            Vector3 screen = camera.WorldToScreenPoint(world);
            float x = screen.x;
            float y = Screen.height - screen.y;
            bool onScreen = screen.z > 0f && x > 46f && x < Screen.width - 46f && y > 96f && y < Screen.height - 92f;
            if (onScreen)
                continue;

            if (dir.sqrMagnitude < 0.01f)
                dir = Vector2.up;
            dir.Normalize();

            float halfW = Screen.width * 0.5f - 46f;
            float halfH = Screen.height * 0.5f - 96f;
            float t = Mathf.Min(
                Mathf.Abs(dir.x) > 0.001f ? halfW / Mathf.Abs(dir.x) : 99999f,
                Mathf.Abs(dir.y) > 0.001f ? halfH / Mathf.Abs(dir.y) : 99999f);
            Vector2 point = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) + dir * t;

            Color color = enemy.Tier == EnemyTier.Boss ? new Color(1f, 0.25f, 0.2f)
                        : enemy.IsTelegraphing ? new Color(1f, 0.62f, 0.2f)
                        : new Color(1f, 0.85f, 0.45f);

            DrawThreatArrow(point, dir, color, enemy.Tier == EnemyTier.Boss ? "БОС" : "");
            drawn++;
        }
    }

    static void DrawThreatArrow(Vector2 point, Vector2 dir, Color color, string label)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Matrix4x4 previous = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, point);

        GUI.color = color;
        GUI.DrawTexture(new Rect(point.x - 11f, point.y - 2f, 15f, 4f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(point.x + 1f, point.y - 8f, 4f, 16f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(point.x - 4f, point.y - 4f, 7f, 8f), Texture2D.whiteTexture);
        GUI.matrix = previous;

        if (!string.IsNullOrEmpty(label))
        {
            var style = new GUIStyle(_smallLabel) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            style.normal.textColor = color;
            GUI.Label(new Rect(point.x - 26f, point.y - 28f, 52f, 18f), label, style);
        }

        GUI.color = Color.white;
    }

    static GUIStyle _smallLabel
    {
        get
        {
            if (_cachedSmallLabel == null)
            {
                _cachedSmallLabel = new GUIStyle(GUI.skin.label) { fontSize = 11 };
                _cachedSmallLabel.normal.textColor = Color.white;
            }
            return _cachedSmallLabel;
        }
    }
    static GUIStyle _cachedSmallLabel;

    void DrawBar(float x, float y, float w, float h, float fill, Color color, string text)
    {
        GUI.color = new Color(1f, 1f, 1f, 0.55f);
        GUI.DrawTexture(new Rect(x - 3f, y - 3f, w + 6f, h + 6f), _panel, ScaleMode.StretchToFill);
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = color;
        GUI.DrawTexture(new Rect(x, y, w * Mathf.Clamp01(fill), h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        var mini = new GUIStyle(_label) { fontSize = 12, alignment = TextAnchor.MiddleLeft };
        mini.normal.textColor = Color.white;
        GUI.Label(new Rect(x + 6f, y - 1f, w, h), text, mini);
    }

    static void DrawCrosshair()
    {
        var session = GameSession.Instance;
        if (session == null || !session.AllowsGameplay)
            return;

        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;
        float gap = 6f + _bloom * 12f;
        float len = 8f + _bloom * 5f;

        GUI.color = new Color(1f, 0.9f, 0.55f, 0.95f);
        GUI.DrawTexture(new Rect(cx - gap - len, cy - 1f, len, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx + gap, cy - 1f, len, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - 1f, cy - gap - len, 2f, len), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - 1f, cy + gap, 2f, len), Texture2D.whiteTexture);

        GUI.color = new Color(1f, 0.9f, 0.55f, 0.35f + _bloom * 0.35f);
        GUI.DrawTexture(new Rect(cx - 1f, cy - 1f, 2f, 2f), Texture2D.whiteTexture);

        float sinceHit = Time.unscaledTime - _lastHit;
        if (sinceHit < 0.16f)
        {
            float a = 1f - sinceHit / 0.16f;
            bool kill = Time.unscaledTime - _lastKill < 0.22f;
            GUI.color = kill ? new Color(1f, 0.3f, 0.22f, a) : new Color(1f, 1f, 1f, a);
            float r = 5f + (1f - a) * 5f;
            DrawDiagonal(cx - r, cy - r, r, 1f);
            DrawDiagonal(cx + r, cy - r, -r, 1f);
            DrawDiagonal(cx - r, cy + r, r, -1f);
            DrawDiagonal(cx + r, cy + r, -r, -1f);
        }

        GUI.color = Color.white;
    }

    static void DrawDiagonal(float x, float y, float dx, float dy)
    {
        // Маленький хит-маркер без потреби в окремому спрайті.
        Matrix4x4 previous = GUI.matrix;
        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
        GUIUtility.RotateAroundPivot(angle, new Vector2(x + dx * 0.5f, y + dy * 0.5f));
        GUI.DrawTexture(new Rect(x, y - 1f, Mathf.Sqrt(dx * dx + dy * dy), 2f), Texture2D.whiteTexture);
        GUI.matrix = previous;
    }

    void Overlay(string text)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(40f, Screen.height * 0.5f - 70f, Screen.width - 80f, 150f), text, _center);
    }
}
