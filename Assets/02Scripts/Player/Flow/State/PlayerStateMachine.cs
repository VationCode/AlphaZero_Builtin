using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public enum ELocomotionType
{
    GroundMove,
    Jump,
    Fall,
    Land,
    Dash
}

public enum ECombatType
{
    Normal,
    Combat,
    Aim,
    Attack
}

public class PlayerStateMachine : MonoBehaviour
{
    private UIModule _uiModule;

    private BaseState _locoState;
    private BaseState _combatState;

    private Dictionary<ELocomotionType, BaseState> _locoCreateDic;
    private Dictionary<ECombatType, BaseState> _combatCreateDic;

    public ELocomotionType? CurrentLoco => _currentLocoType;
    private ELocomotionType? _currentLocoType;

    public ECombatType? CurrentCombat=> _currentCombatType;
    private ECombatType? _currentCombatType;

    private void Awake()
    {
        _locoCreateDic = new()
        {
            { ELocomotionType.GroundMove, new GroundMoveState() },
            { ELocomotionType.Jump, new JumpState() },
            { ELocomotionType.Fall, new FallState() },
            { ELocomotionType.Land, new LandState() },
            { ELocomotionType.Dash, new DashState() }
        };

        _combatCreateDic = new()
        {
            {ECombatType.Normal, new NormalState() },
            {ECombatType.Combat, new CombatState() },
            {ECombatType.Aim, new AimState() },
            {ECombatType.Attack, new AttackState() },
        };
    }

    public void Bind(PlayerCore p_core)
    {
        _uiModule = p_core.UIModule;

        foreach (var state in _locoCreateDic.Values)
        {
            state.Initialize(p_core);
        }

        foreach (var state in _combatCreateDic.Values)
        {
            state.Initialize(p_core);
        }

        ChangeLocoState(ELocomotionType.GroundMove);

        ChangeCombatState(ECombatType.Normal);
    }


    private void Update()
    {
        _locoState?.Update();

        _combatState?.Update();
    }

    public void ChangeLocoState(ELocomotionType p_newState)
    {
        if (_currentLocoType.HasValue && _currentLocoType.Value == p_newState)
            return;

        _locoState?.Exit();

        _locoState = _locoCreateDic[p_newState];

        _currentLocoType = p_newState;

        _locoState.Enter();

        _uiModule.ChangeLocoText($"{_currentLocoType}");
    }

    public void ChangeCombatState(ECombatType p_newState)
    {
        if (_currentCombatType.HasValue && _currentCombatType.Value == p_newState)
            return;

        _combatState?.Exit();

        _combatState = _combatCreateDic[p_newState];

        _currentCombatType = p_newState;

        _combatState.Enter();

        _uiModule.ChangeCombatText($"{_currentCombatType}");;
    }
}
