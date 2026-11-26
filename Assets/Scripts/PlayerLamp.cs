using UnityEngine;

public class PlayerLamp : MonoBehaviour
{
    public static PlayerLamp Instance { get; private set; }

    /// <summary>Поточне світло ліхтаря — використовується дзеркалом заднього виду.</summary>
    public Light Light { get { return _light; } }

    Light _light;
    // Промінь закінчується далі за туман (44 м), щоб не було видно різкої межі зрізу.
    float _baseRange = 48f;
    // Менше пересвітлення в конусі: яскравість помірна, спалах короткий.
    float _baseIntensity = 9f;
    float _flash;

    public static void Attach(Transform parent)
    {
        var go = new GameObject("PlayerLamp");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0.12f, 0f);

        var lamp = go.AddComponent<PlayerLamp>();
        lamp._light = go.AddComponent<Light>();
        lamp._light.type = LightType.Spot;
        lamp._light.spotAngle = 52f;
        // Широкий півтіньовий перехід — межа променя не «пересічена» різкою лінією.
        lamp._light.innerSpotAngle = 16f;
        lamp._light.color = new Color(1f, 0.94f, 0.84f);
        lamp._light.shadows = LightShadows.None;
        lamp._light.range = lamp._baseRange;
        lamp._light.intensity = lamp._baseIntensity;
    }

    void Awake()
    {
        Instance = this;
        if (_light == null)
            _light = GetComponent<Light>();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetRange(float range)
    {
        // Дальність ліхтаря задається параметром зору гравця, але промінь має діставати
        // далі, ніж старий точковий "ламповий" радіус.
        _baseRange = Mathf.Max(36f, range * 4.4f);
    }

    public void AddRange(float amount)
    {
        _baseRange += amount;
    }

    public void Flash(float amount)
    {
        _flash = Mathf.Max(_flash, amount);
    }

    void LateUpdate()
    {
        // Світимо саме туди, куди дивиться гравець, а не навколо себе.
        var cam = Camera.main;
        if (cam != null)
            transform.rotation = cam.transform.rotation;
        else
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    void Update()
    {
        if (_light == null)
            return;
        _flash = Mathf.MoveTowards(_flash, 0f, Time.unscaledDeltaTime * 6f);
        _light.intensity = _baseIntensity + _flash * 2.5f;
        _light.range = _baseRange + _flash * 3f;
    }
}
