using Alpha.Player;
using UnityEngine;

namespace Alpha.UI
{
    // Player의 상호작용 가능 상태를 F키 안내 UI로 표현한다.
    public sealed class InteractionView : MonoBehaviour
    {
        [SerializeField]
        private GameObject _content;

        private ItemPickupFlow _pickupFlow;

        private void Awake()
        {
            SetVisible(false);
        }

        public void Bind(ItemPickupFlow p_pickupFlow)
        {
            Unbind();
            _pickupFlow = p_pickupFlow;

            if (_pickupFlow == null)
                return;

            _pickupFlow.OnInteractionAvailabilityChanged +=
                HandleInteractionAvailabilityChanged;

            SetVisible(_pickupFlow.HasCandidate);
        }

        public void Unbind()
        {
            if (_pickupFlow != null)
            {
                _pickupFlow.OnInteractionAvailabilityChanged -=
                    HandleInteractionAvailabilityChanged;
            }

            _pickupFlow = null;
            SetVisible(false);
        }

        private void HandleInteractionAvailabilityChanged(bool p_isAvailable)
        {
            SetVisible(p_isAvailable);
        }

        private void SetVisible(bool p_isVisible)
        {
            if (_content != null && _content.activeSelf != p_isVisible)
                _content.SetActive(p_isVisible);
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
