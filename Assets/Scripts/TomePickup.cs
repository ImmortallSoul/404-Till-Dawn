using UnityEngine;

public class TomePickup : MonoBehaviour
{
    static Material _material;
    bool _collected;

    public static void Spawn(Vector3 position)
    {
        position.y = 0.45f;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Tome";
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.42f, 0.14f, 0.55f);
        go.transform.rotation = Quaternion.Euler(0f, 25f, 12f);

        var col = go.GetComponent<BoxCollider>();
        col.isTrigger = true;
        go.GetComponent<MeshRenderer>().sharedMaterial = SharedMaterial();
        PickupGlow.Attach(go.transform, new Color(0.7f, 0.35f, 1f), 1.55f);
        go.AddComponent<TomePickup>();
    }

    static Material SharedMaterial()
    {
        if (_material == null)
            _material = GameMaterials.Create(new Color(0.45f, 0.3f, 0.75f), new Color(1.2f, 0.5f, 2f),
                RuntimeTextureLibrary.LeatherColor, new Vector2(1f, 1f), RuntimeTextureLibrary.LeatherNormal, 0.24f);
        return _material;
    }

    void Update()
    {
        if (GameSession.Instance == null || GameSession.Instance.Player == null || !GameSession.Instance.AllowsGameplay)
            return;
        transform.Rotate(0f, 90f * Time.deltaTime, 0f);
        transform.position += Vector3.up * Mathf.Sin(Time.time * 3f) * 0.002f;
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

        int effect = Random.Range(0, 4);
        string text;
        switch (effect)
        {
            case 0:
                stats.DamageMultiplier *= 1.2f;
                text = "ТОМ: +20% урону";
                break;
            case 1:
                stats.FireRateMultiplier *= 1.18f;
                text = "ТОМ: +18% темпу";
                break;
            case 2:
                stats.AddMaxHp(1f, true);
                text = "ТОМ: +1 HP";
                break;
            default:
                stats.XpGainMultiplier *= 1.2f;
                text = "ТОМ: +20% досвіду";
                break;
        }

        Projectile.SpawnBurst(transform.position, new Color(0.7f, 0.4f, 1f), 12);
        GameSession.Instance?.ShowNotice(text, 2.5f);
        Destroy(gameObject);
    }
}
