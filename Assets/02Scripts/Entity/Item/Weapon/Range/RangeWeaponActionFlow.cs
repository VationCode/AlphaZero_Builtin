namespace Alpha.Item.Weapon.Range
{
    internal enum ERangeWeaponActionResult
    {
        Running,
        Completed
    }

    // Trigger와 Secondary 자식 Flow를 조정하고 행동 결과만 부모에 반환한다.
    internal sealed class RangeWeaponActionFlow
    {
        private readonly RangeWeaponTriggerFlow _triggerFlow = new();
        private readonly RangeWeaponSecondaryFlow _secondaryFlow = new();

        private RangeWeaponAttackModule _attackModule;
        private bool _isChargedPrimaryAction;

        public ERangeTriggerMode CurrentTriggerMode =>
            _triggerFlow.CurrentMode;
        public bool DidFireDuringPrimaryAction { get; private set; }
        public bool IsSecondaryActive => _secondaryFlow.IsActive;
        public bool IsChargeEnabled => _secondaryFlow.IsChargeEnabled;
        public float ChargeRatio => _secondaryFlow.ChargeRatio;

        public void Bind(
            RangeWeaponSettings p_settings,
            RangeWeaponAttackModule p_attackModule)
        {
            _attackModule = p_attackModule;
            _triggerFlow.Bind(p_settings);
            _secondaryFlow.Bind(p_settings?.SecondarySettings);
            ResetRuntimeState();
        }

        public void Reset()
        {
            _triggerFlow.Reset();
            _secondaryFlow.Reset();
            ResetRuntimeState();
        }

        public bool TrySwitchTriggerMode(
            bool p_hasUseContext,
            bool p_hasActiveWeaponAction)
        {
            return _triggerFlow.TrySwitchMode(
                p_hasUseContext && !p_hasActiveWeaponAction);
        }

        public bool TryBeginAction(
            EWeaponActionType p_type,
            bool p_hasUseContext)
        {
            if (p_type != EWeaponActionType.Primary ||
                !p_hasUseContext ||
                _attackModule == null)
            {
                return false;
            }

            DidFireDuringPrimaryAction = false;
            _isChargedPrimaryAction =
                _secondaryFlow.IsChargeEnabled &&
                _secondaryFlow.IsActive;

            if (_isChargedPrimaryAction)
            {
                // Auto는 쿨다운 중 입력을 유지하고 준비되는 즉시 발사한다.
                if (!_triggerFlow.IsFireReady)
                    return CurrentTriggerMode == ERangeTriggerMode.Auto;

                return TryFireChargedPrimary();
            }

            if (!_triggerFlow.IsFireReady)
                return CurrentTriggerMode == ERangeTriggerMode.Auto;

            if (!TryFire())
                return false;

            DidFireDuringPrimaryAction = true;
            return true;
        }

        public ERangeWeaponActionResult TickAction(
            EWeaponActionType p_type)
        {
            if (p_type != EWeaponActionType.Primary ||
                _attackModule == null)
            {
                return ERangeWeaponActionResult.Completed;
            }

            if (_isChargedPrimaryAction)
                return TickChargedPrimary();

            if (CurrentTriggerMode == ERangeTriggerMode.Semi)
                return ERangeWeaponActionResult.Completed;

            if (!_triggerFlow.IsFireReady)
                return ERangeWeaponActionResult.Running;

            if (!TryFire())
                return ERangeWeaponActionResult.Completed;

            DidFireDuringPrimaryAction = true;
            return ERangeWeaponActionResult.Running;
        }

        public bool BeginSecondary(
            bool p_hasUseContext,
            bool p_hasSecondaryAction,
            bool p_hasActiveWeaponAction)
        {
            return p_hasUseContext &&
                   _secondaryFlow.Begin(
                       p_hasSecondaryAction,
                       p_hasActiveWeaponAction);
        }

        public void TickSecondary(float p_deltaTime)
        {
            _secondaryFlow.Tick(p_deltaTime);
        }

        public void CancelSecondary()
        {
            _secondaryFlow.Cancel();
        }

        public void EndAction(EWeaponActionType p_type)
        {
            if (p_type == EWeaponActionType.Primary)
                _isChargedPrimaryAction = false;
        }

        public void CancelAction(EWeaponActionType p_type)
        {
            EndAction(p_type);
        }

        private ERangeWeaponActionResult TickChargedPrimary()
        {
            if (!_secondaryFlow.IsActive ||
                CurrentTriggerMode == ERangeTriggerMode.Semi)
            {
                return ERangeWeaponActionResult.Completed;
            }

            if (!_triggerFlow.IsFireReady)
                return ERangeWeaponActionResult.Running;

            return TryFireChargedPrimary()
                ? ERangeWeaponActionResult.Running
                : ERangeWeaponActionResult.Completed;
        }

        private bool TryFireChargedPrimary()
        {
            if (!_secondaryFlow.IsActive || !_triggerFlow.IsFireReady)
                return false;

            if (!TryFire(_secondaryFlow.CalculateBonusDamage()))
                return false;

            _secondaryFlow.ResetChargeAfterFire();
            DidFireDuringPrimaryAction = true;
            return true;
        }

        private bool TryFire(float p_bonusDamage = 0f)
        {
            return _triggerFlow.TryFire(
                p_bonusDamage,
                _secondaryFlow.IsAimViewActive,
                _attackModule.TryFire);
        }

        private void ResetRuntimeState()
        {
            _isChargedPrimaryAction = false;
            DidFireDuringPrimaryAction = false;
        }
    }
}
