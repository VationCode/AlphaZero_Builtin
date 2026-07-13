using UnityEngine;

public abstract class ViewStateBase
{
    protected CameraCore _Core;

    public virtual void Initialize(CameraCore p_core)
    {
        _Core = p_core;
    }

    public abstract void Enter();

    public abstract void LateUpdate();

    public abstract void Exit();

    public abstract Vector3 GetLookDirection();
}