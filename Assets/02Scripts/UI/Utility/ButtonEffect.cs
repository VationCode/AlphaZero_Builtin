using UnityEngine;

public class ButtonEffect : MonoBehaviour
{
    [SerializeField] private GameObject HoverImage;

    private void Awake()
    {
        OnHoverExit();
    }

    public void OnHoverEnter()
    {
        HoverImage.SetActive(true);
    }

    public void OnHoverExit()
    {
        HoverImage.SetActive(false);
    }
}
