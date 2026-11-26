using UnityEditor;
using UnityEngine;

/// <summary>
/// Імпортує завантажені CC0-моделі й друкує діагностику мешів.
/// </summary>
public static class ImportMissingModels
{
    [MenuItem("Tools/Import Missing Models")]
    public static void Run()
    {
        string[] paths =
        {
            "Assets/Resources/Creatures/Dragon.fbx",
            "Assets/Resources/Creatures/Bat.fbx",
            "Assets/Resources/Creatures/Butterfly.fbx",
            "Assets/Resources/Weapons/Crossbow.fbx",
            "Assets/Resources/Weapons/KryvenBlade/V_KRYVEN_BLADE.prefab",
            "Assets/Resources/Summons/ReapersEdge.fbx"
        };
        foreach (string path in paths)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning("[ImportMissingModels] MISSING " + path);
                continue;
            }
            var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
            var skinned = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            int verts = 0;
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool first = true;
            foreach (var filter in filters)
            {
                if (filter.sharedMesh == null) continue;
                verts += filter.sharedMesh.vertexCount;
                Bounds b = filter.sharedMesh.bounds;
                if (first) { bounds = b; first = false; }
                else bounds.Encapsulate(b);
            }
            foreach (var renderer in skinned)
            {
                if (renderer.sharedMesh == null) continue;
                verts += renderer.sharedMesh.vertexCount;
                Bounds b = renderer.sharedMesh.bounds;
                if (first) { bounds = b; first = false; }
                else bounds.Encapsulate(b);
            }
            Debug.Log($"[ImportMissingModels] {path}: filters={filters.Length} skinned={skinned.Length} renderers={renderers.Length} verts={verts} bounds={bounds.size}");
        }
    }
}
