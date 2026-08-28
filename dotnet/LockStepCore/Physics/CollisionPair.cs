namespace LockStepCore.Physics;

public struct CollisionPair
{
    public int EntityA;
    public int EntityB;
}
public enum Shape
{
    None = 0,
    Circle = 1,
    Box = 2,
    Plane = 3,
}
