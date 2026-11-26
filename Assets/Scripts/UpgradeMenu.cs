using UnityEngine;

public class UpgradeMenu : MonoBehaviour
{
    public static UpgradeMenu Instance { get; private set; }

    readonly UpgradeDefinition[] _offered = new UpgradeDefinition[4];
    bool _visible;

    GUIStyle _title;
    GUIStyle _hint;
    GUIStyle _kind;
    GUIStyle _cardTitle;
    GUIStyle _body;
    GUIStyle _rarity;
    GUIStyle _buttonText;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ShowIfNeeded()
    {
        var player = GameSession.Instance != null ? GameSession.Instance.Player : null;
        if (player == null || player.PendingLevels <= 0 || _visible)
            return;

        var combat = player.GetComponent<PlayerCombat>();
        Roll(combat);
        _visible = true;
        GameSession.Instance.BeginLevelUp();
    }

    void Roll(PlayerCombat combat)
    {
        UpgradeCatalog.FillRandomDistinct(_offered, combat);
    }

    void Update()
    {
        if (!_visible)
            return;

        var session = GameSession.Instance;
        if (session == null || session.Player == null)
            return;

        if (Input.GetKeyDown(KeyCode.R) && session.Player.RerollsLeft > 0)
        {
            session.Player.RerollsLeft--;
            Roll(session.Player.GetComponent<PlayerCombat>());
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) Pick(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) Pick(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) Pick(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) Pick(3);
    }

    void Pick(int index)
    {
        if (!_visible || index < 0 || index >= _offered.Length || _offered[index] == null)
            return;

        var session = GameSession.Instance;
        if (session == null || session.Player == null)
            return;

        _visible = false;
        AudioManager.Instance?.Play("card_select", 0.85f, Random.Range(0.96f, 1.06f), 0f);
        var combat = session.Player.GetComponent<PlayerCombat>();
        _offered[index].Apply(session.Player, combat);
        session.Player.PendingLevels--;

        if (session.Player.PendingLevels > 0)
            ShowIfNeeded();
        else
            session.EndLevelUp();
    }

    void BuildStyles()
    {
        if (_title != null)
            return;

        _title = new GUIStyle(GUI.skin.label)
        {
            fontSize = 30,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        _title.normal.textColor = new Color(1f, 0.82f, 0.42f);

        _hint = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleCenter };
        _hint.normal.textColor = new Color(0.78f, 0.8f, 0.86f);

        _kind = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold };
        _kind.normal.textColor = Color.white;

        _cardTitle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 19,
            alignment = TextAnchor.UpperCenter,
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };
        _cardTitle.normal.textColor = new Color(1f, 0.87f, 0.5f);

        _body = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            alignment = TextAnchor.UpperCenter,
            wordWrap = true
        };
        _body.normal.textColor = Color.white;

        _rarity = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold };
        _rarity.normal.textColor = Color.white;

        _buttonText = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _buttonText.normal.textColor = Color.white;
    }

    void OnGUI()
    {
        if (!_visible)
            return;

        BuildStyles();
        MenuVisuals.Ensure();

        GUI.color = new Color(0f, 0f, 0f, 0.82f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        var session = GameSession.Instance;
        if (session == null || session.Player == null)
            return;

        GUI.Label(new Rect(0f, 44f, Screen.width, 44f), "РІВЕНЬ " + session.Player.Level, _title);

        string hintText = "Обери підсилення  1 / 2 / 3 / 4";
        if (session.Player.RerollsLeft > 0)
            hintText += "      [R] Перемішати (" + session.Player.RerollsLeft + ")";
        GUI.Label(new Rect(0f, 88f, Screen.width, 26f), hintText, _hint);

        float cardW = Mathf.Min(270f, (Screen.width - 100f) / 4f - 18f);
        float gap = 16f;
        float total = cardW * 4f + gap * 3f;
        float x = (Screen.width - total) * 0.5f;
        float y = Screen.height * 0.5f - 138f;
        float h = 276f;
        Vector2 mouse = Event.current.mousePosition;

        for (int i = 0; i < _offered.Length; i++)
        {
            var upgrade = _offered[i];
            if (upgrade == null)
                continue;

            var rect = new Rect(x + i * (cardW + gap), y, cardW, h);
            Color accent = RarityColor(upgrade.Rarity);
            Color kindColor = KindColor(upgrade.Kind);
            bool hover = rect.Contains(mouse);

            MenuVisuals.DrawGlow(new Rect(rect.x - 18f, rect.y - 18f, rect.width + 36f, rect.height + 36f),
                hover ? kindColor : accent, hover ? 0.36f : 0.16f);
            MenuVisuals.DrawPanel(rect, accent);

            GUI.color = accent;
            GUI.DrawTexture(new Rect(rect.x + 8f, rect.y + 3f, rect.width - 16f, 3f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x + 3f, rect.yMax - 3f, rect.width - 6f, 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            _kind.normal.textColor = kindColor;
            GUI.Label(new Rect(rect.x, rect.y + 12f, rect.width, 16f),
                Icon(upgrade.Kind) + "  " + UpgradeKindNames.Display(upgrade.Kind), _kind);

            GUI.Label(new Rect(rect.x + 8f, rect.y + 34f, rect.width - 16f, 48f),
                (i + 1) + ". " + upgrade.Title, _cardTitle);

            string body = upgrade.Body;
            if (upgrade.Kind == UpgradeKind.Weapon)
                body += "\n\nЗамінити активний ствол або підвищити його рівень";
            GUI.Label(new Rect(rect.x + 14f, rect.y + 88f, rect.width - 28f, 112f), body, _body);

            _rarity.normal.textColor = accent;
            GUI.Label(new Rect(rect.x, rect.y + h - 74f, rect.width, 16f), upgrade.Rarity.ToString().ToUpper(), _rarity);

            var buttonRect = new Rect(rect.x + 30f, rect.yMax - 46f, rect.width - 60f, 34f);
            bool clicked = MenuVisuals.DrawButton(buttonRect, hover, kindColor);
            GUI.Label(buttonRect, "ВЗЯТИ", _buttonText);
            if (clicked)
                Pick(i);
        }
    }

    static string Icon(UpgradeKind kind)
    {
        switch (kind)
        {
            case UpgradeKind.Weapon: return "◆";
            case UpgradeKind.WeaponBuff: return "✦";
            case UpgradeKind.PlayerBuff: return "✚";
            case UpgradeKind.Survival: return "✧";
            case UpgradeKind.Summon: return "◈";
            default: return "•";
        }
    }

    static Color KindColor(UpgradeKind kind)
    {
        switch (kind)
        {
            case UpgradeKind.Weapon: return new Color(1f, 0.45f, 0.35f);
            case UpgradeKind.WeaponBuff: return new Color(1f, 0.72f, 0.25f);
            case UpgradeKind.PlayerBuff: return new Color(0.45f, 0.85f, 1f);
            case UpgradeKind.Survival: return new Color(0.45f, 0.95f, 0.5f);
            case UpgradeKind.Summon: return new Color(0.85f, 0.5f, 1f);
            default: return Color.white;
        }
    }

    static Color RarityColor(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Rare: return new Color(0.4f, 0.75f, 1f);
            case UpgradeRarity.Epic: return new Color(0.9f, 0.4f, 1f);
            default: return new Color(0.6f, 0.62f, 0.68f);
        }
    }
}
