using UnityEngine;

public static class WeaponCatalog
{
    public static readonly WeaponDefinition Revolver = new ConfigWeapon(
        WeaponId.Revolver, "Револьвер", "Надійна збалансована зброя", WeaponBehavior.Normal,
        20f, 4f, 1, 0f, 0, 30f, 1.5f, 6, 1f, ProjectileStyle.Bolt);

    public static readonly WeaponDefinition Shotgun = new ConfigWeapon(
        WeaponId.Shotgun, "Дробовик", "Коротка дистанція, багато дробу", WeaponBehavior.Shotgun,
        10f, 5f, 4, 6f, 0, 22f, 0.55f, 2, 1f, ProjectileStyle.Pellet);

    public static readonly WeaponDefinition Crossbow = new ConfigWeapon(
        WeaponId.Crossbow, "Арбалет", "Тримай, щоб зарядити: більше урону й пробиття", WeaponBehavior.Charge,
        20f, 6.7f, 1, 0f, 1, 34f, 1.7f, 1, 1f, ProjectileStyle.Arrow);

    public static readonly WeaponDefinition DualSmgs = new ConfigWeapon(
        WeaponId.DualSmgs, "Подвійні SMG", "Два пістолети, подвійні ефекти", WeaponBehavior.Dual,
        8f, 4f, 1, 3f, 0, 33f, 1.4f, 24, 2f, ProjectileStyle.Bolt);

    public static readonly WeaponDefinition Batgun = new ConfigWeapon(
        WeaponId.Batgun, "Кажаноган", "Кажани самі шукають ворогів", WeaponBehavior.Homing,
        18f, 2f, 1, 0f, 0, 17f, 2.2f, 12, 1.5f, ProjectileStyle.Bat);

    public static readonly WeaponDefinition FlameCannon = new ConfigWeapon(
        WeaponId.FlameCannon, "Вогнемет", "Суцільна струя вогню, підпалює ворогів", WeaponBehavior.Burning,
        2.2f, 13f, 1, 3f, 0, 24f, 0.42f, 16, 1.5f, ProjectileStyle.Flame);

    public static readonly WeaponDefinition GrenadeLauncher = new ConfigWeapon(
        WeaponId.GrenadeLauncher, "Гранатомет", "Вибухи можуть зачепити й тебе", WeaponBehavior.Explosive,
        60f, 2.9f, 1, 0f, 0, 22f, 1.6f, 6, 1.5f, ProjectileStyle.Grenade);

    public static readonly WeaponDefinition Staff = new ConfigWeapon(
        WeaponId.Staff, "Посох", "Магічні стріли лишаються й повертаються при перезарядці", WeaponBehavior.Lingering,
        16f, 3.3f, 1, 0f, 1, 38f, 4f, 4, 1.2f, ProjectileStyle.Arrow);

    public static readonly WeaponDefinition SalvoKnife = new ConfigWeapon(
        WeaponId.SalvoKnife, "Сальво-ніж", "Відстежує ворогів і кидає ножі", WeaponBehavior.Tracking,
        20f, 1.7f, 1, 0f, 0, 34f, 1.4f, 12, 0.6f, ProjectileStyle.Knife);

    public static readonly WeaponDefinition WateringGun = new ConfigWeapon(
        WeaponId.WateringGun, "Поливач", "Кожен 5-й постріл вирощує споровий квіт", WeaponBehavior.Spore,
        18f, 5f, 1, 4f, 0, 28f, 1.2f, 10, 0.8f, ProjectileStyle.Spore);

    public static readonly WeaponDefinition CycloneSword = new ConfigWeapon(
        WeaponId.CycloneSword, "Циклон-меч", "Обертається, відбиває снаряди", WeaponBehavior.Spin,
        22f, 2.5f, 1, 20f, 1, 26f, 0.9f, 3, 1.2f, ProjectileStyle.Knife);

    public static readonly WeaponDefinition[] All =
    {
        Revolver, Shotgun, Crossbow, DualSmgs, Batgun, FlameCannon,
        GrenadeLauncher, Staff, SalvoKnife, WateringGun, CycloneSword
    };

    public static WeaponDefinition Get(WeaponId id)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].Id == id)
                return All[i];
        }
        return Revolver;
    }
}

sealed class ConfigWeapon : WeaponDefinition
{
    readonly WeaponId _id;
    readonly string _title;
    readonly string _description;
    readonly WeaponBehavior _behavior;
    readonly float _damage;
    readonly float _fireRate;
    readonly int _projectiles;
    readonly float _spread;
    readonly int _pierce;
    readonly float _speed;
    readonly float _life;
    readonly int _ammo;
    readonly float _reload;
    readonly ProjectileStyle _style;

    public ConfigWeapon(WeaponId id, string title, string description, WeaponBehavior behavior,
        float damage, float fireRate, int projectiles, float spread, int pierce,
        float speed, float life, int ammo, float reload, ProjectileStyle style)
    {
        _id = id;
        _title = title;
        _description = description;
        _behavior = behavior;
        _damage = damage;
        _fireRate = fireRate;
        _projectiles = projectiles;
        _spread = spread;
        _pierce = pierce;
        _speed = speed;
        _life = life;
        _ammo = ammo;
        _reload = reload;
        _style = style;
    }

    public override WeaponId Id { get { return _id; } }
    public override string Title { get { return _title; } }
    public override string BaseDescription { get { return _description; } }
    public override WeaponBehavior Behavior { get { return _behavior; } }
    public override float BaseDamage { get { return _damage; } }
    public override float BaseFireRate { get { return _fireRate; } }
    public override int BaseProjectiles { get { return _projectiles; } }
    public override float BaseSpread { get { return _spread; } }
    public override int BasePierce { get { return _pierce; } }
    public override float BaseBulletSpeed { get { return _speed; } }
    public override float BaseLife { get { return _life; } }
    public override int BaseAmmo { get { return _ammo; } }
    public override float BaseReloadTime { get { return _reload; } }
    public override ProjectileStyle Style { get { return _style; } }
}
