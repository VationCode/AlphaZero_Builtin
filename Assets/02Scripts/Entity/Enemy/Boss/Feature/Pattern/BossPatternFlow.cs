using System;
using UnityEngine;

namespace Alpha.Boss
{
    // 개별 공격을 알지 않고 실행 계약만 호출한다. 콜백 도중 중단은 콜백 반환 후 정리한다.
    public sealed class BossPatternFlow
    {
        private readonly BossContext _context;
        private BossPattern _current;
        private bool _busy;
        private bool _cancelRequested;
        public bool IsRunning => _current != null;
        public float ElapsedTime => _current?.Runtime.ElapsedTime ?? 0f;

        public BossPatternFlow(BossContext p_context)
        {
            _context = p_context ?? throw new ArgumentNullException(nameof(p_context));
        }

        public bool TryStart(BossPattern p_pattern)
        {
            if (_busy || IsRunning || p_pattern == null || p_pattern.IsOwned) return false;
            _busy = true;
            _cancelRequested = false;
            // CanExecute에서도 같은 인스턴스를 다른 실행기에 재진입시키지 않는다.
            p_pattern.IsOwned = true;
            p_pattern.CancellationRequested = false;
            try
            {
                if (!p_pattern.CanStart(_context) || _cancelRequested) return false;
                _current = p_pattern;
                p_pattern.Runtime.Begin(_context.Target, _context.ElapsedTime, p_pattern.Settings.Cooldown);
                p_pattern.Enter(_context);
                return true;
            }
            catch (Exception exception)
            {
                _cancelRequested = true;
                Debug.LogException(exception);
                return false;
            }
            finally
            {
                _busy = false;
                if (_current == null) p_pattern.IsOwned = false;
                Settle();
            }
        }

        public void Tick(float p_deltaTime)
        {
            if (_busy || !IsRunning || p_deltaTime <= 0f || float.IsNaN(p_deltaTime) || float.IsInfinity(p_deltaTime)) return;
            _busy = true;
            try
            {
                if (!_current.IsFinished && !_cancelRequested)
                {
                    _current.Runtime.Advance(p_deltaTime);
                    _current.Update(p_deltaTime);
                }
            }
            catch (Exception exception) { _cancelRequested = true; Debug.LogException(exception); }
            finally { _busy = false; Settle(); }
        }

        public void Cancel()
        {
            _cancelRequested = true;
            if (_current != null) _current.CancellationRequested = true;
            if (!_busy) Settle();
        }

        private void Settle()
        {
            if (_busy || !IsRunning) return;
            bool finished = false;
            _busy = true;
            try { if (!_cancelRequested) finished = _current.IsFinished; }
            catch (Exception exception) { _cancelRequested = true; Debug.LogException(exception); }
            finally { _busy = false; }
            if (_cancelRequested || finished) Finish(_cancelRequested);
        }

        private void Finish(bool p_cancelled)
        {
            BossPattern pattern = _current;
            _current = null;
            _busy = true;
            try
            {
                if (p_cancelled)
                {
                    try { pattern.Cancel(); }
                    catch (Exception exception) { Debug.LogException(exception); }
                }
                try { pattern.Exit(); }
                catch (Exception exception) { Debug.LogException(exception); }
            }
            finally
            {
                pattern.Runtime.Finish(p_cancelled);
                pattern.IsOwned = false;
                pattern.CancellationRequested = false;
                _cancelRequested = false;
                _busy = false;
            }
        }
    }
}
