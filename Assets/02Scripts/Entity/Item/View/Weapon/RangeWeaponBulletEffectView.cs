using Alpha.AlphaCamera;
using Alpha.Item.Weapon.Range;
using UnityEngine;
using UnityEngine.Rendering;

namespace Alpha.Item.Weapon.View
{
    // Range 무기의 Muzzle·탄도·충돌 Effect만 표현한다.
    [DisallowMultipleComponent]
    public sealed class RangeWeaponBulletEffectView : WeaponView
    {
        [SerializeField]
        private RangeWeapon _weapon;

        [Header("Muzzle")]
        [SerializeField]
        private ParticleSystem _muzzleFlashPrefab;

        [SerializeField, Min(0.01f)]
        private float _muzzleLifetime = 0.5f;

        [Header("Hitscan")]
        [SerializeField]
        private BulletTracerView _tracerPrefab;

        [SerializeField]
        private ParticleSystem _impactPrefab;

        [SerializeField, Min(0.01f)]
        private float _impactLifetime = 5f;

        [SerializeField]
        private Vector2 _scopeTracerViewportPosition =
            new(0.5f, 0.25f);

        [SerializeField, Min(0f)]
        private float _scopeTracerDepthOffset = 0.05f;

        [Header("Penetration")]
        [SerializeField]
        private Material _material;

        [SerializeField, Min(0.01f)]
        private float _visibleDuration = 0.05f;

        private CameraCore _cameraCore;
        private LineRenderer _penetrationRenderer;
        private float _penetrationHideTime;

        private void Awake()
        {
            ResolveDependencies();
            HidePenetration();
        }

        private void OnEnable()
        {
            ResolveDependencies();

            if (_weapon == null)
                return;

            _weapon.OnFired -= HandleFired;
            _weapon.OnFired += HandleFired;
            _weapon.OnTrajectoryResolved -= HandleTrajectoryResolved;
            _weapon.OnTrajectoryResolved += HandleTrajectoryResolved;
        }

        private void OnDisable()
        {
            if (_weapon != null)
            {
                _weapon.OnFired -= HandleFired;
                _weapon.OnTrajectoryResolved -= HandleTrajectoryResolved;
            }

            HidePenetration();
        }

        private void Update()
        {
            if (_penetrationRenderer != null &&
                _penetrationRenderer.enabled &&
                Time.time >= _penetrationHideTime)
            {
                _penetrationRenderer.enabled = false;
            }
        }

        public override void BindCamera(CameraCore p_cameraCore)
        {
            _cameraCore = p_cameraCore;
        }

        private void ResolveDependencies()
        {
            _weapon ??= GetComponentInParent<RangeWeapon>();
        }

        private void HandleFired(RangeAttackRequest p_request)
        {
            if (_muzzleFlashPrefab == null)
                return;

            ParticleSystem effect = Instantiate(
                _muzzleFlashPrefab,
                p_request.MuzzleOrigin,
                Quaternion.LookRotation(p_request.Direction));

            effect.Play(true);
            Destroy(effect.gameObject, _muzzleLifetime);
        }

        private void HandleTrajectoryResolved(RangeAttackResult p_result)
        {
            if (_weapon == null)
                return;

            switch (_weapon.AttackType)
            {
                case ERangeAttackType.Hitscan:
                    PlayHitscan(p_result);
                    break;

                case ERangeAttackType.Penetration:
                    PlayPenetration(p_result);
                    break;
            }
        }

        private void PlayHitscan(in RangeAttackResult p_result)
        {
            if (_tracerPrefab != null)
            {
                Vector3 tracerStart = ResolveTracerStart(p_result.StartPoint);
                BulletTracerView tracer = Instantiate(
                    _tracerPrefab,
                    tracerStart,
                    Quaternion.identity);

                tracer.Play(tracerStart, p_result.EndPoint);
            }

            if (!p_result.HasCollision || _impactPrefab == null)
                return;

            ParticleSystem impact = Instantiate(
                _impactPrefab,
                p_result.EndPoint,
                Quaternion.LookRotation(p_result.CollisionNormal));

            impact.Play(true);
            Destroy(impact.gameObject, _impactLifetime);
        }

        private Vector3 ResolveTracerStart(Vector3 p_defaultStart)
        {
            if (_cameraCore?.RenderCamera == null ||
                _cameraCore.Context.EffectiveViewType != ECameraViewType.Scope)
            {
                return p_defaultStart;
            }

            Camera renderCamera = _cameraCore.RenderCamera;
            Ray scopeRay = renderCamera.ViewportPointToRay(
                new Vector3(
                    _scopeTracerViewportPosition.x,
                    _scopeTracerViewportPosition.y));
            float startDistance = Mathf.Max(
                renderCamera.nearClipPlane + _scopeTracerDepthOffset,
                0.01f);

            return scopeRay.GetPoint(startDistance);
        }

        private void PlayPenetration(in RangeAttackResult p_result)
        {
            EnsurePenetrationRenderer();
            SyncPenetrationWidth();
            _penetrationRenderer.SetPosition(0, p_result.StartPoint);
            _penetrationRenderer.SetPosition(1, p_result.EndPoint);
            _penetrationRenderer.enabled = true;
            _penetrationHideTime = Time.time + _visibleDuration;
        }

        private void EnsurePenetrationRenderer()
        {
            if (_penetrationRenderer != null)
                return;

            GameObject rendererObject = new("PenetrationRenderer");
            rendererObject.transform.SetParent(transform, false);
            rendererObject.layer = gameObject.layer;

            _penetrationRenderer =
                rendererObject.AddComponent<LineRenderer>();
            _penetrationRenderer.useWorldSpace = true;
            _penetrationRenderer.loop = false;
            _penetrationRenderer.positionCount = 2;
            _penetrationRenderer.startColor = Color.white;
            _penetrationRenderer.endColor = Color.white;
            _penetrationRenderer.numCapVertices = 4;
            _penetrationRenderer.numCornerVertices = 2;
            _penetrationRenderer.alignment = LineAlignment.View;
            _penetrationRenderer.textureMode = LineTextureMode.Stretch;
            _penetrationRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _penetrationRenderer.receiveShadows = false;
            _penetrationRenderer.sharedMaterial = _material;
            _penetrationRenderer.enabled = false;
            SyncPenetrationWidth();
        }

        private void SyncPenetrationWidth()
        {
            if (_penetrationRenderer == null || _weapon == null)
                return;

            _penetrationRenderer.startWidth = _weapon.StartRadius;
            _penetrationRenderer.endWidth = _weapon.EndRadius;
        }

        private void HidePenetration()
        {
            if (_penetrationRenderer != null)
                _penetrationRenderer.enabled = false;
        }

        private void OnValidate()
        {
            _muzzleLifetime = Mathf.Max(0.01f, _muzzleLifetime);
            _impactLifetime = Mathf.Max(0.01f, _impactLifetime);
            _scopeTracerViewportPosition = new Vector2(
                Mathf.Clamp01(_scopeTracerViewportPosition.x),
                Mathf.Clamp01(_scopeTracerViewportPosition.y));
            _scopeTracerDepthOffset = Mathf.Max(0f, _scopeTracerDepthOffset);
            _visibleDuration = Mathf.Max(0.01f, _visibleDuration);
            ResolveDependencies();
            SyncPenetrationWidth();
        }
    }
}
