using UnityEngine;

public sealed class WeaponAssetView
{
    public Transform Root;
    public Transform Muzzle;
    public Transform SecondMuzzle;
    public float Length;
}

/// <summary>
/// Нормалізує імпортовану CC0-модель для view model'у без евристик за радіусом.
/// Орієнтація, зміщення й точка дула беруться з ViewModelPoseCatalog.
/// </summary>
public static class ViewModelAssetAdapter
{
    const float DualSmgSeparation = ViewModelPoseCatalog.DualSmgGripSeparation;

    public static WeaponAssetView Create(WeaponId id, Transform parent)
    {
        var prefab = WeaponAssetCatalog.LoadPrefab(id);
        if (prefab == null)
            return null;

        string resource;
        float targetLength;
        bool doubled;
        if (!WeaponAssetCatalog.TryGet(id, out resource, out targetLength, out doubled))
            return null;

        ViewModelPose pose = ViewModelPoseCatalog.Get(id);

        var view = new GameObject("Asset_" + id).transform;
        view.SetParent(parent, false);

        var muzzle = new GameObject("MuzzleAnchor").transform;
        muzzle.SetParent(view, false);
        // Перший постріл — права рука, другий — дзеркальна ліва.
        muzzle.localPosition = doubled
            ? pose.Muzzle + Vector3.right * DualSmgSeparation
            : pose.Muzzle;
        muzzle.localRotation = Quaternion.identity;

        Transform secondMuzzle = null;
        if (doubled)
        {
            secondMuzzle = new GameObject("MuzzleAnchor_L").transform;
            secondMuzzle.SetParent(view, false);
            secondMuzzle.localPosition = pose.Muzzle + Vector3.left * DualSmgSeparation;
            secondMuzzle.localRotation = Quaternion.identity;
        }

        int copies = doubled ? 2 : 1;
        for (int i = 0; i < copies; i++)
        {
            bool left = doubled && i == 1;
            var holder = new GameObject(left ? "Model_Left" : "Model_Right").transform;
            holder.SetParent(view, false);
            holder.localPosition = Vector3.zero;
            holder.localRotation = Quaternion.Euler(pose.ModelEuler);
            if (doubled)
            {
                // Модель Quaternius лежить уздовж X, дуло на -X. Дзеркалимо
                // поперечну вісь Z, а не X: інакше дуло розвертається до камери.
                // Після повороту holder на +90° по Y ось Z стає екранним X.
                holder.localScale = left
                    ? new Vector3(0.82f, 0.82f, -0.82f)
                    : Vector3.one * 0.82f;
            }

            GameObject instance = Object.Instantiate(prefab, holder);
            instance.name = "Imported";
            StripAndStyle(instance, id);
            // Fit has to see the holder at zero offset; otherwise both SMGs are
            // measured against the same target point and end up glued together.
            Fit(instance.transform, holder, view, targetLength, pose);
            if (doubled)
                holder.localPosition = new Vector3(left ? -DualSmgSeparation : DualSmgSeparation, 0f, 0f);
        }

        return new WeaponAssetView
        {
            Root = view,
            Muzzle = muzzle,
            SecondMuzzle = secondMuzzle,
            Length = targetLength
        };
    }

    static void StripAndStyle(GameObject instance, WeaponId id)
    {
        var colliders = instance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            Object.Destroy(colliders[i]);

        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            string source = renderer.sharedMaterial != null ? renderer.sharedMaterial.name : renderer.gameObject.name;
            renderer.sharedMaterial = WeaponAssetCatalog.StyleRenderer(id, renderer, source);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    static void Fit(Transform instance, Transform holder, Transform view, float targetLength, ViewModelPose pose)
    {
        instance.localPosition = Vector3.zero;

        // Міряємо в просторі view (після повороту holder), бо саме так виглядає
        // модель відносно камери. У просторі holder довга вісь Quaternius-моделей
        // лежить не по Z, і старий вимір давав масштаб у ~190 разів більший.
        Bounds bounds = RendererBounds(view, instance);
        if (bounds.size.sqrMagnitude < 0.000001f)
            return;

        // Different CC0 packs store the long axis differently (Quaternius often X/Z,
        // KayKit props can be Y-heavy). Scaling by the largest extent prevents a
        // short-axis measurement from blowing the model up in front of the camera.
        float length = Mathf.Max(0.05f, Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z)));
        instance.localScale *= targetLength / length;

        bounds = RendererBounds(view, instance);
        Vector3 targetLocal = holder.InverseTransformPoint(view.TransformPoint(pose.ModelOffset));
        Vector3 centerLocal = holder.InverseTransformPoint(view.TransformPoint(bounds.center));
        instance.localPosition = targetLocal - centerLocal;
    }

    /// <summary>
    /// Використовує mesh.bounds, а не mesh.vertices: імпортовані FBX можуть бути нечитаємими.
    /// </summary>
    static Bounds RendererBounds(Transform space, Transform root)
    {
        var filters = root.GetComponentsInChildren<MeshFilter>(true);
        bool first = true;
        var result = new Bounds(Vector3.zero, Vector3.zero);

        for (int i = 0; i < filters.Length; i++)
        {
            Mesh mesh = filters[i].sharedMesh;
            if (mesh == null)
                continue;

            Bounds local = mesh.bounds;
            Matrix4x4 matrix = space.worldToLocalMatrix * filters[i].transform.localToWorldMatrix;
            Vector3 center = local.center;
            Vector3 extents = local.extents;

            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 point = matrix.MultiplyPoint(center + new Vector3(
                    x * extents.x, y * extents.y, z * extents.z));
                if (first)
                {
                    result = new Bounds(point, Vector3.zero);
                    first = false;
                }
                else
                {
                    result.Encapsulate(point);
                }
            }
        }

        return result;
    }
}
