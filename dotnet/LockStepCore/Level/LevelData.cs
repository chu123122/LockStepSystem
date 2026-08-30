using System;
using System.Collections.Generic;
using LockStepCore.Physics;

namespace LockStepCore.Level;

[Serializable]
public struct EntitySpawn
{
    public int EntityId;
    public Shape Shape;
    public float X;
    public float Y;
    public float Z;
    public float SizeX;
    public float SizeY;
    public float SizeZ;
    public bool IsDynamic;
}

[Serializable]
public class LevelData
{
    public List<EntitySpawn> Spawns = new();
}
