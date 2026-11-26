using System.Collections.Generic;
using UnityEngine;

public static class ArenaBuilder
{
    public static float Size { get; private set; } = 56f;
    public static MapType CurrentMap { get; private set; } = MapType.Forest;
    public static Transform ArenaRoot { get; private set; }
    public static float WalkLimit { get { return Size * 0.5f - 2f; } }
    public static float PropScale { get { return Mathf.Max(1f, WalkLimit / 126f); } }

    static readonly Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();

    // Готові моделі Fab: Simple Lowpoly Nature Pack, Realistic Rocks,
    // Halloween Scary Pumpkins Kit. Старі шляхи лишаємо як запасні.
    static readonly string[] TreeRoundResources =
    {
        "Nature/SM_Oak_Tree_01", "Nature/SM_Oak_Tree_02",
        "Props/Nature/TreeRound", "Props/Nature/SM_TreeRound_01"
    };
    static readonly string[] TreePineResources =
    {
        "Nature/SM_Pine_Tree_01", "Nature/SM_Pine_Tree_02",
        "Props/Nature/TreePine", "Props/Nature/SM_TreePine_01"
    };
    static readonly string[] BushResources =
    {
        "Nature/SM_Plant_01", "Nature/SM_Plant_02", "Nature/SM_Plant_03",
        "Props/Nature/Bush_01", "Props/Nature/SM_Bush_01"
    };
    static readonly string[] StumpResources =
    {
        "Nature/SM_Tree_Stump_01", "Nature/SM_Tree_Stump_02",
        "Props/Nature/Stump_01", "Props/Nature/SM_Stump_01"
    };
    static readonly string[] RockResources =
    {
        "Rocks/SM_LittleRock_01", "Rocks/SM_LittleRock_02",
        "Rocks/SM_LittleRock_03", "Rocks/SM_LittleRock_04",
        "Rocks/SM_LittleRock_05", "Rocks/SM_LittleRock_06",
        "Props/Rocks/SM_LittleRock_01", "Props/Rocks/SM_LittleRock_02"
    };
    static readonly string[] PumpkinResources =
    {
        "Pumpkins/SM_Pumpkin_01", "Pumpkins/SM_Pumpkin_02",
        "Pumpkins/SM_Pumpkin_03", "Pumpkins/SM_Pumpkin_04",
        "Props/Pumpkins/Pumpkin_01", "Props/Pumpkins/Pumpkin_02"
    };

    static int Scaled(int count)
    {
        return Mathf.Max(1, Mathf.RoundToInt(count * PropScale));
    }

    public static void Build(float size)
    {
        Build(size, GameSession.SelectedMap);
    }

