using UnityEngine;

public class GameCamera : MonoBehaviour
{
    public static Vector3 AimPoint { get; private set; }

    Transform _target;
    Camera _camera;
    Vector3 _offset = new Vector3(0f, 18f, -13f);

    public static GameCamera Create(Transform target)
    {
        var go = new GameObject("GameCamera");
        var cam = go.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.fieldOfView = 52f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 200f;
        cam.backgroundColor = new Color(0.03f, 0.04f, 0.07f);
        go.AddComponent<AudioListener>();
        var follower = go.AddComponent<GameCamera>();
        follower._camera = cam;
        follower._target = target;
        follower.transform.position = target.position + follower._offset;
        follower.transform.LookAt(target.position);
        AimPoint = target.position;
        return follower;
    }

    void LateUpdate()
    {
        if (_target == null)
            return;

        transform.position = _target.position + _offset;
        transform.LookAt(_target.position + Vector3.up * 0.6f);
        UpdateAim();
    }

    void UpdateAim()
    {
        if (_camera == null)
            return;

        Vector3 mouse = Input.mousePosition;
        mouse.z = 1f;
        Ray ray = _camera.ScreenPointToRay(mouse);
        Plane ground = new Plane(Vector3.up, new Vector3(0f, 0.9f, 0f));
        float distance;
        if (ground.Raycast(ray, out distance))
            AimPoint = ray.GetPoint(distance);
    }
}
