using UnityEngine;

namespace Alpha.Projectile
{
    // 탄종의 Prefab, 비행 특성, 명중 방식을 하나의 원본 데이터로 보관한다.
    [CreateAssetMenu(
        fileName = "ProjectileDefinition",
        menuName = "Alpha/Combat/Projectile Definition")]
    public sealed class ProjectileDefinition : ScriptableObject
    {
        [Tooltip("이 탄종이 생성할 Projectile Entity Prefab입니다.")]
        [SerializeField]
        private Projectile _prefab;

        [Tooltip("Physics Gravity에 곱할 중력 배율입니다. 0이면 직선으로 이동합니다.")]
        [SerializeField, Min(0f)]
        private float _gravityScale;

        [SerializeField]
        private ProjectileImpactSettings _impactSettings = new(
            EProjectileImpactType.Direct,
            0f);

        public Projectile Prefab => _prefab;
        public float GravityScale => Mathf.Max(0f, _gravityScale);
        public Vector3 Gravity => Physics.gravity * GravityScale;
        public ProjectileImpactSettings ImpactSettings =>
            _impactSettings;

        public bool IsValid =>
            _prefab != null &&
            _prefab.IsConfigurationValid &&
            _impactSettings.IsValid;

        private void OnValidate()
        {
            _gravityScale = Mathf.Max(0f, _gravityScale);
            _impactSettings.Validate();
        }
    }
}
