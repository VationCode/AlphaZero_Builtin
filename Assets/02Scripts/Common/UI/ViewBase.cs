using UnityEngine;

namespace Alpha.UI
{
    public abstract class ViewBase : MonoBehaviour
    {
        [SerializeField] private Animation _openAnimation;

        private void Awake()
        {
            _openAnimation = GetComponent<Animation>();
        }

        internal void Open()
        {
            gameObject.SetActive(true);

            if (_openAnimation != null)
                _openAnimation.Play();
        }

        internal void Close()
        {
            gameObject.SetActive(false);
        }
    }
}