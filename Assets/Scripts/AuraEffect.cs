using UnityEngine;

/// <summary>
/// М'який additive-ореол для нічних вогнів і магічних об'єктів.
/// Використовує процедурну текстуру з RuntimeTextureLibrary — без зовнішніх асетів.
/// </summary>
public sealed class AuraEffect : MonoBehaviour
{
    public static AuraEffect Attach(Transform parent, Color color, float size, float pulse = 0.12f)
    {
        var go = new GameObject("Aura");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one * size;

        var aura = go.AddComponent<AuraEffect>();
        aura._color = color;
        aura._pulse = pulse;

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "AuraQuad";
        quad.transform.SetParent(go.transform, false);
        quad.transform.localRotation = Quaternion.identity;
        var collider = quad.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);
        var renderer = quad.GetComponent<MeshRenderer>();
        var material = new Material(Shader.Find("Sprites/Default"));
        material.shader = Shader.Find("Particles/Additive") ?? material.shader;
        material.mainTexture = RuntimeTextureLibrary.Glow;
        material.color = color;
        material.renderQueue = 3000;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        aura._renderer = renderer;
        return aura;
    }

    Color _color;
    float _pulse;
    MeshRenderer _renderer;

    float _initialSize = -1f;

    void Start()
    {
        _initialSize = transform.localScale.x;
    }

    void Update()
    {
        if (_renderer == null)
            return;
        if (_initialSize <= 0f)
            _initialSize = transform.localScale.x;
        float k = 1f + Mathf.Sin(Time.time * 2.4f + transform.position.x * 0.13f) * _pulse;
        transform.localScale = Vector3.one * _initialSize * k;
        _renderer.sharedMaterial.color = _color * (0.85f + (k - 1f) * 1.5f);
    }
}
