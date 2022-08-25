// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using EnhancedEditor;
using EnhancedFramework.Core;
using EnhancedFramework.Physics3D;
using EnhancedFramework.Settings;
using System;
using UnityEngine;

namespace EnhancedFramework.Movable3D {
    /// <summary>
    /// Object-related gravity mode.
    /// <para/>
    /// Dynamic objects always use the surface of the nearest ground as their reference gravity vector.
    /// </summary>
    public enum GravityMode {
        World = 0,
        Dynamic = 1,
    }

    /// <summary>
    /// Interface to be used on every sensitive moving objects of the game,
    /// on which we want to keep a specific control.
    /// <para/>
    /// Provides multiple common utilities to properly move objects in space.
    /// </summary>
    public interface IMovable3D {
        #region Content
        /// <summary>
        /// This object rigidbody.
        /// </summary>
        Rigidbody Rigidbody { get; }

        // -----------------------

        /// <summary>
        /// Set this object world position.
        /// <br/> Use this instead of setting <see cref="Transform.position"/>.
        /// </summary>
        void SetPosition(Vector3 _position);

        /// <summary>
        /// Set this object world rotation.
        /// <br/> Use this instead of setting <see cref="Transform.rotation"/>.
        /// </summary>
        void SetRotation(Quaternion _rotation);
        #endregion
    }

    /// <summary>
    /// Velocity-wrapper for any <see cref="Movable3D"/> object.
    /// </summary>
    [Serializable]
    public class Velocity {
        #region Velocity
        /// <summary>
        /// Velocity of the object itself, in absolute world coordinates
        /// <br/> (non object-oriented).
        /// <para/>
        /// In unit/second.
        /// </summary>
        public Vector3 Movement = Vector3.zero;

        /// <summary>
        /// External velocity applied on the object, in absolute world coordinates
        /// <br/> (non object-oriented).
        /// <para/>
        /// In unit/second.
        /// </summary>
        public Vector3 Force = Vector3.zero;

        /// <summary>
        /// Instant velocity applied on the object, for this frame only, in absolute world coordinates
        /// <br/> (non object-oriented).
        /// <para/>
        /// In unit/frame.
        /// </summary>
        public Vector3 Instant = Vector3.zero;
        #endregion

        #region Utility
        internal void ComputeImpact(RaycastHit _hit) {
            Force = Force.PerpendicularSurface(_hit.normal);
        }

        internal void ResetFrameVelocity() {
            Movement = Instant
                     = Vector3.zero;
        }

        internal void Reset() {
            Movement = Force
                     = Instant
                     = Vector3.zero;
        }
        #endregion
    }

    /// <summary>
    /// Wrapper for an <see cref="Velocity"/>-related velocity during this frame.
    /// </summary>
    [Serializable]
    public struct FrameVelocity {
        #region Velocity
        public Vector3 Movement;
        public Vector3 Force;
        public Vector3 Instant;
        public Quaternion Rotation;
        #endregion
    }

    /// <summary>
    /// Base class for every moving object of the game using complex velocity and collision detections.
    /// </summary>
    public class Movable3D : EnhancedBehaviour, IMovable3D, IMovableUpdate {
        public override UpdateRegistration UpdateRegistration => base.UpdateRegistration | UpdateRegistration.Init | UpdateRegistration.Movable;

        #region Collision Settings
        /// <summary>
        /// The type of collision system used to calculate
        /// how this object moves in space.
        /// </summary>
        public virtual CollisionSystem3DType CollisionType {
            get {
                return CollisionSystem3DType.Simple;
            }
        }

        /// <summary>
        /// <see cref="LayerMask"/> to be used for object collision detections.
        /// <br/> Uses the GameObject layer collision mask from <see cref="Physics"/> settings by default.
        /// </summary>
        public virtual int CollisionMask {
            get {
                return Physics3DUtility.GetLayerCollisionMask(gameObject);
            } 
        }

        /// <summary>
        /// <see cref="LayerMask"/> to be used for trigger detections.
        /// <br/> Uses the GameObject layer collision mask from <see cref="Physics"/> settings by default.
        /// </summary>
        public virtual int TriggerMask {
            get {
                return Physics3DUtility.GetLayerCollisionMask(gameObject);
            }
        }
        #endregion

