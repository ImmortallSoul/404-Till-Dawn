using System.Collections.Generic;
using UnityEngine;

sealed class EnemyProfile
{
    public EnemyKind Kind;
    public string Title;
    public float Hp;
    public float Speed;
    public float TouchDamage;
    public float Xp;
    public float Height;
    public float ModelHeight;
    public float Radius;
    public float Scale;
    public EnemyBehavior Behavior;
    public Color Color;
    public Color Emission;
    public PrimitiveType Body;
    public bool Flying;
    public bool Explodes;
    public bool Splits;
    public float BurnTick;
    public float CurseTick;
    public float HpBase;
    public float HpEnd;
    public float SpeedBase;
    public float SpeedEndBase;
    public float SpawnRemainingMinute;
    public bool SkeletonVisual;
}

public class Enemy : MonoBehaviour
{
    // Зріст гравця з PlayerAvatarController — орієнтир для наземних ворогів.
    const float PlayerHeight = 1.78f;
    // Моби вдвічі більші за розміром; щільність хвиль зменшена на 50%,
    // щоб більші тіла не перекривали арену суцільною стіною.
    const float EnemySizeMultiplier = 2f;

    static readonly List<Enemy> _all = new List<Enemy>();

    class EnemyDist
    {
        public Enemy Enemy;
        public float Dist;
    }

    EnemyProfile _profile;
    float _hp;
    float _maxHp;
    float _speed;
    float _touchDamage;
    float _touchRadius;
    int _xp;
    float _touchCooldown;
    float _attackTimer;
    float _spawnTimer;
    float _phaseTimer;
    int _phase;
    float _burnTimer;
    float _burnDps;
    float _freezeTimer;
    float _curseTimer;
    float _curseDps;
    float _hitFlash;
    float _telegraph;
    float _telegraphDuration;
    float _hover;
    float _chargeTimer;
    Vector3 _chargeDir;
    bool _charging;
    bool _dead;
    bool _grounded = true;
    EnemyRagdoll _ragdoll;

    CharacterController _controller;
    Transform _visual;
    EnemyModelRig _rig;
    CharacterAnimationController _animations;
    Vector3 _baseScale;
    Vector3 _lastVisualPos;
    float _spawnAnimTimer;
    float _hitAnimTimer;
    float _attackAnimTimer;
    float _deathTimer;
    float _deathDuration;
    float _deathElapsed;
    Vector3 _deathStartScale;
    Vector3 _deathStartPos;
    Quaternion _deathStartRot;

    public EnemyKind Kind { get { return _profile.Kind; } }
    public EnemyTier Tier { get; private set; }

    // Хвилин, що лишились до світанку на момент спавну. Чим менше — тим сильніший моб.
    public float SpawnRemainingMinute { get; private set; }
    public float SpawnMaxHp { get { return _maxHp; } }
    public float Health01 { get { return _maxHp <= 0f ? 0f : Mathf.Clamp01(_hp / _maxHp); } }
    public string Title { get { return _profile.Title; } }
    public bool IsTelegraphing { get { return _telegraph > 0f; } }
    public float Telegraph01
    {
        get
        {
            if (_telegraph <= 0f || _telegraphDuration <= 0f)
                return 0f;
            return 1f - Mathf.Clamp01(_telegraph / _telegraphDuration);
        }
    }

    public static int Alive { get { return _all.Count; } }
    public static Enemy PrimaryBoss
    {
        get
        {
            Enemy best = null;
            for (int i = 0; i < _all.Count; i++)
            {
                var e = _all[i];
                if (e != null && e.Tier == EnemyTier.Boss && e.Health01 > 0f)
                {
                    best = e;
                    break;
                }
            }
            return best;
        }
    }

    public static void ResetAlive()
    {
        _all.Clear();
    }

    void SetTelegraph(float seconds)
    {
        if (seconds <= _telegraph)
            return;
        _telegraph = seconds;
        _telegraphDuration = seconds;
    }

    void OnEnable()
    {
        if (!_all.Contains(this))
            _all.Add(this);
    }

    void OnDisable()
    {
        _all.Remove(this);
    }

    public static Enemy Spawn(Vector3 position, bool brute)
    {
        return Spawn(position, brute ? EnemyKind.Lurker : EnemyKind.Tentacle, EnemyTier.Small);
    }

    public static Enemy Spawn(Vector3 position, EnemyKind kind)
    {
        return Spawn(position, kind, IsBossKind(kind) ? EnemyTier.Boss : EnemyTier.Small);
    }

    public static Enemy Spawn(Vector3 position, EnemyKind kind, EnemyTier tier)
    {
        return Spawn(position, kind, tier, false);
    }

