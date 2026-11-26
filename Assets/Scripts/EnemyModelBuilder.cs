using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Один анімований вузол моделі ворога.
/// Mode: 0 — помах навколо Z, 1 — хитання навколо X, 2 — обертання навколо Y,
/// 3 — пульсація масштабу, 4 — вертикальний поплавок, 5 — хитання навколо Z,
/// 6 — помах навколо X.
/// </summary>
sealed class EnemyRigPart
{
    public Transform Node;
    public Vector3 RestEuler;
    public Vector3 RestPos;
    public Vector3 RestScale;
    public float Phase;
    public float Amount;
    public int Mode;
}

sealed class EnemyModelRig
{
    readonly List<EnemyRigPart> _parts = new List<EnemyRigPart>();

    public void Add(Transform node, int mode, float amount, float phase)
    {
        if (node == null)
            return;

        _parts.Add(new EnemyRigPart
        {
            Node = node,
            RestEuler = node.localEulerAngles,
            RestPos = node.localPosition,
            RestScale = node.localScale,
            Mode = mode,
            Amount = amount,
            Phase = phase
        });
    }

    public void Animate(float time, float energy)
    {
        for (int i = 0; i < _parts.Count; i++)
        {
            var p = _parts[i];
            if (p.Node == null)
                continue;

            float t = time + p.Phase;
            float wave = Mathf.Sin(t * 7f) * p.Amount * energy;

            switch (p.Mode)
            {
                case 0:
                    p.Node.localRotation = Quaternion.Euler(p.RestEuler.x, p.RestEuler.y, p.RestEuler.z + wave);
                    break;
                case 1:
                    p.Node.localRotation = Quaternion.Euler(p.RestEuler.x + wave, p.RestEuler.y, p.RestEuler.z);
                    break;
                case 2:
                    p.Node.localRotation = Quaternion.Euler(p.RestEuler.x, p.RestEuler.y + time * p.Amount * 16f, p.RestEuler.z);
                    break;
                case 3:
                    p.Node.localScale = p.RestScale * (1f + wave * 0.09f);
                    break;
                case 4:
                    p.Node.localPosition = p.RestPos + Vector3.up * wave * 0.1f;
                    break;
                case 5:
                    p.Node.localRotation = Quaternion.Euler(p.RestEuler.x, p.RestEuler.y, p.RestEuler.z + wave * 0.6f);
                    break;
                case 6:
                    p.Node.localRotation = Quaternion.Euler(p.RestEuler.x + wave * 0.8f, p.RestEuler.y, p.RestEuler.z);
                    break;
            }
        }
    }
}

/// <summary>
/// Беру модель ворога з примітивів. Усі матеріали кешуються, колаiderи видаляються.
/// </summary>
static class EnemyModelBuilder
{
    static readonly Dictionary<int, Material> _materials = new Dictionary<int, Material>();

    static Transform _root;
    static EnemyModelRig _rig;
    static EnemyKind _kind;
    static EnemyTier _tier;
    static float _h;
    static float _r;
    static Color _color;
    static Color _emission;

    public static EnemyModelRig Build(Transform root, EnemyKind kind, EnemyTier tier,
        float height, float radius, Color color, Color emission)
    {
        _root = root;
        _rig = new EnemyModelRig();
        _kind = kind;
        _tier = tier;
        _h = height;
        _r = radius;
        _color = color;
        _emission = emission;

        switch (kind)
        {
            case EnemyKind.Tentacle: BuildTentacle(); break;
            case EnemyKind.Sploder: BuildBomber(false); break;
            case EnemyKind.BigSploder: BuildBomber(true); break;
            case EnemyKind.Lurker: BuildLurker(); break;
            case EnemyKind.Lamprey: BuildLamprey(); break;
            case EnemyKind.Spawnling: BuildSpawnling(); break;
            case EnemyKind.Cultist: BuildCultist(); break;
            case EnemyKind.Squid: BuildSquid(); break;
            case EnemyKind.Pumpkin: BuildPumpkin(); break;
            case EnemyKind.Ghost: BuildGhost(); break;
            case EnemyKind.Eyebat: BuildEyebat(); break;
            case EnemyKind.Witch: BuildWitch(); break;
            case EnemyKind.Elder: BuildElder(); break;
            case EnemyKind.Spawner: BuildSpawner(); break;
            case EnemyKind.WingedTerror: BuildWingedTerror(); break;
            case EnemyKind.Shoggoth: BuildShoggoth(); break;
            case EnemyKind.Reaper: BuildReaper(); break;
            case EnemyKind.Hastur: BuildHastur(); break;
            case EnemyKind.Yog: BuildYog(); break;
            case EnemyKind.ShubNiggurath: BuildShubNiggurath(); break;
        }

        AddTierDecor();
        return _rig;
    }

    // ---------- Моделі ----------

