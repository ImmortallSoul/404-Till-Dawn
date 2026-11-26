using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public const int MaxWeapons = 4;

    readonly List<WeaponInstance> _weapons = new List<WeaponInstance>();
    public List<WeaponInstance> Weapons { get { return _weapons; } }

    // Кожен вид зброї зберігає свій рівень навіть тоді, коли зараз не екіпірований.
    readonly Dictionary<WeaponId, int> _weaponLevels = new Dictionary<WeaponId, int>();

    int _activeWeaponIndex;
    public int ActiveWeaponIndex
    {
        get { return _weapons.Count == 0 ? 0 : Mathf.Clamp(_activeWeaponIndex, 0, _weapons.Count - 1); }
        set { _activeWeaponIndex = value; }
    }

    public WeaponInstance ActiveWeapon
    {
        get { return _weapons.Count == 0 ? null : _weapons[ActiveWeaponIndex]; }
    }

    PlayerStats _stats;
    FirstPersonController _fps;
    Transform _muzzle;
    float _fireHeld;
    public bool IsFiring { get { return _fireHeld > 0f; } }
    int _shotCounter;
    float _specialCooldown;
    float _specialReadyAt;

    public float DamageMultiplier { get { return _stats.DamageMultiplier; } set { _stats.DamageMultiplier = value; } }
    public float FireRateMultiplier { get { return _stats.FireRateMultiplier; } set { _stats.FireRateMultiplier = value; } }
    public float ReloadRateMultiplier { get { return _stats.ReloadRateMultiplier; } set { _stats.ReloadRateMultiplier = value; } }
    public float BulletSpeedMultiplier { get { return _stats.BulletSpeedMultiplier; } set { _stats.BulletSpeedMultiplier = value; } }
    public int Pierce { get { return _stats.Pierce; } set { _stats.Pierce = value; } }
    public int ExtraProjectiles { get { return _stats.ExtraProjectiles; } set { _stats.ExtraProjectiles = value; } }
    public int Bounces { get { return _stats.Bounces; } set { _stats.Bounces = value; } }
    public float Spread { get { return _stats.Spread; } set { _stats.Spread = value; } }

    public float SpecialCooldown { get { return _specialCooldown; } }
    public float SpecialRemaining { get { return Mathf.Max(0f, _specialReadyAt - Time.time); } }
    public float Special01 { get { return SpecialRemaining <= 0f ? 1f : 1f - SpecialRemaining / Mathf.Max(0.1f, _specialCooldown); } }

    void Awake()
    {
        _stats = GetComponent<PlayerStats>();
    }

    void Start()
    {
        _fps = GetComponent<FirstPersonController>();
        _muzzle = _fps != null && _fps.Muzzle != null ? _fps.Muzzle : transform;
        Transform anchor = _fps != null && _fps.ViewModelAnchor != null
            ? _fps.ViewModelAnchor
            : Camera.main.transform;
        WeaponViewModel.Attach(anchor, this);
        AddOrLevelWeapon(WeaponCatalog.Get(GameSession.SelectedWeapon), true);

        var character = CharacterCatalog.Get(GameSession.SelectedCharacter);
        _specialCooldown = character.SpecialCooldown > 0f ? character.SpecialCooldown : 14f;
        _specialReadyAt = Time.time + _specialCooldown * 0.35f;
    }

    void Update()
    {
        if (GameSession.Instance == null || !GameSession.Instance.AllowsGameplay)
        {
            AudioManager.Instance?.SetLoop("flame_loop", false);
            return;
        }

        if (_fps == null)
        {
            Vector3 to = GameCamera.AimPoint - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
        }

        bool firing = Input.GetMouseButton(0);
        _fireHeld = firing ? _fireHeld + Time.deltaTime : 0f;

        bool flamethrower = ActiveWeapon != null && ActiveWeapon.Definition.Id == WeaponId.FlameCannon;
        AudioManager.Instance?.SetLoop("flame_loop", flamethrower && firing, 0.62f);

        for (int i = 0; i < _weapons.Count; i++)
            _weapons[i].Tick(this, _muzzle, firing, _fireHeld);

        if (Input.GetMouseButtonDown(1))
            UseSpecial();
    }

    void OnWeaponFiredViewModel(PlayerCombat owner)
    {
        WeaponViewModel.Instance?.NotifyFired();
    }

    void OnFired(PlayerCombat owner)
    {
        _shotCounter++;
        GameHud.NotifyShot();

        var active = ActiveWeapon;
        string cue = null;
        float volume = 0.58f;
        if (active != null)
        {
            switch (active.Definition.Id)
            {
                case WeaponId.Revolver: cue = "gun_revolver"; volume = 0.62f; break;
                case WeaponId.Shotgun: cue = "gun_shotgun"; volume = 0.66f; break;
                case WeaponId.DualSmgs: cue = "gun_smg"; volume = 0.36f; break;
                case WeaponId.FlameCannon: break; // безперервний flame_loop керується в Update
                case WeaponId.Staff: cue = "gun_staff"; volume = 0.46f; break;
                case WeaponId.Crossbow: cue = "gun_crossbow"; volume = 0.5f; break;
                case WeaponId.Batgun: cue = "gun_bat"; volume = 0.42f; break;
                case WeaponId.GrenadeLauncher: cue = "gun_grenade"; volume = 0.58f; break;
                case WeaponId.SalvoKnife: cue = "gun_knife"; volume = 0.42f; break;
                case WeaponId.WateringGun: cue = "gun_water"; volume = 0.42f; break;
                case WeaponId.CycloneSword: cue = "gun_sword"; volume = 0.46f; break;
            }
        }
        if (!string.IsNullOrEmpty(cue))
            AudioManager.Instance?.Play(cue, volume, Random.Range(0.97f, 1.03f), 0.045f);

        // Додатковий шар магії/клинка з паку HomkaPlay; основний звук зброї не змінюємо.
        if (active != null && _shotCounter % 3 == 0 &&
            (active.Definition.Id == WeaponId.Staff ||
             active.Definition.Id == WeaponId.CycloneSword ||
             active.Definition.Id == WeaponId.SalvoKnife))
        {
            AudioManager.Instance?.Play("extra_magic", 0.24f, Random.Range(0.92f, 1.08f), 0.12f);
        }

        if (active != null && _muzzle != null &&
            (active.Definition.Id == WeaponId.CycloneSword || active.Definition.Id == WeaponId.SalvoKnife))
        {
            // Kryven Blade світиться зеленими рунами — дуга удару того ж кольору.
            Color slash = active.Definition.Id == WeaponId.CycloneSword
                ? new Color(0.45f, 1f, 0.55f, 0.78f)
                : new Color(0.85f, 0.9f, 1f, 0.6f);
            Projectile.SpawnSlash(_muzzle.position + _muzzle.forward * 0.62f, _muzzle.rotation, slash);
        }
    }

    public void OnReloadFinished()
    {
        for (int i = 0; i < _weapons.Count; i++)
        {
            if (_weapons[i].Definition.Behavior != WeaponBehavior.Spin)
                continue;

            float damage = 22f * DamageMultiplier;
            var enemies = Enemy.NearestEnemies(transform.position, 40, 5.5f);
            for (int j = 0; j < enemies.Count; j++)
                enemies[j].TakeDamage(damage);
            SummonManager.Instance?.Spin(transform.position, damage);
        }
    }

    void UseSpecial()
    {
        if (Time.time < _specialReadyAt)
            return;

        CharacterId id = GameSession.SelectedCharacter;
        var character = CharacterCatalog.Get(id);
        float radius = id == CharacterId.Spark ? 9f : 7f;
        var enemies = Enemy.NearestEnemies(transform.position, 40, radius);

        // Порожній клік не здається «мертвим» і не спалює довгу перезарядку.
        if (enemies.Count == 0)
        {
            GameSession.Instance?.ShowNotice(
                "ЗДІБНІСТЬ: " + character.SpecialName + " — немає цілей поруч", 1.4f);
            AudioManager.Instance?.Play("ui_back", 0.5f, 1f, 0.15f);
            _specialReadyAt = Time.time + 0.6f;
            return;
        }

        _specialReadyAt = Time.time + Mathf.Max(6f, _specialCooldown);

        float damage = 28f * DamageMultiplier;
        var targets = new List<Vector3>(enemies.Count);
        for (int i = 0; i < enemies.Count; i++)
        {
            targets.Add(enemies[i].transform.position + Vector3.up * 0.6f);
            enemies[i].TakeDamage(damage);

            switch (id)
            {
                case CharacterId.Scarlett:
                    enemies[i].ApplyBurn(7f * _stats.BurnMultiplier, 4f);
                    break;
                case CharacterId.Spark:
                    enemies[i].StrikeLightning(18f + _stats.LightningDamage);
                    break;
                case CharacterId.Lilith:
                    SummonManager.Instance?.SpawnSpirit(enemies[i].transform.position);
                    break;
                case CharacterId.Diamond:
                    enemies[i].ApplyFreeze(2f);
                    break;
                case CharacterId.Shana:
                default:
                    enemies[i].ApplyCurse(damage, 2.5f);
                    break;
            }
        }

        if (id == CharacterId.Shana)
        {
            _stats.RerollsLeft += 2;
            _stats.Heal(0.5f);
        }
        else if (id == CharacterId.Diamond)
        {
            _stats.Heal(1f);
        }

        Color color;
        float pitch;
        switch (id)
        {
            case CharacterId.Diamond: color = new Color(0.45f, 0.82f, 1f); pitch = 1.12f; break;
            case CharacterId.Scarlett: color = new Color(1f, 0.38f, 0.08f); pitch = 0.86f; break;
            case CharacterId.Spark: color = new Color(0.35f, 0.95f, 1f); pitch = 1.25f; break;
            case CharacterId.Lilith: color = new Color(0.85f, 0.35f, 1f); pitch = 1f; break;
            default: color = new Color(0.78f, 0.35f, 1f); pitch = 0.92f; break;
        }

        SkillFx.Spawn(transform.position, color, radius,
            id == CharacterId.Spark ? targets : null);
        AudioManager.Instance?.Play("skill", 0.9f, pitch, 0f);
        GameSession.Instance?.ShowNotice("ЗДІБНІСТЬ: " + character.SpecialName, 1.8f);
        PlayerLamp.Instance?.Flash(3f);
    }

    // Виклик здатностей із меню розробника (F12). У звичайному геймплеї не використовується.
    public void DevTriggerSpecial()
    {
        if (_stats == null)
            return;
        _specialReadyAt = 0f;
        UseSpecial();
    }

    public void DevGrantWeapon(WeaponId id)
    {
        var def = WeaponCatalog.Get(id);
        if (def != null)
            AddOrLevelWeapon(def, true);
    }

    public bool CanAddWeapon { get { return _weapons.Count < MaxWeapons; } }

    public bool HasWeapon(WeaponId id)
    {
        for (int i = 0; i < _weapons.Count; i++)
        {
            if (_weapons[i].Definition.Id == id)
                return true;
        }
        return false;
    }

    public int WeaponLevel(WeaponId id)
    {
        for (int i = 0; i < _weapons.Count; i++)
        {
            if (_weapons[i].Definition.Id == id)
                return _weapons[i].Level;
        }
        int stored;
        return _weaponLevels.TryGetValue(id, out stored) ? stored : 0;
    }

    int StoredLevel(WeaponId id)
    {
        int level;
        return _weaponLevels.TryGetValue(id, out level) ? level : 0;
    }

    void SetStoredLevel(WeaponId id, int level)
    {
        _weaponLevels[id] = Mathf.Max(1, level);
    }

    public void AddOrLevelWeapon(WeaponDefinition definition, bool silent = false)
    {
        if (definition == null)
            return;

        for (int i = 0; i < _weapons.Count; i++)
        {
            if (_weapons[i].Definition.Id != definition.Id)
                continue;

            _weapons[i].LevelUp();
            SetStoredLevel(definition.Id, _weapons[i].Level);
            if (!silent)
                GameSession.Instance?.ShowNotice(definition.Title + " Lv" + _weapons[i].Level, 2f);
            return;
        }

        int nextLevel = StoredLevel(definition.Id) + 1;
        if (_weapons.Count < MaxWeapons)
        {
            _weapons.Add(CreateWeaponInstance(definition, nextLevel));
            SetStoredLevel(definition.Id, nextLevel);
            if (!silent)
                GameSession.Instance?.ShowNotice("Нова зброя: " + definition.Title + " Lv" + nextLevel, 2.5f);
            return;
        }

        _weapons[0].LevelUp();
        SetStoredLevel(_weapons[0].Definition.Id, _weapons[0].Level);
        if (!silent)
            GameSession.Instance?.ShowNotice(definition.Title + " → " + _weapons[0].DisplayName, 2f);
    }

    /// <summary>
    /// Картка зброї завжди підвищує рівень обраного виду зброї.
    /// Якщо вона вже є в іншому слоті —.level і слот міняються місцями,
    /// а не створюється новий екземпляр з першого рівня.
    /// </summary>
    public void ReplaceOrLevelWeapon(WeaponDefinition definition, bool silent = false)
    {
        if (definition == null)
            return;

        if (_weapons.Count == 0)
        {
            AddOrLevelWeapon(definition, silent);
            return;
        }

        int index = ActiveWeaponIndex;
        var current = _weapons[index];

        if (current.Definition.Id == definition.Id)
        {
            current.LevelUp();
            SetStoredLevel(definition.Id, current.Level);
            if (!silent)
                GameSession.Instance?.ShowNotice(definition.Title + " Lv" + current.Level, 2f);
            return;
        }

        string previous = current.DisplayName;
        int nextLevel;
        int existing = -1;
        for (int i = 0; i < _weapons.Count; i++)
        {
            if (_weapons[i].Definition.Id == definition.Id)
            {
                existing = i;
                break;
            }
        }

        if (existing >= 0)
        {
            var owned = _weapons[existing];
            owned.LevelUp();
            nextLevel = owned.Level;
            _weapons[existing] = current;
            _weapons[index] = owned;
        }
        else
        {
            nextLevel = StoredLevel(definition.Id) + 1;
            _weapons[index] = CreateWeaponInstance(definition, nextLevel);
        }

        SetStoredLevel(definition.Id, nextLevel);

        var viewModel = WeaponViewModel.Instance;
        if (viewModel != null)
            viewModel.NotifyWeaponReplaced();

        if (!silent)
            GameSession.Instance?.ShowNotice(previous + " → " + definition.Title + " Lv" + nextLevel, 2.6f);
    }

    WeaponInstance CreateWeaponInstance(WeaponDefinition definition, int level = 1)
    {
        var instance = definition.Create(level);
        instance.OnFired += OnFired;
        instance.OnFired += OnWeaponFiredViewModel;
        return instance;
    }
}