    public static void Build(float size, MapType map)
    {
        Size = size;
        CurrentMap = map;

        var old = GameObject.Find("Arena");
        if (old != null)
            Object.Destroy(old);

        var root = new GameObject("Arena");
        ArenaRoot = root.transform;
        float half = size * 0.5f;
        const float wallHeight = 9f;
        const float wallThickness = 1.6f;

        Color floor;
        Color wall;
        Color prop;
        Color accent;

        switch (map)
        {
            case MapType.Temple:
                floor = new Color(0.28f, 0.25f, 0.22f);
                wall = new Color(0.40f, 0.37f, 0.32f);
                prop = new Color(0.52f, 0.47f, 0.37f);
                accent = new Color(0.95f, 0.68f, 0.22f);
                break;
            case MapType.PumpkinPatch:
                floor = new Color(0.13f, 0.08f, 0.10f);
                wall = new Color(0.18f, 0.12f, 0.09f);
                prop = new Color(0.95f, 0.42f, 0.08f);
                accent = new Color(1f, 0.35f, 0.05f);
                break;
            default:
                floor = new Color(0.16f, 0.23f, 0.15f);
                wall = new Color(0.20f, 0.26f, 0.19f);
                prop = new Color(0.28f, 0.22f, 0.16f);
                accent = new Color(0.45f, 0.72f, 0.36f);
                break;
        }

        Vector2 floorTiling = new Vector2(size / 6f, size / 6f);
        Vector2 wallTiling = new Vector2(size / 8f, wallHeight / 4f);

        bool temple = map == MapType.Temple;
        var floorMat = Textured(temple ? RuntimeTextureLibrary.CeramicColor : RuntimeTextureLibrary.GroundColor,
            temple ? RuntimeTextureLibrary.CeramicNormal : RuntimeTextureLibrary.GroundNormal,
            Color.Lerp(floor, Color.white, 0.46f), floorTiling);
        var wallMat = Textured(RuntimeTextureLibrary.StoneColor, RuntimeTextureLibrary.StoneNormal,
            Color.Lerp(wall, Color.white, 0.5f), wallTiling);
        var propMat = Textured(RuntimeTextureLibrary.WoodColor, RuntimeTextureLibrary.WoodNormal,
            Color.Lerp(prop, Color.white, 0.55f), new Vector2(2f, 2f), prop * 0.22f);
        var accentMat = Textured(RuntimeTextureLibrary.MetalColor, RuntimeTextureLibrary.MetalNormal,
            Color.Lerp(accent, Color.white, 0.42f), new Vector2(1.6f, 1.6f), accent * 0.35f, 0.55f, 0.7f);
        var grassMat = Textured(RuntimeTextureLibrary.GrassColor, RuntimeTextureLibrary.GrassNormal,
            Color.Lerp(prop, Color.white, 0.52f), new Vector2(1.8f, 1.8f), prop * 0.08f);
        // Гарбузи мають власну смугасту текстуру, а не землю з підлоги.
        var pumpkinMat = Textured(RuntimeTextureLibrary.PumpkinColor, null,
            Color.Lerp(prop, Color.white, 0.58f), new Vector2(1.4f, 1.4f),
            new Color(1f, 0.32f, 0.04f), 0.42f, 0f);
        var rockMat = Textured(RuntimeTextureLibrary.StoneColor, RuntimeTextureLibrary.StoneNormal,
            Color.Lerp(wall, Color.white, 0.62f), new Vector2(1.5f, 1.5f), wall * 0.08f);

        CreateBox(root.transform, "Floor", new Vector3(0f, -0.5f, 0f), new Vector3(size + 2f, 1f, size + 2f), floorMat);
        CreateBox(root.transform, "WallNorth", new Vector3(0f, wallHeight * 0.5f, half), new Vector3(size, wallHeight, wallThickness), wallMat);
        CreateBox(root.transform, "WallSouth", new Vector3(0f, wallHeight * 0.5f, -half), new Vector3(size, wallHeight, wallThickness), wallMat);
        CreateBox(root.transform, "WallEast", new Vector3(half, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, size), wallMat);
        CreateBox(root.transform, "WallWest", new Vector3(-half, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, size), wallMat);

        switch (map)
        {
            case MapType.Temple:
                BuildTemple(root.transform, wallMat, wallMat, accentMat, rockMat, grassMat);
                break;
            case MapType.PumpkinPatch:
                BuildPumpkinPatch(root.transform, pumpkinMat, wallMat, propMat, rockMat, grassMat);
                break;
            default:
                BuildForest(root.transform, propMat, grassMat, rockMat);
                break;
        }

        // Легка імла біля землі підсилює туман і нічну глибину кадра.
        GroundMist.Attach(root.transform,
            map == MapType.PumpkinPatch
                ? new Color(0.42f, 0.22f, 0.48f)
                : new Color(0.28f, 0.38f, 0.36f), WalkLimit);

        // Локальна навігація мобів завжди працює з актуальною картою.
        ObstacleRegistry.Rebuild();
    }

    static Material Textured(Texture2D color, Texture2D normal, Color tint, Vector2 tiling,
        Color? emission = null, float smoothness = 0.22f, float metallic = 0f)
    {
        return GameMaterials.Create(tint, emission, color, tiling, normal, smoothness, metallic);
    }

