using System.Numerics;
using LockStepCore.Base;

namespace LockStepCore.Physics;

public static class CollisionSolver
{
    public static bool Overlap(int idA, int idB, IComponentStore world)
    {
        if (!world.TryGetComponent<Transform>(idA, out var a) || !world.TryGetComponent<Transform>(idB, out var b))
            return false;

        return (GetShape(idA, world), GetShape(idB, world)) switch
        {
            (Shape.Circle, Shape.Circle) => OverlapCircleCircle(a, b, idA, idB, world),
            (Shape.Circle, Shape.Box) => OverlapCircleBox(a, b, idA, world),
            (Shape.Box, Shape.Circle) => OverlapCircleBox(b, a, idB, world),
            (Shape.Circle, Shape.Plane) => OverlapCirclePlane(a, b, idA, world),
            (Shape.Plane, Shape.Circle) => OverlapCirclePlane(b, a, idB, world),
            _ => false,
        };
    }

    public static void ResolvePair(in CollisionPair pair, IComponentStore world, in PhysicsSettings settings)
    {
        if (!world.TryGetComponent<Transform>(pair.EntityA, out var ta) || !world.TryGetComponent<Transform>(pair.EntityB, out var tb))
            return;

        var hasA = world.TryGetComponent<Rigidbody>(pair.EntityA, out var pa);
        var hasB = world.TryGetComponent<Rigidbody>(pair.EntityB, out var pb);

        switch (GetShape(pair.EntityA, world), GetShape(pair.EntityB, world))
        {
            case (Shape.Circle, Shape.Circle):
                ResolveCircleCircle(pair.EntityA, pair.EntityB, ref ta, ref pa, ref tb, ref pb, world, settings);
                break;
            case (Shape.Circle, Shape.Box):
                ResolveCircleBox(pair.EntityA, ref ta, ref pa, ref tb, world, settings);
                break;
            case (Shape.Box, Shape.Circle):
                ResolveCircleBox(pair.EntityB, ref tb, ref pb, ref ta, world, settings);
                break;
            case (Shape.Circle, Shape.Plane):
                ResolveCirclePlane(pair.EntityA, ref ta, ref pa, ref tb, world, settings);
                break;
            case (Shape.Plane, Shape.Circle):
                ResolveCirclePlane(pair.EntityB, ref tb, ref pb, ref ta, world, settings);
                break;
            default:
                return;
        }

        world.SetComponent(pair.EntityA, ta);
        world.SetComponent(pair.EntityB, tb);
        if (hasA)
            world.SetComponent(pair.EntityA, pa);
        if (hasB)
            world.SetComponent(pair.EntityB, pb);
    }

    private static void ResolveCircleCircle(int idA, int idB, ref Transform ta, ref Rigidbody pa, ref Transform tb, ref Rigidbody pb, IComponentStore world, in PhysicsSettings settings)
    {
        var radiusA = TryGetCircleRadius(idA, world, out var ra) ? ra : settings.DefaultRadius;
        var radiusB = TryGetCircleRadius(idB, world, out var rb) ? rb : settings.DefaultRadius;
        var radiusSum = radiusA + radiusB;
        var delta = tb.Position - ta.Position;
        var distance = delta.Length();
        if (distance >= radiusSum)
            return;

        var normal = distance > 1e-6f ? delta / distance : Vector3.UnitY;

        var massSum = pa.InverseMass + pb.InverseMass;
        if (massSum > 0f)
        {
            var push = radiusSum - distance;
            ta.Position -= normal * push * (pa.InverseMass / massSum);
            tb.Position += normal * push * (pb.InverseMass / massSum);
        }

        ResolveNormalImpulse(ref pa, ref pb, normal, settings.WallBounce);
    }

