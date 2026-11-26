using UnityEngine;

public enum WeaponId
{
    Revolver,
    Shotgun,
    Crossbow,
    DualSmgs,
    Batgun,
    FlameCannon,
    GrenadeLauncher,
    Staff,
    SalvoKnife,
    WateringGun,
    CycloneSword
}

public enum WeaponBehavior
{
    Normal,
    Shotgun,
    Charge,
    Dual,
    Homing,
    Burning,
    Explosive,
    Lingering,
    Tracking,
    Spore,
    Spin
}

public enum ProjectileStyle
{
    Bolt,
    Pellet,
    Flame,
    Rail,
    Grenade,
    Arrow,
    Knife,
    Bat,
    Spore,
    Orb
}

public abstract class WeaponDefinition
{
    public abstract WeaponId Id { get; }
    public abstract string Title { get; }
    public abstract string BaseDescription { get; }
    public abstract WeaponBehavior Behavior { get; }

    public abstract float BaseDamage { get; }
    public abstract float BaseFireRate { get; }
    public abstract int BaseProjectiles { get; }
    public abstract float BaseSpread { get; }
    public abstract int BasePierce { get; }
    public abstract float BaseBulletSpeed { get; }
    public abstract float BaseLife { get; }
    public abstract int BaseAmmo { get; }
    public abstract float BaseReloadTime { get; }
    public abstract ProjectileStyle Style { get; }

    public virtual string Description(int level)
    {
        float damage = BaseDamage * (1f + 0.18f * (level - 1));
        float rate = BaseFireRate * (1f + 0.1f * (level - 1));
        int shots = BaseProjectiles + (level >= 4 ? 1 : 0);
        string text = BaseDescription + "\nУрон " + Mathf.RoundToInt(damage)
                      + " · темп " + rate.ToString("0.0") + "/с"
                      + " · патронів " + (BaseAmmo + level / 3);
        if (shots > 1)
            text += " · снарядів " + shots;
        return text;
    }

    public WeaponInstance Create(int level)
    {
        return new WeaponInstance(this, level);
    }
}

public class WeaponInstance
{
    public WeaponDefinition Definition { get; private set; }
    public int Level { get; private set; }
    public int Ammo { get; private set; }
    public int MaxAmmo { get; private set; }
    public bool IsReloading { get; private set; }
    public float ReloadProgress { get { return IsReloading ? 1f - _reloadTimer / _reloadLength : 1f; } }
    public string DisplayName { get { return Definition.Title + " Lv" + Level; } }
    public string Description { get { return Definition.Description(Level); } }
    public System.Action<PlayerCombat> OnFired;

    float _cooldown;
    float _reloadTimer;
    float _reloadLength;
    float _charge;
    float _damage;
    float _fireRate;
    float _speed;
    float _spread;
    float _life;
    int _projectiles;
    int _pierce;
    int _shotsFired;

    public WeaponInstance(WeaponDefinition definition, int level)
    {
        Definition = definition;
        Level = Mathf.Max(1, level);
        Recalculate();
        Ammo = MaxAmmo;
    }

    void Recalculate()
    {
        float s = Level - 1;
        _damage = Definition.BaseDamage * (1f + 0.18f * s);
        _fireRate = Definition.BaseFireRate * (1f + 0.1f * s);
        _speed = Definition.BaseBulletSpeed * (1f + 0.05f * s);
        _spread = Definition.BaseSpread;
        _life = Definition.BaseLife;
        _projectiles = Definition.BaseProjectiles + (Level >= 4 ? 1 : 0);
        _pierce = Definition.BasePierce + (Level >= 3 ? 1 : 0);
        MaxAmmo = Definition.BaseAmmo + Level / 3;
    }

    public void LevelUp()
    {
        Level++;
        int oldMax = MaxAmmo;
        Recalculate();
        Ammo += MaxAmmo - oldMax;
        PlayerLamp.Instance?.Flash(1f);
    }

    public void Reload()
    {
        if (IsReloading || Ammo >= MaxAmmo)
            return;
        IsReloading = true;
        _reloadLength = Definition.BaseReloadTime;
        _reloadTimer = _reloadLength;
    }

    public void Tick(PlayerCombat owner, Transform muzzle, bool firing, float chargeSeconds)
    {
        var stats = owner.GetComponent<PlayerStats>();
        int bonusAmmo = stats != null ? stats.MaxAmmoBonus : 0;
        int desiredMax = Definition.BaseAmmo + Level / 3 + bonusAmmo;
        if (MaxAmmo != desiredMax)
        {
            int delta = desiredMax - MaxAmmo;
            MaxAmmo = desiredMax;
            Ammo = Mathf.Clamp(Ammo + (delta > 0 ? delta : 0), 0, MaxAmmo);
        }

        if (IsReloading)
        {
            _reloadTimer -= Time.deltaTime * owner.ReloadRateMultiplier;
            if (_reloadTimer <= 0f)
            {
                IsReloading = false;
                Ammo = MaxAmmo;
                owner.OnReloadFinished();
            }
            return;
        }

        if (Ammo <= 0)
        {
            Reload();
            return;
        }

        _charge = firing ? _charge + Time.deltaTime : 0f;
        _cooldown -= Time.deltaTime;
        if (!firing || _cooldown > 0f)
            return;

        _cooldown = 1f / Mathf.Max(0.2f, _fireRate * owner.FireRateMultiplier);
        Fire(owner, muzzle, chargeSeconds);
        Ammo--;
        _shotsFired++;
        if (OnFired != null)
            OnFired(owner);

        if (Ammo <= 0)
            Reload();
    }

