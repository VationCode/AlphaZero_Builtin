using UnityEngine;
using UnityEngine.Windows;

[RequireComponent(typeof(Animator))]
public class AnimationBoundary : MonoBehaviour
{
    [SerializeField] private Animator _flyUpDownAnim;

    private Animator _anim;
    private int _isSprint = Animator.StringToHash("IsSprint");
    private int _isIncombat = Animator.StringToHash("IsInCombat");
    private int _isGround = Animator.StringToHash("IsGround");

    private Transform _playerTr;
    private void Awake()
    {
        _anim = GetComponent<Animator>();
    }

    public void Bind(Transform p_playerTr)
    {
        _playerTr = p_playerTr;
    }

    // 비전투 : 그냥 입력키값
    // 전투 : 플레이어 현재 방향 기준 좌표
    public void MoveAnim(Vector3 p_moveDir, bool p_isSprint, bool p_isCombat = false)
    {
        _anim.SetBool(_isSprint, p_isSprint);
        _anim.SetBool(_isIncombat, p_isCombat);

        if (!p_isCombat)
        {
            _anim.SetFloat("MoveMagnitude", p_moveDir.sqrMagnitude, 0.1f, Time.deltaTime);
            return;
        }

        // 현재 플레이어가 바라본 방향에서의 입력키를 계산
        var animDir = CalculateTransformPosInput(p_moveDir);

        _anim.SetFloat("InputX", animDir.x, 0.1f, Time.deltaTime);
        _anim.SetFloat("InputY", animDir.z, 0.1f, Time.deltaTime);
    }

    // 현재 입력받은 값을 Transform 방향 기준에 맞게 변경
    public Vector3 CalculateTransformPosInput(Vector3 p_moveDir)
    {
        // InverseTransformDirection : 입력값에 대해 플레이어 기준에서으로 방향값을 변환해줌
        // 전투시 오른쪽 보고 있을 때 W키 눌러 위로 가면 캐릭터 기준 왼쪽으로 이동이기에 그값으로 만들어준다는것
        Vector3 localDir = _playerTr.InverseTransformDirection(p_moveDir);

        return localDir;
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