        #region Global Members
        [Section("Movable 3D")]

        [SerializeField, Enhanced, Required] internal protected new Rigidbody rigidbody = null;
        [SerializeField, Enhanced, Required] internal protected new Transform transform = null;

        public Rigidbody Rigidbody {
            get {
                return rigidbody;
            }
        }

        public override Transform Transform {
            get {
                return transform;
            }
        }

        [Space(5f)]

        [SerializeField, Enhanced, Inline] internal protected new PhysicsCollider3D collider = new PhysicsCollider3D();
        [SerializeField, Enhanced, Inline] internal protected PhysicsCollider3D triggerCollider = new PhysicsCollider3D();

        [Space(5f)]

        [SerializeField] internal Collider[] ignoredColliders = new Collider[] { };

        [Space(10f)]

        [SerializeField, Enhanced, Required] protected Movable3DAttributes attributes = null;

        // -----------------------

        [Space(5f), HorizontalLine(SuperColor.Green), Space(5f)]

        public Velocity Velocity = new Velocity();

        [Space(5f)]

        [Enhanced, HelpBox("Velocity equalization makes sure that when this object movement stops, its velocity is equalized based on the previous frame value " +
                           "instead of continuing on the actual force value.", MessageType.Info)]

        [SerializeField] protected bool doEqualizeVelocity = false;
        [SerializeField] protected bool doDebugVelocity = false;

        [Space(10f)]

        [SerializeField, Enhanced, ReadOnly] protected Vector3 previousPosition = new Vector3();
        [field: SerializeField, Enhanced, ReadOnly] public FrameVelocity PreviousFrameVelocity { get; protected set; } = new FrameVelocity();

        // -----------------------

        [Space(5f), HorizontalLine(SuperColor.Turquoise), Space(5f)]

        public bool UseGravity = true;
        [field:SerializeField] public GravityMode GravityMode   { get; protected set; } = GravityMode.World;
        [field:SerializeField] public Vector3 GravitySense      { get; protected set; } = Vector3.down;

        [field: SerializeField, Enhanced, ReadOnly(true), Space(10f)] public bool IsGrounded    { get; protected set; } = false;
        [field: SerializeField, Enhanced, ReadOnly] public Vector3 GroundNormal                 { get; protected set; } = Vector3.up;

        // -----------------------

        [field:SerializeField, Enhanced, ReadOnly(true), Space(5f), HorizontalLine(SuperColor.Crimson), Space(5f)]

        public bool IsMoving                                                            { get; protected set; } = false;
        [field:SerializeField, Enhanced, ReadOnly(true)] public bool HasReachedMaxSpeed { get; protected set; } = false;
        [field:SerializeField, Enhanced, ReadOnly] public float Speed                   { get; protected set; } = 1f;
        [field:SerializeField, Enhanced, ReadOnly] public float VelocityCoef            { get; protected set; } = 1f;

        // -----------------------

        public float ClimbHeight {
            get {
                return attributes.ClimbHeight;
            }
        }

        public float SnapHeight {
            get {
                return attributes.SnapHeight;
            }
        }

        // -----------------------
        
        private bool shouldBeRefreshed = false;
        #endregion

        #region Enhanced Behaviour
        protected override void OnInit() {
            base.OnInit();

            // Colliders initialization.
            collider.Initialize(CollisionMask);
            triggerCollider.Initialize(TriggerMask);

            rigidbody.isKinematic = true;
        }

        protected override void OnBehaviourDisabled() {
            base.OnBehaviourDisabled();

            // State update.
            if (IsMoving) {
                OnSetMoving(false);
            }

            if (HasReachedMaxSpeed) {
                OnReachedMaxSpeed(false);
            }

            ExitTriggers();
        }

        #if UNITY_EDITOR
        private void OnValidate() {
            // Editor required components validation.
            if (Application.isPlaying) {
                return;
            }

            if (!transform) {
                transform = GetComponent<Transform>();
            }

            if (!rigidbody) {
                rigidbody = gameObject.AddComponentIfNone<Rigidbody>();
            }
        }
        #endif
        #endregion

        #region Coefficient
        private readonly Stamp<float> velocityCoefBuffer = new Stamp<float>(1);

