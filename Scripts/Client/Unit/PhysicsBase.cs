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
            smoothTime = 0.1f;
            _velocity = Vector3.zero;
        }
        

        protected virtual void RenderUpdate()
        {
            if (gameObject.CompareTag("Debug"))
            {
                Debug.Log($"<color=orange>Render UPDATE! " +
                          $"Name: {gameObject.name}, " +
                          $"Logic Pos: {currentLogicPosition}, " +
                          $"Render Pos: {transform.position}</color>");
            }
          

            transform.position = Vector3.SmoothDamp(
                transform.position,
                currentLogicPosition,
                ref _velocity,
                smoothTime);
            // transform.position = Vector3.MoveTowards(
            //     transform.position,
            //     currentLogicPosition,
            //     Time.deltaTime * 10f);
        }
        
    }
}