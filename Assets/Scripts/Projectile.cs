using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    static readonly Dictionary<ProjectileStyle, Material> _materials = new Dictionary<ProjectileStyle, Material>();
    static readonly Dictionary<string, Material> _fxMaterials = new Dictionary<string, Material>();

    float _damage;
    float _speed;
    int _pierce;
    float _life;
    float _maxLife;
    float _radius;
    int _bounces;
    ProjectileStyle _style;
    ProjectileBehavior _behavior;
    bool _fromPlayer;
    bool _returning;
    bool _stopped;
    Collider _lastTarget;
    Vector3 _direction;

    public static void Spawn(
        Vector3 position,
        Quaternion rotation,
        float damage,
        float speed,
        int pierce,
        float life,
        ProjectileStyle style = ProjectileStyle.Bolt,
        ProjectileBehavior behavior = ProjectileBehavior.Normal,
        bool fromPlayer = true,
        int bounces = 0)
    {
        GameObject go;
        if (style == ProjectileStyle.Knife)
        {
            // Справжній клинок замість витягнутої сфери — для ножів і Циклон-меча.
            go = new GameObject(fromPlayer ? "Projectile" : "EnemyProjectile");
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.AddComponent<MeshFilter>().sharedMesh = RuntimeMeshLibrary.WedgeBlade(0.52f, 0.16f, 0.035f);
            go.AddComponent<MeshRenderer>();
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = fromPlayer ? "Projectile" : "EnemyProjectile";
            go.transform.position = position;
            go.transform.rotation = rotation;
        }

        float size = 1f;
        if (fromPlayer && GameSession.Instance != null && GameSession.Instance.Player != null)
            size = GameSession.Instance.Player.BulletSizeMultiplier;
        go.transform.localScale = (style == ProjectileStyle.Knife
            ? Vector3.one
            : StyleScale(style)) * size;
        if (style == ProjectileStyle.Bat)
            go.transform.localScale = new Vector3(0.22f, 0.14f, 0.26f) * size;
        else if (style == ProjectileStyle.Flame)
            go.transform.localScale = new Vector3(0.13f, 0.13f, 0.34f) * size;

        var collider = go.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = SharedMaterial(style, fromPlayer);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        if (style == ProjectileStyle.Bat)
            AttachBatWings(go, renderer.sharedMaterial);

        var proj = go.AddComponent<Projectile>();
        proj._damage = damage;
        proj._speed = speed;
        proj._pierce = pierce;
        proj._life = life;
        proj._maxLife = life;
        proj._style = style;
        proj._behavior = behavior;
        proj._fromPlayer = fromPlayer;
        proj._bounces = bounces;
        proj._direction = rotation * Vector3.forward;
        proj._radius = (style == ProjectileStyle.Grenade ? 0.24f
                     : style == ProjectileStyle.Flame ? 0.22f
                     : style == ProjectileStyle.Rail ? 0.1f
                     : style == ProjectileStyle.Orb ? 0.26f : 0.13f) * size;
    }

    /// <summary>
    /// Клинкова дуга для Циклон-меча й ножів. Не снаряд, а короткий візуальний слід удару.
    /// </summary>
    public static void SpawnSlash(Vector3 position, Quaternion rotation, Color color)
    {
        var go = new GameObject("SlashFx");
        go.transform.position = position;
        go.transform.rotation = rotation;

        go.AddComponent<MeshFilter>().sharedMesh = RuntimeMeshLibrary.ArcRibbon(0.62f, 0.45f, 220f, 24);
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = SlashMaterial(color);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        go.AddComponent<SlashFx>();
    }

    static Material SlashMaterial(Color color)
    {
        string key = "slash_" + Mathf.RoundToInt(color.r * 255f) + "_" +
                     Mathf.RoundToInt(color.g * 255f) + "_" + Mathf.RoundToInt(color.b * 255f);
        Material material;
        if (_fxMaterials.TryGetValue(key, out material) && material != null)
            return material;
        material = GameMaterials.CreateGlow(color, RuntimeTextureLibrary.Glow, true);
        _fxMaterials[key] = material;
        return material;
    }

    static Vector3 StyleScale(ProjectileStyle style)
    {
        switch (style)
        {
            case ProjectileStyle.Pellet: return Vector3.one * 0.15f;
            case ProjectileStyle.Flame: return Vector3.one * 0.26f;
            case ProjectileStyle.Rail: return new Vector3(0.09f, 0.09f, 0.62f);
            case ProjectileStyle.Grenade: return Vector3.one * 0.34f;
            case ProjectileStyle.Arrow: return new Vector3(0.08f, 0.08f, 0.6f);
            case ProjectileStyle.Knife: return new Vector3(0.09f, 0.09f, 0.5f);
            case ProjectileStyle.Bat: return new Vector3(0.34f, 0.15f, 0.3f);
            case ProjectileStyle.Spore: return Vector3.one * 0.17f;
            case ProjectileStyle.Orb: return Vector3.one * 0.3f;
            default: return Vector3.one * 0.17f;
        }
    }

    static void AttachBatWings(GameObject root, Material material)
    {
        var visual = root.AddComponent<BatProjectileVisual>();
        for (int s = -1; s <= 1; s += 2)
        {
            var wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wing.name = "BatWing";
            wing.transform.SetParent(root.transform, false);
            wing.transform.localPosition = new Vector3(s * 0.55f, 0f, 0f);
            wing.transform.localScale = new Vector3(1.1f, 0.05f, 0.7f);
            wing.transform.localRotation = Quaternion.Euler(0f, 0f, s * 22f);
            Object.Destroy(wing.GetComponent<Collider>());
            wing.GetComponent<MeshRenderer>().sharedMaterial = material;

            if (s < 0)
                visual.LeftWing = wing.transform;
            else
                visual.RightWing = wing.transform;
        }
        visual.Phase = Random.Range(0f, 6f);
    }

    static Material SharedMaterial(ProjectileStyle style, bool fromPlayer)
    {
        int key = (int)style + (fromPlayer ? 0 : 100);
        Material material;
        if (_materials.TryGetValue((ProjectileStyle)key, out material) && material != null)
            return material;

        Color baseColor;
        Color emission;
        switch (style)
        {
            case ProjectileStyle.Pellet:
                baseColor = new Color(1f, 0.78f, 0.42f); emission = new Color(1.8f, 1.1f, 0.3f); break;
            case ProjectileStyle.Flame:
                baseColor = new Color(1f, 0.4f, 0.1f); emission = new Color(2.4f, 0.7f, 0.1f); break;
            case ProjectileStyle.Rail:
                baseColor = new Color(0.55f, 0.9f, 1f); emission = new Color(0.4f, 1.8f, 2.4f); break;
            case ProjectileStyle.Grenade:
                baseColor = new Color(0.35f, 0.42f, 0.25f); emission = new Color(0.5f, 0.8f, 0.2f); break;
            case ProjectileStyle.Arrow:
                baseColor = new Color(0.7f, 0.95f, 0.6f); emission = new Color(0.5f, 1.6f, 0.5f); break;
            case ProjectileStyle.Knife:
                baseColor = new Color(0.85f, 0.9f, 1f); emission = new Color(0.7f, 1.2f, 2f); break;
            case ProjectileStyle.Bat:
                baseColor = new Color(0.5f, 0.25f, 0.65f); emission = new Color(0.9f, 0.3f, 1.4f); break;
            case ProjectileStyle.Spore:
                baseColor = new Color(0.4f, 0.9f, 0.4f); emission = new Color(0.3f, 1.5f, 0.4f); break;
            case ProjectileStyle.Orb:
                baseColor = fromPlayer ? new Color(0.9f, 0.3f, 0.85f) : new Color(0.85f, 0.15f, 0.45f);
                emission = fromPlayer ? new Color(1.6f, 0.4f, 1.8f) : new Color(2f, 0.15f, 0.5f);
                break;
            default:
                baseColor = fromPlayer ? new Color(1f, 0.92f, 0.55f) : new Color(1f, 0.35f, 0.4f);
                emission = fromPlayer ? new Color(2.2f, 1.6f, 0.4f) : new Color(2f, 0.2f, 0.3f);
                break;
        }

        Texture2D colorMap;
        Texture2D normalMap;
        float smoothness;
        float metallic;

        switch (style)
        {
            case ProjectileStyle.Bolt:
            case ProjectileStyle.Arrow:
            case ProjectileStyle.Knife:
            case ProjectileStyle.Grenade:
                colorMap = RuntimeTextureLibrary.MetalColor;
                normalMap = RuntimeTextureLibrary.MetalNormal;
                smoothness = 0.55f;
                metallic = 0.65f;
                break;
            case ProjectileStyle.Bat:
                colorMap = RuntimeTextureLibrary.LeatherColor;
                normalMap = RuntimeTextureLibrary.LeatherNormal;
                smoothness = 0.2f;
                metallic = 0f;
                break;
            case ProjectileStyle.Flame:
            case ProjectileStyle.Spore:
            case ProjectileStyle.Rail:
            case ProjectileStyle.Orb:
                colorMap = RuntimeTextureLibrary.FabricColor;
                normalMap = RuntimeTextureLibrary.FabricNormal;
                smoothness = 0.45f;
                metallic = 0f;
                break;
            default:
                colorMap = RuntimeTextureLibrary.MetalColor;
                normalMap = RuntimeTextureLibrary.MetalNormal;
                smoothness = 0.42f;
                metallic = 0.5f;
                break;
        }

        material = GameMaterials.Create(Color.Lerp(baseColor, Color.white, 0.32f), emission,
            colorMap, new Vector2(1.2f, 1.2f), normalMap, smoothness, metallic);
        _materials[(ProjectileStyle)key] = material;
        return material;
    }

    void Update()
    {
        if (GameSession.Instance == null || !GameSession.Instance.AllowsGameplay)
            return;

        _life -= Time.deltaTime;
        if (_life <= 0f)
        {
            if (_behavior == ProjectileBehavior.Explosive && _fromPlayer)
                Explode(transform.position);
            Destroy(gameObject);
            return;
        }

        if (_behavior == ProjectileBehavior.Lingering && !_returning)
        {
            if (_maxLife - _life > 0.45f)
                _stopped = true;
            if (IsReloading())
                _returning = true;
        }

        if (_returning)
        {
            var player = GameSession.Instance.Player;
            if (player == null)
            {
                Destroy(gameObject);
                return;
            }
            Vector3 home = player.transform.position + Vector3.up * 0.7f - transform.position;
            home.y = 0f;
            if (home.sqrMagnitude < 0.5f)
            {
                Destroy(gameObject);
                return;
            }
            _direction = home.normalized;
            transform.rotation = Quaternion.LookRotation(_direction, Vector3.up);
        }
        else if (_behavior == ProjectileBehavior.Homing && _fromPlayer)
        {
            var target = Enemy.NearestEnemy(transform.position, 13f);
            if (target != null)
                SteerTo(target.transform.position, Time.deltaTime * 6f);
        }
        else if (_behavior == ProjectileBehavior.EnemyHoming || _behavior == ProjectileBehavior.EnemyCurse)
        {
            var player = GameSession.Instance.Player;
            if (player != null)
                SteerTo(player.transform.position + Vector3.up * 0.7f, Time.deltaTime * 3f);
        }

        if (_stopped && !_returning)
            return;

        bool phased = GameSession.Instance != null && GameSession.Instance.Player != null
                      && GameSession.Instance.Player.IsPhased;

        float step = _speed * Time.deltaTime;
        var hits = Physics.SphereCastAll(transform.position, _radius, _direction, step + 0.12f);
        System.Array.Sort(hits, delegate (RaycastHit a, RaycastHit b) { return a.distance.CompareTo(b.distance); });

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            Collider col = hit.collider;
            if (col == null)
                continue;

            if (col.GetComponentInParent<XpOrb>() != null
                || col.GetComponentInParent<HealOrb>() != null
                || col.GetComponentInParent<ChestPickup>() != null
                || col.GetComponentInParent<TomePickup>() != null)
                continue;

            var enemy = col.GetComponentInParent<Enemy>();
            var stats = col.GetComponentInParent<PlayerStats>();

            if (_fromPlayer)
            {
                // Власний гравець ніколи не повинен перехоплювати кулю.
                if (stats != null)
                    continue;

                if (enemy != null)
                {
                    // У паралельному вимірі кулі проходять крізь монстрів цього часу.
                    if (phased)
                        continue;

                    if (col == _lastTarget)
                        continue;

                    OnHitEnemy(enemy);
                    _lastTarget = col;

                    if (_behavior == ProjectileBehavior.Explosive)
                    {
                        Explode(transform.position);
                        Destroy(gameObject);
                        return;
                    }
                    if (_pierce <= 0 && !_returning)
                    {
                        Destroy(gameObject);
                        return;
                    }
                    if (!_returning)
                        _pierce--;
                    continue;
                }

                if (BounceOrDestroy(hit))
                    return;
                break;
            }

            // Ворожі снаряди проходять крізь монстрів.
            if (enemy != null)
                continue;

            if (stats != null)
            {
                stats.TryTakeDamage(_damage);
                SpawnBurst(transform.position, new Color(1f, 0.2f, 0.3f), 5);
                Destroy(gameObject);
                return;
            }

            if (BounceOrDestroy(hit))
                return;
            break;
        }

        transform.position += _direction * step;
    }

    bool BounceOrDestroy(RaycastHit hit)
    {
        if (_bounces > 0)
        {
            _direction = Vector3.Reflect(_direction, hit.normal).normalized;
            _direction.y = 0f;
            if (_direction.sqrMagnitude < 0.01f)
                _direction = -_direction;
            transform.rotation = Quaternion.LookRotation(_direction.normalized, Vector3.up);
            _bounces--;
            _lastTarget = null;
            return false;
        }

        if (_behavior == ProjectileBehavior.Explosive && _fromPlayer)
            Explode(transform.position);
        SpawnBurst(transform.position, _fromPlayer
            ? new Color(1f, 0.8f, 0.4f)
            : new Color(1f, 0.2f, 0.3f), 4);
        Destroy(gameObject);
        return true;
    }

    void SteerTo(Vector3 target, float amount)
    {
        Vector3 to = target - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.01f)
            return;
        _direction = Vector3.Slerp(_direction, to.normalized, Mathf.Clamp01(amount)).normalized;
        transform.rotation = Quaternion.LookRotation(_direction, Vector3.up);
    }

    static bool IsReloading()
    {
        var player = GameSession.Instance != null ? GameSession.Instance.Player : null;
        if (player == null)
            return false;
        var combat = player.GetComponent<PlayerCombat>();
        if (combat == null)
            return false;
        for (int i = 0; i < combat.Weapons.Count; i++)
        {
            if (combat.Weapons[i].IsReloading)
                return true;
        }
        return false;
    }

    void OnHitEnemy(Enemy enemy)
    {
        var stats = GameSession.Instance != null ? GameSession.Instance.Player : null;
        float damage = _damage;
        bool crit = false;

        if (stats != null)
        {
            if (stats.Assassin && enemy.Health01 > 0.99f)
                damage *= 1.6f;
            if (stats.CritChance > 0f && Random.value < stats.CritChance)
            {
                damage *= stats.CritMultiplier;
                crit = true;
            }
        }

        enemy.TakeDamage(damage);
        bool killed = enemy.Health01 <= 0f;
        GameHud.NotifyHit(crit, killed);
        DamageNumber.Spawn(enemy.transform.position + Vector3.up * 1.2f, damage, crit);
        SpawnBurst(transform.position, crit ? new Color(1f, 0.9f, 0.3f) : new Color(0.8f, 0.9f, 1f), crit ? 7 : 3);

        if (_behavior == ProjectileBehavior.Burning)
        {
            enemy.ApplyBurn(3f * (stats != null ? stats.BurnMultiplier : 1f), 3f);
        }
        else if (_behavior == ProjectileBehavior.Charged && stats != null)
        {
            enemy.ApplyFreeze(0.6f);
        }

        if (stats != null)
            stats.ApplyOnHit(enemy, damage);
    }

    void Explode(Vector3 position)
    {
        var enemies = Enemy.NearestEnemies(position, 40, 3.6f);
        for (int i = 0; i < enemies.Count; i++)
        {
            enemies[i].TakeDamage(_damage * 0.8f);
            enemies[i].ApplyBurn(2f, 1.5f);
        }

        var player = GameSession.Instance.Player;
        if (player != null && Vector3.Distance(player.transform.position, position) < 2.2f)
            player.TryTakeDamage(0.8f);

        SpawnBurst(position, new Color(1f, 0.55f, 0.15f), 14);

        SpawnFireBurst(position + Vector3.up * 0.3f, 12);
    }

    public static void SpawnBurst(Vector3 position, Color color, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Spark";
            go.transform.position = position;
            go.transform.localScale = Vector3.one * Random.Range(0.06f, 0.14f);
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = GameMaterials.Create(color * 0.5f, color * 1.6f);
            var fx = go.AddComponent<SparkFx>();
            fx.Velocity = new Vector3(Random.Range(-3f, 3f), Random.Range(1f, 4f), Random.Range(-3f, 3f));
        }
    }

    /// <summary>
    /// Полум'я вибуху — sprite-частки Kenney Particle Pack, а не снаряди-сфери.
    /// </summary>
    public static void SpawnFireBurst(Vector3 position, int count)
    {
        string[] sprites = { "flame_01", "flame_03", "flame_05", "fire_01", "spark_01", "smoke_01" };
        Quaternion facing = Camera.main != null
            ? Quaternion.LookRotation(-Camera.main.transform.forward, Vector3.up)
            : Quaternion.identity;

        for (int i = 0; i < count; i++)
        {
            string sprite = sprites[i % sprites.Length];
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "FireBurst_" + sprite;
            go.transform.position = position + Random.insideUnitSphere * 0.16f;
            go.transform.rotation = facing * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            float size = Random.Range(0.18f, 0.42f);
            go.transform.localScale = Vector3.one * size;
            Object.Destroy(go.GetComponent<Collider>());

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = FireBurstMaterial(sprite);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var fx = go.AddComponent<FireBurstParticle>();
            fx.BaseScale = size;
            fx.Velocity = new Vector3(Random.Range(-3.2f, 3.2f), Random.Range(1.2f, 4.4f),
                Random.Range(-3.2f, 3.2f));
            fx.Duration = Random.Range(0.35f, 0.65f);
        }
    }

    static Material FireBurstMaterial(string sprite)
    {
        Material material;
        if (_fxMaterials.TryGetValue(sprite, out material) && material != null)
            return material;

        Color tint = sprite.StartsWith("smoke", System.StringComparison.Ordinal)
            ? new Color(0.62f, 0.66f, 0.72f, 0.6f)
            : sprite.StartsWith("spark", System.StringComparison.Ordinal)
                ? new Color(1f, 0.92f, 0.5f, 0.95f)
                : new Color(1f, 0.58f, 0.2f, 0.95f);
        material = GameMaterials.CreateGlow(tint, Resources.Load<Texture2D>("Vfx/Particles/" + sprite), true);
        _fxMaterials[sprite] = material;
        return material;
    }
}

