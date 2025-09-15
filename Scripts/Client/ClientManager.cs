using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Client;
using UnityEngine.Serialization;

public class ClientManager : MonoSingleton<ClientManager>
{
    //TODO:需要考虑单纯用List的Index作为指令的逻辑帧数的可行性（或许用Dic才可以达到效果？）
    private readonly Dictionary<int, player_input_command> _logicCommandsDic =
        new Dictionary<int, player_input_command>();

    //  private readonly List<player_input_command> _logicCommandsList = new List<player_input_command>();
    public readonly Dictionary<int, player_input_command[]> CommandSetDic = new();
    private IPEndPoint _anyIP;
    private UdpClient _client;

    private GameClockManager _gameClockManager;
    private int _id = -1;

    private bool _isConnect = false;
    private float _lastJoinRequestTime = -1f;

    private const float NoResponseTime = 2f;
    private float _lastNoResponseTime = 0f;

    public override void Awake()
    {
        base.Awake();
        _client = new UdpClient();
        _anyIP = new IPEndPoint(IPAddress.Parse("172.27.158.10"), 8888);
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

        SendPacketToServer(command, packet_type.Command);
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
    /// <param name="playerInputState"></param>
    public player_input_command CreateInputCommand(PlayerInputState playerInputState)
    {
        Vector3 movePos = playerInputState.MovePos;
        player_input_command playerInputCommand = new player_input_command()
        {
            packet_type = (int)packet_type.Command,
            id = _id,
            x = movePos.x,
            y = movePos.y,
            z = movePos.z
        };
        return playerInputCommand;
    }

    public void AddLocalPlayerInputCommand(player_input_command playerInputCommand, int currentFrame)
    {
        _logicCommandsDic.Add(currentFrame, playerInputCommand);
    }

    public void ReceivePacketFromServer()
    {
        if (_client.Available <= 0)
        {
            _lastNoResponseTime = Time.time;
            if (Time.time - _lastNoResponseTime > NoResponseTime)
                Debug.Log("未接收到服务端回应" +
                          $"当前时间：{DateTime.Now.ToString(CultureInfo.CurrentCulture)} ");
            return;
        }

        byte[] bytes = _client.Receive(ref _anyIP);
        //截取头部作为特征码
        packet_header packetHeader = Common.BytesToStruct<packet_header>(bytes);
        packet_type packetType = (packet_type)packetHeader.packet_type;

        switch (packetType)
        {
            case packet_type.Response:
                join_packet joinPacket = Common.BytesToStruct<join_packet>(bytes);
                _isConnect = true;
                _id = joinPacket.id;
                _gameClockManager.currentInputFrame = joinPacket.frame_number; //同步输入逻辑帧
                Debug.LogWarning($"从服务端接收回应成功 " +
                                 $"分配客户端id：{_id}" +
                                 $"当前时间：{DateTime.Now.ToString(CultureInfo.CurrentCulture)} " +
                                 $"客户端逻辑帧(同步后)：{_gameClockManager.currentLogicFrame}");
                break;
            case packet_type.CommandSet:
                frame_packet framePacket = Common.BytesToStruct<frame_packet>(bytes);
                int currentFrame = framePacket.frame_number;
                player_input_command[] inputCommands = framePacket.commands;
                int commandCount = framePacket.command_count; //TODO:不确定要如何处理
                CommandSetDic.Add(currentFrame, inputCommands);
                break;
        }
    }

    /// <summary>
    /// 发送连接请求给服务端
    /// </summary>
    private void SendJoinRequest()
    {
        join_packet joinPacket = new join_packet()
        {
            packet_type = (int)packet_type.Join,
        };
        SendPacketToServer(joinPacket, packet_type.Join);
    }

    private void SendPacketToServer(object packet, packet_type type)
    {
        byte[] myData = Array.Empty<byte>();
        int sendValue = 0;
        switch (type)
        {
            //发送请求连接
            case packet_type.Join:
                join_packet joinPacket = (join_packet)packet;
                myData = Common.StructToBytes(joinPacket);
                sendValue = _client.Send(myData, myData.Length, _anyIP);
                break;
            //发送当前指令
            case packet_type.Command:
                player_input_command command = (player_input_command)packet;
                myData = Common.StructToBytes(command);
                sendValue = _client.Send(myData, myData.Length, _anyIP);
                break;
            default:
                Debug.LogError("未知错误，无法判定发送往服务器包类型");
                break;
        }

        Debug.Log($"发送数据包往服务端成功 " +
                  $"数据包类型：{type}" +
                  $"发送返回值：{sendValue}" +
                  $"当前时间：{DateTime.Now.ToString(CultureInfo.CurrentCulture)} " +
                  $"客户端逻辑帧：{_gameClockManager.currentLogicFrame}");
    }
}