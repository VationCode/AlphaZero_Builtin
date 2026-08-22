using System;
using Alpha.Combat;
using UnityEngine;

[Serializable]
public sealed class DamageColliderGroup
{
    [SerializeField]
    private Collider[] _colliders = Array.Empty<Collider>();

    [SerializeField]
    private DamageProfile _profile = new DamageProfile();

    public DamageProfile Profile => _profile;

    // 등록된 공격 영역만 Trigger로 사용하고 시작 시 닫아 둔다.
    public void Initialize()
    {
        _profile ??= new DamageProfile();

        foreach (Collider damageCollider in _colliders)
        {
            if (damageCollider == null)
                continue;

            damageCollider.isTrigger = true;
            damageCollider.enabled = false;
        }
    }

    public void Open()
    {
        SetEnabled(true);
    }

    public void Close()
    {
        SetEnabled(false);
    }

    private void SetEnabled(bool p_enabled)
    {
        foreach (Collider damageCollider in _colliders)
        {
            if (damageCollider != null)
                damageCollider.enabled = p_enabled;
        }
    }

    public void Validate()
    {
        _profile?.Validate();
    }
}

public class DamageCollision : MonoBehaviour, IDamageSource
{
    [SerializeField]
    private Transform _attacker;

    [SerializeField]
    private DamageColliderGroup[] _meleeAreas = Array.Empty<DamageColliderGroup>();

    [SerializeField]
    private DamageColliderGroup _jumpAreas;

    [SerializeField]
    private DamageColliderGroup _rushAreas;

    private int _nextAttackId;
    private AttackSession _activeSession;
    private DamageColliderGroup _activeArea;
    private bool _hasActiveSession;

    public int SourceId => GetInstanceID();
    public int AttackId => _hasActiveSession ? _activeSession.AttackId : 0;

    private void Awake()
    {
        _attacker ??= transform;
        InitializeAreas();
    }

    // Entity Core가 실제 공격 소유자 Transform을 연결한다.
    public void Bind(Transform p_attacker)
    {
        if (p_attacker != null)
            _attacker = p_attacker;
    }

    // Animation Event 진입점
    public void OnMeleeArea(int p_num)
    {
        if (TryGetMeleeArea(p_num, out DamageColliderGroup area))
            OpenArea(area);
    }

    public void OffMeleeArea(int p_num)
    {
        if (TryGetMeleeArea(p_num, out DamageColliderGroup area))
            CloseArea(area);
    }

    public void OnJumpDamageArea()
    {
        OpenArea(_jumpAreas);
    }

    public void OffJumpDamageArea()
    {
        CloseArea(_jumpAreas);
    }

    public void OnRushDamageArea()
    {
        OpenArea(_rushAreas);
    }

    public void OffRushDamageArea()
    {
        CloseArea(_rushAreas);
    }

    // Player가 현재 공격 세션의 피해 정보를 요청할 때 호출한다.
    public bool TryCreateDamageInfo(
        Transform p_target,
        out DamageInfo p_damageInfo)
    {
        p_damageInfo = default;

        if (!_hasActiveSession ||
            p_target == null ||
            _activeSession.Attacker == null ||
            _activeSession.Profile == null ||
            !_activeSession.Profile.IsValid)
        {
            return false;
        }

        Vector3 direction = p_target.position - _activeSession.Attacker.position;
        if (direction.sqrMagnitude <= 0.0001f)
            direction = _activeSession.Attacker.forward;

        p_damageInfo = new DamageInfo(
            _activeSession.Attacker,
            _activeSession.Profile.Damage,
            p_target.position,
            -direction.normalized,
            direction,
            _activeSession.Profile.DamageType,
            _activeSession.Profile.HitReaction,
            _activeSession.Profile.KnockbackDistance,
            _activeSession.Profile.KnockbackDuration,
            EDamageDeliveryType.Melee);

        return p_damageInfo.IsValid;
    }

    // 공격 취소·피격·사망 시 안전하게 전부 닫는다.
    public void CloseAll()
    {
        foreach (DamageColliderGroup area in _meleeAreas)
        {
            area?.Close();
        }

        _jumpAreas?.Close();
        _rushAreas?.Close();

        _activeArea = null;
        _activeSession = default;
        _hasActiveSession = false;
    }

    private void OnDisable()
    {
        CloseAll();
    }

    private void OnValidate()
    {
        foreach (DamageColliderGroup area in _meleeAreas)
            area?.Validate();

        _jumpAreas?.Validate();
        _rushAreas?.Validate();
    }

    private void InitializeAreas()
    {
        foreach (DamageColliderGroup area in _meleeAreas)
            area?.Initialize();

        _jumpAreas?.Initialize();
        _rushAreas?.Initialize();

        CloseAll();
    }

    private void OpenArea(DamageColliderGroup p_area)
    {
        if (p_area == null || p_area.Profile == null || !p_area.Profile.IsValid)
            return;

        CloseArea(_activeArea);

        _nextAttackId = _nextAttackId == int.MaxValue
            ? 1
            : _nextAttackId + 1;

        _activeSession = new AttackSession(
            _nextAttackId,
            _attacker,
            p_area.Profile);
        _activeArea = p_area;
        _hasActiveSession = true;

        p_area.Open();
    }

    private void CloseArea(DamageColliderGroup p_area)
    {
        if (p_area == null)
            return;

        p_area.Close();

        if (!ReferenceEquals(_activeArea, p_area))
            return;

        _activeArea = null;
        _activeSession = default;
        _hasActiveSession = false;
    }

    private bool TryGetMeleeArea(
        int p_index,
        out DamageColliderGroup p_area)
    {
        p_area = null;

        if (p_index < 0 || p_index >= _meleeAreas.Length)
            return false;

        p_area = _meleeAreas[p_index];
        return p_area != null;
    }
}
