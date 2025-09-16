using System.Collections;
using System.Collections.Generic;
using Client;
using Client.Unit;
using UnityEngine;


public class UnitController : MonoBehaviour, IClient
{
    public float logicSpeed;
    public float smoothTime = 0.5f;

    private ClientUnit _clientUnit;
    private Vector3 _currentLogicPosition; // 当前逻辑帧的权威位置
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

        ClientManager.Instance.OnConnectServer += OnConnectServer;
    }

    private void OnDisable()
    {
        GameClockManager.Instance.OnGameLogicUpdate -= LogicUpdate;
        GameClockManager.Instance.OnReceiveCommand -= ReceiveCommand;

        ClientManager.Instance.OnConnectServer -= OnConnectServer;
    }

    public ClientUnit ClientUnit { get; set; }

    public void LogicUpdate()
    {
        switch (_unitState)
        {
            case UnitState.Idle:
                break;
            case UnitState.Move:
                if (Vector3.Distance(_currentLogicPosition, _targetPosition) > 0.1f)
                {
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

    public void ReceiveCommand(player_input_command command)
    {
        if (command.command_type == (int)command_type.Move)
        {
            _unitState = UnitState.Move;
            _targetPosition = new Vector3(command.x, command.y, command.z);
        }
    }


    public void OnConnectServer(ClientUnit client)
    {
        ClientUnit = client;
    }
}