        // -----------------------

        /// <summary>
        /// Adds a coefficient to this object velocity.
        /// <param name="coef">Speed coefficient to add.</param>
        /// </summary>
        public void AddVelocityCoef(float coef) {
            if (coef != 0f) {
                velocityCoefBuffer.Add(coef);
                VelocityCoef *= coef;
            } else {
                this.LogWarning("You are trying to add a zero velocity coefficient. This is not allowed.");
            }
        }

        /// <summary>
        /// Removes a coefficient from this object velocity.
        /// </summary>
        /// <param name="coef">Speed coefficient to remove.</param>
        public void RemoveVelocityCoef(float coef) {
            if ((coef != 0f) && velocityCoefBuffer.Contains(coef)) {
                velocityCoefBuffer.Remove(coef);
                VelocityCoef /= coef;
            } else {
                this.LogWarning("You are trying to remove an invalid velocity coefficient. This is not allowed.");
            }
        }

        /// <summary>
        /// Resets all of this object velocity coefficients.
        /// </summary>
        public void ResetVelocityCoef() {
            VelocityCoef = 1f;
            velocityCoefBuffer.Clear();
        }
        #endregion

        #region Velocity
        /// <summary>
        /// Adds a relative movement to this object velocity:
        /// <para/>
        /// Velocity of the object itself, in local coordinates.
        /// <para/>
        /// In unit/second.
        /// </summary>
        public virtual void AddRelativeMovementVelocity(Vector3 _movement) {
            AddMovementVelocity(GetWorldVector(_movement));
        }

        /// <summary>
        /// Adds a movement to this object velocity:
        /// <para/>
        /// <inheritdoc cref="Velocity.Movement" path="/summary"/>
        /// </summary>
        public virtual void AddMovementVelocity(Vector3 _movement) {
            Velocity.Movement += _movement;
        }

        /// <summary>
        /// Adds a force to this object velocity:
        /// <para/>
        /// <inheritdoc cref="Velocity.Force" path="/summary"/>
        /// </summary>
        public virtual void AddForceVelocity(Vector3 _force) {
            Velocity.Force += _force;
        }

        /// <summary>
        /// Adds an instant velocity to this object:
        /// <para/>
        /// <inheritdoc cref="Velocity.Instant" path="/summary"/>
        /// </summary>
        public virtual void AddInstantVelocity(Vector3 _velocity) {
            Velocity.Instant += _velocity;
        }

        // -----------------------

        /// <summary>
        /// Completely resets this object velocity back to zero.
        /// </summary>
        public virtual void ResetVelocity() {
            ResetSpeed();
            Velocity.Reset();
        }
        #endregion

        #region Speed
        /// <summary>
        /// Resets this object speed.
        /// <para/>
        /// Speed is the coefficient applied only to this object velocity movement.
        /// </summary>
        public virtual void ResetSpeed() {
            CurveValue _speed = attributes.Speed;

            if (_speed.CurrentTime == 0f) {
                return;
            }

            Speed = _speed.Reset();
        }

        /// <summary>
        /// Decreases this object speed according to its curve.
        /// </summary>
        public virtual void DecreaseSpeed() {
            Speed = attributes.Speed.Decrease(DeltaTime);
        }

        /// <summary>
        /// Set this object speed ratio according to its curve.
        /// </summary>
        public virtual void SetSpeedRatio(float _ratio) {
            Speed = attributes.Speed.EvaluatePercent(_ratio);
        }

        /// <summary>
        /// Get this object speed ratio according to its curve.
        /// </summary>
        public float GetSpeedRatio() {
            CurveValue _speed = attributes.Speed;
            return _speed.CurrentTime / _speed.Duration;
        }

        // -----------------------

        /// <summary>
        /// Called on update to know if this object speed should be increased.
        /// <br/> Also used to reset the speed.
        /// </summary>
        /// <returns>True if this object speed should be increased, false otherwise.</returns>
        protected virtual bool DoIncreaseSpeed() {
            // Resets this object speed when not moving.
            Vector3 _movement = GetRelativeVector(Velocity.Movement);

            if (Mathm.AreEquals(_movement.x, _movement.z, 0f)) {
                if (attributes.Speed.CurrentTime != 0f) {
                    DecreaseSpeed();
                }

                return false;
            }

            return true;
        }