    public static Enemy Spawn(Vector3 position, EnemyKind kind, EnemyTier tier, bool skeletonVisual)
    {
        if (IsBossKind(kind))
            tier = EnemyTier.Boss;

        float t = GameSession.Instance != null ? GameSession.Instance.Progress : 0f;
        // Таймер іде назад: 15 хвилин до світанку — пізніше, ніж 17.
        float remainingMinute = GameSession.Instance != null
            ? GameSession.Instance.Remaining / 60f
            : GameSession.RunDuration / 60f;
        var profile = BuildProfile(kind, tier, t, remainingMinute);
        profile.SkeletonVisual = skeletonVisual;
        if (skeletonVisual && tier == EnemyTier.Boss)
        {
            profile.Hp *= 1.35f;
            profile.TouchDamage *= 1.2f;
            profile.Speed *= 1.08f;
            profile.Xp *= 2;
        }

        var go = new GameObject(kind.ToString());
        go.transform.position = new Vector3(position.x, 0f, position.z);

        var cc = go.AddComponent<CharacterController>();
        // Фінальний зріст = ModelHeight × Scale. CharacterController має збігатися
        // з реальною моделлю, бо Scale застосовується до візуального контейнера.
        float visualFactor = profile.ModelHeight / Mathf.Max(0.1f, profile.Height);
        float finalHeight = profile.ModelHeight * profile.Scale;
        cc.height = finalHeight;
        cc.radius = profile.Radius * Mathf.Clamp(visualFactor, 0.8f, 1.8f) *
                    Mathf.Clamp(profile.Scale, 0.8f, 1.6f) * EnemySizeMultiplier;
        cc.center = new Vector3(0f, finalHeight * 0.5f, 0f);
        cc.skinWidth = 0.08f;
        cc.minMoveDistance = 0f;

        var enemy = go.AddComponent<Enemy>();
        enemy._profile = profile;
        enemy._controller = cc;
        enemy.Tier = tier;
        enemy.SpawnRemainingMinute = profile.SpawnRemainingMinute;
        enemy._hp = profile.Hp;
        enemy._maxHp = profile.Hp;
        enemy._speed = profile.Speed;
        enemy._touchDamage = profile.TouchDamage;
        enemy._touchRadius = (profile.Radius + 0.95f) * EnemySizeMultiplier;
        enemy._xp = Mathf.Max(1, Mathf.RoundToInt(profile.Xp));
        enemy._attackTimer = Random.Range(0.8f, 2.4f);
        enemy._spawnTimer = 3f;
        enemy._phaseTimer = 2.5f;
        enemy._hover = Random.Range(0f, 6f);
        enemy._baseScale = Vector3.one * profile.Scale;

        enemy.BuildVisual();

        if (profile.Explodes)
            enemy._spawnTimer = 0.2f;

        return enemy;
    }

    void BuildVisual()
    {
        _visual = new GameObject("Visual").transform;
        _visual.SetParent(transform, false);

        // Усі вороги, включно зі звичайним натовпом, використовують готові
        // імпортовані моделі з анімаціями. Процедурні меші — лише fallback.
        var visual = CharacterVisualCatalog.CreateEnemy(_visual, _profile.Kind, Tier,
            _profile.ModelHeight, _profile.Color, _profile.SkeletonVisual);
        if (visual != null)
            _animations = visual.Animations;

        if (_animations == null)
        {
            _rig = EnemyModelBuilder.Build(_visual, _profile.Kind, Tier,
                _profile.ModelHeight, _profile.Radius, _profile.Color, _profile.Emission);
        }
        else
        {
            _spawnAnimTimer = 0.6f;
        }

        _lastVisualPos = _visual.position;
        _visual.localScale = _baseScale;
    }