    static void BuildTentacle()
    {
        Orb("Base", new Vector3(0f, _h * 0.13f, 0f), new Vector3(_r * 1.8f, _h * 0.26f, _r * 1.8f), 2);
        var stalk = Cyl("Stalk", new Vector3(0f, _h * 0.42f, 0f), _r * 0.48f, _h * 0.62f, 0,
            Quaternion.Euler(8f, 0f, 5f), null, 1, 6f, 0.4f);
        Orb("Bulb", new Vector3(0f, _h * 0.78f, _r * 0.18f), new Vector3(_r * 1.35f, _r * 1.05f, _r * 1.5f), 1, null, 3, 2.2f);
        Orb("Maw", new Vector3(0f, _h * 0.7f, _r * 0.95f), new Vector3(_r * 0.62f, _r * 0.42f, _r * 0.25f), 2);
        AddEyes(_h * 0.86f, _r * 0.88f, _r * 0.4f, _r * 0.2f);
        AddTentacles(5, _r * 0.7f, _h * 0.24f, _r * 0.13f, _h * 0.34f, 0);
        AddTeeth(_h * 0.7f, _r * 1.08f, _r * 0.5f, 4);
    }

    static void BuildBomber(bool big)
    {
        var core = Orb("Core", new Vector3(0f, _h * 0.48f, 0f),
            new Vector3(_r * 1.7f, _h * 0.68f, _r * 1.7f), 0, null, 3, big ? 3.4f : 4.5f);
        Orb("Belly", new Vector3(0f, _h * 0.38f, _r * 0.65f), new Vector3(_r * 1.1f, _h * 0.32f, _r * 0.7f), 7);

        int spikes = big ? 12 : 8;
        for (int i = 0; i < spikes; i++)
        {
            float a = i * 360f / spikes;
            float y = _h * (0.25f + (i % 3) * 0.18f);
            Quaternion rot = Quaternion.Euler(Mathf.Sin(a * Mathf.Deg2Rad) * 42f, a, Mathf.Cos(a * Mathf.Deg2Rad) * 42f);
            Vector3 dir = rot * Vector3.forward;
            Box("Spike", new Vector3(0f, _h * 0.48f, 0f) + dir * _r * 0.9f + Vector3.up * (y - _h * 0.48f) * 0.3f,
                new Vector3(_r * 0.2f, _r * 0.5f, _r * 0.2f), 5, rot);
        }

        Cyl("Fuse", new Vector3(0f, _h * 0.9f, 0f), _r * 0.1f, _h * 0.3f, 6, Quaternion.Euler(0f, 0f, -16f));
        Orb("Spark", new Vector3(_r * 0.1f, _h * 1.06f, 0f), Vector3.one * _r * 0.34f, 3, null, 4, 1.5f);
        AddEyes(_h * 0.6f, _r * 0.85f, _r * 0.38f, _r * 0.2f);

        if (big)
        {
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f + 45f;
                Vector3 p = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * _r * 1.35f, _h * 0.3f,
                    Mathf.Sin(a * Mathf.Deg2Rad) * _r * 1.35f);
                Orb("ClusterBomb", p, Vector3.one * _r * 0.7f, 7, null, 3, 2f);
            }
        }
    }

    static void BuildLurker()
    {
        Box("Body", new Vector3(0f, _h * 0.45f, 0f), new Vector3(_r * 1.5f, _h * 0.42f, _r * 2f), 0);
        Orb("Head", new Vector3(0f, _h * 0.56f, _r * 1.05f), new Vector3(_r * 1.2f, _r * 0.95f, _r * 1.1f), 1);
        Box("Jaw", new Vector3(0f, _h * 0.38f, _r * 1.45f), new Vector3(_r * 0.95f, _r * 0.22f, _r * 0.7f), 2);
        AddEyes(_h * 0.66f, _r * 1.55f, _r * 0.42f, _r * 0.22f);
        AddTeeth(_h * 0.44f, _r * 1.75f, _r * 0.7f, 4);

        Cyl("Tail", new Vector3(0f, _h * 0.5f, -_r * 1.5f), _r * 0.16f, _r * 1.6f, 0,
            Quaternion.Euler(70f, 0f, 0f), null, 1, 10f);
        AddLegs(4, _r * 0.95f, _h * 0.32f, _r * 0.14f, 12f);
        AddHorns(_h * 0.9f, _r * 0.55f, 2, 2);
    }

    static void BuildLamprey()
    {
        var body = Cyl("Body", new Vector3(0f, _h * 0.5f, 0f), _r * 0.55f, _h * 0.95f, 0,
            Quaternion.Euler(90f, 0f, 0f), null, 6, 5f);
        Orb("Head", new Vector3(0f, _h * 0.52f, _h * 0.44f), new Vector3(_r * 1.3f, _r * 1.05f, _r * 1.2f), 1);
        Orb("Mouth", new Vector3(0f, _h * 0.46f, _h * 0.44f + _r * 0.58f), new Vector3(_r * 0.9f, _r * 0.75f, _r * 0.2f), 2);

        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Orb("Tooth", new Vector3(Mathf.Cos(a) * _r * 0.36f, _h * 0.46f + Mathf.Sin(a) * _r * 0.3f,
                _h * 0.44f + _r * 0.72f), Vector3.one * _r * 0.12f, 4);
        }

        AddEyes(_h * 0.68f, _h * 0.44f + _r * 0.62f, _r * 0.45f, _r * 0.16f);

        for (int s = -1; s <= 1; s += 2)
        {
            var fin = Box("Fin", new Vector3(s * _r * 0.85f, _h * 0.48f, -_h * 0.15f),
                new Vector3(_r * 0.9f, _h * 0.06f, _r * 0.9f), 1, Quaternion.Euler(0f, s * 22f, s * 18f), null, 0, s * 18f, s);
            Box("FinRay", new Vector3(s * _r * 1.1f, _h * 0.48f, -_h * 0.15f),
                new Vector3(_r * 0.08f, _h * 0.03f, _r * 0.8f), 3, Quaternion.identity, fin);
        }

        var tail = Box("Tail", new Vector3(0f, _h * 0.5f, -_h * 0.55f),
            new Vector3(_r * 0.15f, _h * 0.45f, _r * 0.9f), 1, Quaternion.identity, null, 1, 16f, 1.2f);
        Box("TailRay", Vector3.zero, new Vector3(_r * 0.06f, _h * 0.36f, _r * 0.7f), 3, Quaternion.identity, tail);
    }

    static void BuildSpawnling()
    {
        Orb("Body", new Vector3(0f, _h * 0.34f, 0f), new Vector3(_r * 1.1f, _h * 0.42f, _r * 1.05f), 0);
        var head = Orb("Head", new Vector3(0f, _h * 0.68f, _r * 0.15f), new Vector3(_r * 1.45f, _r * 1.25f, _r * 1.35f), 1, null, 4, 1.6f);
        Orb("Eye", new Vector3(0f, _h * 0.72f, _r * 0.85f), Vector3.one * _r * 0.55f, 4, head);
        Orb("Pupil", new Vector3(0f, _h * 0.72f, _r * 1.05f), Vector3.one * _r * 0.25f, 2, head);
        AddLegs(2, _r * 0.45f, _h * 0.2f, _r * 0.12f, 14f);
        AddHorns(_h * 0.95f, _r * 0.35f, 2, 2);
    }

    static void BuildCultist()
    {
        Cyl("Robe", new Vector3(0f, _h * 0.36f, 0f), _r * 1.15f, _h * 0.68f, 0);
        Cyl("Shoulders", new Vector3(0f, _h * 0.67f, 0f), _r * 1.05f, _h * 0.1f, 1);
        Orb("Hood", new Vector3(0f, _h * 0.86f, 0f), new Vector3(_r * 1.25f, _r * 1.2f, _r * 1.2f), 0, null, 4, 0.8f);
        Orb("Face", new Vector3(0f, _h * 0.84f, _r * 0.65f), new Vector3(_r * 0.75f, _r * 0.7f, _r * 0.22f), 2);
        AddEyes(_h * 0.86f, _r * 0.8f, _r * 0.27f, _r * 0.12f);

        var staff = Group("Staff", new Vector3(_r * 1.3f, _h * 0.45f, 0f), Quaternion.identity, 5, 7f, 0.5f);
        Cyl("Shaft", new Vector3(0f, 0f, 0f), _r * 0.08f, _h * 0.9f, 6, Quaternion.identity, staff);
        Orb("Orb", new Vector3(0f, _h * 0.5f, 0f), Vector3.one * _r * 0.45f, 3, staff, 3, 3f);
        AddRuneRing(_h * 0.5f, _r * 0.65f, 4, 3, staff, 0.4f);
    }

    static void BuildSquid()
    {
        Orb("Mantle", new Vector3(0f, _h * 0.68f, 0f), new Vector3(_r * 1.25f, _h * 0.5f, _r * 1.25f), 0, null, 3, 2f);
        Orb("Head", new Vector3(0f, _h * 0.4f, _r * 0.1f), new Vector3(_r * 1.2f, _h * 0.32f, _r * 1.15f), 1);
        Orb("EyeL", new Vector3(-_r * 0.55f, _h * 0.42f, _r * 0.78f), Vector3.one * _r * 0.36f, 4);
        Orb("EyeR", new Vector3(_r * 0.55f, _h * 0.42f, _r * 0.78f), Vector3.one * _r * 0.36f, 4);
        Orb("PupilL", new Vector3(-_r * 0.55f, _h * 0.42f, _r * 0.98f), Vector3.one * _r * 0.16f, 2);
        Orb("PupilR", new Vector3(_r * 0.55f, _h * 0.42f, _r * 0.98f), Vector3.one * _r * 0.16f, 2);
        AddTentacles(6, _r * 0.7f, _h * 0.24f, _r * 0.14f, _h * 0.36f, 0);
    }

    static void BuildPumpkin()
    {
        var body = Orb("Pumpkin", new Vector3(0f, _h * 0.5f, 0f),
            new Vector3(_r * 1.7f, _h * 0.68f, _r * 1.7f), 0, null, 3, 2.4f);

        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Orb("Rib", new Vector3(Mathf.Cos(a) * _r * 0.72f, _h * 0.5f, Mathf.Sin(a) * _r * 0.72f),
                new Vector3(_r * 0.16f, _h * 0.6f, _r * 0.16f), 5, body, 5, 1.5f);
        }

        Cyl("Stem", new Vector3(0f, _h * 0.92f, 0f), _r * 0.12f, _h * 0.24f, 6, Quaternion.Euler(0f, 0f, 14f));
        Box("EyeL", new Vector3(-_r * 0.42f, _h * 0.62f, _r * 0.86f), new Vector3(_r * 0.3f, _r * 0.22f, _r * 0.12f), 3, Quaternion.Euler(0f, 0f, 18f));
        Box("EyeR", new Vector3(_r * 0.42f, _h * 0.62f, _r * 0.86f), new Vector3(_r * 0.3f, _r * 0.22f, _r * 0.12f), 3, Quaternion.Euler(0f, 0f, -18f));
        Box("Mouth", new Vector3(0f, _h * 0.36f, _r * 0.92f), new Vector3(_r * 0.9f, _r * 0.2f, _r * 0.12f), 3);
        AddTeeth(_h * 0.4f, _r * 1f, _r * 0.7f, 3, false);
        AddLegs(2, _r * 0.55f, _h * 0.18f, _r * 0.12f, 9f);
    }

    static void BuildGhost()
    {
        var body = Group("Body", new Vector3(0f, _h * 0.5f, 0f), Quaternion.identity, 4, 3.2f);
        Orb("Head", new Vector3(0f, _h * 0.18f, 0f), new Vector3(_r * 1.35f, _r * 1.3f, _r * 1.25f), 1, body);
        Orb("Sheet", new Vector3(0f, -_h * 0.12f, 0f), new Vector3(_r * 1.5f, _h * 0.42f, _r * 1.45f), 0, body);
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            Orb("Tail", new Vector3(Mathf.Cos(a) * _r * 0.6f, -_h * 0.36f, Mathf.Sin(a) * _r * 0.6f),
                new Vector3(_r * 0.5f, _h * 0.18f, _r * 0.5f), 0, body, 5, 8f);
        }
        Orb("EyeL", new Vector3(-_r * 0.38f, _h * 0.2f, _r * 0.75f), new Vector3(_r * 0.3f, _r * 0.42f, _r * 0.12f), 2, body);
        Orb("EyeR", new Vector3(_r * 0.38f, _h * 0.2f, _r * 0.75f), new Vector3(_r * 0.3f, _r * 0.42f, _r * 0.12f), 2, body);
        Orb("Mouth", new Vector3(0f, -_h * 0.02f, _r * 0.8f), new Vector3(_r * 0.32f, _r * 0.4f, _r * 0.1f), 2, body);

        for (int s = -1; s <= 1; s += 2)
        {
            var arm = Group("Arm", new Vector3(s * _r * 1.05f, _h * 0.52f, 0f), Quaternion.identity, 0, s * 20f, 1f);
            Orb("Hand", new Vector3(s * _r * 0.5f, -_r * 0.15f, 0f), Vector3.one * _r * 0.5f, 1, arm);
        }
    }

    static void BuildEyebat()
    {
        var eye = Orb("Eye", new Vector3(0f, _h * 0.5f, 0f), Vector3.one * _r * 1.5f, 4, null, 3, 2.6f);
        Orb("Iris", new Vector3(0f, _h * 0.5f, _r * 0.72f), Vector3.one * _r * 0.78f, 3, eye);
        Orb("Pupil", new Vector3(0f, _h * 0.5f, _r * 0.98f), Vector3.one * _r * 0.38f, 2, eye);
        AddTeeth(_h * 0.34f, _r * 1.12f, _r * 0.55f, 4);

        AddWings(_h * 0.58f, _r * 2.2f, _r * 1.1f, 0);

        for (int s = -1; s <= 1; s += 2)
        {
            Cyl("Ear", new Vector3(s * _r * 0.75f, _h * 0.9f, 0f), _r * 0.12f, _r * 0.55f, 2,
                Quaternion.Euler(0f, 0f, s * 28f));
        }
    }

    static void BuildWitch()
    {
        Cyl("Robe", new Vector3(0f, _h * 0.34f, 0f), _r * 1.1f, _h * 0.64f, 0);
        Orb("Head", new Vector3(0f, _h * 0.76f, 0f), new Vector3(_r, _r, _r), 1);
        AddEyes(_h * 0.78f, _r * 0.55f, _r * 0.25f, _r * 0.11f);

        Cyl("HatBrim", new Vector3(0f, _h * 0.95f, 0f), _r * 1.5f, _r * 0.1f, 2);
        Cyl("HatLow", new Vector3(0f, _h * 1.04f, 0f), _r * 0.72f, _r * 0.24f, 2);
        Cyl("HatMid", new Vector3(0f, _h * 1.16f, 0f), _r * 0.45f, _r * 0.24f, 2);
        Cyl("HatTip", new Vector3(_r * 0.12f, _h * 1.28f, 0f), _r * 0.18f, _r * 0.3f, 2, Quaternion.Euler(0f, 0f, -18f));
        Orb("HatGem", new Vector3(0f, _h * 1.04f, _r * 0.7f), Vector3.one * _r * 0.2f, 3);

        var staff = Group("Staff", new Vector3(_r * 1.2f, _h * 0.45f, 0f), Quaternion.identity, 5, 6f, 1.2f);
        Cyl("Shaft", Vector3.zero, _r * 0.07f, _h * 0.9f, 6, Quaternion.identity, staff);
        Orb("Gem", new Vector3(0f, _h * 0.5f, 0f), Vector3.one * _r * 0.4f, 3, staff, 3, 3f);
    }

    static void BuildElder()
    {
        Cyl("Robe", new Vector3(0f, _h * 0.38f, 0f), _r * 1.25f, _h * 0.7f, 1);
        Orb("Head", new Vector3(0f, _h * 0.82f, 0f), new Vector3(_r, _r * 1.05f, _r), 1);
        AddEyes(_h * 0.86f, _r * 0.55f, _r * 0.24f, _r * 0.11f);

        for (int i = 0; i < 5; i++)
        {
            Box("Beard", new Vector3((i - 2) * _r * 0.22f, _h * (0.6f - i * 0.02f), _r * 0.55f),
                new Vector3(_r * 0.18f, _h * 0.25f, _r * 0.18f), 4, Quaternion.Euler(0f, 0f, (i - 2) * 5f), null, 5, 2.5f);
        }

        AddHorns(_h * 1.02f, _r * 0.5f, 5, 3);

        var staff = Group("Staff", new Vector3(_r * 1.35f, _h * 0.5f, 0f), Quaternion.identity, 5, 5f, 2f);
        Cyl("Shaft", Vector3.zero, _r * 0.08f, _h * 0.95f, 6, Quaternion.identity, staff);
        Orb("Sun", new Vector3(0f, _h * 0.54f, 0f), Vector3.one * _r * 0.55f, 3, staff, 2, 0.5f);
        AddRuneRing(_h * 0.54f, _r * 0.8f, 6, 3, staff, 0.5f);
    }

    static void BuildSpawner()
    {
        Orb("Pod", new Vector3(0f, _h * 0.42f, 0f), new Vector3(_r * 1.6f, _h * 0.62f, _r * 1.6f), 0);
        var core = Orb("Core", new Vector3(0f, _h * 0.48f, _r * 0.85f), Vector3.one * _r * 0.75f, 3, null, 3, 4f);
        Orb("CoreInner", Vector3.zero, Vector3.one * 0.55f, 4, core);

        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Mathf.Deg2Rad;
            Orb("Egg", new Vector3(Mathf.Cos(a) * _r * 1.15f, _h * (0.22f + (i % 2) * 0.2f), Mathf.Sin(a) * _r * 1.15f),
                new Vector3(_r * 0.5f, _r * 0.65f, _r * 0.5f), 1, null, 3, 1.8f);
        }

        AddTentacles(7, _r * 0.9f, _h * 0.2f, _r * 0.12f, _h * 0.3f, 0);
    }

    static void BuildWingedTerror()
    {
        Box("Body", new Vector3(0f, _h * 0.45f, 0f), new Vector3(_r * 1.1f, _h * 0.55f, _r * 1.35f), 0);
        Orb("Head", new Vector3(0f, _h * 0.72f, _r * 0.7f), new Vector3(_r, _r * 0.9f, _r), 1);
        AddEyes(_h * 0.76f, _r * 1.2f, _r * 0.35f, _r * 0.18f);
        AddTeeth(_h * 0.56f, _r * 1.2f, _r * 0.55f, 4, false);
        AddHorns(_h * 0.98f, _r * 0.45f, 2, 2);
        AddWings(_h * 0.66f, _r * 2.8f, _r * 1.5f, 0);

        for (int s = -1; s <= 1; s += 2)
        {
            Cyl("Claw", new Vector3(s * _r * 0.6f, _h * 0.2f, _r * 0.4f), _r * 0.1f, _h * 0.32f, 4,
                Quaternion.Euler(28f, 0f, s * 12f), null, 1, 10f, s);
        }
    }

    static void BuildShoggoth()
    {
        for (int i = 0; i < 7; i++)
        {
            float a = i * 51f * Mathf.Deg2Rad;
            float rr = i == 0 ? 0f : _r * 0.85f;
            Orb("Blob", new Vector3(Mathf.Cos(a) * rr, _h * (0.38f + (i % 3) * 0.14f), Mathf.Sin(a) * rr),
                Vector3.one * _r * (i == 0 ? 1.9f : 1.2f), i % 2 == 0 ? 0 : 1, null, 3, 1.5f);
            Orb("BlobEye", new Vector3(Mathf.Cos(a) * rr * 1.15f, _h * (0.5f + (i % 3) * 0.14f), Mathf.Sin(a) * rr * 1.15f + _r * 0.5f),
                Vector3.one * _r * 0.3f, 3);
        }

        AddTentacles(9, _r * 1.1f, _h * 0.28f, _r * 0.17f, _h * 0.4f, 0);
        AddRuneRing(_h * 0.72f, _r * 1.5f, 7, 3, null, 0.25f);
    }

    static void BuildReaper()
    {
        Cyl("Robe", new Vector3(0f, _h * 0.36f, 0f), _r * 1.2f, _h * 0.68f, 2);
        Orb("Hood", new Vector3(0f, _h * 0.76f, 0f), new Vector3(_r * 1.15f, _r * 1.2f, _r * 1.1f), 2);
        Orb("Void", new Vector3(0f, _h * 0.74f, _r * 0.6f), new Vector3(_r * 0.7f, _r * 0.75f, _r * 0.18f), 5);
        AddEyes(_h * 0.78f, _r * 0.75f, _r * 0.26f, _r * 0.14f);

        for (int i = 0; i < 4; i++)
        {
            Box("Rib", new Vector3(0f, _h * (0.32f + i * 0.09f), _r * 0.75f),
                new Vector3(_r * 1.15f, _r * 0.07f, _r * 0.12f), 4);
        }

        var scythe = Group("Scythe", new Vector3(_r * 1.4f, _h * 0.5f, 0f), Quaternion.identity, 5, 6f, 0.8f);
        Cyl("Shaft", Vector3.zero, _r * 0.08f, _h * 1.1f, 6, Quaternion.identity, scythe);
        for (int i = 0; i < 6; i++)
        {
            float a = Mathf.Lerp(-70f, 60f, i / 5f);
            Box("Blade", new Vector3(-_r * 0.35f + Mathf.Cos(a * Mathf.Deg2Rad) * _r * 0.85f,
                    _h * 0.58f + Mathf.Sin(a * Mathf.Deg2Rad) * _r * 0.7f, 0f),
                new Vector3(_r * 0.5f, _r * 0.1f, _r * 0.12f), 4, Quaternion.Euler(0f, 0f, a + 90f), scythe);
        }
    }

    static void BuildHastur()
    {
        var head = Group("Head", new Vector3(0f, _h * 0.58f, 0f), Quaternion.identity, 4, 2.6f);
        Orb("Face", Vector3.zero, new Vector3(_r * 1.6f, _r * 1.5f, _r * 1.5f), 0, head);
        AddEyes(0f, _r * 0.75f, _r * 0.45f, _r * 0.22f, head);
        Orb("Maw", new Vector3(0f, -_r * 0.45f, _r * 0.85f), new Vector3(_r * 0.6f, _r * 0.4f, _r * 0.15f), 2, head);

        for (int i = 0; i < 10; i++)
        {
            float a = i * 36f * Mathf.Deg2Rad;
            var g = Group("Whisker", new Vector3(Mathf.Cos(a) * _r * 0.9f, Mathf.Sin(a) * _r * 0.9f, 0f),
                Quaternion.identity, 1, 12f, i * 0.5f, head);
            Cyl("Tentacle", new Vector3(Mathf.Cos(a) * _r * 0.5f, Mathf.Sin(a) * _r * 0.5f, 0f),
                _r * 0.07f, _r * 1.5f, 0, Quaternion.Euler(0f, 0f, -a * Mathf.Rad2Deg + 90f), g);
        }

        AddRuneRing(0f, _r * 1.9f, 8, 3, head, 0.35f);
    }

    static void BuildYog()
    {
        var core = Orb("Core", new Vector3(0f, _h * 0.55f, 0f), Vector3.one * _r * 1.9f, 0, null, 3, 3f);
        Orb("Inner", Vector3.zero, Vector3.one * _r * 0.55f, 7, core);

        AddRuneRing(_h * 0.55f, _r * 2.2f, 10, 3, null, 0.3f);
        AddRuneRing(_h * 0.55f, _r * 1.8f, 8, 7, null, -0.45f);

        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            float y = _h * (0.35f + (i % 3) * 0.16f);
            var eye = Group("EyeStalk", new Vector3(Mathf.Cos(a) * _r * 1.4f, y, Mathf.Sin(a) * _r * 1.4f),
                Quaternion.identity, 1, 8f, i * 0.7f);
            Orb("Eye", Vector3.zero, Vector3.one * _r * 0.38f, 4, eye);
            Orb("Pupil", new Vector3(0f, 0f, _r * 0.22f), Vector3.one * _r * 0.16f, 2, eye);
        }

        AddTentacles(8, _r * 1.2f, _h * 0.25f, _r * 0.18f, _h * 0.42f, 0);
    }

    static void BuildShubNiggurath()
    {
        Orb("Mass", new Vector3(0f, _h * 0.45f, 0f), new Vector3(_r * 2.1f, _h * 0.65f, _r * 2f), 0, null, 3, 2f);

        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * _r * 1.15f;
            var head = Group("Head", new Vector3(x, _h * (0.8f + (i == 1 ? 0.12f : 0f)), _r * 0.5f),
                Quaternion.Euler(0f, (i - 1) * 12f, 0f), 4, 2.2f + i, i * 1.4f);
            Orb("Face", Vector3.zero, Vector3.one * _r * 0.9f, 1, head);
            AddEyes(0f, _r * 0.5f, _r * 0.26f, _r * 0.16f, head);
            Orb("Maw", new Vector3(0f, -_r * 0.28f, _r * 0.55f), new Vector3(_r * 0.42f, _r * 0.3f, _r * 0.12f), 2, head);
            AddHorns(0f, _r * 0.65f, 2, 4, head);
        }

        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Mathf.Deg2Rad;
            Orb("Sac", new Vector3(Mathf.Cos(a) * _r * 1.25f, _h * 0.28f, Mathf.Sin(a) * _r * 1.25f),
                Vector3.one * _r * 0.65f, i % 2 == 0 ? 1 : 0, null, 3, 1.8f);
        }

        AddTentacles(10, _r * 1.3f, _h * 0.22f, _r * 0.2f, _h * 0.45f, 0);
        AddRuneRing(_h * 0.7f, _r * 2.1f, 9, 3, null, 0.25f);
    }

    // ---------- Спільні деталі ----------

    static void AddEyes(float y, float z, float spread, float size, Transform parent = null)
    {
        for (int s = -1; s <= 1; s += 2)
        {
            Orb("Eye", new Vector3(s * spread, y, z), Vector3.one * size, 3, parent);
            Orb("Pupil", new Vector3(s * spread, y, z + size * 0.45f), Vector3.one * size * 0.42f, 2, parent);
        }
    }

    static void AddTeeth(float y, float z, float width, int count, bool upper = true)
    {
        for (int i = 0; i < count; i++)
        {
            float x = count == 1 ? 0f : Mathf.Lerp(-width * 0.5f, width * 0.5f, i / (float)(count - 1));
            Box("Tooth", new Vector3(x, y, z), new Vector3(width * 0.11f, _r * 0.22f, _r * 0.08f), 4,
                Quaternion.Euler(0f, 0f, upper ? 180f : 0f));
        }
    }

    static void AddHorns(float y, float spread, int count, int palette, Transform parent = null)
    {
        if (count <= 1)
        {
            Cyl("Horn", new Vector3(0f, y, 0f), _r * 0.1f, _r * 0.7f, palette,
                Quaternion.identity, parent, 5, 5f);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1) * 2f - 1f;
            Cyl("Horn", new Vector3(t * spread, y - Mathf.Abs(t) * _r * 0.12f, 0f),
                _r * 0.08f, _r * (0.5f + (1f - Mathf.Abs(t)) * 0.25f), palette,
                Quaternion.Euler(0f, 0f, -t * 26f), parent, 5, 4f, i * 0.4f);
        }
    }

    static void AddLegs(int count, float ring, float y, float thickness, float amount, Transform parent = null)
    {
        for (int i = 0; i < count; i++)
        {
            float a = i * 360f / count + (count == 2 ? 0f : 45f);
            float rad = a * Mathf.Deg2Rad;
            var leg = Group("Leg", new Vector3(Mathf.Cos(rad) * ring, y, Mathf.Sin(rad) * ring),
                Quaternion.Euler(0f, -a, 0f), 1, amount, i * 0.9f, parent);
            Cyl("Limb", new Vector3(0f, -y * 0.42f, ring * 0.32f), thickness, y * 0.9f, 2,
                Quaternion.Euler(28f, 0f, 0f), leg);
            Box("Foot", new Vector3(0f, -y * 0.82f, ring * 0.6f),
                new Vector3(thickness * 1.8f, thickness * 0.8f, thickness * 2.6f), 2, Quaternion.identity, leg);
        }
    }

    static void AddTentacles(int count, float ring, float y, float thickness, float length, int palette, Transform parent = null)
    {
        for (int i = 0; i < count; i++)
        {
            float a = i * 360f / count;
            float rad = a * Mathf.Deg2Rad;
            var g = Group("Tentacle", new Vector3(Mathf.Cos(rad) * ring, y, Mathf.Sin(rad) * ring),
                Quaternion.Euler(0f, -a, 0f), 1, 12f, i * 0.65f, parent);
            Cyl("Limb", new Vector3(0f, -length * 0.25f, ring * 0.45f), thickness, length, palette,
                Quaternion.Euler(52f, 0f, 0f), g);
            Orb("Tip", new Vector3(0f, -length * 0.5f, ring * 0.85f), Vector3.one * thickness * 1.8f, 1, g);
        }
    }

    static void AddWings(float y, float span, float chord, int palette, Transform parent = null)
    {
        for (int s = -1; s <= 1; s += 2)
        {
            var wing = Group("Wing", new Vector3(s * _r * 0.7f, y, 0f), Quaternion.identity, 0, s * 30f, s > 0 ? 0f : 1.1f, parent);
            Box("Membrane", new Vector3(s * span * 0.5f, 0f, 0f),
                new Vector3(span, _h * 0.05f, chord), palette,
                Quaternion.Euler(0f, s * 10f, s * 8f), wing);

            for (int i = 0; i < 3; i++)
            {
                float z = Mathf.Lerp(-chord * 0.45f, chord * 0.45f, i / 2f);
                Box("Rib", new Vector3(s * span * 0.5f, _h * 0.035f, z),
                    new Vector3(span * 0.92f, _h * 0.025f, _r * 0.08f), 4, Quaternion.identity, wing);
            }
        }
    }

    static void AddRuneRing(float y, float radius, int count, int palette, Transform parent, float spin)
    {
        if (count <= 0)
            return;

        var ring = Group("RuneRing", new Vector3(0f, y, 0f), Quaternion.identity, 2, spin, 0f, parent);
        for (int i = 0; i < count; i++)
        {
            float a = i * 360f / count * Mathf.Deg2Rad;
            Box("Rune", new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius),
                new Vector3(_r * 0.16f, _r * 0.16f, _r * 0.16f), palette,
                Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 45f), ring);
        }
    }

    static void AddTierDecor()
    {
        if (_tier == EnemyTier.Elite)
        {
            AddHorns(_h * 1.02f, _r * 0.6f, 3, 3);
        }
        else if (_tier == EnemyTier.Boss)
        {
            AddRuneRing(_h * 1.08f, _r * 1.7f, 8, 3, null, 0.35f);
            AddHorns(_h * 1.04f, _r * 0.7f, 5, 3);
        }
    }

    // ---------- Primitives ----------

    static Transform Group(string name, Vector3 position, Quaternion rotation,
        int anim = -1, float amount = 0f, float phase = 0f, Transform parent = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent != null ? parent : _root, false);
        go.transform.localPosition = position;
        go.transform.localRotation = rotation;
        if (anim >= 0)
            _rig.Add(go.transform, anim, amount, phase);
        return go.transform;
    }

    static Transform Orb(string name, Vector3 position, Vector3 size, int palette,
        Transform parent = null, int anim = -1, float amount = 0f, float phase = 0f)
    {
        return Part(PrimitiveType.Sphere, name, position, size, palette, Quaternion.identity, parent, anim, amount, phase);
    }

    static Transform Box(string name, Vector3 position, Vector3 size, int palette,
        Quaternion rotation = default, Transform parent = null, int anim = -1, float amount = 0f, float phase = 0f)
    {
        return Part(PrimitiveType.Cube, name, position, size, palette, rotation, parent, anim, amount, phase);
    }

    static Transform Cyl(string name, Vector3 position, float radius, float height, int palette,
        Quaternion rotation = default, Transform parent = null, int anim = -1, float amount = 0f, float phase = 0f)
    {
        return Part(PrimitiveType.Cylinder, name, position,
            new Vector3(radius * 2f, height * 0.5f, radius * 2f), palette, rotation, parent, anim, amount, phase);
    }

    static Transform Part(PrimitiveType type, string name, Vector3 position, Vector3 scale, int palette,
        Quaternion rotation, Transform parent, int anim, float amount, float phase)
    {
        if (rotation == default(Quaternion))
            rotation = Quaternion.identity;

        var part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent != null ? parent : _root, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.transform.localRotation = rotation;
        Object.Destroy(part.GetComponent<Collider>());

        var renderer = part.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = MaterialFor(palette);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        if (anim >= 0)
            _rig.Add(part.transform, anim, amount, phase);

        return part.transform;
    }

    static Material MaterialFor(int palette)
    {
        int key = ((int)_kind * 3 + (int)_tier) * 8 + palette;
        Material material;
        if (_materials.TryGetValue(key, out material) && material != null)
            return material;

        Texture2D colorMap = null;
        Texture2D normalMap = null;
        float smoothness = 0.18f;
        float metallic = 0f;
        Color tint = PaletteColor(palette);

        switch (palette)
        {
            case 3:
            case 7:
                break;
            case 4:
                colorMap = RuntimeTextureLibrary.LeatherColor;
                normalMap = RuntimeTextureLibrary.LeatherNormal;
                tint = Color.Lerp(tint, Color.white, 0.35f);
                break;
            case 5:
                colorMap = RuntimeTextureLibrary.MetalColor;
                normalMap = RuntimeTextureLibrary.MetalNormal;
                tint = Color.Lerp(tint, Color.white, 0.55f);
                smoothness = 0.42f;
                metallic = 0.65f;
                break;
            case 6:
                colorMap = RuntimeTextureLibrary.WoodColor;
                normalMap = RuntimeTextureLibrary.WoodNormal;
                tint = Color.Lerp(tint, Color.white, 0.45f);
                break;
            default:
                colorMap = IsArmored() ? RuntimeTextureLibrary.LeatherColor : RuntimeTextureLibrary.FabricColor;
                normalMap = IsArmored() ? RuntimeTextureLibrary.LeatherNormal : RuntimeTextureLibrary.FabricNormal;
                tint = Color.Lerp(tint, Color.white, 0.42f);
                break;
        }

        // Тіла ворогів мають читатися приблизно як ландшафт поза променем:
        // емісія лише ледь помітний слід, а не власне джерело світла.
        Color emission = PaletteEmission(palette) * 0.015f;
        material = GameMaterials.Create(tint, emission, colorMap,
            new Vector2(1.6f, 1.6f), normalMap, smoothness, metallic);
        _materials[key] = material;
        return material;
    }

    static bool IsArmored()
    {
        return _kind == EnemyKind.Lurker || _kind == EnemyKind.Cultist || _kind == EnemyKind.Elder
               || _kind == EnemyKind.Witch || _kind == EnemyKind.WingedTerror
               || _tier != EnemyTier.Small;
    }

    static Color PaletteColor(int palette)
    {
        switch (palette)
        {
            case 1: return Color.Lerp(_color, Color.white, 0.35f);
            case 2: return new Color(_color.r * 0.28f, _color.g * 0.28f, _color.b * 0.28f, 1f);
            case 3: return Color.Lerp(_emission, Color.white, 0.3f);
            case 4: return new Color(0.86f, 0.83f, 0.7f, 1f);
            case 5: return new Color(0.14f, 0.15f, 0.18f, 1f);
            case 6: return new Color(0.3f, 0.16f, 0.07f, 1f);
            case 7: return new Color(1f, 0.36f, 0.06f, 1f);
            default: return _color;
        }
    }

    static Color PaletteEmission(int palette)
    {
        switch (palette)
        {
            case 0: return _emission;
            case 1: return _emission * 0.55f;
            case 2: return _emission * 0.12f;
            case 3: return _emission * 1.4f;
            case 4: return _emission * 0.15f;
            case 5: return _emission * 0.08f;
            case 6: return new Color(0.2f, 0.08f, 0.02f, 1f);
            case 7: return new Color(1.6f, 0.4f, 0.04f, 1f);
            default: return _emission;
        }
    }
}
