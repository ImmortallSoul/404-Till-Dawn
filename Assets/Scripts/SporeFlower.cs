using UnityEngine;

public class SporeFlower : MonoBehaviour
{
    static Material _material;

    float _life = 12f;
    float _timer = 0.6f;

    public static void Spawn(Vector3 position)
    {
        position.y = 0.35f;
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "SporeFlower";
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.9f, 0.45f, 0.9f);
        Object.Destroy(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = SharedMaterial();
        go.AddComponent<SporeFlower>();
    }

    static Material SharedMaterial()
    {
        if (_material == null)
            _material = GameMaterials.Create(new Color(0.35f, 0.85f, 0.35f), new Color(0.2f, 1.2f, 0.3f));
        return _material;
    }

    void Update()
    {
        if (GameSession.Instance == null || !GameSession.Instance.AllowsGameplay)
            return;

        _life -= Time.deltaTime;
        if (_life <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        _timer -= Time.deltaTime;
        if (_timer > 0f)
            return;
        _timer = 1.2f;

        var enemies = Enemy.NearestEnemies(transform.position, 7, 12f);
        if (enemies.Count == 0)
            return;

        var stats = GameSession.Instance.Player;
        float damage = 12f * (stats != null ? stats.DamageMultiplier : 1f);
        for (int i = 0; i < enemies.Count && i < 7; i++)
        {
            Vector3 dir = (enemies[i].transform.position - transform.position).normalized;
            dir.y = 0f;
            Projectile.Spawn(transform.position + Vector3.up * 0.3f,
                Quaternion.LookRotation(dir.normalized, Vector3.up), damage, 20f, 0, 1f,
                ProjectileStyle.Spore, ProjectileBehavior.Burning, true, 0);
        }
    }
}
