import imageImport from "@/assets/voice-indicator.png";
import { DevlogEntry } from "@/data/devlog";

export const templateDevlog: DevlogEntry = {
  id: "fsm-playercontroller-2026-05-03",
  title: "FSM: Clean, Event-Driven Player State Machine",
  teaser: "Why `PlayerController`'s FSM is tidy, event-driven, and how `JumpState` demonstrates the pattern.",
  image: imageImport,
  date: "2026-05-03",
  content: `# FSM: Clean, Event-Driven Player State Machine

## Overview

The player's FSM is a compact, event-driven design: \`PlayerController\` wires input and game events to small state objects implementing \`IState\`. States encapsulate behavior (enter/tick/exit) so the controller stays clean.

## \`IState\` — tiny and explicit

\`\`\`csharp
public interface IState
{
    public void OnEnter();
    public void OnTick();
    public void OnExit();
}
\`\`\`

## \`PlayerController\` — wiring states and inputs

On network spawn \`PlayerController\` constructs state instances, subscribes to input/events, and keeps the controller logic minimal by delegating to the current state.

Key excerpt (state setup & input hooks):

\`\`\`csharp
public override void OnNetworkSpawn()
{
    if (!IsOwner)
    {
        _interactor.gameObject.SetActive(false);
        return;
    }
    base.OnNetworkSpawn();
    Animator animator = GetComponent<Animator>();
    gameObject.GetOrAdd<ObjectFader>();
    _idleState = new IdleState(this, animator);
    _moveState = new MoveState(this, animator, _moveSpeed);
    _jumpState = new JumpState(this, animator, _onAirSpeed, _initJumpVelocity);
    _attackState = new AttackState(animator);
    _sitState = new SitState(this, animator);
    _emoteState = new EmoteState(this, animator);
    _currentState = _idleState;

    _inputReader.Move += HandleMove;
    _inputReader.Attack += HandleAttack;
    _inputReader.Interact += HandleInteract;
    _inputReader.Interact += _interactor.Interact;
}
\`\`\`

Core movement handler stays small — it computes direction and switches states:

\`\`\`csharp
private void HandleMove(Vector2 arg0)
{
    MoveDirection = new Vector3(arg0.x, 0, arg0.y).normalized;
    if (_currentState == _attackState || _currentState == _sitState || _currentState == _jumpState)
        return;
    if (MoveDirection == Vector3.zero)
        ToState(_idleState);
    else
        ToState(_moveState);
}
\`\`\`

State transition helper is explicit and predictable:

\`\`\`csharp
private void ToState(IState newState)
{
    if (_currentState == newState) return;
    _currentState.OnExit();
    _currentState = newState;
    _currentState.OnEnter();
}
\`\`\`

## \`JumpState\` — an example implementation

\`JumpState\` demonstrates how a state encapsulates startup impulse, per-frame airborne control, and rotation while keeping physics integration inside the state.

\`\`\`csharp
using UnityEngine;

public class JumpState : IState
{
    protected static readonly int _animHash = Animator.StringToHash("Jump");
    protected Animator _animator;
    protected PlayerController _host;
    private readonly float _defaultJumpVelocityX = 6f;
    private readonly float _initJumpVelocityY = 20f;
    private readonly float _airAcceleration = 4f;
    private readonly float _rotationSpeed = 500f;

    private Vector3 _currentHorizontalMomentum;
    public JumpState(PlayerController host, Animator animator, float moveSpeed = 5f, float initJumpVelocityY = 20f)
    {
        _host = host;
        _animator = animator;
        _defaultJumpVelocityX = moveSpeed;
        _initJumpVelocityY = initJumpVelocityY;
    }
    public virtual void OnEnter()
    {
        _animator.Play(_animHash);
        _host.Movement = _host.PreviousMovement + Vector3.up * _initJumpVelocityY;
        _currentHorizontalMomentum = _host.MoveDirection * _defaultJumpVelocityX;
    }

    public virtual void OnExit()
    {
    }

    public virtual void OnTick()
    {
        Vector3 targetDirection;
        if (_host.MoveDirection == Vector3.zero)
        {
            float decayTargetSpeed = _defaultJumpVelocityX * 0.5f;
            _currentHorizontalMomentum = Vector3.Lerp(_currentHorizontalMomentum, _currentHorizontalMomentum.normalized * decayTargetSpeed, _airAcceleration * Time.deltaTime);
        }
        else
        {
            float _targetAngle = Mathf.Atan2(_host.MoveDirection.x, _host.MoveDirection.z) * Mathf.Rad2Deg + Camera.main.transform.eulerAngles.y;
            Quaternion desiredRotation = Quaternion.Euler(0.0f, _targetAngle, 0.0f);
            _host.transform.rotation = Quaternion.RotateTowards(_host.transform.rotation, desiredRotation, _rotationSpeed * Time.deltaTime);
            targetDirection = desiredRotation * Vector3.forward;

            _currentHorizontalMomentum = targetDirection * _defaultJumpVelocityX;
        }
        _currentHorizontalMomentum.y = _host.Movement.y;
        _host.Movement = _currentHorizontalMomentum;
    }
}
\`\`\`

## Short takeaways
- Keep the controller as an event router — states own behavior.
- Small interface (\`IState\`) makes states easy to test and reason about.
- \`JumpState\` shows how to encapsulate animation, initial impulse, and per-frame control.
`,
};