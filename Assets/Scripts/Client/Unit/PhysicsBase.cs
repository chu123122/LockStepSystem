using UnityEngine;

namespace Client.Unit
{
    public class PhysicsBase : MonoBehaviour
    {
        public Vector3 currentLogicPosition;
        public float smoothTime;
        private Vector3 _velocity;

        protected virtual void Awake()
        {
            currentLogicPosition = transform.position;
            smoothTime = 0.25f;
            _velocity = Vector3.zero;
        }

        protected virtual void RenderUpdate()
        {
            if (GameClockManager.Instance.IsReplayTime())
            {
                smoothTime = 0.03f;
            }
            else
            {
                smoothTime = 0.25f;
            }

            transform.position = Vector3.SmoothDamp(
                transform.position,
                currentLogicPosition,
                ref _velocity,
                smoothTime);
        }
    }
}
