using UnityEngine;

// Pointer Hover 상태에 따라 Button 강조 이미지를 표시한다.
public class ButtonEffect : MonoBehaviour
{
    [SerializeField] private GameObject HoverImage;

    // 시작 시 Hover 이미지를 숨긴다.
    private void Awake()
    {
        OnHoverExit();
    }

    // Pointer 진입 시 Hover 이미지를 표시한다.
    public void OnHoverEnter()
    {
        HoverImage.SetActive(true);
    }

    // Pointer 이탈 시 Hover 이미지를 숨긴다.
    public void OnHoverExit()
    {
        HoverImage.SetActive(false);
    }
}
