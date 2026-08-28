namespace LockStepCore.Physics;

public struct PhysicsSettings
{
    public float Friction;              // 速度每帧衰减系数
    public float RestSpeedThreshold;    // 低于此速度视为静止并清零
    public float WallBounce;            // 碰撞反弹恢复系数
    public float MoveStrikeForce;       // 移动指令作用到实体的力道
    public float DefaultRadius;         // 无 CircleCollider 时兜底的半径

    public static readonly PhysicsSettings Default = new PhysicsSettings
    {
        Friction = 0.98f,
        RestSpeedThreshold = 0.1f,
        WallBounce = 0.9f,
        MoveStrikeForce = 20f,
        DefaultRadius = 0.5f,
    };
}
