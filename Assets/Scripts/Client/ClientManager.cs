using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Client;
using Client.Unit;
using UnityEngine.Serialization;

public class ClientManager : MonoSingleton<ClientManager>
{
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
    public void SendInputCommandToServer(int currentFrame)
    {
        if (!_logicCommandsDic.TryGetValue(currentFrame, out var command))
        {
            Debug.LogError("当前逻辑帧中不存在玩家输入指令");
            return;
        }

        SendPacketToServer(command, PacketType.Command);
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
        //截取头部作为特征码
        PacketHeader packetHeader = Common.BytesToStruct<PacketHeader>(bytes);
        PacketType packetType = (PacketType)packetHeader.packet_type;

        switch (packetType)
        {
            case PacketType.Response:
                //头部之后才是 body(跳过 4 字节头)
                JoinPacket joinPacket = Common.BytesToStruct<JoinPacket>(bytes, 4);
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
            case PacketType.CommandSet:
                FramePacket framePacket = Common.BytesToStruct<FramePacket>(bytes, 4);
                int currentFrame = framePacket.frame_number;
                PlayerInputCommand[] inputCommands = framePacket.commands;
                int commandCount = framePacket.command_count; //TODO:不确定要如何处理
                ServerCommandSetDic.Add(currentFrame, inputCommands);

                foreach (var command in inputCommands)
                {
                    if (command.id != -1)
                    {
                        Debug.LogWarning($"从服务端接收非空指令集成功 " +
                                      $"非空指令类型{(CommandType)command.command_type}"+
                                      $"指令集执行逻辑帧：{framePacket.frame_number}" +
                                      $"客户端逻辑帧：{_gameClockManager.currentLogicFrame}" +
                                      $"当前时间：{DateTime.Now.ToString(CultureInfo.CurrentCulture)} ");}
                }

                break;
        }
    }

    /// <summary>
    /// 发送连接请求给服务端
    /// </summary>
    private void SendJoinRequest()
    {
        SendPacketToServer(new PacketHeader { packet_type = (int)PacketType.Join }, PacketType.Join);
    }

    private void SendPacketToServer(object packet, PacketType type)
    {
        byte[] myData = Array.Empty<byte>();
        int sendValue = 0;
        switch (type)
        {
            //发送请求连接(服务器只读头部,body 为空)
            case PacketType.Join:
                myData = Common.StructToBytes((PacketHeader)packet);
                break;
            //发送当前指令:头部(4B)+ 指令 body
            case PacketType.Command:
                myData = Combine(
                    Common.StructToBytes(new PacketHeader { packet_type = (int)PacketType.Command }),
                    Common.StructToBytes((PlayerInputCommand)packet));
                break;
            default:
                Debug.LogError("未知错误，无法判定发送往服务器包类型");
                return;
        }

        sendValue = _client.Send(myData, myData.Length, _anyIP);
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
