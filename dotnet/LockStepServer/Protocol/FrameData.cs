namespace LockStepServer.Protocol;



public enum FrameStatus
{
    Collecting,   // 收集玩家指令中
    Ready,        // 指令齐(或超时补位),等待广播
    Dispatched,   // 已广播,本帧完成
}

public class FrameData
{
    public FrameStatus Status { get; set; }
    public DateTime CreationTime { get; set; }
    public List<PlayerInputCommand> PlayerInputCommands { get; set; } = new();

    //超时时间
    public TimeSpan Age => DateTime.UtcNow - CreationTime;

    public FrameData()
    {
        Status = FrameStatus.Collecting;
        CreationTime = DateTime.UtcNow;
    }
}
