using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Візуальний відгук здатності на ПКМ: розширюване кільце з кольором героя
/// та, для Спарка, розряди до кожної ураженої цілі. Короткоживущий об'єкт,
/// не впливає на геймплей і працює навіть при Time.timeScale = 0.
/// </summary>
public sealed class SkillFx : MonoBehaviour
{
    const int RingSegments = 48;

    Material _material;
    LineRenderer _ring;
    readonly List<LineRenderer> _arcs = new List<LineRenderer>();
    readonly List<Vector3[]> _arcPoints = new List<Vector3[]>();
    Color _color;
    float _radius;
    float _elapsed;

    public static void Spawn(Vector3 center, Color color, float radius, IList<Vector3> targets)
    {
        var go = new GameObject("SkillFx");
        go.transform.position = center + Vector3.up * 0.18f;
        var fx = go.AddComponent<SkillFx>();
        fx._color = color;
        fx._radius = Mathf.Max(2f, radius);
        fx._material = GameMaterials.CreateGlow(color, null, true);

        fx._ring = go.AddComponent<LineRenderer>();
        ConfigureLine(fx._ring, fx._material);
        fx._ring.positionCount = RingSegments + 1;

        if (targets == null)
            return;

        for (int i = 0; i < targets.Count; i++)
        {
            Vector3 to = targets[i] - go.transform.position;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist < 0.5f || dist > radius * 1.4f)
                continue;

            var arcObject = new GameObject("SkillArc");
            arcObject.transform.SetParent(go.transform, false);
            var line = arcObject.AddComponent<LineRenderer>();
            ConfigureLine(line, fx._material);
            line.positionCount = 6;

            Vector3 dir = to / dist;
            Vector3 side = new Vector3(-dir.z, 0f, dir.x);
            var points = new Vector3[6];
            for (int p = 0; p < points.Length; p++)
            {
                float t = p / (float)(points.Length - 1);
                float jitter = p == 0 || p == points.Length - 1
                    ? 0f
                    : Random.Range(-0.9f, 0.9f);
                points[p] = dir * (dist * t) + side * jitter + Vector3.up * (0.25f + jitter * 0.3f);
            }
            fx._arcs.Add(line);
            fx._arcPoints.Add(points);
        }
    }

    static void ConfigureLine(LineRenderer line, Material material)
    {
        line.sharedMaterial = material;
        line.useWorldSpace = false;
        line.widthMultiplier = 0.14f;
        line.numCapVertices = 2;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
    }

    void Update()
    {
        _elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(_elapsed / 0.5f);
        float radius = Mathf.Lerp(0.7f, _radius, Mathf.SmoothStep(0f, 1f, t));
        float alpha = (1f - t) * 0.9f;
        var color = new Color(_color.r, _color.g, _color.b, alpha);

        _ring.startColor = color;
        _ring.endColor = color;
        for (int i = 0; i <= RingSegments; i++)
        {
            float a = i / (float)RingSegments * Mathf.PI * 2f;
            _ring.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
        }

        for (int i = 0; i < _arcs.Count; i++)
        {
            _arcs[i].startColor = color;
            _arcs[i].endColor = color;
            Vector3[] points = _arcPoints[i];
            for (int p = 0; p < points.Length; p++)
                _arcs[i].SetPosition(p, points[p] * (0.35f + 0.65f * t));
        }

        if (t >= 1f)
            Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (_material != null)
            Destroy(_material);
    }
}
