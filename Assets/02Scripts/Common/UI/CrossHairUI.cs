using System.Collections;
using Alpha.AlphaCamera;
using Alpha.Combat;
using Alpha.Item.Weapon.Melee;
using Alpha.Player.Combat;
using UnityEngine;

// Camera View에 맞는 Rifle·Scope 조준 UI를 표시한다.
public class CrossHairUI : MonoBehaviour
{
    [SerializeField] private GameObject _content;
    [SerializeField] private GameObject _rifleCrossHair;
    [SerializeField] private GameObject _scopeCrossHair;
    [SerializeField] private GameObject _hitCrossHair;

    [Header("Hit Feedback")]
    [SerializeField, Min(0.01f)]
    private float _hitDuration = 0.12f;

    private CameraCore _cameraCore;
    private CombatModule _combatModule;
    private Coroutine _hitRoutine;
    private bool _isWeaponVisible = true;

    private void Awake()
    {
        _rifleCrossHair.SetActive(true);
        _scopeCrossHair.SetActive(false);
        _hitCrossHair.SetActive(false);
    }

    public bool Bind(
        CameraCore p_cameraCore,
        CombatModule p_combatModule)
    {
        Unbind();

        if (p_cameraCore == null ||
            p_combatModule == null ||
            _content == null ||
            _rifleCrossHair == null ||
            _scopeCrossHair == null ||
            _hitCrossHair == null)
        {
            return false;
        }

        _cameraCore = p_cameraCore;
        _combatModule = p_combatModule;

        _cameraCore.OnViewTransitionStarted += HandleTransitionStarted;
        _cameraCore.OnViewTransitionCompleted += HandleTransitionCompleted;
        _combatModule.OnHitConfirmed += HandleHitConfirmed;

        ApplyView(_cameraCore.Context.EffectiveViewType);
        return true;
    }

    // Melee 무기에서는 조준 UI 전체를 사용하지 않는다.
    public void HandleWeaponChanged(WeaponDTO p_weapon)
    {
        _isWeaponVisible =
            p_weapon?.WeaponType != EWeaponType.Melee;

        if (_cameraCore != null)
        {
            ApplyView(_cameraCore.Context.EffectiveViewType);
            return;
        }

        ApplyContentVisibility();
    }

    public void Unbind()
    {
        if (_cameraCore != null)
        {
            _cameraCore.OnViewTransitionStarted -= HandleTransitionStarted;
            _cameraCore.OnViewTransitionCompleted -= HandleTransitionCompleted;
            _cameraCore = null;
        }

        if (_combatModule != null)
        {
            _combatModule.OnHitConfirmed -= HandleHitConfirmed;
            _combatModule = null;
        }

        StopHitFeedback();
    }

    private void HandleTransitionStarted(ECameraViewType p_fromViewType, ECameraViewType p_targetViewType)
    {
        ApplyView(p_targetViewType);
    }

    private void HandleTransitionCompleted(ECameraViewType p_viewType)
    {
        ApplyView(p_viewType);
    }

    private void HandleHitConfirmed(DamageInfo p_damageInfo)
    {
        // Melee는 무기 자체 타격 연출을 사용하므로 Hit Marker를 표시하지 않는다.
        if (_combatModule?.CurrentWeapon is MeleeWeapon)
            return;

        PlayHit();
    }

    // 연속 명중 시 표시 시간을 처음부터 다시 시작한다.
    public void PlayHit()
    {
        if (_hitCrossHair == null || !isActiveAndEnabled)
            return;

        if (_hitRoutine != null)
            StopCoroutine(_hitRoutine);

        _hitCrossHair.SetActive(true);
        ApplyContentVisibility();

        _hitRoutine = StartCoroutine(
            HideHitAfterDelay());
    }

    private IEnumerator HideHitAfterDelay()
    {
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0.01f, _hitDuration));

        _hitCrossHair.SetActive(false);
        _hitRoutine = null;
        ApplyContentVisibility();
    }

    private void ApplyView(ECameraViewType p_viewType)
    {
        bool isScope = p_viewType == ECameraViewType.Scope;

        _rifleCrossHair.SetActive(
            _isWeaponVisible && !isScope);

        _scopeCrossHair.SetActive(
            _isWeaponVisible && isScope);

        ApplyContentVisibility();
    }

    // 원거리 무기의 명중 표시는 일반 조준점과 독립적으로 노출한다.
    private void ApplyContentVisibility()
    {
        if (_content == null)
            return;

        bool isHitVisible =
            _hitCrossHair != null &&
            _hitCrossHair.activeSelf;

        _content.SetActive(
            _isWeaponVisible || isHitVisible);
    }

    private void StopHitFeedback()
    {
        if (_hitRoutine != null)
        {
            StopCoroutine(_hitRoutine);
            _hitRoutine = null;
        }

        _hitCrossHair?.SetActive(false);
        ApplyContentVisibility();
    }

    private void OnDestroy()
    {
        Unbind();
    }
}