        private void UpdateSpeed() {
            if (!DoIncreaseSpeed()) {
                return;
            }

            float _increase = DeltaTime;
            if (!IsGrounded) {
                _increase *= attributes.AirSpeedAccelCoef;
            }

            Speed = attributes.Speed.EvaluateContinue(_increase);
        }
        #endregion

        #region Update
        /// <summary>
        /// Pre-movable update callback.
        /// </summary>
        protected virtual void OnPreUpdate() { }

        void IMovableUpdate.Update() {
            // Pre update callback.
            OnPreUpdate();

            // Position refresh.
            if (shouldBeRefreshed || (transform.position != previousPosition)) {
                RefreshPosition();
            }

            // Gravity.
            if (UseGravity && !IsGrounded) {
                ApplyGravity();
            }

            UpdateSpeed();
            FrameVelocity _velocity = ComputeVelocity();

            // Collision calculs.
            CollisionInfos _infos = CollisionType.PerformCollisions(this, Velocity, _velocity, ignoredColliders);

            if (!_infos.AppliedVelocity.IsNull()) {
                SetPosition(rigidbody.position);
            }

            OnAppliedVelocity(_velocity, _infos);

            // Position refresh, after applied velocity.
            if (shouldBeRefreshed) {
                RefreshPosition();
            }

            OnRefreshedObject(_velocity, _infos);

            // Update previous velocity.
            PreviousFrameVelocity = _velocity;

            // Debug.
            if (doDebugVelocity) {
                this.LogWarning($"Velocity => M{_velocity.Movement} | F{_velocity.Force} | I{_velocity.Instant} | Final{_infos.AppliedVelocity}");
            }

            // Post update callback.
            OnPostUpdate();
        }

        /// <summary>
        /// Post-movable update callback.
        /// </summary>
        protected virtual void OnPostUpdate() { }
        #endregion

        #region Gravity
        /// <summary>
        /// Applies gravity on this object.
        /// <br/> Override this to specify a custom gravity.
        /// <para/>
        /// Use <see cref="AddGravity(float, float)"/> for a quick implementation.
        /// </summary>
        protected virtual void ApplyGravity() {
            AddGravity();
        }

        /// <summary>
        /// Adds gravity as a force on this object.
        /// <br/> Uses game standard gravity.
        /// </summary>
        public void AddGravity(float _gravityCoef = 1f, float _maxGravityCoef = 1f) {
            float _maxGravity = PhysicsSettings.I.MaxGravity * _maxGravityCoef;

            Quaternion _rotation = Quaternion.FromToRotation(Vector3.down, GravitySense);
            float _vertical = GetRelativeVector(Velocity.Force, _rotation).y;

            if (_vertical > _maxGravity) {
                _vertical = Mathf.Max(PhysicsSettings.I.Gravity * DeltaTime * _gravityCoef * attributes.GravityCoef, _maxGravity - _vertical);
                AddForceVelocity(-GravitySense * _vertical);
            }
        }

        // -----------------------

        /// <summary>
        /// Set this object <see cref="EnhancedFramework.Movable3D.GravityMode"/>.
        /// </summary>
        public virtual void SetGravityMode(GravityMode _gravityMode) {
            GravityMode = _gravityMode;

            this.Log($"New GravityMode affected to this movable => {_gravityMode}");
        }
        #endregion

