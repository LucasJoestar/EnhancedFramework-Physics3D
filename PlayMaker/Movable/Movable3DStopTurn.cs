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
    /// <see cref="FsmStateAction"/> used to stop the current turn operation of a <see cref="CreatureMovable3D"/>.
    /// </summary>
    [Tooltip("Stops the current turn operation of a Movable3D.")]
    [ActionCategory("Movable 3D")]
    public class Movable3DCompleteTurn : FsmStateAction {
        #region Global Members
        // -------------------------------------------
        // Movable
        // -------------------------------------------

        [Tooltip("The Movable instance to complete the turn operation.")]
        [RequiredField, ObjectType(typeof(CreatureMovable3D))]
        public FsmObject Movable;
        #endregion

        #region Behaviour
        public override void Reset() {
            base.Reset();

            Movable = null;
        }

        public override void OnEnter() {
            base.OnEnter();

            Stop();
            Finish();
        }

        // -----------------------

        private void Stop() {
            if (Movable.Value is CreatureMovable3D _movable) {
                _movable.StopTurnTo();
            }
        }
        #endregion
    }
}
