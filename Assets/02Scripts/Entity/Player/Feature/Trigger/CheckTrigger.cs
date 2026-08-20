using System;
using System.Collections.Generic;
using Alpha.Combat;
using Alpha.Player.Health;
using UnityEngine;

public class CheckTrigger : MonoBehaviour
{
    private HealthModule _healthModule;

    private readonly Dictionary<int, int> _lastReceivedAttackIds = new();

    public event Action<DamageInfo> OnDamageReceived;

    public void Bind(HealthModule p_healthModule)
    {
        _healthModule = p_healthModule;
    }

    private void OnTriggerEnter(Collider p_other)
    {
        if (_healthModule == null)
            return;

        IDamageSource damageSource = p_other.GetComponentInParent<IDamageSource>();
        if (damageSource == null || damageSource.AttackId <= 0)
            return;

        if (_lastReceivedAttackIds.TryGetValue(
                damageSource.SourceId,
                out int lastAttackId) &&
            lastAttackId == damageSource.AttackId)
        {
            return;
        }

        if (!damageSource.TryCreateDamageInfo(transform, out DamageInfo damageInfo))
            return;

        if (!_healthModule.TryApplyDamage(damageInfo))
            return;

        _lastReceivedAttackIds[damageSource.SourceId] = damageSource.AttackId;
        OnDamageReceived?.Invoke(damageInfo);
    }

    private void OnDisable()
    {
        _lastReceivedAttackIds.Clear();
    }
}
