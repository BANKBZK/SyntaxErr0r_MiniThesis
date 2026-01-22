using UnityEngine;
using UnityEngine.InputSystem;

namespace SyntaxError.Inputs
{
    public class InputManager : MonoBehaviour
    {
        [Header("Input Action Asset")]
        [SerializeField] private InputSystem_Actions _inputActions;

        // Public Properties for other scripts to read
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsJumpPressed { get; private set; }
        public bool IsInteractPressed { get; private set; }


        private void OnEnable()
        {
            if (_inputActions == null)
            {
                _inputActions = new InputSystem_Actions();
            }

            // Enable the Input Map
            _inputActions.Player.Enable();

            // Movement
            _inputActions.Player.Move.performed += i => MoveInput = i.ReadValue<Vector2>();
            _inputActions.Player.Move.canceled += i => MoveInput = Vector2.zero;

            // Look
            _inputActions.Player.Look.performed += i => LookInput = i.ReadValue<Vector2>();
            _inputActions.Player.Look.canceled += i => LookInput = Vector2.zero;

            // Sprint
            _inputActions.Player.Sprint.performed += i => IsSprinting = true;
            _inputActions.Player.Sprint.canceled += i => IsSprinting = false;

            // Jump
            _inputActions.Player.Jump.performed += i => IsJumpPressed = true;
            _inputActions.Player.Jump.canceled += i => IsJumpPressed = false;

            // Interact (กดปุ่ม E)
            _inputActions.Player.Interact.performed += i => IsInteractPressed = true;
            _inputActions.Player.Interact.canceled += i => IsInteractPressed = false;
        }

        private void OnDisable()
        {
            _inputActions.Player.Disable();
        }

    }

}