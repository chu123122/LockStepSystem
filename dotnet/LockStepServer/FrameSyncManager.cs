using LockStepServer.Protocol;

namespace LockStepServer;

/// <summary>
/// 持有帧输入数据
/// </summary>
public class FrameSyncManager
{
    private static readonly Dictionary<int, FrameData> FrameDataMap = new();

    public FrameData GetFrameData(int frame)
    {
        if (!FrameDataMap.TryGetValue(frame, out FrameData frameData))
        {
            frameData = new FrameData();     
            FrameDataMap.Add(frame, frameData);
        }
        return frameData;
    }

    public void AddCommandInMap(PlayerInputCommand cmd, int frame)
    {
        GetFrameData(frame).PlayerInputCommands.Add(cmd);
    }


    public void FullNullCommandInFrameData(FrameData frameData, int clientCount)
    {
        int emptyCount = clientCount - frameData.PlayerInputCommands.Count;
        for (int i = 0; i < emptyCount; i++)
        {
            frameData.PlayerInputCommands.Add(new PlayerInputCommand { id = -1 });
        }
    }
}
