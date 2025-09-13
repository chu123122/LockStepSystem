using UnityEngine;

namespace Client
{
    public class InputManager:MonoSingleton<InputManager>
    {
        private PlayerInputState _playerInputState;
        private Camera _camera;

        private bool _haveInput = false;
        public override void Awake()
        {
            _playerInputState=new PlayerInputState();
        }

        private void Start()
        {
            _camera = Camera.main;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity,
                        LayerMask.GetMask("Ground")))
                {
                   Vector3 targetPosition = hit.point;
                   targetPosition.y = transform.position.y;
                   
                   _playerInputState.MovePos=targetPosition;
                   _haveInput=true;
                }
            }
        }

        public bool GetPlayerInput()
        {
            return _haveInput;
        }

        public void ResetInput()
        {
            _haveInput = false;
        }

        public PlayerInputState GetPlayerInputCommand()
        {
            PlayerInputState currentInput=_playerInputState;
            _playerInputState = new PlayerInputState();
            return currentInput;
        }
    }
}