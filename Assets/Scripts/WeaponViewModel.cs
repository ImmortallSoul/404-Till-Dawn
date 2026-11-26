using System.Collections.Generic;
using UnityEngine;

public enum WeaponSurface
{
    Metal,
    Wood,
    Leather,
    Fabric,
    Gold,
    Glow
}

/// <summary>
/// Відображає вибрану зброю від першої особи.
/// Спершу пробує готові CC0-моделі; якщо асета немає — збирає детальну модель з примітивів.
/// Колесо миші або 1–4 вибирає активний ствол; картка нової зброї замінює саме його.
/// </summary>
public class WeaponViewModel : MonoBehaviour
{
    const float PitchInfluence = 0.75f;
    // Усі стволи збільшені на 25%; руки масштабуються локально через HandScale().
    const float WeaponScale = 1.25f;

    static readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();
    static readonly Dictionary<string, Material> _vfxMaterials = new Dictionary<string, Material>();

    static WeaponViewModel _instance;

    PlayerCombat _combat;
    Transform _modelRoot;
    Transform _muzzleAnchor;
    Transform _muzzleFlash;
    Transform _secondMuzzleAnchor;
    Transform _secondMuzzleFlash;
    Transform _animatedPart;
    Transform _spinPart;
    Transform _handsRoot;
    Transform _rightHand;
    Transform _leftHand;
    Transform _castOrb;
    Transform _castRing;
    Transform _flameStream;
    Vector3 _rightHandRestPos;
    Vector3 _leftHandRestPos;
    Quaternion _rightHandRestRot;
    Quaternion _leftHandRestRot;
    readonly List<Transform> _fingers = new List<Transform>();
    readonly List<Quaternion> _fingerRestRots = new List<Quaternion>();
    readonly List<Transform> _flameParticles = new List<Transform>();
    readonly List<Vector3> _flameParticleRest = new List<Vector3>();
    readonly List<float> _flamePhase = new List<float>();
    readonly List<float> _flameParticleScale = new List<float>();
    Vector3 _partRestPos;
    Quaternion _partRestRot;
    int _partAnimMode;
    int _shotCounter;
    float _swing;
    int _swingSign;
    readonly List<Transform> _flyKnives = new List<Transform>();
    readonly List<Vector3> _flyKnifeRest = new List<Vector3>();

    WeaponId _currentId = (WeaponId)(-1);
    int _currentLevel = -1;

    Vector3 _basePos;
    Quaternion _baseRot;
    float _baseScale = 1f;
    float _handScale = 1f;
    Vector3 _lastWorldPos;

    float _kick;
    float _kickVelocity;
    float _reload;
    float _reloadProgress;
    float _walkPhase;
    float _equip;
    float _replaceFlash;
    float _flash;
    float _cast;
    float _swayX;
    float _swayY;
    float _walk;
    bool _wasReloading;

    readonly List<Transform> _levelParts = new List<Transform>();
    readonly List<Vector3> _levelPartScales = new List<Vector3>();

    public static WeaponViewModel Instance { get { return _instance; } }
    public int DisplayIndex { get { return _combat != null ? _combat.ActiveWeaponIndex : 0; } }
    public bool HasHands { get { return _rightHand != null || _leftHand != null; } }
    public float CastAmount { get { return _cast; } }

    public void NotifyWeaponReplaced()
    {
        _currentId = (WeaponId)(-1);
        _currentLevel = -1;
        _replaceFlash = 1f;
        _equip = 1f;
        _kickVelocity += 2.5f;
    }

    void Awake()
    {
        _instance = this;
        _lastWorldPos = transform.position;
    }

    public static void Attach(Transform camera, PlayerCombat combat)
    {
        if (_instance != null)
            Destroy(_instance.gameObject);

        var go = new GameObject("WeaponViewModel");
        go.transform.SetParent(camera, false);
        var vm = go.AddComponent<WeaponViewModel>();
        vm._combat = combat;
    }

    public void NotifyFired(float strength = 1f)
    {
        _shotCounter++;
        _kickVelocity += 5.2f * Mathf.Clamp(strength, 0.2f, 2f);
        _flash = Mathf.Min(1f, _flash + 0.9f);
        if (_currentId == WeaponId.Staff)
            _cast = 1f;
        if (_currentId == WeaponId.CycloneSword || _currentId == WeaponId.SalvoKnife)
        {
            // Кожен удар — окремий замах у протилежний бік, а не стала vibrating-поза.
            _swingSign = -_swingSign;
            _swing = 1f;
        }
    }

    void Update()
    {
        if (_combat == null || _combat.Weapons.Count == 0)
            return;

        bool playing = GameSession.Instance == null || GameSession.Instance.AllowsGameplay;
        float dt = playing ? Time.deltaTime : 0f;

        if (playing)
            HandleSelectionInput();

        SyncWeapon();
        Animate(dt, playing);
    }

    void LateUpdate()
    {
        // Зброя частково стабілізується, коли гравець дивиться вгору/вниз.
        // Обертаємо саму модель, а не якір: інакше зброя «їздить» по екрану.
        if (_modelRoot == null)
            return;

        var fps = _combat != null ? _combat.GetComponent<FirstPersonController>() : null;
        float pitch = fps != null ? fps.Pitch : 0f;
        _modelRoot.localRotation *= Quaternion.Euler(-pitch * (1f - PitchInfluence), 0f, 0f);
    }

    void HandleSelectionInput()
    {
        int count = _combat.Weapons.Count;
        if (count <= 1)
            return;

        int index = _combat.ActiveWeaponIndex;
        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0.01f)
            index = (index + 1) % count;
        else if (scroll < -0.01f)
            index = (index - 1 + count) % count;

