using UnityEngine;

public abstract class ViewBase : MonoBehaviour
{
    public Animation m_UIOpenAnim;

    internal void Open()
    {
        if (m_UIOpenAnim)
        {
            this.m_UIOpenAnim.Play();
        }
    }
    internal void Close()
    {

    }
}
