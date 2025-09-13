using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Client;
using UnityEngine.Serialization;

public class ClientManager : MonoSingleton<ClientManager>
{
    public readonly Dictionary<int, player_input_command[]> CommandSetDic = new();

    // public readonly List<player_input_command[]> CommandSetList = new List<player_input_command[]>();
    private readonly List<player_input_command> _logicCommandsList = new List<player_input_command>();

    private GameClockManager _gameClockManager;
    private UdpClient _client;
    private IPEndPoint _anyIP;

    private bool _isConnect = false;
    private int _id = -1;
    private float _lastJoinRequestTime = -1f;

    public override void Awake()
    {
        base.Awake();
        _client = new UdpClient();
        _anyIP = new IPEndPoint(IPAddress.Parse("172.27.148.19"), 8888);
    }

    private void Start()
    {
        _gameClockManager = GameClockManager.Instance;
    }

    private void Update()
    {
        if (!_isConnect)
        {
            if (Time.time - _lastJoinRequestTime > 1.0f)
            {
                SendJoinRequest("1");
                _lastJoinRequestTime = Time.time;
            }

            ReceiveJoinResponse();
        }
        else
            _gameClockManager.LogicUpdate();
    }

    private void SendJoinRequest(string playerName = null)
    {
        join_packet joinPacket = new join_packet()
        {
            type_index = (int)packet_type.Join,
            // player_name = playerName,
        };
        byte[] myData = Common.StructToBytes(joinPacket);
        int sendValue=_client.Send(myData, myData.Length, _anyIP);
        Debug.LogWarning($"发送请求连接指令往服务端成功 " +
                         //   $"玩家用户名：{playerName}" +
                         $"发送返回信息：{sendValue}"+
                         $"当前时间：{Time.time} " +
                         $"客户端逻辑帧：{_gameClockManager.currentLogicFrame}");
    }

    private void ReceiveJoinResponse()
    {
        if (_client.Available <= 0)
        {
            Debug.Log("未接收到服务端回应");
            return;
        }

        byte[] bytes = _client.Receive(ref _anyIP);
        join_packet joinPacket = Common.BytesToStruct<join_packet>(bytes);
        packet_type receiveType = (packet_type)joinPacket.type_index;
        if (receiveType == packet_type.Response)
        {
            _isConnect = true;
            _id = joinPacket.id;
            _gameClockManager.currentLogicFrame = joinPacket.frame_number;
            Debug.LogWarning($"从服务端接收回应成功 " +
                             $"分配客户端id：{_id}" +
                             $"当前时间：{Time.time} " +
                             $"客户端逻辑帧(同步后)：{_gameClockManager.currentLogicFrame}");
        }
        else
        {
            Debug.LogError("接收回应错误！！！");
        }
    }

    /// <summary>
    /// 发送当前帧的指令
    /// </summary>
    public void SendInputToServer(int currentFrame)
    {
        if (_logicCommandsList.Count < currentFrame) return;
        player_input_command command = _logicCommandsList[currentFrame];
        SendCommandToServer(command);
    }

    /// <summary>
    /// 接收从服务端发送过来的指令集
    /// </summary>
    public void ReceiveInputFromServer()
    {
        Debug.Log("尝试接收服务端指令集");
        if (_client.Available <= 0)
            return;

        byte[] bytes = _client.Receive(ref _anyIP);
        Debug.Log($"从服务端接收指令集成功 " +
                  $"当前时间：{Time.time} " +
                  $"客户端逻辑帧：{_gameClockManager.currentLogicFrame}");

        frame_packet framePacket = Common.BytesToStruct<frame_packet>(bytes);

        int currentFrame = framePacket.frame_number;
        player_input_command[] inputCommands = framePacket.commands;
        int commandCount = framePacket.command_count; //TODO:不确定要如何处理
        CommandSetDic.Add(currentFrame, inputCommands);
    }

    public void CreateInputCommand(PlayerInputState playerInputState)
    {
        Vector3 movePos = playerInputState.MovePos;
        player_input_command playerInputCommand = new player_input_command()
        {
            x = movePos.x,
            y = movePos.y,
            z = movePos.z
        };
        _logicCommandsList.Add(playerInputCommand);
    }


    private void SendCommandToServer(player_input_command command)
    {
        byte[] myData = Common.StructToBytes(command);
        _client.Send(myData, myData.Length, _anyIP);
        Debug.Log($"发送指令往服务端成功 " +
                  $"当前时间：{Time.time} " +
                  $"客户端逻辑帧：{_gameClockManager.currentLogicFrame}");
    }
}