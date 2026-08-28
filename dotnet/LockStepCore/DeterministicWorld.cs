using System;
using System.Collections.Generic;
using System.Numerics;
using LockStepCore.Base;
using LockStepCore.Physics;

namespace LockStepCore;
public interface IComponentStore
{
    IReadOnlyList<int> DynamicEntities { get; }
    IReadOnlyList<int> StaticEntities { get; }
    bool TryGetComponent<T>(int id, out T component) where T : struct, IComponent;
    void SetComponent<T>(int id, T component) where T : struct, IComponent;
}

public struct PlayerFrameInput
{
    public int PlayerId;
    public Vector3 MoveTarget;
}

public class DeterministicWorld : IComponentStore
{
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

    public void Update(float dt)
    {
        ApplyInputs();
        _physicsSystem.Update(dt);
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
}