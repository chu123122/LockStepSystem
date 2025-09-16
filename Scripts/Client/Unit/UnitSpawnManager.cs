using System.Collections.Generic;
using UnityEngine;

namespace Client.Unit
{
    public class UnitSpawnManager : MonoSingleton<UnitSpawnManager>, IClient
    {
        private struct ClientUnitWithVec
        {
            public ClientUnit ClientUnit;
            public Vector3 Position;
        }

        public GameObject unitPrefab;

        private Queue<ClientUnitWithVec> _spawnPosQueue;

        public override void Awake()
        {
            base.Awake();
            _spawnPosQueue = new Queue<ClientUnitWithVec>();
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
            if (_spawnPosQueue.Count > 0)
            {
                InstantiateUnit(_spawnPosQueue.Dequeue());
                PhysicsManager.Instance.RefreshPhysicsObjects();
            }
        }

        public void ReceiveCommand(player_input_command command)
        {
            if (command.command_type == (int)command_type.Create)
            {
                _spawnPosQueue.Enqueue(new ClientUnitWithVec()
                {
                    ClientUnit = new ClientUnit()
                    {
                        ID = command.id
                    },
                    Position = new Vector3(command.x, command.y, command.z)
                });
            }
        }


        public void OnConnectServer(ClientUnit client)
        {
            ClientUnit = client;
        }

        private void InstantiateUnit(ClientUnitWithVec clientPair)
        {
            GameObject unit = Instantiate(unitPrefab, clientPair.Position, Quaternion.identity);
            UnitController unitController = unit.GetComponent<UnitController>();
            unitController.ClientUnit = clientPair.ClientUnit;
            Debug.Log($"生成单位在逻辑帧:{GameClockManager.Instance.currentLogicFrame}");
        }
    }
}