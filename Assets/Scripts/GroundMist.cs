using UnityEngine;

/// <summary>
/// Наземна імла: тонкі аддитивні шари, що повільно дрейфують над ґрунтом.
/// Створюється програмно, без зовнішніх текстур чи шейдерів.
/// </summary>
public sealed class GroundMist : MonoBehaviour
{
    const int Layers = 14;
    readonly Transform[] _layers = new Transform[Layers];

    public static void Attach(Transform parent, Color color, float arenaRadius)
    {
        var go = new GameObject("GroundMist");
        go.transform.SetParent(parent, false);
        var mist = go.AddComponent<GroundMist>();
        mist.Build(color, arenaRadius);
    }

    void Build(Color color, float arenaRadius)
    {
        var rng = new System.Random(907);
        for (int i = 0; i < Layers; i++)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "MistLayer_" + i;
            quad.transform.SetParent(transform, false);
            quad.transform.rotation = Quaternion.Euler(90f, 0f, (float)rng.NextDouble() * 360f);
            float radius = 18f + (float)rng.NextDouble() * Mathf.Min(arenaRadius, 90f);
            float a = (float)(rng.NextDouble() * System.Math.PI * 2f);
            quad.transform.position = new Vector3(Mathf.Cos(a) * radius, 0.35f + (float)rng.NextDouble() * 0.9f,
                Mathf.Sin(a) * radius);
            quad.transform.localScale = new Vector3(16f + (float)rng.NextDouble() * 22f,
                16f + (float)rng.NextDouble() * 22f, 1f);

            var collider = quad.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);
            var renderer = quad.GetComponent<MeshRenderer>();
            var material = new Material(Shader.Find("Sprites/Default"));
            var additive = Shader.Find("Particles/Additive");
            if (additive != null)
                material.shader = additive;
            material.mainTexture = RuntimeTextureLibrary.Glow;
            material.color = new Color(color.r, color.g, color.b, 0.055f + (float)rng.NextDouble() * 0.045f);
            material.renderQueue = 2500;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            _layers[i] = quad.transform;
        }
    }

    void Update()
    {
        float t = Time.time;
        for (int i = 0; i < Layers; i++)
        {
            if (_layers[i] == null)
                continue;
            Vector3 p = _layers[i].position;
            p.x += Mathf.Sin(t * 0.09f + i) * Time.deltaTime * 0.25f;
            p.z += Mathf.Cos(t * 0.07f + i * 1.7f) * Time.deltaTime * 0.22f;
            _layers[i].position = p;
        }
    }
}
