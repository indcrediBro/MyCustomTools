using UnityEngine;

namespace StateMachine
{
    /// <summary>
    /// Abstract base for all state behaviours.
    /// TOwner is the MonoBehaviour that "owns" this state machine
    /// (e.g. PlayerController, EnemyController).
    ///
    /// Inherit from this, override the hooks you need, and create
    /// a ScriptableObject asset via [CreateAssetMenu] on your subclass.
    ///
    /// Lifecycle order per frame:
    ///   OnEnter → (OnUpdate → OnFixedUpdate → OnLateUpdate) × N → OnExit
    /// </summary>
    public abstract class StateBehaviour<TOwner> : ScriptableObject
        where TOwner : MonoBehaviour
    {
        // ── Lifecycle ──────────────────────────────────────────────────────────

        /// <summary>Called once for every state during StateMachine.Initialise().</summary>
        public virtual void OnInitialise(StateMachine<TOwner> machine, TOwner owner) { }

        /// <summary>Called every time the parent state becomes active.</summary>
        public virtual void OnEnter(StateMachine<TOwner> machine, TOwner owner) { }

        /// <summary>Called each frame while the parent state is active.</summary>
        public virtual void OnTick(StateMachine<TOwner> machine, TOwner owner) { }

        /// <summary>Called each fixed timestep while the parent state is active.</summary>
        public virtual void OnFixedTick(StateMachine<TOwner> machine, TOwner owner) { }

        /// <summary>Called after OnUpdate each frame while the parent state is active.</summary>
        public virtual void OnLateTick(StateMachine<TOwner> machine, TOwner owner) { }

        /// <summary>Called when the parent state is leaving and another is becoming active.</summary>
        public virtual void OnExit(StateMachine<TOwner> machine, TOwner owner) { }

        public virtual void OnDrawSelected(StateMachine<TOwner> machine, TOwner owner) { }
    }
}