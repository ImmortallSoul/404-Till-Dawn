using System.Collections.Generic;
using UnityEngine;

public class SummonManager : MonoBehaviour
{
    public static SummonManager Instance { get; private set; }

    class Minion
    {
        public SummonKind Kind;
        public GameObject Go;
        public float Timer;
        public float Angle;
        public float Life;
        public float DeathTimer;
        public float AnimTimer;
        public float SwingTimer;
        public readonly Dictionary<Enemy, float> HitCooldowns = new Dictionary<Enemy, float>();
        public CharacterAnimationController Animations;
        public string State;
    }

    readonly List<Minion> _minions = new List<Minion>();
    PlayerStats _stats;
    float _spinFlash;

    void Awake()
    {
        Instance = this;
        _stats = GetComponent<PlayerStats>();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void AddOrLevel(SummonKind kind)
    {
        for (int i = 0; i < _minions.Count; i++)
        {
            if (_minions[i].Kind == kind && _minions[i].Life < 0f)
            {
                Destroy(_minions[i].Go);
                _minions.RemoveAt(i);
                break;
            }
        }

        int count = 0;
        for (int i = 0; i < _minions.Count; i++)
        {
            if (_minions[i].Kind == kind && _minions[i].DeathTimer <= 0f)
                count++;
        }

        if (count >= 4)
            return;

        SpawnOrbit(kind, count);
    }

    void SpawnOrbit(SummonKind kind, int index)
    {
        Color color = ColorFor(kind);
        float size = kind == SummonKind.Dragon ? 0.55f : 0.35f;
        var go = new GameObject(kind.ToString());

        var visual = CharacterVisualCatalog.CreateSummon(go.transform, kind, size);
        if (visual == null)
        {
            var primitive = GameObject.CreatePrimitive(kind == SummonKind.Scythe ? PrimitiveType.Cube : PrimitiveType.Sphere);
            primitive.name = "Fallback";
            primitive.transform.SetParent(go.transform, false);
            primitive.transform.localScale = Vector3.one * size;
            Object.Destroy(primitive.GetComponent<Collider>());
            primitive.GetComponent<MeshRenderer>().sharedMaterial = GameMaterials.Create(color * 0.4f, color * 1.6f);
        }

        _minions.Add(new Minion
        {
            Kind = kind,
            Go = go,
            Angle = index * 90f,
            Life = -1f,
            Animations = visual != null ? visual.Animations : null,
            State = "Spawn",
            AnimTimer = 0.55f
        });
        AudioManager.Instance?.Play("summon_spawn", 0.65f, Random.Range(0.92f, 1.08f), 0.08f);
    }

    public void SpawnSpirit(Vector3 position)
    {
        var go = new GameObject("Spirit");
        go.transform.position = position + Vector3.up * 0.7f;

        var visual = CharacterVisualCatalog.CreateSummon(go.transform, SummonKind.Spirit, 0.35f);
        if (visual == null)
        {
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            primitive.name = "Fallback";
            primitive.transform.SetParent(go.transform, false);
            primitive.transform.localScale = Vector3.one * 0.32f;
            Object.Destroy(primitive.GetComponent<Collider>());
            primitive.GetComponent<MeshRenderer>().sharedMaterial = GameMaterials.Create(
                new Color(0.5f, 0.85f, 1f), new Color(0.3f, 1.4f, 1.8f));
        }

        _minions.Add(new Minion
        {
            Kind = SummonKind.Spirit,
            Go = go,
            Life = 9f,
            Animations = visual != null ? visual.Animations : null,
            State = "Spawn",
            AnimTimer = 0.5f
        });
        AudioManager.Instance?.Play("summon_spawn", 0.55f, Random.Range(1f, 1.15f), 0.08f);
    }

    public void Spin(Vector3 position, float damage)
    {
        _spinFlash = 0.3f;
        for (int i = 0; i < 16; i++)
        {
            Quaternion rot = Quaternion.Euler(0f, i * 22.5f, 0f);
            Projectile.Spawn(position + Vector3.up * 0.6f + rot * Vector3.forward * 0.8f,
                rot, damage * 0.4f, 16f, 1, 0.7f,
                ProjectileStyle.Knife, ProjectileBehavior.Bouncing, true, 1);
        }
    }

    void Update()
    {
        if (GameSession.Instance == null || !GameSession.Instance.AllowsGameplay)
            return;

        _spinFlash = Mathf.Max(0f, _spinFlash - Time.deltaTime);

        for (int i = _minions.Count - 1; i >= 0; i--)
        {
            var m = _minions[i];
            if (m.Go == null)
            {
                _minions.RemoveAt(i);
                continue;
            }

            if (m.DeathTimer > 0f)
            {
                m.DeathTimer -= Time.deltaTime;
                if (m.DeathTimer <= 0f)
                {
                    Destroy(m.Go);
                    _minions.RemoveAt(i);
                }
                continue;
            }

            if (m.AnimTimer > 0f)
            {
                m.AnimTimer -= Time.deltaTime;
                if (m.AnimTimer <= 0f && m.State == "Spawn")
                    SetState(m, "Idle");
            }

            if (m.Life > 0f)
            {
                m.Life -= Time.deltaTime;
                if (m.Life <= 0f)
                {
                    SetState(m, "Death");
                    m.DeathTimer = 0.55f;
                    continue;
                }
                UpdateSpirit(m);
            }
            else
            {
                UpdateOrbit(m);
            }
        }
    }

    static void SetState(Minion minion, string state)
    {
        if (minion.Animations == null || minion.State == state)
            return;
        minion.State = state;
        minion.Animations.SetState(state, 0.14f);
    }

    void UpdateSpirit(Minion m)
    {
        m.SwingTimer = Mathf.Max(0f, m.SwingTimer - Time.deltaTime);
        var target = Enemy.NearestEnemy(m.Go.transform.position, 14f);
        if (target == null)
        {
            if (m.SwingTimer <= 0f)
                SetState(m, "Idle");
            return;
        }

        Vector3 dir = target.transform.position - m.Go.transform.position;
        dir.y = 0.7f - m.Go.transform.position.y;
        if (dir.sqrMagnitude > 0.01f)
        {
            m.Go.transform.position += dir.normalized * (8f * Time.deltaTime);
            Vector3 flat = dir;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f)
                m.Go.transform.rotation = Quaternion.Slerp(m.Go.transform.rotation, Quaternion.LookRotation(flat.normalized), Time.deltaTime * 9f);
        }

        if (m.SwingTimer <= 0f)
            SetState(m, "Run");

        m.Timer -= Time.deltaTime;
        if (m.Timer <= 0f && dir.sqrMagnitude < 1.5f)
        {
            m.Timer = 0.7f / Mathf.Max(0.3f, _stats.SummonSpeedMultiplier);
            m.SwingTimer = 0.25f;
            target.TakeDamage(8f * _stats.DamageMultiplier * _stats.SummonDamageMultiplier);
            SetState(m, "Attack");
            AudioManager.Instance?.Play("summon_attack", 0.55f, Random.Range(0.92f, 1.12f), 0.12f);
        }
    }

