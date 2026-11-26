using UnityEngine;

/// <summary>
/// Скіл у стилі Titanfall: перехід у паралельний часовий вимір.
/// Поки активний, гравець невразливий і проходить крізь ворогів, але й сам не завдає шкоди зброєю.
/// Повернення створює часовий вибух, що заморожує й пошкоджує навколишніх монстрів.
/// Вивчається через картку підвищення "Часовий зсув", активується клавішею E.
/// </summary>
public class TimeDisplacement : MonoBehaviour
{
    PlayerStats _stats;
    float _duration;
    float _cooldown;
    float _remaining;
    float _cooldownRemaining;
    bool _active;

    public bool IsActive { get { return _active; } }
    public float Remaining { get { return _remaining; } }
    public float CooldownRemaining { get { return _cooldownRemaining; } }
    public float Active01 { get { return _duration <= 0f ? 0f : Mathf.Clamp01(_remaining / _duration); } }
    public float Cooldown01
    {
        get
        {
            if (_cooldown <= 0f)
                return 0f;
            return Mathf.Clamp01(1f - _cooldownRemaining / _cooldown);
        }
    }

    public static void Attach(PlayerStats stats)
    {
        var go = new GameObject("TimeDisplacement");
        go.transform.SetParent(stats.transform, false);
        var shift = go.AddComponent<TimeDisplacement>();
        shift._stats = stats;
    }

    bool _devOverride;

    void Awake()
    {
        if (_stats == null)
            _stats = GetComponentInParent<PlayerStats>();
    }

    /// <summary>Меню розробника (F12): дозволяє зсув без апгрейд-карти.</summary>
    public void DevTrigger()
    {
        if (_stats == null)
            return;
        if (_active)
        {
            End();
            return;
        }
        _devOverride = true;
        _cooldownRemaining = 0f;
        Begin();
    }

    public void DevForceEnd()
    {
        _devOverride = false;
        if (_active)
            End();
        else
            ForceEnd();
    }

    void Update()
    {
        bool playing = GameSession.Instance == null || GameSession.Instance.AllowsGameplay;
        if (!playing || _stats == null)
            return;

        if (!_devOverride && !_stats.TimeDisplacement)
        {
            ForceEnd();
            return;
        }

        if (_active)
        {
            _remaining -= Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.E) || _remaining <= 0f)
            {
                End();
                return;
            }
            return;
        }

        if (_cooldownRemaining > 0f)
            _cooldownRemaining -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.E) && _cooldownRemaining <= 0f)
            Begin();
    }

    void Begin()
    {
        int level = Mathf.Max(1, _stats.TimeDisplacementLevel);
        _duration = 3.5f + level * 0.8f;
        _cooldown = Mathf.Max(6f, 11f - level * 1.5f);
        _remaining = _duration;
        _active = true;
        _stats.IsPhased = true;
        AudioManager.Instance?.Play("phased", 0.65f, 1.15f, 0f);

        Projectile.SpawnBurst(_stats.transform.position + Vector3.up, new Color(0.45f, 0.35f, 1f), 20);
        GameSession.Instance?.ShowNotice("ЧАСОВИЙ ЗСУВ · ПАРАЛЕЛЬНИЙ ВИМІР", 2f);
    }

    void End()
    {
        _active = false;
        _remaining = 0f;
        _cooldownRemaining = _cooldown;
        if (_stats != null)
            _stats.IsPhased = false;

        int level = Mathf.Max(1, _stats.TimeDisplacementLevel);
        float damage = (9f + level * 4f) * _stats.DamageMultiplier;
        var enemies = Enemy.NearestEnemies(_stats.transform.position, 12, 6f);
        for (int i = 0; i < enemies.Count; i++)
        {
            enemies[i].TakeDamage(damage);
            enemies[i].ApplyFreeze(0.7f + level * 0.25f);
            DamageNumber.Spawn(enemies[i].transform.position + Vector3.up * 1.2f, damage, true);
        }

        Projectile.SpawnBurst(_stats.transform.position + Vector3.up, new Color(0.75f, 0.6f, 1f), 24);
        GameSession.Instance?.ShowNotice("ПОВЕРНЕННЯ У ЧАСІ", 1.6f);
    }

    void ForceEnd()
    {
        if (!_active)
            return;
        _active = false;
        _remaining = 0f;
        if (_stats != null)
            _stats.IsPhased = false;
    }

    void OnDestroy()
    {
        if (_stats != null)
            _stats.IsPhased = false;
    }
}
