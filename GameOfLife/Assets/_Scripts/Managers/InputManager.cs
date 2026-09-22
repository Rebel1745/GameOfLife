using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    // Events
    public event Action OnSimulationToggled;
    public event Action OnRandomiseRequested;
    public event Action OnClearRequested;
    public event Action OnStepRequested;
    public event Action<Vector2> OnCellLeftClicked;
    public event Action<Vector2> OnCellRightClicked;
    public event Action<Vector2> OnNavigationInputChanged;

    // these are not used at the moment as painting is not implemented
    // private bool _isLeftClickHeld = false;
    // private bool _isRightClickHeld = false;

    private GameInput _inputActions;
    private Vector2 _navigationInput;

    void Awake()
    {
        if (Instance == null) Instance = this;

        _inputActions = new GameInput();

        // Wire up Actions
        _inputActions.Gameplay.ToggleSimulation.performed += ctx => OnSimulationToggled?.Invoke();
        _inputActions.Gameplay.Randomise.performed += ctx => OnRandomiseRequested?.Invoke();
        _inputActions.Gameplay.Clear.performed += ctx => OnClearRequested?.Invoke();
        _inputActions.Gameplay.Step.performed += ctx => OnStepRequested?.Invoke();

        _inputActions.Gameplay.LeftClick.started += ctx => OnCellLeftClicked?.Invoke(_inputActions.Gameplay.PointerPosition.ReadValue<Vector2>());
        _inputActions.Gameplay.RightClick.started += ctx => OnCellRightClicked?.Invoke(_inputActions.Gameplay.PointerPosition.ReadValue<Vector2>());

        _inputActions.Gameplay.Navigation.performed += NavigationChanged;

        // _inputActions.Gameplay.LeftClick.started += ctx => _isLeftClickHeld = true;
        // _inputActions.Gameplay.LeftClick.canceled += ctx => _isLeftClickHeld = false;

        // _inputActions.Gameplay.RightClick.started += ctx => _isRightClickHeld = true;
        // _inputActions.Gameplay.RightClick.canceled += ctx => _isRightClickHeld = false;

        _inputActions.Gameplay.Enable();
    }

    private void NavigationChanged(InputAction.CallbackContext context)
    {
        Vector2 lastInput = _navigationInput;
        _navigationInput = context.ReadValue<Vector2>();

        if (lastInput != _navigationInput && _navigationInput != Vector2.zero)
            OnNavigationInputChanged?.Invoke(_navigationInput);

    }

    private void Update()
    {
        // if (_isLeftClickHeld) OnCellLeftClicked?.Invoke(_inputActions.Gameplay.PointerPosition.ReadValue<Vector2>());
        // if (_isRightClickHeld) OnCellRightClicked?.Invoke(_inputActions.Gameplay.PointerPosition.ReadValue<Vector2>());
    }

    void OnDestroy()
    {
        _inputActions.Gameplay.Disable();
    }
}