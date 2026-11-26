using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class TopDownController : MonoBehaviour
{
    const float WalkSpeed = 6.8f;

    CharacterController _controller;
    PlayerStats _stats;

    public static TopDownController Spawn(Vector3 position)
    {
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = position;

        var controller = player.AddComponent<CharacterController>();
        controller.height = 1.7f;
        controller.radius = 0.42f;
        controller.center = new Vector3(0f, 0.85f, 0f);
        controller.skinWidth = 0.06f;
        controller.minMoveDistance = 0f;

        var character = CharacterCatalog.Get(GameSession.SelectedCharacter);
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(player.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        body.transform.localScale = new Vector3(0.62f, 0.72f, 0.62f);
        Object.Destroy(body.GetComponent<CapsuleCollider>());
        var renderer = body.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = GameMaterials.Create(character.Color * 0.55f, character.Color * 0.45f);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var hood = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hood.name = "Hood";
        hood.transform.SetParent(player.transform, false);
        hood.transform.localPosition = new Vector3(0f, 1.42f, 0f);
        hood.transform.localScale = new Vector3(0.68f, 0.42f, 0.68f);
        Object.Destroy(hood.GetComponent<SphereCollider>());
        hood.GetComponent<MeshRenderer>().sharedMaterial = renderer.sharedMaterial;

        var gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gun.name = "WeaponModel";
        gun.transform.SetParent(player.transform, false);
        gun.transform.localPosition = new Vector3(0.16f, 0.95f, 0.55f);
        gun.transform.localScale = new Vector3(0.14f, 0.14f, 0.72f);
        Object.Destroy(gun.GetComponent<BoxCollider>());
        gun.GetComponent<MeshRenderer>().sharedMaterial = GameMaterials.Create(new Color(0.14f, 0.15f, 0.19f));

        var stats = player.AddComponent<PlayerStats>();
        player.AddComponent<PlayerCombat>();
        player.AddComponent<SummonManager>();
        var topDown = player.AddComponent<TopDownController>();

        stats.ApplyCharacter(character);
        PlayerLamp.Attach(player.transform);
        GameCamera.Create(player.transform);
        GameSession.SetCursor(false);

        return topDown;
    }

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (GameSession.Instance == null || !GameSession.Instance.AllowsGameplay)
            return;

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f)
            input.Normalize();

        Vector3 motion = input * (WalkSpeed * _stats.MoveSpeedMultiplier);
        motion.y = -2f;
        _controller.Move(motion * Time.deltaTime);

        float limit = ArenaBuilder.WalkLimit;
        var p = transform.position;
        p.x = Mathf.Clamp(p.x, -limit, limit);
        p.z = Mathf.Clamp(p.z, -limit, limit);
        transform.position = p;
    }
}
