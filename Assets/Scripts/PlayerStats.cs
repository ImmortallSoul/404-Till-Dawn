using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public float MaxHp = 4f;
    public float Hp = 4f;
    public float MoveSpeedMultiplier = 1f;
    public float PickupRadius = 2.4f;
    public int Level = 1;
    public int Xp;
    public int PendingLevels;
    public int Gems;

    public float Regen;
    public float DamageReduction;

    public float DamageMultiplier = 1f;
    public float FireRateMultiplier = 1f;
    public float ReloadRateMultiplier = 1f;
    public float BulletSpeedMultiplier = 1f;
    public float BulletSizeMultiplier = 1f;
    public int Pierce;
    public int ExtraProjectiles;
    public int Bounces;
    public int MaxAmmoBonus;
    public float Spread;

    public float CritChance;
    public float CritMultiplier = 2f;

    public float VisionRange = 11f;
    public float XpGainMultiplier = 1f;
    public float SummonDamageMultiplier = 1f;
    public float SummonSpeedMultiplier = 1f;

    public float BurnChance;
    public float BurnMultiplier = 1f;
    public float FreezeChance;
    public float LightningChance;
    public float LightningDamage = 9f;
    public float CurseChance;
    public float CurseMultiplier = 1.8f;
    public float SmiteChance;
    public float SmiteDamage = 14f;

    public bool CanReroll;
    public int RerollsLeft;
    public bool DiamondBulkUp;
    public bool DiamondBerserk;
    public bool DiamondLowHp;
    public bool Scarlett;
    public bool LilithSpirit;
    public bool Splinter;
    public bool Assassin;

    // Обидва скіли вивчаються через картки підвищень.
    public int RearViewMirrorLevel;
    public bool RearViewMirror { get { return RearViewMirrorLevel > 0; } }

    public int TimeDisplacementLevel;
    public bool TimeDisplacement { get { return TimeDisplacementLevel > 0; } }
    public bool IsPhased;
    public System.Action<PlayerStats> OnDamaged;

    // Візуальний фідбек отримання урону для HUD і тряски камери.
    public float DamageFlash;
    public Vector3 DamageDirection;

    float _iFrameUntil;

    public float Hp01 { get { return MaxHp <= 0f ? 0f : Hp / MaxHp; } }
    public int XpToNext { get { return 5 + (Level - 1) * 3; } }
    public bool IsLowHp { get { return Hp < MaxHp * 0.5f; } }

    public void ApplyCharacter(CharacterDefinition character)
    {
        if (character == null)
            return;

        MaxHp = character.MaxHp;
        Hp = MaxHp;
        Level = 1;
        Xp = 0;
        Gems = 0;

        if (character.Apply != null)
            character.Apply(this, GetComponent<PlayerCombat>());

        if (CanReroll)
            RerollsLeft = 1;
    }

    void Update()
    {
        var session = GameSession.Instance;
        if (session == null || !session.AllowsGameplay)
            return;

        if (Regen > 0f && Hp > 0f)
            Heal(Regen * Time.deltaTime);

        DamageFlash = Mathf.MoveTowards(DamageFlash, 0f, Time.deltaTime * 2.8f);

        if (PlayerLamp.Instance != null)
            PlayerLamp.Instance.SetRange(VisionRange);
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || Hp >= MaxHp)
            return;
        Hp = Mathf.Min(MaxHp, Hp + amount);

        if (DiamondBulkUp)
        {
            DamageMultiplier = Mathf.Min(3f, DamageMultiplier * 1.08f);
            BulletSizeMultiplier = Mathf.Min(2.5f, BulletSizeMultiplier * 1.06f);
        }
    }

    public void AddMaxHp(float amount, bool healToo)
    {
        MaxHp += amount;
        if (healToo)
            Hp = Mathf.Min(MaxHp, Hp + amount);
    }

    public bool TryTakeDamage(float amount)
    {
        return TryTakeDamage(amount, Vector3.zero);
    }

    public bool TryTakeDamage(float amount, Vector3 source)
    {
        if (GameSession.Instance == null || !GameSession.Instance.AllowsGameplay)
            return false;
        if (IsPhased || amount <= 0f)
            return false;

        var fps = GetComponent<FirstPersonController>();
        if (fps != null && fps.IsRolling)
            return false;

        if (Time.time < _iFrameUntil)
            return false;

        float reduction = Mathf.Clamp01(DamageReduction);
        if (DiamondLowHp && IsLowHp)
            reduction = Mathf.Clamp01(reduction + 0.2f);

        float taken = amount * (1f - reduction);
        Hp -= taken;
        _iFrameUntil = Time.time + 0.7f;
        DamageFlash = Mathf.Clamp01(DamageFlash + 0.35f + taken * 0.12f);
        if (source.sqrMagnitude > 0.01f)
            DamageDirection = (source - transform.position).normalized;
        else
            DamageDirection = Vector3.zero;
        if (fps != null)
            fps.Shake(Mathf.Clamp01(0.35f + taken * 0.25f));
        AudioManager.Instance?.Play("hurt", 0.7f, Random.Range(0.95f, 1.08f), 0.25f);
        if (OnDamaged != null)
            OnDamaged(this);

        if (DiamondBerserk)
        {
            var combat = GetComponent<PlayerCombat>();
            if (combat != null)
                combat.FireRateMultiplier = Mathf.Min(4f, combat.FireRateMultiplier * 1.12f);
        }

        if (Hp <= 0f)
        {
            Hp = 0f;
            GameSession.Instance.PlayerDied();
        }
        return true;
    }

    public void AddXp(int amount)
    {
        if (GameSession.Instance == null || GameSession.Instance.State == RunState.Dead)
            return;

        int gained = Mathf.Max(1, Mathf.RoundToInt(amount * XpGainMultiplier));
        Xp += gained;
        Gems += gained;

        bool levelled = false;
        while (Xp >= XpToNext)
        {
            Xp -= XpToNext;
            Level++;
            PendingLevels++;
            levelled = true;
            if (CanReroll)
                RerollsLeft = 1;
        }
        if (levelled)
            AudioManager.Instance?.Play("level_up", 0.75f, 1f, 0.3f);

        if (PendingLevels > 0)
            UpgradeMenu.Instance?.ShowIfNeeded();
    }

    public void OnKill(Enemy killed)
    {
        if (killed == null)
            return;

        if (LilithSpirit)
            SummonManager.Instance?.SpawnSpirit(killed.transform.position);

        if (Splinter && Random.value < 0.32f)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
                Projectile.Spawn(killed.transform.position + Vector3.up * 0.45f,
                    Quaternion.LookRotation(dir, Vector3.up), 5f * DamageMultiplier, 19f, 0, 0.7f,
                    ProjectileStyle.Bolt, ProjectileBehavior.Normal, true, 0);
            }
        }
    }

    public void ApplyOnHit(Enemy enemy, float bulletDamage)
    {
        if (enemy == null)
            return;

        if (BurnChance > 0f && Random.value < BurnChance)
            enemy.ApplyBurn(3f * BurnMultiplier, 3f);

        if (FreezeChance > 0f && Random.value < FreezeChance)
            enemy.ApplyFreeze(1.5f);

        if (LightningChance > 0f && Random.value < LightningChance)
            enemy.StrikeLightning(LightningDamage);

        if (CurseChance > 0f && Random.value < CurseChance)
            enemy.ApplyCurse(bulletDamage * CurseMultiplier, 2.5f);

        if (SmiteChance > 0f && Random.value < SmiteChance)
            enemy.TakeDamage(SmiteDamage);
    }
}
