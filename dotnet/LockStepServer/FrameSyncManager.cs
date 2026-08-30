using LockStepServer.Protocol;

namespace LockStepServer;


public class FrameSyncManager
{
    private static readonly Dictionary<int, FrameData> frameDataMap = new();

    public FrameData GetFrameData(int frame)
    {
        if (!frameDataMap.TryGetValue(frame, out FrameData frameData))
        {
            frameData = new FrameData();     
            frameDataMap.Add(frame, frameData);
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
