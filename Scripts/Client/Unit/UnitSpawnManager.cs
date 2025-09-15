using System.Collections.Generic;
using UnityEngine;

namespace Client.Unit
{
    public class UnitSpawnManager:MonoSingleton<UnitSpawnManager>
    {
        public GameObject unitPrefab;
        private Queue<Vector3>  _spawnPosQueue;
        public override void Awake()
        {
            base.Awake();
            _spawnPosQueue=new Queue<Vector3>();
        }

        private void LogicUpdate()
        {
            if(_spawnPosQueue.Count>0)
                InstantiateUnit(_spawnPosQueue.Dequeue());
        }
        private void ReceiveCommand(player_input_command command)
        {
            if (command.command_type == (int)command_type.Create)
            {
                _spawnPosQueue.Enqueue(new Vector3(command.x,command.y,command.z));
            }
        }

        private void InstantiateUnit(Vector3 position)
        {
            Instantiate(unitPrefab, position, Quaternion.identity);
        }


        private void OnEnable()
        {
            GameClockManager.Instance.OnGameLogicUpdate += LogicUpdate;
            GameClockManager.Instance.OnReceiveCommand += ReceiveCommand;

        }

        private void OnDisable()
        {
            GameClockManager.Instance.OnGameLogicUpdate += LogicUpdate;
            GameClockManager.Instance.OnReceiveCommand += ReceiveCommand;

        }
    }
}