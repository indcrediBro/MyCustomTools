using System;
using System.Collections.Generic;
using IncredibleAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace StateMachine
{
    /// <summary>
    /// Generic, owner-agnostic state machine engine.
    /// TOwner is the MonoBehaviour whose data and components your behaviours need.
    ///
    /// ── Setup ─────────────────────────────────────────────────────────────────
    /// 1. Create a concrete subclass (e.g. PlayerStateMachine, EnemyStateMachine).
    /// 2. TOwner is retrieved via GetComponent on the same GameObject in Awake.
    ///    Override FindOwner() if your owner lives elsewhere.
    /// 3. Assign States and InitialState in the Inspector.
    ///
    /// ── Transitions ───────────────────────────────────────────────────────────
    /// Call TransitionTo(state) or TransitionTo("StateName") from anywhere:
    ///   - Inside a StateBehaviour
    ///   - From external code (combat system, cutscene, AI)
    ///   - From UnityEvents
    ///
    /// ── Shared data ───────────────────────────────────────────────────────────
    /// Use Context to pass transient data between behaviours without coupling:
    ///   machine.SetContext("IsGrounded", true);
    ///   bool grounded = machine.GetContext&lt;bool&gt;("IsGrounded");
    /// </summary>
    public abstract class StateMachine<TOwner> : MonoBehaviour
        where TOwner : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("States")] [Tooltip("All states this machine can enter. Order does not matter."), Expandable]
        public List<State<TOwner>> States = new();

        [Tooltip("State entered on Start. Falls back to States[0] if null.")]
        public State<TOwner> InitialState;

        [Header("Debug")] [SerializeField] private bool _logTransitions = false;

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>The MonoBehaviour this machine drives.</summary>
        public TOwner Owner { get; private set; }

        /// <summary>The state currently running.</summary>
        public State<TOwner> CurrentState { get; private set; }

        /// <summary>The state active immediately before the current one.</summary>
        public State<TOwner> PreviousState { get; private set; }

        /// <summary>
        /// Shared blackboard — lets behaviours exchange transient data without
        /// referencing each other. Keyed by string; values are boxed objects.
        /// </summary>
        public Dictionary<string, object> Context { get; } = new();

        public Action<String> OnStateChange;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        protected virtual void Awake()
        {
            Owner = FindOwner();

            if (Owner == null)
                Debug.LogError($"[StateMachine] Could not find owner of type {typeof(TOwner).Name} " +
                               $"on '{name}'. Override FindOwner() if the owner is on another GameObject.", this);
        }

        protected virtual void Start()
        {
            foreach (var state in States)
                state?.Initialise(this, Owner);

            var entry = InitialState ?? (States.Count > 0 ? States[0] : null);

            if (entry != null)
                TransitionTo(entry);
            else
                Debug.LogWarning($"[StateMachine] '{name}' has no states assigned.", this);
        }

        protected virtual void Update() => CurrentState?.Tick(this, Owner);
        protected virtual void FixedUpdate() => CurrentState?.FixedTick(this, Owner);
        protected virtual void LateUpdate() => CurrentState?.LateTick(this, Owner);
#if UNITY_EDITOR
        protected void OnDrawGizmosSelected() => CurrentState?.DrawSelected(this, Owner);
#endif
        // ── Owner resolution ───────────────────────────────────────────────────

        /// <summary>
        /// Override to provide the owner from a different location.
        /// Default: GetComponent&lt;TOwner&gt;() on the same GameObject.
        /// </summary>
        protected virtual TOwner FindOwner() => GetComponent<TOwner>();

        // ── Transitions ────────────────────────────────────────────────────────

        /// <summary>
        /// Transitions to <paramref name="next"/>.
        /// Calls Exit on current state then Enter on the new one.
        /// No-op if <paramref name="next"/> is already active.
        /// </summary>
        public void TransitionTo(State<TOwner> next)
        {
            if (next == null)
            {
                Debug.LogWarning("[StateMachine] TransitionTo called with null.", this);
                return;
            }

            if (next == CurrentState) return;

            if (_logTransitions)
                Debug.Log($"[{name}] {CurrentState?.StateName ?? "—"} → {next.StateName}");

            CurrentState?.Exit(this, Owner);
            PreviousState = CurrentState;
            CurrentState = next;
            CurrentState.Enter(this, Owner);

            OnStateChange?.Invoke(CurrentState.StateName);
        }

        /// <summary>Look up a state by name and transition to it.</summary>
        public void TransitionTo(string stateName)
        {
            var target = States.Find(s => s != null && s.StateName == stateName);
            if (target == null)
                Debug.LogWarning($"[StateMachine] No state named '{stateName}' found on '{name}'.", this);
            else
                TransitionTo(target);
        }

        /// <summary>Return to whatever state was active before the current one.</summary>
        public void TransitionToPrevious()
        {
            if (PreviousState != null) TransitionTo(PreviousState);
        }

        // ── Context helpers ────────────────────────────────────────────────────

        public void SetContext<T>(string key, T value) => Context[key] = value;
        public void RemoveContext(string key) => Context.Remove(key);
        public bool HasContext(string key) => Context.ContainsKey(key);

        public T GetContext<T>(string key, T fallback = default)
        {
            if (Context.TryGetValue(key, out var raw) && raw is T typed) return typed;
            return fallback;
        }
    }
}