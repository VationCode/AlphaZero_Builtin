using UnityEngine;

namespace Alpha.UI
{
    // GameObject 활성 상태와 선택적 열림 Animation을 제어하는 공통 UI View이다.
    public class ViewBase : MonoBehaviour
    {
        [SerializeField]
        private Animation _openAnimation;

        public bool IsOpen => gameObject.activeSelf;

        // 같은 GameObject의 선택적 열림 Animation을 캐시한다.
        private void Awake()
        {
            TryGetComponent<Animation>(out _openAnimation);
        }

        // View를 활성화하고 열림 Animation이 있으면 재생한다.
        internal virtual void Open()
        {
            gameObject.SetActive(true);

            if (_openAnimation != null)
                _openAnimation.Play();
        }

        // View GameObject를 비활성화한다.
        internal virtual void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
