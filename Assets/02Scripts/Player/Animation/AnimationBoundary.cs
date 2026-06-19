using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimationBoundary : MonoBehaviour
{
    private Animator _anim;

    private int _isSprint = Animator.StringToHash("IsSprint");
    private int _isIncombat = Animator.StringToHash("IsInCombat");
    private void Awake()
    {
        _anim = GetComponent<Animator>();
    }

    public void MoveAnim(Vector2 p_moveInput, bool p_isSprint, bool p_isCombat = false)
    {
        _anim.SetBool(_isSprint, p_isSprint);
        _anim.SetBool(_isIncombat, p_isCombat);

        if (!p_isCombat) 
            _anim.SetFloat("MoveMagnitude", p_moveInput.sqrMagnitude, 0.1f, Time.deltaTime);
        else
        {
            _anim.SetFloat("InputX", p_moveInput.x, 0.1f, Time.deltaTime);
            _anim.SetFloat("InputY", p_moveInput.y, 0.1f, Time.deltaTime);
        }
    }
}
