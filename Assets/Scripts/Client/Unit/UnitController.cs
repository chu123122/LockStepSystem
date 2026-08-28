using Client;
using Client.Unit;
using UnityEngine;
using CoreTransform = LockStepCore.Base.Transform;

public class UnitController : PhysicsBase, IClient
{
    protected void Update()
    {
        Vector3 euler = transform.rotation.eulerAngles;
        euler.x = 0;
        euler.z = 0;
        transform.rotation = Quaternion.Euler(euler);

        if (ClientUnit.ID >= 0)
        {
            var world = GameClockManager.Instance.World;
            if (world.TryGetComponent<CoreTransform>(ClientUnit.ID, out var t))
            {
                currentLogicPosition = new Vector3(t.Position.X, t.Position.Y, t.Position.Z);
            }
        }

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

    public void ReceiveCommand(PlayerInputCommand command, int entityId)
    {
    }

    public void OnConnectServer(ClientUnit client)
    {
        ClientUnit = client;
    }
}
