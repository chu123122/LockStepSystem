using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Client
{
    public class GameClockManager : MonoSingleton<GameClockManager>
    {
        public event Action<player_input_command> OnGameLogicUpdate;

        public int currentLogicFrame = 0;
        public int executeLogicFrame = 0;

        private const float LOGIC_FRAME_RATE = 30.0f;
        private const float TIME_STEP = 1.0f / LOGIC_FRAME_RATE; // 每帧的固定时长，约0.033秒
        private const int INPUT_DELAY = 3; //输入延迟
        
        private ClientManager _clientManager;
        private InputManager _inputManager;
        private float _accumulator = 0.0f;

        private void Start()
        {
            _clientManager=ClientManager.Instance;
            _inputManager=InputManager.Instance;
        }

        public void LogicUpdate()
        {
            _accumulator += Time.deltaTime;

            while (_accumulator >= TIME_STEP)
            {
                //从输入管理器,收集输入创建指令
                _clientManager.CreateInputCommand(_inputManager.GetPlayerInputCommand());
                _clientManager.SendInputToServer(currentLogicFrame); //发送指令往服务端
                _clientManager.ReceiveInputFromServer(); //接收从服务端传输过来的指令集

                executeLogicFrame = currentLogicFrame - INPUT_DELAY; //当前执行帧
                if (_clientManager.CommandSetDic.Keys.Contains(executeLogicFrame)) //检查执行帧的指令集是否到达
                {
                    player_input_command[] commands=_clientManager.CommandSetDic[executeLogicFrame];
                    RunGameLogic(commands); //执行指令
                }
                else
                {
                    //游戏暂停等待
                }

                _accumulator -= TIME_STEP;
                currentLogicFrame += 1;
            }
        }
        

        private void RunGameLogic(player_input_command[] inputCommands)
        {
            foreach (var inputCommand in inputCommands)
            {
                OnGameLogicUpdate?.Invoke(inputCommand);
            }
        }
    }
}