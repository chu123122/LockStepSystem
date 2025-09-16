using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Client
{
    public class GameClockManager : MonoSingleton<GameClockManager>
    {
        private const float LOGIC_FRAME_RATE = 30.0f;
        public const float TIME_STEP = 1.0f / LOGIC_FRAME_RATE; // 每帧的固定时长，约0.033秒
        private const int INPUT_DELAY = 5; //输入延迟

        public int currentLogicFrame = 0; //实际跑的逻辑帧，只依靠服务端控制
        public int currentInputFrame = 0; //进行输入采集的逻辑帧，依靠客户端进行一直运行
        public int executeLogicFrame = 0;
        public float accumulator = 0.0f;

        private ClientManager _clientManager;
        private InputManager _inputManager;

        private void Start()
        {
            _clientManager = ClientManager.Instance;
            _inputManager = InputManager.Instance;
        }

        public event Action OnGameLogicUpdate;
        public event Action<player_input_command> OnReceiveCommand;

        public void LogicUpdate()
        {
            accumulator += Time.deltaTime;

            while (accumulator >= TIME_STEP)
            {
                Debug.LogError($"当前输入帧：{currentInputFrame}，当前逻辑帧：{currentLogicFrame}");
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

                //TODO 注意，这里进行了逻辑的简化，我们在开始时直接忽略了输入延迟的作用,并通过硬编码让逻辑帧0的指令集只执行一次
                executeLogicFrame = currentLogicFrame - INPUT_DELAY; //当前执行帧

                if (_clientManager.ServerCommandSetDic.Keys.Contains(executeLogicFrame)) //检查执行帧的指令集是否到达
                {
                    player_input_command[] commands = _clientManager.ServerCommandSetDic[executeLogicFrame];
                    SendCommandSetToClient(commands); //执行指令

                    currentLogicFrame += 1;
                    OnGameLogicUpdate?.Invoke();
                }
                else if (executeLogicFrame < 0)
                {
                    currentLogicFrame += 1;
                }
                else
                {
                    //游戏暂停等待
                }

                accumulator -= TIME_STEP;
                currentInputFrame += 1;
            }
        }


        private void SendCommandSetToClient(player_input_command[] inputCommands)
        {
            foreach (var inputCommand in inputCommands)
            {
                if (inputCommand.id != -1)
                    OnReceiveCommand?.Invoke(inputCommand);
            }
        }
    }
}