using UnityEngine;

/// <summary>
/// Генерує всю графіку стартового меню під час виконання — без зовнішніх асетів.
/// </summary>
public static class MenuVisuals
{
    static Texture2D _background;
    static Texture2D _stars;
    static Texture2D _vignette;
    static Texture2D _panel;
    static Texture2D _button;
    static Texture2D _glow;

    static GUIStyle _panelStyle;
    static GUIStyle _buttonStyle;

    static int _screenW;
    static int _screenH;

    public static void Ensure()
    {
        if (_background != null && _screenW == Screen.width && _screenH == Screen.height)
            return;

        _screenW = Screen.width;
        _screenH = Screen.height;

        _background = MakeBackground();
        _stars = MakeStars();
        _vignette = MakeVignette();
        _panel = MakeRoundedTexture(64, 64, 12, new Color(0.055f, 0.065f, 0.105f, 0.94f), true);
        _button = MakeRoundedTexture(64, 32, 9, new Color(0.09f, 0.1f, 0.15f, 0.96f), false);
        _glow = MakeGlow();

        _panelStyle = new GUIStyle
        {
            border = new RectOffset(14, 14, 14, 14)
        };
        _panelStyle.normal = new GUIStyleState { background = _panel };

        _buttonStyle = new GUIStyle
        {
            border = new RectOffset(10, 10, 10, 10)
        };
        _buttonStyle.normal = new GUIStyleState { background = _button };
        _buttonStyle.hover = new GUIStyleState { background = _button };
        _buttonStyle.active = new GUIStyleState { background = _button };
        _buttonStyle.focused = new GUIStyleState { background = _button };
    }

    public static void DrawBackground(float time)
    {
        Ensure();

        Rect full = new Rect(0f, 0f, Screen.width, Screen.height);
        GUI.DrawTexture(full, _background, ScaleMode.StretchToFill);

        float drift = (time * 5f) % Mathf.Max(1f, Screen.height);
        GUI.color = new Color(1f, 1f, 1f, 0.7f);
        GUI.DrawTexture(new Rect(0f, -drift, Screen.width, Screen.height), _stars, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(0f, Screen.height - drift, Screen.width, Screen.height), _stars, ScaleMode.StretchToFill);

        float breathe = 0.5f + 0.5f * Mathf.Sin(time * 0.9f);
        DrawGlow(new Rect(Screen.width * 0.1f + Mathf.Sin(time * 0.23f) * 60f, Screen.height * 0.08f,
            Screen.width * 0.42f, Screen.height * 0.55f), new Color(0.16f, 0.35f, 0.9f), 0.12f + breathe * 0.05f);
        DrawGlow(new Rect(Screen.width * 0.62f + Mathf.Cos(time * 0.19f) * 70f, Screen.height * 0.4f,
            Screen.width * 0.4f, Screen.height * 0.5f), new Color(0.65f, 0.16f, 0.75f), 0.1f + breathe * 0.06f);
        DrawGlow(new Rect(Screen.width * 0.3f, -Screen.height * 0.18f,
            Screen.width * 0.4f, Screen.height * 0.5f), new Color(1f, 0.62f, 0.2f), 0.07f + breathe * 0.03f);

        GUI.color = Color.white;
        GUI.DrawTexture(full, _vignette, ScaleMode.StretchToFill);
    }

