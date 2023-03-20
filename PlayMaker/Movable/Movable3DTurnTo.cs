// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using HutongGames.PlayMaker;
using UnityEngine;

using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace EnhancedFramework.Physics3D.PlayMaker {
    /// <summary>
    /// <see cref="FsmStateAction"/> used to turn a <see cref="CreatureMovable3D"/> in a direction.
    /// </summary>
    [Tooltip("Turns a Movable3D in a direction.")]
    [ActionCategory("Movable 3D")]
    public class Movable3DTurnTo : FsmStateAction {
        #region Global Members
        // -------------------------------------------
        // Movable - Direction - Events
        // -------------------------------------------

        [Tooltip("The Movable instance to turn.")]
        [RequiredField, ObjectType(typeof(CreatureMovable3D))]
        public FsmObject Movable;

        [Tooltip("Forward direction to turn the Movable to.")]
        [RequiredField]
        public FsmVector3 Forward;

        [Tooltip("Event to send when the turn operation is stopped.")]
        public FsmEvent StopEvent;
        #endregion

        #region Behaviour
        public override void Reset() {
            base.Reset();

            Movable = null;
            Forward = null;
            StopEvent = null;
        }

        public override void OnEnter() {
            base.OnEnter();

            TurnTo();
            Finish();
        }

        // -----------------------

        private void TurnTo() {
            if (Movable.Value is CreatureMovable3D _movable) {
                _movable.TurnTo(Forward.Value, OnComplete);
            } else {
                OnComplete();
            }

            // ----- Local Method ----- \\

            void OnComplete() {
                Fsm.Event(StopEvent);
            }
        }
        #endregion
    }
}
