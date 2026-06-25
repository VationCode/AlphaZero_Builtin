using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimationBoundary : MonoBehaviour
{
    [SerializeField] private Animator _flyUpDownAnim;

    private Animator _anim;
    private int _isSprint = Animator.StringToHash("IsSprint");
    private int _isIncombat = Animator.StringToHash("IsInCombat");
    private int _isGround = Animator.StringToHash("IsGround");
    private void Awake()
    {
        _anim = GetComponent<Animator>();
    }

    public void MoveAnim(Vector2 p_moveInput, bool p_isSprint, bool p_isCombat = false)
    {
        _anim.SetBool(_isSprint, p_isSprint);
        _anim.SetBool(_isIncombat, p_isCombat);

        if (p_isCombat)
        {
            _anim.SetFloat("InputX", p_moveInput.x, 0.1f, Time.deltaTime);
            _anim.SetFloat("InputY", p_moveInput.y, 0.1f, Time.deltaTime);
        }
        else
            _anim.SetFloat("MoveMagnitude", p_moveInput.sqrMagnitude, 0.1f, Time.deltaTime);
    }

    public Vector3 GetAnimationInput(Transform playerTr,Vector3 p_moveDir)
    {
        // InverseTransformDirection : 입력값에 대해 플레이어 기준에서으로 방향값을 변환해줌
        // 전투시 오른쪽 보고 있을 때 W키 눌러 위로 가면 캐릭터 기준 왼쪽으로 이동이기에 그값으로 만들어준다는것
        Vector3 localMove = playerTr.InverseTransformDirection(p_moveDir);
        localMove.y = 0f;

        return localMove;
    }


    public void JumpAnim()
    {
        _anim.SetTrigger("Jump");
    }

    public void DashAnim()
    {
        _anim.SetTrigger("Dash");
    }
    public void IsDashingAnim(bool p_isDashing)
    {
        _anim.SetBool("IsDashing", p_isDashing);
    }

    public void IsGround(bool p_isGround)
    {
        _anim.SetBool(_isGround, p_isGround);
    }

    public void FlyUp()
    {

    }

    public void IsFlyUpDownPos(bool p_isFly)
    {
        _flyUpDownAnim.SetBool("IsFly", p_isFly);
    }
}
