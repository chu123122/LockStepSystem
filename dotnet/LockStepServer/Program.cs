using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.Json;
using LockStepCore.Level;
using LockStepServer.Protocol;

namespace LockStepServer;

internal static class Program
{
    private static void Main()
    {
        ServerLoop.Start();
    }
}

public static class ServerLoop
{
    private const float ServerTickRate = 30.0f;
    private const float TimeStep = 1.0f / ServerTickRate;
    private static readonly TimeSpan TimeoutDuration = TimeSpan.FromMilliseconds(200);
    private const int RecvBufSize = Utils.MaxPacketSize;   // 缓冲与协议最大包同源,不再拍数字

    private static int _currentServerFrame = 0;

    private static long _lastTime;
    private static float _accumulator = 0f;

    private static NetworkManager _network = null!;
    private static ClientManager _clients = null!;
    private static FrameSyncManager _frames = null!;
    private static LevelData _levelData = null!;

    private static readonly Dictionary<PacketType, Action<byte[], IPEndPoint>> Dispatch = new()
    {
        [PacketType.Join] = HandleJoin,
        [PacketType.Command] = HandleCommand,
    };

    public static void Start()
    {
        _network = new NetworkManager();
        _clients = new ClientManager();
        _frames = new FrameSyncManager();
        LoadLevelData();
        if (!_network.Init(out string? err))
            throw new ArgumentException($"{err}");

        while (true)
        {
            long frameStartTime = Stopwatch.GetTimestamp();

            float deltaTime = (float)((frameStartTime - _lastTime) / (double)Stopwatch.Frequency);
            _lastTime = frameStartTime;

            _accumulator += deltaTime;
            while (_accumulator >= TimeStep)
            {
                //收集客户端发来数据包
                while (true)
                {
                    byte[] buf = new byte[RecvBufSize];
                    int bytesReceived = _network.ReceiveFromClient(buf, RecvBufSize, out var from);
                    if (bytesReceived <= 0)
                        break;
                    // 防御:单包超过缓冲(协议最大包 < 缓冲,正常不应触发)
                    if (bytesReceived > RecvBufSize)
                    {
                        Console.WriteLine($"包长度异常 {bytesReceived} > {RecvBufSize},丢弃");
                        continue;
                    }

                    byte[] data = new byte[bytesReceived];
                    Array.Copy(buf, data, bytesReceived);

                    PacketType type = (PacketType)BitConverter.ToInt32(data, 0);
                    if (Dispatch.TryGetValue(type, out var handler))
                        handler(data, from!);
                    else
                        Console.WriteLine($"未知包类型:{(int)type}");
                }

                // 2.处理指令阶段
                int clientCount = _clients.GetClientCount();
                if (clientCount > 0)
                {
                    var frameData = _frames.GetFrameData(_currentServerFrame);

                    if (frameData.Status == FrameStatus.Collecting)
                    {
                        if (frameData.PlayerInputCommands.Count == clientCount)
                            frameData.Status = FrameStatus.Ready;
                        else if (frameData.Age > TimeoutDuration)
                        {
                            _frames.FullNullCommandInFrameData(frameData, clientCount);
                            frameData.Status = FrameStatus.Ready;
                        }
                    }

                    if (frameData.Status == FrameStatus.Ready)
                    {
                        var packet = BuildCommandSetPacket(_currentServerFrame, frameData.PlayerInputCommands);
                        var bytes = Utils.SerializedPacket(
                            new PacketHeader { packet_type = (int)PacketType.CommandSet }, packet);
                        foreach (var addr in _clients.GetAllClientAddresses())
                            _network.SendBufToClient(_currentServerFrame, bytes, bytes.Length, addr);

                        frameData.Status = FrameStatus.Dispatched;
                        _currentServerFrame++;
                    }
                }

                _accumulator -= TimeStep;

                // 3.休眠阶段
                long frameEnd = Stopwatch.GetTimestamp();
                float frameDuration = (float)((frameEnd - frameStartTime) / (double)Stopwatch.Frequency);
                if (frameDuration < TimeStep)
                    Thread.Sleep((int)((TimeStep - frameDuration) * 1000));
            }

            Thread.Sleep(1);
        }
    }

    private static void HandleJoin(byte[] data, IPEndPoint from)
    {
        Console.WriteLine("接收客户端请求链接:成功");

        var response = new JoinPacket
        {
            id = _clients.GetClientId(),
            frame_number = _currentServerFrame,
        };
        _clients.AddClientWithCheck(from);

        var responseBytes =
            Utils.SerializedPacket(new PacketHeader { packet_type = (int)PacketType.Response }, response);
        _network.SendBufToClient(_currentServerFrame, responseBytes, responseBytes.Length, from);

        if (_levelData.Spawns.Count > 0)
        {
            var initBytes = Utils.SerializedPacket(new PacketHeader { packet_type = (int)PacketType.InitWorld }, _levelData.Spawns);
            _network.SendBufToClient(_currentServerFrame, initBytes, initBytes.Length, from);
        }

        var createCommand = new PlayerInputCommand
        {
            id = response.id,
            command_type = (int)CommandType.Create,
            x = 8,
            y = 0,
            z = 8,
        };
        _frames.AddCommandInMap(createCommand, _currentServerFrame);

        // 下发历史帧指令集
        for (int i = 0; i < _currentServerFrame; i++)
        {
            var historyFrame = _frames.GetFrameData(i);
            var packet = BuildCommandSetPacket(i, historyFrame.PlayerInputCommands);
            var bytes = Utils.SerializedPacket(new PacketHeader { packet_type = (int)PacketType.CommandSet }, packet);
            _network.SendBufToClient(_currentServerFrame, bytes, bytes.Length, from);
        }
    }

    private static void LoadLevelData()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Data", "levels", "world.json");
            if (File.Exists(candidate))
            {
                var json = File.ReadAllText(candidate);
                _levelData = JsonSerializer.Deserialize<LevelData>(json, new JsonSerializerOptions { IncludeFields = true }) ?? new LevelData();
                Console.WriteLine($"关卡数据已加载:{_levelData.Spawns.Count} 个实体 ({candidate})");
                return;
            }
            dir = dir.Parent;
        }
        _levelData = new LevelData();
        Console.WriteLine("未找到关卡数据,以空世界启动");
    }

    private static void HandleCommand(byte[] data, IPEndPoint from)
    {
        if (Utils.DeserializedPacket(data).Body is not PlayerInputCommand body)
            return;

        _frames.AddCommandInMap(body, _currentServerFrame);
        Console.WriteLine($"接收客户端指令 当前服务端逻辑帧:{_currentServerFrame} 已连接客户端数量:{_clients.GetClientCount()}");
    }

    private static FramePacket BuildCommandSetPacket(int frame, List<PlayerInputCommand> commands)
    {
        var slots = new PlayerInputCommand[10];
        for (int i = 0; i < slots.Length; i++)
            slots[i] = new PlayerInputCommand { id = -1 };

        for (int i = 0; i < commands.Count && i < slots.Length; i++)
            slots[i] = commands[i];

        return new FramePacket
        {
            frame_number = frame,
            command_count = commands.Count,
            commands = slots,
        };
    }
}