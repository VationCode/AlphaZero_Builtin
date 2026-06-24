using alpha.camera;
using UnityEngine;

public class CombatModule : MonoBehaviour
{
    private CameraCore _cameraCore;

    public void Bind(PlayerCore p_core)
    {
        _cameraCore = p_core.CameraCore;
    }

    public void Aim(bool p_isAim)
    {
        /*if(p_isAim)
            _cameraModule.SetView(EViewType.Aim);
        else
            _cameraModule.SetView(EViewType.ThirdPerson);*/
    }
}
