using System;

namespace Alpha.Enemy.CrabBoss.Combat
{
    [Serializable]
    public abstract class BossAttackStrategy
    {
        public abstract EAttackPattern Pattern { get; }
        public bool IsComplete { get; protected set; } = true;

        public abstract bool Begin(
            CrabBossContext p_context,
            CrabBossLocomotionModule p_locomotion);

        public abstract void Tick(float p_deltaTime);

        public virtual void Complete()
        {
            IsComplete = true;
        }

        public virtual void Cancel()
        {
            IsComplete = true;
        }
    }
}