    private static void ResolveCircleBox(int circleId, ref Transform circle, ref Rigidbody ball, ref Transform box, IComponentStore world, in PhysicsSettings settings)
    {
        var radius = TryGetCircleRadius(circleId, world, out var r) ? r : settings.DefaultRadius;
        if (!TryGetColliderHalfExtents(box.EntityId, world, out var he))
            return;

        var closest = Vector3.Clamp(circle.Position, box.Position - he, box.Position + he);
        var delta = circle.Position - closest;
        var distance = delta.Length();
        if (distance >= radius)
            return;

        var normal = distance > 1e-6f ? delta / distance : -Vector3.UnitY;

        var invMassBox = 0f;
        if (world.TryGetComponent<Rigidbody>(box.EntityId, out var boxBody))
            invMassBox = boxBody.InverseMass;
        var massSum = ball.InverseMass + invMassBox;
        if (massSum > 0f)
        {
            var push = radius - distance;
            circle.Position += normal * push * (ball.InverseMass / massSum);
            box.Position -= normal * push * (invMassBox / massSum);
        }

        var vn = Vector3.Dot(ball.Velocity, normal);
        if (vn > 0f)
            ball.Velocity -= normal * (vn * (1f + settings.WallBounce));
    }

    private static void ResolveCirclePlane(int circleId, ref Transform circle, ref Rigidbody ball, ref Transform plane, IComponentStore world, in PhysicsSettings settings)
    {
        var radius = TryGetCircleRadius(circleId, world, out var r) ? r : settings.DefaultRadius;
        var height = plane.Position.Y;

        if (circle.Position.Y + radius <= height)
            return;

        circle.Position.Y = height + radius;
        if (ball.Velocity.Y < 0f)
            ball.Velocity.Y = -ball.Velocity.Y * settings.WallBounce;
    }

    private static void ResolveNormalImpulse(ref Rigidbody pa, ref Rigidbody pb, Vector3 normal, float bounce)
    {
        var rv = pb.Velocity - pa.Velocity;
        var vn = Vector3.Dot(rv, normal);
        if (vn >= 0f)
            return;

        var massSum = pa.InverseMass + pb.InverseMass;
        if (massSum <= 0f)
            return;

        var impulse = -(1f + bounce) * vn / massSum;
        pa.Velocity -= normal * (impulse * pa.InverseMass);
        pb.Velocity += normal * (impulse * pb.InverseMass);
    }

    private static bool OverlapCircleCircle(Transform a, Transform b, int idA, int idB, IComponentStore world)
    {
        var radiusSum = 0.5f * 2f;
        if (TryGetCircleRadius(idA, world, out var ra) && TryGetCircleRadius(idB, world, out var rb))
            radiusSum = ra + rb;
        return Vector3.DistanceSquared(a.Position, b.Position) < radiusSum * radiusSum;
    }

    private static bool OverlapCircleBox(Transform circle, Transform box, int circleId, IComponentStore world)
    {
        var radius = TryGetCircleRadius(circleId, world, out var r) ? r : 0.5f;
        if (!TryGetColliderHalfExtents(box.EntityId, world, out var he))
            return false;
        var closest = Vector3.Clamp(circle.Position, box.Position - he, box.Position + he);
        return Vector3.DistanceSquared(circle.Position, closest) < radius * radius;
    }

    private static bool OverlapCirclePlane(Transform circle, Transform plane, int circleId, IComponentStore world)
    {
        var radius = TryGetCircleRadius(circleId, world, out var r) ? r : 0.5f;
        return circle.Position.Y + radius > plane.Position.Y;
    }

    private static Shape GetShape(int id, IComponentStore world)
    {
        if (world.TryGetComponent<CircleCollider>(id, out _))
            return Shape.Circle;
        if (world.TryGetComponent<BoxCollider>(id, out _))
            return Shape.Box;
        if (world.TryGetComponent<PlaneCollider>(id, out _))
            return Shape.Plane;
        return Shape.None;
    }

    private static bool TryGetCircleRadius(int id, IComponentStore world, out float radius)
    {
        if (world.TryGetComponent<CircleCollider>(id, out var c))
        {
            radius = c.Radius;
            return true;
        }
        radius = 0f;
        return false;
    }

    private static bool TryGetColliderHalfExtents(int id, IComponentStore world, out Vector3 halfExtents)
    {
        if (world.TryGetComponent<BoxCollider>(id, out var b))
        {
            halfExtents = b.HalfExtents;
            return true;
        }
        halfExtents = default;
        return false;
    }
}
