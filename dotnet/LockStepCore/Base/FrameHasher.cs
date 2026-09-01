using System.Collections.Generic;
using System.Numerics;

namespace LockStepCore.Hash;

public interface IFrameHasher
{
    ulong Compute(IReadOnlyList<(Vector3 Position, Vector3 Velocity)> entityStates);
}

public class Fnv1A64Hasher : IFrameHasher
{
    private const ulong Prime = 1099511628211;
    private const ulong Offset = 14695981039346656037;

    public ulong Compute(IReadOnlyList<(Vector3 Position, Vector3 Velocity)> entityStates)
    {
        ulong hash = Offset;
        for (int i = 0; i < entityStates.Count; i++)
        {
            (Vector3 Position, Vector3 Velocity) s = entityStates[i];
            hash = Mix(hash, s.Position.X);
            hash = Mix(hash, s.Position.Y);
            hash = Mix(hash, s.Position.Z);
            hash = Mix(hash, s.Velocity.X);
            hash = Mix(hash, s.Velocity.Y);
            hash = Mix(hash, s.Velocity.Z);
        }

        return hash;
    }

    private static ulong Mix(ulong hash, float value)
    {
        byte[] bytes = System.BitConverter.GetBytes(value);
        for (int i = 0; i < bytes.Length; i++)
        {
            hash = (hash ^ bytes[i]) * Prime;
        }

        return hash;
    }
}
