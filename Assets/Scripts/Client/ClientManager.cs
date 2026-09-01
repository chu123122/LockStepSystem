using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Client;
using Client.Base;
using Client.Protocol;
using Client.Unit;
using UnityEngine.Serialization;
using LockStepCore.Level;
using LockStepCore.Physics;

public class ClientManager : MonoSingleton<ClientManager>
{
    private const int ProtocolVersion = 1;

    private readonly Dictionary<int, PlayerInputCommand> _logicCommandsDic =
        new Dictionary<int, PlayerInputCommand>();

    public readonly Dictionary<int, PlayerInputCommand[]> ServerCommandSetDic = new();
    private IPEndPoint _anyIP;
    private UdpClient _client;

    private GameClockManager _gameClockManager;
    public event Action<ClientUnit> OnConnectServer;
    private int _id = -1;

    private bool _isConnect = false;
    private float _lastJoinRequestTime = -1f;

    private const float NoResponseTime = 2f;
    private float _lastNoResponseTime = 0f;

    private List<EntitySpawn>? _pendingWorldInit;

    public override void Awake()
    {
        base.Awake();
        _client = new UdpClient();
        _anyIP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);
    }

    private void Start()
    {
        _gameClockManager = GameClockManager.Instance;
    }

    private void Update()
    {
        if (!_isConnect) //未与服务端连接，间隔发送请求连接需求
        {
            if (Time.time - _lastJoinRequestTime > 1.0f)
            {
                SendJoinRequest();
                _lastJoinRequestTime = Time.time;
            }

            ReceivePacketFromServer();
        }
        else //连接后进行逻辑帧更新
            _gameClockManager.LogicUpdate();
    }

    public int GetClientId()
    {
        return _id;
    }

    /// <summary>
    /// 发送当前帧的玩家输入指令
    /// </summary>
    public void SendInputCommandToServer(int currentFrame, int frameNumber, ulong hash)
    {
        if (!_logicCommandsDic.TryGetValue(currentFrame, out PlayerInputCommand command))
        {
            Debug.LogError("当前逻辑帧中不存在玩家输入指令");
            return;
        }

        FramePacketC2S input = new FramePacketC2S
        {
            command = command,
            frame_number = frameNumber,
            hash = hash,
        };
        SendPacketToServer(input, PacketTypeC2S.FrameC2S);
    }

    public bool HaveInputCommandInFrame(int currentFrame)
    {
        if (_logicCommandsDic.ContainsKey(currentFrame)) //TODO
            return true;
        return false;
    }


    /// <summary>
    /// 接收输入状态，将其转化为输入指令
    /// </summary>
    public PlayerInputCommand CreateInputCommand(PlayerInputState playerInputState)
    {
        Vector3 movePos = playerInputState.MovePos;
        PlayerInputCommand playerInputCommand = new PlayerInputCommand()
        {
            id = _id,
            command_type = (int)playerInputState.Type,
            x = movePos.x,
            y = movePos.y,
            z = movePos.z
        };
        return playerInputCommand;
    }

    public void AddLocalPlayerInputCommand(PlayerInputCommand playerInputCommand, int currentFrame)
    {
        _logicCommandsDic.Add(currentFrame, playerInputCommand);
    }

    public void ReceivePacketFromServer()
    {
        if (_client.Available <= 0)
        {
            _lastNoResponseTime = Time.time;
            if (Time.time - _lastNoResponseTime > NoResponseTime) //TODO 存在bug导致基本不输出信息
                Debug.Log("未接收到服务端回应" +
                          $"当前时间：{DateTime.Now.ToString(CultureInfo.CurrentCulture)} ");
            return;
        }

        byte[] bytes = _client.Receive(ref _anyIP);
        (PacketHeader header, object? body) = Utils.DeserializedPacket(bytes);

        switch ((PacketTypeS2C)header.type)
        {
            case PacketTypeS2C.Response:
            {
                JoinPacket joinPacket = (JoinPacket)body!;
                if (joinPacket.version != ProtocolVersion)
                {
                    Debug.LogError($"协议版本不匹配:服务器 {joinPacket.version} 客户端 {ProtocolVersion}");
                    return;
                }

                _isConnect = true;
                _id = joinPacket.id;
                _gameClockManager.currentInputFrame = joinPacket.frame_number; //同步输入逻辑帧
                _gameClockManager.replayFrame = joinPacket.frame_number;//确定历史和加入后的界限

                ClientUnit client = new ClientUnit()
                {
                    ID = _id
                };
                OnConnectServer?.Invoke(client);
                Debug.LogWarning($"从服务端接收回应成功 " +
                                 $"分配客户端id：{_id}" +
                                 $"当前时间：{DateTime.Now.ToString(CultureInfo.CurrentCulture)} " +
                                 $"客户端输入帧(同步后)：{_gameClockManager.currentInputFrame}");
                break;
            }
            case PacketTypeS2C.FrameS2C:
            {
                FramePacketS2C framePacket = (FramePacketS2C)body!;
                ServerCommandSetDic.Add(framePacket.frame_number, framePacket.commands);

                foreach (PlayerInputCommand command in framePacket.commands)
                {
                    if (command.id != -1)
                    {
                        Debug.LogWarning($"从服务端接收非空指令集成功 " +
                                      $"非空指令类型{(CommandType)command.command_type}" +
                                      $"指令集执行逻辑帧：{framePacket.frame_number}" +
                                      $"客户端逻辑帧：{_gameClockManager.currentLogicFrame}" +
                                      $"当前时间：{DateTime.Now.ToString(CultureInfo.CurrentCulture)} ");
                    }
                }

                break;
            }
            case PacketTypeS2C.InitWorld:
            {
                List<EntitySpawn> spawns = (List<EntitySpawn>)body!;
                _pendingWorldInit = spawns;
                Debug.LogWarning($"收到世界初始化数据:{spawns.Count} 个实体");
                break;
            }
            case PacketTypeS2C.HashError:
            {
                HashErrorPacket hashErr = (HashErrorPacket)body!;
                Debug.LogError($"帧哈希不一致!帧号:{hashErr.frame_number}");
                break;
            }
        }
    }

    public List<EntitySpawn>? ConsumeWorldInit()
    {
        List<EntitySpawn>? spawns = _pendingWorldInit;
        _pendingWorldInit = null;
        return spawns;
    }

    /// <summary>
    /// 发送连接请求给服务端
    /// </summary>
    private void SendJoinRequest()
    {
        SendPacketToServer(new PacketHeader { type = (int)PacketTypeC2S.Join }, PacketTypeC2S.Join);
    }

    private void SendPacketToServer(object packet, PacketTypeC2S type)
    {
        byte[] data = Array.Empty<byte>();
        int sendValue = 0;
        switch (type)
        {
            //发送请求连接
            case PacketTypeC2S.Join:
                data = Utils.StructToBytes((PacketHeader)packet);
                break;
            //发送当前指令(附带上一逻辑帧号+哈希)
            case PacketTypeC2S.FrameC2S:
                data = Combine(
                    Utils.StructToBytes(new PacketHeader { type = (int)PacketTypeC2S.FrameC2S }),
                    Utils.StructToBytes((FramePacketC2S)packet));
                break;
            default:
                Debug.LogError("未知错误，无法判定发送往服务器包类型");
                return;
        }

        sendValue = _client.Send(data, data.Length, _anyIP);
        Debug.Log($"发送数据包往服务端成功 " +
                  $"数据包类型：{type}" +
                  $"发送返回值：{sendValue}" +
                  $"当前时间：{DateTime.Now.ToString(CultureInfo.CurrentCulture)} " +
                  $"客户端逻辑帧：{_gameClockManager.currentLogicFrame}");
    }

    private static byte[] Combine(byte[] head, byte[] tail)
    {
        byte[] result = new byte[head.Length + tail.Length];
        Array.Copy(head, result, head.Length);
        Array.Copy(tail, 0, result, head.Length, tail.Length);
        return result;
    }
}