    void Update()
    {
        if (GameSession.Instance == null || !GameSession.Instance.AllowsGameplay)
            return;

        float dt = Time.deltaTime;

        if (_dead)
        {
            if (_deathTimer > 0f)
            {
                _deathTimer -= dt;
                _deathElapsed += dt;
                // Ragdoll керується фізикою окремого компонента — процедурний
                // «завал» застосовуємо лише коли фізичну смерть не створено.
                if (_ragdoll == null)
                {
                    float k = Mathf.Clamp01(_deathElapsed / Mathf.Max(0.01f, _deathDuration));
                    float ease = k * k * (3f - 2f * k);

                    if (_visual != null)
                    {
                        _visual.localScale = _deathStartScale * (1f - ease * 0.92f);
                        Vector3 p = _deathStartPos;
                        p.y -= ease * _profile.ModelHeight * 0.55f;
                        _visual.localPosition = p;
                        _visual.localRotation = Quaternion.Slerp(_deathStartRot,
                            Quaternion.Euler(78f, _deathStartRot.eulerAngles.y, 14f), ease);
                    }
                }

                if (_deathTimer <= 0f)
                    Destroy(gameObject);
            }
            return;
        }
        UpdateStatuses(dt);

        if (_dead)
            return;

        var player = GameSession.Instance.Player;
        if (player == null)
            return;

        Vector3 to = player.transform.position - transform.position;
        to.y = 0f;
        float dist = to.magnitude;
        Vector3 dir = dist > 0.01f ? to / dist : Vector3.forward;

        if (dist > 0.3f)
            _visual.rotation = Quaternion.Slerp(_visual.rotation, Quaternion.LookRotation(dir, Vector3.up), dt * 8f);

        float speed = _speed;
        if (_freezeTimer > 0f)
            speed *= 0.35f;

        if (_profile.Behavior == EnemyBehavior.Ranged && _attackTimer < 0.5f)
            SetTelegraph(0.5f);
        if (_profile.Behavior == EnemyBehavior.Exploder && dist < _profile.Radius + 2.2f)
            SetTelegraph(0.45f);

        switch (_profile.Behavior)
        {
            case EnemyBehavior.Ranged:
                if (dist > 7f)
                    Move(dir, speed, dt);
                else if (dist < 4.5f)
                    Move(-dir, speed * 0.6f, dt);
                ShootAtPlayer(dir, dt);
                break;
            case EnemyBehavior.Exploder:
                Move(dir, speed, dt);
                if (dist < _profile.Radius + 1.25f)
                    Detonate(player);
                break;
            case EnemyBehavior.Spawner:
                UpdateSpawner(dt);
                break;
            case EnemyBehavior.Flying:
                MoveFlying(dir, speed, dt);
                break;
            case EnemyBehavior.BossHastur:
                UpdateBoss(dir, dist, dt, 12, ProjectileStyle.Orb, ProjectileBehavior.EnemyOrb);
                break;
            case EnemyBehavior.BossShoggoth:
                UpdateBoss(dir, dist, dt, 8, ProjectileStyle.Flame, ProjectileBehavior.EnemyCurse);
                break;
            case EnemyBehavior.BossReaper:
                UpdateBoss(dir, dist, dt, 6, ProjectileStyle.Knife, ProjectileBehavior.EnemyHoming);
                break;
            case EnemyBehavior.BossYog:
                UpdateBoss(dir, dist, dt, 16, ProjectileStyle.Orb, ProjectileBehavior.EnemyOrb);
                break;
            case EnemyBehavior.BossShub:
                UpdateBoss(dir, dist, dt, 10, ProjectileStyle.Spore, ProjectileBehavior.EnemyCurse);
                break;
            default:
                Move(dir, speed, dt);
                break;
        }

        _touchCooldown -= dt;
        if (_touchCooldown <= 0f && dist < _touchRadius)
        {
            if (player.TryTakeDamage(_touchDamage, transform.position))
            {
                _touchCooldown = 0.75f;
                _attackAnimTimer = 0.3f;
            }
        }

        _hitFlash = Mathf.Max(0f, _hitFlash - dt * 5f);
        _telegraph = Mathf.Max(0f, _telegraph - dt);
        float pulse = 1f + _hitFlash * 0.16f + Mathf.Sin(Time.time * 5f + _hover) * 0.025f;
        _visual.localScale = _baseScale * pulse;

        _spawnAnimTimer = Mathf.Max(0f, _spawnAnimTimer - dt);
        _hitAnimTimer = Mathf.Max(0f, _hitAnimTimer - dt);
        _attackAnimTimer = Mathf.Max(0f, _attackAnimTimer - dt);

        if (_animations != null)
        {
            float moved = dt > 0f ? Vector3.Distance(_visual.position, _lastVisualPos) / dt : 0f;
            _lastVisualPos = _visual.position;

            if (_spawnAnimTimer > 0f)
                _animations.SetState("Spawn", 0.12f);
            else if (_hitAnimTimer > 0f)
                _animations.SetState("Hit", 0.08f);
            else if (_attackAnimTimer > 0f || _telegraph > 0f)
                _animations.SetState("Attack", 0.1f);
            else if (moved > 0.4f)
                _animations.SetState(_speed > 2.4f ? "Run" : "Walk", 0.15f);
            else
                _animations.SetState("Idle", 0.18f);
        }
        else if (_rig != null)
        {
            float energy = Mathf.Clamp(0.45f + _speed / 220f + _hitFlash * 0.6f, 0.45f, 1.5f);
            _rig.Animate(Time.time + _hover, energy);
        }
    }

    void UpdateStatuses(float dt)
    {
        if (_burnTimer > 0f)
        {
            _burnTimer -= dt;
            _burnDps -= dt;
            if (_burnDps <= 0f)
            {
                _burnDps = 0.5f;
                TakeDamage(_profile.BurnTick);
                Projectile.SpawnBurst(transform.position + Vector3.up * _profile.Height * 0.5f, new Color(1f, 0.4f, 0.1f), 2);
            }
        }

        if (_curseTimer > 0f)
        {
            _curseTimer -= dt;
            _curseDps -= dt;
            if (_curseDps <= 0f)
            {
                _curseDps = 0.5f;
                TakeDamage(_profile.CurseTick);
            }
        }

        if (_freezeTimer > 0f)
            _freezeTimer -= dt;
    }

    void Move(Vector3 dir, float speed, float dt)
    {
        if (_charging)
        {
            _chargeTimer -= dt;
            Vector3 charge = _chargeDir * speed * 3.2f;
            charge.y = _grounded ? -2f : -8f;
            CollisionFlags flags = _controller.Move(charge * dt);
            _grounded = (flags & CollisionFlags.Below) != 0;
            if (_chargeTimer <= 0f)
                _charging = false;
            return;
        }

        Vector3 motion = dir * speed;
        motion.y = _grounded ? -2f : -8f;
        CollisionFlags hit = _controller.Move(motion * dt);
        _grounded = (hit & CollisionFlags.Below) != 0;
    }

    void MoveFlying(Vector3 dir, float speed, float dt)
    {
        transform.position += dir * speed * dt;
        float targetY = _profile.Height * 0.8f + Mathf.Sin(Time.time * 2.5f + _hover) * 0.35f;
        var p = transform.position;
        p.y = Mathf.Lerp(p.y, targetY - _profile.Height * 0.5f, dt * 4f);
        transform.position = p;
    }

