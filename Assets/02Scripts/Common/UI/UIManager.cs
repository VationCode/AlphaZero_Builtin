using UnityEngine;

namespace Alpha.UI
{
    // Scene의 공용 HUD View 참조를 Installer에 제공한다.
    public class UIManager : MonoBehaviour
    {
        public CrossHairUI CrossHairUI;
        public StateUI StateUI;
        public InteractionView InteractionUI;
    }
}
