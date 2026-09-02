using UnityEngine;

namespace Alpha.Enemy.View
{
    // 애니메이션 본 Collider를 물리 충돌에서 분리해 루트 Collider를 안정적으로 유지한다.
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class EnemyStableBodyColliderView : MonoBehaviour
    {
        [Tooltip("물리 충돌에서 분리할 애니메이션 모델의 루트")]
        [SerializeField]
        private Transform _animatedColliderRoot;

        [Tooltip("지면 충돌을 전담할 Enemy 루트의 고정 Collider")]
        [SerializeField]
        private BoxCollider _stableBodyCollider;

        private void Awake()
        {
            DetachAnimatedCollidersFromPhysics();
        }

        // 본 Collider는 피격 Trigger로 유지하고 실제 물리 충돌은 루트에 맡긴다.
        private void DetachAnimatedCollidersFromPhysics()
        {
            _stableBodyCollider ??= GetComponent<BoxCollider>();

            if (_animatedColliderRoot == null || _stableBodyCollider == null)
            {
                Debug.LogWarning(
                    $"[{name}] 고정 Body Collider에 사용할 모델과 BoxCollider가 필요합니다.",
                    this);
                return;
            }

            Rigidbody body = GetComponent<Rigidbody>();
            Collider[] colliders =
                _animatedColliderRoot.GetComponentsInChildren<Collider>(true);

            foreach (Collider source in colliders)
            {
                if (source == null ||
                    source == _stableBodyCollider ||
                    !source.enabled ||
                    source.isTrigger ||
                    source.attachedRigidbody != body)
                {
                    continue;
                }

                source.isTrigger = true;
            }

            _stableBodyCollider.isTrigger = false;
            _stableBodyCollider.enabled = true;
        }

        private void Reset()
        {
            _stableBodyCollider = GetComponent<BoxCollider>();
        }

        private void OnValidate()
        {
            _stableBodyCollider ??= GetComponent<BoxCollider>();
        }
    }
}
