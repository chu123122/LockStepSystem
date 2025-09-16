
using Client;
using Client.Unit;
using UnityEngine;


public class UnitController : PhysicsBase, IClient
{
    protected void Update()
    {
        Vector3 euler = transform.rotation.eulerAngles;
        euler.x = 0;
        euler.z = 0;
        transform.rotation = Quaternion.Euler(euler);

        this.RenderUpdate();
    }

    private void OnEnable()
    {
        GameClockManager.Instance.OnReceiveCommand += ReceiveCommand;
        ClientManager.Instance.OnConnectServer += OnConnectServer;
    }

    private void OnDisable()
    {
        GameClockManager.Instance.OnReceiveCommand -= ReceiveCommand;
        ClientManager.Instance.OnConnectServer -= OnConnectServer;
    }

    public ClientUnit ClientUnit { get; set; }

    public void LogicUpdate()
    {
       
    }

    public void ReceiveCommand(player_input_command command)
    {
        if (command.id != ClientUnit.ID) return;

        if (command.command_type == (int)command_type.Move)
        {
            Vector3 targetPosition = new Vector3(command.x, command.y, command.z);
            Vector3 direction = (targetPosition - this.currentLogicPosition).normalized;
            float strikeForce = 20.0f; // 固定的“力道”
            this.currentVelocity = direction * strikeForce;
        }
    }


    public void OnConnectServer(ClientUnit client)
    {
        ClientUnit = client;
    }
}