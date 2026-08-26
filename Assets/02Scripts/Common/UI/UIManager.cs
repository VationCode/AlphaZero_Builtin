using UnityEngine;

namespace Alpha.UI
{
    // Scene의 공용 HUD View 참조를 Installer에 제공한다.
    public class UIManager : MonoBehaviour
    {
        public CrossHairUI CrossHairUI;
        public StateUI StateUI;
        public InteractionView InteractionUI;

        private bool _isGameplayHudVisible = true;
        private bool _crossHairWasActive;
        private bool _stateWasActive;
        private bool _interactionWasActive;

        // Cinematic 동안 공용 HUD만 숨기고 Fade 같은 연출 UI는 유지한다.
        public void SetGameplayHudVisible(bool p_isVisible)
        {
            if (_isGameplayHudVisible == p_isVisible)
                return;

            _isGameplayHudVisible = p_isVisible;

            if (!p_isVisible)
            {
                _crossHairWasActive =
                    CrossHairUI != null && CrossHairUI.gameObject.activeSelf;
                _stateWasActive =
                    StateUI != null && StateUI.gameObject.activeSelf;
                _interactionWasActive =
                    InteractionUI != null && InteractionUI.gameObject.activeSelf;

                SetHudObjectsActive(false, false, false);
                return;
            }

            SetHudObjectsActive(
                _crossHairWasActive,
                _stateWasActive,
                _interactionWasActive);
        }

        private void SetHudObjectsActive(
            bool p_crossHair,
            bool p_state,
            bool p_interaction)
        {
            if (CrossHairUI != null)
                CrossHairUI.gameObject.SetActive(p_crossHair);

            if (StateUI != null)
                StateUI.gameObject.SetActive(p_state);

            if (InteractionUI != null)
                InteractionUI.gameObject.SetActive(p_interaction);
        }
    }
}
