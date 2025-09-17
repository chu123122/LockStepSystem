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
            smoothTime = 0.3f;
            _velocity = Vector3.zero;
        }
        

        protected virtual void RenderUpdate()
        {
            string message = $"<color=orange>Render UPDATE! " +
                             $"Name: {gameObject.name}, " +
                             $"Logic Pos: {currentLogicPosition}, " +
                             $"Render Pos: {transform.position}</color>";
            if(gameObject.CompareTag("Debug"))
                Debug.Log(message);
            if (ClientManager.Instance.testDic.TryGetValue(gameObject.name, out List<string> strings))
            {
                strings.Add(message);
            }
            else
            {
                ClientManager.Instance.testDic.Add(gameObject.name, new List<string>()
                {
                    message
                });
            }
            transform.position = currentLogicPosition;
            // if (ClientManager.Instance.test < 2)
            // {
            //     transform.position = currentLogicPosition;
            // }
            // else
            // {
            //     transform.position = Vector3.SmoothDamp(
            //         transform.position,
            //         currentLogicPosition,
            //         ref _velocity,
            //         smoothTime);
            // }

          
            // transform.position = Vector3.MoveTowards(
            //     transform.position,
            //     currentLogicPosition,
            //     Time.deltaTime * 10f);
        }
        
    }
}