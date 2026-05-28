using System.Collections.Generic;
using IncredibleAttributes;
using UnityEngine;

namespace StateMachine
{
    /// <summary>
    /// A single state in a StateMachine&lt;TOwner&gt;.
    /// Holds an ordered list of StateBehaviour&lt;TOwner&gt; assets and forwards
    /// every lifecycle call to each one.
    ///
    /// Do not use this directly — create a concrete subclass per owner type
    /// and apply [CreateAssetMenu] there so Unity can create assets from it:
    ///
    /// <code>
    /// [CreateAssetMenu(menuName = "StateMachine/Player/State")]
    /// public class PlayerState : State&lt;PlayerController&gt; { }
    /// </code>
    /// </summary>
    public abstract class State<TOwner> : ScriptableObject
        where TOwner : MonoBehaviour
    {
        [Header("Identity")]
        [Tooltip("Human-readable name used for transitions and debug.")]
        public string StateName = "NewState";

        [Header("Behaviours")]
        [Tooltip("Behaviours executed in order. Drag ScriptableObject behaviour assets here."), Expandable]
        public List<StateBehaviour<TOwner>> Behaviours;

        // ── Forwarded lifecycle ────────────────────────────────────────────────

        public void Initialise(StateMachine<TOwner> machine, TOwner owner)
        {
            foreach (var b in Behaviours) b?.OnInitialise(machine, owner);
        }

        public void Enter(StateMachine<TOwner> machine, TOwner owner)
        {
            foreach (var b in Behaviours) b?.OnEnter(machine, owner);
        }

        public void Tick(StateMachine<TOwner> machine, TOwner owner)
        {
            foreach (var b in Behaviours) b?.OnTick(machine, owner);
        }

        public void FixedTick(StateMachine<TOwner> machine, TOwner owner)
        {
            foreach (var b in Behaviours) b?.OnFixedTick(machine, owner);
        }

        public void LateTick(StateMachine<TOwner> machine, TOwner owner)
        {
            foreach (var b in Behaviours) b?.OnLateTick(machine, owner);
        }

        public void Exit(StateMachine<TOwner> machine, TOwner owner)
        {
            foreach (var b in Behaviours) b?.OnExit(machine, owner);
        }

        public void DrawSelected(StateMachine<TOwner> machine, TOwner owner)
        {
            foreach (var b in Behaviours) b?.OnDrawSelected(machine, owner);
        }

        public override string ToString() => StateName;
    }

}