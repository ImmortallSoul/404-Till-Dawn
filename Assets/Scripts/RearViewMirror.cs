using UnityEngine;

/// <summary>
/// Скіл "Дзеркало заднього виду".
/// Створює другу камеру, яка дивиться назад, і виводить її в кут екрана через GameHud.
/// Увімкнення/вимкнення — клавіша Q.
/// </summary>
public class RearViewMirror : MonoBehaviour
{
    const int TextureWidth = 512;
    const int TextureHeight = 288;

    static RearViewMirror _instance;

    Camera _sourceCamera;
    Camera _camera;
    RenderTexture _texture;
    PlayerStats _stats;
    bool _visible = true;
    bool _devOverride;

    public static RearViewMirror Instance { get { return _instance; } }
    public Texture Texture { get { return _texture; } }
    public float Aspect { get { return TextureWidth / (float)TextureHeight; } }
    public bool IsActive
    {
        get { return _visible && (_devOverride || (_stats != null && _stats.RearViewMirror)); }
    }

    /// <summary>Меню розробника (F12): дзеркало без апгрейд-карти.</summary>
    public void DevSetActive(bool active)
    {
        _devOverride = true;
        _visible = active;
    }

    void Awake()
    {
        _instance = this;
    }

    public static void Attach(Transform sourceCamera, PlayerStats stats)
    {
        if (_instance != null)
            Destroy(_instance.gameObject);

        var go = new GameObject("RearViewMirror");
        var mirror = go.AddComponent<RearViewMirror>();
        mirror._sourceCamera = sourceCamera != null ? sourceCamera.GetComponent<Camera>() : null;
        mirror._stats = stats;
        mirror.Build();
    }

    void Build()
    {
        _texture = new RenderTexture(TextureWidth, TextureHeight, 16, RenderTextureFormat.Default);
        _texture.name = "RearViewTexture";
        _texture.filterMode = FilterMode.Bilinear;
        _texture.Create();

        var cameraObject = new GameObject("RearViewCamera");
        cameraObject.transform.SetParent(transform, false);

        _camera = cameraObject.AddComponent<Camera>();
        _camera.targetTexture = _texture;
        _camera.clearFlags = CameraClearFlags.Skybox;
        _camera.cullingMask = ~0;
        _camera.nearClipPlane = 0.08f;
        _camera.farClipPlane = 2200f;
        _camera.fieldOfView = 82f;
        _camera.useOcclusionCulling = false;
        _camera.allowHDR = false;
        _camera.allowMSAA = false;

        // Дзеркало не повинно додавати другий AudioListener.
        ApplyLevel();
    }

    void Update()
    {
        bool playing = GameSession.Instance == null || GameSession.Instance.AllowsGameplay;
        if (!playing || _stats == null || !_stats.RearViewMirror)
        {
            if (_camera != null)
                _camera.enabled = false;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Q))
            _visible = !_visible;

        if (_camera != null)
            _camera.enabled = _visible;

        ApplyLevel();
    }

    void ApplyLevel()
    {
        if (_camera == null || _stats == null)
            return;

        int level = Mathf.Max(1, _stats.RearViewMirrorLevel);
        _camera.fieldOfView = level >= 3 ? 100f : level >= 2 ? 90f : 82f;
    }

    void LateUpdate()
    {
        if (_sourceCamera == null || _camera == null)
            return;

        _camera.transform.position = _sourceCamera.transform.position;
        _camera.transform.rotation = _sourceCamera.transform.rotation * Quaternion.Euler(0f, 180f, 0f);
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;

        if (_texture != null)
        {
            _texture.Release();
            Destroy(_texture);
            _texture = null;
        }
    }
}
