// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using EnhancedFramework.Core;
using HutongGames.PlayMaker;
using UnityEngine;

using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace EnhancedFramework.Physics3D.PlayMaker {
    /// <summary>
    /// <see cref="FsmStateAction"/> used to teleport a <see cref="Movable3D"/> to a specific position.
    /// </summary>
    [Tooltip("Teleports a Movable3D to a specific position in space.")]
    [ActionCategory("Movable 3D")]
    public class Movable3DTeleportTo : FsmStateAction {
        #region Global Members
        // -------------------------------------------
        // Position - Movable
        // -------------------------------------------

        [Tooltip("Destination position to teleport the Movable to.")]
        [RequiredField]
        public FsmOwnerDefault Position;

        [Tooltip("The Movable instance to teleport.")]
        [RequiredField, ObjectType(typeof(Movable3D))]
        public FsmObject Movable;
        #endregion

        #region Behaviour
        public override void Reset() {
            base.Reset();

            Position = null;
        }

        public override void OnEnter() {
            base.OnEnter();

            Move();
            Finish();
        }

        // -----------------------

        private void Move() {
            GameObject _gameObject = Fsm.GetOwnerDefaultTarget(Position);

            if (_gameObject.IsValid() && (Movable.Value is Movable3D _movable)) {
                _movable.SetPositionAndRotation(_gameObject.transform);
            }
        }
        #endregion
    }
}
