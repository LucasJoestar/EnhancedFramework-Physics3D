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
    /// <see cref="FsmStateAction"/> used to move a <see cref="Movable3D"/> in a direction.
    /// </summary>
    [Tooltip("Moves a Movable3D in a direction.")]
    [ActionCategory("Movable 3D")]
    public class Movable3DMove : FsmStateAction {
        #region Global Members
        // -------------------------------------------
        // Movable - Velocity - Every Frame
        // -------------------------------------------

        [Tooltip("The Movable instance to move.")]
        [RequiredField, ObjectType(typeof(Movable3D))]
        public FsmObject Movable;

        [Tooltip("Direction used to move the object.")]
        [RequiredField]
        public FsmVector3 Direction;

        [Tooltip("Repeat every frame.")]
        public bool EveryFrame;
        #endregion

        #region Behaviour
        public override void Reset() {
            base.Reset();

            Movable = null;
            Direction = null;
            EveryFrame = false;
        }

        public override void OnEnter() {
            base.OnEnter();

            Move();

            if (!EveryFrame) {
                Finish();
            }
        }

        public override void OnUpdate() {
            base.OnUpdate();

            Move();
        }

        // -----------------------

        private void Move() {
            if (Movable.Value is Movable3D _movable) {

                _movable.Move(Direction.Value);
            }
        }
        #endregion
    }
}
