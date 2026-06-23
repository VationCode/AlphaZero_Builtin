using alpha.camera;
using UnityEngine;

public class CombatModule : MonoBehaviour
{
    private CameraModule _cameraModule;

    public void Bind(PlayerCore p_core)
    {
        _cameraModule = p_core.CameraModule;
    }

    public void Aim(bool p_isAim)
    {
        if(p_isAim)
            _cameraModule.SetView(EViewType.ShoulderView);
        else
            _cameraModule.SetView(EViewType.BackView);
    }
}
