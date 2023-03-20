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
    /// <see cref="FsmStateAction"/> used to push a velocity coefficient on a <see cref="Movable3D"/>.
    /// </summary>
    [Tooltip("Pushes and apply a velocity coefficient on a Movable3D.")]
    [ActionCategory("Movable 3D")]
    public class Movable3DPushVelocityCoef : FsmStateAction {
        #region Global Members
        // -------------------------------------------
        // Movable - Coefficient
        // -------------------------------------------

        [Tooltip("The Movable instance to push a velocity coefficient on.")]
        [RequiredField, ObjectType(typeof(Movable3D))]
        public FsmObject Movable;

        [Tooltip("Velocity coefficient to push and apply.")]
        [RequiredField]
        public FsmFloat Coefficient;
        #endregion

        #region Behaviour
        public override void Reset() {
            base.Reset();

            Movable = null;
            Coefficient = null;
        }

        public override void OnEnter() {
            base.OnEnter();

            Push();
            Finish();
        }

        // -----------------------

        private void Push() {
            if (Movable.Value is Movable3D _movable) {
                _movable.PushVelocityCoef(Coefficient.Value);
            }
        }
        #endregion
    }
}
