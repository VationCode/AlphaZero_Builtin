using UnityEngine;

public abstract class UIPanel : MonoBehaviour
{
    [SerializeField] private Animation anim;

    private void Awake()
    {
        TryGetComponent<Animation>(out anim);
    }
    public virtual void Show()
    {
        if(anim != null)
            anim.Play();
        gameObject.SetActive(true);
    }

    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }
}