    void ShootAtPlayer(Vector3 dir, float dt)
    {
        _attackTimer -= dt;
        if (_attackTimer > 0f)
            return;

        _attackTimer = Tier == EnemyTier.Small ? Random.Range(2.4f, 3.8f) : Random.Range(1.6f, 2.6f);
        _attackAnimTimer = 0.35f;
        Vector3 origin = transform.position + Vector3.up * (_profile.Height * 0.6f) + dir * _profile.Radius;
        Projectile.Spawn(origin, Quaternion.LookRotation(dir, Vector3.up),
            _profile.TouchDamage * 0.8f, 10f, 0, 3f,
            ProjectileStyle.Orb, ProjectileBehavior.EnemyOrb, false, 0);
    }

    void UpdateSpawner(float dt)
    {
        _spawnTimer -= dt;
        if (_spawnTimer > 0f)
            return;

        _spawnTimer = Tier == EnemyTier.Boss ? 5f : 8f;
        int count = Tier == EnemyTier.Boss ? 4 : 2;
        for (int i = 0; i < count; i++)
        {
            float a = i * (360f / count) + Random.Range(-20f, 20f);
            Vector3 offset = Quaternion.Euler(0f, a, 0f) * Vector3.forward * (_profile.Radius + 1.4f);
            Enemy.Spawn(transform.position + offset, EnemyKind.Spawnling);
        }
        Projectile.SpawnBurst(transform.position + Vector3.up, new Color(0.5f, 1f, 0.4f), 8);
    }

    void UpdateBoss(Vector3 dir, float dist, float dt, int burstCount, ProjectileStyle style, ProjectileBehavior behavior)
    {
        float hp = Health01;
        int newPhase = hp > 0.66f ? 0 : hp > 0.33f ? 1 : 2;
        if (newPhase != _phase)
        {
            _phase = newPhase;
            _speed *= 1.16f;
            GameSession.Instance?.ShowNotice(_profile.Title + " — фаза " + (_phase + 1), 2f);
            Projectile.SpawnBurst(transform.position + Vector3.up * _profile.Height, _profile.Emission, 16);
        }

        if (dist > 5f || _profile.Behavior == EnemyBehavior.BossShoggoth)
            Move(dir, _speed * (_profile.Flying ? 1f : 1f), dt);

        if (_profile.Behavior == EnemyBehavior.BossShoggoth && _attackTimer <= 0f)
        {
            _attackTimer = 4.2f - _phase * 0.5f;
            _charging = true;
            _chargeTimer = 1.1f;
            _chargeDir = dir;
            SetTelegraph(1.1f);
        }

        _attackTimer -= dt;
        _phaseTimer -= dt;
        if (_attackTimer <= 0.6f && _attackTimer > 0f)
            SetTelegraph(0.6f);

        if (_attackTimer <= 0f)
        {
            _attackTimer = Mathf.Max(1.4f, 3.2f - _phase * 0.55f);
            FireBurst(dir, burstCount, style, behavior);
        }

        if (_phaseTimer <= 0f)
        {
            _phaseTimer = 9f - _phase * 2f;
            int minions = 2 + _phase * 2;
            for (int i = 0; i < minions; i++)
            {
                float a = i * (360f / minions) + Random.Range(-15f, 15f);
                Vector3 offset = Quaternion.Euler(0f, a, 0f) * Vector3.forward * (_profile.Radius + 2f);
                EnemyKind minion = _profile.Behavior == EnemyBehavior.BossShub
                    ? EnemyKind.Spawnling
                    : _profile.Behavior == EnemyBehavior.BossShoggoth
                        ? EnemyKind.Sploder
                        : EnemyKind.Eyebat;
                Enemy.Spawn(transform.position + offset, minion);
            }
        }
    }

    void FireBurst(Vector3 dir, int count, ProjectileStyle style, ProjectileBehavior behavior)
    {
        Vector3 origin = transform.position + Vector3.up * (_profile.Height * 0.6f);
        float offset = _phase * 15f;

        if (behavior == ProjectileBehavior.EnemyHoming)
        {
            for (int i = 0; i < count; i++)
            {
                Quaternion rot = Quaternion.Euler(0f, offset + i * (360f / count), 0f);
                Projectile.Spawn(origin + rot * Vector3.forward * _profile.Radius, rot,
                    _profile.TouchDamage * 0.75f, 13f, 0, 3.5f, style, behavior, false, 0);
            }
            return;
        }

        for (int i = 0; i < count; i++)
        {
            float angle = offset + i * (360f / count);
            Quaternion rot = dir.sqrMagnitude > 0.01f
                ? Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(0f, angle - 180f / Mathf.Max(1, count - 1) * (count - 1) * 0.5f, 0f)
                : Quaternion.Euler(0f, angle, 0f);
            Projectile.Spawn(origin + rot * Vector3.forward * _profile.Radius, rot,
                _profile.TouchDamage * 0.7f, 9f + _phase, 0, 4f, style, behavior, false, 0);
        }
    }

    public void ApplyBurn(float dps, float duration)
    {
        if (_dead || dps <= 0f)
            return;
        _burnDps = 0.5f;
        _profile.BurnTick = dps;
        _burnTimer = Mathf.Max(_burnTimer, duration);
        _hitFlash = Mathf.Max(_hitFlash, 0.4f);
    }

    public void ApplyFreeze(float duration)
    {
        if (_dead)
            return;
        _freezeTimer = Mathf.Max(_freezeTimer, duration);
        _hitFlash = Mathf.Max(_hitFlash, 0.35f);
    }