    void Fire(PlayerCombat owner, Transform muzzle, float chargeSeconds)
    {
        float damage = _damage * owner.DamageMultiplier;
        int pierce = _pierce + owner.Pierce;
        int count = _projectiles + owner.ExtraProjectiles;
        float spread = _spread + owner.Spread;
        ProjectileStyle style = Definition.Style;
        ProjectileBehavior behavior = ProjectileBehavior.Normal;
        float speed = _speed * owner.BulletSpeedMultiplier;
        float life = _life;
        bool fromPlayer = true;
        int bounces = owner.Bounces;

        // Вогнемет не випускає снарядів-кульок: це суцільна струя з конусним уроном.
        if (Definition.Id == WeaponId.FlameCannon)
        {
            ApplyFlameCone(owner, muzzle, damage);
            PlayerLamp.Instance?.Flash(0.45f);
            return;
        }

        switch (Definition.Behavior)
        {
            case WeaponBehavior.Shotgun:
                count = Mathf.Max(2, count + 2);
                spread = spread > 0f ? spread : 9f;
                life = Mathf.Min(life, 0.62f);
                break;
            case WeaponBehavior.Charge:
                float charge = Mathf.Clamp01(chargeSeconds / 1.1f);
                damage *= 1f + charge * 2.5f;
                pierce += Mathf.RoundToInt(charge * 3f);
                speed *= 1f + charge * 0.8f;
                behavior = ProjectileBehavior.Charged;
                style = ProjectileStyle.Rail;
                break;
            case WeaponBehavior.Dual:
                count = Mathf.Max(2, count + 1);
                spread = Mathf.Max(spread, 2f);
                break;
            case WeaponBehavior.Homing:
                behavior = ProjectileBehavior.Homing;
                style = ProjectileStyle.Bat;
                speed *= 0.85f;
                life = 2.2f;
                break;
            case WeaponBehavior.Burning:
                behavior = ProjectileBehavior.Burning;
                style = ProjectileStyle.Flame;
                life = 0.75f;
                break;
            case WeaponBehavior.Explosive:
                behavior = ProjectileBehavior.Explosive;
                style = ProjectileStyle.Grenade;
                life = 1.6f;
                speed *= 0.8f;
                break;
            case WeaponBehavior.Lingering:
                behavior = ProjectileBehavior.Lingering;
                style = ProjectileStyle.Arrow;
                speed *= 1.6f;
                life = 4f;
                break;
            case WeaponBehavior.Tracking:
                behavior = ProjectileBehavior.Tracking;
                style = ProjectileStyle.Knife;
                speed *= 1.4f;
                break;
            case WeaponBehavior.Spore:
                behavior = ProjectileBehavior.Normal;
                style = ProjectileStyle.Spore;
                if (_shotsFired % 5 == 0)
                    SporeFlower.Spawn(muzzle.position + muzzle.forward * 2f);
                break;
            case WeaponBehavior.Spin:
                behavior = ProjectileBehavior.Bouncing;
                style = ProjectileStyle.Knife;
                bounces += 2;
                break;
        }

        if (behavior == ProjectileBehavior.Burning)
            damage = Mathf.Max(1f, damage);

        float step = count <= 1 ? 0f : spread;
        float start = -step * (count - 1) * 0.5f;
        Vector3 origin = muzzle.position + muzzle.forward * 0.9f;

        if (behavior == ProjectileBehavior.Tracking)
        {
            var targets = Enemy.NearestEnemies(origin, 4, 14f);
            for (int i = 0; i < targets.Count; i++)
            {
                Vector3 dir = (targets[i].transform.position - origin).normalized;
                SpawnProjectile(owner, origin, Quaternion.LookRotation(dir, Vector3.up), damage, speed, pierce, life, style, behavior, fromPlayer, bounces);
            }
            if (targets.Count == 0)
                SpawnProjectile(owner, origin, muzzle.rotation, damage, speed, pierce, life, style, behavior, fromPlayer, bounces);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            float yaw = count <= 1 ? 0f : start + step * i;
            Quaternion rotation = muzzle.rotation * Quaternion.Euler(0f, yaw, 0f);
            SpawnProjectile(owner, origin, rotation, damage, speed, pierce, life, style, behavior, fromPlayer, bounces);
        }

        PlayerLamp.Instance?.Flash(style == ProjectileStyle.Flame ? 0.6f : 1.1f);
    }

    /// <summary>
    /// Струя вогню вражає ворогів у конусі попереду й підпалює їх.
    /// </summary>
    static void ApplyFlameCone(PlayerCombat owner, Transform muzzle, float damage)
    {
        Vector3 origin = muzzle.position;
        Vector3 forward = muzzle.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            return;
        forward.Normalize();

        var stats = owner.GetComponent<PlayerStats>();
        // Струя сягає 7.5 м і вражає все в конусі — це суцільний потік, не снаряди.
        const float range = 7.5f;
        var enemies = Enemy.NearestEnemies(origin, 40, range);
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null)
                continue;

            Vector3 to = enemy.transform.position - origin;
            to.y = 0f;
            float distance = to.magnitude;
            if (distance > range || distance < 0.01f)
                continue;
            if (Vector3.Dot(forward, to / distance) < 0.72f)
                continue;

            enemy.TakeDamage(damage);
            enemy.ApplyBurn(2.6f * (stats != null ? stats.BurnMultiplier : 1f), 2.2f);
        }
    }

    static void SpawnProjectile(PlayerCombat owner, Vector3 origin, Quaternion rotation, float damage, float speed,
        int pierce, float life, ProjectileStyle style, ProjectileBehavior behavior, bool fromPlayer, int bounces)
    {
        Projectile.Spawn(origin, rotation, damage, speed, pierce, life, style, behavior, fromPlayer, bounces);
    }
}
