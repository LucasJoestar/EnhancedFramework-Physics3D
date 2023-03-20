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
    /// <see cref="FsmStateAction"/> used to make a <see cref="CreatureMovable3D"/> navigate to a position.
    /// </summary>
    [Tooltip("Makes a Movable3D navigate to a position.")]
    [ActionCategory("Movable 3D")]
    public class Movable3DNavigateTo : FsmStateAction {
        #region Global Members
        // -------------------------------------------
        // Position - Movable - Rotation - Events
        // -------------------------------------------

        [Tooltip("Destination position to make the object navigate to.")]
        [RequiredField]
        public FsmOwnerDefault Position;

        [Tooltip("The Movable instance to navigate.")]
        [RequiredField, ObjectType(typeof(CreatureMovable3D))]
        public FsmObject Movable;

        [Tooltip("Whether to use the destination rotation or not.")]
        [RequiredField]
        public FsmBool UseRotation;

        [Tooltip("Event to send if the destination position was successfully reached.")]
        public FsmEvent SuccessEvent;

        [Tooltip("Event to send if the destination position was not successfully reached.")]
        public FsmEvent FailEvent;
        #endregion

        #region Behaviour
        public override void Reset() {
            base.Reset();

            Position = null;
            Movable = null;
            UseRotation = true;
            SuccessEvent = null;
            FailEvent = null;
        }

        public override void OnEnter() {
            base.OnEnter();

            NavigateTo();
            Finish();
        }

        // -----------------------

        private void NavigateTo() {
            GameObject _gameObject = Fsm.GetOwnerDefaultTarget(Position);

            if (_gameObject.IsValid() && (Movable.Value is CreatureMovable3D _movable)) {
                _movable.NavigateTo(_gameObject.transform, UseRotation.Value, OnComplete);
            } else {
                OnComplete(false, null);
            }

            // ----- Local Method ----- \\

            void OnComplete(bool _success, CreatureMovable3D _movable) {
                Fsm.Event(_success ? SuccessEvent : FailEvent);
            }
        }
        #endregion
    }
}
