using UnityEngine;

// Animation을 선택적으로 재생하며 Panel의 활성 상태를 제어하는 공통 View이다.
public abstract class UIPanel : MonoBehaviour
{
    [SerializeField] private Animation anim;

    // 같은 GameObject의 선택적 Animation 컴포넌트를 캐시한다.
    private void Awake()
    {
        TryGetComponent<Animation>(out anim);
    }
    // 열림 Animation을 재생하고 Panel을 활성화한다.
    public virtual void Open()
    {
        if(anim != null)
            anim.Play();
        gameObject.SetActive(true);
    }

    // Panel GameObject를 비활성화한다.
    public virtual void Close()
    {
        gameObject.SetActive(false);
    }
}
