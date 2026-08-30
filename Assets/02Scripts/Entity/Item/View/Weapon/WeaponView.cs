using Alpha.AlphaCamera;
using UnityEngine;

namespace Alpha.Item.Weapon.View
{
    // 장착 Entity가 무기 표현에 외부 View 의존성을 전달하는 공통 진입점이다.
    public abstract class WeaponView : MonoBehaviour
    {
        public virtual void BindCamera(CameraCore p_cameraCore) { }
    }
}
