using System.Collections.Generic;
using System.Numerics;
using LockStepCore.Base;

namespace LockStepCore.Physics;

public class PhysicsSystem
{
    private readonly IComponentStore _world;
    private readonly PhysicsSettings _settings;
    private readonly List<CollisionPair> _pairs = new();

    public PhysicsSystem(IComponentStore world, in PhysicsSettings settings)
    {
        _world = world;
        _settings = settings;
    }

    public void Update(float delta)
    {
        Integrate(delta);
        _pairs.Clear();
        CollectCollisionPairs();
        ResolveCollisionPairs();
    }

    private void Integrate(float delta)
    {
        foreach (var id in _world.DynamicEntities)
        {
            if (!_world.TryGetComponent<Rigidbody>(id, out var body))
                continue;
            if (!_world.TryGetComponent<Transform>(id, out var t))
                continue;

            body.Velocity *= _settings.Friction;
            if (body.Velocity.Length() < _settings.RestSpeedThreshold)
                body.Velocity = Vector3.Zero;

            t.Position += body.Velocity * delta;
            _world.SetComponent(id, t);
            _world.SetComponent(id, body);
        }
    }

    private void CollectCollisionPairs()
    {
        for (var i = 0; i < _world.DynamicEntities.Count; i++)
        {
            for (var j = i + 1; j < _world.DynamicEntities.Count; j++)
            {
                var a = _world.DynamicEntities[i];
                var b = _world.DynamicEntities[j];
                if (CollisionSolver.Overlap(a, b, _world))
                    _pairs.Add(new CollisionPair { EntityA = a, EntityB = b });
            }
        }

        for (var i = 0; i < _world.DynamicEntities.Count; i++)
        {
            for (var j = 0; j < _world.StaticEntities.Count; j++)
            {
                var a = _world.DynamicEntities[i];
                var b = _world.StaticEntities[j];
                if (CollisionSolver.Overlap(a, b, _world))
                    _pairs.Add(new CollisionPair { EntityA = a, EntityB = b });
            }
        }
    }

    private void ResolveCollisionPairs()
    {
        foreach (var pair in _pairs)
            CollisionSolver.ResolvePair(pair, _world, _settings);
    }
}
