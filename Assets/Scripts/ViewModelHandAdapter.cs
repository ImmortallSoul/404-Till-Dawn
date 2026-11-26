using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Скелетні руки view model'у беруться з тієї самої FabSkeleton-моделі гравця,
/// що й тіло гравця/final boss. Меш печеться один раз, розрізається за вагами
/// кісток руки й повертається так, щоб пальці дивилися вздовж +Z view model'у.
/// WRAD/PSX і тканинні манжети більше не використовуються.
/// </summary>
public sealed class HandVisual
{
    public Mesh Mesh;
    public Material Material;
    public float Scale;
}

public static class ViewModelHandAdapter
{
    const string Resource = "Characters/FabSkeleton/Skeleton_Model";

    static bool _loaded;
    static HandVisual _right;
    static HandVisual _left;

    public static bool TryGet(out HandVisual right, out HandVisual left)
    {
        if (!_loaded)
        {
            _loaded = true;
            TryLoad(out _right, out _left);
        }
        right = _right;
        left = _left;
        return right != null && left != null;
    }

    static void TryLoad(out HandVisual right, out HandVisual left)
    {
        right = null;
        left = null;

        var prefab = Resources.Load<GameObject>(Resource);
        if (prefab == null)
            return;

        GameObject instance = Object.Instantiate(prefab);
        try
        {
            var smrs = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < smrs.Length; i++)
            {
                var smr = smrs[i];
                if (TryBuild(smr, out right, out left))
                    break;
            }
        }
        finally
        {
            Object.Destroy(instance);
        }
    }

    static bool TryBuild(SkinnedMeshRenderer smr, out HandVisual right, out HandVisual left)
    {
        right = null;
        left = null;
        if (smr == null || smr.sharedMesh == null)
            return false;

        var source = smr.sharedMesh;
        var bones = smr.bones;
        if (bones == null || bones.Length == 0)
            return false;

        Transform rightWrist, rightTip, rightForearm;
        Transform leftWrist, leftTip, leftForearm;
        FindBones(bones, true, out rightWrist, out rightTip, out rightForearm);
        FindBones(bones, false, out leftWrist, out leftTip, out leftForearm);
        if (rightWrist == null || leftWrist == null || rightTip == null || leftTip == null)
            return false;

        var baked = new Mesh();
        smr.BakeMesh(baked);
        var verts = baked.vertices;
        var normals = baked.normals;
        if (verts.Length == 0)
        {
            Object.Destroy(baked);
            return false;
        }

        var meshMatrix = smr.transform.localToWorldMatrix;
        var normalMatrix = meshMatrix.inverse.transpose;
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i] = meshMatrix.MultiplyPoint3x4(verts[i]);
            if (normals != null && normals.Length == verts.Length)
                normals[i] = normalMatrix.MultiplyVector(normals[i]).normalized;
        }

        var side = new bool[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i] == null)
                continue;
            side[i] = IsArmBone(bones[i].name, true) || IsArmBone(bones[i].name, false);
        }

        var trisRight = new List<int>();
        var trisLeft = new List<int>();
        var weights = source.boneWeights;
        for (int sub = 0; sub < baked.subMeshCount; sub++)
        {
            int[] tris = baked.GetTriangles(sub);
            for (int i = 0; i + 2 < tris.Length; i += 3)
            {
                int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                if (a >= verts.Length || b >= verts.Length || c >= verts.Length)
                    continue;

                float r = SideWeight(weights, a, b, c, bones, side, true);
                float l = SideWeight(weights, a, b, c, bones, side, false);
                if (r <= l && r > 0.28f)
                {
                    trisRight.Add(a); trisRight.Add(b); trisRight.Add(c);
                }
                else if (l > r && l > 0.28f)
                {
                    trisLeft.Add(a); trisLeft.Add(b); trisLeft.Add(c);
                }
            }
        }

        Vector3 anchorR = rightWrist.position;
        Vector3 anchorL = leftWrist.position;
        Vector3 tipR = rightTip.position;
        Vector3 tipL = leftTip.position;
        Vector3 foreR = rightForearm != null ? rightForearm.position : anchorR - (tipR - anchorR);
        Vector3 foreL = leftForearm != null ? leftForearm.position : anchorL - (tipL - anchorL);

        right = BuildHand(baked, verts, normals, trisRight, anchorR,
            Orientation(tipR - anchorR, anchorR - foreR), smr.sharedMaterial,
            Vector3.Distance(anchorR, tipR));
        left = BuildHand(baked, verts, normals, trisLeft, anchorL,
            Orientation(tipL - anchorL, anchorL - foreL), smr.sharedMaterial,
            Vector3.Distance(anchorL, tipL));

        Object.Destroy(baked);
        return right != null && left != null;
    }

    static float SideWeight(BoneWeight[] weights, int a, int b, int c, Transform[] bones,
        bool[] side, bool right)
    {
        if (weights == null || weights.Length <= Mathf.Max(a, Mathf.Max(b, c)))
            return 0f;

        float total = 0f;
        int[] indices = { a, b, c };
        for (int v = 0; v < 3; v++)
        {
            var w = weights[indices[v]];
            total += Weight(w.boneIndex0, side, right, w.weight0, bones);
            total += Weight(w.boneIndex1, side, right, w.weight1, bones);
            total += Weight(w.boneIndex2, side, right, w.weight2, bones);
            total += Weight(w.boneIndex3, side, right, w.weight3, bones);
        }
        return total / 3f;
    }

    static float Weight(int index, bool[] side, bool right, float amount, Transform[] bones)
    {
        if (amount <= 0f || index < 0 || index >= side.Length || !side[index] || bones[index] == null)
            return 0f;
        return IsArmBone(bones[index].name, right) ? amount : 0f;
    }

    static void FindBones(Transform[] bones, bool right,
        out Transform wrist, out Transform tip, out Transform forearm)
    {
        wrist = null;
        tip = null;
        forearm = null;
        if (bones == null)
            return;

        for (int i = 0; i < bones.Length; i++)
        {
            var bone = bones[i];
            if (bone == null || !IsSide(bone.name, right))
                continue;

            string name = bone.name.ToLowerInvariant();
            if (wrist == null && (name == "hand.r" || name == "hand.l" ||
                name.Contains("hand.") || name.Contains("wrist")))
                wrist = bone;

            if (tip == null && (name.Contains("middle3") || name.Contains("index3") ||
                name.Contains("finger_middle3")))
                tip = bone;

            if (forearm == null && (name.Contains("arm_stretch") || name.Contains("forearm_twist")))
                forearm = bone;
        }
    }

    static bool IsArmBone(string boneName, bool right)
    {
        if (!IsSide(boneName, right))
            return false;
        string n = boneName.ToLowerInvariant();
        return n.Contains("arm") || n.Contains("hand") || n.Contains("thumb") ||
               n.Contains("pinky") || n.Contains("ring") || n.Contains("middle") ||
               n.Contains("index") || n.Contains("shoulder");
    }

    static bool IsSide(string boneName, bool right)
    {
        string name = boneName.ToLowerInvariant();
        if (right)
            return name.EndsWith(".r") || name.EndsWith("_r") || name.EndsWith(".right") ||
                   name.EndsWith("_right") || name.Contains("right");
        return name.EndsWith(".l") || name.EndsWith("_l") || name.EndsWith(".left") ||
               name.EndsWith("_left") || name.Contains("left");
    }

    static Quaternion Orientation(Vector3 forward, Vector3 arm)
    {
        if (forward.sqrMagnitude < 0.000001f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 up = arm - Vector3.Dot(arm, forward) * forward;
        if (up.sqrMagnitude < 0.000001f)
            up = Vector3.up - Vector3.Dot(Vector3.up, forward) * forward;
        if (up.sqrMagnitude < 0.000001f)
            up = Vector3.Cross(forward, Vector3.right);
        up.Normalize();

        return Quaternion.Inverse(Quaternion.LookRotation(forward, up));
    }

    static HandVisual BuildHand(Mesh source, Vector3[] sourceVerts, Vector3[] sourceNormals,
        List<int> triangles, Vector3 anchor, Quaternion rotation, Material material, float handLength)
    {
        if (triangles == null || triangles.Count == 0)
            return null;

        var map = new Dictionary<int, int>();
        var used = new List<int>();
        for (int i = 0; i < triangles.Count; i++)
        {
            int old = triangles[i];
            if (!map.ContainsKey(old))
            {
                map[old] = used.Count;
                used.Add(old);
            }
        }

        var oldUvs = source.uv;
        var oldColors = source.colors;
        var oldTangents = source.tangents;

        var verts = new Vector3[used.Count];
        var normals = new Vector3[used.Count];
        var uvs = new Vector2[used.Count];
        var colors = new Color[used.Count];
        var tangents = new Vector4[used.Count];

        // Нормалізуємо за довжиною кисті, щоб різні масштаби імпорту не ламали grip.
        float scale = 0.32f / Mathf.Max(0.02f, handLength);
        for (int i = 0; i < used.Count; i++)
        {
            int old = used[i];
            verts[i] = rotation * (sourceVerts[old] - anchor) * scale;
            normals[i] = sourceNormals != null && sourceNormals.Length == sourceVerts.Length
                ? rotation * sourceNormals[old]
                : Vector3.up;
            uvs[i] = oldUvs != null && oldUvs.Length == sourceVerts.Length ? oldUvs[old] : Vector2.zero;
            colors[i] = oldColors != null && oldColors.Length == sourceVerts.Length ? oldColors[old] : Color.white;
            tangents[i] = oldTangents != null && oldTangents.Length == sourceVerts.Length ? oldTangents[old] : Vector4.zero;
        }

        var indices = new int[triangles.Count];
        for (int i = 0; i < triangles.Count; i++)
            indices[i] = map[triangles[i]];

        var mesh = new Mesh { name = "FabSkeletonHand" };
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);
        if (tangents.Length > 0 && tangents[0] != Vector4.zero)
            mesh.SetTangents(tangents);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateBounds();
        mesh.UploadMeshData(false);

        return new HandVisual { Mesh = mesh, Material = material, Scale = 1f };
    }
}
