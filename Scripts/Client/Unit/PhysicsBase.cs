using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Client.Unit
{
    public class PhysicsBase : MonoBehaviour
    {
        public Vector3 currentVelocity;
        public Vector3 currentLogicPosition;
        public float ballRadius;

        public float smoothTime;
        private Vector3 _velocity;
        protected virtual void Awake() 
        {
            currentVelocity = Vector3.zero;
            currentLogicPosition = transform.position;
            ballRadius = 0.5f;
            //过小，会导致渲染帧等待逻辑帧，出现停顿现象
            //过大，会导致渲染帧未更新完逻辑帧就进行下一次更新，出现隔空碰撞现象
            smoothTime = 0.25f;
            _velocity = Vector3.zero;
        }
        

        protected virtual void RenderUpdate()
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                currentLogicPosition,
                ref _velocity,
                smoothTime);
        }
        
    }
}