using TMPro;

namespace Client
{
    public class UIManager:MonoSingleton<UIManager>
    {
        public TMP_Text currentFrame;
        public TMP_Text worldHash;
        private GameClockManager _gameClockManager;
        private PhysicsManager _physicsManager;

        private void Start()
        {
            _gameClockManager = GameClockManager.Instance;
            _physicsManager= PhysicsManager.Instance;
        }

        private void Update()
        {
              currentFrame.text = _gameClockManager.currentLogicFrame.ToString();
              worldHash.text = _physicsManager.GetWorldStateHash().ToString();
        }
    }
}