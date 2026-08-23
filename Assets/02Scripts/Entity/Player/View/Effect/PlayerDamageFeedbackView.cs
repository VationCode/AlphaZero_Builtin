using Alpha.AlphaCamera;
using Alpha.Combat;
using Alpha.Player.Actions;
using UnityEngine;

namespace Alpha.Player.Effect
{
    // Player가 받는 피격·사망을 Local Camera Shake로 표현한다.
    [DisallowMultipleComponent]
    public sealed class PlayerDamageFeedbackView : MonoBehaviour
    {
        [Header("Camera Shake")]
        [SerializeField]
        private string _lightShakeName = "Weak";

        [SerializeField]
        private string _heavyShakeName = "Medium";

        [SerializeField]
        private string _knockdownShakeName = "Strong";

        private PlayerActionFlow _actionFlow;
        private CameraCore _cameraCore;
        private bool _isSubscribed;

        public void Bind(
            PlayerActionFlow p_actionFlow,
            CameraCore p_cameraCore)
        {
            Unbind();
            _actionFlow = p_actionFlow;
            _cameraCore = p_cameraCore;
            Subscribe();
        }

        public void Unbind()
        {
            Unsubscribe();
            _actionFlow = null;
            _cameraCore = null;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_isSubscribed ||
                !isActiveAndEnabled ||
                _actionFlow == null)
            {
                return;
            }

            _actionFlow.OnDamageFeedbackRequested +=
                HandleDamageFeedbackRequested;
            _actionFlow.OnDeathStarted += HandleDeathStarted;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
                return;

            if (_actionFlow != null)
            {
                _actionFlow.OnDamageFeedbackRequested -=
                    HandleDamageFeedbackRequested;
                _actionFlow.OnDeathStarted -= HandleDeathStarted;
            }

            _isSubscribed = false;
        }

        private void HandleDamageFeedbackRequested(
            EHitReaction p_reaction)
        {
            string shakeName = p_reaction switch
            {
                EHitReaction.Light => _lightShakeName,
                EHitReaction.Heavy => _heavyShakeName,
                EHitReaction.Knockdown or EHitReaction.Launch =>
                    _knockdownShakeName,
                _ => null
            };

            RequestShake(shakeName);
        }

        private void HandleDeathStarted()
        {
            RequestShake(_knockdownShakeName);
        }

        private void RequestShake(string p_name)
        {
            if (!string.IsNullOrWhiteSpace(p_name))
                _cameraCore?.RequestShake(p_name.Trim());
        }
    }
}
