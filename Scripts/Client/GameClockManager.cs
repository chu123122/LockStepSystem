using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Client
{
    public class GameClockManager : MonoSingleton<GameClockManager>
    {
        public event Action OnGameLogicUpdate;
        public event Action<player_input_command> OnReceiveCommand;

        public int currentLogicFrame = 0; //实际跑的逻辑帧，只依靠服务端控制
        public int currentInputFrame = 0; //进行输入采集的逻辑帧，依靠客户端进行一直运行
        public int executeLogicFrame = 0;

        private const float LOGIC_FRAME_RATE = 30.0f;
        private const float TIME_STEP = 1.0f / LOGIC_FRAME_RATE; // 每帧的固定时长，约0.033秒
        private const int INPUT_DELAY = 5; //输入延迟

        private ClientManager _clientManager;
        private InputManager _inputManager;
        private float _accumulator = 0.0f;

        private void Start()
        {
            _clientManager = ClientManager.Instance;
            _inputManager = InputManager.Instance;
        }

        public void LogicUpdate()
        {
            _accumulator += Time.deltaTime;

            while (_accumulator >= TIME_STEP)
            {
                Debug.LogError($"当前输入逻辑帧：{currentInputFrame}");
                //从输入管理器,收集输入创建指令
                PlayerInputState playerInputState = _inputManager.GetPlayerInputCommand();
                player_input_command command = _clientManager.CreateInputCommand(playerInputState);
                if (_inputManager.GetPlayerInput())
                {
                    _inputManager.ResetInput();
                    _clientManager.AddLocalPlayerInputCommand(command, currentInputFrame);
                }

                //检查当前逻辑帧是否收集到了玩家输入指令
                if (_clientManager.HaveInputCommandInFrame(currentInputFrame))
                {
                    _clientManager.SendInputCommandToServer(currentInputFrame); //发送指令往服务端
                }

                _clientManager.ReceivePacketFromServer(); //接收从服务端传输过来的指令集

                executeLogicFrame = currentLogicFrame - INPUT_DELAY; //当前执行帧
                if (_clientManager.CommandSetDic.Keys.Contains(executeLogicFrame)) //检查执行帧的指令集是否到达
                {
                    player_input_command[] commands = _clientManager.CommandSetDic[executeLogicFrame];
                    SendCommandSetToClient(commands); //执行指令
                    
                    OnGameLogicUpdate?.Invoke();
                    currentLogicFrame += 1;
                }
                else
                {
                    //游戏暂停等待
                }

                _accumulator -= TIME_STEP;
                currentInputFrame += 1;
            }
        }


        private void SendCommandSetToClient(player_input_command[] inputCommands)
        {
            foreach (var inputCommand in inputCommands)
            {
                if (inputCommand.id != -1 &&
                    inputCommand.id == _clientManager.GetClientId())
                    OnReceiveCommand?.Invoke(inputCommand);
            }
        }
    }
}