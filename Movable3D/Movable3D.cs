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
using System.Collections.Generic;
using UnityEngine;

using Range = EnhancedEditor.RangeAttribute;

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
    /// Controller contract for a <see cref="Movable3D"/>.
    /// <br/> Use this to receive various callbacks and override this object default behaviour.
    /// <para/>
    /// Should be set on Awake, as some operations are made on the Start callback.
    /// </summary>
    public interface IMovable3DController {
        #region Collision Setup
        /// <inheritdoc cref="Movable3D.CollisionType"/>
        CollisionSystem3DType CollisionType { get; }

        // -----------------------

        /// <inheritdoc cref="Movable3D.GetColliderMask"/>
        /// <returns>-1 to use the movable default collision mask implementation, otherwise the collision mask to be used.</returns>
        int GetColliderMask(Collider _collider);

        /// <returns><inheritdoc cref="GetColliderMask(Collider)" path="/returns"/></returns>
        /// <inheritdoc cref="Movable3D.GetTriggerMask"/>
        int GetTriggerMask(Collider _trigger);
        #endregion

        #region Velocity
        /// <inheritdoc cref="Movable3D.ResetVelocity"/>
        /// <returns>False to completely override this behaviour, true to continue execution and call the base definition.</returns>
        bool OnResetVelocity();
        #endregion

        #region Speed
        /// <returns><inheritdoc cref="OnResetVelocity" path="/returns"/></returns>
        /// <inheritdoc cref="Movable3D.UpdateSpeed"/>
        bool OnUpdateSpeed();

        /// <returns><inheritdoc cref="OnResetVelocity" path="/returns"/></returns>
        /// <inheritdoc cref="Movable3D.IncreaseSpeed"/>
        bool OnIncreaseSpeed();

        /// <inheritdoc cref="Movable3D.DecreaseSpeed()"/>
        /// <returns><inheritdoc cref="OnResetVelocity" path="/returns"/></returns>
        bool OnDecreaseSpeed();

        /// <inheritdoc cref="Movable3D.ResetSpeed"/>
        /// <returns><inheritdoc cref="OnResetVelocity" path="/returns"/></returns>
        bool OnResetSpeed();
        #endregion

        #region Update
        /// <inheritdoc cref="Movable3D.OnPreUpdate"/>
        void OnPreUpdate();

        /// <inheritdoc cref="Movable3D.OnPostUpdate"/>
        void OnPostUpdate();
        #endregion

        #region Gravity
        /// <inheritdoc cref="Movable3D.ApplyGravity"/>
        /// <returns><inheritdoc cref="OnResetVelocity" path="/returns"/></returns>
        bool OnApplyGravity();
        #endregion

        #region Computation
        /// <returns><inheritdoc cref="OnResetVelocity" path="/returns"/></returns>
        /// <inheritdoc cref="Movable3D.OnPreComputeVelocity"/>
        bool OnPreComputeVelocity();

        /// <param name="_velocity">Actual velocity of the object</param>
        /// <param name="_frameVelocity"><inheritdoc cref="Movable3D.ComputeVelocity()" path="/returns"/></param>
        /// <returns><inheritdoc cref="OnResetVelocity" path="/returns"/></returns>
        /// <inheritdoc cref="Movable3D.ComputeVelocity"/>
        bool OnComputeVelocity(Velocity _velocity, ref FrameVelocity _frameVelocity);

        /// <returns><inheritdoc cref="OnResetVelocity" path="/returns"/></returns>
        /// <inheritdoc cref="Movable3D.OnPostComputeVelocity"/>
        bool OnPostComputeVelocity(ref FrameVelocity _frameVelocity);
        #endregion

        #region Collision Callback
        /// <inheritdoc cref="Movable3D.SetGroundState(bool, RaycastHit)"/>
        /// <returns><inheritdoc cref="OnResetVelocity" path="/returns"/></returns>
        bool OnSetGroundState(ref bool _isGrounded, RaycastHit _hit);

        /// <inheritdoc cref="Movable3D.OnAppliedVelocity(FrameVelocity, CollisionInfos)"/>
        /// <returns><inheritdoc cref="OnResetVelocity" path="/returns"/></returns>
        bool OnAppliedVelocity(ref FrameVelocity _velocity, CollisionInfos _infos);

        /// <inheritdoc cref="Movable3D.OnRefreshedObject(FrameVelocity, CollisionInfos)"/>
        /// <returns><inheritdoc cref="OnResetVelocity" path="/returns"/></returns>
        bool OnRefreshedObject(ref FrameVelocity _velocity, CollisionInfos _infos);

        /// <inheritdoc cref="Movable3D.OnSetGrounded(bool)"/>
        /// <returns><inheritdoc cref="OnResetVelocity" path="/returns"/></returns>
        bool OnSetGrounded(bool _isGrounded);
        #endregion

        #region Overlap and Trigger
        /// <inheritdoc cref="Movable3D.OnExtractFromCollider(Collider, Vector3, float)"/>
        /// <returns><inheritdoc cref="OnResetVelocity" path="/returns"/></returns>
        bool OnExtractFromCollider(Collider _collider, Vector3 _direction, float _distance);

        /// <inheritdoc cref="Movable3D.OnEnterTrigger(ITrigger3D)"/>
        void OnEnterTrigger(ITrigger3D _trigger);

        /// <inheritdoc cref="Movable3D.OnExitTrigger(ITrigger3D)"/>
        void OnExitTrigger(ITrigger3D _trigger);
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
    [RequireComponent(typeof(Rigidbody))]
    public class Movable3D : EnhancedBehaviour, IMovable3D, IMovableUpdate {
        /// <summary>
        /// The default <see cref="IMovable3DController"/> used when no controller is specified (avoids null reference check).
        /// </summary>
        private class DefaultController : IMovable3DController {
            #region Instance
            /// <summary>
            /// The static instance of this class.
            /// </summary>
            public static DefaultController Instance = new DefaultController();
            #endregion

            #region Controller
            CollisionSystem3DType IMovable3DController.CollisionType {
                get { return CollisionSystem3DType.Simple; }
            }

            int IMovable3DController.GetColliderMask(Collider _collider) {
                return -1;
            }

            int IMovable3DController.GetTriggerMask(Collider _collider) {
                return -1;
            }

            // -----------------------

            bool IMovable3DController.OnResetVelocity() {
                return true;
            }

            // -----------------------

            bool IMovable3DController.OnUpdateSpeed() {
                return true;
            }

            bool IMovable3DController.OnIncreaseSpeed() {
                return true;
            }

            bool IMovable3DController.OnDecreaseSpeed() {
                return true;
            }

            bool IMovable3DController.OnResetSpeed() {
                return true;
            }

            // -----------------------

            void IMovable3DController.OnPreUpdate() { }

            void IMovable3DController.OnPostUpdate() { }

            // -----------------------

            bool IMovable3DController.OnApplyGravity() {
                return true;
            }

            // -----------------------

            bool IMovable3DController.OnPreComputeVelocity() {
                return true;
            }

            bool IMovable3DController.OnComputeVelocity(Velocity _velocity, ref FrameVelocity _frameVelocity) {
                return true;
            }

            bool IMovable3DController.OnPostComputeVelocity(ref FrameVelocity _frameVelocity) {
                return true;
            }

            // -----------------------

            bool IMovable3DController.OnAppliedVelocity(ref FrameVelocity _velocity, CollisionInfos _infos) {
                return true;
            }

            bool IMovable3DController.OnRefreshedObject(ref FrameVelocity _velocity, CollisionInfos _infos) {
                return true;
            }

            bool IMovable3DController.OnSetGrounded(bool _isGrounded) {
                return true;
            }

            bool IMovable3DController.OnSetGroundState(ref bool _isGrounded, RaycastHit _hit) {
                return true;
            }

            // -----------------------

            bool IMovable3DController.OnExtractFromCollider(Collider _collider, Vector3 _direction, float _distance) {
                return true;
            }

            void IMovable3DController.OnEnterTrigger(ITrigger3D _trigger) { }

            void IMovable3DController.OnExitTrigger(ITrigger3D _trigger) { }
            #endregion
        }

        public override UpdateRegistration UpdateRegistration => base.UpdateRegistration | UpdateRegistration.Init | UpdateRegistration.Movable;

        #region Global Members
        [Section("Movable")]

        [SerializeField] protected SerializedInterface<IMovable3DController> controller = new SerializedInterface<IMovable3DController>();

        [SerializeField, HideInInspector] internal protected new Rigidbody rigidbody = null;
        [SerializeField, HideInInspector] internal protected new Transform transform = null;

        public Rigidbody Rigidbody {
            get { return rigidbody; }
        }

        public override Transform Transform {
            get { return transform; }
        }

        /// <summary>
        /// This movable controller instance.
        /// </summary>
        public IMovable3DController Controller {
            get { return controller.Interface; }
            set { controller.Interface = value; }
        }

        [Space(5f)]

        [SerializeField, Enhanced, Inline] internal protected new PhysicsCollider3D collider = new PhysicsCollider3D();
        [SerializeField, Enhanced, Inline] internal protected PhysicsCollider3D triggerCollider = new PhysicsCollider3D();

        [Space(5f)]

        [SerializeField] internal Collider[] ignoredColliders = new Collider[] { };

        // -----------------------

        [Space(5f), HorizontalLine(SuperColor.Green), Space(5f)]

        [SerializeField] protected float gravityFactor = 1f;

        [Space(5f)]

        [SerializeField, Enhanced, Range(0f, 1f)] protected float groundOrientationFactor = 1f;

        [Tooltip("The speed of this object orientation according to its ground normal, in quarter-circle per second.")]
        [SerializeField, Enhanced, Range(0f, 100f)] protected float groundOrientationSpeed = 1f;

        [Space(5f)]

        [SerializeField] protected float speed = 1f;
        [field: SerializeField, Enhanced, ReadOnly] public float VelocityCoef { get; protected set; } = 1f;

        public float Speed {
            get { return speed; }
        }

        // -----------------------

        [Space(5f), HorizontalLine(SuperColor.Pumpkin), Space(5f)]

        public bool UseGravity = true;

        [field: SerializeField] public GravityMode GravityMode { get; protected set; } = GravityMode.World;
        [field: SerializeField] public Vector3 GravitySense { get; protected set; } = Vector3.down;

        [field: SerializeField, Enhanced, ReadOnly(true), Space(10f)] public bool IsGrounded { get; protected set; } = false;
        [field: SerializeField, Enhanced, ReadOnly] public Vector3 GroundNormal { get; protected set; } = Vector3.up;

        // -----------------------

        [Space(5f), HorizontalLine(SuperColor.Purple), Space(5f)]

        public Velocity Velocity = new Velocity();

        [Space(5f)]

        [SerializeField] protected bool debugVelocity = false;

        [Enhanced, HelpBox("Velocity equalization makes sure that when this object movement stops, its velocity is equalized based on the previous frame value " +
                           "instead of continuing on the actual force value.", MessageType.Info, false)]

        [SerializeField] protected bool equalizeVelocity = false;

        [Space(5f)]

        [SerializeField, Enhanced, ReadOnly] protected FrameVelocity previousFrameVelocity = new FrameVelocity();
        [SerializeField, Enhanced, ReadOnly] protected Vector3 previousPosition = new Vector3();

        public FrameVelocity PreviousFrameVelocity {
            get { return previousFrameVelocity; }
        }

        // -----------------------

        public virtual float ClimbHeight {
            get { return PhysicsSettings.I.ClimbHeight; }
        }

        public virtual float SnapHeight {
            get { return PhysicsSettings.I.SnapHeight; }
        }

        // -----------------------
        
        private bool shouldBeRefreshed = false;
        #endregion

        #region Enhanced Behaviour
        protected override void OnBehaviourEnabled() {
            base.OnBehaviourEnabled();

            collider.Collider.enabled = true;
            triggerCollider.Collider.enabled = true;
        }

        protected override void OnInit() {
            base.OnInit();

            // Initialization.
            if (Controller == null) {
                Controller = DefaultController.Instance;
            }

            collider.Initialize(GetColliderMask());
            triggerCollider.Initialize(GetTriggerMask());

            rigidbody.isKinematic = true;
        }

        protected override void OnBehaviourDisabled() {
            base.OnBehaviourDisabled();

            // Clear state and disable collisions.
            ExitTriggers();

            collider.Collider.enabled = false;
            triggerCollider.Collider.enabled = false;
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

        #region Collision Setup
        /// <summary>
        /// The type of collision system used to calculate how this object moves in space.
        /// </summary>
        public virtual CollisionSystem3DType CollisionType {
            get { return Controller.CollisionType; }
        }

        // -----------------------

        /// <summary>
        /// Get the default collision mask used for this object collider.
        /// </summary>
        public int GetColliderMask() {
            Collider _collider = collider.Collider;
            int _mask = Controller.GetColliderMask(_collider);

            if (_mask != -1) {
                return _mask;
            }

            return Physics3DUtility.GetLayerCollisionMask(_collider.gameObject);
        }

        /// <summary>
        /// Get the default collision mask used for this object trigger.
        /// </summary>
        public int GetTriggerMask() {
            Collider _collider = triggerCollider.Collider;
            int _mask = Controller.GetTriggerMask(_collider);

            if (_mask != -1) {
                return _mask;
            }

            return Physics3DUtility.GetLayerCollisionMask(_collider.gameObject);
        }

        // -----------------------

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
            if (!Controller.OnResetVelocity()) {
                return;
            }

            Velocity.Reset();
        }
        #endregion

        #region Update
        /// <summary>
        /// Pre-movable update callback.
        /// </summary>
        protected virtual void OnPreUpdate() {
            Controller.OnPreUpdate();
        }

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

            OnPreComputeVelocity();
            FrameVelocity _velocity = ComputeVelocity();
            OnPostComputeVelocity(ref _velocity);

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
            previousFrameVelocity = _velocity;

            // Debug.
            if (debugVelocity) {
                this.LogWarning($"Velocity => M{_velocity.Movement} | F{_velocity.Force} | I{_velocity.Instant} | Final{_infos.AppliedVelocity}");
            }

            // Post update callback.
            OnPostUpdate();
        }

        /// <summary>
        /// Post-movable update callback.
        /// </summary>
        protected virtual void OnPostUpdate() {
            Controller.OnPostUpdate();
        }
        #endregion

        #region Gravity
        /// <summary>
        /// Applies gravity on this object.
        /// <br/> Override this to specify a custom gravity.
        /// <para/>
        /// Use <see cref="AddGravity(float, float)"/> for a quick implementation.
        /// </summary>
        protected virtual void ApplyGravity() {
            if (!Controller.OnApplyGravity()) {
                return;
            }

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
                _vertical = Mathf.Max(PhysicsSettings.I.Gravity * DeltaTime * _gravityCoef * gravityFactor, _maxGravity - _vertical);
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

        #region Computation
        /// <summary>
        /// Called before computing this object frame velocity.
        /// <br/> Used to perform various velocity-related operations,
        /// like incrementing the object speed.
        /// </summary>
        protected virtual void OnPreComputeVelocity() {
            if (!Controller.OnPreComputeVelocity()) {
                return;
            }
        }

        /// <summary>
        /// Computes this object velocity just before its collision calculs.
        /// </summary>
        /// <returns>Velocity to be used for this frame.</returns>
        protected virtual FrameVelocity ComputeVelocity() {
            FrameVelocity _velocity = new FrameVelocity();

            if (!Controller.OnComputeVelocity(Velocity, ref _velocity)) {
                return _velocity;
            }

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
            Vector3 _flatMovement = _movement.Flat() * speed;
            Vector3 _flatForce = _force.Flat();

            _movement = Vector3.MoveTowards(_flatMovement, _flatMovement.PerpendicularSurface(_flatForce), _flatForce.magnitude * _delta).SetY(_movement.y);
            _force = Vector3.MoveTowards(_flatForce, _flatForce.PerpendicularSurface(_flatMovement), _flatMovement.magnitude * _delta).SetY(_force.y);

            // When movement is added to the opposite force direction, the resulting velocity is the addition of both.
            // But when this opposite movement is stopped, we need to resume the velocity where it previously was.
            if (equalizeVelocity) {
                Vector3 _previousMovement = GetRelativeVector(previousFrameVelocity.Movement, previousFrameVelocity.Rotation).SetY(0f);
                Vector3 _previousForce = GetRelativeVector(previousFrameVelocity.Force, previousFrameVelocity.Rotation).SetY(0f);

                if (_flatMovement.IsNull() && !_previousMovement.IsNull() && !_previousForce.IsNull()) {
                    _force = (_previousMovement + _previousForce) + (_force - _previousForce);
                }
            }

            // Get this frame object velocity.
            _delta *= VelocityCoef;
            _movement = GetWorldVector(_movement, _rotation);

            _velocity = new FrameVelocity() {
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

        /// <summary>
        /// Called after computing this object frame velocity.
        /// <br/> Use this to perform additional operations.
        /// </summary>
        /// <param name="_velocity"></param>
        protected virtual void OnPostComputeVelocity(ref FrameVelocity _velocity) {
            if (!Controller.OnPostComputeVelocity(ref _velocity)) {
                return;
            }
        }
        #endregion

        #region Collision Callback
        private const float DynamicGravityDetectionDistance = 15f;

        // -----------------------

        /// <summary>
        /// Called just after velocity has been applied on this object, but before extracting the object from overlapping colliders.
        /// </summary>
        protected virtual void OnAppliedVelocity(FrameVelocity _velocity, CollisionInfos _infos) {
            if (!Controller.OnAppliedVelocity(ref _velocity, _infos)) {
                return;
            }

            // Rotation according to the ground normal.
            Vector3 _up = IsGrounded
                        ? GroundNormal
                        : -GravitySense;

            Quaternion _from = transform.rotation;
            Quaternion _to = Quaternion.LookRotation(transform.forward, Vector3.Lerp(Vector3.up, _up, groundOrientationFactor));

            if (_from != _to) {
                _to = Quaternion.RotateTowards(_from, _to, groundOrientationSpeed * DeltaTime * 90f);
                SetRotation(_to);
            }
        }

        /// <summary>
        /// Called at the end of the update, and after all velocity calculs and overlap extraction operations have been performed.
        /// </summary>
        protected virtual void OnRefreshedObject(FrameVelocity _velocity, CollisionInfos _infos) {
            if (!Controller.OnRefreshedObject(ref _velocity, _infos)) {
                return;
            }
        }

        /// <summary>
        /// Called when this object ground state has changed.
        /// </summary>
        protected virtual void OnSetGrounded(bool _isGrounded) {
            if (!Controller.OnSetGrounded(_isGrounded)) {
                return;
            }

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
        /// Set this object ground state, from collision results.
        /// </summary>
        /// <param name="_isGrounded">Is the object grounded at the end of the collisions.</param>
        /// <param name="_hit">Collision ground hit (default is not grounded).</param>
        internal protected virtual void SetGroundState(bool _isGrounded, RaycastHit _hit) {
            if (!Controller.OnSetGroundState(ref _isGrounded, _hit)) {
                return;
            }

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
        #endregion

        #region Overlap and Trigger
        private static readonly List<ITrigger3D> triggerBuffer = new List<ITrigger3D>();
        protected readonly List<ITrigger3D> overlappingTriggers = new List<ITrigger3D>();

        // -----------------------

        /// <summary>
        /// Refresh this object position, and extract it from any overlapping collider.
        /// </summary>
        public void RefreshPosition() {
            // Triggers.
            int _amount = OverlapTriggers();
            triggerBuffer.Clear();

            for (int i = 0; i < _amount; i++) {
                Collider _overlap = GetOverlapTrigger(i);

                if (_overlap.isTrigger && _overlap.TryGetComponent(out ITrigger3D _trigger)) {
                    triggerBuffer.Add(_trigger);

                    // Trigger enter.
                    if (HasEnteredTrigger(_trigger)) {
                        overlappingTriggers.Add(_trigger);
                        OnEnterTrigger(_trigger);
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
                    OnExtractFromCollider(_overlap, _direction, _distance);
                }
            }

            shouldBeRefreshed = false;

            // Exit from no-more detected triggers.
            for (int i = overlappingTriggers.Count; i-- > 0;) {
                ITrigger3D _trigger = overlappingTriggers[i];

                if (HasExitedTrigger(_trigger)) {
                    overlappingTriggers.RemoveAt(i);
                    OnExitTrigger(_trigger);
                }
            }

            // ----- Local Methods ----- \\

            bool HasEnteredTrigger(ITrigger3D _trigger) {
                for (int i = 0; i < overlappingTriggers.Count; i++) {
                    ITrigger3D _other = overlappingTriggers[i];

                    if (_trigger == _other)
                        return false;
                }

                return true;
            }

            bool HasExitedTrigger(ITrigger3D _trigger) {
                for (int i = 0; i < triggerBuffer.Count; i++) {
                    ITrigger3D _other = triggerBuffer[i];

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
            return PhysicsCollider3D.GetOverlapCollider(_index);
        }

        public Collider GetOverlapTrigger(int _index) {
            return PhysicsCollider3D.GetOverlapCollider(_index);
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

        /// <summary>
        /// Called when this objects enters a new trigger.
        /// </summary>
        /// <param name="_trigger">The <see cref="ITrigger3D"/> this object entered.</param>
        protected virtual void OnEnterTrigger(ITrigger3D _trigger) {
            _trigger.OnEnterTrigger(this);
            Controller.OnEnterTrigger(_trigger);
        }

        /// <summary>
        /// Called when this objects exits a trigger.
        /// </summary>
        /// <param name="_trigger">The <see cref="ITrigger3D"/> this object exited.</param>
        protected virtual void OnExitTrigger(ITrigger3D _trigger) {
            _trigger.OnExitTrigger(this);
            Controller.OnExitTrigger(_trigger);
        }

        /// <summary>
        /// Called when this objects is extracting from a collider.
        /// </summary>
        protected virtual void OnExtractFromCollider(Collider _collider, Vector3 _direction, float _distance) {
            if (!Controller.OnExtractFromCollider(_collider, _direction, _distance)) {
                return;
            }

            Vector3 _position = rigidbody.position + (_direction * _distance);
            SetPosition(_position);
        }
        #endregion

        #region Transform Utility
        /// <summary>
        /// Sets this object position.
        /// <br/> Use this instead of setting <see cref="Transform.position"/>.
        /// </summary>
        public virtual void SetPosition(Vector3 _position) {
            rigidbody.position = _position;
            transform.position = _position;

            shouldBeRefreshed = true;
        }

        /// <summary>
        /// Sets this object rotation.
        /// <br/> Use this instead of setting <see cref="Transform.rotation"/>.
        /// </summary>
        public virtual void SetRotation(Quaternion _rotation) {
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

        /// <inheritdoc cref="SetPositionAndRotation(Vector3, Quaternion)"/>
        public void SetPositionAndRotation(Transform _transform, bool _useLocal = false) {
            Vector3 _position;
            Quaternion _rotation;

            if (_useLocal) {
                _position = _transform.localPosition;
                _rotation = _transform.localRotation;
            } else {
                _position = _transform.position;
                _rotation = _transform.rotation;
            }

            SetPositionAndRotation(_position, _rotation);
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
        protected void SetTransformValues(Transform transform, bool usePosition = true, bool useRotation = true) {
            if (usePosition) {
                SetPosition(transform.position);
            }

            if (useRotation) {
                SetRotation(transform.rotation);
            }
        }
        #endregion
    }
}
