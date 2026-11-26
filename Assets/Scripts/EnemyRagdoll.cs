using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Фізична смерть ворога. Для ригованих моделей кістки SkinnedMeshRenderer
/// перетворюються на ланцюжок Rigidbody + CharacterJoint, тому меш реально
/// падає й згинається. Для статичних моделей створюється менший набір
/// фізичних частин із з'єднаннями. Усі частини мають обмежений час життя.
/// </summary>
public sealed class EnemyRagdoll : MonoBehaviour
{
    const int MaxActive = 8;
    static int _active;

    readonly List<Rigidbody> _bodies = new List<Rigidbody>();
    float _life;
    bool _released;

    public static EnemyRagdoll Spawn(Enemy enemy, Transform visual, Vector3 impulse, float height)
    {
        if (enemy == null || visual == null || _active >= MaxActive)
            return null;

        var go = new GameObject("Ragdoll_" + enemy.Kind);
        go.transform.position = visual.position;
        go.transform.rotation = visual.rotation;

        var ragdoll = go.AddComponent<EnemyRagdoll>();
        ragdoll._life = 5.5f;
        _active++;

        var bones = CollectBones(visual);
        // Зберігаємо початкову ієрархію до перебатькування — за нею відновлюємо joints.
        var parents = OriginalParents(bones);
        // Кінцівки не можуть лишатися дітьми одна одної: вкладені Rigidbody
        // починають тягнути сусідні кістки й «колбаситися» після падіння.
        ReparentBones(bones, ragdoll.transform);
        if (bones.Count >= 3)
            ragdoll.BuildBones(bones, parents, visual, impulse, height);
        else
            ragdoll.BuildParts(visual, impulse, height);

        ragdoll.IgnoreSelfCollisions();
        return ragdoll;
    }

