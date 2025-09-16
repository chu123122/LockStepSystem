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
    public float speed;
    private Vector3 _currentLogicPosition; // 当前逻辑帧的权威位置

    private Vector3 _previousLogicPosition; // 上一个逻辑帧的权威位置
    private Vector3 _targetPosition;
    private UnitState _unitState;

    private void Awake()
    {
        _targetPosition = transform.position;
        _unitState = UnitState.Idle;
        speed = 10f;
    }

    private void Update()
    {
        Vector3 euler = transform.rotation.eulerAngles;
        euler.x = 0;
        euler.z = 0;
        transform.rotation = Quaternion.Euler(euler);

        float t = GameClockManager.Instance.accumulator / GameClockManager.TIME_STEP;
        Debug.LogWarning($"当前渲染位置：{transform.position}，" +
                         $"先前逻辑位置{_previousLogicPosition}，" +
                         $"当前逻辑位置{_currentLogicPosition}" +
                         $"当前t：{Time.deltaTime}");
        transform.position = Vector3.MoveTowards(
            transform.position,
            _currentLogicPosition,
            Time.deltaTime);
        Debug.Log($"当前渲染位置：{transform.position}，" +
                  $"先前逻辑位置{_previousLogicPosition}，" +
                  $"当前逻辑位置{_currentLogicPosition}");
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
                        GameClockManager.TIME_STEP * speed);
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