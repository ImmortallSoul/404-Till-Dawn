using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    sealed class Wave
    {
        public EnemyKind Kind;
        public float Unlock;
        public float Interval;
        public int Per;
        public int Max;
    }

    sealed class Schedule
    {
        public float Time;
        public EnemyKind Kind;
        public EnemyTier Tier;
    }

    Wave[] _waves;
    float[] _timers;
    Schedule[] _elites;
    Schedule[] _bosses;
    int _nextElite;
    int _nextBoss;
    int _nextSurge;
    float _surgeUntil;
    bool _finalRushAnnounced;
    MapType _map;
    int _skeletonEliteIndex;

    static readonly float[] Surges = { 180f, 420f, 660f, 900f };
    // Однаковий спаунрейт для всіх типів мобів і протягом усього раунду.
    const float UniformSpawnInterval = 1.5f;
    // Рейт зменшено на 75%: залишається 25% від базового.
    const float MobSpawnRateMultiplier = 0.25f;
    // По одному мобу за спавн у кожній хвилі.
    const int MobPerSpawn = 1;
    // Межа туману піднята до 44 м. Вороги з'являються ще далі, тому навіть
    // у POL зору гравець бачить їх лише коли вони наближаються крізь туман.
    public const float FogEnd = 44f;
    public const float SpawnSafeDistance = 48f;

    void Start()
    {
        Configure(GameSession.SelectedMap);
    }

    void Configure(MapType map)
    {
        _map = map;
        _nextElite = 0;
        _nextBoss = 0;
        _nextSurge = 0;
        _surgeUntil = 0f;
        _finalRushAnnounced = false;
        _skeletonEliteIndex = Random.Range(0, 7);

        switch (map)
        {
            case MapType.Temple:
                _waves = new[]
                {
                    W(EnemyKind.Squid, 0f, 1.5f, 1, 22),
                    W(EnemyKind.Lamprey, 0.08f, 2.1f, 1, 14),
                    W(EnemyKind.Sploder, 0.2f, 2.6f, 1, 12),
                    W(EnemyKind.Cultist, 0.35f, 3.4f, 1, 10),
                    W(EnemyKind.WingedTerror, 0.55f, 6f, 1, 6),
                    W(EnemyKind.BigSploder, 0.68f, 7f, 1, 5),
                    W(EnemyKind.Spawner, 0.8f, 12f, 1, 3)
                };
                _elites = new[]
                {
                    S(120f, EnemyKind.Elder, EnemyTier.Elite),
                    S(260f, EnemyKind.Cultist, EnemyTier.Elite),
                    S(400f, EnemyKind.WingedTerror, EnemyTier.Elite),
                    S(540f, EnemyKind.Elder, EnemyTier.Elite),
                    S(700f, EnemyKind.Spawner, EnemyTier.Elite),
                    S(860f, EnemyKind.WingedTerror, EnemyTier.Elite),
                    S(1020f, EnemyKind.Elder, EnemyTier.Elite)
                };
                _bosses = new[]
                {
                    S(300f, EnemyKind.Reaper, EnemyTier.Boss),
                    S(600f, EnemyKind.Yog, EnemyTier.Boss),
                    S(900f, EnemyKind.Hastur, EnemyTier.Boss),
                    S(1140f, EnemyKind.ShubNiggurath, EnemyTier.Boss)
                };
                break;

            case MapType.PumpkinPatch:
                _waves = new[]
                {
                    W(EnemyKind.Pumpkin, 0f, 1.3f, 1, 26),
                    W(EnemyKind.Sploder, 0.12f, 2.2f, 1, 14),
                    W(EnemyKind.Ghost, 0.25f, 2.4f, 1, 16),
                    W(EnemyKind.Eyebat, 0.4f, 2.8f, 1, 14),
                    W(EnemyKind.Spawnling, 0.52f, 4f, 2, 12),
                    W(EnemyKind.Cultist, 0.66f, 5f, 1, 8),
                    W(EnemyKind.Witch, 0.8f, 9f, 1, 4)
                };
                _elites = new[]
                {
                    S(120f, EnemyKind.Witch, EnemyTier.Elite),
                    S(260f, EnemyKind.Eyebat, EnemyTier.Elite),
                    S(400f, EnemyKind.WingedTerror, EnemyTier.Elite),
                    S(560f, EnemyKind.Witch, EnemyTier.Elite),
                    S(720f, EnemyKind.BigSploder, EnemyTier.Elite),
                    S(880f, EnemyKind.WingedTerror, EnemyTier.Elite),
                    S(1040f, EnemyKind.Witch, EnemyTier.Elite)
                };
                _bosses = new[]
                {
                    S(300f, EnemyKind.Shoggoth, EnemyTier.Boss),
                    S(600f, EnemyKind.Hastur, EnemyTier.Boss),
                    S(900f, EnemyKind.ShubNiggurath, EnemyTier.Boss),
                    S(1140f, EnemyKind.Reaper, EnemyTier.Boss)
                };
                break;

            default:
                _waves = new[]
                {
                    W(EnemyKind.Tentacle, 0f, 1.15f, 1, 28),
                    W(EnemyKind.Sploder, 0.1f, 2.4f, 1, 14),
                    W(EnemyKind.Lurker, 0.22f, 1.7f, 1, 18),
                    W(EnemyKind.Lamprey, 0.36f, 2.3f, 1, 12),
                    W(EnemyKind.Spawnling, 0.5f, 4f, 2, 12),
                    W(EnemyKind.Cultist, 0.62f, 4.5f, 1, 9),
                    W(EnemyKind.Spawner, 0.76f, 11f, 1, 4)
                };
                _elites = new[]
                {
                    S(120f, EnemyKind.Witch, EnemyTier.Elite),
                    S(260f, EnemyKind.Elder, EnemyTier.Elite),
                    S(400f, EnemyKind.Lurker, EnemyTier.Elite),
                    S(540f, EnemyKind.WingedTerror, EnemyTier.Elite),
                    S(700f, EnemyKind.Elder, EnemyTier.Elite),
                    S(860f, EnemyKind.Witch, EnemyTier.Elite),
                    S(1020f, EnemyKind.WingedTerror, EnemyTier.Elite)
                };
                _bosses = new[]
                {
                    S(300f, EnemyKind.Hastur, EnemyTier.Boss),
                    S(600f, EnemyKind.Shoggoth, EnemyTier.Boss),
                    S(900f, EnemyKind.Yog, EnemyTier.Boss),
                    S(1140f, EnemyKind.ShubNiggurath, EnemyTier.Boss)
                };
                break;
        }

        _timers = new float[_waves.Length];
        for (int i = 0; i < _timers.Length; i++)
            _timers[i] = Random.Range(0.2f, 1.4f);
    }

    void Update()
    {
        var session = GameSession.Instance;
        if (session == null || !session.AllowsGameplay || session.Player == null)
            return;

        if (_map != GameSession.SelectedMap || _waves == null)
            Configure(GameSession.SelectedMap);

        float dt = Time.deltaTime;
        float t = session.Progress;

        while (_nextSurge < Surges.Length && session.Elapsed >= Surges[_nextSurge])
        {
            _surgeUntil = session.Elapsed + 25f;
            _nextSurge++;
            session.ShowNotice("ХВИЛЯ! НАТОВП ПРИСКОРЮЄТЬСЯ", 3f);
        }

        bool finalRush = session.Remaining <= 120f && session.Elapsed > 0f;
        if (finalRush && !_finalRushAnnounced)
        {
            _finalRushAnnounced = true;
            session.ShowNotice("ОСТАННІ 120 СЕКУНД ДО СВІТАНКУ!", 4f);
        }

        bool surge = session.Elapsed < _surgeUntil || finalRush;
        int maxAlive = 260 + GameSession.Darkness * 50 + (surge ? 70 : 0);
        float crowdScale = Mathf.Clamp(ArenaBuilder.WalkLimit / 126f, 1f, 3f);
        // Спаунрейт не прискорюється ні з часом, ні під час surge —
        // всі хвилі працюють з однаковим інтервалом.
        float spawnInterval = UniformSpawnInterval / MobSpawnRateMultiplier * 0.6f;

        for (int i = 0; i < _waves.Length; i++)
        {
            var wave = _waves[i];
            if (t < wave.Unlock)
                continue;

            _timers[i] -= dt;
            if (_timers[i] > 0f)
                continue;

            _timers[i] = spawnInterval;
            int waveMax = Mathf.Max(2, Mathf.RoundToInt(wave.Max * crowdScale * 1.4f)) + (surge ? 4 : 0);
            if (Enemy.Alive >= maxAlive || Enemy.Count(wave.Kind) >= waveMax)
                continue;

            for (int n = 0; n < MobPerSpawn; n++)
                Spawn(wave.Kind, EnemyTier.Small);
        }

        while (_nextElite < _elites.Length && session.Elapsed >= _elites[_nextElite].Time)
        {
            var elite = _elites[_nextElite];
            bool skeleton = _nextElite == _skeletonEliteIndex;
            Spawn(elite.Kind, elite.Tier, skeleton);
            _nextElite++;
            session.ShowNotice("ЕЛІТА НАБЛИЖАЄТЬСЯ", 3f);
        }

        while (_nextBoss < _bosses.Length && session.Elapsed >= _bosses[_nextBoss].Time)
        {
            var boss = _bosses[_nextBoss];
            // Останній бос на кожній мапі — посилений скелет із FabSkeleton.
            bool skeleton = _nextBoss == _bosses.Length - 1;
            Spawn(boss.Kind, boss.Tier, skeleton);
            _nextBoss++;
            AudioManager.Instance?.Play("boss_spawn", 0.9f, 0.85f, 0f);
            session.ShowNotice("БОС НАБЛИЖАЄТЬСЯ!", 3.5f);
        }
    }

    static Wave W(EnemyKind kind, float unlock, float interval, int per, int max)
    {
        // Інтервал і кількість за спавн однакові для всіх типів;
        // значення в таблицях хвиль лишаються як довідкові.
        return new Wave
        {
            Kind = kind,
            Unlock = unlock,
            Interval = UniformSpawnInterval,
            Per = MobPerSpawn,
            Max = max
        };
    }

    static Schedule S(float time, EnemyKind kind, EnemyTier tier)
    {
        return new Schedule { Time = time, Kind = kind, Tier = tier };
    }

    static void Spawn(EnemyKind kind, EnemyTier tier, bool skeletonVisual = false)
    {
        var session = GameSession.Instance;
        if (session == null || session.Player == null)
            return;

        Vector3 player = session.Player.transform.position;
        float limit = ArenaBuilder.WalkLimit;
        // Спавн завжди відбувається за межею туману: навіть у полі зору
        // ворог з'являється невидимим і підходить уже в межах видимості.
        float minRadius = 44f;
        float maxRadius = 54f;
        if (tier == EnemyTier.Elite)
        {
            minRadius = 46f;
            maxRadius = 58f;
        }
        else if (tier == EnemyTier.Boss)
        {
            minRadius = 48f;
            maxRadius = 64f;
        }
        var fps = session.Player.GetComponent<FirstPersonController>();
        var camera = fps != null ? fps.GetComponentInChildren<Camera>() : null;
        Vector3 forward = camera != null ? camera.transform.forward : session.Player.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.000001f)
            forward = Vector3.forward;
        forward.Normalize();

        // Горизонтальний конус видимості. Кандидат у конусі допустимий лише
        // якщо він далі за межу туману — тоді гравець його не бачить.
        float halfFov = 48f;
        if (camera != null)
        {
            float vertical = camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            halfFov = Mathf.Atan(Mathf.Tan(vertical) * Mathf.Max(1f, camera.aspect)) * Mathf.Rad2Deg + 12f;
        }

        Vector3? chosen = null;
        for (int attempt = 0; attempt < 16 && !chosen.HasValue; attempt++)
        {
            Vector2 circle = Random.insideUnitCircle.normalized;
            Vector3 dir = new Vector3(circle.x, 0f, circle.y);
            float radius = Random.Range(minRadius, maxRadius);
            // Усі радіуси ≥44 м, тож навіть у конусі зору моб ховається туманом.
            if (radius < SpawnSafeDistance && Vector3.Angle(forward, dir) < halfFov)
                continue;

            Vector3 candidate = Candidate(player, dir, radius, limit);
            if (Blocked(candidate))
                continue;
            if ((candidate - player).sqrMagnitude < SpawnSafeDistance * SpawnSafeDistance)
                continue;
            chosen = candidate;
        }

        // Якщо навколо щільний лабіринт — жорсткий fallback строго за спиною.
        for (int attempt = 0; attempt < 8 && !chosen.HasValue; attempt++)
        {
            Vector3 dir = Quaternion.Euler(0f, 180f + Random.Range(-55f, 55f), 0f) * forward;
            Vector3 candidate = Candidate(player, dir, Random.Range(minRadius, maxRadius), limit);
            if (!Blocked(candidate))
                chosen = candidate;
        }

        if (chosen.HasValue)
            Enemy.Spawn(chosen.Value, kind, tier, skeletonVisual);
    }

    static Vector3 Candidate(Vector3 player, Vector3 dir, float radius, float limit)
    {
        Vector3 pos = player + dir.normalized * radius;
        pos.x = Mathf.Clamp(pos.x, -limit, limit);
        pos.z = Mathf.Clamp(pos.z, -limit, limit);
        pos.y = 0f;
        return pos;
    }

    static bool Blocked(Vector3 pos)
    {
        var hits = Physics.OverlapSphere(pos + Vector3.up * 0.9f, 1.15f);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null || hit.GetComponent<Enemy>() != null || hit.GetComponent<PlayerStats>() != null)
                continue;

            string name = hit.gameObject.name;
            if (name == "Floor" || name.StartsWith("Wall") || hit.bounds.max.y <= 0.35f)
                continue;
            return true;
        }
        return false;
    }
}
