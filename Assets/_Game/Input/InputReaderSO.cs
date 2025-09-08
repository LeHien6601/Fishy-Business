using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputReaderSO", menuName = "InputReader")]
public class InputReaderSO : ScriptableObject, InputSystem_Actions.IPlayerActions
{
    private InputSystem_Actions _gameInput;
    public event UnityAction<Vector2> Move = delegate { };
    public event UnityAction<Vector2> Look = delegate { };
    public event UnityAction Attack = delegate { };
    public event UnityAction Interact = delegate { };
    public event UnityAction<Vector2> Scroll = delegate { };


    public event UnityAction<float> Navigate = delegate { };
    public event UnityAction Select = delegate { };
    public event UnityAction Confirm = delegate { };

    public enum ActionMap
    {
        Player,
        TableTop,
        UI
    }


    private void OnEnable()
    {
        _gameInput = new InputSystem_Actions();
        _gameInput.Player.SetCallbacks(this);
        _gameInput.Player.Enable();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        Move.Invoke(context.ReadValue<Vector2>());
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        Look.Invoke(context.ReadValue<Vector2>());
    }

    public void OnScroll(InputAction.CallbackContext context)
    {
        Scroll.Invoke(context.ReadValue<Vector2>());
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            Attack.Invoke();
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            Interact.Invoke();
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
    }

    public void OnPrevious(InputAction.CallbackContext context)
    {
    }

    public void OnNext(InputAction.CallbackContext context)
    {
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
    }

    private void OnDisable()
    {
        if (_gameInput != null)
        {
            _gameInput.Player.Disable();
            _gameInput.UI.Disable();

            _gameInput.Player.SetCallbacks(null);
            _gameInput.UI.SetCallbacks(null);

            _gameInput = null;
        }
    }

    public void OnNavigate(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            Debug.Log("navigate");
            Navigate?.Invoke(context.ReadValue<float>());
        }
    }

    public void OnSelect(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
            Select.Invoke();
    }

    public void OnConfirm(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
            Confirm.Invoke();
    }
}

