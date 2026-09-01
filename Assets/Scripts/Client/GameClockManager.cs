using System;
using System.Collections.Generic;
using System.Linq;
using Client.Protocol;
using UnityEngine;
using LockStepCore;
using LockStepCore.Level;
using LockStepCore.Physics;
using CoreTransform = LockStepCore.Base.Transform;
using CoreRigidbody = LockStepCore.Base.Rigidbody;
using CoreCircleCollider = LockStepCore.Base.CircleCollider;
using CoreBoxCollider = LockStepCore.Base.BoxCollider;
using NumericsVector3 = System.Numerics.Vector3;

namespace Client
{
    public class GameClockManager : MonoSingleton<GameClockManager>
    {
        private const float LOGIC_FRAME_RATE = 30.0f;
        public const float TIME_STEP = 1.0f / LOGIC_FRAME_RATE;

        public int currentLogicFrame = 0;
        public int currentInputFrame = 0;
        public float accumulator = 0.0f;

        private ClientManager _clientManager;
        private InputManager _inputManager;
        private readonly DeterministicWorld _world = new();

        public int replayFrame = 0;

        private ulong _lastFrameHash = 0;

        public DeterministicWorld World => _world;

        private void Start()
        {
            _clientManager = ClientManager.Instance;
            _inputManager = InputManager.Instance;
        }

        public event Action OnGameLogicUpdate;
        public event Action<PlayerInputCommand, int> OnReceiveCommand;
        public event Action<EntitySpawn> OnSpawnEntity;

        public void LogicUpdate()
        {
            List<EntitySpawn>? pendingInit = _clientManager.ConsumeWorldInit();
            if (pendingInit != null)
                InitWorldEntities(pendingInit);

            accumulator += Time.deltaTime;

            while (accumulator >= TIME_STEP)
            {
                PlayerInputState playerInputState = _inputManager.GetPlayerInputCommand();
                PlayerInputCommand command = _clientManager.CreateInputCommand(playerInputState);
                if (_inputManager.GetPlayerInput())
                {
                    _inputManager.ResetInput();
                    _clientManager.AddLocalPlayerInputCommand(command, currentInputFrame);
                }

                if (_clientManager.HaveInputCommandInFrame(currentInputFrame))
                {
                    _clientManager.SendInputCommandToServer(currentInputFrame, currentLogicFrame, _lastFrameHash);
                }

                _clientManager.ReceivePacketFromServer();


                if (_clientManager.ServerCommandSetDic.Keys.Contains(currentLogicFrame))
                {
                    PlayerInputCommand[] commands = _clientManager.ServerCommandSetDic[currentLogicFrame];
                    ExecuteFrameCommands(commands);
                    currentLogicFrame += 1;
                    _world.Update(TIME_STEP);
                    _lastFrameHash = _world.ComputeFrameHash();
                    OnGameLogicUpdate?.Invoke();
                }

                accumulator -= TIME_STEP;
                currentInputFrame += 1;
            }
        }

        private void ExecuteFrameCommands(PlayerInputCommand[] commands)
        {
            List<PlayerFrameInput> inputs = new List<PlayerFrameInput>();
            foreach (PlayerInputCommand cmd in commands)
            {
                if (cmd.id == -1)
                    continue;
                if (cmd.command_type == (int)CommandType.Create)
                {
                    int entityId = CreatePlayerEntity(cmd);
                    OnReceiveCommand?.Invoke(cmd, entityId);
                }
                else if (cmd.command_type == (int)CommandType.Move)
                {
                    inputs.Add(new PlayerFrameInput
                    {
                        PlayerId = cmd.id,
                        MoveTarget = new NumericsVector3(cmd.x, cmd.y, cmd.z),
                    });
                }
            }
            _world.SetFrameInputs(inputs);
        }

        private void InitWorldEntities(List<EntitySpawn> spawns)
        {
            _world.InitWorld(new LevelData { Spawns = spawns });
            foreach (EntitySpawn s in spawns)
            {
                OnSpawnEntity?.Invoke(s);
            }
        }

        private int CreatePlayerEntity(PlayerInputCommand cmd)
        {
            return _world.CreatePlayerEntity(cmd.id, new NumericsVector3(cmd.x, cmd.y, cmd.z), 0.5f);
        }

        public bool IsReplayTime()
        {
            return currentLogicFrame <= replayFrame;
        }
    }
}