    static void BuildForest(Transform root, Material trunkMat, Material crownMat, Material rockMat)
    {
        var rng = new System.Random(1337);

        int treeCount = Scaled(150);
        for (int i = 0; i < treeCount; i++)
        {
            float a = (float)(i / (float)treeCount * System.Math.PI * 2f + rng.NextDouble() * 0.35f);
            // Площинно-рівномірний розподіл: дерева не злипаються в центрі,
            // а рівномірно заповнюють ліс у межах видимості до туману.
            float r = 9f + Mathf.Sqrt((float)rng.NextDouble()) * (WalkLimit - 12f);
            Vector3 pos = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            if (Mathf.Abs(pos.x) < 5f && Mathf.Abs(pos.z) < 5f)
                continue;

            float h = 3.2f + (float)rng.NextDouble() * 3.4f;
            float yaw = (float)rng.NextDouble() * 360f;
            bool pine = rng.NextDouble() > 0.46;
            string[] trees = pine ? TreePineResources : TreeRoundResources;
            if (TrySpawnFitted(root, trees, pine ? "Pine" : "Tree", pos, h + 1.1f, yaw,
                    true, new Vector3(0.32f, 0.9f, 0.32f), rockMat))
                continue;

            CreateCylinder(root, "TreeTrunk", pos + Vector3.up * h * 0.5f,
                new Vector3(0.32f, h * 0.5f, 0.32f), trunkMat);
            CreateSphere(root, "TreeCrown", pos + Vector3.up * (h + 0.6f),
                Vector3.one * (1.2f + (float)rng.NextDouble()), crownMat);
        }

        // Густі гаї: кілька дерев у тісному радіусі дають відчуття справжнього
        // лісу саме в кадрі, а не лише у загальній статистиці.
        int groveCount = 25;
        for (int g = 0; g < groveCount; g++)
        {
            float ga = (float)(rng.NextDouble() * System.Math.PI * 2f);
            float gr = 14f + Mathf.Sqrt((float)rng.NextDouble()) * (WalkLimit - 18f);
            Vector3 center = new Vector3(Mathf.Cos(ga) * gr, 0f, Mathf.Sin(ga) * gr);
            int inGrove = 6 + rng.Next(5);
            for (int j = 0; j < inGrove; j++)
            {
                float a = (float)(rng.NextDouble() * System.Math.PI * 2f);
                float r = (float)rng.NextDouble() * 9f;
                Vector3 pos = center + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                if (Mathf.Abs(pos.x) < 6f && Mathf.Abs(pos.z) < 6f)
                    continue;

                float h = 3.4f + (float)rng.NextDouble() * 3.2f;
                bool pine = rng.NextDouble() > 0.4;
                string[] trees = pine ? TreePineResources : TreeRoundResources;
                if (TrySpawnFitted(root, trees, pine ? "PineGrove" : "TreeGrove", pos, h + 1.1f,
                        (float)rng.NextDouble() * 360f, true, new Vector3(0.32f, 0.9f, 0.32f), rockMat))
                    continue;

                CreateCylinder(root, "TreeTrunk", pos + Vector3.up * h * 0.5f,
                    new Vector3(0.32f, h * 0.5f, 0.32f), trunkMat);
                CreateSphere(root, "TreeCrown", pos + Vector3.up * (h + 0.6f),
                    Vector3.one * (1.2f + (float)rng.NextDouble()), crownMat);
            }
        }

        int rockCount = Scaled(46);
        Vector3 cluster = Vector3.zero;
        for (int i = 0; i < rockCount; i++)
        {
            float r = 6f + (float)rng.NextDouble() * (WalkLimit - 9f);
            Vector3 pos;
            if (i % 4 == 0)
            {
                float a = (float)(rng.NextDouble() * System.Math.PI * 2f);
                cluster = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                pos = cluster;
            }
            else
            {
                pos = cluster + new Vector3(
                    (float)(rng.NextDouble() - 0.5) * 7f, 0f,
                    (float)(rng.NextDouble() - 0.5) * 7f);
            }
            if (Mathf.Abs(pos.x) < 5f && Mathf.Abs(pos.z) < 5f)
                continue;

            float size = 0.65f + (float)rng.NextDouble() * 1.7f;
            string[] candidates = { RockResources[rng.Next(RockResources.Length)] };
            if (!TrySpawnFitted(root, candidates, "Rock", pos, size,
                    (float)rng.NextDouble() * 360f, true, new Vector3(0.7f, 0.75f, 0.7f), rockMat))
                CreateSphere(root, "Rock", pos + Vector3.up * size * 0.28f,
                    new Vector3(size * 0.65f, size * 0.38f, size * 0.55f), rockMat);
        }

        int bushCount = Scaled(80);
        for (int i = 0; i < bushCount; i++)
        {
            float a = (float)(rng.NextDouble() * System.Math.PI * 2f);
            float r = 7f + (float)rng.NextDouble() * (WalkLimit - 10f);
            Vector3 pos = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            float size = 0.5f + (float)rng.NextDouble() * 0.9f;
            if (!TrySpawnFitted(root, BushResources, "Bush", pos, size,
                    (float)rng.NextDouble() * 360f, false, Vector3.one, crownMat))
                CreateSphere(root, "Bush", pos + Vector3.up * size * 0.35f,
                    new Vector3(size * 0.8f, size * 0.55f, size * 0.8f), crownMat);
        }

        int stumpCount = Scaled(14);
        for (int i = 0; i < stumpCount; i++)
        {
            float a = (float)(rng.NextDouble() * System.Math.PI * 2f);
            float r = 8f + (float)rng.NextDouble() * (WalkLimit - 11f);
            Vector3 pos = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            float size = 0.45f + (float)rng.NextDouble() * 0.55f;
            if (!TrySpawnFitted(root, StumpResources, "Stump", pos, size,
                    (float)rng.NextDouble() * 360f, true, new Vector3(0.8f, 0.8f, 0.8f), trunkMat))
                CreateCylinder(root, "Stump", pos + Vector3.up * size * 0.5f,
                    new Vector3(0.4f, size * 0.5f, 0.4f), trunkMat);
        }
    }

