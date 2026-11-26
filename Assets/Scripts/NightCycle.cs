using UnityEngine;

public class NightCycle : MonoBehaviour
{
    readonly Color _nightSky = new Color(0.10f, 0.12f, 0.20f);
    readonly Color _nightEquator = new Color(0.06f, 0.07f, 0.10f);
    readonly Color _nightGround = new Color(0.02f, 0.02f, 0.03f);
    readonly Color _nightFog = new Color(0.06f, 0.07f, 0.11f);

    readonly Color _dawnSky = new Color(0.48f, 0.34f, 0.30f);
    readonly Color _dawnEquator = new Color(0.32f, 0.22f, 0.22f);
    readonly Color _dawnGround = new Color(0.16f, 0.12f, 0.12f);
    readonly Color _dawnFog = new Color(0.34f, 0.26f, 0.26f);

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

        // Ніч триває до останніх 12% часу: небо не світліє зарано.
        float dawn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.88f, 1f, session.Progress));

        RenderSettings.ambientSkyColor = Color.Lerp(_nightSky, _dawnSky, dawn);
        RenderSettings.ambientEquatorColor = Color.Lerp(_nightEquator, _dawnEquator, dawn);
        RenderSettings.ambientGroundColor = Color.Lerp(_nightGround, _dawnGround, dawn);
        RenderSettings.fogColor = Color.Lerp(_nightFog, _dawnFog, dawn);
        RenderSettings.fogStartDistance = Mathf.Lerp(28f, 34f, dawn);
        RenderSettings.fogEndDistance = Mathf.Lerp(EnemySpawner.FogEnd, 52f, dawn);
        NightSky.SetDawn(dawn);

        if (_sun != null)
        {
            _sun.intensity = Mathf.Lerp(0.14f, 0.62f, dawn);
            _sun.color = Color.Lerp(_moonColor, _sunColor, dawn);
        }
    }
}
