using System.Collections.Generic;
using Client.Unit;
using UnityEngine;

namespace Client
{
    public class PhysicsManager : MonoSingleton<PhysicsManager>
    {
        // 我们不再用GameObject，直接用Transform更高效
        public List<Transform> walls; 

        // 核心物理对象列表
        private readonly List<PhysicsBase> _physicsObjects = new List<PhysicsBase>();

        // 边界值，我们可以在Start时计算一次，后面直接用
        private float _topWallZ, _bottomWallZ, _rightWallX, _leftWallX;

        public override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            if (walls.Count >= 4)
            {
                _topWallZ = walls[0].position.z - walls[0].localScale.z / 2f;
                _bottomWallZ = walls[1].position.z + walls[1].localScale.z / 2f;
                _leftWallX = walls[2].position.x + walls[2].localScale.x / 2f;
                _rightWallX = walls[3].position.x - walls[3].localScale.x / 2f;
            }
            
            RefreshPhysicsObjects();
        }
        
        public void LogicUpdate()
        {
            foreach (var obj in _physicsObjects)
            {
                obj.currentVelocity *= 0.998f;
                obj.currentPhysicsPosition += obj.currentVelocity * GameClockManager.TIME_STEP;

                //  墙壁碰撞检测与响应
                // 检查X轴 (左右墙)
                if (obj.currentPhysicsPosition.x - obj.ballRadius < _leftWallX)
                {
                    obj.currentPhysicsPosition.x = _leftWallX + obj.ballRadius;
                    obj.currentVelocity.x *= -0.9f;
                }
                else if (obj.currentPhysicsPosition.x + obj.ballRadius > _rightWallX)
                {
                    obj.currentPhysicsPosition.x = _rightWallX - obj.ballRadius;
                    obj.currentVelocity.x *= -0.9f;
                }

                // 检查Z轴 (上下墙)
                if (obj.currentPhysicsPosition.z - obj.ballRadius < _bottomWallZ)
                {
                    obj.currentPhysicsPosition.z = _bottomWallZ + obj.ballRadius;
                    obj.currentVelocity.z *= -0.9f;
                }
                else if (obj.currentPhysicsPosition.z + obj.ballRadius > _topWallZ)
                {
                    obj.currentPhysicsPosition.z = _topWallZ - obj.ballRadius;
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

                    float distance = Vector3.Distance(ballA.currentPhysicsPosition, ballB.currentPhysicsPosition);
            
                    if (distance < (ballA.ballRadius + ballB.ballRadius))
                    {
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