    static void BuildTemple(Transform root, Material stoneMat, Material pillarMat, Material goldMat,
        Material rockMat, Material bushMat)
    {
        BuildTempleLayout(root, stoneMat, goldMat);

        var rng = new System.Random(4242);

        for (int i = 0; i < 8; i++)
        {
            float a = i / 8f * Mathf.PI * 2f;
            Vector3 pos = new Vector3(Mathf.Cos(a) * 10f, 0f, Mathf.Sin(a) * 10f);
            CreateBox(root, "TemplePillar", pos + Vector3.up * 2.2f,
                new Vector3(1.4f, 4.4f, 1.4f), pillarMat);
            CreateBox(root, "TempleCap", pos + Vector3.up * 4.6f,
                new Vector3(2f, 0.35f, 2f), goldMat);
        }

        int altarCount = Scaled(12);
        for (int i = 0; i < altarCount; i++)
        {
            float a = i / (float)altarCount * Mathf.PI * 2f;
            float r = 18f + (i % 3) * 11f;
            Vector3 pos = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            CreateBox(root, "Altar", pos + Vector3.up * 0.6f,
                new Vector3(2.2f, 1.2f, 1.8f), stoneMat);
            CreateSphere(root, "Brazier", pos + Vector3.up * 1.45f, Vector3.one * 0.42f, goldMat);
        }

        int rockCount = Scaled(24);
        for (int i = 0; i < rockCount; i++)
        {
            float a = (float)(rng.NextDouble() * System.Math.PI * 2f);
            float r = WalkLimit - 6f - (float)rng.NextDouble() * 16f;
            Vector3 pos = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            float size = 0.9f + (float)rng.NextDouble() * 1.9f;
            string[] candidates = { RockResources[rng.Next(RockResources.Length)] };
            if (!TrySpawnFitted(root, candidates, "TempleRock", pos, size,
                    (float)rng.NextDouble() * 360f, true, new Vector3(0.7f, 0.75f, 0.7f), rockMat))
                CreateSphere(root, "TempleRock", pos + Vector3.up * size * 0.28f,
                    new Vector3(size * 0.7f, size * 0.4f, size * 0.6f), rockMat);
        }

        int bushCount = Scaled(26);
        for (int i = 0; i < bushCount; i++)
        {
            float a = (float)(rng.NextDouble() * System.Math.PI * 2f);
            float r = WalkLimit - 4f - (float)rng.NextDouble() * 22f;
            Vector3 pos = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            float size = 0.5f + (float)rng.NextDouble() * 0.8f;
            if (!TrySpawnFitted(root, BushResources, "TempleBush", pos, size,
                    (float)rng.NextDouble() * 360f, false, Vector3.one, bushMat))
                CreateSphere(root, "TempleBush", pos + Vector3.up * size * 0.3f,
                    Vector3.one * size * 0.7f, bushMat);
        }
    }

