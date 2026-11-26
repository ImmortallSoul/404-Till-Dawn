using UnityEngine;

public class PlayerLamp : MonoBehaviour
{
    public static PlayerLamp Instance { get; private set; }

    Light _light;
    float _baseRange = 10f;
    float _baseIntensity = 2.4f;
    float _flash;

    public static void Attach(Transform parent)
    {
        var go = new GameObject("PlayerLamp");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0.4f, 0f);

        var lamp = go.AddComponent<PlayerLamp>();
        lamp._light = go.AddComponent<Light>();
        lamp._light.type = LightType.Point;
        lamp._light.color = new Color(1f, 0.88f, 0.7f);
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
        _baseRange = Mathf.Max(4f, range);
    }

    public void AddRange(float amount)
    {
        _baseRange += amount;
    }

    public void Flash(float amount)
    {
        _flash = Mathf.Max(_flash, amount);
    }

    void Update()
    {
        if (_light == null)
            return;
        _flash = Mathf.MoveTowards(_flash, 0f, Time.unscaledDeltaTime * 6f);
        _light.intensity = _baseIntensity + _flash * 1.8f;
        _light.range = _baseRange + _flash * 1.5f;
    }
}
