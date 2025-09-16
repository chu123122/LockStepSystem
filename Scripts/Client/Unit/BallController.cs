using UnityEngine;

namespace Client.Unit
{
    public class BallController : PhysicsBase
    {
        public float ballRadius;

        protected override void Awake()
        {
            base.Awake();
            ballRadius = 0.5f;
        }

        private void Update()
        {
            this.RenderUpdate();
        }
    }
}