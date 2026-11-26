using UnityEngine;

/// <summary>
/// Явні пози для всіх view model'ів відносно камери.
/// +Z = вперед, +Y = вгору, +X = вправо.
/// Для імпортованих FBX ModelEuler компенсує власну орієнтацію моделі.
/// </summary>
public struct ViewModelPose
{
    public Vector3 Position;
    public Vector3 Euler;
    public float Scale;
    public Vector3 Muzzle;

    public Vector3 ModelEuler;
    public Vector3 ModelOffset;

    public ViewModelPose(Vector3 position, Vector3 euler, Vector3 muzzle,
        Vector3 modelEuler = default(Vector3), Vector3 modelOffset = default(Vector3), float scale = 1f)
    {
        Position = position;
        Euler = euler;
        Scale = scale;
        Muzzle = muzzle;
        ModelEuler = modelEuler;
        ModelOffset = modelOffset;
    }
}

public static class ViewModelPoseCatalog
{
    // SMG рознесені до протилежних країв екрана: один у правій руці,
    // другий — дзеркальний у лівій. Значення підібране так, щоб модель
    // потрапляла приблизно на 0.2 та 0.8 ширини екрана.
    public const float DualSmgGripSeparation = 0.8f;

    public static ViewModelPose Get(WeaponId id)
    {
        switch (id)
        {
            // Quaternius (FBX): довга вісь X, дуло на -X, руків'я вниз.
            // Тому +90° навколо Y: -X -> +Z (вперед).
            case WeaponId.Revolver:
                return new ViewModelPose(
                    new Vector3(0.10f, -0.20f, 0.62f), new Vector3(1f, -8f, 1f),
                    new Vector3(0f, 0.08f, 0.30f), new Vector3(0f, 90f, 0f),
                    new Vector3(0f, -0.02f, 0f));
            case WeaponId.Shotgun:
                return new ViewModelPose(
                    new Vector3(0.18f, -0.20f, 0.64f), new Vector3(1f, -6f, 0.5f),
                    new Vector3(0f, 0.0f, 0.43f), new Vector3(0f, 90f, 0f),
                    new Vector3(0f, -0.01f, 0f), 0.8f);
            case WeaponId.DualSmgs:
                // По одному SMG у кожній руці: корінь по центру, копії рознесені
                // вліво/вправо, ліва дзеркальна.
                return new ViewModelPose(
                    new Vector3(0.12f, -0.26f, 0.78f), new Vector3(0f, -8f, 1f),
                    new Vector3(0f, 0.12f, 0.30f), new Vector3(0f, 90f, 0f),
                    new Vector3(0f, -0.02f, 0.06f), 0.75f);

            // Kenney blaster kit: довга вісь Z, дуло на -Z, руків'я на +Z.
            // Тому 180° навколо Y: -Z -> +Z (вперед).
            case WeaponId.Batgun:
                return new ViewModelPose(
                    new Vector3(0.22f, -0.18f, 0.62f), new Vector3(1f, -7f, 1f),
                    new Vector3(0f, 0.01f, 0.38f), new Vector3(0f, 180f, 0f),
                    new Vector3(0f, -0.02f, 0f), 0.7f);
            case WeaponId.FlameCannon:
                return new ViewModelPose(
                    new Vector3(0.27f, -0.25f, 0.60f), new Vector3(0f, -6f, 1f),
                    new Vector3(0f, 0.02f, 0.46f), new Vector3(0f, 180f, 0f),
                    new Vector3(0f, -0.02f, 0f), 0.6f);
            case WeaponId.GrenadeLauncher:
                return new ViewModelPose(
                    new Vector3(0.28f, -0.24f, 0.58f), new Vector3(0f, -7f, 1f),
                    new Vector3(0f, 0.02f, 0.42f), new Vector3(0f, 180f, 0f),
                    new Vector3(0f, -0.02f, 0f), 0.5f);
            case WeaponId.WateringGun:
                return new ViewModelPose(
                    new Vector3(0.20f, -0.20f, 0.64f), new Vector3(0f, -7f, 1f),
                    new Vector3(0f, 0.09f, 0.40f), new Vector3(0f, 180f, 0f),
                    new Vector3(0f, -0.02f, 0f), 0.7f);

            // Процедурні моделі: чиста геометрія з віссю +Z.
            case WeaponId.Crossbow:
                return new ViewModelPose(
                    new Vector3(0.10f, -0.16f, 0.78f), new Vector3(0f, -6f, 0f),
                    new Vector3(0f, 0.075f, 0.42f), new Vector3(0f, 180f, 0f),
                    new Vector3(0f, -0.02f, 0f), 0.75f);
            case WeaponId.Staff:
                return new ViewModelPose(
                    new Vector3(0.12f, -0.16f, 0.72f), new Vector3(0f, -5f, 0f),
                    new Vector3(0f, 0f, 0.46f), new Vector3(0f, 180f, 0f),
                    new Vector3(0f, -0.02f, 0f), 0.8f);
            case WeaponId.SalvoKnife:
                return new ViewModelPose(
                    new Vector3(0.27f, -0.13f, 0.70f), new Vector3(0f, -8f, 2f),
                    new Vector3(0f, 0f, 0.46f), default(Vector3), default(Vector3), 0.62f);
            case WeaponId.CycloneSword:
                // Kryven Blade: клинок уздовж +Y locally. +90° по X → +Z (вперед).
                return new ViewModelPose(
                    new Vector3(0.23f, -0.09f, 0.62f), new Vector3(0f, -8f, 2f),
                    new Vector3(0f, 0f, 0.56f), new Vector3(90f, 0f, 0f),
                    new Vector3(0f, -0.02f, 0.04f), 0.95f);
            default:
                return new ViewModelPose(
                    new Vector3(0.30f, -0.27f, 0.55f), new Vector3(1f, -7f, 1f),
                    new Vector3(0f, 0.02f, 0.39f));
        }
    }

