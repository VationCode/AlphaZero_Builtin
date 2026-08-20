using UnityEngine;

namespace Alpha.Projectile.View
{
    // 선택한 Projectile Prefab의 Radial 피해 범위를 Scene View에 표현한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Projectile))]
    public sealed class ProjectileImpactAreaPreviewView : MonoBehaviour
    {
        [SerializeField]
        private Projectile _projectile;

        [Header("Preview")]
        [SerializeField]
        private bool _showDamageRadius = true;

        [SerializeField]
        private Color _damageRadiusColor =
            new(1f, 0.25f, 0.1f, 0.2f);

        private void Reset()
        {
            _projectile = GetComponent<Projectile>();
        }

        private void OnValidate()
        {
            _projectile ??= GetComponent<Projectile>();
        }

        private void OnDrawGizmosSelected()
        {
            if (!_showDamageRadius)
                return;

            _projectile ??= GetComponent<Projectile>();

            if (_projectile == null ||
                !_projectile.ImpactSettings.IsRadial ||
                _projectile.ImpactSettings.DamageRadius <= 0f)
            {
                return;
            }

            Color previousColor = Gizmos.color;
            float radius = _projectile.ImpactSettings.DamageRadius;

            Gizmos.color = _damageRadiusColor;
            Gizmos.DrawSphere(transform.position, radius);

            Color wireColor = _damageRadiusColor;
            wireColor.a = 1f;

            Gizmos.color = wireColor;
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.color = previousColor;
        }
    }
}
