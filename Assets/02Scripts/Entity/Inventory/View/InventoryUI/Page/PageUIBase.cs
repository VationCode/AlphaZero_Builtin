using UnityEngine;

public abstract class PageUIBase : MonoBehaviour
{
    public Animation m_UIOpenAnim;

    public void OnOpen()
    {
        if (m_UIOpenAnim)
        {
            m_UIOpenAnim.Play();
        }
    }
    public void OnClose()
    {

    }
}
