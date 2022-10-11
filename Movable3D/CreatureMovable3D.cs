// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using EnhancedEditor;
using EnhancedFramework.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EnhancedFramework.Movable3D {
    /// <summary>
    /// <see cref="CreatureMovable3D"/>-related path wrapper class.
    /// </summary>
    [Serializable]
    public class CreatureMovable3DPath {
        #region Global Members
        /// <summary>
        /// The minimum distance between the object position and its current destination to be considered as reached, on the X & Z axises.
        /// </summary>
        public const float MinFlatDestinationDistance = .01f;

        /// <summary>
        /// The minimum distance between the object position and its current destination to be considered as reached, on the Y axis.
        /// </summary>
        public const float MinVerticalDestinationDistance = .25f;

        // -----------------------

        /// <summary>
        /// Called when this path is completed.
        /// <para/>
        /// Parameters are: 
        /// <br/> • A boolean indicating whether the path was fully completed or not.
        /// <br/> • The associated <see cref="CreatureMovable3D"/>.
        /// </summary>
        public Action<bool, CreatureMovable3D> OnComplete = null;

        /// <summary>
        /// All positions contained in this path.
        /// </summary>
        [SerializeField] private List<Vector3> path = new List<Vector3>();

        /// <summary>
        /// The index of the current path destination position.
        /// </summary>
        [field: SerializeField, Enhanced, ReadOnly] public int Index { get; private set; } = -1;

        /// <summary>
        /// Indicate whether this path is currently active or not.
        /// </summary>
        public bool IsActive {
            get { return Index != -1; }
        }
        #endregion

        #region Navigation
        /// <summary>
        /// Get the next destination position on the path.
        /// </summary>
        /// <param name="_position">Next destination position on the path.</param>
        /// <returns>True if the path is active and a new destination was found, false otherwise.</returns>
        public bool NextPosition(out Vector3 _position) {
            if (IsActive) {
                _position = path[Index];
                return true;
            }

            _position = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Get the distance vector between the object and its next desination position.
        /// </summary>
        /// <param name="_movable">The object on the path.</param>
        /// <param name="_distance">The direction vector (not normalized) between the object and its next destination position.</param>
        /// <returns>True if the path is active and a new destination was found, false otherwise.</returns>
        public bool NextDistance(CreatureMovable3D _movable, out Vector3 _distance) {
            if (!IsActive) {
                _distance = Vector3.zero;
                return false;
            }

            Transform _transform = _movable.Transform;
            _distance = path[Index] - _transform.position;

            return true;
        }

        /// <summary>
        /// Get the direction to the next destination position.
        /// </summary>
        /// <param name="_movable">The object on the path.</param>
        /// <param name="_direction">The direction vector (not normalized) to the next destination position.</param>
        /// <returns><inheritdoc cref="NextDistance(CreatureMovable3D, out Vector3)" path="/returns"/></returns>
        public bool MoveNext(CreatureMovable3D _movable, out Vector3 _direction) {
            if (!NextDistance(_movable, out _direction)) {
                return false;
            }

            // Remove the vertical component of the direction.
            Quaternion _rotation = _movable.Transform.rotation;
            _direction = _direction.RotateInverse(_rotation).SetY(0f).Rotate(_rotation);

            return true;
        }

        /// <summary>
        /// Update the path next position based on the object current position.
        /// </summary>
        /// <param name="_movable">The object on the path.</param>
        /// <returns><inheritdoc cref="NextDistance(CreatureMovable3D, out Vector3)" path="/returns"/></returns>
        public bool UpdatePosition(CreatureMovable3D _movable) {
            if (!NextDistance(_movable, out Vector3 _distance)) {
                return false;
            }

            _distance = _distance.RotateInverse(_movable.Transform.rotation);

            if ((_distance.Flat().magnitude <= MinFlatDestinationDistance) && (Mathf.Abs(_distance.y) <= MinVerticalDestinationDistance)) {
                Index++;

                if (Index == path.Count) {
                    Index = -1;
                    OnComplete?.Invoke(true, _movable);

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Cancels this path navigation.
        /// </summary>
        /// <param name="_movable">The object on the path.</param>
        public void CancelPath(CreatureMovable3D _movable) {
            Index = -1;
            OnComplete?.Invoke(false, _movable);
        }
        #endregion

        #region Utility
        /// <summary>
        /// Set this path position.
        /// </summary>
        /// <param name="_destination">Destination position of this path.</param>
        /// <param name="_onComplete"><inheritdoc cref="OnComplete" path="/summary"/></param>
        public void SetPath(Vector3 _destination, Action<bool, CreatureMovable3D> _onComplete = null) {
            path.Clear();
            path.Add(_destination);

            Index = 0;
            OnComplete = _onComplete;
        }

        /// <summary>
        /// Set this path positions.
        /// </summary>
        /// <param name="_path">All positions to initialize this path with.</param>
        /// <param name="_onComplete"><inheritdoc cref="OnComplete" path="/summary"/></param>
        public void SetPath(Vector3[] _path, Action<bool, CreatureMovable3D> _onComplete = null) {
            path.Clear();
            path.AddRange(_path);

            Index = (_path.Length != 0) ? 0 : -1;
            OnComplete = _onComplete;
        }
        #endregion
    }

    /// <summary>
    /// Advanced <see cref="Movable3D"/> with the addition of various creature-like behaviours.
    /// </summary>
    public class CreatureMovable3D : Movable3D {
        #region Global Members
        public override CollisionSystem3DType CollisionType {
            get { return CollisionSystem3DType.Creature; }
        }

        [Section("Creature Movable")]

        [SerializeField, Enhanced, Required] protected CreatureMovable3DAttributes attributes = null;

        [Space(5f)]

        [SerializeField, Enhanced, ReadOnly] protected Vector3 forward = Vector3.zero;
        [SerializeField, Enhanced, ReadOnly] protected CreatureMovable3DPath path = new CreatureMovable3DPath();

        // -----------------------

        public override float ClimbHeight {
            get { return attributes.ClimbHeight; }
        }

        public override float SnapHeight {
            get { return attributes.SnapHeight; }
        }
        #endregion

        #region Enhanced Behaviour
        protected override void OnInit() {
            base.OnInit();

            attributes.Setup(this);
        }

        protected virtual void OnDestroy() {
            attributes.Unset(this);
        }
        #endregion

        #region Velocity
        public override void ResetVelocity() {
            base.ResetVelocity();

            ResetSpeed();
        }
        #endregion

        #region Speed
        /// <summary>
        /// Update this object speed for this frame (increase or decrease).
        /// </summary>
        protected virtual void UpdateSpeed() {
            if (!Controller.OnUpdateSpeed()) {
                return;
            }

            // Resets this object speed when not moving.
            Vector3 _movement = GetRelativeVector(Velocity.Movement);

            if (!Mathm.AreEquals(_movement.x, _movement.z, 0f)) {
                IncreaseSpeed();
            } else {
                DecreaseSpeed();
            }
        }

        /// <summary>
        /// Increases this object speed according to its curve.
        /// </summary>
        public virtual void IncreaseSpeed() {
            if (!Controller.OnIncreaseSpeed()) {
                return;
            }

            float _increase = DeltaTime;
            if (!IsGrounded) {
                _increase *= attributes.AirAccelCoef;
            }

            speed = attributes.MoveSpeed.EvaluateContinue(ID, _increase);
        }

        /// <summary>
        /// Decreases this object speed according to its curve.
        /// </summary>
        public virtual void DecreaseSpeed() {
            if (!Controller.OnDecreaseSpeed()) {
                return;
            }

            speed = attributes.MoveSpeed.Decrease(ID, DeltaTime);
        }

        /// <summary>
        /// Resets this object speed.
        /// <para/>
        /// Speed is the coefficient applied only to this object velocity movement.
        /// </summary>
        public virtual void ResetSpeed() {
            if (!Controller.OnResetSpeed()) {
                return;
            }

            speed = attributes.MoveSpeed.Reset(ID);
        }

        // -----------------------

        /// <summary>
        /// Get this object speed ratio according to its curve.
        /// </summary>
        public float GetSpeedRatio() {
            CurveValue _speed = attributes.MoveSpeed;
            return _speed.GetTimeRatio(ID);
        }

        /// <summary>
        /// Set this object speed ratio according to its curve.
        /// </summary>
        public virtual void SetSpeedRatio(float _ratio) {
            speed = attributes.MoveSpeed.EvaluatePercent(ID, _ratio);
        }
        #endregion

        #region Pathing
        /// <summary>
        /// Setup this object destination position.
        /// </summary>
        /// <inheritdoc cref="CreatureMovable3DPath.SetPath(Vector3, Action{bool, Movable3D})"/>
        public void SetPath(Vector3 _destination, Action<bool, Movable3D> _onComplete = null) {
            path.SetPath(_destination, _onComplete);
        }

        /// <summary>
        /// Setup this object path positions.
        /// </summary>
        /// <inheritdoc cref="CreatureMovable3DPath.SetPath(Vector3[], Action{bool, Movable3D})"/>
        public void SetPath(Vector3[] _path, Action<bool, Movable3D> _onComplete = null) {
            path.SetPath(_path, _onComplete);
        }
        #endregion

        #region Orientation
        /// <summary>
        /// Turns the creature on its Y axis.
        /// </summary>
        /// <param name="_right">Should the creature turn to its right or left side?</param>
        public void Turn(bool _right) {
            float _angle = attributes.TurnSpeed.EvaluateContinue(ID, DeltaTime) * _right.Signf();
            TurnTo(_angle);
        }

        /// <summary>
        /// Turns the creature on its Y axis at a specific angle.
        /// </summary>
        /// <param name="_angleIncrement">Local rotation angle increment.</param>
        public virtual void Turn(float _angleIncrement) {
            OffsetRotation(Quaternion.Euler(transform.up * _angleIncrement));
        }

        // -----------------------

        /// <summary>
        /// Turns the character on its Y axis to a specific forward.
        /// </summary>
        /// <param name="_angleIncrement">The creature forward angle increment target.</param>
        public void TurnTo(float _angleIncrement) {
            TurnTo(transform.forward.Rotate(Quaternion.Euler(transform.up * _angleIncrement)));
        }

        /// <summary>
        /// Turns the character on its Y axis to a specific forward.
        /// </summary>
        /// <param name="_forward">The creature new forward target.</param>
        public void TurnTo(Vector3 _forward) {
            forward = _forward;
        }
        #endregion

        #region Computation
        protected override void OnPreComputeVelocity() {
            // Follow path.
            if (path.MoveNext(this, out Vector3 _direction)) {
                forward = _direction.normalized;
                AddMovementVelocity(_direction.normalized);
            }

            base.OnPreComputeVelocity();

            UpdateSpeed();
        }

        protected override void OnPostComputeVelocity(ref FrameVelocity _velocity) {
            base.OnPostComputeVelocity(ref _velocity);

            // Clamp path velocity magnitude.
            if (path.MoveNext(this, out Vector3 _direction)) {
                _velocity.Movement = Vector3.ClampMagnitude(_velocity.Movement, _direction.magnitude);
            }
        }
        #endregion

        #region Collision Callback
        protected override void OnAppliedVelocity(FrameVelocity _velocity, CollisionInfos _infos) {
            // Path update.
            if (path.IsActive) {
                path.UpdatePosition(this);
            }

            // Forward rotation.
            if (!forward.IsNull()) {
                float _angle = Vector3.SignedAngle(transform.forward, forward, transform.up);

                if (_angle != 0f) {
                    _angle = Mathf.MoveTowards(0f, _angle, attributes.TurnSpeed.EvaluateContinue(ID, DeltaTime) * DeltaTime * 90f);
                    Turn(_angle);
                } else {
                    forward = Vector3.zero;
                    attributes.TurnSpeed.Reset(ID);
                }
            }

            base.OnAppliedVelocity(_velocity, _infos);
        }
        #endregion
    }
}
