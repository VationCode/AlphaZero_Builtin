using UnityEngine;

namespace Alpha.Boss
{
    // Cinematic 전후의 Boss 타겟 연결과 무적 상태를 실행한다.
    [DisallowMultipleComponent]
    public sealed class BossCinematicModule : MonoBehaviour
    {
        private BossCore _boss;
        private bool _ownsInvulnerability;

        public bool Bind(BossCore p_boss)
        {
            _boss = p_boss;
            return _boss != null;
        }

        // Animator는 건드리지 않고 연출 전 대기 조건만 준비한다.
        public bool PrepareWaitingForCinematic()
        {
            if (_boss == null)
                return false;

            _boss.ClearTarget();

            if (!_ownsInvulnerability)
            {
                _ownsInvulnerability =
                    _boss.DamageReceiver?.BeginInvulnerability(this) == true;
            }

            return true;
        }

        public bool PrepareCinematic()
        {
            return _boss != null &&
                   _boss.AnimationView?.Animator != null;
        }

        public void BeginCombat()
        {
            ReleaseInvulnerability();
        }

        public bool ReturnToWaitingForCinematic()
        {
            return PrepareWaitingForCinematic();
        }

        public void Release()
        {
            ReleaseInvulnerability();
        }

        private void ReleaseInvulnerability()
        {
            if (_ownsInvulnerability)
            {
                _boss?.DamageReceiver?.EndInvulnerability(this);
                _ownsInvulnerability = false;
            }
        }

        private void OnDisable()
        {
            Release();
        }
    }
}