        #region Computations
        /// <summary>
        /// Computes this object velocity just before its collision calculs.
        /// </summary>
        /// <returns>Velocity to be used for this frame.</returns>
        protected virtual FrameVelocity ComputeVelocity() {
            // Get the movement and force velocity relatively to this object local space.
            // Do not use the GetRelativeVector & GetWorldVector methods,
            // prefering caching the transform rotation value for optimization purpose.
            Quaternion _rotation = transform.rotation;
            Vector3 _movement = GetRelativeVector(Velocity.Movement, _rotation);
            Vector3 _force = GetRelativeVector(Velocity.Force, _rotation); 

            float _delta = DeltaTime;

            // If movement and force have opposite vertical velocity, accordingly reduce them.
            if (Mathm.HaveDifferentSignAndNotNull(_movement.y, _force.y)) {
                float _absMovement = Mathf.Abs(_movement.y);

                _movement.y = Mathf.MoveTowards(_movement.y, 0f, Mathf.Abs(_force.y));
                _force.y = Mathf.MoveTowards(_force.y, 0f, _absMovement);
            }

            // Compute movement and force flat velocity.
            Vector3 _flatMovement = _movement.Flat() * Speed;
            Vector3 _flatForce = _force.Flat();

            _movement = Vector3.MoveTowards(_flatMovement, _flatMovement.PerpendicularSurface(_flatForce), _flatForce.magnitude * _delta).SetY(_movement.y);
            _force = Vector3.MoveTowards(_flatForce, _flatForce.PerpendicularSurface(_flatMovement), _flatMovement.magnitude * _delta).SetY(_force.y);

            // When movement is added to the opposite force direction, the resulting velocity is the addition of both.
            // But when this opposite movement is stopped, we need to resume the velocity where it previously was.
            if (doEqualizeVelocity) {
                Vector3 _previousMovement = GetRelativeVector(PreviousFrameVelocity.Movement, PreviousFrameVelocity.Rotation).SetY(0f);
                Vector3 _previousForce = GetRelativeVector(PreviousFrameVelocity.Force, PreviousFrameVelocity.Rotation).SetY(0f);

                if (_flatMovement.IsNull() && !_previousMovement.IsNull() && !_previousForce.IsNull()) {
                    _force = (_previousMovement + _previousForce) + (_force - _previousForce);
                }
            }

            // Get this frame object velocity.
            _delta *= VelocityCoef;
            _movement = GetWorldVector(_movement, _rotation);

            FrameVelocity _velocity = new FrameVelocity() {
                Movement = _movement * _delta,
                Force = GetWorldVector(_force, _rotation) * _delta,
                Instant = Velocity.Instant,
                Rotation = _rotation,
            };

            // Reduce flat force velocity for the next frame.
            if (!_force.Flat().IsNull()) {
                float forceDeceleration = IsGrounded
                                        ? PhysicsSettings.I.GroundDecelerationForce
                                        : PhysicsSettings.I.AirDecelerationForce;

                _force = Vector3.MoveTowards(_force, new Vector3(0f, _force.y, 0f), forceDeceleration * DeltaTime);
            }

            // Update velocity values.
            Velocity.Movement = _movement;
            Velocity.Force = GetWorldVector(_force, _rotation);

            return _velocity;
        }
        #endregion

        #region Collision Callbacks
        private const float DynamicGravityDetectionDistance = 15f;

        // -----------------------

        /// <summary>
        /// Set this object ground state, from collision results.
        /// </summary>
        /// <param name="_isGrounded">Is the object grounded at the end of the collisions.</param>
        /// <param name="_hit">Collision ground hit (default is not grounded).</param>
        internal protected virtual void SetGroundState(bool _isGrounded, RaycastHit _hit) {
            // Changed ground state callback.
            if (IsGrounded != _isGrounded) {
                OnSetGrounded(_isGrounded);
            }

            bool _isDynamicGravity = GravityMode == GravityMode.Dynamic;

            // Only update normal when grounded (hit is set to default when not).
            if (IsGrounded) {
                GroundNormal = _hit.normal;

                if (_isDynamicGravity) {
                    GravitySense = -_hit.normal;
                }
            } else if (_isDynamicGravity && collider.Cast(GravitySense, out _hit, DynamicGravityDetectionDistance, QueryTriggerInteraction.Ignore, ignoredColliders)
                       && Physics3DUtility.IsGroundSurface(_hit, -GravitySense)) {
                // When using dynamic gravity, detect nearest ground and use it as reference surface.
                GroundNormal = _hit.normal;
                GravitySense = -_hit.normal;
            }
        }

