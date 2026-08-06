// IWeaponAction 구현체가 제공해야 하는 기능 계약을 정의한다.
public interface IWeaponAction
{
    // 쿨타임, 상태, 자원 등을 확인하고 행동을 시작한다.
    bool TryBegin(WeaponActionContext p_context);

    // 조준 유지나 차지 시간처럼 진행 중인 행동을 갱신한다.
    void Tick(WeaponActionContext p_context, float p_deltaTime);

    // 입력을 정상적으로 해제했을 때 행동을 종료한다.
    void End(WeaponActionContext p_context);

    // 피격, 사망, 무기 교체 등으로 행동을 강제 중단한다.
    void Cancel(WeaponActionContext p_context);
}
