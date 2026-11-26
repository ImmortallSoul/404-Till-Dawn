using UnityEngine;

/// <summary>
/// Повнотілий аватар гравця від третьої особи. Основна камера його не бачить,
/// але дзеркало Q, тіні та інші камери — бачать. Спершу пробує готові
/// скелетні моделі Fab, потім CC0-скелет KayKit як запасний варіант.
/// </summary>
public class PlayerAvatarController : MonoBehaviour
{
    // Окремий шар: basic-камера виключає його з cullingMask, дзеркало — включає.
    public const int AvatarLayer = 31;

    static readonly string[] ModelCandidates =
    {
        "Characters/FabSkeleton/SKEL",
        "Characters/FabSkeleton/Skeleton",
        "Characters/FabSkeleton/Skeleton_Model",
        "Characters/Skeleton_Warrior",
        "Characters/Skeleton_Rogue",
        "Characters/Skeleton_Minion"
    };

    CharacterAnimationController _animations;
    PlayerStats _stats;
    PlayerCombat _combat;
    CharacterController _controller;
    float _hitUntil;
    bool _dead;

    public GameObject Model { get; private set; }

    public static PlayerAvatarController Attach(Transform player, PlayerStats stats,
        PlayerCombat combat, Camera mainCamera, Color tint)
    {
        if (player == null)
            return null;

        GameObject root = new GameObject("PlayerAvatar");
        root.transform.SetParent(player, false);
        root.transform.localPosition = new Vector3(0f, 0f, -0.35f);
        root.transform.localRotation = Quaternion.identity;

        var avatar = root.AddComponent<PlayerAvatarController>();
        avatar._stats = stats;
        avatar._combat = combat;
        avatar._controller = player.GetComponent<CharacterController>();

        GameObject prefab = null;
        for (int i = 0; i < ModelCandidates.Length && prefab == null; i++)
            prefab = Resources.Load<GameObject>(ModelCandidates[i]);

        if (prefab != null)
        {
            var model = Object.Instantiate(prefab, root.transform);
            model.name = "AvatarModel";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            var animator = model.GetComponent<Animator>();
            if (animator != null)
                Object.Destroy(animator);
            var colliders = model.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                Object.Destroy(colliders[i]);

            Texture2D texture = Resources.Load<Texture2D>("Characters/skeleton_texture");
            Material material = GameMaterials.Create(Color.Lerp(new Color(0.78f, 0.80f, 0.86f), tint, 0.42f),
                tint * 0.16f, texture, Vector2.one, null, 0.28f, 0.06f);
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = material;
                renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderers[i].receiveShadows = true;
            }

            Bounds bounds = BoundsOf(model.transform);
            if (bounds.size.sqrMagnitude > 0.000001f)
            {
                float scale = 1.78f / Mathf.Max(0.05f, bounds.size.y);
                model.transform.localScale = Vector3.one * scale;
                model.transform.localPosition = new Vector3(0f, -bounds.min.y * scale, 0f);
            }
            else
            {
                model.transform.localScale = Vector3.one;
            }

            SetLayer(root.transform, AvatarLayer);
            avatar.Model = model;
            avatar._animations = CharacterAnimationController.Attach(model, "fab");
            Debug.Log("[Avatar] full-body model attached: " + prefab.name);
        }
        else
        {
            // Якщо жодна готова модель не знайдена — лишаємо процедурне тіло.
            Debug.LogWarning("[Avatar] no ready model found; keeping procedural body.");
            Object.Destroy(root);
            return null;
        }

        if (mainCamera != null)
            mainCamera.cullingMask &= ~(1 << AvatarLayer);

        var body = player.Find("Body");
        if (body != null)
            body.gameObject.SetActive(false);

        if (stats != null)
            stats.OnDamaged += avatar.OnDamaged;
        return avatar;
    }

    void OnDestroy()
    {
        if (_stats != null)
            _stats.OnDamaged -= OnDamaged;
    }

    void OnDamaged(PlayerStats stats)
    {
        _hitUntil = Time.unscaledTime + 0.35f;
    }

    void Update()
    {
        var session = GameSession.Instance;
        if (session == null || session.State == RunState.Dead || session.State == RunState.Won)
        {
            if (!_dead)
            {
                _dead = true;
                _animations?.SetState("Death", 0f);
            }
            return;
        }

        if (_animations == null)
            return;

        float speed = _controller != null
            ? new Vector2(_controller.velocity.x, _controller.velocity.z).magnitude
            : 0f;

        if (Time.unscaledTime < _hitUntil)
            _animations.SetState("Hit", 0.08f);
        else if (_combat != null && _combat.IsFiring)
            _animations.SetState("Attack", 0.1f);
        else if (speed > 4.2f)
            _animations.SetState("Run");
        else if (speed > 0.6f)
            _animations.SetState("Walk");
        else
            _animations.SetState("Idle");
    }

    static void SetLayer(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
            SetLayer(root.GetChild(i), layer);
    }

    static Bounds BoundsOf(Transform root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        bool first = true;
        Bounds result = new Bounds(root.position, Vector3.zero);
        for (int i = 0; i < renderers.Length; i++)
        {
            Bounds b = renderers[i].bounds;
            if (b.size.sqrMagnitude < 0.000001f)
                continue;
            if (first)
            {
                result = b;
                first = false;
            }
            else
            {
                result.Encapsulate(b);
            }
        }
        return result;
    }
}