        /// <summary>
        /// Called just after velocity has been applied on this object, but before extracting the object from overlapping colliders.
        /// </summary>
        protected virtual void OnAppliedVelocity(FrameVelocity _velocity, CollisionInfos _infos) {
            // Moving state.
            bool _isMoving = (!_velocity.Movement.IsNull() && ((_infos.AppliedVelocity - _velocity.Instant).sqrMagnitude > (_velocity.Movement.sqrMagnitude * .5f)));

            if (IsMoving != _isMoving) {
                OnSetMoving(_isMoving);
            }

            // Max speed state.
            bool _hasReachedMaxSpeed = GetSpeedRatio() == 1f;

            if (HasReachedMaxSpeed != _hasReachedMaxSpeed) {
                OnReachedMaxSpeed(_hasReachedMaxSpeed);
            }

            // Rotation according to ground normal.
            Vector3 _up = IsGrounded
                        ? GroundNormal
                        : -GravitySense;

            _up = Vector3.Lerp(Vector3.up, _up, attributes.GroundSurfaceRotationCoef);

            Quaternion _from = transform.rotation;
            Quaternion _to = Quaternion.LookRotation(transform.forward, _up);

            if (_from != _to) {
                _to = Quaternion.RotateTowards(_from, _to, DeltaTime * attributes.GroundSurfaceRotationSpeed * 100f);
                SetRotation(_to);
            }
        }

        /// <summary>
        /// Called at the end of the update, and after all velocity calculs and overlap extraction operations have been performed.
        /// </summary>
        protected virtual void OnRefreshedObject(FrameVelocity _velocity, CollisionInfos _infos) { }

        /// <summary>
        /// Called when this object ground state has changed.
        /// </summary>
        protected virtual void OnSetGrounded(bool _isGrounded) {
            IsGrounded = _isGrounded;

            // Dampen force velocity when getting grounded.
            if (_isGrounded && !Velocity.Force.IsNull()) {
                Quaternion _rotation = transform.rotation;
                Vector3 _force = GetRelativeVector(Velocity.Force, _rotation);

                _force.y = 0f;
                _force *= PhysicsSettings.I.OnGroundedForceMultiplier;

                Velocity.Force = GetWorldVector(_force, _rotation);
            }
        }

        /// <summary>
        /// Called when this object moving state has changed.
        /// </summary>
        protected virtual void OnSetMoving(bool _isMoving) {
            IsMoving = _isMoving;

            // Speed reset.
            if (!_isMoving) {
                //ResetSpeed();
            }
        }

        /// <summary>
        /// Called when this object has reached or lost its max speed.
        /// </summary>
        protected virtual void OnReachedMaxSpeed(bool _hasReachedMaxSpeed) {
            HasReachedMaxSpeed = _hasReachedMaxSpeed;
        }
        #endregion

        #region Overlapping & Triggers
        protected readonly Stamp<Trigger3D> overlappingTriggers = new Stamp<Trigger3D>(2);

        // -----------------------

        /// <summary>
        /// Refresh this object position, and extract it from any overlapping collider.
        /// </summary>
        public void RefreshPosition() {
            // Triggers.
            int _amount = OverlapTriggers();

            for (int i = 0; i < _amount; i++) {
                Collider _overlap = GetOverlapTrigger(i);

                if (_overlap.isTrigger && _overlap.TryGetComponent(out Trigger3D _trigger)) {
                    // Trigger enter.
                    if (HasEnteredTrigger(_trigger)) {
                        OnEnterTrigger(_trigger);
                        overlappingTriggers.Add(_trigger);
                    }
                }
            }

            // Solid colliders.
            _amount = OverlapColliders();

            for (int i = 0; i < _amount; i++) {
                Collider _overlap = GetOverlapCollider(i);

                if (Physics.ComputePenetration(collider.Collider, rigidbody.position, rigidbody.rotation,
                                               _overlap, _overlap.transform.position, _overlap.transform.rotation,
                                               out Vector3 _direction, out float _distance)) {
                    // Collider extraction.
                    OnExtractCollider(_overlap, _direction, _distance);
                }
            }

            shouldBeRefreshed = false;

            // Exit from no-more detected triggers.
            for (int i = overlappingTriggers.Count; i-- > 0;) {
                Trigger3D _trigger = overlappingTriggers[i];

                if (HasExitedTrigger(_trigger)) {
                    OnExitTrigger(_trigger);
                    overlappingTriggers.RemoveAt(i);
                }
            }

            // ----- Local Methods ----- \\

            bool HasEnteredTrigger(Trigger3D _trigger) {
                for (int i = 0; i < overlappingTriggers.Count; i++) {
                    Trigger3D _other = overlappingTriggers[i];

                    if (_trigger == _other)
                        return false;
                }

                return true;
            }

            bool HasExitedTrigger(Trigger3D _trigger) {
                for (int i = 0; i < overlappingTriggers.Count; i++) {
                    Trigger3D _other = overlappingTriggers[i];

                    if (_trigger == _other)
                        return false;
                }

                return true;
            }
        }