        KeyCode[] keys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4 };
        for (int i = 0; i < keys.Length && i < count; i++)
        {
            if (Input.GetKeyDown(keys[i]))
                index = i;
        }

        if (index != _combat.ActiveWeaponIndex)
            _equip = 1f;
        _combat.ActiveWeaponIndex = index;
    }

    void SyncWeapon()
    {
        int count = _combat.Weapons.Count;
        if (count == 0)
            return;

        _combat.ActiveWeaponIndex = Mathf.Clamp(_combat.ActiveWeaponIndex, 0, count - 1);
        var weapon = _combat.Weapons[_combat.ActiveWeaponIndex];
        if (weapon.Definition.Id != _currentId || weapon.Level != _currentLevel)
            BuildModel(weapon);

        if (weapon.IsReloading && !_wasReloading)
        {
            _reload = 1f;
            AudioManager.Instance?.Play("reload_start", 0.6f, Random.Range(0.95f, 1.05f), 0.1f);
        }
        else if (!weapon.IsReloading && _wasReloading)
        {
            AudioManager.Instance?.Play("reload_end", 0.65f, Random.Range(0.95f, 1.05f), 0.1f);
        }
        _wasReloading = weapon.IsReloading;
        _reloadProgress = weapon.IsReloading ? weapon.ReloadProgress : 0f;
    }

    void BuildModel(WeaponInstance weapon)
    {
        if (_modelRoot != null)
            Destroy(_modelRoot.gameObject);

        _currentId = weapon.Definition.Id;
        _currentLevel = weapon.Level;
        _spinPart = null;
        _animatedPart = null;
        _muzzleAnchor = null;
        _muzzleFlash = null;
        _secondMuzzleAnchor = null;
        _secondMuzzleFlash = null;
        _handsRoot = null;
        _rightHand = null;
        _leftHand = null;
        _castOrb = null;
        _castRing = null;
        _flameStream = null;
        _flyKnives.Clear();
        _flyKnifeRest.Clear();
        _swing = 0f;
        _swingSign = 1;
        _levelParts.Clear();
        _levelPartScales.Clear();
        _fingers.Clear();
        _fingerRestRots.Clear();
        _flameParticles.Clear();
        _flameParticleRest.Clear();
        _flamePhase.Clear();
        _flameParticleScale.Clear();
        _kick = 0f;
        _kickVelocity = 0f;
        _reload = 0f;
        _reloadProgress = 0f;
        _equip = 1f;
        _cast = 0f;

        ViewModelPose pose = ViewModelPoseCatalog.Get(_currentId);
        _modelRoot = new GameObject("Model_" + _currentId).transform;
        _modelRoot.SetParent(transform, false);
        _basePos = pose.Position;
        _baseRot = Quaternion.Euler(pose.Euler);
        _baseScale = pose.Scale * WeaponScale;
        _handScale = ViewModelPoseCatalog.HandScale(_currentId);
        _modelRoot.localPosition = _basePos;
        _modelRoot.localRotation = _baseRot;
        _modelRoot.localScale = Vector3.one * _baseScale;

        var imported = ViewModelAssetAdapter.Create(_currentId, _modelRoot);
        if (imported != null)
        {
            _muzzleAnchor = imported.Muzzle;
            _secondMuzzleAnchor = imported.SecondMuzzle;
            // Kryven Blade зберігає власні руни, але циклонічне кільце лишається
            // окремим ефектом і працює однаково для будь-якої моделі меча.
            if (_currentId == WeaponId.CycloneSword)
                BuildBladeRing();
        }
        else
        {
            switch (_currentId)
            {
                case WeaponId.Revolver: BuildRevolver(); break;
                case WeaponId.Shotgun: BuildShotgun(); break;
                case WeaponId.Crossbow: BuildCrossbow(); break;
                case WeaponId.DualSmgs: BuildDualSmgs(); break;
                case WeaponId.Batgun: BuildBatgun(); break;
                case WeaponId.FlameCannon: BuildFlameCannon(); break;
                case WeaponId.GrenadeLauncher: BuildGrenadeLauncher(); break;
                case WeaponId.Staff: BuildStaff(); break;
                case WeaponId.SalvoKnife: BuildSalvoKnife(); break;
                case WeaponId.WateringGun: BuildWateringGun(); break;
                case WeaponId.CycloneSword: BuildCycloneSword(); break;
            }
            if (_muzzleAnchor == null)
                SetMuzzleAnchor(pose.Muzzle);
        }

        BuildMuzzleFlash();
        if (_currentId == WeaponId.FlameCannon)
            BuildFlameStream();
        BuildHands(_currentId);
        if (_currentId == WeaponId.Staff)
            BuildCastOrb(pose.Muzzle);
        ApplyLevelCosmetics();
    }

    void Animate(float dt, bool playing)
    {
        if (_modelRoot == null)
            return;

        // Пружинний recoil замість лінійного затухання.
        _kickVelocity += -_kick * 92f * dt;
        _kickVelocity *= Mathf.Exp(-13f * dt);
        _kick += _kickVelocity * dt;
        if (_kick < 0f)
        {
            _kick = 0f;
            if (_kickVelocity < 0f)
                _kickVelocity = 0f;
        }
        _kick = Mathf.Min(1.5f, _kick);

        _reload = Mathf.MoveTowards(_reload, 0f, dt * 2.6f);
        _equip = Mathf.MoveTowards(_equip, 0f, dt * 3.1f);
        _replaceFlash = Mathf.MoveTowards(_replaceFlash, 0f, dt * 2f);
        _flash = Mathf.MoveTowards(_flash, 0f, dt * 9f);
        _cast = Mathf.MoveTowards(_cast, 0f, dt * 3.4f);
        _swing = Mathf.MoveTowards(_swing, 0f, dt * 5.2f);

        Vector3 world = transform.position;
        float speed = 0f;
        if (dt > 0f)
        {
            Vector3 a = _lastWorldPos;
            Vector3 b = world;
            a.y = 0f;
            b.y = 0f;
            speed = Vector3.Distance(a, b) / dt;
        }
        _lastWorldPos = world;
        _walk = Mathf.MoveTowards(_walk, Mathf.Clamp01(speed / 8f), dt * 4f);
        _walkPhase += speed * dt * 1.65f;

        float mouseX = dt > 0f ? Input.GetAxis("Mouse X") : 0f;
        float mouseY = dt > 0f ? Input.GetAxis("Mouse Y") : 0f;
        _swayX = Mathf.Lerp(_swayX, Mathf.Clamp(mouseX * 0.035f, -0.045f, 0.045f), Mathf.Clamp01(dt * 9f));
        _swayY = Mathf.Lerp(_swayY, Mathf.Clamp(-mouseY * 0.028f, -0.04f, 0.04f), Mathf.Clamp01(dt * 9f));

        //Reload has a clear dip-and-return arc instead of a single decaying offset.
        float reloadArc = _reloadProgress > 0f
            ? Mathf.Sin(Mathf.Clamp01(_reloadProgress) * Mathf.PI)
            : 0f;
        bool spinWeapon = _combat.ActiveWeapon != null && _combat.ActiveWeapon.Definition.Behavior == WeaponBehavior.Spin;
        float spinTwist = spinWeapon ? _reloadProgress * 300f : 0f;
        float castArc = Mathf.Sin(Mathf.Clamp01(_cast) * Mathf.PI);
        float swingArc = Mathf.Sin(Mathf.Clamp01(_swing) * Mathf.PI) * _swingSign;

        Vector3 pos = _basePos;
        pos += Vector3.back * (_kick * 0.14f + reloadArc * 0.05f);
        pos += Vector3.down * (_reload * 0.08f + reloadArc * 0.17f + _equip * 0.34f);
        pos += Vector3.up * (castArc * 0.04f) + Vector3.forward * (castArc * 0.025f);
        pos.x += swingArc * 0.11f;
        pos.y += swingArc * 0.035f;
        pos.x += _swayX + Mathf.Sin(_walkPhase) * _walk * 0.021f + Mathf.Sin(Time.time * 1.7f) * 0.003f;
        pos.y += _swayY + Mathf.Abs(Mathf.Sin(_walkPhase)) * _walk * 0.022f;
        pos.z += Mathf.Cos(_walkPhase * 0.5f) * _walk * 0.012f;

        Quaternion rot = _baseRot * Quaternion.Euler(
            -_kick * 18f + _reload * 18f + reloadArc * 26f + _equip * 44f - castArc * 16f,
            _kick * 6f + _swayX * 60f + Mathf.Sin(_walkPhase) * _walk * 1.6f + spinTwist + castArc * 10f + swingArc * 52f,
            _reload * 10f + reloadArc * 22f + _equip * 18f + Mathf.Sin(_walkPhase) * _walk * 2.4f - castArc * 12f + swingArc * 38f);

        _modelRoot.localPosition = pos;
        _modelRoot.localRotation = rot;

        UpdateFlash(_muzzleFlash);
        // Два SMG мають дві незалежні точки дула: спалахує поперемінно.
        if (_secondMuzzleFlash != null)
        {
            bool secondActive = _currentId == WeaponId.DualSmgs && _shotCounter % 2 == 1;
            if (secondActive)
            {
                UpdateFlash(_secondMuzzleFlash);
                if (_muzzleFlash != null)
                    _muzzleFlash.gameObject.SetActive(false);
            }
            else
            {
                _secondMuzzleFlash.gameObject.SetActive(false);
                UpdateFlash(_muzzleFlash);
            }
        }

        if (_castOrb != null)
        {
            float pulse = 0.85f + castArc * 0.9f + _flash * 0.25f;
            _castOrb.localScale = Vector3.one * (0.075f * pulse);
            if (_castRing != null)
            {
                _castRing.localScale = Vector3.one * (0.9f + castArc * 0.55f);
                if (dt > 0f)
                    _castRing.Rotate(0f, 0f, 160f * dt, Space.Self);
            }
            if (dt > 0f)
                _castOrb.Rotate(0f, 220f * dt, 0f, Space.Self);
        }

        if (_flameStream != null)
        {
            bool firing = playing && _currentId == WeaponId.FlameCannon &&
                          _combat != null && _combat.IsFiring;
            _flameStream.gameObject.SetActive(firing);
            if (firing)
            {
                float flicker = 0.92f + Mathf.Sin(Time.time * 34f) * 0.11f;
                _flameStream.localScale = new Vector3(flicker, flicker,
                    0.96f + Mathf.Sin(Time.time * 21f) * 0.1f);

                if (dt > 0f)
                {
                    int count = Mathf.Min(_flameParticles.Count,
                        Mathf.Min(_flameParticleRest.Count, _flameParticleScale.Count));
                    for (int i = 0; i < count; i++)
                    {
                        var particle = _flameParticles[i];
                        if (particle == null)
                            continue;

                        // Суцільна цівка: ядро йде від білого біля сопла до оранжевого,
                        // а кінчики червоніють і розсіюються на відстані ~2.4 м.
                        float phase = Mathf.Repeat(Time.time * 2.35f + _flamePhase[i], 1f);
                        Vector3 p = _flameParticleRest[i];
                        float spread = 0.35f + phase * 1.4f;
                        p.x *= spread;
                        p.y *= spread;
                        p.z = 0.08f + phase * 2.35f;
                        particle.localPosition = p;

                        float taper = Mathf.Sin(phase * Mathf.PI);
                        float size = _flameParticleScale[i] *
                                     Mathf.Lerp(0.85f, 0.28f, phase) *
                                     (0.6f + 0.4f * taper);
                        particle.localScale = Vector3.one * size;
                        particle.localRotation = Quaternion.Euler(0f, 0f,
                            _flamePhase[i] * 57f + Time.time * 150f + phase * 90f);
                        var renderer = particle.GetComponent<MeshRenderer>();
                        if (renderer != null && renderer.sharedMaterial != null)
                        {
                            Color core = new Color(1f, 0.96f, 0.82f, 0.95f);
                            Color mid = new Color(1f, 0.52f, 0.12f, 0.8f);
                            Color tip = new Color(0.85f, 0.12f, 0.04f, 0.32f);
                            renderer.sharedMaterial.color = phase < 0.3f
                                ? Color.Lerp(core, mid, phase / 0.3f)
                                : Color.Lerp(mid, tip, (phase - 0.3f) / 0.7f);
                        }
                    }
                }
            }
        }

        AnimateHands(reloadArc, castArc);

        if (_spinPart != null && dt > 0f)
            _spinPart.Rotate(0f, 0f, (460f + _swing * 1080f) * dt, Space.Self);

        // Ножі вилітають вперед на кожному кидку й повертаються на місце.
        for (int i = 0; i < _flyKnives.Count && i < _flyKnifeRest.Count; i++)
        {
            var knife = _flyKnives[i];
            if (knife == null)
                continue;
            Vector3 rest = _flyKnifeRest[i];
            knife.localPosition = rest + Vector3.forward * (0.24f * Mathf.Sin(_swing * Mathf.PI)) +
                                  Vector3.right * (Mathf.Sign(rest.x) * 0.1f * Mathf.Sin(_swing * Mathf.PI));
            knife.localRotation = Quaternion.Euler(0f, Mathf.Sign(rest.x) * 14f,
                Mathf.Sign(rest.x) * 8f + _swing * 720f * _swingSign);
        }

        if (_animatedPart != null)
            AnimatePart();

        if (_replaceFlash > 0f)
        {
            float pulse = 1f + _replaceFlash * 0.06f;
            _modelRoot.localScale = Vector3.one * (_baseScale * pulse);
        }
        else
        {
            _modelRoot.localScale = Vector3.one * _baseScale;
        }

        for (int i = 0; i < _levelParts.Count; i++)
        {
            var part = _levelParts[i];
            if (part == null)
                continue;
            float s = 1f + Mathf.Sin(Time.time * 4f + i * 0.7f) * 0.08f;
            part.localScale = _levelPartScales[i] * s;
        }
    }

    void AnimatePart()
    {
        Vector3 p = _partRestPos;
        Quaternion r = _partRestRot;

        switch (_partAnimMode)
        {
            case 1: // барабан
                r = _partRestRot * Quaternion.Euler(0f, 0f, _reload * 220f);
                break;
            case 2: // помпа
                p += Vector3.back * (_reload * 0.16f);
                break;
            case 3: // магазин
                p += Vector3.down * (_reload * 0.17f);
                r = _partRestRot * Quaternion.Euler(_reload * 14f, 0f, 0f);
                break;
            case 4: // трос/натяг
                p += Vector3.back * (_reload * 0.09f);
                break;
            case 5: // барабан гранат
                r = _partRestRot * Quaternion.Euler(0f, 0f, _reload * 160f);
                break;
        }

        _animatedPart.localPosition = p;
        _animatedPart.localRotation = r;
    }

    void ApplyLevelCosmetics()
    {
        if (_currentLevel <= 1)
            return;

        int count = Mathf.Min(4, _currentLevel - 1);
        Color gold = new Color(0.95f, 0.68f, 0.2f);
        Color glow = LevelGlow(_currentId);

        for (int i = 0; i < count; i++)
        {
            float a = i * (360f / count) * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Cos(a) * 0.085f, Mathf.Sin(a) * 0.085f, 0.1f + i * 0.035f);
            var rune = Part(PrimitiveType.Cube, "LevelRune", p, new Vector3(0.026f, 0.026f, 0.05f),
                i % 2 == 0 ? gold : glow, glow * 0.7f, Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg),
                _modelRoot, i % 2 == 0 ? WeaponSurface.Gold : WeaponSurface.Glow);
            AddLevelPart(rune);
        }

        MeshPart(RuntimeMeshLibrary.Ring(0.11f, 0.125f, 24), "LevelRing", new Vector3(0f, 0f, 0.24f),
            Vector3.one, glow, glow * 0.9f, Quaternion.Euler(90f, 0f, 0f), _modelRoot, WeaponSurface.Glow);
    }

    static Color LevelGlow(WeaponId id)
    {
        switch (id)
        {
            case WeaponId.FlameCannon: return new Color(1f, 0.42f, 0.1f);
            case WeaponId.Batgun: return new Color(0.95f, 0.3f, 1f);
            case WeaponId.WateringGun: return new Color(0.4f, 1f, 0.45f);
            case WeaponId.Crossbow: return new Color(0.35f, 1f, 0.5f);
            case WeaponId.Staff: return new Color(0.4f, 0.7f, 1f);
            case WeaponId.GrenadeLauncher: return new Color(0.6f, 1f, 0.25f);
            default: return new Color(0.45f, 0.8f, 1f);
        }
    }

    void SetMuzzleAnchor(Vector3 localPosition)
    {
        _muzzleAnchor = new GameObject("MuzzleAnchor").transform;
        _muzzleAnchor.SetParent(_modelRoot, false);
        _muzzleAnchor.localPosition = localPosition;
        _muzzleAnchor.localRotation = Quaternion.identity;
    }

    void BuildMuzzleFlash()
    {
        if (_muzzleAnchor == null)
            SetMuzzleAnchor(new Vector3(0f, 0.015f, 0.39f));

        _muzzleFlash = CreateFlash(_muzzleAnchor);
        if (_secondMuzzleAnchor != null)
            _secondMuzzleFlash = CreateFlash(_secondMuzzleAnchor);
    }

    void UpdateFlash(Transform flash)
    {
        if (flash == null)
            return;
        flash.gameObject.SetActive(_flash > 0.04f);
        if (_flash > 0.04f)
            flash.localScale = Vector3.one * (0.65f + _flash * 0.75f);
    }

    Transform CreateFlash(Transform anchor)
    {
        var flash = new GameObject("MuzzleFlash").transform;
        flash.SetParent(anchor, false);
        flash.localPosition = Vector3.zero;
        flash.localRotation = Quaternion.identity;

        Color flame = new Color(1f, 0.75f, 0.25f);
        Part(PrimitiveType.Sphere, "Core", Vector3.zero, Vector3.one * 0.1f,
            flame, flame * 1.6f, Quaternion.identity, flash, WeaponSurface.Glow);
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Cos(a) * 0.055f, Mathf.Sin(a) * 0.055f, 0.01f);
            Part(PrimitiveType.Cube, "Tongue", p, new Vector3(0.035f, 0.035f, 0.13f),
                flame, flame * 1.4f, Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg), flash, WeaponSurface.Glow);
        }
        flash.gameObject.SetActive(false);
        return flash;
    }

    // ---------- Детальні процедурні моделі (fallback) ----------

    void BuildRevolver()
    {
        Color steel = new Color(0.24f, 0.27f, 0.32f);
        Color dark = new Color(0.08f, 0.09f, 0.12f);
        Color wood = new Color(0.4f, 0.21f, 0.09f);
        Color brass = new Color(0.9f, 0.65f, 0.2f);

        Part(PrimitiveType.Cube, "Frame", Vector3.zero, new Vector3(0.09f, 0.1f, 0.23f), steel, steel * 0.1f, Quaternion.identity, null, WeaponSurface.Metal);
        Part(PrimitiveType.Cube, "TopStrap", new Vector3(0f, 0.058f, 0.12f), new Vector3(0.07f, 0.025f, 0.26f), steel, steel * 0.1f, Quaternion.identity, null, WeaponSurface.Metal);

        var drum = Part(PrimitiveType.Cylinder, "Drum", new Vector3(0f, 0f, 0.025f), new Vector3(0.14f, 0.05f, 0.14f),
            dark, brass * 0.2f, Quaternion.Euler(90f, 0f, 0f), null, WeaponSurface.Metal);
        MarkAnimated(drum, 1);

        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Mathf.Deg2Rad;
            Part(PrimitiveType.Cylinder, "Chamber", new Vector3(Mathf.Cos(a) * 0.046f, Mathf.Sin(a) * 0.046f, 0.025f),
                new Vector3(0.025f, 0.052f, 0.025f), brass, brass * 0.2f, Quaternion.Euler(90f, 0f, 0f), drum, WeaponSurface.Gold);
        }

        MeshPart(RuntimeMeshLibrary.TaperedCylinder(0.026f, 0.034f, 0.26f), "Barrel",
            new Vector3(0f, 0.026f, 0.2f), Vector3.one, steel, steel * 0.12f,
            Quaternion.Euler(90f, 0f, 0f), null, WeaponSurface.Metal);
        Part(PrimitiveType.Cylinder, "MuzzleRing", new Vector3(0f, 0.026f, 0.335f), new Vector3(0.068f, 0.018f, 0.068f),
            brass, brass * 0.2f, Quaternion.Euler(90f, 0f, 0f), null, WeaponSurface.Gold);

        Part(PrimitiveType.Cube, "Grip", new Vector3(0f, -0.135f, -0.07f), new Vector3(0.08f, 0.2f, 0.09f),
            wood, wood * 0.1f, Quaternion.Euler(18f, 0f, 0f), null, WeaponSurface.Wood);
        for (int i = 0; i < 4; i++)
            Part(PrimitiveType.Cube, "GripGroove", new Vector3(0f, -0.1f - i * 0.035f, -0.02f),
                new Vector3(0.085f, 0.006f, 0.092f), dark, dark * 0.08f, Quaternion.Euler(18f, 0f, 0f), null, WeaponSurface.Leather);

        MeshPart(RuntimeMeshLibrary.Ring(0.065f, 0.075f, 16), "TriggerGuard", new Vector3(0f, -0.075f, 0.02f),
            Vector3.one * 0.6f, steel, steel * 0.1f, Quaternion.identity, null, WeaponSurface.Metal);
        Part(PrimitiveType.Cube, "Trigger", new Vector3(0f, -0.055f, 0.025f), new Vector3(0.012f, 0.045f, 0.014f),
            brass, brass * 0.15f, Quaternion.Euler(-12f, 0f, 0f), null, WeaponSurface.Gold);
        Part(PrimitiveType.Cube, "Hammer", new Vector3(0f, 0.07f, -0.09f), new Vector3(0.02f, 0.055f, 0.035f),
            dark, dark * 0.1f, Quaternion.Euler(-18f, 0f, 0f), null, WeaponSurface.Metal);
        Part(PrimitiveType.Cube, "Sight", new Vector3(0f, 0.085f, 0.29f), new Vector3(0.014f, 0.02f, 0.025f),
            brass, brass, Quaternion.identity, null, WeaponSurface.Gold);

        SetMuzzleAnchor(new Vector3(0f, 0.026f, 0.35f));
    }

    void BuildShotgun()
    {
        Color steel = new Color(0.22f, 0.24f, 0.28f);
        Color wood = new Color(0.42f, 0.2f, 0.08f);
        Color gold = new Color(0.9f, 0.66f, 0.2f);

        Part(PrimitiveType.Cube, "Stock", new Vector3(0f, -0.045f, -0.16f), new Vector3(0.12f, 0.11f, 0.34f),
            wood, wood * 0.08f, Quaternion.Euler(-4f, 0f, 0f), null, WeaponSurface.Wood);
        Part(PrimitiveType.Cube, "Receiver", new Vector3(0f, 0f, 0.01f), new Vector3(0.13f, 0.12f, 0.22f),
            steel, steel * 0.12f, Quaternion.identity, null, WeaponSurface.Metal);

        for (int s = -1; s <= 1; s += 2)
        {
            MeshPart(RuntimeMeshLibrary.TaperedCylinder(0.029f, 0.034f, 0.5f), "Barrel",
                new Vector3(s * 0.037f, 0.032f, 0.24f), Vector3.one, steel, steel * 0.12f,
                Quaternion.Euler(90f, 0f, 0f), null, WeaponSurface.Metal);
            Part(PrimitiveType.Cylinder, "Muzzle", new Vector3(s * 0.037f, 0.032f, 0.49f), new Vector3(0.072f, 0.016f, 0.072f),
                steel, steel * 0.12f, Quaternion.Euler(90f, 0f, 0f), null, WeaponSurface.Metal);
        }

        var pump = Part(PrimitiveType.Cube, "Pump", new Vector3(0f, -0.06f, 0.25f), new Vector3(0.145f, 0.07f, 0.2f),
            wood, wood * 0.1f, Quaternion.identity, null, WeaponSurface.Wood);
        MarkAnimated(pump, 2);
        for (int i = 0; i < 5; i++)
            Part(PrimitiveType.Cube, "PumpRidge", new Vector3(0f, -0.098f, 0.17f + i * 0.04f),
                new Vector3(0.15f, 0.008f, 0.012f), wood, wood * 0.08f, Quaternion.identity, pump, WeaponSurface.Wood);

        MeshPart(RuntimeMeshLibrary.Ring(0.045f, 0.055f, 16), "TriggerGuard", new Vector3(0f, -0.085f, -0.01f),
            Vector3.one * 0.7f, steel, steel * 0.1f, Quaternion.identity, null, WeaponSurface.Metal);
        Part(PrimitiveType.Cube, "Bead", new Vector3(0f, 0.073f, 0.47f), new Vector3(0.018f, 0.022f, 0.03f),
            gold, gold, Quaternion.identity, null, WeaponSurface.Gold);
        Part(PrimitiveType.Cube, "Shell", new Vector3(0.075f, 0.02f, 0.02f), new Vector3(0.014f, 0.05f, 0.06f),
            gold, gold * 0.5f, Quaternion.identity, null, WeaponSurface.Gold);

        SetMuzzleAnchor(new Vector3(0f, 0.032f, 0.51f));
    }

    void BuildCrossbow()
    {
        Color wood = new Color(0.34f, 0.18f, 0.08f);
        Color steel = new Color(0.3f, 0.33f, 0.38f);
        Color glow = new Color(0.35f, 0.95f, 0.5f);

        Part(PrimitiveType.Cube, "Stock", new Vector3(0f, -0.03f, -0.13f), new Vector3(0.10f, 0.09f, 0.36f),
            wood, wood * 0.08f, Quaternion.identity, null, WeaponSurface.Wood);
        Part(PrimitiveType.Cube, "Rail", new Vector3(0f, 0.035f, 0.15f), new Vector3(0.055f, 0.035f, 0.40f),
            steel, steel * 0.15f, Quaternion.identity, null, WeaponSurface.Metal);
        Part(PrimitiveType.Cube, "Grip", new Vector3(0f, -0.14f, -0.17f), new Vector3(0.075f, 0.18f, 0.09f),
            wood, wood * 0.1f, Quaternion.Euler(12f, 0f, 0f), null, WeaponSurface.Wood);
        MeshPart(RuntimeMeshLibrary.Ring(0.05f, 0.062f, 16), "TriggerGuard", new Vector3(0f, -0.085f, -0.02f),
            Vector3.one * 0.65f, steel, steel * 0.1f, Quaternion.identity, null, WeaponSurface.Metal);
        Part(PrimitiveType.Cube, "Trigger", new Vector3(0f, -0.06f, 0f), new Vector3(0.012f, 0.045f, 0.014f),
            steel, steel * 0.15f, Quaternion.Euler(-10f, 0f, 0f), null, WeaponSurface.Metal);
        TubeZ("Scope", new Vector3(0f, 0.105f, 0.02f), 0.028f, 0.028f, 0.16f, steel, WeaponSurface.Metal, null, glow * 0.2f);

        Vector3 hub = new Vector3(0f, 0.045f, 0.31f);
        Vector3 leftTip = new Vector3(-0.29f, 0.045f, 0.38f);
        Vector3 rightTip = new Vector3(0.29f, 0.045f, 0.38f);
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 shoulder = new Vector3(s * 0.13f, 0.05f, 0.34f);
            Vector3 tip = s < 0 ? leftTip : rightTip;
            Rod("LimbRoot", hub, shoulder, 0.022f, steel, WeaponSurface.Metal);
            Rod("LimbTip", shoulder, tip, 0.016f, glow, WeaponSurface.Glow, null, glow * 0.8f);
            Part(PrimitiveType.Sphere, "Tip", tip, Vector3.one * 0.028f, glow, glow,
                Quaternion.identity, null, WeaponSurface.Glow);
        }

        var stringGroup = new GameObject("String").transform;
        stringGroup.SetParent(_modelRoot, false);
        Vector3 nock = new Vector3(0f, 0.045f, 0.15f);
        Rod("StringLeft", leftTip, nock, 0.006f, Color.white, WeaponSurface.Glow, stringGroup, glow);
        Rod("StringRight", rightTip, nock, 0.006f, Color.white, WeaponSurface.Glow, stringGroup, glow);
        MarkAnimated(stringGroup, 4);

        Rod("Bolt", new Vector3(0f, 0.075f, 0.12f), new Vector3(0f, 0.075f, 0.34f), 0.012f,
            steel, WeaponSurface.Metal);
        Blade("BoltTip", new Vector3(0f, 0.075f, 0.38f), 0.09f, 0.032f, 0.012f,
            steel, WeaponSurface.Metal, null, glow * 0.35f);
        for (int s = -1; s <= 1; s += 2)
            Part(PrimitiveType.Cube, "Fletching", new Vector3(s * 0.018f, 0.075f, 0.16f),
                new Vector3(0.028f, 0.006f, 0.06f), glow, glow * 0.5f,
                Quaternion.Euler(0f, 0f, s * 18f), null, WeaponSurface.Glow);
    }

    void BuildDualSmgs()
    {
        Color steel = new Color(0.2f, 0.23f, 0.28f);
        Color dark = new Color(0.07f, 0.08f, 0.1f);
        Color glow = new Color(0.4f, 0.75f, 1f);
        float sep = ViewModelPoseCatalog.DualSmgGripSeparation;

        for (int s = -1; s <= 1; s += 2)
        {
            // Кожен SMG живе у власній руці; лівий дзеркальний, а не просто
            // другий ствол поруч із правим.
            var holder = new GameObject(s > 0 ? "SMG_Right" : "SMG_Left").transform;
            holder.SetParent(_modelRoot, false);
            holder.localPosition = new Vector3(s * sep, 0f, 0f);
            holder.localScale = s > 0 ? Vector3.one : new Vector3(-1f, 1f, 1f);

            Part(PrimitiveType.Cube, "Body", new Vector3(0f, 0f, 0.02f), new Vector3(0.095f, 0.1f, 0.26f),
                steel, steel * 0.15f, Quaternion.identity, holder, WeaponSurface.Metal);
            Part(PrimitiveType.Cube, "Upper", new Vector3(0f, 0.06f, 0.02f), new Vector3(0.07f, 0.035f, 0.24f),
                dark, dark * 0.15f, Quaternion.identity, holder, WeaponSurface.Metal);

            MeshPart(RuntimeMeshLibrary.TaperedCylinder(0.022f, 0.027f, 0.2f), "Barrel",
                new Vector3(0f, 0.015f, 0.22f), Vector3.one, steel, steel * 0.15f,
                Quaternion.Euler(90f, 0f, 0f), holder, WeaponSurface.Metal);
            Part(PrimitiveType.Cylinder, "Suppressor", new Vector3(0f, 0.015f, 0.34f), new Vector3(0.07f, 0.07f, 0.07f),
                dark, glow * 0.12f, Quaternion.Euler(90f, 0f, 0f), holder, WeaponSurface.Metal);

            var mag = Part(PrimitiveType.Cube, "Mag", new Vector3(0f, -0.14f, -0.01f), new Vector3(0.058f, 0.19f, 0.075f),
                dark, dark * 0.2f, Quaternion.Euler(-8f, 0f, 0f), holder, WeaponSurface.Metal);
            if (s < 0)
                MarkAnimated(mag, 3);

            for (int i = 0; i < 3; i++)
                Part(PrimitiveType.Cube, "Vent", new Vector3(0f, 0.08f, 0.04f + i * 0.04f),
                    new Vector3(0.04f, 0.006f, 0.018f), dark, glow * 0.3f, Quaternion.identity, holder, WeaponSurface.Glow);

            Part(PrimitiveType.Cube, "Sight", new Vector3(0f, 0.09f, 0.16f), new Vector3(0.018f, 0.022f, 0.025f),
                glow, glow, Quaternion.identity, holder, WeaponSurface.Glow);
        }

        // Права рука стріляє першою, ліва (дзеркальна) — другою.
        SetMuzzleAnchor(new Vector3(sep, 0.015f, 0.39f));
        _secondMuzzleAnchor = new GameObject("MuzzleAnchor_L").transform;
        _secondMuzzleAnchor.SetParent(_modelRoot, false);
        _secondMuzzleAnchor.localPosition = new Vector3(-sep, 0.015f, 0.39f);
        _secondMuzzleAnchor.localRotation = Quaternion.identity;
    }

    void BuildBatgun()
    {
        Color body = new Color(0.3f, 0.11f, 0.38f);
        Color glow = new Color(0.95f, 0.32f, 1f);
        Color dark = new Color(0.06f, 0.04f, 0.09f);

        Part(PrimitiveType.Cube, "Body", Vector3.zero, new Vector3(0.13f, 0.12f, 0.28f),
            body, body * 0.3f, Quaternion.identity, null, WeaponSurface.Leather);
        MeshPart(RuntimeMeshLibrary.TaperedCylinder(0.05f, 0.065f, 0.28f), "Horn",
            new Vector3(0f, 0.01f, 0.24f), Vector3.one, body, glow * 0.2f,
            Quaternion.Euler(90f, 0f, 0f), null, WeaponSurface.Leather);

        for (int s = -1; s <= 1; s += 2)
        {
            for (int i = 0; i < 3; i++)
            {
                float t = i / 2f;
                Part(PrimitiveType.Cube, "Wing", new Vector3(s * (0.09f + t * 0.1f), 0.02f + t * 0.03f, 0.12f + t * 0.05f),
                    new Vector3(0.12f, 0.018f, 0.11f), body, glow * 0.45f,
                    Quaternion.Euler(0f, s * t * 16f, s * (24f - t * 10f)), null, WeaponSurface.Leather);
            }
            Part(PrimitiveType.Cube, "Ear", new Vector3(s * 0.06f, 0.12f, -0.07f), new Vector3(0.045f, 0.08f, 0.035f),
                body, glow * 0.25f, Quaternion.Euler(0f, 0f, s * 18f), null, WeaponSurface.Leather);
            Part(PrimitiveType.Sphere, "Eye", new Vector3(s * 0.045f, 0.04f, 0.15f), Vector3.one * 0.025f,
                glow, glow * 1.4f, Quaternion.identity, null, WeaponSurface.Glow);
        }

        Part(PrimitiveType.Sphere, "BatCore", new Vector3(0f, 0.01f, 0.37f), new Vector3(0.095f, 0.07f, 0.095f),
            glow, glow * 1.2f, Quaternion.identity, null, WeaponSurface.Glow);
        Part(PrimitiveType.Cube, "Handle", new Vector3(0f, -0.14f, -0.08f), new Vector3(0.075f, 0.18f, 0.085f),
            dark, glow * 0.12f, Quaternion.Euler(15f, 0f, 0f), null, WeaponSurface.Leather);

        SetMuzzleAnchor(new Vector3(0f, 0.01f, 0.42f));
    }

    void BuildFlameCannon()
    {
        Color metal = new Color(0.3f, 0.24f, 0.17f);
        Color orange = new Color(1f, 0.45f, 0.08f);
        Color red = new Color(0.9f, 0.14f, 0.06f);
        Color dark = new Color(0.08f, 0.07f, 0.06f);

        Part(PrimitiveType.Cylinder, "Tank", new Vector3(0f, 0.01f, -0.02f), new Vector3(0.2f, 0.2f, 0.2f),
            metal, orange * 0.15f, Quaternion.Euler(90f, 0f, 0f), null, WeaponSurface.Metal);
        Part(PrimitiveType.Cylinder, "TankBand", new Vector3(0f, 0.01f, -0.02f), new Vector3(0.215f, 0.03f, 0.215f),
            dark, dark * 0.1f, Quaternion.Euler(90f, 0f, 0f), null, WeaponSurface.Metal);
        Part(PrimitiveType.Sphere, "Fuel", new Vector3(0f, 0.14f, -0.08f), new Vector3(0.105f, 0.105f, 0.16f),
            red, red * 1f, Quaternion.identity, null, WeaponSurface.Glow);

        MeshPart(RuntimeMeshLibrary.TaperedCylinder(0.055f, 0.075f, 0.3f), "NozzleBody",
            new Vector3(0f, 0.015f, 0.27f), Vector3.one, metal, orange * 0.2f,
            Quaternion.Euler(90f, 0f, 0f), null, WeaponSurface.Metal);
        Part(PrimitiveType.Cylinder, "Nozzle", new Vector3(0f, 0.015f, 0.43f), new Vector3(0.15f, 0.045f, 0.15f),
            orange, orange * 1.3f, Quaternion.Euler(90f, 0f, 0f), null, WeaponSurface.Glow);

        for (int i = 0; i < 7; i++)
        {
            float a = i / 7f * Mathf.PI * 2f;
            Vector3 p = new Vector3(Mathf.Cos(a) * 0.09f, 0.01f + Mathf.Sin(a) * 0.09f, 0.16f + i * 0.024f);
            Part(PrimitiveType.Cylinder, "Hose", p, new Vector3(0.022f, 0.035f, 0.022f),
                dark, red * 0.15f, Quaternion.Euler(90f, 0f, 0f), null, WeaponSurface.Leather);
        }

        Part(PrimitiveType.Cube, "Handle", new Vector3(0f, -0.16f, -0.06f), new Vector3(0.08f, 0.19f, 0.09f),
            metal, orange * 0.1f, Quaternion.identity, null, WeaponSurface.Metal);
        Part(PrimitiveType.Cylinder, "Gauge", new Vector3(0.11f, 0.09f, -0.07f), new Vector3(0.07f, 0.02f, 0.07f),
            dark, orange * 0.7f, Quaternion.Euler(90f, 0f, 0f), null, WeaponSurface.Gold);

        SetMuzzleAnchor(new Vector3(0f, 0.015f, 0.47f));
    }

    void BuildGrenadeLauncher()
    {
        Color olive = new Color(0.26f, 0.3f, 0.16f);
        Color metal = new Color(0.17f, 0.18f, 0.19f);
        Color green = new Color(0.55f, 1f, 0.2f);

        Part(PrimitiveType.Cube, "Body", new Vector3(0f, 0f, -0.02f), new Vector3(0.13f, 0.12f, 0.28f),
            olive, olive * 0.15f, Quaternion.identity, null, WeaponSurface.Metal);
        MeshPart(RuntimeMeshLibrary.TaperedCylinder(0.075f, 0.085f, 0.32f), "Tube",
            new Vector3(0f, 0.025f, 0.23f), Vector3.one, metal, metal * 0.1f,
            Quaternion.Euler(90f, 0f, 0f), null, WeaponSurface.Metal);
        Part(PrimitiveType.Cylinder, "Muzzle", new Vector3(0f, 0.025f, 0.4f), new Vector3(0.17f, 0.03f, 0.17f),
            metal, green * 0.25f, Quaternion.Euler(90f, 0f, 0f), null, WeaponSurface.Metal);

        var drum = Part(PrimitiveType.Cylinder, "Drum", new Vector3(0f, -0.03f, 0.02f), new Vector3(0.19f, 0.065f, 0.19f),
            metal, olive * 0.3f, Quaternion.Euler(90f, 0f, 0f), null, WeaponSurface.Metal);
        MarkAnimated(drum, 5);
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Mathf.Deg2Rad;
            Part(PrimitiveType.Sphere, "Grenade", new Vector3(Mathf.Cos(a) * 0.07f, Mathf.Sin(a) * 0.07f, 0.02f),
                Vector3.one * 0.055f, green, green * 0.7f, Quaternion.identity, drum, WeaponSurface.Glow);
        }

        Part(PrimitiveType.Sphere, "Chambered", new Vector3(0f, 0.025f, 0.34f), Vector3.one * 0.12f,
            green, green * 0.9f, Quaternion.identity, null, WeaponSurface.Glow);
        Part(PrimitiveType.Cube, "Grip", new Vector3(0f, -0.16f, -0.09f), new Vector3(0.08f, 0.19f, 0.085f),
            olive, olive * 0.1f, Quaternion.Euler(16f, 0f, 0f), null, WeaponSurface.Leather);
        Part(PrimitiveType.Cube, "Sight", new Vector3(0f, 0.1f, 0.18f), new Vector3(0.02f, 0.04f, 0.03f),
            green, green, Quaternion.identity, null, WeaponSurface.Glow);

        SetMuzzleAnchor(new Vector3(0f, 0.025f, 0.43f));
    }

    void BuildStaff()
    {
        Color glow = new Color(0.35f, 0.8f, 1f);
        Color violet = new Color(0.7f, 0.32f, 1f);
        Color steel = new Color(0.24f, 0.26f, 0.32f);
        Color gold = new Color(0.95f, 0.72f, 0.25f);

        Rod("Shaft", new Vector3(0f, -0.02f, -0.22f), new Vector3(0f, 0.03f, 0.43f),
            0.032f, gold, WeaponSurface.Wood);
        for (int i = 0; i < 4; i++)
            Rod("GripWrap", new Vector3(0f, -0.02f, -0.16f + i * 0.045f),
                new Vector3(0f, -0.015f, -0.13f + i * 0.045f), 0.036f, violet, WeaponSurface.Leather);

        Part(PrimitiveType.Sphere, "Head", new Vector3(0f, 0.04f, 0.45f), Vector3.one * 0.13f,
            violet, violet * 1.5f, Quaternion.identity, null, WeaponSurface.Glow);
        MeshPart(RuntimeMeshLibrary.Ring(0.10f, 0.12f, 20), "HeadRune", new Vector3(0f, 0.04f, 0.45f),
            Vector3.one, gold, glow * 0.9f, Quaternion.Euler(90f, 0f, 0f), null, WeaponSurface.Gold);

        // Механіка лишається стріловою, але це магічний заряд посоха.
        Rod("Bolt", new Vector3(0f, 0.04f, 0.50f), new Vector3(0f, 0.04f, 0.74f), 0.012f,
            steel, WeaponSurface.Metal);
        Blade("BoltTip", new Vector3(0f, 0.04f, 0.78f), 0.10f, 0.038f, 0.014f,
            glow, WeaponSurface.Glow, null, glow);
        for (int s = -1; s <= 1; s += 2)
            Part(PrimitiveType.Cube, "Fletching", new Vector3(s * 0.016f, 0.04f, 0.56f),
                new Vector3(0.026f, 0.006f, 0.06f), violet, glow * 0.6f,
                Quaternion.Euler(0f, 0f, s * 20f), null, WeaponSurface.Glow);

        SetMuzzleAnchor(new Vector3(0f, 0.04f, 0.80f));
    }

    void BuildSalvoKnife()
    {
        Color steel = new Color(0.75f, 0.8f, 0.88f);
        Color dark = new Color(0.09f, 0.1f, 0.13f);
        Color gold = new Color(0.9f, 0.68f, 0.22f);
        Color glow = new Color(0.55f, 0.85f, 1f);

        Rod("Handle", new Vector3(0f, -0.03f, -0.18f), new Vector3(0f, -0.03f, 0.03f),
            0.035f, dark, WeaponSurface.Leather);
        for (int i = 0; i < 5; i++)
            Rod("Wrap", new Vector3(0f, -0.03f, -0.14f + i * 0.03f),
                new Vector3(0f, -0.03f, -0.13f + i * 0.03f), 0.038f, gold, WeaponSurface.Gold);

        Part(PrimitiveType.Cube, "Guard", new Vector3(0f, -0.03f, 0.05f), new Vector3(0.16f, 0.03f, 0.04f),
            gold, gold, Quaternion.identity, null, WeaponSurface.Gold);
        Blade("Blade", new Vector3(0f, 0f, 0.24f), 0.34f, 0.075f, 0.018f,
            steel, WeaponSurface.Metal, null, glow * 0.35f);
        Part(PrimitiveType.Cube, "Fuller", new Vector3(0f, 0.013f, 0.24f), new Vector3(0.010f, 0.004f, 0.24f),
            glow, glow * 0.5f, Quaternion.identity, null, WeaponSurface.Glow);

        for (int i = -1; i <= 1; i += 2)
        {
            var knife = new GameObject("FlyKnife").transform;
            knife.SetParent(_modelRoot, false);
            knife.localPosition = new Vector3(i * 0.15f, 0.05f, 0.26f);
            knife.localRotation = Quaternion.Euler(0f, i * 14f, i * 8f);
            Rod("FlyGrip", new Vector3(0f, 0f, -0.12f), new Vector3(0f, 0f, 0f),
                0.015f, dark, WeaponSurface.Leather, knife);
            Blade("FlyBlade", new Vector3(0f, 0f, 0.10f), 0.20f, 0.038f, 0.010f,
                steel, WeaponSurface.Metal, knife, glow * 0.5f);
            _flyKnives.Add(knife);
            _flyKnifeRest.Add(knife.localPosition);
            AddLevelPart(knife);
        }
    }

    void BuildWateringGun()
    {
        Color green = new Color(0.3f, 0.6f, 0.24f);
        Color glow = new Color(0.45f, 1f, 0.45f);
        Color metal = new Color(0.24f, 0.26f, 0.28f);
        Color wood = new Color(0.36f, 0.18f, 0.07f);

        Part(PrimitiveType.Sphere, "Tank", new Vector3(0f, 0.01f, -0.05f), new Vector3(0.22f, 0.21f, 0.27f),
            green, glow * 0.2f, Quaternion.identity, null, WeaponSurface.Metal);
        Part(PrimitiveType.Cylinder, "TankBand", new Vector3(0f, 0.01f, -0.05f), new Vector3(0.23f, 0.035f, 0.23f),
            metal, metal * 0.1f, Quaternion.Euler(90f, 0f, 0f), null, WeaponSurface.Metal);

        MeshPart(RuntimeMeshLibrary.TaperedCylinder(0.045f, 0.065f, 0.28f), "Spout",
            new Vector3(0f, 0.02f, 0.23f), Vector3.one, metal, metal * 0.12f,
            Quaternion.Euler(90f, 0f, 0f), null, WeaponSurface.Metal);
        Part(PrimitiveType.Sphere, "Head", new Vector3(0f, 0.02f, 0.38f), new Vector3(0.15f, 0.15f, 0.095f),
            green, glow * 0.5f, Quaternion.identity, null, WeaponSurface.Metal);

        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Part(PrimitiveType.Cylinder, "Nozzle", new Vector3(Mathf.Cos(a) * 0.11f, 0.02f + Mathf.Sin(a) * 0.11f, 0.44f),
                new Vector3(0.022f, 0.028f, 0.022f), glow, glow * 1.1f, Quaternion.Euler(90f, 0f, 0f), null, WeaponSurface.Glow);
        }

        for (int i = 0; i < 5; i++)
        {
            float a = i * 72f * Mathf.Deg2Rad;
            Part(PrimitiveType.Cylinder, "Hose", new Vector3(Mathf.Cos(a) * 0.17f, Mathf.Sin(a) * 0.17f - 0.04f, -0.08f),
                new Vector3(0.02f, 0.08f, 0.02f), metal, glow * 0.2f,
                Quaternion.Euler(70f, 0f, a * Mathf.Rad2Deg), null, WeaponSurface.Leather);
        }

        Part(PrimitiveType.Cube, "Handle", new Vector3(0f, -0.17f, -0.05f), new Vector3(0.075f, 0.19f, 0.085f),
            wood, wood * 0.1f, Quaternion.identity, null, WeaponSurface.Wood);
        Part(PrimitiveType.Cube, "Leaf", new Vector3(0f, 0.17f, -0.04f), new Vector3(0.09f, 0.025f, 0.05f),
            glow, glow, Quaternion.Euler(0f, 0f, 24f), null, WeaponSurface.Glow);

        SetMuzzleAnchor(new Vector3(0f, 0.02f, 0.47f));
    }

    void BuildCycloneSword()
    {
        Color dark = new Color(0.09f, 0.1f, 0.13f);
        Color gold = new Color(0.9f, 0.65f, 0.2f);
        Color steel = new Color(0.78f, 0.84f, 0.92f);
        Color glow = new Color(0.45f, 0.82f, 1f);

        Rod("Hilt", new Vector3(0f, -0.02f, -0.18f), new Vector3(0f, -0.02f, 0.03f),
            0.04f, dark, WeaponSurface.Leather);
        for (int i = 0; i < 4; i++)
            Rod("Wrap", new Vector3(0f, -0.02f, -0.14f + i * 0.03f),
                new Vector3(0f, -0.02f, -0.13f + i * 0.03f), 0.043f, gold, WeaponSurface.Gold);

        Part(PrimitiveType.Cube, "Guard", new Vector3(0f, -0.02f, 0.05f), new Vector3(0.22f, 0.032f, 0.05f),
            gold, gold * 0.4f, Quaternion.identity, null, WeaponSurface.Gold);
        Blade("Blade", new Vector3(0f, 0f, 0.29f), 0.46f, 0.078f, 0.022f,
            steel, WeaponSurface.Metal, null, steel * 0.2f);
        Part(PrimitiveType.Cube, "Edge", new Vector3(0f, 0.015f, 0.29f), new Vector3(0.012f, 0.006f, 0.40f),
            glow, glow * 1.2f, Quaternion.identity, null, WeaponSurface.Glow);

        BuildBladeRing();
    }

    void BuildBladeRing()
    {
        Color glow = new Color(0.42f, 1f, 0.58f);
        _spinPart = new GameObject("SpinRing").transform;
        _spinPart.SetParent(_modelRoot, false);
        _spinPart.localPosition = new Vector3(0f, 0f, 0.30f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Cos(a) * 0.17f, Mathf.Sin(a) * 0.17f, 0f);
            Part(PrimitiveType.Cube, "Ring", p, new Vector3(0.055f, 0.022f, 0.02f),
                glow, glow * 1.1f, Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg), _spinPart, WeaponSurface.Glow);
        }
        MeshPart(RuntimeMeshLibrary.Ring(0.19f, 0.205f, 28), "RingGlow", Vector3.zero,
            Vector3.one, glow, glow, Quaternion.identity, _spinPart, WeaponSurface.Glow);
    }

    // ---------- Hands + staff cast ----------

    void BuildHands(WeaponId id)
    {
        Vector3 right;
        Vector3 left;
        ViewModelPoseCatalog.Grips(id, out right, out left);
        bool twoHands = ViewModelPoseCatalog.UsesLeftHand(id);

        _handsRoot = new GameObject("Hands").transform;
        _handsRoot.SetParent(_modelRoot, false);

        HandVisual rightVisual;
        HandVisual leftVisual;
        bool skeletonHands = ViewModelHandAdapter.TryGet(out rightVisual, out leftVisual);

        if (skeletonHands)
        {
            _rightHand = AttachSkeletonHand("RightHand", rightVisual, right, true);
            if (twoHands)
                _leftHand = AttachSkeletonHand("LeftHand", leftVisual, left, false);
        }
        else
        {
            // Запасний варіант — власна процедурна кисть, але не WRAD/PSX і не тканина.
            _rightHand = BuildProceduralHand("RightHand", right, true);
            if (twoHands)
                _leftHand = BuildProceduralHand("LeftHand", left, false);
        }

        if (_rightHand != null)
        {
            _rightHandRestPos = _rightHand.localPosition;
            _rightHandRestRot = _rightHand.localRotation;
        }
        if (_leftHand != null)
        {
            _leftHandRestPos = _leftHand.localPosition;
            _leftHandRestRot = _leftHand.localRotation;
        }
    }

    Transform AttachSkeletonHand(string name, HandVisual visual, Vector3 grip, bool right)
    {
        var hand = new GameObject(name).transform;
        hand.SetParent(_handsRoot, false);
        hand.localPosition = grip;
        hand.localRotation = Quaternion.Euler(right ? 2f : -2f, right ? -6f : 6f, right ? -8f : 8f);

        var meshObject = new GameObject("FabSkeletonHand");
        meshObject.transform.SetParent(hand, false);
        meshObject.transform.localPosition = Vector3.zero;
        meshObject.transform.localRotation = Quaternion.identity;
        meshObject.transform.localScale = Vector3.one * visual.Scale * _handScale;
        meshObject.AddComponent<MeshFilter>().sharedMesh = visual.Mesh;
        var renderer = meshObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = visual.Material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        // Без манжет: кисть і передпліччя вже є частиною скелетної моделі гравця.
        return hand;
    }

    Transform BuildProceduralHand(string name, Vector3 grip, bool right)
    {
        var hand = new GameObject(name).transform;
        hand.SetParent(_handsRoot, false);
        hand.localPosition = grip;
        hand.localRotation = Quaternion.Euler(0f, right ? -10f : 10f, right ? -12f : 12f);
        hand.localScale = Vector3.one * _handScale;

        Color glove = new Color(0.26f, 0.21f, 0.17f);

        Part(PrimitiveType.Cube, "Palm", Vector3.zero, new Vector3(0.088f, 0.064f, 0.11f),
            glove, glove * 0.12f, Quaternion.identity, hand, WeaponSurface.Leather);

        for (int i = 0; i < 4; i++)
        {
            var finger = Part(PrimitiveType.Cube, "Finger",
                new Vector3(-0.033f + i * 0.022f, -0.006f, 0.076f),
                new Vector3(0.018f, 0.052f, 0.022f), glove, glove * 0.14f,
                Quaternion.Euler(38f, 0f, 0f), hand, WeaponSurface.Leather);
            _fingers.Add(finger);
            _fingerRestRots.Add(finger.localRotation);
        }

        Part(PrimitiveType.Cube, "Thumb",
            new Vector3(right ? 0.052f : -0.052f, 0.014f, 0.032f),
            new Vector3(0.022f, 0.044f, 0.03f), glove, glove * 0.14f,
            Quaternion.Euler(0f, 0f, right ? -28f : 28f), hand, WeaponSurface.Leather);
        Rod("Forearm", new Vector3(0f, -0.01f, -0.05f),
            new Vector3(right ? 0.12f : -0.12f, -0.52f, -0.34f),
            0.052f, new Color(0.72f, 0.68f, 0.58f), WeaponSurface.Gold, hand);
        return hand;
    }

    void AnimateHands(float reloadArc, float castArc)
    {
        if (_rightHand == null && _leftHand == null)
            return;

        float walkBob = Mathf.Abs(Mathf.Sin(_walkPhase)) * _walk;
        float curl = Mathf.Clamp01(_kick * 0.35f + reloadArc * 0.65f);

        if (_rightHand != null)
        {
            Vector3 rp = _rightHandRestPos
                + Vector3.back * (_kick * 0.045f)
                + Vector3.down * (reloadArc * 0.055f + _equip * 0.09f)
                + Vector3.up * (castArc * 0.05f)
                + Vector3.forward * (castArc * 0.035f);
            rp.x += Mathf.Sin(_walkPhase) * _walk * 0.008f;
            rp.y += walkBob * 0.01f;
            _rightHand.localPosition = rp;
            _rightHand.localRotation = _rightHandRestRot * Quaternion.Euler(
                -_kick * 14f + reloadArc * 20f - castArc * 22f,
                _kick * 5f,
                reloadArc * 10f - castArc * 14f);
        }

        if (_leftHand != null)
        {
            Vector3 lp = _leftHandRestPos
                + Vector3.back * (_kick * 0.025f + reloadArc * 0.09f)
                + Vector3.down * (reloadArc * 0.11f + _equip * 0.07f)
                + Vector3.up * (castArc * 0.045f)
                + Vector3.forward * (castArc * 0.02f);
            lp.y -= walkBob * 0.012f;
            _leftHand.localPosition = lp;
            _leftHand.localRotation = _leftHandRestRot * Quaternion.Euler(
                reloadArc * 26f - castArc * 18f,
                -_kick * 4f,
                -reloadArc * 12f + castArc * 10f);
        }

        int count = Mathf.Min(_fingers.Count, _fingerRestRots.Count);
        for (int i = 0; i < count; i++)
        {
            if (_fingers[i] == null)
                continue;
            _fingers[i].localRotation = _fingerRestRots[i] *
                Quaternion.Euler(curl * 26f - castArc * 42f, 0f, 0f);
        }
    }

    void BuildFlameStream()
    {
        if (_muzzleAnchor == null)
            return;

        _flameStream = new GameObject("FlameStream").transform;
        _flameStream.SetParent(_muzzleAnchor, false);
        _flameStream.localPosition = Vector3.zero;
        _flameStream.localRotation = Quaternion.identity;

        string[] sprites =
        {
            "flame_01", "flame_02", "flame_03", "flame_04", "flame_05", "flame_06",
            "fire_01", "fire_02", "smoke_01", "smoke_02", "spark_01", "magic_01"
        };

        for (int i = 0; i < 22; i++)
        {
            string sprite = sprites[i % sprites.Length];
            float t = i / (float)(sprites.Length - 1);
            float distance = 0.18f + (i % sprites.Length) * 0.18f + Mathf.Floor(i / sprites.Length) * 0.12f;
            float spread = Mathf.Lerp(0.04f, 0.24f, t);
            Vector3 rest = new Vector3(
                Mathf.Sin(i * 2.7f) * spread,
                Mathf.Cos(i * 1.9f) * spread * 0.75f,
                distance);
            float scale = Mathf.Lerp(0.20f, 0.62f, t);
            Color tint = i % 9 >= 7
                ? new Color(0.55f, 0.62f, 0.68f, 0.7f)
                : new Color(1f, 0.62f + Mathf.Sin(i) * 0.12f, 0.22f, 0.95f);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Flame_" + sprite;
            quad.transform.SetParent(_flameStream, false);
            quad.transform.localPosition = rest;
            quad.transform.localRotation = Quaternion.Euler(0f, 0f, i * 37f);
            quad.transform.localScale = Vector3.one * scale;
            var collider = quad.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);
            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = FlameMaterial(sprite, tint);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            _flameParticles.Add(quad.transform);
            _flameParticleRest.Add(rest);
            _flamePhase.Add(i * 0.37f);
            _flameParticleScale.Add(scale);
        }

        var lightObject = new GameObject("FlameLight");
        lightObject.transform.SetParent(_flameStream, false);
        lightObject.transform.localPosition = new Vector3(0f, 0f, 0.45f);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.46f, 0.12f);
        light.range = 3.2f;
        light.intensity = 2.4f;
        light.shadows = LightShadows.None;

        _flameStream.gameObject.SetActive(false);
    }

    static Material FlameMaterial(string sprite, Color tint)
    {
        string key = sprite + "_" + Key(tint);
        Material material;
        if (_vfxMaterials.TryGetValue(key, out material) && material != null)
            return material;
        Texture2D texture = Resources.Load<Texture2D>("Vfx/Particles/" + sprite);
        material = GameMaterials.CreateGlow(tint, texture, true);
        _vfxMaterials[key] = material;
        return material;
    }

    void BuildCastOrb(Vector3 muzzleLocal)
    {
        Color glow = new Color(0.45f, 0.78f, 1f);
        _castOrb = Part(PrimitiveType.Sphere, "CastOrb", muzzleLocal, Vector3.one * 0.075f,
            glow, glow * 1.8f, Quaternion.identity, _modelRoot, WeaponSurface.Glow);
        _castRing = MeshPart(RuntimeMeshLibrary.Ring(0.085f, 0.10f, 20), "CastRing", muzzleLocal,
            Vector3.one, new Color(0.72f, 0.9f, 1f), glow * 1.2f,
            Quaternion.Euler(90f, 0f, 0f), _modelRoot, WeaponSurface.Glow);
    }

    // ---------- Helpers ----------

    void AddLevelPart(Transform part)
    {
        _levelParts.Add(part);
        _levelPartScales.Add(part.localScale);
    }

    void MarkAnimated(Transform part, int mode)
    {
        _animatedPart = part;
        _partAnimMode = mode;
        if (part == null)
            return;
        _partRestPos = part.localPosition;
        _partRestRot = part.localRotation;
    }

    void Barrel(Vector3 position, float radius, float length, Color color, WeaponSurface surface = WeaponSurface.Metal)
    {
        TubeZ("Barrel", position, radius, radius * 0.85f, length, color, surface);
    }

    /// <summary>Циліндр між двома точками в локальному просторі батька.</summary>
    Transform Rod(string name, Vector3 from, Vector3 to, float radius, Color color,
        WeaponSurface surface = WeaponSurface.Metal, Transform parent = null, Color? emission = null)
    {
        Vector3 delta = to - from;
        float length = Mathf.Max(0.005f, delta.magnitude);
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, delta / length);
        return Part(PrimitiveType.Cylinder, name, (from + to) * 0.5f,
            new Vector3(radius, length * 0.5f, radius), color,
            emission.HasValue ? emission.Value : color * 0.2f,
            rotation, parent, surface);
    }

    /// <summary>Труба/ствіл уздовж +Z. radiusBack — біля рукояті, radiusFront — біля дула.</summary>
    Transform TubeZ(string name, Vector3 position, float radiusBack, float radiusFront, float length,
        Color color, WeaponSurface surface = WeaponSurface.Metal, Transform parent = null, Color? emission = null)
    {
        return MeshPart(RuntimeMeshLibrary.TaperedCylinder(radiusFront, radiusBack, length), name, position,
            Vector3.one, color, emission.HasValue ? emission.Value : color * 0.22f,
            Quaternion.Euler(90f, 0f, 0f), parent, surface);
    }

    Transform Blade(string name, Vector3 position, float length, float width, float thickness,
        Color color, WeaponSurface surface = WeaponSurface.Metal, Transform parent = null, Color? emission = null)
    {
        return MeshPart(RuntimeMeshLibrary.WedgeBlade(length, width, thickness), name, position,
            Vector3.one, color, emission.HasValue ? emission.Value : color * 0.3f,
            Quaternion.identity, parent, surface);
    }

    Transform Part(PrimitiveType type, string name, Vector3 position, Vector3 scale,
        Color baseColor, Color emission, Quaternion rotation, Transform parent = null,
        WeaponSurface surface = WeaponSurface.Metal)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent != null ? parent : _modelRoot, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.transform.localRotation = rotation;

        Object.Destroy(go.GetComponent<Collider>());
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = SharedMaterial(baseColor, emission, surface);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return go.transform;
    }

    Transform MeshPart(Mesh mesh, string name, Vector3 position, Vector3 scale,
        Color baseColor, Color emission, Quaternion rotation, Transform parent = null,
        WeaponSurface surface = WeaponSurface.Metal)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent != null ? parent : _modelRoot, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.transform.localRotation = rotation;

        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = SharedMaterial(baseColor, emission, surface);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return go.transform;
    }

    static Material SharedMaterial(Color baseColor, Color emission, WeaponSurface surface)
    {
        string key = (int)surface + "_" + Key(baseColor) + "_" + Key(emission);
        Material material;
        if (_materials.TryGetValue(key, out material) && material != null)
            return material;

        Texture2D colorMap = null;
        Texture2D normalMap = null;
        Color tint = baseColor;
        float smoothness = 0.3f;
        float metallic = 0f;

        switch (surface)
        {
            case WeaponSurface.Wood:
                colorMap = RuntimeTextureLibrary.WoodColor;
                normalMap = RuntimeTextureLibrary.WoodNormal;
                tint = Color.Lerp(baseColor, Color.white, 0.42f);
                smoothness = 0.16f;
                break;
            case WeaponSurface.Leather:
                colorMap = RuntimeTextureLibrary.LeatherColor;
                normalMap = RuntimeTextureLibrary.LeatherNormal;
                tint = Color.Lerp(baseColor, Color.white, 0.36f);
                smoothness = 0.12f;
                break;
            case WeaponSurface.Fabric:
                colorMap = RuntimeTextureLibrary.FabricColor;
                normalMap = RuntimeTextureLibrary.FabricNormal;
                tint = Color.Lerp(baseColor, Color.white, 0.4f);
                smoothness = 0.1f;
                break;
            case WeaponSurface.Gold:
                colorMap = RuntimeTextureLibrary.MetalColor;
                normalMap = RuntimeTextureLibrary.MetalNormal;
                tint = Color.Lerp(baseColor, Color.white, 0.18f);
                smoothness = 0.68f;
                metallic = 0.85f;
                break;
            case WeaponSurface.Glow:
                tint = baseColor;
                smoothness = 0.55f;
                break;
            default:
                colorMap = RuntimeTextureLibrary.MetalColor;
                normalMap = RuntimeTextureLibrary.MetalNormal;
                tint = Color.Lerp(baseColor, Color.white, 0.34f);
                smoothness = 0.48f;
                metallic = 0.7f;
                break;
        }

        material = GameMaterials.Create(tint, emission, colorMap, new Vector2(1.8f, 1.8f), normalMap, smoothness, metallic);
        _materials[key] = material;
        return material;
    }

    static int Key(Color c)
    {
        return Mathf.RoundToInt(c.r * 255f) * 65536
               + Mathf.RoundToInt(c.g * 255f) * 256
               + Mathf.RoundToInt(c.b * 255f);
    }
}
