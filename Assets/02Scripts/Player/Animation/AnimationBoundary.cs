using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimationBoundary : MonoBehaviour
{
    private Animator _anim;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
    }

    public void MoveAnim(Vector2 p_moveInput, bool p_isCombat = false)
    {
        if (!p_isCombat) 
            _anim.SetFloat("MoveMagnitude", p_moveInput.sqrMagnitude);
        else
        {
            _anim.SetFloat("InputX", p_moveInput.x);
            _anim.SetFloat("InputY", p_moveInput.y);
        }
    }
}
