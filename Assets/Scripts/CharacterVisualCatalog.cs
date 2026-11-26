using System.Collections.Generic;
using UnityEngine;

public sealed class CharacterVisual
{
    public GameObject Instance;
    public CharacterAnimationController Animations;
}

/// <summary>
/// Готові CC0-префаби KayKit Skeletons для summon'ів та еліт.
/// Якщо ресурс відсутній — повертає null, і гра folkback'иться до процедурної моделі.
/// </summary>
public static class CharacterVisualCatalog
{
    sealed class Spec
    {
        public string Resource;
        public string AnimationProfile;
        public float Scale;
        public Color Tint;
        public bool KeepMaterials;
    }

    static readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();
    static Texture2D _skeletonTexture;
    static bool _textureChecked;

    public static CharacterVisual CreateSummon(Transform parent, SummonKind kind, float size)
    {
        // Коса смерті — окрема готова CC0-модель (Quaternius), без KayKit-анімацій.
        if (kind == SummonKind.Scythe)
            return CreateScythe(parent, size);

        Spec spec;
        float targetHeight = size * 2.6f;
        switch (kind)
        {
            case SummonKind.Dragon:
                // Quaternius CC0 dragon — справжній дракон, не скелет-замінник.
                spec = new Spec { Resource = "Creatures/Dragon", AnimationProfile = "none", Scale = size * 1.5f, Tint = Color.white, KeepMaterials = true };
                targetHeight = size * 5.2f;
                break;
            case SummonKind.Scythe:
                spec = new Spec { Resource = "Skeleton_Rogue", AnimationProfile = "melee", Scale = size * 1.5f, Tint = new Color(0.78f, 0.88f, 1f) };
                break;
            case SummonKind.Butterfly:
                // CC BY 3.0 метелик Google Data Arts Team; орбіту й політ
                // задає SummonManager, тому KayKit-анімації не накладаємо.
                spec = new Spec { Resource = "Creatures/Butterfly", AnimationProfile = "none", Scale = size * 1.35f, Tint = Color.white, KeepMaterials = true };
                targetHeight = size * 1.9f;
                break;
            case SummonKind.FrenzyBat:
                // Quaternius CC0 bat замість Skeleton_Warrior.
                spec = new Spec { Resource = "Creatures/Bat", AnimationProfile = "none", Scale = size * 1.35f, Tint = Color.white, KeepMaterials = true };
                targetHeight = size * 2.3f;
                break;
            case SummonKind.Spirit:
            default:
                spec = new Spec { Resource = "Skeleton_Mage", AnimationProfile = "magic", Scale = size * 1.5f, Tint = new Color(0.55f, 0.85f, 1f) };
                break;
        }
        return Create(parent, spec, targetHeight);
    }

    /// <summary>
    /// Плаваюча CC0-коса. Статичний меш: анімацію робить SummonManager
    /// через transform, бо до цієї моделі немає сумісного rig/кліпів.
    /// </summary>
    static CharacterVisual CreateScythe(Transform parent, float size)
    {
        var prefab = Resources.Load<GameObject>("Summons/ReapersEdge");
        if (prefab == null || parent == null)
            return null;

        // Поворот і гойдання застосовуються до порожнього pivot, а не до самої
        // моделі: так центр меша не з'їжджає з орбіти при AnimateScythe().
        var pivot = new GameObject("ReapersEdge");
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = Vector3.zero;
        pivot.transform.localRotation = Quaternion.identity;

        var model = Object.Instantiate(prefab, pivot.transform);
        model.name = "ReapersEdgeMesh";

        var animator = model.GetComponent<Animator>();
        if (animator != null)
            Object.Destroy(animator);
        var colliders = model.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            Object.Destroy(colliders[i]);

        Material material = GameMaterials.Create(
            new Color(0.30f, 0.32f, 0.38f),
            new Color(0.42f, 0.18f, 0.75f),
            RuntimeTextureLibrary.MetalColor, new Vector2(1.4f, 1.4f), null, 0.58f, 0.42f);
        var renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sharedMaterial = material;
            renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        Bounds bounds = BoundsOf(model.transform);
        if (bounds.size.sqrMagnitude > 0.000001f)
        {
            float scale = Mathf.Max(0.05f, size * 4.2f) / Mathf.Max(0.05f, bounds.size.y);
            model.transform.localScale = Vector3.one * scale;
            bounds = BoundsOf(model.transform);
            model.transform.localPosition -= pivot.transform.InverseTransformPoint(bounds.center);
        }

        Debug.Log("[Visuals] CC0 ReapersEdge attached to Scythe summon");
        return new CharacterVisual { Instance = pivot, Animations = null };
    }

