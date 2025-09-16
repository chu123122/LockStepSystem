using UnityEngine;
using UnityEngine.Serialization;

namespace Client.Unit
{
    public class PhysicsBase : MonoBehaviour
    {
        public Vector3 currentVelocity;
        public Vector3 currentPhysicsPosition;
        public float ballRadius;

        protected virtual void Awake() 
        {
            currentVelocity = Vector3.zero;
            currentPhysicsPosition = transform.position;
            ballRadius = 0.5f;
        }
        
    }
}