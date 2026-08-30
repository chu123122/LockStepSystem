using System;
using System.Collections.Generic;
using System.Numerics;
using LockStepCore.Base;
using LockStepCore.Hash;
using LockStepCore.Level;
using LockStepCore.Physics;

namespace LockStepCore;

public interface IComponentStore
{
    IReadOnlyList<int> DynamicEntities { get; }
    IReadOnlyList<int> StaticEntities { get; }
    bool TryGetComponent<T>(int id, out T component) where T : struct, IComponent;
    void SetComponent<T>(int id, T component) where T : struct, IComponent;
}

public class DeterministicWorld : IComponentStore
{
    private readonly IFrameHasher _hasher = new Fnv1A64Hasher();
    private const int MaxEntities = 1024;
    private readonly Dictionary<Type, Dictionary<int, IComponent>> _componentStores = new();
    private readonly List<int> _dynamicIds = new();
    private readonly List<int> _staticIds = new();
    private readonly int[] _entities = new int[MaxEntities];
    private int _entityCount;
    private int _nextEntityId;
    private readonly PhysicsSettings _settings;
    private readonly Dictionary<int, int> _playerEntities = new();
    private readonly List<PlayerFrameInput> _pendingInputs = new();
    private readonly PhysicsSystem _physicsSystem;

    public IReadOnlyList<int> DynamicEntities => _dynamicIds;
    public IReadOnlyList<int> StaticEntities => _staticIds;
    public int EntityCount => _entityCount;
    public ReadOnlySpan<int> Entities => _entities.AsSpan(0, _entityCount);

    public DeterministicWorld() : this(PhysicsSettings.Default)
    {
    }

    public DeterministicWorld(PhysicsSettings settings)
    {
        _settings = settings;
        _physicsSystem = new PhysicsSystem(this, settings);
    }

    public void InitWorld(LevelData data)
    {
        foreach (EntitySpawn s in data.Spawns)
        {
            CreateEntity(s.EntityId);
            AddComponent(s.EntityId, new Transform { EntityId = s.EntityId, Position = new Vector3(s.X, s.Y, s.Z) });
            switch (s.Shape)
            {
                case Shape.Circle:
                    AddComponent(s.EntityId, new CircleCollider { EntityId = s.EntityId, Radius = s.SizeX });
                    break;
                case Shape.Box:
                    AddComponent(s.EntityId,
                        new BoxCollider
                            { EntityId = s.EntityId, HalfExtents = new Vector3(s.SizeX, s.SizeY, s.SizeZ) });
                    break;
            }

            if (s.IsDynamic)
                AddComponent(s.EntityId,
                    new Rigidbody { EntityId = s.EntityId, Velocity = Vector3.Zero, InverseMass = 1f });
        }
    }

    public void Update(float dt)
    {
        ApplyInputs();
        _physicsSystem.Update(dt);
    }

    public int CreatePlayerEntity(int playerId, Vector3 position, float radius)
    {
        int entityId = CreateEntity();
        AddComponent(entityId, new Transform { EntityId = entityId, Position = position });
        AddComponent(entityId, new Rigidbody { EntityId = entityId, Velocity = Vector3.Zero, InverseMass = 1f });
        AddComponent(entityId, new CircleCollider { EntityId = entityId, Radius = radius });
        BindPlayerEntity(playerId, entityId);
        return entityId;
    }

    public void BindPlayerEntity(int playerId, int entityId)
    {
        _playerEntities[playerId] = entityId;
    }

    public void SetFrameInputs(List<PlayerFrameInput> inputs)
    {
        _pendingInputs.Clear();
        _pendingInputs.AddRange(inputs);
    }

    private void ApplyInputs()
    {
        foreach (var input in _pendingInputs)
        {
            if (!_playerEntities.TryGetValue(input.PlayerId, out var entityId))
                continue;
            if (!TryGetComponent<Transform>(entityId, out var t))
                continue;
            if (!TryGetComponent<Rigidbody>(entityId, out var body))
                continue;

            var dir = input.MoveTarget - t.Position;
            if (dir.LengthSquared() > 1e-8f)
            {
                body.Velocity = Vector3.Normalize(dir) * _settings.MoveStrikeForce;
                SetComponent(entityId, body);
            }
        }

        _pendingInputs.Clear();
    }

    public int CreateEntity()
    {
        var id = _nextEntityId++;
        _entities[_entityCount++] = id;
        return id;
    }

    public int CreateEntity(int explicitId)
    {
        _entities[_entityCount++] = explicitId;
        if (explicitId >= _nextEntityId)
            _nextEntityId = explicitId + 1;
        return explicitId;
    }

    public void DestroyEntity(int id)
    {
        foreach (var store in _componentStores.Values)
            store.Remove(id);
        _dynamicIds.Remove(id);
        _staticIds.Remove(id);

        for (var i = 0; i < _entityCount; i++)
        {
            if (_entities[i] == id)
            {
                _entities[i] = _entities[_entityCount - 1];
                _entityCount--;
                break;
            }
        }
    }

    public T AddComponent<T>(int entityId, T component) where T : IComponent
    {
        if (!_componentStores.TryGetValue(typeof(T), out var store))
        {
            store = new Dictionary<int, IComponent>();
            _componentStores[typeof(T)] = store;
        }

        store[entityId] = component;
        RefreshClassification(entityId);
        return component;
    }

    public void RemoveComponent<T>(int entityId) where T : IComponent
    {
        if (_componentStores.TryGetValue(typeof(T), out var store))
            store.Remove(entityId);
        RefreshClassification(entityId);
    }

    public bool TryGetComponent<T>(int id, out T component) where T : struct, IComponent
    {
        component = default;
        if (!_componentStores.TryGetValue(typeof(T), out var store))
            return false;
        if (!store.TryGetValue(id, out var c))
            return false;
        component = (T)c;
        return true;
    }

    public void SetComponent<T>(int id, T component) where T : struct, IComponent
    {
        if (!_componentStores.TryGetValue(typeof(T), out var store))
        {
            store = new Dictionary<int, IComponent>();
            _componentStores[typeof(T)] = store;
        }

        store[id] = component;
    }

    private void RefreshClassification(int id)
    {
        _dynamicIds.Remove(id);
        _staticIds.Remove(id);

        if (HasComponent<Rigidbody>(id))
        {
            _dynamicIds.Add(id);
        }
        else if (HasComponent<CircleCollider>(id) || HasComponent<BoxCollider>(id) || HasComponent<PlaneCollider>(id))
        {
            _staticIds.Add(id);
        }
    }

    private bool HasComponent<T>(int id) where T : IComponent
    {
        return _componentStores.TryGetValue(typeof(T), out var store) && store.ContainsKey(id);
    }

    /// <summary>
    /// 依据所有Entity的位置和速度进行世界Hash的计算
    /// </summary>
    /// <returns></returns>
    public ulong ComputeFrameHash()
    {
        int[] ordered = new int[_entityCount];
        Array.Copy(_entities, ordered, _entityCount);
        Array.Sort(ordered);

        List<(Vector3 Position, Vector3 Velocity)> states =
            new List<(Vector3 Position, Vector3 Velocity)>(ordered.Length);
        for (int i = 0; i < ordered.Length; i++)
        {
            int id = ordered[i];

            if (!TryGetComponent<Transform>(id, out Transform t))
                continue;

            Vector3 velocity = Vector3.Zero;
            if (TryGetComponent<Rigidbody>(id, out Rigidbody rb))
                velocity = rb.Velocity;

            states.Add((t.Position, velocity));
        }

        return _hasher.Compute(states);
    }
}