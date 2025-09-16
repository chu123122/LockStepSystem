using UnityEngine;
using UnityEngine.Serialization;

namespace Client.Unit
{
    public class PhysicsBase : MonoBehaviour
    {
        public Vector3 currentVelocity;
        public Vector3 currentLogicPosition;
      

        public float smoothTime;
        private Vector3 _velocity;
        protected virtual void Awake() 
        {
            currentVelocity = Vector3.zero;
            currentLogicPosition = transform.position;
            smoothTime = 0.4f;
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