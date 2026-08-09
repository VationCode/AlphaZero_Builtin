using System.Collections.Generic;
using Alpha.AlphaCamera;
using UnityEngine;

namespace Alpha.Player
{
    // Scope 카메라가 Player 내부를 통과할 때 외형만 숨기고 원래 상태를 복구한다.
    public class PlayerScopeView : MonoBehaviour
    {
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Animator _animator;

        private readonly Dictionary<Renderer, bool> _rendererStates = new();

        private CameraCore _cameraCore;
        private AnimatorCullingMode _animatorCullingMode;
        private bool _isHidden;

        public bool Bind(CameraCore p_cameraCore)
        {
            Unbind();

            if (p_cameraCore == null || _visualRoot == null)
                return false;

            _cameraCore = p_cameraCore;
            _cameraCore.OnViewTransitionStarted += HandleTransitionStarted;
            _cameraCore.OnViewTransitionCompleted += HandleTransitionCompleted;

            if (_cameraCore.Context.EffectiveViewType == ECameraViewType.Scope)
                HideVisuals();

            return true;
        }

        public void Unbind()
        {
            if (_cameraCore != null)
            {
                _cameraCore.OnViewTransitionStarted -= HandleTransitionStarted;
                _cameraCore.OnViewTransitionCompleted -= HandleTransitionCompleted;
                _cameraCore = null;
            }

            RestoreVisuals();
        }

        // Scope 진입 전환이 시작되기 전에 Player Mesh를 숨긴다.
        private void HandleTransitionStarted(
            ECameraViewType p_fromViewType,
            ECameraViewType p_targetViewType)
        {
            if (p_targetViewType == ECameraViewType.Scope)
                HideVisuals();
        }

        // Scope에서 완전히 빠져나온 뒤에만 Player Mesh를 다시 표시한다.
        private void HandleTransitionCompleted(ECameraViewType p_viewType)
        {
            if (p_viewType == ECameraViewType.Scope)
                HideVisuals();
            else
                RestoreVisuals();
        }

        private void HideVisuals()
        {
            if (_isHidden)
                return;

            _rendererStates.Clear();

            foreach (Renderer playerRenderer in
                     _visualRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (playerRenderer == null)
                    continue;

                _rendererStates[playerRenderer] = playerRenderer.enabled;
                playerRenderer.enabled = false;
            }

            // Renderer가 숨겨져도 조준과 무기 Bone은 계속 갱신한다.
            if (_animator != null)
            {
                _animatorCullingMode = _animator.cullingMode;
                _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            _isHidden = true;
        }

        private void RestoreVisuals()
        {
            if (!_isHidden)
                return;

            foreach (KeyValuePair<Renderer, bool> rendererState in _rendererStates)
            {
                if (rendererState.Key != null)
                    rendererState.Key.enabled = rendererState.Value;
            }

            _rendererStates.Clear();

            if (_animator != null)
                _animator.cullingMode = _animatorCullingMode;

            _isHidden = false;
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
