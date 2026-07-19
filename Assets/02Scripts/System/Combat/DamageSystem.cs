using UnityEngine;

public class DamageSystem
{
    public bool TryApply(Collider p_targetCollider, DamageInfo p_damageInfo)
    {
        if (p_targetCollider == null ||
            p_damageInfo.Amount <= 0f)
        {
            return false;
        }

        MonoBehaviour[] behaviours = p_targetCollider.GetComponentsInParent<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IDamageable damageable)
            {
                return damageable.TryTakeDamage(p_damageInfo);
            }
        }

        return false;
    }
}
