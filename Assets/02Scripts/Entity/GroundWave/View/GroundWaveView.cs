using UnityEngine;

namespace Alpha.GroundWave.View
{
    // 지면파의 실제 폭발 위치를 받아 독립적인 One-shot Particle만 표현한다.
    [RequireComponent(typeof(GroundWave))]
    public sealed class GroundWaveView : MonoBehaviour
    {
        [SerializeField] private GameObject _eruptionEffectPrefab;
        [SerializeField, Min(0.1f)] private float _effectLifetime = 4f;
        private GroundWave _wave;

        private void OnEnable()
        {
            _wave = GetComponent<GroundWave>();
            _wave.OnErupted += HandleErupted;
        }

        private void OnDisable()
        {
            if (_wave != null)
                _wave.OnErupted -= HandleErupted;
        }

        private void HandleErupted(Vector3 p_point, Vector3 p_normal)
        {
            if (_eruptionEffectPrefab == null)
                return;
            GameObject effect = Instantiate(_eruptionEffectPrefab, p_point,
                Quaternion.FromToRotation(Vector3.up, p_normal));
            effect.SetActive(true);
            foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = particle.main;
                main.loop = false;
                particle.Play(false);
            }
            Destroy(effect, Mathf.Max(0.1f, _effectLifetime));
        }
    }
}
