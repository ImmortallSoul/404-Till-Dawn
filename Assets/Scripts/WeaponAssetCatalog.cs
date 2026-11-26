using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Каталог готових CC0-моделей. Якщо ресурс відсутній — ViewModel використовує процедурний fallback.
/// </summary>
public static class WeaponAssetCatalog
{
    sealed class Entry
    {
        public string Resource;
        public float Length;
        public bool Double;
    }

    static readonly Dictionary<WeaponId, Entry> _entries = new Dictionary<WeaponId, Entry>
    {
        { WeaponId.Revolver,        E("Revolver", 0.56f) },
        { WeaponId.Shotgun,         E("Shotgun", 0.82f) },
        { WeaponId.DualSmgs,        E("DualSmgs", 0.62f, true) },
        { WeaponId.Batgun,          E("Batgun", 0.72f) },
        { WeaponId.FlameCannon,     E("FlameCannon", 0.88f) },
        { WeaponId.GrenadeLauncher, E("GrenadeLauncher", 0.8f) },
        { WeaponId.WateringGun,     E("WateringGun", 0.76f) },
        { WeaponId.Crossbow,        E("Crossbow", 0.74f) },
        { WeaponId.Staff,           E("KayKitStaff", 0.82f) },
        { WeaponId.CycloneSword,    E("KryvenBlade/V_KRYVEN_BLADE", 0.66f) }
    };

    static readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();
    static readonly Dictionary<string, Material> _importedMaterials = new Dictionary<string, Material>();

    static Entry E(string resource, float length, bool doubled = false)
    {
        return new Entry { Resource = resource, Length = length, Double = doubled };
    }

    public static bool TryGet(WeaponId id, out string resource, out float length, out bool doubled)
    {
        Entry entry;
        if (_entries.TryGetValue(id, out entry))
        {
            resource = entry.Resource;
            length = entry.Length;
            doubled = entry.Double;
            return true;
        }

        resource = null;
        length = 0f;
        doubled = false;
        return false;
    }

    public static GameObject LoadPrefab(WeaponId id)
    {
        string resource;
        float length;
        bool doubled;
        if (!TryGet(id, out resource, out length, out doubled))
            return null;
        return Resources.Load<GameObject>("Weapons/" + resource);
    }

    public static Material SurfaceMaterial(WeaponId id, string sourceMaterialName)
    {
        Surface surface = GuessSurface(id, sourceMaterialName);
        string key = id + "_" + surface;
        Material material;
        if (_materials.TryGetValue(key, out material) && material != null)
            return material;

        Texture2D color;
        Texture2D normal;
        Color tint;
        float smoothness;
        float metallic;
        Color? emission = null;

        switch (surface)
        {
            case Surface.Wood:
                color = RuntimeTextureLibrary.WoodColor;
                normal = RuntimeTextureLibrary.WoodNormal;
                tint = new Color(0.82f, 0.72f, 0.62f);
                smoothness = 0.18f;
                metallic = 0f;
                break;
            case Surface.Leather:
                color = RuntimeTextureLibrary.LeatherColor;
                normal = RuntimeTextureLibrary.LeatherNormal;
                tint = new Color(0.8f, 0.7f, 0.65f);
                smoothness = 0.12f;
                metallic = 0f;
                break;
            case Surface.Fabric:
                color = RuntimeTextureLibrary.FabricColor;
                normal = RuntimeTextureLibrary.FabricNormal;
                tint = new Color(0.78f, 0.78f, 0.84f);
                smoothness = 0.1f;
                metallic = 0f;
                break;
            case Surface.Glow:
                color = null;
                normal = null;
                tint = GlowTint(id);
                emission = tint * 1.4f;
                smoothness = 0.6f;
                metallic = 0f;
                break;
            case Surface.Gold:
                color = RuntimeTextureLibrary.MetalColor;
                normal = RuntimeTextureLibrary.MetalNormal;
                tint = new Color(0.95f, 0.78f, 0.35f);
                emission = new Color(0.24f, 0.16f, 0.03f);
                smoothness = 0.68f;
                metallic = 0.85f;
                break;
            default:
                color = RuntimeTextureLibrary.MetalColor;
                normal = RuntimeTextureLibrary.MetalNormal;
                tint = new Color(0.76f, 0.8f, 0.88f);
                smoothness = 0.45f;
                metallic = 0.72f;
                break;
        }

        material = GameMaterials.Create(tint, emission, color, new Vector2(2.2f, 2.2f), normal, smoothness, metallic);
        _materials[key] = material;
        return material;
    }

