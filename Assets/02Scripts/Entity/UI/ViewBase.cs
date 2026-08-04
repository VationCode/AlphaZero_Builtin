using UnityEngine;

namespace Alpha.UI
{
    public class ViewBase : MonoBehaviour
    {
        [SerializeField]
        private Animation _openAnimation;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            TryGetComponent<Animation>(out _openAnimation);
        }

        internal virtual void Open()
        {
            gameObject.SetActive(true);

            if (_openAnimation != null)
                _openAnimation.Play();
        }

        internal virtual void Close()
        {
            gameObject.SetActive(false);
        }
    }
}