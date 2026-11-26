using UnityEngine;

/// <summary>
/// Простий аддитивний ореол для дропів. Працює без зовнішніх спрайтів,
/// але використовує згенеровану glow-текстуру з RuntimeTextureLibrary.
/// </summary>
public class PickupGlow : MonoBehaviour
{
    Transform _camera;
    float _baseSize;
    float _phase;

    public static void Attach(Transform parent, Color color, float size)
    {
        if (parent == null)
            return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "Glow";
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one * size;
        Object.Destroy(go.GetComponent<Collider>());

        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = GameMaterials.CreateGlow(color, RuntimeTextureLibrary.TintedGlow(color), true);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        var glow = go.AddComponent<PickupGlow>();
        glow._baseSize = size;
        glow._phase = Random.Range(0f, 6f);
    }

    void Awake()
    {
        if (Camera.main != null)
            _camera = Camera.main.transform;
    }

    void Update()
    {
        if (_camera == null && Camera.main != null)
            _camera = Camera.main.transform;
        if (_camera != null)
            transform.LookAt(_camera.position, _camera.up);

        float pulse = 1f + Mathf.Sin(Time.time * 3.5f + _phase) * 0.12f;
        transform.localScale = Vector3.one * (_baseSize * pulse);
    }
}
