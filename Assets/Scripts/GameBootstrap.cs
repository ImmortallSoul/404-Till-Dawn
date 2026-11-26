using UnityEngine;
using UnityEngine.Rendering;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] float arenaSize = 2048f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBootstrap()
    {
        if (FindAnyObjectByType<GameBootstrap>() != null)
            return;

        var go = new GameObject("GameBootstrap");
        go.AddComponent<GameBootstrap>();
    }

    void Start()
    {
        AudioManager.Ensure();
        if (GameSession.Instance == null || GameSession.Instance.State == RunState.Menu)
            AudioManager.Instance?.SetState(MusicState.Menu);
        Debug.Log("[Assets] animation clips=" + CharacterAnimationController.LoadedClipCount);
        StripDefaultSceneCameras();
        SetupLighting();
        CreateMenuCamera();
        gameObject.AddComponent<NightCycle>();
        ArenaBuilder.Build(arenaSize, GameSession.SelectedMap);

        var sessionObject = new GameObject("GameSession");
        var session = sessionObject.AddComponent<GameSession>();
        sessionObject.AddComponent<EnemySpawner>();
        sessionObject.AddComponent<GameHud>();
        sessionObject.AddComponent<UpgradeMenu>();
        sessionObject.AddComponent<PauseMenu>();
        sessionObject.AddComponent<StartMenu>();

        GameSession.SetCursor(false);
    }

    static void StripDefaultSceneCameras()
    {
        var cameras = FindObjectsByType<Camera>();
        for (int i = 0; i < cameras.Length; i++)
            Destroy(cameras[i].gameObject);
    }

    static void CreateMenuCamera()
    {
        var go = new GameObject("MenuCamera");
        var cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 62f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 3600f;
        cam.transform.position = new Vector3(0f, 14f, -34f);
        cam.transform.rotation = Quaternion.Euler(9f, 18f, 0f);
    }

    static void SetupLighting()
    {
        var lights = FindObjectsByType<Light>();
        Light sun = null;
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].type == LightType.Directional)
            {
                sun = lights[i];
                break;
            }
        }

        if (sun == null)
        {
            var lightObject = new GameObject("Directional Light");
            sun = lightObject.AddComponent<Light>();
            sun.type = LightType.Directional;
        }

        bool pumpkinPatch = GameSession.SelectedMap == MapType.PumpkinPatch;
        sun.intensity = pumpkinPatch ? 0.24f : 0.12f;
        sun.color = pumpkinPatch ? new Color(1f, 0.54f, 0.26f) : new Color(0.5f, 0.62f, 0.95f);
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(48f, -30f, 0f);

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = pumpkinPatch
            ? new Color(0.17f, 0.10f, 0.24f) : new Color(0.08f, 0.10f, 0.17f);
        RenderSettings.ambientEquatorColor = pumpkinPatch
            ? new Color(0.12f, 0.055f, 0.17f) : new Color(0.05f, 0.06f, 0.09f);
        RenderSettings.ambientGroundColor = pumpkinPatch
            ? new Color(0.055f, 0.02f, 0.07f) : new Color(0.015f, 0.018f, 0.025f);
        RenderSettings.fog = true;
        // Лінійний туман повністю закриває все далі 44 м — саме там спавняться моби.
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 28f;
        RenderSettings.fogEndDistance = EnemySpawner.FogEnd;
        RenderSettings.fogColor = pumpkinPatch
            ? new Color(0.13f, 0.06f, 0.18f) : new Color(0.05f, 0.06f, 0.10f);

        NightSky.Create();
    }
}
