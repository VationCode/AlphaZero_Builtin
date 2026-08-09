using Alpha.AlphaCamera;
using UnityEngine;

// Camera View에 맞는 Rifle·Scope 조준 UI를 표시한다.
public class CrossHairUI : MonoBehaviour
{
    [SerializeField] private GameObject _rifleRoot;
    [SerializeField] private GameObject _scopeRoot;

    private CameraCore _cameraCore;

    public bool Bind(CameraCore p_cameraCore)
    {
        Unbind();

        if (p_cameraCore == null || _rifleRoot == null || _scopeRoot == null)
            return false;

        _cameraCore = p_cameraCore;
        _cameraCore.OnViewTransitionStarted += HandleTransitionStarted;
        _cameraCore.OnViewTransitionCompleted += HandleTransitionCompleted;

        ApplyView(_cameraCore.Context.EffectiveViewType);
        return true;
    }

    public void Unbind()
    {
        if (_cameraCore == null)
            return;

        _cameraCore.OnViewTransitionStarted -= HandleTransitionStarted;
        _cameraCore.OnViewTransitionCompleted -= HandleTransitionCompleted;
        _cameraCore = null;
    }

    private void HandleTransitionStarted(
        ECameraViewType p_fromViewType,
        ECameraViewType p_targetViewType)
    {
        ApplyView(p_targetViewType);
    }

    private void HandleTransitionCompleted(ECameraViewType p_viewType)
    {
        ApplyView(p_viewType);
    }

    private void ApplyView(ECameraViewType p_viewType)
    {
        bool isScope = p_viewType == ECameraViewType.Scope;

        _rifleRoot.SetActive(!isScope);
        _scopeRoot.SetActive(isScope);
    }

    private void OnDestroy()
    {
        Unbind();
    }
}