    /// <summary>
    /// Політика рук: легка зброя — одна рука, важка — дві, SMG — по руці на ствол.
    /// </summary>
    public static bool UsesLeftHand(WeaponId id)
    {
        switch (id)
        {
            case WeaponId.Shotgun:
            case WeaponId.Crossbow:
            case WeaponId.FlameCannon:
            case WeaponId.GrenadeLauncher:
            case WeaponId.WateringGun:
            case WeaponId.DualSmgs:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Локальна корекція рук після загального збільшення зброї в 1.25×.
    /// Значення 1f = руки масштабуються разом зі зброєю; менше — трохи менші.
    /// </summary>
    public static float HandScale(WeaponId id)
    {
        switch (id)
        {
            case WeaponId.DualSmgs:
            case WeaponId.GrenadeLauncher:
            case WeaponId.SalvoKnife:
            case WeaponId.CycloneSword:
                return 0.92f;
            case WeaponId.Shotgun:
            case WeaponId.FlameCannon:
            case WeaponId.Crossbow:
            case WeaponId.WateringGun:
                return 0.96f;
            default:
                return 1f;
        }
    }

    /// <summary>
    /// Точки, де реальні CC0-руки тримають зброю, у локальному просторі view model'у.
    /// Використовуються і для імпортованих FBX-моделей.
    /// </summary>
    public static void Grips(WeaponId id, out Vector3 right, out Vector3 left)
    {
        switch (id)
        {
            case WeaponId.DualSmgs:
                right = new Vector3(DualSmgGripSeparation, -0.13f, -0.02f);
                left = new Vector3(-DualSmgGripSeparation, -0.13f, -0.02f);
                break; // по руці на дзеркальний ствол
            case WeaponId.Shotgun:
                right = new Vector3(0.02f, -0.13f, -0.05f);
                left = new Vector3(-0.01f, -0.08f, 0.19f);
                break;
            case WeaponId.Crossbow:
                right = new Vector3(0.02f, -0.13f, -0.02f);
                left = new Vector3(-0.01f, -0.08f, 0.16f);
                break;
            case WeaponId.FlameCannon:
                right = new Vector3(0.02f, -0.14f, -0.05f);
                left = new Vector3(-0.02f, -0.09f, 0.20f);
                break;
            case WeaponId.GrenadeLauncher:
                right = new Vector3(0.02f, -0.14f, -0.05f);
                left = new Vector3(-0.02f, -0.08f, 0.18f);
                break;
            case WeaponId.WateringGun:
                right = new Vector3(0.02f, -0.13f, -0.05f);
                left = new Vector3(-0.02f, -0.08f, 0.18f);
                break;
            case WeaponId.Staff:
                right = new Vector3(0.01f, -0.17f, 0.02f);
                left = new Vector3(0.01f, 0.02f, 0.18f);
                break;
            case WeaponId.CycloneSword:
                right = new Vector3(0.0f, -0.14f, 0.0f);
                left = new Vector3(-0.02f, -0.04f, 0.08f);
                break;
            case WeaponId.SalvoKnife:
                right = new Vector3(0.02f, -0.12f, 0.0f);
                left = new Vector3(-0.02f, -0.07f, 0.08f);
                break;
            default:
                right = new Vector3(0.02f, -0.13f, -0.03f);
                left = new Vector3(-0.02f, -0.10f, 0.15f);
                break;
        }
    }
}
