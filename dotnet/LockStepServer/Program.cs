using System.Diagnostics;
using System.IO;
using System.Net;
using System.Numerics;
using System.Text.Json;
using LockStepCore;
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
    private const int RecvBufSize = Utils.MaxPacketSize;
    private const int InputDelay = 5;   // 延迟广播窗口:帧 I 在 I+InputDelay 时才广播

    private static int _currentServerFrame = 0;
    private static readonly HashSet<int> _broadcastedFrames = new();   // 已广播帧,防重复

    private static long _lastTime;
    private static float _accumulator = 0f;

    private static NetworkManager _networkManager = null!;
    private static ClientManager _clientManager = null!;
    private static FrameSyncManager _frameSyncManager = null!;

    private static DeterministicWorld _world = null!;
    private static LevelData _levelData = null!;
    //世界哈希校验
    private static readonly Dictionary<int, ulong> AuthorityHashes = new();

    private static readonly Dictionary<PacketTypeC2S, Action<byte[], IPEndPoint>> Dispatch = new()
    {
        [PacketTypeC2S.Join] = HandleJoin,
        [PacketTypeC2S.FrameC2S] = HandleCommand,
    };

    public static void Start()
    {
        _networkManager = new NetworkManager();
        _clientManager = new ClientManager();
        _frameSyncManager = new FrameSyncManager();
        _world = new DeterministicWorld();

        LoadLevelData();
        if (!_networkManager.Init(out string? err))
            throw new ArgumentException($"{err}");

        _world.InitWorld(_levelData);

        while (true)
        {
            long frameStartTime = Stopwatch.GetTimestamp();

            float deltaTime = (float)((frameStartTime - _lastTime) / (double)Stopwatch.Frequency);
            _lastTime = frameStartTime;

            _accumulator += deltaTime;
            while (_accumulator >= TimeStep)
            {
                ReceiveAndDispatch();//收包

                int clientCount = _clientManager.GetClientCount();
                if (clientCount > 0)
                {
                    FrameData frameData = _frameSyncManager.GetFrameData(_currentServerFrame);
                    if (TryCollectFrameInput(clientCount, frameData))
                    {
                        UpdateWorld(frameData);              // 更新世界(暂不广播)
                        frameData.Status = FrameStatus.Dispatched;
                        _currentServerFrame++;
                    }
                }

                // 延迟广播窗口
                int broadcastFrame = _currentServerFrame - InputDelay;
                if (broadcastFrame >= 0 && !_broadcastedFrames.Contains(broadcastFrame))
                {
                    FrameData bfd = _frameSyncManager.GetFrameData(broadcastFrame);
                    if (bfd.Status == FrameStatus.Dispatched)
                    {
                        BroadcastFrame(broadcastFrame, bfd);
                        _broadcastedFrames.Add(broadcastFrame);
                    }
                }

                _accumulator -= TimeStep;

                long frameEnd = Stopwatch.GetTimestamp();
                float frameDuration = (float)((frameEnd - frameStartTime) / (double)Stopwatch.Frequency);
                if (frameDuration < TimeStep)
                    Thread.Sleep((int)((TimeStep - frameDuration) * 1000));
            }

            Thread.Sleep(1);
        }
    }

    private static void ReceiveAndDispatch()
    {
        while (true)
        {
            byte[] buf = new byte[RecvBufSize];
            int bytesReceived = _networkManager.ReceiveFromClient(buf, RecvBufSize, out IPEndPoint? from);
            if (bytesReceived <= 0)
                break;

            if (bytesReceived > RecvBufSize)
            {
                Console.WriteLine($"包长度异常 {bytesReceived} > {RecvBufSize},丢弃");
                continue;
            }

            byte[] data = new byte[bytesReceived];
            Array.Copy(buf, data, bytesReceived);

            PacketTypeC2S type = (PacketTypeC2S)BitConverter.ToInt32(data, 0);
            if (Dispatch.TryGetValue(type, out Action<byte[], IPEndPoint> handler))
                handler(data, from!);
            else
                Console.WriteLine($"未知包类型:{(int)type}");
        }
    }

    private static bool TryCollectFrameInput(int clientCount, FrameData frameData)
    {
        if (frameData.Status == FrameStatus.Collecting)
        {
            if (frameData.PlayerInputCommands.Count == clientCount)
                frameData.Status = FrameStatus.Ready;
            else if (frameData.Age > TimeoutDuration)
            {
                _frameSyncManager.FullNullCommandInFrameData(frameData, clientCount);
                frameData.Status = FrameStatus.Ready;
            }
        }

        return frameData.Status == FrameStatus.Ready;
    }

    private static void UpdateWorld(FrameData frameData)
    {
        List<PlayerFrameInput> inputs = new List<PlayerFrameInput>();
        foreach (PlayerInputCommand cmd in frameData.PlayerInputCommands)
        {
            if (cmd.id == -1)
                continue;
            if (cmd.command_type != (int)CommandType.Move)
                continue;

            inputs.Add(new PlayerFrameInput
            {
                PlayerId = cmd.id,
                MoveTarget = new Vector3(cmd.x, cmd.y, cmd.z),
            });
        }

        _world.SetFrameInputs(inputs);
        _world.Update(TimeStep);
        AuthorityHashes[_currentServerFrame] = _world.ComputeFrameHash();
    }

    private static void BroadcastFrame(int frame, FrameData frameData)
    {
        FramePacketS2C packetS2C =
            Utils.BuildCommandSetPacket(frame, frameData.PlayerInputCommands);
        Byte[] bytes = Utils.SerializedPacket(
            new PacketHeader { type = (int)PacketTypeS2C.FrameS2C }, packetS2C);
        foreach (IPEndPoint addr in _clientManager.GetAllClientAddresses())
            _networkManager.SendBufToClient(bytes, bytes.Length, addr);
    }

    private static void HandleJoin(byte[] data, IPEndPoint from)
    {
        Console.WriteLine("接收客户端请求链接:成功");

        JoinPacket response = new JoinPacket
        {
            id = _clientManager.GetClientId(),
            frame_number = _currentServerFrame,
        };
        _clientManager.AddClientWithCheck(from);

        Byte[] responseBytes =
            Utils.SerializedPacket(new PacketHeader { type = (int)PacketTypeS2C.Response }, response);
        _networkManager.SendBufToClient(responseBytes, responseBytes.Length, from);

        if (_levelData.Spawns.Count > 0)
        {
            Byte[] initBytes =
                Utils.SerializedPacket(new PacketHeader { type = (int)PacketTypeS2C.InitWorld }, _levelData.Spawns);
            _networkManager.SendBufToClient(initBytes, initBytes.Length, from);
        }

        PlayerInputCommand createCommand = new PlayerInputCommand
        {
            id = response.id,
            command_type = (int)CommandType.Create,
            x = 8,
            y = 0,
            z = 8,
        };
        _frameSyncManager.AddCommandInMap(createCommand, _currentServerFrame);

        _world.CreatePlayerEntity(response.id, new Vector3(8, 0, 8), 0.5f);

        for (int i = 0; i < _currentServerFrame; i++)
        {
            FrameData historyFrame = _frameSyncManager.GetFrameData(i);
            FramePacketS2C packetS2C =
                Utils.BuildCommandSetPacket(i, historyFrame.PlayerInputCommands);
            Byte[] bytes = Utils.SerializedPacket(new PacketHeader { type = (int)PacketTypeS2C.FrameS2C }, packetS2C);
            _networkManager.SendBufToClient(bytes, bytes.Length, from);
        }
    }

    private static void LoadLevelData()
    {
        DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, "Data", "levels", "world.json");
            if (File.Exists(candidate))
            {
                string json = File.ReadAllText(candidate);
                _levelData =
                    JsonSerializer.Deserialize<LevelData>(json, new JsonSerializerOptions { IncludeFields = true }) ??
                    new LevelData();
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
        if (Utils.DeserializedPacket(data).Body is not FramePacketC2S input)
            return;

        if (AuthorityHashes.TryGetValue(input.frame_number, out ulong authorityHash) && authorityHash != input.hash)
        {
            Console.WriteLine($"帧哈希不一致:帧 {input.frame_number} 客户端 {input.hash} 权威 {authorityHash}");

            Byte[] errBytes = Utils.SerializedPacket(
                new PacketHeader { type = (int)PacketTypeS2C.HashError },
                new HashErrorPacket { frame_number = input.frame_number });
            _networkManager.SendBufToClient(errBytes, errBytes.Length, from);
            return;
        }

        _frameSyncManager.AddCommandInMap(input.command, _currentServerFrame);
        Console.WriteLine($"接收客户端指令 当前服务端逻辑帧:{_currentServerFrame} 已连接客户端数量:{_clientManager.GetClientCount()}");
    }
}
