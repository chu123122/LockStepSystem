using System.Collections;
using System.Collections.Generic;
using Client;
using UnityEngine;

public class UnitController : MonoBehaviour
{
    public float speed = 1f;
    private Camera _camera;
    private Vector3 _targetPosition;

    private void Awake()
    {
        _targetPosition = transform.position;
    }

    private void Start()
    {
        _camera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 targetPos = new Vector3(5,transform.position.y,5);
            Debug.LogWarning(targetPos);
       
            while (Vector3.Distance(transform.position, targetPos) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position,
                    targetPos, Time.deltaTime * speed);
            }
        }
    }

    private void OnEnable()
    {
        GameClockManager.Instance.OnGameLogicUpdate += Move;
    }

    private void OnDisable()
    {
        GameClockManager.Instance.OnGameLogicUpdate -= Move;
    }

    private void Move(player_input_command command)
    {
        Vector3 targetPos = new Vector3(command.x, command.y, command.z);
        Debug.LogWarning(targetPos);
       
        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position,
                targetPos, Time.deltaTime * speed);
        }
    }
}