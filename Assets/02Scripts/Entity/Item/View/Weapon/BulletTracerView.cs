using UnityEngine;

namespace Alpha.Item.Weapon.View
{
    // Hitscan의 시작점부터 판정 끝점까지 Tracer를 이동시켜 표현한다.
    [RequireComponent(typeof(TrailRenderer))]
    public sealed class BulletTracerView : MonoBehaviour
    {
        [SerializeField, Min(0.01f)]
        private float _speed = 200f;

        private TrailRenderer _trail;
        private Vector3 _endPoint;
        private bool _isPlaying;

        private void Awake()
        {
            _trail = GetComponent<TrailRenderer>();
        }

        public void Play(
            Vector3 p_startPoint,
            Vector3 p_endPoint)
        {
            transform.position = p_startPoint;
            _endPoint = p_endPoint;

            _trail.Clear();
            _trail.emitting = true;
            _isPlaying = true;
        }

        private void Update()
        {
            if (!_isPlaying)
                return;

            transform.position = Vector3.MoveTowards(
                transform.position,
                _endPoint,
                _speed * Time.deltaTime);

            if ((transform.position - _endPoint).sqrMagnitude <= 0.0001f)
                Complete();
        }

        private void Complete()
        {
            _isPlaying = false;
            _trail.emitting = false;

            // 잔상이 사라진 뒤 표현 객체를 정리한다.
            Destroy(
                gameObject,
                Mathf.Max(_trail.time, 0.01f));
        }
    }
}
