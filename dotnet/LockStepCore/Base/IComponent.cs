using System.Numerics;

namespace LockStepCore.Base;

public interface IComponent
{
    int EntityId { get; set; }
}

public struct Entity
{
    public int Id;
}

public struct Transform : IComponent
{
    public int EntityId { get; set; }
    public Vector3 Position;
}

public struct Rigidbody : IComponent
{
    public int EntityId { get; set; }
    public Vector3 Velocity;
    public float InverseMass;
}

public struct CircleCollider : IComponent
{
    public int EntityId { get; set; }
    public float Radius;
}

public struct BoxCollider : IComponent
{
    public int EntityId { get; set; }
    public Vector3 HalfExtents;
}

public struct PlaneCollider : IComponent
{
    public int EntityId { get; set; }
    public float Height;
}
