using UnityEngine;

public class ChestPickup : MonoBehaviour
{
    static Material _material;
    bool _collected;

    public static void Spawn(Vector3 position)
    {
        position.y = 0.35f;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Chest";
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.7f, 0.45f, 0.55f);

        var col = go.GetComponent<BoxCollider>();
        col.isTrigger = true;
        go.GetComponent<MeshRenderer>().sharedMaterial = SharedMaterial();
        PickupGlow.Attach(go.transform, new Color(1f, 0.75f, 0.2f), 1.6f);
        go.AddComponent<ChestPickup>();
    }

    static Material SharedMaterial()
    {
        if (_material == null)
            _material = GameMaterials.Create(new Color(0.72f, 0.5f, 0.2f), new Color(1.4f, 0.9f, 0.2f),
                RuntimeTextureLibrary.WoodColor, new Vector2(1f, 1f), RuntimeTextureLibrary.WoodNormal, 0.2f);
        return _material;
    }

    void Update()
    {
        if (GameSession.Instance == null || GameSession.Instance.Player == null || !GameSession.Instance.AllowsGameplay)
            return;
        transform.Rotate(0f, 45f * Time.deltaTime, 0f);
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

        int gems = 3 + GameSession.Darkness * 2;
        stats.Gems += gems;
        stats.AddXp(2);
        Projectile.SpawnBurst(transform.position, new Color(1f, 0.8f, 0.25f), 12);
        GameSession.Instance?.ShowNotice("СКРИНЯ: +" + gems + " кристалів", 2f);
        Destroy(gameObject);
    }
}
