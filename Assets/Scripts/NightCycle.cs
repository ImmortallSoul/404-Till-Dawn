using UnityEngine;

public class NightCycle : MonoBehaviour
{
    // Зона поза променем ліхтаря на 15% світліша; до світанку підсилення зникає.
    const float DarkFillBoost = 1.15f;

    readonly Color _nightSky = new Color(0.007f, 0.009f, 0.016f);
    readonly Color _nightEquator = new Color(0.004f, 0.005f, 0.008f);
    readonly Color _nightGround = new Color(0.0015f, 0.002f, 0.003f);
    readonly Color _nightFog = new Color(0.006f, 0.008f, 0.014f);

    readonly Color _dawnSky = new Color(0.3f, 0.22f, 0.21f);
    readonly Color _dawnEquator = new Color(0.2f, 0.14f, 0.15f);
    readonly Color _dawnGround = new Color(0.1f, 0.075f, 0.08f);
    readonly Color _dawnFog = new Color(0.22f, 0.17f, 0.18f);

    readonly Color _moonColor = new Color(0.55f, 0.65f, 0.95f);
    readonly Color _sunColor = new Color(1f, 0.72f, 0.5f);

    Light _sun;

    void Awake()
    {
        var lights = FindObjectsByType<Light>();
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].type == LightType.Directional)
            {
                _sun = lights[i];
                break;
            }
        }
    }

    void Update()
    {
        var session = GameSession.Instance;
        if (session == null)
            return;

        // Світління йде рівномірно разом із таймером: чим менше лишилось,
        // тим ближче ніч до світанку. На самому фіналі — повний світанок.
        bool won = session.State == RunState.Won;
        float dawn = won ? 1f : Mathf.Pow(session.Progress, 2.4f);

        // Дистанції туману лишаються бойовими майже всю ніч; розширюються
        // лише на фінальному світанку, щоб не змінювати читаємість арени.
        float fogDawn = won ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.9f, 1f, session.Progress));

        float fill = Mathf.Lerp(DarkFillBoost, 1f, dawn);
        RenderSettings.ambientSkyColor = Color.Lerp(_nightSky, _dawnSky, dawn) * fill;
        RenderSettings.ambientEquatorColor = Color.Lerp(_nightEquator, _dawnEquator, dawn) * fill;
        RenderSettings.ambientGroundColor = Color.Lerp(_nightGround, _dawnGround, dawn) * fill;
        RenderSettings.fogColor = Color.Lerp(_nightFog, _dawnFog, dawn) * fill;
        RenderSettings.fogStartDistance = Mathf.Lerp(28f, 34f, fogDawn);
        RenderSettings.fogEndDistance = Mathf.Lerp(EnemySpawner.FogEnd, 52f, fogDawn);
        NightSky.SetDawn(dawn);

        if (_sun != null)
        {
            _sun.intensity = Mathf.Lerp(0.02f * DarkFillBoost, 0.62f, dawn);
            _sun.color = Color.Lerp(_moonColor, _sunColor, dawn);
        }
    }
}
