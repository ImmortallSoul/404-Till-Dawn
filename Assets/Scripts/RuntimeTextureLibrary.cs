using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Завантажує готові CC0-текстури з Assets/Resources/Textures.
/// Якщо асет відсутній — створює просту процедурну заміну, щоб гра не ламалася.
/// </summary>
public static class RuntimeTextureLibrary
{
    static readonly Dictionary<string, Texture2D> _cache = new Dictionary<string, Texture2D>();

    public static Texture2D GroundColor { get { return GetColor("Ground", new Color(0.28f, 0.25f, 0.2f)); } }
    public static Texture2D GroundNormal { get { return GetNormal("Ground"); } }
    public static Texture2D GrassColor { get { return GetColor("Grass", new Color(0.2f, 0.32f, 0.18f)); } }
    public static Texture2D GrassNormal { get { return GetNormal("Grass"); } }

    public static Texture2D PumpkinColor
    {
        get { return Get("Pumpkin", new Color(1f, 0.46f, 0.08f), "pumpkin", true); }
    }
    public static Texture2D StoneColor { get { return GetColor("Stone", new Color(0.36f, 0.35f, 0.32f)); } }
    public static Texture2D StoneNormal { get { return GetNormal("Stone"); } }
    public static Texture2D WoodColor { get { return GetColor("Wood", new Color(0.36f, 0.22f, 0.12f)); } }
    public static Texture2D WoodNormal { get { return GetNormal("Wood"); } }
    public static Texture2D MetalColor { get { return GetColor("Metal", new Color(0.42f, 0.45f, 0.5f)); } }
    public static Texture2D MetalNormal { get { return GetNormal("Metal"); } }
    public static Texture2D FabricColor { get { return GetColor("Fabric", new Color(0.38f, 0.34f, 0.42f)); } }
    public static Texture2D FabricNormal { get { return GetNormal("Fabric"); } }
    public static Texture2D LeatherColor { get { return GetColor("Leather", new Color(0.34f, 0.2f, 0.12f)); } }
    public static Texture2D LeatherNormal { get { return GetNormal("Leather"); } }
    public static Texture2D CeramicColor { get { return GetColor("Ceramic", new Color(0.62f, 0.58f, 0.5f)); } }
    public static Texture2D CeramicNormal { get { return GetNormal("Ceramic"); } }

    public static Texture2D Panel { get { return Get("UI_Panel", Color.white, "panel", false); } }
    public static Texture2D Glow { get { return Get("UI_Glow", Color.white, "glow", false); } }
    public static Texture2D Ring { get { return Get("UI_Ring", Color.white, "ring", false); } }

    public static Texture2D TintedGlow(Color color)
    {
        // Біла маска: колір накладає матеріал, щоб не подвоювати tint.
        return Get("Glow_" + Key(color), Color.white, "glow", false);
    }

    public static Texture2D GetColor(string name, Color fallback)
    {
        return Get(name + "_Color", fallback, "surface", true);
    }

    public static Texture2D GetNormal(string name)
    {
        return Get(name + "_Normal", new Color(0.5f, 0.5f, 1f), "normal", true);
    }

    static Texture2D Get(string key, Color fallback, string style, bool repeat)
    {
        Texture2D texture;
        if (_cache.TryGetValue(key, out texture) && texture != null)
            return texture;

        texture = Resources.Load<Texture2D>("Textures/" + key);
        if (texture == null)
            texture = Generate(key, fallback, style);

        texture.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Trilinear;
        texture.anisoLevel = 4;
        _cache[key] = texture;
        return texture;
    }

    static Texture2D Generate(string key, Color color, string style)
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        var pixels = new Color[size * size];

        float seed = 0f;
        for (int i = 0; i < key.Length; i++)
            seed += key[i] * 0.137f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float v = y / (float)size;
                float dx = u - 0.5f;
                float dy = v - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float n = Mathf.PerlinNoise(u * 6f + seed, v * 6f + seed * 0.37f);

                Color c;
                switch (style)
                {
                    case "normal":
                        float h = Mathf.PerlinNoise(u * 7f + seed, v * 7f);
                        float hx = Mathf.PerlinNoise((u + 0.02f) * 7f + seed, v * 7f);
                        float hy = Mathf.PerlinNoise(u * 7f + seed, (v + 0.02f) * 7f);
                        c = new Color(0.5f + (h - hx) * 1.6f, 0.5f + (h - hy) * 1.6f, 1f, 1f);
                        break;
                    case "glow":
                        float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f);
                        c = new Color(color.r, color.g, color.b, a);
                        break;
                    case "ring":
                        float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 0.72f) * 9f);
                        c = new Color(1f, 1f, 1f, ring);
                        break;
                    case "panel":
                        float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
                        c = new Color(1f, 1f, 1f, Mathf.Clamp01(edge * 14f));
                        break;
                    case "pumpkin":
                        // Вертикальні ребра + легка плямистість, без земляного шуму.
                        float rib = 0.78f + 0.22f * Mathf.Cos(u * Mathf.PI * 16f);
                        float speckle = Mathf.Lerp(0.9f, 1.08f, n);
                        float shade = Mathf.Lerp(0.82f, 1.06f, v);
                        c = new Color(color.r * rib * speckle * shade,
                            color.g * rib * speckle * shade,
                            color.b * rib * speckle * shade, 1f);
                        break;
                    default:
                        float grain = Mathf.Lerp(0.72f, 1.12f, n);
                        c = new Color(color.r * grain, color.g * grain, color.b * grain, 1f);
                        break;
                }

                pixels[y * size + x] = c;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(true, false);
        texture.name = key;
        return texture;
    }

    static int Key(Color c)
    {
        return Mathf.RoundToInt(c.r * 255f) * 65536
               + Mathf.RoundToInt(c.g * 255f) * 256
               + Mathf.RoundToInt(c.b * 255f);
    }
}