    static List<Transform> CollectBones(Transform visual)
    {
        var result = new List<Transform>();
        var skins = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skins.Length; i++)
        {
            var bones = skins[i].bones;
            if (bones == null)
                continue;
            for (int b = 0; b < bones.Length; b++)
            {
                if (bones[b] != null && !result.Contains(bones[b]))
                    result.Add(bones[b]);
            }
        }
        return result;
    }

    static Dictionary<Transform, Transform> OriginalParents(List<Transform> bones)
    {
        var result = new Dictionary<Transform, Transform>();
        for (int i = 0; i < bones.Count; i++)
        {
            var bone = bones[i];
            if (bone != null && bone.parent != null && bones.Contains(bone.parent))
                result[bone] = bone.parent;
        }
        return result;
    }

    static void ReparentBones(List<Transform> bones, Transform root)
    {
        for (int i = 0; i < bones.Count; i++)
        {
            var bone = bones[i];
            if (bone != null && bone.parent != root)
                bone.SetParent(root, true);
        }
    }

    void BuildBones(List<Transform> bones, Dictionary<Transform, Transform> parents,
        Transform visual, Vector3 impulse, float height)
    {
        Transform root = FindRoot(bones);
        float unit = Mathf.Max(0.05f, height / 1.8f);

        for (int i = 0; i < bones.Count; i++)
        {
            var bone = bones[i];
            if (bone == root || IsDetail(bone.name))
                continue;

            Transform parent;
            parents.TryGetValue(bone, out parent);
            if (parent != null && parent.GetComponent<Rigidbody>() == null && parent != root)
                continue;

            AddBody(bone, parent, unit, height);
        }

        AddBody(root, null, unit, height);

        // Батьківські кістки мають існувати раніше за дітей — будуємо joints окремим проходом.
        for (int i = 0; i < _bodies.Count; i++)
        {
            var body = _bodies[i];
            Transform bone = body.transform;
            Transform parent;
            parents.TryGetValue(bone, out parent);
            Rigidbody connected = parent != null ? parent.GetComponent<Rigidbody>() : null;
            if (connected == null)
                continue;

            var joint = body.gameObject.AddComponent<CharacterJoint>();
            joint.connectedBody = connected;
            joint.enableCollision = false;
            joint.autoConfigureConnectedAnchor = true;
            ConfigureJoint(joint);
        }

        ApplyImpulse(impulse);
    }

    void BuildParts(Transform visual, Vector3 impulse, float height)
    {
        var renderers = visual.GetComponentsInChildren<Renderer>(true);
        float unit = Mathf.Max(0.05f, height / 1.8f);
        int added = 0;

        for (int i = 0; i < renderers.Length && added < 6; i++)
        {
            var renderer = renderers[i];
            if (renderer.bounds.size.sqrMagnitude < 0.000001f)
                continue;

            var size = new Vector3(
                Mathf.Max(0.08f, renderer.bounds.size.x),
                Mathf.Max(0.08f, renderer.bounds.size.y),
                Mathf.Max(0.08f, renderer.bounds.size.z));

            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = "Part_" + added;
            Object.Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(transform, false);
            part.transform.position = renderer.bounds.center;
            part.transform.localScale = size;
            part.GetComponent<MeshRenderer>().sharedMaterial = renderer.sharedMaterial;

            var body = part.AddComponent<Rigidbody>();
            body.mass = 1.2f;
            body.linearDamping = 0.35f;
            body.angularDamping = 0.45f;
            body.maxAngularVelocity = 8f;

            var box = part.AddComponent<BoxCollider>();
            box.size = Vector3.one;

            if (added > 0 && _bodies.Count > 0)
            {
                var joint = body.gameObject.AddComponent<CharacterJoint>();
                joint.connectedBody = _bodies[0];
                joint.autoConfigureConnectedAnchor = true;
                joint.enableCollision = false;
                ConfigureJoint(joint);
            }
            _bodies.Add(body);
            renderer.enabled = false;
            added++;
        }

        if (added == 0)
        {
            var body = gameObject.AddComponent<Rigidbody>();
            body.mass = 2f;
            body.maxAngularVelocity = 8f;
            var box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(0.5f, Mathf.Max(0.3f, height), 0.5f);
            _bodies.Add(body);
        }

        ApplyImpulse(impulse * Mathf.Max(0.1f, unit));
    }

    static void ConfigureJoint(CharacterJoint joint)
    {
        joint.lowTwistLimit = new SoftJointLimit { limit = -35f, bounciness = 0f };
        joint.highTwistLimit = new SoftJointLimit { limit = 70f, bounciness = 0f };
        joint.swing1Limit = new SoftJointLimit { limit = 55f, bounciness = 0f };
        joint.swing2Limit = new SoftJointLimit { limit = 40f, bounciness = 0f };
        joint.enableProjection = true;
        joint.projectionDistance = 0.1f;
        joint.projectionAngle = 8f;
    }

    void IgnoreSelfCollisions()
    {
        for (int i = 0; i < _bodies.Count; i++)
        {
            var a = _bodies[i];
            if (a == null)
                continue;
            var ca = a.GetComponent<Collider>();
            if (ca == null)
                continue;

            for (int j = i + 1; j < _bodies.Count; j++)
            {
                var b = _bodies[j];
                if (b == null)
                    continue;
                var cb = b.GetComponent<Collider>();
                if (cb != null)
                    Physics.IgnoreCollision(ca, cb, true);
            }
        }
    }

    void AddBody(Transform bone, Transform parent, float unit, float height)
    {
        var body = bone.gameObject.AddComponent<Rigidbody>();
        body.mass = Mathf.Clamp(0.4f + unit * 1.4f, 0.4f, 2.4f);
        body.linearDamping = 0.28f;
        body.angularDamping = 0.42f;
        body.maxAngularVelocity = 8f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        _bodies.Add(body);

        var collider = bone.gameObject.AddComponent<CapsuleCollider>();
        float radius = Mathf.Clamp(unit * (IsTorso(bone.name) ? 0.22f : 0.11f), 0.035f, 0.32f);
        collider.radius = radius;
        collider.height = Mathf.Clamp(unit * (IsTorso(bone.name) ? 0.6f : 0.3f), 0.12f, 1f);
        collider.direction = 1;
        collider.center = Vector3.zero;

        // Ignore Raycast: стрілянина й пошук цілей не можуть зачіпати мертве тіло.
        bone.gameObject.layer = 2;
    }

    void ApplyImpulse(Vector3 impulse)
    {
        for (int i = 0; i < _bodies.Count; i++)
        {
            if (_bodies[i] == null)
                continue;
            float factor = i == 0 ? 1f : Random.Range(0.35f, 0.85f);
            _bodies[i].AddForce(impulse * factor, ForceMode.Impulse);
            _bodies[i].AddTorque(Random.insideUnitSphere * 2.4f, ForceMode.Impulse);
        }
    }

    static Transform FindRoot(List<Transform> bones)
    {
        for (int i = 0; i < bones.Count; i++)
        {
            string n = bones[i].name.ToLowerInvariant();
            if (n.Contains("hips") || n.Contains("pelvis") || n.Contains("root"))
                return bones[i];
        }

        Transform best = bones[0];
        int bestDepth = int.MaxValue;
        for (int i = 0; i < bones.Count; i++)
        {
            int depth = Depth(bones[i]);
            if (depth < bestDepth)
            {
                bestDepth = depth;
                best = bones[i];
            }
        }
        return best;
    }

    static int Depth(Transform t)
    {
        int depth = 0;
        while (t.parent != null)
        {
            depth++;
            t = t.parent;
        }
        return depth;
    }

    static bool IsDetail(string name)
    {
        name = name.ToLowerInvariant();
        return name.Contains("twist") || name.Contains("ik") || name.Contains("tip") ||
               name.Contains("finger") || name.Contains("thumb") || name.Contains("toe") ||
               name.Contains("eye") || name.Contains("jaw") || name.Contains("hair");
    }

    static bool IsTorso(string name)
    {
        name = name.ToLowerInvariant();
        return name.Contains("hips") || name.Contains("pelvis") || name.Contains("spine") ||
               name.Contains("chest") || name.Contains("head") || name.Contains("neck");
    }

    void Update()
    {
        if (_released || _life <= 0f)
            return;

        _life -= Time.deltaTime;
        if (_life < 1f && !_released)
        {
            _released = true;
            Freeze();
        }
        if (_life <= 0f)
            Destroy(gameObject);
    }

    /// <summary>
    /// Заморозка замість вимкнення колайдерів: тіло лишається на місці й не
    /// провалюється крізь підлогу, коли з'єднання втрачають опору.
    /// </summary>
    void Freeze()
    {
        for (int i = 0; i < _bodies.Count; i++)
        {
            var body = _bodies[i];
            if (body == null)
                continue;

            var joints = body.GetComponents<CharacterJoint>();
            for (int j = 0; j < joints.Length; j++)
                joints[j].connectedBody = null;

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;

            var collider = body.GetComponent<Collider>();
            if (collider != null)
                collider.isTrigger = true;

            body.Sleep();
        }
    }

    void OnDestroy()
    {
        if (_active > 0)
            _active--;
    }
}
