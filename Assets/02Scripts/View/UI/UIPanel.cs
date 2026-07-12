using UnityEngine;

public abstract class UIPanel : MonoBehaviour
{
    [SerializeField] private Animation anim;

    private void Awake()
    {
        TryGetComponent<Animation>(out anim);
    }
    public virtual void Open()
    {
        if(anim != null)
            anim.Play();
        gameObject.SetActive(true);
    }

    public virtual void Close()
    {
        gameObject.SetActive(false);
    }
}
