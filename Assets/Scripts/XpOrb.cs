using UnityEngine;

public class XpOrb : MonoBehaviour
{
    static Material _material;

    bool _collected;

    public static void Spawn(Vector3 position)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "XpOrb";
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.28f;

        var col = go.GetComponent<SphereCollider>();
        col.isTrigger = true;
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = SharedMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        PickupGlow.Attach(go.transform, new Color(0.35f, 0.95f, 1f), 0.95f);
        go.AddComponent<XpOrb>();
    }

    static Material SharedMaterial()
    {
        if (_material == null)
            _material = GameMaterials.Create(new Color(0.45f, 0.95f, 1f), new Color(0.2f, 1.4f, 1.8f));
        return _material;
    }

    void Update()
    {
        if (GameSession.Instance == null || GameSession.Instance.Player == null)
            return;
        if (!GameSession.Instance.AllowsGameplay)
            return;

        var stats = GameSession.Instance.Player;
        Vector3 target = stats.transform.position + Vector3.up * 0.9f;
        float dist = Vector3.Distance(transform.position, target);
        if (dist <= stats.PickupRadius)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, 14f * Time.deltaTime);
            if (dist < 0.65f)
                Collect(stats);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        var stats = other.GetComponentInParent<PlayerStats>();
        if (stats != null)
            Collect(stats);
    }

    void Collect(PlayerStats stats)
    {
        if (_collected)
            return;
        _collected = true;
        AudioManager.Instance?.Play("xp", 0.42f, Random.Range(0.95f, 1.18f), 0.045f);
        stats.AddXp(1);
        Destroy(gameObject);
    }
}
