// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using EnhancedEditor;
using HutongGames.PlayMaker;
using System;
using UnityEngine;

using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace EnhancedFramework.Physics3D.PlayMaker {
    /// <summary>
    /// Base <see cref="FsmStateAction"/> used to make a <see cref="CreatureMovable3D"/> navigate to a position.
    /// </summary>
    public abstract class BaseMovable3DNavigateTo : BaseCreatureMovable3DFSM {
        #region Global Members
        // -------------------------------------------
        // Position - Rotation - Events
        // -------------------------------------------

        [Tooltip("Destination position to make the object navigate to.")]
        [RequiredField]
        public FsmOwnerDefault Position;

        [Tooltip("Whether to use the destination rotation or not.")]
        [RequiredField]
        public FsmBool UseRotation;

        [Tooltip("Event to send if the destination position was successfully reached.")]
        public FsmEvent SuccessEvent;

        [Tooltip("Event to send if the destination position was not successfully reached.")]
        public FsmEvent FailEvent;
        #endregion

        #region Behaviour
        private Action<bool, CreatureMovable3D> onCompleteCallback = null;

        // -----------------------

        public override void Reset() {
            base.Reset();

            UseRotation  = true;
            SuccessEvent = null;
            FailEvent    = null;
            Position     = null;
        }

        public override void OnEnter() {
            base.OnEnter();

            NavigateTo();
            Finish();
        }

        // -------------------------------------------
        // Behaviour
        // -------------------------------------------

        private void NavigateTo() {
            GameObject _gameObject = Fsm.GetOwnerDefaultTarget(Position);

            if (_gameObject.IsValid() && GetMovable(out CreatureMovable3D _movable)) {

                onCompleteCallback ??= OnComplete;
                _movable.NavigateTo(_gameObject.transform, UseRotation.Value, onCompleteCallback);

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

    /// <summary>
    /// <see cref="FsmStateAction"/> used to make a <see cref="CreatureMovable3D"/> navigate to a position.
    /// </summary>
    [Tooltip("Makes a Movable3D navigate to a position.")]
    [ActionCategory(CategoryName)]
    public sealed class Movable3DNavigateTo : BaseMovable3DNavigateTo {
        #region Global Members
        // -------------------------------------------
        //  Movable
        // -------------------------------------------

        [Tooltip("The Movable instance to navigate.")]
        [RequiredField, ObjectType(typeof(CreatureMovable3D))]
        public FsmObject Movable;
        #endregion

        #region Behaviour
        public override void Reset() {
            base.Reset();

            Movable = null;
        }

        // -------------------------------------------
        // Behaviour
        // -------------------------------------------

        public override bool GetMovable(out CreatureMovable3D _movable) {

            if (Movable.Value is CreatureMovable3D _temp) {
                _movable = _temp;
                return true;
            }

            _movable = null;
            return false;
        }
        #endregion
    }
}