    /// <summary>
    /// Храм без лабіринту: відкритий двір, 8 широких радіальних коридорів,
    /// кільцева галерея й короткі руїнні стіни між коридорами. Усі проходи
    /// ширші за моба, глухих кутів немає — вороги можуть іти прямо до гравця.
    /// </summary>
    static void BuildTempleLayout(Transform root, Material wallMat, Material goldMat)
    {
        const float wallHeight = 4.8f;
        const float corridorHalfWidth = 6f;
        const float lateral = corridorHalfWidth + 1.4f;
        int wallIndex = 0;

        // Вісім променів. Стіни лише з боків, із проміжками по 8 м уздовж шляху.
        float[] starts = { 28f, 46f, 64f };
        float[] ends = { 38f, 56f, 74f };
        for (int spoke = 0; spoke < 8; spoke++)
        {
            float a = spoke * 45f * Mathf.Deg2Rad;
            Vector3 axis = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            Vector3 side = new Vector3(-axis.z, 0f, axis.x);
            Quaternion rotation = Quaternion.LookRotation(axis, Vector3.up);

            for (int s = -1; s <= 1; s += 2)
            {
                for (int k = 0; k < starts.Length; k++)
                {
                    float middle = (starts[k] + ends[k]) * 0.5f;
                    float length = ends[k] - starts[k];
                    Vector3 center = axis * middle + side * (s * lateral);
                    CreateRotatedBox(root, "TempleWall_" + wallIndex++,
                        center + Vector3.up * (wallHeight * 0.5f),
                        new Vector3(1.6f, wallHeight, length), rotation, wallMat);

                    if ((spoke + k + (s > 0 ? 1 : 0)) % 3 == 0)
                        CreateRotatedBox(root, "TempleCap",
                            center + Vector3.up * (wallHeight + 0.16f),
                            new Vector3(1.9f, 0.3f, length * 0.88f), rotation, goldMat);
                }
            }
        }

        // Внутрішні руїни навколо двору: короткі дуги з проміжками ~10 м.
        // Вони дають храму «тінь» для близького спавну, але не перекривають
        // шлях до гравця — моб проходить між сегментами без патфайндингу.
        const float innerRadius = 15.5f;
        for (int i = 0; i < 6; i++)
        {
            float a = (i * 60f + 15f) * Mathf.Deg2Rad;
            Vector3 center = new Vector3(Mathf.Cos(a) * innerRadius, 0f, Mathf.Sin(a) * innerRadius);
            Vector3 tangent = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
            CreateRotatedBox(root, "TempleWall_" + wallIndex++,
                center + Vector3.up * (3.4f * 0.5f),
                new Vector3(1.4f, 3.4f, 6f), Quaternion.LookRotation(tangent, Vector3.up), wallMat);
        }

        // Зовнішня галерея: короткі дуги з великими проміжками, моб завжди
        // може перейти з одного коридору в інший, не потрапивши в пастку.
        const float ringRadius = 78f;
        for (int i = 0; i < 16; i++)
        {
            float a = (i * 22.5f + 11.25f) * Mathf.Deg2Rad;
            Vector3 center = new Vector3(Mathf.Cos(a) * ringRadius, 0f, Mathf.Sin(a) * ringRadius);
            Vector3 tangent = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
            CreateRotatedBox(root, "TempleWall_" + wallIndex++,
                center + Vector3.up * (wallHeight * 0.5f),
                new Vector3(1.6f, wallHeight, 9f), Quaternion.LookRotation(tangent, Vector3.up), wallMat);
        }

        // Короткі руїни в секторах між коридорами — декорація, не перешкода шляху.
        var rng = new System.Random(9137);
        for (int i = 0; i < 24; i++)
        {
            float a = (i % 8 * 45f + 22.5f + (float)rng.NextDouble() * 8f - 4f) * Mathf.Deg2Rad;
            float r = 32f + (float)rng.NextDouble() * 40f;
            Vector3 center = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            Quaternion rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 180f, 0f);
            float height = 2.8f + (float)rng.NextDouble() * 1.6f;
            CreateRotatedBox(root, "TempleWall_" + wallIndex++,
                center + Vector3.up * (height * 0.5f),
                new Vector3(1.4f, height, 8f), rotation, wallMat);
        }
    }

    static void BuildPumpkinPatch(Transform root, Material pumpkinMat, Material graveMat,
        Material deadTreeMat, Material rockMat, Material bushMat)
    {
        var rng = new System.Random(7331);
        var faceMat = GameMaterials.Create(new Color(1f, 0.48f, 0.08f),
            new Color(2.6f, 0.82f, 0.10f));
        var ghostMat = GameMaterials.Create(new Color(0.68f, 0.92f, 1f),
            new Color(0.35f, 0.95f, 1.45f));
        int lightsPlaced = 0;

        int pumpkinCount = Scaled(58);
        for (int i = 0; i < pumpkinCount; i++)
        {
            float a = (float)(rng.NextDouble() * System.Math.PI * 2f);
            float r = 8f + (float)rng.NextDouble() * (WalkLimit - 10f);
            Vector3 pos = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            if (Mathf.Abs(pos.x) < 5f && Mathf.Abs(pos.z) < 5f)
                continue;

            float s = 0.9f + (float)rng.NextDouble() * 0.95f;
            float yaw = (float)rng.NextDouble() * 360f;
            string[] candidates = { PumpkinResources[rng.Next(PumpkinResources.Length)] };
            if (!TrySpawnFitted(root, candidates, "Pumpkin", pos, s, yaw,
                    true, new Vector3(0.7f, 0.7f, 0.7f), pumpkinMat))
                CreateSphere(root, "Pumpkin", pos + Vector3.up * s * 0.4f,
                    new Vector3(s, s * 0.75f, s), pumpkinMat);

            // Світні обличчя роблять мапу впізнавано хеллоуїнською, а не просто осінньою.
            if (i % 3 != 0)
            {
                Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
                Vector3 forward = rot * Vector3.forward;
                Vector3 right = rot * Vector3.right;
                Vector3 faceBase = pos + Vector3.up * (s * 0.52f) + forward * (s * 0.57f);
                NoCollider(CreateSphere(root, "JackEyeL", faceBase - right * (s * 0.24f),
                    Vector3.one * (s * 0.16f), faceMat));
                NoCollider(CreateSphere(root, "JackEyeR", faceBase + right * (s * 0.24f),
                    Vector3.one * (s * 0.16f), faceMat));
                NoCollider(CreateRotatedBox(root, "JackMouth",
                    faceBase + Vector3.down * (s * 0.27f),
                    new Vector3(s * 0.5f, s * 0.12f, s * 0.1f), rot, faceMat));
            }

            if (i % 64 == 0 && lightsPlaced < 8)
            {
                lightsPlaced++;
                var glowObject = new GameObject("PumpkinGlow");
                glowObject.transform.SetParent(root, false);
                glowObject.transform.position = pos + Vector3.up * (s + 0.6f);
                var glow = glowObject.AddComponent<Light>();
                glow.type = LightType.Point;
                glow.color = new Color(1f, 0.42f, 0.08f);
                glow.intensity = 0.5f;
                glow.range = 8f * Mathf.Max(1f, PropScale * 0.35f);
                glow.shadows = LightShadows.None;
                AuraEffect.Attach(glowObject.transform, new Color(1f, 0.42f, 0.08f, 0.18f), 2.4f);
            }
        }

        // Кладовище з огорожею: могили, хрести, примарні вогники та ворота з проходом.
        Vector3 cemetery = new Vector3(-WalkLimit * 0.18f, 0f, WalkLimit * 0.12f);
        float cemeteryRadius = Mathf.Min(22f * PropScale, 90f);
        int graves = Scaled(22);
        for (int i = 0; i < graves; i++)
        {
            float a = (float)(rng.NextDouble() * System.Math.PI * 2f);
            float r = (float)rng.NextDouble() * cemeteryRadius * 0.85f;
            Vector3 pos = cemetery + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            float yaw = (float)rng.NextDouble() * 360f;
            NoCollider(CreateRotatedBox(root, "Grave", pos + Vector3.up * 0.75f,
                new Vector3(1f, 1.5f, 0.3f), Quaternion.Euler(0f, yaw, 0f), graveMat));
            if (i % 4 == 0)
            {
                NoCollider(CreateBox(root, "GraveCross", pos + Vector3.up * 1.55f,
                    new Vector3(0.18f, 1.3f, 0.18f), deadTreeMat));
                NoCollider(CreateBox(root, "GraveCrossArm", pos + Vector3.up * 1.85f,
                    new Vector3(0.9f, 0.18f, 0.18f), deadTreeMat));
            }
            if (i % 7 == 0)
            {
                var wisp = CreateSphere(root, "WillOWisp", pos + Vector3.up * 2.4f,
                    Vector3.one * 0.22f, ghostMat);
                AuraEffect.Attach(wisp.transform, new Color(0.45f, 0.85f, 1f, 0.42f), 1.6f, 0.22f);
            }
        }

        int posts = 32;
        for (int i = 0; i < posts; i++)
        {
            float a0 = i / (float)posts * Mathf.PI * 2f;
            float a1 = (i + 1) / (float)posts * Mathf.PI * 2f;
            bool gate = i >= 5 && i <= 7;
            Vector3 p0 = cemetery + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * cemeteryRadius;
            Vector3 p1 = cemetery + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * cemeteryRadius;
            if (!gate)
            {
                NoCollider(CreateBox(root, "FencePost", p0 + Vector3.up * 1.1f,
                    new Vector3(0.2f, 2.2f, 0.2f), deadTreeMat));
                Vector3 mid = (p0 + p1) * 0.5f;
                Vector3 delta = p1 - p0;
                float yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
                for (int rail = 0; rail < 2; rail++)
                    NoCollider(CreateRotatedBox(root, "FenceRail",
                        mid + Vector3.up * (0.85f + rail * 0.7f),
                        new Vector3(0.12f, 0.16f, delta.magnitude),
                        Quaternion.Euler(0f, yaw, 0f), deadTreeMat));
            }
        }

        int deadTreeCount = Scaled(14);
        for (int i = 0; i < deadTreeCount; i++)
        {
            float a = i / (float)deadTreeCount * Mathf.PI * 2f;
            float r = WalkLimit - 5f;
            Vector3 pos = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            float yaw = (float)rng.NextDouble() * 360f;
            NoCollider(CreateRotatedBox(root, "DeadTree", pos + Vector3.up * 2.4f,
                new Vector3(0.5f, 4.8f, 0.5f), Quaternion.Euler(0f, yaw, 0f), deadTreeMat));
            NoCollider(CreateRotatedBox(root, "DeadBranch", pos + Vector3.up * 3.4f,
                new Vector3(1.8f, 0.24f, 0.24f), Quaternion.Euler(0f, yaw, 32f), deadTreeMat));
            NoCollider(CreateRotatedBox(root, "DeadBranch", pos + Vector3.up * 2.7f,
                new Vector3(1.4f, 0.22f, 0.22f), Quaternion.Euler(0f, yaw + 115f, -28f), deadTreeMat));
        }

        int rockCount = Scaled(28);
        for (int i = 0; i < rockCount; i++)
        {
            float a = (float)(rng.NextDouble() * System.Math.PI * 2f);
            float r = 9f + (float)rng.NextDouble() * (WalkLimit - 11f);
            Vector3 pos = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            float size = 0.7f + (float)rng.NextDouble() * 1.5f;
            string[] candidates = { RockResources[rng.Next(RockResources.Length)] };
            if (!TrySpawnFitted(root, candidates, "PatchRock", pos, size,
                    (float)rng.NextDouble() * 360f, true, new Vector3(0.7f, 0.75f, 0.7f), rockMat))
                CreateSphere(root, "PatchRock", pos + Vector3.up * size * 0.3f,
                    new Vector3(size * 0.65f, size * 0.38f, size * 0.58f), rockMat);
        }

        int bushCount = Scaled(30);
        for (int i = 0; i < bushCount; i++)
        {
            float a = (float)(rng.NextDouble() * System.Math.PI * 2f);
            float r = 7f + (float)rng.NextDouble() * (WalkLimit - 9f);
            Vector3 pos = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            float size = 0.5f + (float)rng.NextDouble() * 0.8f;
            if (!TrySpawnFitted(root, BushResources, "PatchBush", pos, size,
                    (float)rng.NextDouble() * 360f, false, Vector3.one, bushMat))
                CreateSphere(root, "PatchBush", pos + Vector3.up * size * 0.3f,
                    Vector3.one * size * 0.7f, bushMat);
        }
    }

    static bool TrySpawnFitted(Transform parent, string[] resources, string name, Vector3 pos,
        float targetHeight, float yaw, bool solid, Vector3 colliderScale, Material fallbackMaterial)
    {
        if (resources == null)
            return false;

        for (int i = 0; i < resources.Length; i++)
        {
            string resource = resources[i];
            if (string.IsNullOrEmpty(resource))
                continue;

            GameObject prefab;
            if (!_prefabs.TryGetValue(resource, out prefab))
            {
                prefab = Resources.Load<GameObject>(resource);
                _prefabs[resource] = prefab;
            }
            if (prefab == null)
                continue;

            GameObject instance;
#if UNITY_EDITOR
            instance = Object.Instantiate(prefab);
#else
            instance = Object.Instantiate(prefab);
#endif
            instance.name = name;
            instance.transform.SetParent(parent, true);
            instance.transform.position = pos;
            instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            instance.transform.localScale = Vector3.one;

            Bounds bounds = WorldBounds(instance.transform);
            if (bounds.size.sqrMagnitude > 0.000001f && bounds.size.y > 0.05f)
            {
                float scale = targetHeight / bounds.size.y;
                instance.transform.localScale = Vector3.one * scale;
                bounds = WorldBounds(instance.transform);
                instance.transform.position += Vector3.up * (pos.y - bounds.min.y);
            }

            var colliders = instance.GetComponentsInChildren<Collider>(true);
            for (int c = 0; c < colliders.Length; c++)
                Object.Destroy(colliders[c]);

            // Імпортовані паки розраховані на URP; проєкт працює на Built-in,
            // тому готові моделі перефарбовуємо в матеріал поточної мапи.
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                renderers[r].sharedMaterial = fallbackMaterial;
                renderers[r].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderers[r].receiveShadows = true;
            }

            if (solid)
            {
                // Беремо локальні mesh.bounds, а не світовий AABB: світовий AABB
                // повернутого каменя дає криву колізію з боків.
                Bounds local = LocalBounds(instance.transform);
                var box = instance.AddComponent<BoxCollider>();
                box.center = local.center;
                box.size = new Vector3(
                    Mathf.Max(0.05f, local.size.x) * colliderScale.x,
                    Mathf.Max(0.05f, local.size.y) * colliderScale.y,
                    Mathf.Max(0.05f, local.size.z) * colliderScale.z);
            }

            return true;
        }
        return false;
    }

    static Bounds WorldBounds(Transform root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        bool first = true;
        Bounds result = new Bounds(root.position, Vector3.zero);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].bounds.size.sqrMagnitude < 0.000001f)
                continue;
            if (first)
            {
                result = renderers[i].bounds;
                first = false;
            }
            else
                result.Encapsulate(renderers[i].bounds);
        }
        return result;
    }

    /// <summary>
    /// Об'єднує mesh.bounds усіх мешів у локальному просторі кореня.
    /// На відміну від світового AABB, ці межі обертаються разом із об'єктом.
    /// </summary>
    static Bounds LocalBounds(Transform root)
    {
        var filters = root.GetComponentsInChildren<MeshFilter>(true);
        bool first = true;
        Bounds result = new Bounds(Vector3.zero, Vector3.zero);
        for (int i = 0; i < filters.Length; i++)
        {
            Mesh mesh = filters[i].sharedMesh;
            if (mesh == null)
                continue;

            Bounds local = mesh.bounds;
            Matrix4x4 matrix = root.worldToLocalMatrix * filters[i].transform.localToWorldMatrix;
            Vector3 center = matrix.MultiplyPoint3x4(local.center);
            Vector3 extents = local.extents;
            Vector3 transformedExtents = new Vector3(
                Mathf.Abs(matrix.m00) * extents.x + Mathf.Abs(matrix.m01) * extents.y + Mathf.Abs(matrix.m02) * extents.z,
                Mathf.Abs(matrix.m10) * extents.x + Mathf.Abs(matrix.m11) * extents.y + Mathf.Abs(matrix.m12) * extents.z,
                Mathf.Abs(matrix.m20) * extents.x + Mathf.Abs(matrix.m21) * extents.y + Mathf.Abs(matrix.m22) * extents.z);

            Bounds part = new Bounds(center, transformedExtents * 2f);
            if (first)
            {
                result = part;
                first = false;
            }
            else
                result.Encapsulate(part);
        }
        return result;
    }

    static GameObject CreateBox(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.position = position;
        cube.transform.localScale = scale;
        cube.GetComponent<MeshRenderer>().sharedMaterial = material;
        return cube;
    }

    static GameObject CreateRotatedBox(Transform parent, string name, Vector3 position, Vector3 scale,
        Quaternion rotation, Material material)
    {
        var cube = CreateBox(parent, name, position, scale, material);
        cube.transform.rotation = rotation;
        return cube;
    }

    static GameObject CreateSphere(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.SetParent(parent, false);
        sphere.transform.position = position;
        sphere.transform.localScale = scale;
        sphere.GetComponent<MeshRenderer>().sharedMaterial = material;
        return sphere;
    }

    static GameObject NoCollider(GameObject go)
    {
        if (go != null)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;
        }
        return go;
    }

    static void CreateCylinder(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.transform.SetParent(parent, false);
        cylinder.transform.position = position;
        cylinder.transform.localScale = scale;
        cylinder.GetComponent<MeshRenderer>().sharedMaterial = material;
    }
}