    void UpdateOrbit(Minion m)
    {
        float dt = Time.deltaTime;
        m.SwingTimer = Mathf.Max(0f, m.SwingTimer - dt);
        m.Angle += dt * (110f * _stats.SummonSpeedMultiplier);

        float radius = 1.7f;
        if (m.Kind == SummonKind.Dragon)
            radius = 2f;
        else if (m.Kind == SummonKind.Scythe)
            radius = 1.9f;

        // Пер-ворожий кулдаун: один дотик = один хіт.
        var cooldowns = m.HitCooldowns;
        var expired = new List<Enemy>();
        foreach (var pair in cooldowns)
        {
            if (pair.Value <= 0f)
                expired.Add(pair.Key);
            else
                cooldowns[pair.Key] = pair.Value - dt;
        }
        for (int i = 0; i < expired.Count; i++)
            cooldowns.Remove(expired[i]);

        Vector3 offset = Quaternion.Euler(0f, m.Angle, 0f) * Vector3.forward * radius;
        Vector3 targetPos = transform.position + offset +
                            Vector3.up * (1f + Mathf.Sin(Time.time * 2.2f + m.Angle * 0.02f) * 0.16f);
        m.Go.transform.position = Vector3.Lerp(m.Go.transform.position, targetPos, Mathf.Clamp01(dt * 16f));
        Quaternion targetRot = Quaternion.Euler(0f, m.Angle + 90f, 0f);
        m.Go.transform.rotation = Quaternion.Slerp(m.Go.transform.rotation, targetRot, Mathf.Clamp01(dt * 9f));

        m.Timer -= dt;

        float interval = 0.6f;
        float damage = 10f;
        int maxTargets = 2;
        if (m.Kind == SummonKind.Dragon) { interval = 0.45f; damage = 16f; }
        else if (m.Kind == SummonKind.Scythe) { interval = 0.5f; damage = 14f; maxTargets = 3; }
        else if (m.Kind == SummonKind.Butterfly) { interval = 0.8f; damage = 6f; }
        else if (m.Kind == SummonKind.FrenzyBat) { interval = 0.3f; damage = 7f; }

        // Урон тільки при реальному дотику до ворога. Радіус враховує розмір
        // ворога й довжину леза в коси; пошук іде виключно біля самого сумона.
        float contactRadius = 0.75f;
        if (m.Kind == SummonKind.Dragon) contactRadius = 1.15f;
        else if (m.Kind == SummonKind.Scythe) contactRadius = 1.25f;
        else if (m.Kind == SummonKind.Butterfly) contactRadius = 0.65f;

        var enemies = Enemy.NearestEnemies(m.Go.transform.position, 8, contactRadius + 1.1f);
        var touching = new List<Enemy>();
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || cooldowns.ContainsKey(enemy))
                continue;
            if (Vector3.Distance(enemy.transform.position, m.Go.transform.position) <= contactRadius + 0.55f)
                touching.Add(enemy);
        }

        bool engaged = touching.Count > 0;
        if (!engaged)
        {
            if (m.SwingTimer <= 0f)
                SetState(m, "Walk");
            AnimateScythe(m);
            return;
        }

        // Не перемикаємо Walk/Attack щокадрово: стан Attack тримається, поки
        // є ціль і триває замах. Це прибирає «дригання» анімації.
        SetState(m, "Attack");

        if (m.Timer > 0f)
        {
            AnimateScythe(m);
            return;
        }

        m.Timer = interval / Mathf.Max(0.3f, _stats.SummonSpeedMultiplier);
        m.SwingTimer = 0.28f;
        int hits = 0;
        float scaledDamage = damage * 1.2f * _stats.SummonDamageMultiplier * _stats.DamageMultiplier;
        for (int i = 0; i < touching.Count && hits < maxTargets; i++)
        {
            var enemy = touching[i];
            enemy.TakeDamage(scaledDamage);
            cooldowns[enemy] = interval;
            if (m.Kind == SummonKind.Butterfly)
                enemy.ApplyFreeze(1f);
            hits++;
        }
        if (hits > 0)
            AudioManager.Instance?.Play("summon_attack", 0.5f, Random.Range(0.92f, 1.12f), 0.12f);

        AnimateScythe(m);
    }

    static void AnimateScythe(Minion m)
    {
        if (m.Kind != SummonKind.Scythe)
            return;
        var model = m.Go.transform.Find("ReapersEdge");
        if (model == null)
            return;

        float swing = m.SwingTimer > 0f ? Mathf.Sin((1f - m.SwingTimer / 0.28f) * Mathf.PI) : 0f;
        model.localRotation = Quaternion.Euler(
            10f + swing * 62f,
            Time.time * 28f,
            -16f + swing * 48f);
        model.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 2.5f) * 0.07f, 0f);
    }

    static Color ColorFor(SummonKind kind)
    {
        switch (kind)
        {
            case SummonKind.Dragon: return new Color(1f, 0.5f, 0.15f);
            case SummonKind.Scythe: return new Color(0.75f, 0.85f, 1f);
            case SummonKind.Butterfly: return new Color(0.5f, 0.9f, 1f);
            case SummonKind.FrenzyBat: return new Color(0.9f, 0.4f, 0.8f);
            default: return new Color(0.5f, 0.85f, 1f);
        }
    }
}
