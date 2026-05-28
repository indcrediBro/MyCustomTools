# StateMachine

A generic, data-driven state machine for Unity built on ScriptableObjects. Write zero boilerplate per new entity — just configure assets in the Inspector.

---

## Overview

| Class | Role |
|---|---|
| `StateMachine<TOwner>` | MonoBehaviour engine — drives the active state each frame |
| `State<TOwner>` | ScriptableObject — holds an ordered list of behaviours |
| `StateBehaviour<TOwner>` | ScriptableObject — a single reusable chunk of logic |

`TOwner` is any `MonoBehaviour` (e.g. `PlayerController`, `EnemyAI`) that your behaviours need to read from or write to.

---

## Setup

### 1. Create a concrete StateMachine subclass

```csharp
// PlayerStateMachine.cs
using UnityEngine;
using StateMachine;

public class PlayerStateMachine : StateMachine<PlayerController> { }
```

Attach it to the same GameObject as your `PlayerController`, or override `FindOwner()` if the owner lives elsewhere.

### 2. Create a concrete State subclass

```csharp
// PlayerState.cs
using UnityEngine;
using StateMachine;

[CreateAssetMenu(menuName = "StateMachine/Player/State")]
public class PlayerState : State<PlayerController> { }
```

### 3. Create a concrete StateBehaviour subclass

```csharp
// PlayerMoveBehaviour.cs
using UnityEngine;
using StateMachine;

[CreateAssetMenu(menuName = "StateMachine/Player/Behaviours/Move")]
public class PlayerMoveBehaviour : StateBehaviour<PlayerController>
{
    public float Speed = 5f;

    public override void OnTick(StateMachine<PlayerController> machine, PlayerController owner)
    {
        float h = Input.GetAxis("Horizontal");
        owner.transform.Translate(Vector3.right * h * Speed * Time.deltaTime);
    }
}
```

### 4. Wire up in the Inspector

1. Create **State** assets via `Assets > Create > StateMachine/Player/State`.
2. Create **Behaviour** assets and drag them into a State's `Behaviours` list.
3. On the `PlayerStateMachine` component, populate **States** and set an **Initial State**.

---

## Lifecycle

Each frame the machine forwards calls to the active state, which forwards them to each behaviour in order:

```
Awake   → FindOwner()
Start   → Initialise() on all states → TransitionTo(InitialState)

Per frame:
  Update      → CurrentState.Tick()
  FixedUpdate → CurrentState.FixedTick()
  LateUpdate  → CurrentState.LateTick()

On transition:
  CurrentState.Exit() → PreviousState = CurrentState → CurrentState = next → next.Enter()
```

Override any of these hooks in your `StateBehaviour` subclass:

```csharp
public override void OnInitialise(StateMachine<TOwner> machine, TOwner owner) { }
public override void OnEnter     (StateMachine<TOwner> machine, TOwner owner) { }
public override void OnTick      (StateMachine<TOwner> machine, TOwner owner) { }
public override void OnFixedTick (StateMachine<TOwner> machine, TOwner owner) { }
public override void OnLateTick  (StateMachine<TOwner> machine, TOwner owner) { }
public override void OnExit      (StateMachine<TOwner> machine, TOwner owner) { }
public override void OnDrawSelected(StateMachine<TOwner> machine, TOwner owner) { } // Gizmos
```

---

## Transitions

Call from anywhere — a behaviour, external system, or UnityEvent:

```csharp
// By state name
machine.TransitionTo("Run");

// Return to the previous state
machine.TransitionToPrevious();
```

Transitioning to the already-active state is a no-op. `TransitionTo(State<TOwner>)` is private; always go through the name overload or `TransitionToPrevious()` from outside the machine.

### Listening for state changes

```csharp
playerStateMachine.OnStateChange += stateName => Debug.Log($"Now in: {stateName}");
```

---

## Shared Context (Blackboard)

Pass data between behaviours without coupling them together:

```csharp
// Write
machine.SetContext("IsGrounded", true);
machine.SetContext("LastHitBy", enemyTransform);

// Read (with optional fallback)
bool grounded = machine.GetContext<bool>("IsGrounded", fallback: false);

// Check / Remove
if (machine.HasContext("LastHitBy")) machine.RemoveContext("LastHitBy");
```

Values are boxed objects internally, so use consistent types per key.

---

## Debug

Enable **Log Transitions** on the `StateMachine` component in the Inspector to print every state change to the Console:

```
[PlayerStateMachine] Idle → Run
```

Gizmos from `OnDrawSelected` are automatically forwarded in the Editor when the GameObject is selected.

---

## Advanced

### Owner on a different GameObject

```csharp
public class EnemyStateMachine : StateMachine<EnemyController>
{
    protected override EnemyController FindOwner() =>
        transform.parent.GetComponent<EnemyController>();
}
```

### Triggering transitions from external code

```csharp
// Combat system
playerStateMachine.TransitionTo("Stagger");

// After a cutscene
playerStateMachine.TransitionTo("Idle");
```

---

## File Reference

| File | Purpose |
|---|---|
| `StateMachine.cs` | Core engine — copy as-is, subclass per owner type |
| `State.cs` | State container — subclass and add `[CreateAssetMenu]` |
| `StateBehaviour.cs` | Behaviour base — subclass per behaviour, add `[CreateAssetMenu]` |
