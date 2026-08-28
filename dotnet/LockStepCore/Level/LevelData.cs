using System.Collections.Generic;
using LockStepCore.Physics;

namespace LockStepCore.Level;

public struct EntitySpawn
{
    public int EntityId;
    public Shape Shape;
    public float X;
    public float Y;
    public float Z;
    public float Size;
    public bool IsDynamic;
}

public class LevelData
{
    public List<EntitySpawn> Spawns = new();
}