        // -----------------------

        public int OverlapColliders() {
            return collider.Overlap(ignoredColliders, QueryTriggerInteraction.Ignore);
        }

        public int OverlapTriggers() {
            return triggerCollider.Overlap(ignoredColliders, QueryTriggerInteraction.Collide);
        }

        public Collider GetOverlapCollider(int _index) {
            return collider.GetOverlapCollider(_index);
        }

        public Collider GetOverlapTrigger(int _index) {
            return triggerCollider.GetOverlapCollider(_index);
        }

        // -----------------------

        /// <summary>
        /// Exits from all overlapping triggers.
        /// </summary>
        protected void ExitTriggers() {
            foreach (var _trigger in overlappingTriggers) {
                OnExitTrigger(_trigger);
            }

            overlappingTriggers.Clear();
        }

        protected virtual void OnEnterTrigger(Trigger3D _trigger) {
            _trigger.OnEnterTrigger(this);
        }

        protected virtual void OnExitTrigger(Trigger3D _trigger) {
            _trigger.OnExitTrigger(this);
        }

        protected virtual void OnExtractCollider(Collider _collider, Vector3 _direction, float _distance) {
            Vector3 _position = rigidbody.position + (_direction * _distance);
            SetPosition(_position);
        }
        #endregion

        #region Transform Utility
        /// <summary>
        /// Sets this object position.
        /// <br/> Use this instead of setting <see cref="Transform.position"/>.
        /// </summary>
        public void SetPosition(Vector3 _position) {
            rigidbody.position = _position;
            transform.position = _position;

            shouldBeRefreshed = true;
        }

        /// <summary>
        /// Sets this object rotation.
        /// <br/> Use this instead of setting <see cref="Transform.rotation"/>.
        /// </summary>
        public void SetRotation(Quaternion _rotation) {
            rigidbody.rotation = _rotation;
            transform.rotation = _rotation;

            shouldBeRefreshed = true;
        }

        /// <summary>
        /// Sets this object position and rotation.
        /// <br/> Use this instead of setting <see cref="Transform.position"/> and <see cref="Transform.rotation"/>.
        /// </summary>
        public void SetPositionAndRotation(Vector3 _position, Quaternion _rotation) {
            SetPosition(_position);
            SetRotation(_rotation);
        }

        // -----------------------

        /// <summary>
        /// Adds an offset to this object position.
        /// </summary>
        /// <param name="_offset">Transform position offset.</param>
        public void OffsetPosition(Vector3 _offset) {
            SetPosition(transform.position + _offset);
        }

        /// <summary>
        /// Adds an offset to this object rotation.
        /// </summary>
        /// <param name="_offset">Transform rotation offset.</param>
        public void OffsetRotation(Quaternion _offset) {
            SetRotation(transform.rotation * _offset);
        }

        // -----------------------

        [Button(ActivationMode.Play, SuperColor.Raspberry, IsDrawnOnTop = false)]
        #pragma warning disable IDE0051
        private void SetTransformValues(Transform transform, bool usePosition = true, bool useRotation = true) {
            if (usePosition) {
                SetPosition(transform.position);
            }

            if (useRotation) {
                SetRotation(transform.rotation);
            }
        }
        #endregion

        #region Collision Masks
        /// <summary>
        /// Override this object collider collision mask.
        /// </summary>
        /// <param name="_mask">New collider collision mask.</param>
        public void SetColliderMask(int _mask) {
            collider.CollisionMask = _mask;
        }

        /// <summary>
        /// Override this object trigger collision mask.
        /// </summary>
        /// <param name="_mask">New trigger collision mask.</param>
        public void SetTriggerMask(int _mask) {
            triggerCollider.CollisionMask = _mask;
        }
        #endregion
    }
}
