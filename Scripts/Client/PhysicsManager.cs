using System.Collections.Generic;
using Client.Unit;
using UnityEngine;

namespace Client
{
    public class PhysicsManager : MonoSingleton<PhysicsManager>
    {
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
            for (int i = 0; i < _physicsObjects.Count; i++)
            {
                PhysicsBase obj = _physicsObjects[i];
                obj.currentVelocity *= 0.995f;
                obj.currentLogicPosition += obj.currentVelocity * GameClockManager.TIME_STEP;
                //obj.GetComponent<UnitController>()!=null
                
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
                    float distance=Vector3.Distance(ballA.transform.position, ballB.transform.position);
                    
                    if (logicDistance < (ballA.ballRadius + ballB.ballRadius))
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

        private void ResolveCollision(PhysicsBase a, PhysicsBase b)
        {
            (a.currentVelocity, b.currentVelocity) =
                (b.currentVelocity, a.currentVelocity);
          
        }
    }
}