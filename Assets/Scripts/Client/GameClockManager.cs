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
        private const int INPUT_DELAY = 5;

        public int currentLogicFrame = 0;
        public int currentInputFrame = 0;
        public int executeLogicFrame = 0;
        public float accumulator = 0.0f;

        private ClientManager _clientManager;
        private InputManager _inputManager;
        private readonly DeterministicWorld _world = new();

        public int replayFrame = 0;

        public DeterministicWorld World => _world;

        private void Start()
        {
            _clientManager = ClientManager.Instance;
            _inputManager = InputManager.Instance;
        }

        public event Action OnGameLogicUpdate;
        public event Action<PlayerInputCommand, int> OnReceiveCommand;
        public event Action<int, Vector3> OnSpawnEntity;

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
                    _clientManager.SendInputCommandToServer(currentInputFrame);
                }

                _clientManager.ReceivePacketFromServer();

                executeLogicFrame = currentLogicFrame - INPUT_DELAY;

                if (_clientManager.ServerCommandSetDic.Keys.Contains(executeLogicFrame))
                {
                    PlayerInputCommand[] commands = _clientManager.ServerCommandSetDic[executeLogicFrame];
                    ExecuteFrameCommands(commands);
                    currentLogicFrame += 1;
                    _world.Update(TIME_STEP);
                    OnGameLogicUpdate?.Invoke();
                }
                else if (executeLogicFrame < 0)
                {
                    currentLogicFrame += 1;
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
            foreach (EntitySpawn s in spawns)
            {
                _world.CreateEntity(s.EntityId);
                _world.AddComponent(s.EntityId, new CoreTransform { EntityId = s.EntityId, Position = new NumericsVector3(s.X, s.Y, s.Z) });
                switch (s.Shape)
                {
                    case Shape.Circle:
                        _world.AddComponent(s.EntityId, new CoreCircleCollider { EntityId = s.EntityId, Radius = s.SizeX });
                        break;
                    case Shape.Box:
                        _world.AddComponent(s.EntityId, new CoreBoxCollider { EntityId = s.EntityId, HalfExtents = new NumericsVector3(s.SizeX, s.SizeY, s.SizeZ) });
                        break;
                }
                if (s.IsDynamic)
                    _world.AddComponent(s.EntityId, new CoreRigidbody { EntityId = s.EntityId, Velocity = NumericsVector3.Zero, InverseMass = 1f });

                OnSpawnEntity?.Invoke(s.EntityId, new Vector3(s.X, s.Y, s.Z));
            }
        }

        private int CreatePlayerEntity(PlayerInputCommand cmd)
        {
            int entityId = _world.CreateEntity();
            _world.AddComponent(entityId, new CoreTransform { EntityId = entityId, Position = new NumericsVector3(cmd.x, cmd.y, cmd.z) });
            _world.AddComponent(entityId, new CoreRigidbody { EntityId = entityId, Velocity = NumericsVector3.Zero, InverseMass = 1f });
            _world.AddComponent(entityId, new CoreCircleCollider { EntityId = entityId, Radius = 0.5f });
            _world.BindPlayerEntity(cmd.id, entityId);
            return entityId;
        }

        public bool IsReplayTime()
        {
            return currentLogicFrame <= replayFrame;
        }
    }
}