    public static void DrawPanel(Rect rect, Color accent)
    {
        Ensure();

        Color old = GUI.color;
        GUI.color = Color.white;
        GUI.Box(rect, GUIContent.none, _panelStyle);

        GUI.color = new Color(accent.r, accent.g, accent.b, 0.85f);
        GUI.DrawTexture(new Rect(rect.x + 14f, rect.y + 1f, rect.width - 28f, 2f), Texture2D.whiteTexture);
        GUI.color = new Color(accent.r, accent.g, accent.b, 0.18f);
        GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 12f, 2f, rect.height - 24f), Texture2D.whiteTexture);

        GUI.color = old;
    }

    public static bool DrawButton(Rect rect, bool selected, Color accent)
    {
        Ensure();

        Vector2 mouse = Event.current.mousePosition;
        bool hover = rect.Contains(mouse);
        Color old = GUI.color;

        Color tint;
        if (selected)
            tint = Color.Lerp(new Color(0.85f, 0.88f, 1f), accent, 0.38f);
        else if (hover)
            tint = new Color(0.62f, 0.68f, 0.85f);
        else
            tint = new Color(0.34f, 0.38f, 0.5f);

        GUI.color = tint;
        bool clicked = GUI.Button(rect, GUIContent.none, _buttonStyle);

        if (selected)
        {
            GUI.color = new Color(accent.r, accent.g, accent.b, 0.95f);
            GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 5f, 3f, rect.height - 10f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x + 6f, rect.yMax - 3f, rect.width - 12f, 2f), Texture2D.whiteTexture);
        }

        GUI.color = old;
        return clicked;
    }

    public static void DrawGlow(Rect rect, Color color, float alpha)
    {
        Ensure();
        Color old = GUI.color;
        GUI.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        GUI.DrawTexture(rect, _glow, ScaleMode.StretchToFill);
        GUI.color = old;
    }

    public static void DrawLine(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    public static float Pulse(float time, float speed = 2f)
    {
        return 0.5f + 0.5f * Mathf.Sin(time * speed);
    }

    // ---------- Генерація текстур ----------

    static Texture2D MakeBackground()
    {
        int w = Mathf.Min(1024, Mathf.Max(256, Screen.width));
        int h = Mathf.Min(576, Mathf.Max(144, Screen.height));
        var rng = new System.Random(404);
        var pixels = new Color32[w * h];

        Color top = new Color(0.012f, 0.018f, 0.045f);
        Color bottom = new Color(0.075f, 0.035f, 0.105f);

        for (int y = 0; y < h; y++)
        {
            float v = y / (float)Mathf.Max(1, h - 1);
            Color row = Color.Lerp(top, bottom, v);

            for (int x = 0; x < w; x++)
            {
                float u = x / (float)Mathf.Max(1, w - 1);
                float dx = (u - 0.5f) * 1.7f;
                float dy = (v - 0.58f) * 2.3f;
                float center = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 1.25f);
                Color c = row + new Color(0.08f, 0.04f, 0.13f) * center * center;
                c *= 0.94f + (float)rng.NextDouble() * 0.08f;
                c.a = 0.88f;
                pixels[y * w + x] = c;
            }
        }

        return MakeTexture(w, h, pixels, "MenuBackground");
    }

    static Texture2D MakeStars()
    {
        int w = Mathf.Min(1400, Mathf.Max(256, Screen.width));
        int h = Mathf.Min(800, Mathf.Max(144, Screen.height));
        var rng = new System.Random(7331);
        var pixels = new Color32[w * h];

        for (int i = 0; i < 280; i++)
        {
            int x = rng.Next(w);
            int y = rng.Next(h);
            Color c = Color.Lerp(new Color(0.5f, 0.72f, 1f), Color.white, (float)rng.NextDouble());
            c.a = 0.3f + (float)rng.NextDouble() * 0.65f;
            int size = rng.NextDouble() < 0.86 ? 1 : 2;

            DrawDot(pixels, w, h, x, y, size, c);

            if (rng.NextDouble() < 0.09)
            {
                Color cross = new Color(c.r, c.g, c.b, c.a * 0.45f);
                DrawDot(pixels, w, h, x - 2, y, 1, cross);
                DrawDot(pixels, w, h, x + 2, y, 1, cross);
                DrawDot(pixels, w, h, x, y - 2, 1, cross);
                DrawDot(pixels, w, h, x, y + 2, 1, cross);
            }
        }

        return MakeTexture(w, h, pixels, "MenuStars");
    }

    static Texture2D MakeVignette()
    {
        const int size = 256;
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x / (float)(size - 1) - 0.5f) * 2f;
                float ny = (y / (float)(size - 1) - 0.5f) * 2f;
                float d = Mathf.Clamp01(Mathf.Sqrt(nx * nx + ny * ny) / 1.4142f);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.34f) / 0.66f)) * 0.88f;
                pixels[y * size + x] = new Color(0f, 0f, 0f, a);
            }
        }

        return MakeTexture(size, size, pixels, "MenuVignette");
    }

    static Texture2D MakeRoundedTexture(int w, int h, float radius, Color baseColor, bool vertical)
    {
        var pixels = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float qx = Mathf.Max(radius - x, x - (w - 1 - radius), 0f);
                float qy = Mathf.Max(radius - y, y - (h - 1 - radius), 0f);
                float d = Mathf.Sqrt(qx * qx + qy * qy);
                float inside = 1f - Mathf.SmoothStep(radius - 1.2f, radius + 0.4f, d);

                if (inside <= 0f)
                    continue;

                Color c = baseColor;
                if (vertical)
                    c *= 0.9f + y / (float)(h - 1) * 0.16f;

                float edge = Mathf.Clamp01(1f - Mathf.Abs(d - (radius - 0.5f)) / 1.6f);
                c += new Color(0.12f, 0.15f, 0.24f) * edge;
                c.a *= inside;
                pixels[y * w + x] = c;
            }
        }

        return MakeTexture(w, h, pixels, vertical ? "MenuPanel" : "MenuButton");
    }

    static Texture2D MakeGlow()
    {
        const int size = 128;
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x / (float)(size - 1) - 0.5f) * 2f;
                float ny = (y / (float)(size - 1) - 0.5f) * 2f;
                float d = Mathf.Clamp01(Mathf.Sqrt(nx * nx + ny * ny));
                float a = Mathf.Pow(1f - d, 2.4f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }

        return MakeTexture(size, size, pixels, "MenuGlow");
    }

    static void DrawDot(Color32[] pixels, int w, int h, int x, int y, int size, Color color)
    {
        for (int oy = 0; oy < size; oy++)
        {
            for (int ox = 0; ox < size; ox++)
            {
                int px = x + ox;
                int py = y + oy;
                if (px < 0 || py < 0 || px >= w || py >= h)
                    continue;

                Color old = pixels[py * w + px];
                pixels[py * w + px] = new Color(
                    Mathf.Clamp01(old.r + color.r * color.a),
                    Mathf.Clamp01(old.g + color.g * color.a),
                    Mathf.Clamp01(old.b + color.b * color.a),
                    Mathf.Clamp01(old.a + color.a));
            }
        }
    }

    static Texture2D MakeTexture(int w, int h, Color32[] pixels, string name)
    {
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            name = name,
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }
}
