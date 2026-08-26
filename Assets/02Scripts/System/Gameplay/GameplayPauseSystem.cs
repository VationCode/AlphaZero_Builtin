using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Gameplay
{
    // 여러 요청자의 정지를 중첩 관리하고 마지막 요청이 끝날 때 게임 시간을 복구한다.
    [DisallowMultipleComponent]
    public sealed class GameplayPauseSystem : MonoBehaviour
    {
        private readonly HashSet<object> _owners = new();

        private AlphaInputSystem _input;
        private float _previousTimeScale = 1f;

        public bool IsPaused => _owners.Count > 0;

        public void Bind(AlphaInputSystem p_input)
        {
            _input = p_input;
        }

        public bool Acquire(object p_owner)
        {
            if (p_owner == null || !_owners.Add(p_owner))
                return false;

            if (_owners.Count > 1)
                return true;

            _previousTimeScale = Time.timeScale;
            _input?.BeginGameplayInputBlock(this);
            Time.timeScale = 0f;
            return true;
        }

        public bool Release(object p_owner)
        {
            if (p_owner == null || !_owners.Remove(p_owner))
                return false;

            if (_owners.Count > 0)
                return true;

            RestoreGameplay();
            return true;
        }

        private void RestoreGameplay()
        {
            Time.timeScale = _previousTimeScale;
            _input?.EndGameplayInputBlock(this);
        }

        private void OnDisable()
        {
            if (_owners.Count == 0)
                return;

            _owners.Clear();
            RestoreGameplay();
        }
    }
}
