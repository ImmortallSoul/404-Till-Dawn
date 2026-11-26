using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [SerializeField] float walkSpeed = 7.2f;
    [SerializeField] float jumpSpeed = 7.5f;
    [SerializeField] float gravity = 20f;
    [SerializeField] float mouseSensitivity = 2.2f;
    [SerializeField] float pitchMin = -85f;
    [SerializeField] float pitchMax = 85f;

    CharacterController _controller;
    Transform _cameraPivot;
    PlayerStats _stats;
    Vector3 _velocity;
    float _pitch;
    float _stepDistance;
    bool _cursorLocked = true;

    // Спринт і підкат (кувирок). Підкат дає короткий імунітет до урону.
    float _rollTimer;
    float _rollCooldown;
    Vector3 _rollDir;
    float _shake;
    float _shakeTime;

    public Transform Muzzle { get; private set; }
    public Transform ViewModelAnchor { get; private set; }
    public float Pitch { get { return _pitch; } }
    public float MouseSensitivity { get; set; } = 2.2f;
    public bool InvertY { get; set; }
    public bool IsRolling { get { return _rollTimer > 0f; } }
    public float RollCooldown01 { get { return Mathf.Clamp01(_rollCooldown / 1.2f); } }
    public void Shake(float strength) { _shake = Mathf.Max(_shake, strength); _shakeTime = 0.28f; }

    public static FirstPersonController Spawn(Vector3 position)
    {
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = position;

        var controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.38f;
        controller.center = new Vector3(0f, 0.9f, 0f);
        controller.skinWidth = 0.08f;
        controller.minMoveDistance = 0f;

        var character = CharacterCatalog.Get(GameSession.SelectedCharacter);

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(player.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        body.transform.localScale = new Vector3(0.66f, 0.78f, 0.66f);
        Object.Destroy(body.GetComponent<CapsuleCollider>());
        body.GetComponent<MeshRenderer>().sharedMaterial =
            GameMaterials.Create(character.Color * 0.55f, character.Color * 0.45f);
        body.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var pivot = new GameObject("CameraPivot");
        pivot.transform.SetParent(player.transform, false);
        pivot.transform.localPosition = new Vector3(0f, 1.62f, 0f);

        var cameraObject = new GameObject("PlayerCamera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(pivot.transform, false);
        var cam = cameraObject.AddComponent<Camera>();
        cam.nearClipPlane = 0.06f;
        cam.farClipPlane = 3600f;
        cam.fieldOfView = 78f;
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.backgroundColor = new Color(0.03f, 0.04f, 0.07f);
        cameraObject.AddComponent<AudioListener>();
        PlayerLamp.Attach(cameraObject.transform);

        var muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(cameraObject.transform, false);
        muzzle.transform.localPosition = new Vector3(0.2f, -0.16f, 0.62f);

        var viewModelAnchor = new GameObject("ViewModelAnchor");
        viewModelAnchor.transform.SetParent(cameraObject.transform, false);

        var stats = player.AddComponent<PlayerStats>();
        var combat = player.AddComponent<PlayerCombat>();
        player.AddComponent<SummonManager>();
        var fps = player.AddComponent<FirstPersonController>();
        fps._controller = controller;
        fps._cameraPivot = pivot.transform;
        fps.Muzzle = muzzle.transform;
        fps.ViewModelAnchor = viewModelAnchor.transform;
        fps.MouseSensitivity = PrototypeSettings.Get(PrototypeSettings.MouseSensitivity, fps.mouseSensitivity);
        fps.InvertY = PrototypeSettings.Get(PrototypeSettings.InvertY, false);

        stats.ApplyCharacter(character);
        RearViewMirror.Attach(cameraObject.transform, stats);
        TimeDisplacement.Attach(stats);
        combat.enabled = true;
        PlayerAvatarController.Attach(player.transform, stats, combat, cam, character.Color);

        return fps;
    }

    void Awake()
    {
        MouseSensitivity = PrototypeSettings.Get(PrototypeSettings.MouseSensitivity, mouseSensitivity);
        InvertY = PrototypeSettings.Get(PrototypeSettings.InvertY, false);
        _controller = GetComponent<CharacterController>();
        _stats = GetComponent<PlayerStats>();
        if (_cameraPivot == null)
        {
            var cam = GetComponentInChildren<Camera>();
            if (cam != null)
                _cameraPivot = cam.transform.parent != null ? cam.transform.parent : cam.transform;
        }
    }

    void Start()
    {
        if (GameSession.Instance == null || GameSession.Instance.AllowsGameplay)
            SetCursorLocked(true);
    }

    void Update()
    {
        bool playing = GameSession.Instance == null || GameSession.Instance.AllowsGameplay;
        if (!playing)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            SetCursorLocked(!_cursorLocked);

        if (Input.GetMouseButtonDown(0) && !_cursorLocked)
            SetCursorLocked(true);

        _rollCooldown = Mathf.Max(0f, _rollCooldown - Time.deltaTime);
        _shakeTime = Mathf.Max(0f, _shakeTime - Time.deltaTime);
        if (_shakeTime <= 0f)
            _shake = Mathf.MoveTowards(_shake, 0f, Time.deltaTime * 3f);

        if (_rollTimer <= 0f && _rollCooldown <= 0f && _controller.isGrounded &&
            (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C)))
        {
            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            _rollDir = input.sqrMagnitude > 0.01f
                ? transform.TransformDirection(input).normalized
                : transform.forward;
            _rollDir.y = 0f;
            _rollTimer = 0.48f;
            _rollCooldown = 1.2f;
            AudioManager.Instance?.Play("footstep", 0.5f, 0.82f, 0.08f);
        }

        Look();
        Move();
        ApplyCameraKick();
    }

    void ApplyCameraKick()
    {
        if (_cameraPivot == null)
            return;

        float roll = _rollTimer > 0f
            ? Mathf.Sin((1f - _rollTimer / 0.48f) * Mathf.PI)
            : 0f;
        Vector3 pos = new Vector3(0f, 1.62f, 0f);
        pos.y -= roll * 0.42f;

        if (_shake > 0.001f)
        {
            pos += new Vector3(
                Mathf.Sin(Time.time * 61f) * _shake * 0.075f,
                Mathf.Sin(Time.time * 47f) * _shake * 0.06f,
                0f) * _shakeTime;
        }
        _cameraPivot.localPosition = pos;

        Vector3 rot = new Vector3(_pitch, 0f, roll * 11f);
        if (_shake > 0.001f)
            rot.z += Mathf.Sin(Time.time * 53f) * _shake * 2.4f * _shakeTime;
        _cameraPivot.localRotation = Quaternion.Euler(rot);
    }

    void Look()
    {
        if (!_cursorLocked || _cameraPivot == null)
            return;

        float mouseX = Input.GetAxis("Mouse X") * MouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * MouseSensitivity * (InvertY ? 1f : -1f);

        transform.Rotate(0f, mouseX, 0f);
        _pitch = Mathf.Clamp(_pitch + mouseY, pitchMin, pitchMax);
        _cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    void Move()
    {
        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f)
            input.Normalize();

        float mul = _stats != null
            ? _stats.MoveSpeedMultiplier * (_stats.IsPhased ? 1.35f : 1f)
            : 1f;

        if (_rollTimer > 0f)
        {
            _rollTimer = Mathf.Max(0f, _rollTimer - Time.deltaTime);
            Vector3 rollMotion = _rollDir * 14.5f;
            rollMotion.y = _controller.isGrounded ? -2f : _velocity.y;
            _controller.Move(rollMotion * Time.deltaTime);
            ApplyBoundary();
            return;
        }

        bool sprint = Input.GetKey(KeyCode.LeftShift) && input.sqrMagnitude > 0.01f;
        if (sprint)
            mul *= 1.55f;
        Vector3 wish = transform.TransformDirection(input) * (walkSpeed * mul);

        if (_controller.isGrounded)
        {
            _velocity.y = -2f;
            if (Input.GetButtonDown("Jump"))
                _velocity.y = jumpSpeed;
        }
        else
        {
            _velocity.y -= gravity * Time.deltaTime;
        }

        Vector3 before = transform.position;
        _controller.Move((wish + new Vector3(0f, _velocity.y, 0f)) * Time.deltaTime);
        if (_controller.isGrounded)
        {
            Vector3 travelled = transform.position - before;
            travelled.y = 0f;
            _stepDistance += Mathf.Min(1f, travelled.magnitude);
            if (_stepDistance >= 2.1f)
            {
                _stepDistance = 0f;
                AudioManager.Instance?.Play("footstep", 0.32f, Random.Range(0.92f, 1.08f), 0.12f);
                if (Random.value > 0.55f)
                    AudioManager.Instance?.Play("extra_step", 0.2f, Random.Range(0.94f, 1.06f), 0.1f);
            }
        }

        ApplyBoundary();
    }

    void ApplyBoundary()
    {
        float limit = ArenaBuilder.WalkLimit;
        var p = transform.position;
        p.x = Mathf.Clamp(p.x, -limit, limit);
        p.z = Mathf.Clamp(p.z, -limit, limit);
        transform.position = p;
    }

    void SetCursorLocked(bool locked)
    {
        _cursorLocked = locked;
        GameSession.SetCursor(locked);
    }
}
