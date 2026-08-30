using System.Collections.Generic;
using Client.Protocol;
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
            GameClockManager.Instance.OnSpawnEntity += OnSpawnEntity;

            ClientManager.Instance.OnConnectServer += OnConnectServer;
        }

        private void OnDisable()
        {
            GameClockManager.Instance.OnGameLogicUpdate -= LogicUpdate;
            GameClockManager.Instance.OnReceiveCommand -= ReceiveCommand;
            GameClockManager.Instance.OnSpawnEntity -= OnSpawnEntity;

            ClientManager.Instance.OnConnectServer -= OnConnectServer;
        }

        public ClientUnit ClientUnit { get; set; }

        public void LogicUpdate()
        {
            while (_spawnPosQueue.Count > 0)
            {
                InstantiateUnit(_spawnPosQueue.Dequeue());
            }
        }

        public void OnSpawnEntity(int entityId, Vector3 position)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = position;
            UnitController view = go.AddComponent<UnitController>();
            view.ClientUnit = new ClientUnit { ID = entityId };
            Debug.Log($"生成世界球壳 entityId:{entityId} pos:{position}");
        }

        public void ReceiveCommand(PlayerInputCommand command, int entityId)
        {
            if (command.command_type == (int)CommandType.Create)
            {
                _spawnPosQueue.Enqueue(new ClientUnitWithVec()
                {
                    ClientUnit = new ClientUnit()
                    {
                        ID = entityId
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
