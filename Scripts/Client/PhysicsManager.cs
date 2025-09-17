using System.Collections.Generic;
using Client.Unit;
using UnityEngine;

namespace Client
{
    public class PhysicsManager : MonoSingleton<PhysicsManager>
    {
        // 32位的FNV-1a哈希算法的初始“种子”和“质数”
        private const uint FnvPrime = 16777619;
        private const uint FnvOffsetBasis = 2166136261;
        public Vector2 worldSize;
        public Vector3 worldCenter = Vector3.zero;
        private readonly List<PhysicsBase> _physicsObjects = new List<PhysicsBase>();

        // 边界值
        private float _topWallZ, _bottomWallZ, _rightWallX, _leftWallX;


        public override void Awake()
        {
            base.Awake();
            worldSize = new Vector2(15f, 15f);
            worldCenter = new Vector3(7.5f, 0, 7.5f);

            _leftWallX = worldCenter.x - worldSize.x / 2f;
            _rightWallX = worldCenter.x + worldSize.x / 2f;
            _bottomWallZ = worldCenter.z - worldSize.y / 2f;
            _topWallZ = worldCenter.z + worldSize.y / 2f;
            Debug.Log(
                $"Physics Bounds Initialized: " +
                $"X({_leftWallX:F2} to {_rightWallX:F2}), " +
                $"Z({_bottomWallZ:F2} to {_topWallZ:F2})");
        }

        private void Start()
        {
            RefreshPhysicsObjects();
        }

        public void LogicUpdate()
        {
            // Debug.Log($"<color=green>Physics LOGIC TICK! " +
            //           $"Frame: {GameClockManager.Instance.currentLogicFrame}," +
            //           $" Object Count: {_physicsObjects.Count}</color>");
            for (int i = 0; i < _physicsObjects.Count; i++)
            {
                PhysicsBase obj = _physicsObjects[i];
                float velocityValue = Vector3.Magnitude(obj.currentVelocity);
                if (velocityValue < 0.1f)
                    obj.currentVelocity = Vector3.zero;
                else
                    Debug.Log(velocityValue);
                obj.currentVelocity *= 0.98f;
                obj.currentLogicPosition += obj.currentVelocity * GameClockManager.TIME_STEP;
                //  墙壁碰撞检测与响应
                // 检查X轴 (左右墙)
                if (obj.currentLogicPosition.x - obj.ballRadius < _leftWallX)
                {
                    obj.currentLogicPosition.x = _leftWallX + obj.ballRadius;
                    obj.currentVelocity.x *= -0.9f;
                }
                else if (obj.currentLogicPosition.x + obj.ballRadius > _rightWallX)
                {
                    obj.currentLogicPosition.x = _rightWallX - obj.ballRadius;
                    obj.currentVelocity.x *= -0.9f;
                }

                // 检查Z轴 (上下墙)
                if (obj.currentLogicPosition.z - obj.ballRadius < _bottomWallZ)
                {
                    obj.currentLogicPosition.z = _bottomWallZ + obj.ballRadius;
                    obj.currentVelocity.z *= -0.9f;
                }
                else if (obj.currentLogicPosition.z + obj.ballRadius > _topWallZ)
                {
                    obj.currentLogicPosition.z = _topWallZ - obj.ballRadius;
                    obj.currentVelocity.z *= -0.9f;
                }
            }

            // 对象间碰撞 (处理物体与物体之间的关系) 

            for (int i = 0; i < _physicsObjects.Count; i++)
            {
                for (int j = i + 1; j < _physicsObjects.Count; j++)
                {
                    var ballA = _physicsObjects[i];
                    var ballB = _physicsObjects[j];

                    float logicDistance = Vector3.Distance(ballA.currentLogicPosition, ballB.currentLogicPosition);
                    float distance = Vector3.Distance(ballA.transform.position, ballB.transform.position);

                    if (ballA.name != ballB.name &&
                        logicDistance < (ballA.ballRadius + ballB.ballRadius))
                    {
                        Debug.Log($"球体发生碰撞，" +
                                  $"球体A：{ballA.gameObject.name}," +
                                  $"球体B：{ballB.gameObject.name}");
                        ResolveCollision(ballA, ballB);
                    }
                }
            }
        }

        public void RefreshPhysicsObjects()
        {
            _physicsObjects.Clear();
            _physicsObjects.AddRange(FindObjectsOfType<PhysicsBase>());
        }

        public uint GetWorldStateHash()
        {
            uint hash = FnvOffsetBasis;

            _physicsObjects.Sort((a, b)
                => a.gameObject.GetInstanceID().CompareTo(b.gameObject.GetInstanceID()));
            foreach (var obj in _physicsObjects)
            {
                hash = UpdateHash(hash, obj.currentLogicPosition);
                hash = UpdateHash(hash, obj.currentVelocity);
            }

            return hash;
        }

// 把一个Vector3的二进制数据，“混合”进当前的哈希值
        private uint UpdateHash(uint currentHash, Vector3 value)
        {
            byte[] xBytes = System.BitConverter.GetBytes(value.x);
            byte[] yBytes = System.BitConverter.GetBytes(value.y);
            byte[] zBytes = System.BitConverter.GetBytes(value.z);

            for (int i = 0; i < 4; i++)
            {
                currentHash = (currentHash ^ xBytes[i]) * FnvPrime;
            }

            for (int i = 0; i < 4; i++)
            {
                currentHash = (currentHash ^ yBytes[i]) * FnvPrime;
            }

            for (int i = 0; i < 4; i++)
            {
                currentHash = (currentHash ^ zBytes[i]) * FnvPrime;
            }

            return currentHash;
        }

        private void ResolveCollision(PhysicsBase a, PhysicsBase b)
        {
            (a.currentVelocity, b.currentVelocity) =
                (b.currentVelocity, a.currentVelocity);
        }
    }
}