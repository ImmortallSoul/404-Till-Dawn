using UnityEngine;

/// <summary>
/// Процедурне нічне небо: темний градієнт, зорі й місяць без зовнішніх текстур.
/// Використовує Skybox/Panoramic, якщо шейдер доступний, інакше — інвертований купол.
/// </summary>
public static class NightSky
{
    const int Width = 1024;
    const int Height = 512;

    static Texture2D _texture;
    static Material _skyMaterial;
    static Material _domeMaterial;
    static Transform _dome;

    public static void Create()
    {
        if (_texture != null)
        {
            // При повторному запуску сцена може мати інший skybox — відновлюємо.
            if (_skyMaterial != null)
                RenderSettings.skybox = _skyMaterial;
            return;
        }

        BuildTexture();

        var shader = Shader.Find("Skybox/Panoramic");
        if (shader != null)
        {
            _skyMaterial = new Material(shader) { name = "NightSkyPanoramic" };
            if (_skyMaterial.HasProperty("_MainTex"))
                _skyMaterial.SetTexture("_MainTex", _texture);
            if (_skyMaterial.HasProperty("_Tint"))
                _skyMaterial.SetColor("_Tint", new Color(0.85f, 0.9f, 1f));
            if (_skyMaterial.HasProperty("_Exposure"))
                _skyMaterial.SetFloat("_Exposure", 1f);
            if (_skyMaterial.HasProperty("_Mapping"))
                _skyMaterial.SetFloat("_Mapping", 0f); // Latitude-Longitude
            if (_skyMaterial.HasProperty("_ImageType"))
                _skyMaterial.SetFloat("_ImageType", 0f);

            RenderSettings.skybox = _skyMaterial;
        }
        else
        {
            BuildDome();
        }
    }

    public static void SetDawn(float dawn)
    {
        dawn = Mathf.Clamp01(dawn);

        if (_skyMaterial != null)
        {
            if (_skyMaterial.HasProperty("_Exposure"))
                _skyMaterial.SetFloat("_Exposure", Mathf.Lerp(1f, 1.12f, dawn));
            if (_skyMaterial.HasProperty("_Tint"))
                _skyMaterial.SetColor("_Tint", Color.Lerp(new Color(0.85f, 0.9f, 1f),
                    new Color(1f, 0.72f, 0.56f), dawn));
        }

        if (_domeMaterial != null && _domeMaterial.HasProperty("_Color"))
            _domeMaterial.SetColor("_Color", Color.Lerp(new Color(0.8f, 0.86f, 1f),
                new Color(1f, 0.72f, 0.56f), dawn));
    }

    static void BuildDome()
    {
        var shader = Shader.Find("Unlit/Texture");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader == null)
            return;

        _domeMaterial = new Material(shader) { name = "NightSkyDome" };
        if (_domeMaterial.HasProperty("_MainTex"))
            _domeMaterial.SetTexture("_MainTex", _texture);

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "NightSkyDome";
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.localScale = new Vector3(-4000f, -4000f, -4000f);
        go.GetComponent<MeshRenderer>().sharedMaterial = _domeMaterial;
        go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.GetComponent<MeshRenderer>().receiveShadows = false;
        go.AddComponent<SkyDomeFollow>();
        _dome = go.transform;
    }

    static void BuildTexture()
    {
        _texture = new Texture2D(Width, Height, TextureFormat.RGB24, false)
        {
            name = "NightSkyStars",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            anisoLevel = 1
        };

        var pixels = new Color[Width * Height];

        Color zenith = new Color(0.012f, 0.018f, 0.055f);
        Color horizon = new Color(0.045f, 0.06f, 0.13f);
        Color groundHorizon = new Color(0.025f, 0.03f, 0.055f);
        Color ground = new Color(0.008f, 0.01f, 0.02f);

        for (int y = 0; y < Height; y++)
        {
            float v = y / (float)(Height - 1);
            float fromHorizon = Mathf.Abs(v - 0.5f) * 2f;
            Color row = v >= 0.5f
                ? Color.Lerp(horizon, zenith, Mathf.SmoothStep(0f, 1f, fromHorizon))
                : Color.Lerp(groundHorizon, ground, Mathf.SmoothStep(0f, 1f, fromHorizon));

            int offset = y * Width;
            for (int x = 0; x < Width; x++)
                pixels[offset + x] = row;
        }

        var rng = new System.Random(20261122);

        // Розсіяні зорі, переважно у верхній півкулі.
        for (int i = 0; i < 1500; i++)
        {
            int x = rng.Next(Width);
            int y = Height / 2 + 2 + rng.Next(Height / 2 - 4);
            float brightness = 0.35f + (float)rng.NextDouble() * 0.65f;
            float warmth = 0.85f + (float)rng.NextDouble() * 0.15f;
            var star = new Color(brightness, brightness * warmth, brightness * (0.9f + warmth * 0.1f));

            AddPixel(pixels, x, y, star);
            if (brightness > 0.75f)
            {
                AddPixel(pixels, x + 1, y, star * 0.28f);
                AddPixel(pixels, x - 1, y, star * 0.28f);
                AddPixel(pixels, x, y + 1, star * 0.28f);
                AddPixel(pixels, x, y - 1, star * 0.28f);
            }
        }

        // Місяць із м'яким ореолом.
        int moonX = (int)(Width * 0.19f);
        int moonY = (int)(Height * 0.76f);
        int radius = 17;
        for (int dy = -radius * 2; dy <= radius * 2; dy++)
        {
            for (int dx = -radius * 2; dx <= radius * 2; dx++)
            {
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > radius * 2f)
                    continue;

                Color c;
                if (d <= radius)
                {
                    float core = 1f - d / radius;
                    c = Color.Lerp(new Color(0.55f, 0.62f, 0.8f), Color.white, core);
                }
                else
                {
                    float glow = 1f - (d - radius) / radius;
                    c = new Color(0.16f, 0.2f, 0.34f) * glow;
                }

                AddPixel(pixels, moonX + dx, moonY + dy, c);
            }
        }

        _texture.SetPixels(pixels);
        _texture.Apply(false, true);
    }

    static void AddPixel(Color[] pixels, int x, int y, Color color)
    {
        x = (x % Width + Width) % Width;
        if (y < 0 || y >= Height)
            return;

        int index = y * Width + x;
        pixels[index] += color;
    }
}

public class SkyDomeFollow : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main != null)
            transform.position = Camera.main.transform.position;
    }
}
