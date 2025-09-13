using System.Collections;
using System.Collections.Generic;
using Client;
using UnityEngine;

public class UnitController : MonoBehaviour
{
    public float speed = 2f;
    private Camera _camera;
    private Vector3 _targetPosition;

    private void Awake()
    {
        _targetPosition = transform.position;
    }

    private void Start()
    {
        _camera = Camera.main;
        GameClockManager.Instance.OnGameLogicUpdate += Move;
    }

    private void Move(player_input_command command)
    {
        Vector3 targetPos = new Vector3(command.x, command.y, command.z);
        float distance = Vector3.Distance(transform.position, targetPos);
        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position,
                targetPos, Time.deltaTime * speed);
        }
    }
}