using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Легкий кеш перешкод арени для локальної навігації ворогів.
/// Замість NavMesh (арена будується процедурно) зберігає орієнтовані
/// прямокутники біля землі й допомагає мобу плавно обійти колону,
/// коротку стіну або камінь, не застрягаючи в них.
/// </summary>
public static class ObstacleRegistry
{
    public struct Obstacle
    {
        public Vector2 Center;
        public Vector2 Half;
        public float Cos;
        public float Sin;
    }

    static readonly List<Obstacle> _items = new List<Obstacle>();

    public static int Count { get { return _items.Count; } }

    public static void Rebuild()
    {
        _items.Clear();

        Transform arena = ArenaBuilder.ArenaRoot;
        var all = Object.FindObjectsByType<Collider>();
        for (int i = 0; i < all.Length; i++)
        {
            Collider c = all[i];
            if (c == null || !c.enabled || c.isTrigger)
                continue;

            // Стара арена знищується з затримкою — беремо тільки поточну.
            if (arena != null && c.transform.root != arena.root)
                continue;

            string name = c.gameObject.name;
            if (name == "Floor" || name.StartsWith("Wall", System.StringComparison.Ordinal))
                continue; // межа арени, уздовж неї мобам ходити не потрібно

            Bounds b = c.bounds;
            if (b.max.y < 0.55f || b.min.y > 2.7f)
                continue; // низькі орнаменти й високі дахи не перешкоджають
            if (b.size.x < 0.7f && b.size.z < 0.7f)
                continue;

            Vector3 center;
            Vector2 half;
            float yaw = 0f;
            var box = c as BoxCollider;
            if (box != null)
            {
                Vector3 scale = c.transform.lossyScale;
                center = c.transform.TransformPoint(box.center);
                half = new Vector2(Mathf.Abs(box.size.x * scale.x) * 0.5f,
                                   Mathf.Abs(box.size.z * scale.z) * 0.5f);
                yaw = c.transform.eulerAngles.y * Mathf.Deg2Rad;
            }
            else
            {
                center = b.center;
                half = new Vector2(b.size.x * 0.5f, b.size.z * 0.5f);
            }

            if (half.x < 0.3f && half.y < 0.3f)
                continue;

            _items.Add(new Obstacle
            {
                Center = new Vector2(center.x, center.z),
                Half = half,
                Cos = Mathf.Cos(yaw),
                Sin = Mathf.Sin(yaw)
            });
        }
    }

    /// <summary>
    /// Повертає напрямок, який веде до цілі, але ковзає повз найближчу перешкоду.
    /// Бік обходу зберігається деякий час, щоб моб не смикався щокадру.
    /// </summary>
    public static Vector3 Steer(Vector3 desired, Vector3 position, float radius,
        ref float side, ref float sideTimer, float dt)
    {
        desired.y = 0f;
        if (desired.sqrMagnitude < 0.000001f)
            return Vector3.forward;
        desired.Normalize();

        sideTimer -= dt;
        const float Influence = 6.5f;
        Vector2 push = Vector2.zero;

        for (int i = 0; i < _items.Count; i++)
        {
            Obstacle o = _items[i];
            float dx = position.x - o.Center.x;
            float dz = position.z - o.Center.y;
            float lx = dx * o.Cos - dz * o.Sin;
            float lz = dx * o.Sin + dz * o.Cos;

            float cx = Mathf.Clamp(lx, -o.Half.x, o.Half.x);
            float cz = Mathf.Clamp(lz, -o.Half.y, o.Half.y);
            float ox = lx - cx;
            float oz = lz - cz;
            float d = Mathf.Sqrt(ox * ox + oz * oz);
            if (d > Influence + radius)
                continue;

            // Перешкода позаду — не впливає на рух.
            float nearestX = cx * o.Cos + cz * o.Sin;
            float nearestZ = -cx * o.Sin + cz * o.Cos;
            Vector3 to = new Vector3(nearestX, 0f, nearestZ) - position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f && Vector3.Dot(desired, to.normalized) < -0.35f)
                continue;

            if (d < 0.001f)
            {
                // Моб уже всередині колайдера — виштовхуємо найближчою гранню.
                float px = o.Half.x - Mathf.Abs(lx);
                float pz = o.Half.y - Mathf.Abs(lz);
                if (px < pz)
                    ox = (lx >= 0f ? 1f : -1f) * (px + 0.2f);
                else
                    oz = (lz >= 0f ? 1f : -1f) * (pz + 0.2f);
                d = Mathf.Sqrt(ox * ox + oz * oz);
            }
            ox /= d;
            oz /= d;

            float weight = Mathf.Clamp01(1f - d / Influence) * 2.2f;
            float worldX = ox * o.Cos + oz * o.Sin;
            float worldZ = -ox * o.Sin + oz * o.Cos;
            push += new Vector2(worldX, worldZ) * weight;
        }

        if (push.sqrMagnitude < 0.0004f)
            return desired;

        if (sideTimer <= 0f || side == 0f)
        {
            float cross = desired.x * push.y - desired.z * push.x;
            side = Mathf.Abs(cross) < 0.12f
                ? (position.x + position.z >= 0f ? 1f : -1f)
                : (cross >= 0f ? 1f : -1f);
            sideTimer = 0.8f;
        }

        Vector2 normal = push.normalized;
        Vector2 tangent = new Vector2(-normal.y, normal.x) * side;
        if (tangent.x * desired.x + tangent.y * desired.z < 0f)
            tangent = -tangent;

        var steered = new Vector3(
            desired.x + normal.x * 0.55f + tangent.x * 0.85f, 0f,
            desired.z + normal.y * 0.55f + tangent.y * 0.85f);

        return steered.sqrMagnitude < 0.000001f
            ? desired
            : Vector3.Slerp(desired, steered.normalized, 0.85f).normalized;
    }
}
