using UnityEngine;
using LockStepCore.Physics;

namespace Client.Level
{
    public class WorldEntityAuthoring : MonoBehaviour
    {
        public int entityId;
        public Shape shape = Shape.Circle;
        public float sizeX = 0.5f;
        public float sizeY = 0.5f;
        public float sizeZ = 0.5f;
        public bool isDynamic = true;
    }
}