    public void ApplyCurse(float dps, float duration)
    {
        if (_dead)
            return;
        _curseDps = 0.5f;
        _profile.CurseTick = dps;
        _curseTimer = Mathf.Max(_curseTimer, duration);
    }

    public void StrikeLightning(float amount)
    {
        if (_dead)
            return;
        TakeDamage(amount);
        DamageNumber.Spawn(transform.position + Vector3.up * (_profile.Height + 0.2f), amount, true);
        Projectile.SpawnBurst(transform.position + Vector3.up * _profile.Height * 0.6f, new Color(0.5f, 0.9f, 1f), 7);

        var nearby = NearestEnemies(transform.position, 3, 4.5f);
        for (int i = 0; i < nearby.Count; i++)
        {
            if (nearby[i] == this)
                continue;
            nearby[i].TakeDamage(amount * 0.45f);
        }
    }

    public void TakeDamage(float amount)
    {
        if (_dead || amount <= 0f)
            return;

        _hp -= amount;
        _hitFlash = 1f;
        _hitAnimTimer = 0.2f;
        AudioManager.Instance?.Play("enemy_hit", 0.6f, Random.Range(0.9f, 1.12f), 0.06f);

        if (amount >= 4f && _controller != null && GameSession.Instance != null && GameSession.Instance.Player != null)
        {
            Vector3 away = transform.position - GameSession.Instance.Player.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude > 0.01f)
            {
                float resistance = Tier == EnemyTier.Small ? 1f : Tier == EnemyTier.Elite ? 0.35f : 0.12f;
                float push = Mathf.Min(0.24f, amount * 0.004f) * resistance;
                _controller.Move(away.normalized * push);
            }
        }

