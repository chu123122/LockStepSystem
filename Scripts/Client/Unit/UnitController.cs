using System.Collections;
using System.Collections.Generic;
using Client;
using UnityEngine;

public enum UnitState
{
    Idle = 0,
    Move = 1,
}

public class UnitController : MonoBehaviour
{
    public float logicSpeed;
    public float smoothTime = 0.5f;
    private Vector3 _currentLogicPosition; // 当前逻辑帧的权威位置

    private Vector3 _previousLogicPosition; // 上一个逻辑帧的权威位置
    private Vector3 _targetPosition;
    private UnitState _unitState;

    private Vector3 _velocity = Vector3.zero;

    private void Awake()
    {
        _targetPosition = transform.position;
        _unitState = UnitState.Idle;
        logicSpeed = 30f;
    }

    private void Update()
    {
        Vector3 euler = transform.rotation.eulerAngles;
        euler.x = 0;
        euler.z = 0;
        transform.rotation = Quaternion.Euler(euler);
        
        transform.position = Vector3.SmoothDamp(
            transform.position,
            _currentLogicPosition,
            ref _velocity,
            smoothTime);
        // transform.position = Vector3.MoveTowards(
        //     transform.position, 
        //     _currentLogicPosition,
        //     smoothTime);
    }

    private void OnEnable()
    {
        GameClockManager.Instance.OnGameLogicUpdate += LogicUpdate;
        GameClockManager.Instance.OnReceiveCommand += ReceiveCommand;
    }

    private void OnDisable()
    {
        GameClockManager.Instance.OnGameLogicUpdate -= LogicUpdate;
        GameClockManager.Instance.OnReceiveCommand -= ReceiveCommand;
    }

    private void LogicUpdate()
    {
        _previousLogicPosition = _currentLogicPosition;
        switch (_unitState)
        {
            case UnitState.Idle:
                break;
            case UnitState.Move:
                if (Vector3.Distance(_currentLogicPosition, _targetPosition) > 0.1f)
                {
                    // transform.position = Vector3.MoveTowards(transform.position, _targetPosition, GameClockManager.TIME_STEP* speed);
                    _currentLogicPosition = Vector3.MoveTowards(
                        _currentLogicPosition,
                        _targetPosition,
                        GameClockManager.TIME_STEP * logicSpeed);
                }
                else
                {
                    _unitState = UnitState.Idle;
                    _currentLogicPosition = _targetPosition;
                }

                break;
        }
    }

    private void ReceiveCommand(player_input_command command)
    {
        if (command.command_type == (int)command_type.Move)
        {
            _unitState = UnitState.Move;
            _targetPosition = new Vector3(command.x, command.y, command.z);
        }
    }
}