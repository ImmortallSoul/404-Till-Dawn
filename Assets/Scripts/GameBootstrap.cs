using UnityEngine;
using UnityEngine.Rendering;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] float arenaSize = 2048f;

    // Темна зона поза променем ліхтаря: +15% до початкової темряви.
    const float DarkFillBoost = 1.15f;

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
        // Системна кнопка "назад" не має закривати застосунок.
        Input.backButtonLeavesApp = false;
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
#if UNITY_EDITOR
        // F12 — єдиний виняток із гейтінгу скілів: меню розробника.
        sessionObject.AddComponent<DevMenu>();
#endif

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
        sun.intensity = (pumpkinPatch ? 0.03f : 0.015f) * DarkFillBoost;
        sun.color = pumpkinPatch ? new Color(1f, 0.54f, 0.26f) : new Color(0.5f, 0.62f, 0.95f);
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(48f, -30f, 0f);

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = (pumpkinPatch
            ? new Color(0.014f, 0.008f, 0.024f) : new Color(0.007f, 0.009f, 0.016f)) * DarkFillBoost;
        RenderSettings.ambientEquatorColor = (pumpkinPatch
            ? new Color(0.01f, 0.004f, 0.016f) : new Color(0.004f, 0.005f, 0.008f)) * DarkFillBoost;
        RenderSettings.ambientGroundColor = (pumpkinPatch
            ? new Color(0.005f, 0.002f, 0.007f) : new Color(0.0015f, 0.002f, 0.003f)) * DarkFillBoost;
        RenderSettings.fog = true;
        // Лінійний туман повністю закриває все далі 44 м — саме там спавняться моби.
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 28f;
        RenderSettings.fogEndDistance = EnemySpawner.FogEnd;
        RenderSettings.fogColor = (pumpkinPatch
            ? new Color(0.018f, 0.008f, 0.028f) : new Color(0.006f, 0.008f, 0.014f)) * DarkFillBoost;

        NightSky.Create();
    }
}
