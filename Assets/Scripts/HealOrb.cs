using UnityEngine;

public class HealOrb : MonoBehaviour
{
    static Material _material;

    bool _collected;

    public static void Spawn(Vector3 position)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "HealOrb";
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.38f;

        var col = go.GetComponent<SphereCollider>();
        col.isTrigger = true;
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = SharedMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        PickupGlow.Attach(go.transform, new Color(0.35f, 1f, 0.5f), 1.25f);
        go.AddComponent<HealOrb>();
    }

    static Material SharedMaterial()
    {
        if (_material == null)
            _material = GameMaterials.Create(new Color(0.35f, 1f, 0.5f), new Color(0.2f, 1.6f, 0.4f));
        return _material;
    }

    void Update()
    {
        var session = GameSession.Instance;
        if (session == null || session.Player == null || !session.AllowsGameplay)
            return;

        var stats = session.Player;
        Vector3 target = stats.transform.position + Vector3.up * 0.9f;
        float dist = Vector3.Distance(transform.position, target);
        if (dist <= stats.PickupRadius + 0.8f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, 10f * Time.deltaTime);
            if (dist < 0.8f)
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
        AudioManager.Instance?.Play("heal", 0.55f, 1.1f, 0.1f);
        stats.Heal(1f);
        Destroy(gameObject);
    }
}