    /// <summary>
    /// Імпортовані FBX вже мають власні UV. Використовуємо текстуру з FBX, якщо вона є,
    /// інакше — готовий CC0-атлас відповідного пака. Матеріал кешується на сесію.
    /// </summary>
    public static Material StyleRenderer(WeaponId id, Renderer renderer, string sourceMaterialName)
    {
        // Kryven Blade має фірмові зелені руни й очі: лишаємо його Built-in Standard
        // матеріал з HDR-емісією замість процедурного перефарбування.
        if (id == WeaponId.CycloneSword && renderer.sharedMaterial != null &&
            renderer.sharedMaterial.name.IndexOf("KRYVEN", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return renderer.sharedMaterial;

        Texture2D fbxTexture = renderer.sharedMaterial != null ? renderer.sharedMaterial.mainTexture as Texture2D : null;
        Texture2D atlas = Atlas(id);
        Texture2D color = fbxTexture != null ? fbxTexture : atlas;

        Surface surface = GuessSurface(id, sourceMaterialName);
        string key = id + "_imported_" + surface + "_" + (fbxTexture != null ? "fbx" : "atlas");
        Material material;
        if (_importedMaterials.TryGetValue(key, out material) && material != null)
            return material;

        Color tint = color != null ? new Color(0.96f, 0.96f, 0.96f) : new Color(0.8f, 0.83f, 0.9f);
        Color? emission = surface == Surface.Glow ? GlowTint(id) * 1.2f : (Color?)null;
        Texture2D normal = NormalFor(surface);
        float smoothness = 0.45f;
        float metallic = 0.55f;
        if (surface == Surface.Wood || surface == Surface.Leather || surface == Surface.Fabric)
        {
            smoothness = 0.14f;
            metallic = 0f;
        }
        else if (surface == Surface.Gold)
        {
            smoothness = 0.68f;
            metallic = 0.85f;
        }
        else if (surface == Surface.Glow)
        {
            smoothness = 0.6f;
            metallic = 0f;
        }

        material = GameMaterials.Create(tint, emission, color, Vector2.one, normal, smoothness, metallic);
        _importedMaterials[key] = material;
        return material;
    }

    public static Texture2D Atlas(WeaponId id)
    {
        if (id == WeaponId.CycloneSword)
            return Resources.Load<Texture2D>("Weapons/weapons_bits_texture");
        bool kayKit = id == WeaponId.Crossbow || id == WeaponId.Staff;
        string path = kayKit ? "Weapons/skeleton_texture" : "Weapons/Textures/colormap";
        return Resources.Load<Texture2D>(path);
    }

    static Texture2D NormalFor(Surface surface)
    {
        switch (surface)
        {
            case Surface.Wood: return RuntimeTextureLibrary.WoodNormal;
            case Surface.Leather: return RuntimeTextureLibrary.LeatherNormal;
            case Surface.Fabric: return RuntimeTextureLibrary.FabricNormal;
            case Surface.Glow: return null;
            default: return RuntimeTextureLibrary.MetalNormal;
        }
    }

    static Color GlowTint(WeaponId id)
    {
        switch (id)
        {
            case WeaponId.Batgun: return new Color(0.9f, 0.32f, 1f);
            case WeaponId.FlameCannon: return new Color(1f, 0.42f, 0.08f);
            case WeaponId.WateringGun: return new Color(0.4f, 1f, 0.45f);
            case WeaponId.GrenadeLauncher: return new Color(0.6f, 1f, 0.25f);
            default: return new Color(0.45f, 0.8f, 1f);
        }
    }

    static Surface GuessSurface(WeaponId id, string materialName)
    {
        if (string.IsNullOrEmpty(materialName))
            return Surface.Metal;

        string lower = materialName.ToLowerInvariant();
        if (lower.Contains("glow") || lower.Contains("emiss") || lower.Contains("energy"))
            return Surface.Glow;
        if (lower.Contains("wood") || lower.Contains("stock") || lower.Contains("grip"))
            return Surface.Wood;
        if (lower.Contains("leather"))
            return Surface.Leather;
        if (lower.Contains("cloth") || lower.Contains("fabric"))
            return Surface.Fabric;
        if (lower.Contains("gold") || lower.Contains("brass"))
            return Surface.Gold;
        return Surface.Metal;
    }

    enum Surface
    {
        Metal,
        Wood,
        Leather,
        Fabric,
        Glow,
        Gold
    }
}