    public static CharacterVisual CreateEnemy(Transform parent, EnemyKind kind, EnemyTier tier, float height,
        Color tint, bool skeleton = false)
    {
        // Фінальний бос і одна еліта на всіх мапах — та сама FabSkeleton-модель,
        // що й у гравця, з власним профілем анімацій "fab".
        if (skeleton)
        {
            return Create(parent, new Spec
            {
                Resource = "FabSkeleton/Skeleton_Model",
                AnimationProfile = "fab",
                Scale = tier == EnemyTier.Boss ? 1.25f : 1.05f,
                Tint = tier == EnemyTier.Boss
                    ? new Color(0.55f, 0.62f, 0.92f)
                    : new Color(0.9f, 0.72f, 0.45f)
            }, height);
        }

        string resource;
        string profile = "melee";
        if (kind == EnemyKind.Pumpkin)
        {
            string[] pumpkins =
            {
                "Pumpkins/SM_Pumpkin_01", "Pumpkins/SM_Pumpkin_02",
                "Pumpkins/SM_Pumpkin_03", "Pumpkins/SM_Pumpkin_04",
                "Props/Pumpkins/PumpkinEnemy", "Props/Pumpkins/Pumpkin_01"
            };
            for (int i = 0; i < pumpkins.Length; i++)
            {
                if (Resources.Load<GameObject>(pumpkins[i]) != null)
                {
                    resource = pumpkins[i];
                    var pumpkinVisual = Create(parent, new Spec
                    {
                        Resource = resource,
                        AnimationProfile = "melee",
                        Scale = 1f,
                        Tint = Color.Lerp(new Color(1f, 0.68f, 0.3f), tint, 0.3f)
                    }, height);
                    if (pumpkinVisual != null)
                        return pumpkinVisual;
                }
            }
        }

        switch (kind)
        {
            case EnemyKind.Cultist:
            case EnemyKind.Witch:
            case EnemyKind.Elder:
            case EnemyKind.Hastur:
            case EnemyKind.Yog:
                resource = "Skeleton_Mage";
                profile = "magic";
                break;
            case EnemyKind.Lamprey:
            case EnemyKind.Spawnling:
            case EnemyKind.Eyebat:
            case EnemyKind.Ghost:
                resource = "Skeleton_Minion";
                profile = "ranged";
                break;
            case EnemyKind.Reaper:
            case EnemyKind.WingedTerror:
            case EnemyKind.Squid:
                resource = "Skeleton_Rogue";
                break;
            default:
                resource = "Skeleton_Warrior";
                break;
        }

        float scale = tier == EnemyTier.Boss ? 1.35f : tier == EnemyTier.Elite ? 1.05f : 0.9f;
        var spec = new Spec
        {
            Resource = resource,
            AnimationProfile = profile,
            Scale = scale,
            Tint = Color.Lerp(new Color(0.72f, 0.76f, 0.88f), tint, 0.55f)
        };
        return Create(parent, spec, height * scale);
    }

    static CharacterVisual Create(Transform parent, Spec spec, float targetHeight)
    {
        if (parent == null || string.IsNullOrEmpty(spec.Resource))
            return null;

        string path = spec.Resource.IndexOf('/') >= 0
            ? spec.Resource
            : "Characters/" + spec.Resource;
        var prefab = Resources.Load<GameObject>(path);
        if (prefab == null)
            return null;

        var instance = Object.Instantiate(prefab, parent);
        instance.name = "CC0_" + spec.Resource;

        var animator = instance.GetComponent<Animator>();
        if (animator != null)
            Object.Destroy(animator);

        var colliders = instance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            Object.Destroy(colliders[i]);

        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (!spec.KeepMaterials)
        {
            Material material = TintedMaterial(spec.Tint);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].sharedMaterial = material;
        }
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderers[i].receiveShadows = false;
        }

        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        Bounds bounds = BoundsOf(instance.transform);
        if (bounds.size.sqrMagnitude > 0.000001f)
        {
            float scale = targetHeight / Mathf.Max(0.05f, bounds.size.y);
            instance.transform.localScale = Vector3.one * scale;
            float bottom = parent.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z)).y;
            var p = instance.transform.localPosition;
            p.y -= bottom * scale;
            instance.transform.localPosition = p;
        }
        else
        {
            instance.transform.localScale = Vector3.one * spec.Scale;
        }

        var animations = CharacterAnimationController.Attach(instance, spec.AnimationProfile);
        Debug.Log("[Visuals] " + spec.Resource + " height=" + targetHeight.ToString("0.00"));
        return new CharacterVisual { Instance = instance, Animations = animations };
    }

    static Bounds BoundsOf(Transform root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        bool first = true;
        Bounds result = new Bounds(Vector3.zero, Vector3.zero);
        for (int i = 0; i < renderers.Length; i++)
        {
            Bounds b = renderers[i].bounds;
            if (b.size.sqrMagnitude < 0.000001f)
                continue;
            if (first)
            {
                result = b;
                first = false;
            }
            else
            {
                result.Encapsulate(b);
            }
        }
        return result;
    }

    static Material TintedMaterial(Color tint)
    {
        if (!_textureChecked)
        {
            _textureChecked = true;
            _skeletonTexture = Resources.Load<Texture2D>("Characters/skeleton_texture");
        }

        string key = Mathf.RoundToInt(tint.r * 255f) + "_" + Mathf.RoundToInt(tint.g * 255f) + "_" + Mathf.RoundToInt(tint.b * 255f);
        Material material;
        if (_materials.TryGetValue(key, out material) && material != null)
            return material;

        material = GameMaterials.Create(tint, tint * 0.28f, _skeletonTexture, Vector2.one, null, 0.3f, 0.08f);
        _materials[key] = material;
        return material;
    }
}
