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
    public float speed = 1f;
    private Camera _camera;
    private Vector3 _targetPosition;
    private UnitState _unitState;

    private void Awake()
    {
        _targetPosition = transform.position;
        _unitState = UnitState.Idle;
    }

    private void Start()
    {
        _camera = Camera.main;
    }

    private void Update()
    {
        Vector3 euler = transform.rotation.eulerAngles;
        euler.x = 0; 
        euler.z = 0;   
        transform.rotation = Quaternion.Euler(euler);
    }

    private void LogicUpdate()
    {
        switch (_unitState)
        {
            case UnitState.Idle:
                break;
            case UnitState.Move:
                if (Vector3.Distance(transform.position, _targetPosition) > 0.1f)
                {
                    transform.position = Vector3.MoveTowards(transform.position,
                        _targetPosition, Time.deltaTime * speed);
                }
                else
                {
                    _unitState = UnitState.Idle;
                }

                break;
        }
    }

    private void ReceiveCommand(player_input_command command)
    {
        _unitState = UnitState.Move;
        _targetPosition = new Vector3(command.x, command.y, command.z);
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
}