public class BatProjectileVisual : MonoBehaviour
{
    public Transform LeftWing;
    public Transform RightWing;
    public float Phase;

    void Update()
    {
        float flap = 20f + Mathf.Sin(Time.time * 19f + Phase) * 42f;
        if (LeftWing != null)
            LeftWing.localRotation = Quaternion.Euler(0f, 0f, flap);
        if (RightWing != null)
            RightWing.localRotation = Quaternion.Euler(0f, 0f, -flap);
    }
}

public class SparkFx : MonoBehaviour
{
    public Vector3 Velocity;
    float _life = 0.45f;

    void Update()
    {
        _life -= Time.unscaledDeltaTime;
        if (_life <= 0f)
        {
            Destroy(gameObject);
            return;
        }
        transform.position += Velocity * Time.unscaledDeltaTime;
        Velocity += Vector3.down * 6f * Time.unscaledDeltaTime;
        transform.localScale *= 1f - Time.unscaledDeltaTime * 1.6f;
    }
}

public class SlashFx : MonoBehaviour
{
    public float Duration = 0.22f;
    float _age;

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        _age += dt;
        if (_age >= Duration)
        {
            Destroy(gameObject);
            return;
        }

        float t = _age / Duration;
        float scale = 0.85f + Mathf.Sin(t * Mathf.PI) * 0.35f;
        transform.localScale = Vector3.one * scale;
        transform.Rotate(0f, 0f, 360f * dt, Space.Self);
    }
}

public class FireBurstParticle : MonoBehaviour
{
    public Vector3 Velocity;
    public float BaseScale = 0.25f;
    public float Duration = 0.5f;
    float _age;

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        _age += dt;
        if (_age >= Duration)
        {
            Destroy(gameObject);
            return;
        }

        float t = _age / Duration;
        transform.position += Velocity * dt;
        Velocity *= 1f - dt * 1.8f;
        Velocity += Vector3.up * dt * 1.2f;
        transform.Rotate(0f, 0f, 120f * dt, Space.Self);
        transform.localScale = Vector3.one * (BaseScale * (1f - t) * (0.75f + Mathf.Sin(t * Mathf.PI) * 0.4f));
    }
}
