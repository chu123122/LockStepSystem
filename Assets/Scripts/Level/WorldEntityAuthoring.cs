using UnityEngine;
using LockStepCore.Physics;

namespace Client.Level
{
    public class WorldEntityAuthoring : MonoBehaviour
    {
        public int entityId;
        public Shape shape = Shape.Circle;
        public float size = 0.5f;
        public bool isDynamic = true;
    }
}
