using Alpha.AlphaCamera;
using UnityEngine;

// Camera View에 맞는 Rifle·Scope 조준 UI를 표시한다.
public class CrossHairUI : MonoBehaviour
{
    [SerializeField] private GameObject _content;
    [SerializeField] private GameObject _rifleCrossHair;
    [SerializeField] private GameObject _scopeCrossHair;
    [SerializeField] private GameObject _hitCrossHair;
    private CameraCore _cameraCore;
    private bool _isWeaponVisible = true;

    private void Awake()
    {
        _rifleCrossHair.SetActive(true);
        _scopeCrossHair.SetActive(false);
        _hitCrossHair.SetActive(false);
    }

    public bool Bind(CameraCore p_cameraCore)
    {
        Unbind();

        if (p_cameraCore == null ||
            _content == null ||
            _rifleCrossHair == null ||
            _scopeCrossHair == null)
            return false;

        _cameraCore = p_cameraCore;
        _cameraCore.OnViewTransitionStarted += HandleTransitionStarted;
        _cameraCore.OnViewTransitionCompleted += HandleTransitionCompleted;

        ApplyView(_cameraCore.Context.EffectiveViewType);
        return true;
    }

    // Melee 무기일 때만 CrossHair의 시각 Root를 숨긴다.
    public void HandleWeaponChanged(WeaponDTO p_weapon)
    {
        _isWeaponVisible =
            p_weapon?.WeaponType != EWeaponType.Melee;

        if (_cameraCore != null)
        {
            ApplyView(_cameraCore.Context.EffectiveViewType);
            return;
        }

        _content?.SetActive(_isWeaponVisible);
    }

    public void Unbind()
    {
        if (_cameraCore == null)
            return;

        _cameraCore.OnViewTransitionStarted -= HandleTransitionStarted;
        _cameraCore.OnViewTransitionCompleted -= HandleTransitionCompleted;
        _cameraCore = null;
    }

    private void HandleTransitionStarted(ECameraViewType p_fromViewType, ECameraViewType p_targetViewType)
    {
        ApplyView(p_targetViewType);
    }

    private void HandleTransitionCompleted(ECameraViewType p_viewType)
    {
        ApplyView(p_viewType);
    }

    private void ApplyView(ECameraViewType p_viewType)
    {
        _content.SetActive(_isWeaponVisible);

        if (!_isWeaponVisible)
            return;

        bool isScope = p_viewType == ECameraViewType.Scope;

        _rifleCrossHair.SetActive(!isScope);
        _scopeCrossHair.SetActive(isScope);
    }

    private void OnDestroy()
    {
        Unbind();
    }
}