        if (_hp <= 0f)
            Die();
    }

    void Detonate(PlayerStats player)
    {
        float dist = Vector3.Distance(player.transform.position, transform.position);
        if (dist < 3f)
            player.TryTakeDamage(_profile.TouchDamage * 1.3f);
        Projectile.SpawnBurst(transform.position + Vector3.up * 0.5f, _profile.Emission, 14);
        _hp = 0f;
        Die(true);
    }

    void Die(bool silent = false)
    {
        if (_dead)
            return;
        _dead = true;
        AudioManager.Instance?.Play("enemy_death", Tier == EnemyTier.Boss ? 1f : 0.62f,
            Tier == EnemyTier.Boss ? 0.82f : Random.Range(0.92f, 1.12f), 0.08f);

        if (!silent && GameSession.Instance != null)
        {
            GameSession.Instance.RegisterKill();
            if (Tier == EnemyTier.Elite)
                GameSession.Instance.ShowNotice("ЕЛІТА ПОВАЛЕНА: " + _profile.Title, 2.5f);
            else if (Tier == EnemyTier.Boss)
                GameSession.Instance.ShowNotice("БОС ПОВАЛЕНИЙ: " + _profile.Title, 3.5f);
        }

        if (GameSession.Instance != null && GameSession.Instance.Player != null)
            GameSession.Instance.Player.OnKill(this);

        Vector3 drop = transform.position;
        drop.y = 0.35f;

        int orbs = Mathf.Min(_xp, 25);
        for (int i = 0; i < orbs; i++)
        {
            Vector3 offset = i == 0
                ? Vector3.zero
                : new Vector3(Random.Range(-0.45f, 0.45f), 0f, Random.Range(-0.45f, 0.45f));
            XpOrb.Spawn(drop + offset);
        }

        if (_profile.Explodes && !silent)
        {
            var player = GameSession.Instance != null ? GameSession.Instance.Player : null;
            if (player != null && Vector3.Distance(player.transform.position, transform.position) < 2.8f)
                player.TryTakeDamage(_profile.TouchDamage);
            Projectile.SpawnBurst(drop + Vector3.up * 0.4f, _profile.Emission, 12);
        }

        if (_profile.Splits)
        {
            int pieces = Tier == EnemyTier.Boss ? 5 : 2;
            for (int i = 0; i < pieces; i++)
            {
                float a = i * (360f / pieces) + Random.Range(-20f, 20f);
                Vector3 offset = Quaternion.Euler(0f, a, 0f) * Vector3.forward * (_profile.Radius + 0.8f);
                Enemy.Spawn(transform.position + offset, EnemyKind.Sploder);
            }
        }

        if (Tier == EnemyTier.Elite)
        {
            HealOrb.Spawn(drop + new Vector3(0.6f, 0f, 0f));
            if (Random.value < 0.3f)
                ChestPickup.Spawn(drop + new Vector3(-0.6f, 0f, 0f));
        }
        else if (Tier == EnemyTier.Boss)
        {
            HealOrb.Spawn(drop + new Vector3(-1f, 0f, 0f));
            HealOrb.Spawn(drop + new Vector3(1f, 0f, 0f));
            ChestPickup.Spawn(drop + new Vector3(0f, 0f, -1f));
            TomePickup.Spawn(drop + new Vector3(0f, 0f, 1f));
        }
        else if (Random.value < 0.012f)
        {
            ChestPickup.Spawn(drop);
        }

        if (_animations != null)
            _animations.SetState("Death", 0.05f);

        _deathDuration = Tier == EnemyTier.Boss ? 5.5f : 3.2f;
        _deathTimer = _deathDuration;
        _deathElapsed = 0f;
        if (_visual != null)
        {
            _deathStartScale = _visual.localScale;
            _deathStartPos = _visual.localPosition;
            _deathStartRot = _visual.localRotation;

            Vector3 away = Vector3.forward;
            if (GameSession.Instance != null && GameSession.Instance.Player != null)
            {
                away = transform.position - GameSession.Instance.Player.transform.position;
                away.y = 0.25f;
            }
            if (away.sqrMagnitude < 0.01f)
                away = Vector3.forward;

            Vector3 impulse = away.normalized * (Tier == EnemyTier.Boss ? 4f : 6.5f) + Vector3.up * 2.2f;
            _ragdoll = EnemyRagdoll.Spawn(this, _visual, impulse, _profile.ModelHeight * _profile.Scale);
            if (_ragdoll != null && _animations != null)
                _animations.enabled = false;
        }

        if (_controller != null)
            _controller.enabled = false;
    }

    public static int Count(EnemyKind kind)
    {
        int count = 0;
        for (int i = 0; i < _all.Count; i++)
        {
            if (_all[i] != null && _all[i].Kind == kind && !_all[i]._dead)
                count++;
        }
        return count;
    }

    public static Enemy NearestEnemy(Vector3 position, float radius)
    {
        Enemy best = null;
        float bestDist = radius * radius;
        for (int i = 0; i < _all.Count; i++)
        {
            var e = _all[i];
            if (e == null || e._dead)
                continue;
            float d = (e.transform.position - position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = e;
            }
        }
        return best;
    }

    public static List<Enemy> NearestEnemies(Vector3 position, int count, float radius)
    {
        var result = new List<Enemy>();
        if (count <= 0)
            return result;

        var found = new List<EnemyDist>();
        float radiusSqr = radius * radius;
        for (int i = 0; i < _all.Count; i++)
        {
            var e = _all[i];
            if (e == null || e._dead)
                continue;
            float d = (e.transform.position - position).sqrMagnitude;
            if (d <= radiusSqr)
                found.Add(new EnemyDist { Enemy = e, Dist = d });
        }

        found.Sort(delegate (EnemyDist a, EnemyDist b) { return a.Dist.CompareTo(b.Dist); });
        for (int i = 0; i < found.Count && i < count; i++)
            result.Add(found[i].Enemy);
        return result;
    }

    static bool IsBossKind(EnemyKind kind)
    {
        return kind == EnemyKind.Shoggoth || kind == EnemyKind.Reaper || kind == EnemyKind.Hastur
               || kind == EnemyKind.Yog || kind == EnemyKind.ShubNiggurath;
    }

    static EnemyProfile BuildProfile(EnemyKind kind, EnemyTier tier, float t, float remainingMinute)
    {
        var p = new EnemyProfile();
        p.Kind = kind;
        p.Scale = 1f;
        p.Flying = false;
        p.Explodes = false;
        p.Splits = false;
        p.BurnTick = 0f;
        p.CurseTick = 0f;

        float darkness = 1f + GameSession.Darkness * 0.22f;

        switch (kind)
        {
            case EnemyKind.Sploder:
                Set(p, "Сплодер", 30f, 78f, 2.4f, 1.3f, 0.6f, 1f, 1f, EnemyBehavior.Exploder,
                    new Color(0.75f, 0.18f, 0.08f), new Color(2f, 0.5f, 0.1f), PrimitiveType.Sphere, 0.45f);
                p.Explodes = true;
                break;
            case EnemyKind.Lurker:
                Set(p, "Леркер", 46f, 125f, 3.9f, 1.8f, 0.7f, 1f, 1.3f, EnemyBehavior.Melee,
                    new Color(0.28f, 0.08f, 0.42f), new Color(0.7f, 0.15f, 1.3f), PrimitiveType.Cube, 0.55f);
                break;
            case EnemyKind.Lamprey:
                Set(p, "Лампрей", 34f, 95f, 4.5f, 2f, 0.6f, 1f, 0.8f, EnemyBehavior.Flying,
                    new Color(0.12f, 0.42f, 0.45f), new Color(0.2f, 1.4f, 1.5f), PrimitiveType.Cylinder, 0.4f);
                p.Flying = true;
                break;
            case EnemyKind.Spawnling:
                Set(p, "Спавнлінг", 14f, 38f, 3.6f, 1.6f, 0.4f, 1f, 0.6f, EnemyBehavior.Melee,
                    new Color(0.3f, 0.55f, 0.22f), new Color(0.5f, 1.2f, 0.3f), PrimitiveType.Sphere, 0.28f);
                p.Scale = 0.72f;
                break;
            case EnemyKind.Cultist:
                Set(p, "Культист", 62f, 160f, 2.5f, 1f, 0.5f, 2f, 1.7f, EnemyBehavior.Ranged,
                    new Color(0.25f, 0.12f, 0.38f), new Color(0.8f, 0.25f, 1.3f), PrimitiveType.Capsule, 0.45f);
                break;
            case EnemyKind.Squid:
                Set(p, "Сквід", 38f, 105f, 2.9f, 1.2f, 0.5f, 1f, 1.1f, EnemyBehavior.Ranged,
                    new Color(0.1f, 0.38f, 0.5f), new Color(0.2f, 1.2f, 1.7f), PrimitiveType.Sphere, 0.5f);
                break;
            case EnemyKind.BigSploder:
                Set(p, "Великий сплодер", 120f, 280f, 2.1f, 1f, 1f, 3f, 1.6f, EnemyBehavior.Exploder,
                    new Color(0.85f, 0.3f, 0.05f), new Color(2.4f, 0.8f, 0.1f), PrimitiveType.Sphere, 0.8f);
                p.Explodes = true;
                p.Splits = true;
                break;
            case EnemyKind.Pumpkin:
                Set(p, "Гарбуз", 32f, 90f, 2.7f, 1.2f, 0.5f, 1f, 1.1f, EnemyBehavior.Melee,
                    new Color(0.85f, 0.38f, 0.05f), new Color(2f, 0.8f, 0.1f), PrimitiveType.Sphere, 0.5f);
                break;
            case EnemyKind.Ghost:
                Set(p, "Привид", 26f, 80f, 4.1f, 1.8f, 0.5f, 1f, 1.2f, EnemyBehavior.Flying,
                    new Color(0.55f, 0.8f, 0.9f), new Color(0.5f, 1.5f, 1.9f), PrimitiveType.Sphere, 0.4f);
                p.Flying = true;
                break;
            case EnemyKind.Eyebat:
                Set(p, "Ококажан", 24f, 65f, 5.3f, 2f, 0.45f, 1f, 0.6f, EnemyBehavior.Flying,
                    new Color(0.55f, 0.12f, 0.5f), new Color(1.5f, 0.3f, 1.6f), PrimitiveType.Sphere, 0.35f);
                p.Flying = true;
                break;
            case EnemyKind.Witch:
                Set(p, "Відьма", 95f, 220f, 2.9f, 1.2f, 0.7f, 3f, 1.8f, EnemyBehavior.Ranged,
                    new Color(0.4f, 0.1f, 0.55f), new Color(1.4f, 0.3f, 2f), PrimitiveType.Capsule, 0.5f);
                break;
            case EnemyKind.Elder:
                Set(p, "Старійшина", 190f, 450f, 2.4f, 1f, 1f, 5f, 2.2f, EnemyBehavior.Ranged,
                    new Color(0.6f, 0.55f, 0.4f), new Color(1.6f, 1.4f, 0.6f), PrimitiveType.Capsule, 0.7f);
                break;
            case EnemyKind.Spawner:
                Set(p, "Розплідник", 170f, 400f, 0.6f, 0f, 0.4f, 5f, 1.5f, EnemyBehavior.Spawner,
                    new Color(0.3f, 0.6f, 0.25f), new Color(0.6f, 1.6f, 0.4f), PrimitiveType.Sphere, 0.8f);
                break;
            case EnemyKind.WingedTerror:
                Set(p, "Крилатий жах", 140f, 340f, 5.5f, 2f, 1f, 5f, 1.3f, EnemyBehavior.Flying,
                    new Color(0.6f, 0.08f, 0.1f), new Color(2f, 0.2f, 0.2f), PrimitiveType.Capsule, 0.7f);
                p.Flying = true;
                break;
            case EnemyKind.Shoggoth:
                Set(p, "Шоггот", 900f, 1200f, 2.2f, 1f, 1.4f, 20f, 2.4f, EnemyBehavior.BossShoggoth,
                    new Color(0.28f, 0.12f, 0.45f), new Color(1.2f, 0.3f, 2f), PrimitiveType.Sphere, 1.4f);
                p.Splits = true;
                break;
            case EnemyKind.Reaper:
                Set(p, "Жнець", 1000f, 1300f, 3f, 1.2f, 1.6f, 20f, 2.6f, EnemyBehavior.BossReaper,
                    new Color(0.12f, 0.08f, 0.14f), new Color(1.8f, 0.1f, 0.25f), PrimitiveType.Capsule, 1.1f);
                break;
            case EnemyKind.Hastur:
                Set(p, "Гастур", 1200f, 1500f, 2f, 1f, 1.5f, 25f, 2.8f, EnemyBehavior.BossHastur,
                    new Color(0.12f, 0.35f, 0.22f), new Color(0.4f, 2f, 0.7f), PrimitiveType.Sphere, 1.4f);
                break;
            case EnemyKind.Yog:
                Set(p, "Йог-Сотот", 1500f, 1800f, 1.8f, 0.8f, 1.8f, 30f, 3.2f, EnemyBehavior.BossYog,
                    new Color(0.55f, 0.25f, 0.05f), new Color(2.2f, 0.9f, 0.15f), PrimitiveType.Sphere, 1.7f);
                break;
            case EnemyKind.ShubNiggurath:
                Set(p, "Шаб-Ніггурат", 1800f, 2000f, 2f, 0.9f, 1.7f, 35f, 3f, EnemyBehavior.BossShub,
                    new Color(0.5f, 0.22f, 0.42f), new Color(2f, 0.5f, 1.5f), PrimitiveType.Sphere, 1.6f);
                break;
            default:
                Set(p, "Щупальце", 26f, 75f, 2.6f, 1.2f, 0.5f, 1f, 1.5f, EnemyBehavior.Melee,
                    new Color(0.16f, 0.38f, 0.18f), new Color(0.3f, 1.1f, 0.3f), PrimitiveType.Cylinder, 0.42f);
                break;
        }

        ApplyTimeCurve(p, t);

        // Лічильник іде назад: менше хвилин до світанку = пізніший і сильніший спавн.
        p.SpawnRemainingMinute = remainingMinute;
        float pressure = Mathf.Clamp01(1f - remainingMinute / (GameSession.RunDuration / 60f));
        // Натовп помітно сильніший: більше HP, відчутний контактний урон.
        p.Hp *= darkness * (1f + pressure * 1.6f) * 1.8f;
        p.TouchDamage *= (1f + t * 0.5f) * (1f + pressure * 1.1f) * 1.35f;
        p.Speed *= (1f + pressure * 0.28f) * 1.08f;
        p.Xp *= (1f + pressure) * 1.1f;
        p.Scale *= 1f + pressure * 0.1f;

        if (tier == EnemyTier.Elite)
        {
            p.Hp *= 3.6f;
            p.Speed *= 1.12f;
            p.TouchDamage *= 1.5f;
            p.Xp *= 6;
            p.Scale *= 1.45f;
            p.Emission = Color.Lerp(p.Emission, new Color(2.2f, 1.6f, 0.2f), 0.45f);
        }
        else if (tier == EnemyTier.Boss && !IsBossKind(kind))
        {
            p.Hp *= 10f;
            p.Speed *= 1.05f;
            p.TouchDamage *= 1.8f;
            p.Xp *= 12;
            p.Scale *= 1.8f;
            p.Emission = Color.Lerp(p.Emission, new Color(2.4f, 0.2f, 0.2f), 0.5f);
        }
        else if (tier == EnemyTier.Boss)
        {
            p.Scale *= 1.05f;
        }

        NormalizeModelHeight(p, kind, tier);
        // Збільшуємо вже нормалізований меш: інакше NormalizeModelHeight
        // компенсувала б новий розмір назад через поділ на Scale.
        p.ModelHeight *= EnemySizeMultiplier;
        p.Height *= EnemySizeMultiplier;

        p.Hp = Mathf.Max(8f, p.Hp);
        p.BurnTick = Mathf.Max(1f, p.Hp * 0.012f);
        p.CurseTick = Mathf.Max(1f, p.Hp * 0.01f);
        return p;
    }

    /// <summary>
    /// Наземні людиноподібні вороги мають бути зростом гравця. Дрібні/літаючі
    /// лишаються меншими навмисно, боси — більшими. ModelHeight використовується
    /// і для меша, і для CharacterController, щоб колайдер збігався з видом.
    /// </summary>
    static void NormalizeModelHeight(EnemyProfile p, EnemyKind kind, EnemyTier tier)
    {
        // CreateEnemy підганяє меш до ModelHeight, а потім усе помножується
        // на Scale. Ділимо на Scale, щоб звичайний наземний моб мав зріст гравця.
        float scale = Mathf.Max(0.1f, p.Scale);
        bool eliteOrBoss = tier == EnemyTier.Elite || tier == EnemyTier.Boss;

        switch (kind)
        {
            case EnemyKind.Cultist:
            case EnemyKind.Witch:
            case EnemyKind.Elder:
            case EnemyKind.Spawner:
            case EnemyKind.Pumpkin:
            case EnemyKind.Lurker:
            case EnemyKind.Reaper:
            case EnemyKind.Shoggoth:
            case EnemyKind.Hastur:
            case EnemyKind.Yog:
            case EnemyKind.ShubNiggurath:
                p.ModelHeight = Mathf.Max(p.Height, PlayerHeight) / scale;
                if (eliteOrBoss)
                    p.ModelHeight *= 1.15f;
                break;
            case EnemyKind.Spawnling:
                // Дрібний моб: фінальний зріст ~1.3 при Scale 0.72.
                p.ModelHeight = PlayerHeight * 1.05f / scale;
                break;
            case EnemyKind.Sploder:
            case EnemyKind.BigSploder:
            case EnemyKind.Squid:
            case EnemyKind.Ghost:
                p.ModelHeight = Mathf.Max(p.Height, PlayerHeight * 0.8f) / scale;
                break;
            case EnemyKind.Lamprey:
            case EnemyKind.Eyebat:
            case EnemyKind.WingedTerror:
                p.ModelHeight = Mathf.Max(p.Height, PlayerHeight * 0.62f) / scale;
                break;
            default:
                p.ModelHeight = p.Height / scale;
                break;
        }
    }

    static void Set(EnemyProfile p, string title, float hp, float endHp, float speed, float speedEnd,
        float damage, float xp, float height, EnemyBehavior behavior, Color color, Color emission,
        PrimitiveType body, float radius)
    {
        p.Title = title;
        p.HpBase = hp;
        p.HpEnd = endHp;
        p.SpeedBase = speed;
        p.SpeedEndBase = speedEnd;
        p.Hp = hp;
        p.Speed = speed;
        p.TouchDamage = damage;
        p.Xp = xp;
        p.Height = height;
        p.Radius = radius;
        p.Behavior = behavior;
        p.Color = color;
        p.Emission = emission;
        p.Body = body;
    }

    static void ApplyTimeCurve(EnemyProfile p, float t)
    {
        p.Hp = Mathf.Lerp(p.HpBase, p.HpEnd, t);
        p.Speed = Mathf.Lerp(p.SpeedBase, p.SpeedEndBase, t);
    }
}
