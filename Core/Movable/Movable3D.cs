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

#if DOTWEEN_ENABLED
using DG.Tweening;
#endif

using Min   = EnhancedEditor.MinAttribute;
using Range = EnhancedEditor.RangeAttribute;

namespace EnhancedFramework.Physics3D {
    /// <summary>
    /// Interface to inherit any sensitive moving object on which to maintain control from.
    /// <para/>
    /// Provides multiple common utilities to properly move an object in space.
    /// </summary>
    public interface IMovable3D {
        #region Content
        /// <summary>
        /// This object <see cref="UnityEngine.Rigidbody"/>.
        /// </summary>
        Rigidbody Rigidbody { get; }

        /// <summary>
        /// Get / set this object world position.
        /// <br/> Use this instead of <see cref="Transform.position"/>.
        /// </summary>
        Vector3 Position { get; set; }

        /// <summary>
        /// Get / set this object world rotation.
        /// <br/> Use this instead of <see cref="Transform.rotation"/>.
        Quaternion Rotation { get; set; }
        #endregion
    }

    // ===== Velocity Data ===== \\

    /// <summary>
    /// <see cref="Movable3D"/> global velocity wrapper.
    /// </summary>
    [Serializable]
    public sealed class Velocity {
        #region Velocity
        /// <summary>
        /// Velocity of the object itself, in absolute world coordinates
        /// <br/> (non object-oriented).
        /// <para/>
        /// In unit/second.
        /// </summary>
        [Tooltip("Velocity of the object itself, in absolute world coordinates\n\nIn unit/second")]
        public Vector3 Movement = Vector3.zero;

        /// <summary>
        /// Velocity of the object itself, in absolute world coordinates
        /// <br/> (non object-oriented).
        /// <para/>
        /// In unit/frame.
        /// </summary>
        [Tooltip("Instant velocity applied on the object, for this frame only, in absolute world coordinates\n\nIn unit/frame")]
        public Vector3 InstantMovement = Vector3.zero;

        /// <summary>
        /// External velocity applied on the object, in absolute world coordinates
        /// <br/> (non object-oriented).
        /// <para/>
        /// In unit/second.
        /// </summary>
        [Tooltip("External velocity applied on the object, in absolute world coordinates\n\nIn unit/second")]
        public Vector3 Force = Vector3.zero;

        /// <summary>
        /// Instant velocity applied on the object, for this frame only, in absolute world coordinates
        /// <br/> (non object-oriented).
        /// <para/>
        /// In unit/frame.
        /// </summary>
        [Tooltip("Instant velocity applied on the object, for this frame only, in absolute world coordinates\n\nIn unit/frame")]
        public Vector3 Instant = Vector3.zero;

        [Space(10f)]

        /// <summary>
        /// Velocity applied over time on the object, using a specific curve and duration.
        /// <br/> Prevents from performing any movement velocity while active.
        /// </summary>
        public List<VelocityOverTime> VelocityOverTime = new List<VelocityOverTime>();
        #endregion

        #region Utility
        /// <summary>
        /// Computes this object velocity for this frame.
        /// </summary>
        internal void ComputeVelocity(float _deltaTime) {

            int _count = VelocityOverTime.Count;
            if (_count == 0)
                return;

            // Remove any movement while applying velocity.
            Movement.Set(0f, 0f, 0f);

            // Apply.
            for (int i = _count; i-- > 0;) {
                VelocityOverTime _velocityOver = VelocityOverTime[i];

                if (_velocityOver.Evaluate(_deltaTime, out Vector3 _velocity)) {
                    VelocityOverTime.RemoveAt(i);
                } else {
                    VelocityOverTime[i] = _velocityOver;
                }

                // Ignore gravity and other opposite vertical force.
                if ((_velocity.y > 0f) && (Force.y < 0f)) {
                    Force.y = 0f;
                }

                Instant += _velocity;
            }
        }

        /// <summary>
        /// Computes an impact on this object velocity, and modifies its vector(s) accordingly.
        /// </summary>
        internal void ComputeImpact(Movable3D _movable, CollisionHit3D _hit) {

            // Force.
            Force = _movable.ComputeImpact(Force, _hit);

            // Velocity over time.
            for (int i = VelocityOverTime.Count; i-- > 0;) {

                VelocityOverTime _velocity = VelocityOverTime[i];
                Vector3 _movement = _movable.ComputeImpact(_velocity.Movement, _hit);

                if (_movement.IsNull()) {
                    VelocityOverTime.RemoveAt(i);
                } else {
                    _velocity.UpdateMovement(_movement);
                    VelocityOverTime[i] = _velocity;
                }
            }
        }

        /// <summary>
        /// Resets this velocity frame-related vectors.
        /// </summary>
        internal void ResetFrameVelocity() {

            InstantMovement.Set(0f, 0f, 0f);
            Instant        .Set(0f, 0f, 0f);
            Movement       .Set(0f, 0f, 0f);
        }

        /// <summary>
        /// Resets all this velocity vectors.
        /// </summary>
        internal void Reset() {

            InstantMovement .Set(0f, 0f, 0f);
            Instant         .Set(0f, 0f, 0f);
            Movement        .Set(0f, 0f, 0f);
            Force           .Set(0f, 0f, 0f);
            VelocityOverTime.Clear();
        }
        #endregion
    }

    /// <summary>
    /// Data wrapper used to apply a velocity to a <see cref="Movable3D"/> over time.
    /// </summary>
    [Serializable]
    public struct VelocityOverTime {
        #region Content
        public Vector3 Movement;
        public float Duration;
        public AnimationCurve Curve;

        public float Timer;
        public Vector3 LastMovement;

        // -----------------------

        public VelocityOverTime(Vector3 _movement, float _duration, AnimationCurve _curve) {
            Movement = _movement;
            Duration = _duration;
            Curve    = _curve;

            Timer = 0f;
            LastMovement = Vector3.zero;
        }
        #endregion

        #region Utility
        public bool Evaluate(float _deltaTime, out Vector3 _movement) {
            float _duration = Duration;
            if (_duration == 0f) {

                _movement = Movement;
                return true;
            }

            float _time = Timer + _deltaTime;
            float _percent;
            bool _isOver;

            if (_time >= _duration) {
                _percent = 1f;
                _isOver = true;
            } else {
                _percent = _time / _duration;
                _isOver = false;
            }

            #if DOTWEEN_ENABLED
            Vector3 _fullMovement = DOVirtual.EasedValue(Vector3.zero, Movement, _percent, Curve);
            #else
            Vector3 _fullMovement = Vector3.Lerp(Vector3.zero, Movement, Curve.Evaluate(_percent));
            #endif
            _movement = _fullMovement - LastMovement;

            Timer = _time;
            LastMovement = _fullMovement;

            return _isOver;
        }

        public void UpdateMovement(Vector3 _movement) {
            Movement = _movement;

            float _duration = Duration;
            if (_duration != 0f) {
                #if DOTWEEN_ENABLED
                LastMovement = DOVirtual.EasedValue(Vector3.zero, _movement, Timer / _duration, Curve);
                #else
                LastMovement = Vector3.Lerp(Vector3.zero, _movement, Curve.Evaluate(Timer / _duration));
                #endif
            }
        }
        #endregion
    }

    /// <summary>
    /// <see cref="Velocity"/> frame wrapper.
    /// </summary>
    [Serializable]
    public struct FrameVelocity {
        #region Velocity
        public Vector3 Movement;
        public Vector3 Force;
        public Vector3 Instant;
        public Quaternion Rotation;

        /// <summary>
        /// This frame time delta.
        /// </summary>
        public float DeltaTime;

        // -----------------------

        /// <summary>
        /// Is this frame velocity valid to perform collisions?
        /// </summary>
        public readonly bool IsValid {
            get { return !Movement.IsNull() || !Force.IsNull() || !Instant.IsNull(); }
        }
        #endregion
    }

    // ===== Enum ===== \\

    /// <summary>
    /// Object-related gravity mode.
    /// </summary>
    public enum GravityMode {
        [Tooltip("Uses the world global vectors for gravity")]
        World   = 0,

        [Tooltip("Uses the surface of the nearest ground as the reference gravity vector")]
        Dynamic = 1,
    }

    /// <summary>
    /// Various <see cref="Movable3D"/>-related options.
    /// </summary>
    [Flags]
    public enum MovableOption {
        None = 0,

        // ----
        [Separator(SeparatorPosition.Top)]

        [Tooltip("If true, continuously refresh this object position every frame, even if no velocity was applied")]
        RefreshContinuously = 1 << 0,

        [Tooltip("Makes sure that when this object stops moving, its Velocity is equalized based on the previous frame instead of continuing on its actual Force")]
        EqualizeVelocity    = 1 << 1,

        [Tooltip("Slide against obstacle surfaces if the velocity and the collision angle allow it")]
        SlideOnSurfaces     = 1 << 2,

        [Tooltip("Always try to push obstacle instead of extracting from them - always keep current position")]
        RockBehaviour       = 1 << 3,

        // ----
        [Ethereal]
        All = EqualizeVelocity | SlideOnSurfaces | RockBehaviour,
    }

    // ===== Component ===== \\

    /// <summary>
    /// Base class for every moving object of the game using complex velocity and collision detections.
    /// </summary>
    [SelectionBase, RequireComponent(typeof(Rigidbody))]
    #pragma warning disable
    public abstract class Movable3D : EnhancedBehaviour, IMovable3D, IMovableUpdate, ITriggerActor {
        public override UpdateRegistration UpdateRegistration => base.UpdateRegistration | UpdateRegistration.Init | UpdateRegistration.Movable;

        #region Global Members
        [Section("Movable [3D]"), PropertyOrder(0)]

        [Tooltip("Collider(s) used for detecting physics collisions")]
        [SerializeField] private Collider collider = null;

        [Tooltip("Collider used for detecting triggers")]
        [SerializeField] private Collider trigger = null;

        // -----------------------

        [Space(10f), HorizontalLine(SuperColor.Grey, 1f), Space(10f), PropertyOrder(2)]

        [Tooltip("Current coefficient applied on this object Velocity")]
        [SerializeField, Enhanced, ReadOnly] protected float velocityCoef = 1f;

        [Tooltip("Current speed of this object")]
        [SerializeField, Enhanced, ReadOnly(nameof(IsSpeedEditable))] protected float speed = 1f;

        [Space(10f), PropertyOrder(5)]

        [Tooltip("System used to perform various physics operations (collision, overlap, extraction...)")]
        [SerializeField] private PhysicsSystem3DType physicsSystem = PhysicsSystem3DType.Intermediate;

        [Tooltip("Additional options used to define this object behaviour")]
        [SerializeField] private MovableOption options = MovableOption.SlideOnSurfaces;

        [Space(10f), PropertyOrder(10)]

        [Tooltip("Sends a log about this object hit Colliders every frame")]
        [SerializeField] private bool debugCollisions = false;

        [Tooltip("Sends a log about this object Frame Velocity every frame")]
        [SerializeField] private bool debugVelocity = false;

        [Space(10f)]

        /// <summary>
        /// Global velocity of this object.
        /// </summary>
        public Velocity Velocity = new Velocity();

        [Tooltip("Specified axies will not be affected by velocity")]
        [SerializeField] private AxisConstraints freezeAxis = AxisConstraints.None;

        // -----------------------

        [Space(10f, order = 0),         HorizontalLine(SuperColor.Grey, 1f, order = 1), Space(10f, order = 2), PropertyOrder(20)]
        [Title("Gravity", order = 4),   Space(5f, order = 5)]

        [Tooltip("Applies gravity on this object, every frame")]
        [SerializeField] private bool useGravity = true;

        [Tooltip("Mode used to apply gravity on this object")]
        [SerializeField, Enhanced, DisplayName("Mode")] private GravityMode gravityMode = GravityMode.World;

        [Space(5f)]

        [Tooltip("Direction in which to apply gravity on this object, in absolute world coordinates")]
        [SerializeField, Enhanced, DisplayName("Direction")] private Vector3 gravitySense = Vector3.down;

        [Tooltip("Coefficient applied to this object gravity")]
        [SerializeField, Enhanced, DisplayName("Coef")] private float gravityFactor = 1f;

        [Space(20f, order = 0), Title("Ground", order = 1), Space(5f, order = 2)]

        [Tooltip("Is this object currently on a ground surface?")]
        [SerializeField, Enhanced, ReadOnly(true)] private bool isGrounded = false;

        [Tooltip("Normal of this object current ground surface")]
        [SerializeField, Enhanced, ReadOnly] private Vector3 groundNormal = Vector3.up;

        [Space(10f)]

        [Tooltip("Percentage on which to orientate this object according to its current ground surface")]
        [SerializeField, Enhanced, Range(0f, 1f)] private float groundOrientationFactor = 1f;

        [Tooltip("Speed used to orientate this object according to its current ground surface\n\nIn quarter-circle per second")]
        [SerializeField, Enhanced, Range(0f, 100f)] private float groundOrientationSpeed = 1f;

        [Space(20f, order = 0), Title("Weight", order = 1), Space(5f, order = 2), PropertyOrder(22)]

        [Tooltip("Weight of this object - used to determine how other objects interact with it")]
        [SerializeField, Enhanced, Min(0f)] private float weight = 1f;

        [Space(10f)]

        [Tooltip("First is the minimum weight this object can push but will start to lose velocity\n\nSecond is the maximum weight this object can push")]
        [SerializeField] private Vector2 pushRange  = new Vector2(0f, 0f);

        [Tooltip("Curve used to evaluate the above range and how this object velocity is modified when pushing other objects\n\n(0 for full velocity, 1 for when this object cannot push the other)")]
        [SerializeField, Enhanced, EnhancedCurve(0f, 0f, 1f, 1f, SuperColor.Lime)] private AnimationCurve pushCurve  = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        // -----------------------

        [Space(10f), HorizontalLine(SuperColor.Grey, 1f), Space(10f), PropertyOrder(50)]

        [Tooltip("Frame displacement Velocity at the last frame")]
        [SerializeField, Enhanced, ReadOnly] private FrameVelocity lastFrameVelocity  = new FrameVelocity();

        [Space(5f)]

        [Tooltip("Total velocity frame displacement applied on this object during the last frame")]
        [SerializeField, Enhanced, ReadOnly] private Vector3 lastFrameAppliedVelocity = Vector3.zero;

        [Tooltip("Last recorded position of this object")]
        [SerializeField, Enhanced, ReadOnly] private Vector3 lastPosition = new Vector3();

        // -----------------------

        [SerializeField, HideInInspector] protected new Rigidbody rigidbody = null;
        [SerializeField, HideInInspector] protected new Transform transform = null;
        [SerializeField, HideInInspector] protected List<Collider> selfColliders = new List<Collider>();

        private bool forceRefreshGravity = false;
        private bool shouldBeRefreshed   = false;

        // -------------------------------------------
        // Properties
        // -------------------------------------------

        /// <summary>
        /// Collider used for detecting physics collisions.
        /// </summary>
        public PhysicsCollider3D PhysicsCollider {
            get { return PhysicsCollider3D.GetTemp(collider, colliderMask); }
        }

        /// <summary>
        /// Collider used for detecting triggers.
        /// </summary>
        public PhysicsCollider3D PhysicsTrigger {
            get { return PhysicsCollider3D.GetTemp(trigger, triggerMask); }
        }

        /// <summary>
        /// This object main <see cref="UnityEngine.Collider"/> used to detect physics collisions.
        /// </summary>
        public Collider Collider {
            get { return collider; }
        }

        /// <summary>
        /// This object <see cref="UnityEngine.Collider"/> used to detect triggers.
        /// </summary>
        public Collider Trigger {
            get { return trigger; }
        }

        /// <summary>
        /// All <see cref="UnityEngine.Collider"/> of this object.
        /// </summary>
        public List<Collider> SelfColliders {
            get { return selfColliders; }
        }

        /// <summary>
        /// The current position of this object.
        /// </summary>
        public Vector3 Position {
            get { return rigidbody.position; }
            set { SetPosition(value); }
        }

        /// <summary>
        /// The current rotation of this object.
        /// </summary>
        public Quaternion Rotation {
            get { return rigidbody.rotation; }
            set { SetRotation(value); }
        }

        /// <summary>
        /// <see cref="PhysicsSystem3DType"/> used to calculate physics operations and how this object moves and collides with other objects in space.
        /// </summary>
        public PhysicsSystem3DType PhysicsSystem {
            get { return physicsSystem; }
        }

        /// <summary>
        /// Current speed of this object.
        /// </summary>
        public float Speed {
            get { return speed; }
        }

        /// <summary>
        /// Is this object currently on a ground surface?
        /// </summary>
        public bool IsGrounded {
            get { return isGrounded; }
        }

        /// <summary>
        /// Mode used to apply gravity on this object.
        /// </summary>
        public GravityMode GravityMode {
            get { return gravityMode; }
            set {
                gravityMode = value;
                this.LogMessage($"New GravityMode assigned: {value.ToString().Bold()}");
            }
        }

        /// <summary>
        /// Direction in which to apply gravity on this object, in absolute world coordinates.
        /// </summary>
        public Vector3 GravitySense {
            get { return gravitySense; }
        }

        /// <summary>
        /// Normal on this object current ground surface.
        /// </summary>
        public Vector3 GroundNormal {
            get { return groundNormal; }
        }

        /// <summary>
        /// If true, applies gravity on this object every frame.
        /// </summary>
        public bool UseGravity {
            get { return useGravity; }
            set { useGravity = value; }
        }

        /// <summary>
        /// Coefficient applied to this object gravity.
        /// </summary>
        public float GravityFactor {
            get { return gravityFactor; }
            set { gravityFactor = value; }
        }

        /// <summary>
        /// Specified axies will not be affected by this object velocity (axis is in local space - relative the object rotation).
        /// </summary>
        public AxisConstraints FreezeAxis {
            get { return freezeAxis; }
        }

        /// <summary>
        /// Frame displacement velocity during the last frame.
        /// </summary>
        public FrameVelocity LastFrameVelocity {
            get { return lastFrameVelocity; }
        }

        /// <summary>
        /// Total velocity frame displacement applied on this object during the last frame.
        /// </summary>
        public Vector3 LastFrameAppliedVelocity {
            get { return lastFrameAppliedVelocity; }
        }

        // -----------------------

        /// <summary>
        /// If true, continuously refresh this object position every frame, even if no velocity was applied.
        /// </summary>
        public bool RefreshContinuously {
            get { return HasOption(MovableOption.RefreshContinuously); }
        }

        /// <summary>
        /// Makes sure that when this object stops moving,
        /// its Velocity is equalized based on the previous frame instead of continuing on its actual Force.
        /// </summary>
        public bool EqualizeVelocity {
            get { return HasOption(MovableOption.EqualizeVelocity); }
        }

        /// <summary>
        /// Slide against obstacle surfaces if the velocity and the collision angle allow it.
        /// </summary>
        public bool SlideOnSurfaces {
            get { return HasOption(MovableOption.SlideOnSurfaces); }
        }

        /// <summary>
        /// Always try to push obstacle instead of extracting from them - always keep current position.
        /// </summary>
        public bool RockBehaviour {
            get { return HasOption(MovableOption.RockBehaviour); }
        }

        // -----------------------

        /// <summary>
        /// Whether this object speed value should be editable in the inspector or not.
        /// </summary>
        public virtual bool IsSpeedEditable {
            get { return true; }
        }

        public virtual float Weight {
            get { return 1f; }
        }

        /// <summary>
        /// Maximum height used to climb steps and surfaces (Creature collisions only).
        /// </summary>
        public virtual float ClimbHeight {
            get { return Physics3DSettings.I.ClimbHeight; }
        }

        /// <summary>
        /// Maximum height used for snapping to the nearest surface (Creature collisions only).
        /// </summary>
        public virtual float SnapHeight {
            get { return Physics3DSettings.I.SnapHeight; }
        }

        // -----------------------

        public Rigidbody Rigidbody {
            get { return rigidbody; }
        }

        public override Transform Transform {
            get { return transform; }
        }
        #endregion

        #region Enhanced Behaviour
        protected override void OnBehaviourEnabled() {
            base.OnBehaviourEnabled();

            // Enable colliders.
            EnableColliders(true);
        }

        protected override void OnInit() {
            base.OnInit();

            // Initialization.
            InitCollisionMasks();
            rigidbody.isKinematic = true;
        }

        protected override void OnBehaviourDisabled() {
            base.OnBehaviourDisabled();

            // Clear state.
            ExitTriggers();
            ResetParentState();

            // Disable colliders.
            EnableColliders(false);
        }

        // -------------------------------------------
        // Editor
        // -------------------------------------------

        #if UNITY_EDITOR
        protected override void OnValidate() {
            base.OnValidate();

            // Editor required components validation.
            if (Application.isPlaying) {
                return;
            }

            if (!transform) {
                transform = GetComponent<Transform>();
            }

            if (!rigidbody) {
                rigidbody = GetComponent<Rigidbody>();
            }

            GetComponentsInChildren<Collider>(selfColliders);
        }
#endif
        #endregion

        #region Controller
        private IMovable3DComputationController computationController   = DefaultMovable3DController.Instance;
        private IMovable3DCollisionController   collisionController     = DefaultMovable3DController.Instance;
        private IMovable3DColliderController    colliderController      = DefaultMovable3DController.Instance;
        private IMovable3DVelocityController    velocityController      = DefaultMovable3DController.Instance;
        private IMovable3DTriggerController     triggerController       = DefaultMovable3DController.Instance;
        private IMovable3DUpdateController      updateController        = DefaultMovable3DController.Instance;

        // -----------------------

        /// <summary>
        /// Registers a controller for this object.
        /// </summary>
        /// <typeparam name="T">Object type to register.</typeparam>
        /// <param name="_object">Controller to register.</param>
        public virtual void RegisterController<T>(T _object) {
            if (_object is IMovable3DComputationController _computation) {
                computationController = _computation;
            }

            if (_object is IMovable3DCollisionController _collision) {
                collisionController = _collision;
            }

            if (_object is IMovable3DColliderController _collider) {
                colliderController = _collider;
            }

            if (_object is IMovable3DVelocityController _velocity) {
                velocityController = _velocity;
            }

            if (_object is IMovable3DTriggerController _trigger) {
                triggerController = _trigger;
            }

            if (_object is IMovable3DUpdateController _update) {
                updateController = _update;
            }
        }

        /// <summary>
        /// Unregisters a controller from this object.
        /// </summary>
        /// <typeparam name="T">Object type to unregister.</typeparam>
        /// <param name="_object">Controller to unregister.</param>
        public virtual void UnregisterController<T>(T _object) {
            if ((_object is IMovable3DComputationController _computation) && computationController.Equals(_computation)) {
                computationController = DefaultMovable3DController.Instance;
            }

            if ((_object is IMovable3DCollisionController _collision) && collisionController.Equals(_collision)) {
                collisionController = DefaultMovable3DController.Instance;
            }

            if ((_object is IMovable3DColliderController _collider) && colliderController.Equals(_collider)) {
                colliderController = DefaultMovable3DController.Instance;
            }

            if ((_object is IMovable3DVelocityController _velocity) && velocityController.Equals(_velocity)) {
                velocityController = DefaultMovable3DController.Instance;
            }

            if ((_object is IMovable3DTriggerController _trigger) && triggerController.Equals(_trigger)) {
                triggerController = DefaultMovable3DController.Instance;
            }

            if ((_object is IMovable3DUpdateController _update) && updateController.Equals(_update)) {
                updateController = DefaultMovable3DController.Instance;
            }
        }
        #endregion

        // --- Velocity --- \\

        #region Collider
        private int colliderMask = -1;
        private int triggerMask  = -1;

        // -----------------------

        /// <summary>
        /// Get the default collision mask used for this object physics collisions.
        /// </summary>
        public int GetColliderMask() {
            return colliderMask;
        }

        /// <summary>
        /// Get the default collision mask used for this object trigger collisions.
        /// </summary>
        public int GetTriggerMask() {
            return triggerMask;
        }

        /// <summary>
        /// Overrides this object physics collision mask.
        /// </summary>
        /// <param name="_mask">New collision mask value.</param>
        public void SetColliderMask(int _mask) {
            colliderMask = _mask;
        }

        /// <summary>
        /// Overrides this object trigger collision mask.
        /// </summary>
        /// <param name="_mask">New collision mask value.</param>
        public void SetTriggerMask(int _mask) {
            triggerMask = _mask;
        }

        // -----------------------

        private void InitCollisionMasks() {

            // Collider.
            Collider _collider = collider;
            colliderMask = GetMask(_collider, colliderController.InitColliderMask(_collider));

            // Trigger.
            Collider _trigger = trigger;
            triggerMask = GetMask(_trigger, colliderController.InitTriggerMask(_trigger));

            // ----- Local Method ----- \\

            static int GetMask(Collider _collider, int _controllerMask) {

                if (_controllerMask == -1) {
                    _controllerMask = Physics3DUtility.GetLayerCollisionMask(_collider.gameObject);
                }

                return _controllerMask;
            }
        }
        #endregion

        #region Velocity
        private readonly EnhancedCollection<float> velocityCoefBuffer = new EnhancedCollection<float>();

        /// <summary>
        /// Total count of velocity coefficients currently applied.
        /// </summary>
        public int VelocityCoefCount {
            get { return velocityCoefBuffer.Count; }
        }

        // -----------------------

        /// <summary>
        /// Adds a relative movement velocity to this object:
        /// <para/>
        /// Velocity of the object itself, in local coordinates.
        /// <para/> In unit/second.
        /// </summary>
        public void AddRelativeMovementVelocity(Vector3 _movement) {
            AddMovementVelocity(GetWorldVector(_movement));
        }

        /// <summary>
        /// Adds a movement velocity to this object:
        /// <para/> <inheritdoc cref="Velocity.Movement" path="/summary"/>
        /// </summary>
        public void AddMovementVelocity(Vector3 _movement) {
            Velocity.Movement += _movement;
        }

        /// <summary>
        /// Adds an instant movement velocity to this object:
        /// <para/> <inheritdoc cref="Velocity.InstantMovement" path="/summary"/>
        /// </summary>
        public void AddInstantMovementVelocity(Vector3 _movement) {
            Velocity.InstantMovement += _movement;
        }

        /// <summary>
        /// Adds a force velocity to this object:
        /// <para/> <inheritdoc cref="Velocity.Force" path="/summary"/>
        /// </summary>
        public void AddForceVelocity(Vector3 _force) {
            Velocity.Force += _force;
        }

        /// <summary>
        /// Adds an instant force velocity to this object:
        /// <para/> <inheritdoc cref="Velocity.Instant" path="/summary"/>
        /// </summary>
        public void AddInstantVelocity(Vector3 _velocity) {
            Velocity.Instant += _velocity;
        }

        /// <summary>
        /// Adds an velocity over time to this object.
        /// <para/> <inheritdoc cref="Velocity.VelocityOverTime" path="/summary"/>
        /// </summary>
        public void AddVelocityOverTime(VelocityOverTime _velocity) {
            Velocity.VelocityOverTime.Add(_velocity);
        }

        // -------------------------------------------
        // Coefficient
        // -------------------------------------------

        /// <summary>
        /// Applies a coefficient to this object velocity.
        /// <param name="_coef">Coefficient to apply.</param>
        /// </summary>
        public void PushVelocityCoef(float _coef) {
            if (_coef == 0f) {
                this.LogWarningMessage("Trying to add a zero coefficient value (This is not allowed)");
                return;
            }

            velocityCoefBuffer.Add(_coef);
            velocityCoef *= _coef;
        }

        /// <summary>
        /// Removes a coefficient from this object velocity.
        /// </summary>
        /// <param name="_coef">Coefficient to remove.</param>
        public void PopVelocityCoef(float _coef) {
            if ((_coef == 0f) || !velocityCoefBuffer.Remove(_coef)) {
                this.LogWarningMessage($"Trying to remove an invalid coefficient value ({_coef})");
                return;
            }

            velocityCoef /= _coef;
        }

        /// <summary>
        /// Get the applied velocity coef at a given index.
        /// <para/> Use <see cref="VelocityCoefCount"/> to get the amount of currently applied coefficients.
        /// </summary>
        /// <param name="_index">Index to get the coef at.</param>
        /// <returns>The velocity coef at the given index.</returns>
        public float GetVelocityCoefAt(int _index) {
            return velocityCoefBuffer[_index];
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        /// <summary>
        /// Makes this object move in a given direction.
        /// </summary>
        /// <param name="_direction">Direction in which to move this object.</param>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        public virtual bool Move(Vector3 _direction) {
            if (velocityController.Move(_direction)) {
                return true;
            }

            AddMovementVelocity(_direction);
            return false;
        }

        /// <summary>
        /// Completely resets this object velocity back to zero.
        /// <br/> Does not reset its coefficient.
        /// </summary>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        public virtual bool ResetVelocity() {
            if (velocityController.OnResetVelocity()) {
                return true;
            }

            Velocity.Reset();
            return false;
        }

        /// <summary>
        /// Resets this object velocity coefficient back to 1, and clear its buffer.
        /// </summary>
        public void ResetVelocityCoef() {
            velocityCoefBuffer.Clear();
            velocityCoef = 1f;
        }
        #endregion

        #region Update
        /// <summary>
        /// Pre-update callback.
        /// </summary>
        protected virtual void OnPreUpdate() {
            updateController.OnPreUpdate();
        }

        void IMovableUpdate.Update() {

            // Avoid calculs when the game is paused - reset frame velocity to also avoid cumulating force and movement from the previous frames.
            if (Mathm.ApproximatelyZero(DeltaTime)) {

                Velocity.ResetFrameVelocity();
                return;
            }

            // Pre-update callback.
            OnPreUpdate();

            // Stand object by its parent.
            FollowParent();

            // Object moved in space since last frame - call SetPosition to update the object rigidbody position.
            //
            // Unity only update the rigidbody during FixedUpdate (or anyway, not every frame),
            // so it's very important to refresh it before anything else.
            Vector3 position = Transform.position;
            if (position != lastPosition) {
                SetPosition(position);
            }

            // Object position or rotation changed, so it requires to be refreshed and verified.
            //
            // For instance, an object may have been teleported and now overlapping with another object,
            // so we would need to extract from it before applying velocity.
            if (shouldBeRefreshed) {
                RefreshPosition();
            }

            // Apply gravity (only if not grounded, for optimization purpose).
            if (useGravity && (!isGrounded || forceRefreshGravity)) {
                ApplyGravity();
                forceRefreshGravity = false;
            }

            // Compute velocity - lerps force and movement to smooth opposite directions.
            OnPreComputeVelocity();
            ComputeVelocity(out FrameVelocity _velocity);
            OnPostComputeVelocity(ref _velocity);

            // Perform collisions:
            //
            // • CollisionData is a static shared class instance used to store collision informations and results.
            // • Perform collisions using this object velocity and ignore self colliders.
            CollisionData3D _data;
            Vector3 _origin = Position;

            if (_velocity.IsValid) {
                _data = PhysicsSystem.PerformCollisions(this, Velocity, _velocity, selfColliders);

                // Call SetPosition to update the object Transform and set "shouldBeRefreshed" to True.
                if (!_data.AppliedVelocity.IsNull()) {
                    SetPosition(Position);
                }
            } else {
                _data = CollisionData3D.Data.Init(_velocity);
            }

            // Reset frame-dependant velocity (movement and instant velocity).
            Velocity.ResetFrameVelocity();

            // Callbacks and refresh.
            OnAppliedVelocity(_velocity, _data);

            // Refresh this object position after moving it to avoid any overlap and perform additional operations.
            if (shouldBeRefreshed || RefreshContinuously) {
                RefreshPosition();
            }

            _data.AppliedVelocity = Position - _origin;
            OnRefreshedObject(_velocity, _data);

            // Cache this frame velocity.
            lastFrameAppliedVelocity = _data.AppliedVelocity;
            lastFrameVelocity        = _velocity;

            #if DEVELOPMENT
            // --- Debug --- \\
            if (debugVelocity) {
                this.LogWarning($"{name.Bold()} Velocity {UnicodeEmoji.RightTriangle.Get()} " +
                                $"M{_velocity.Movement} | F{_velocity.Force} | I{_velocity.Instant} | Final{_data.AppliedVelocity}");
            }
            #endif

            // Post-update callback.
            OnPostUpdate();
        }

        /// <summary>
        /// Post-update callback.
        /// </summary>
        protected virtual void OnPostUpdate() {
            updateController.OnPostUpdate();
        }
        #endregion

        #region Gravity
        /// <summary>
        /// Applies the gravity on this object.
        /// <para/>
        /// Override this to use a specific gravity.
        /// <br/> Use <see cref="AddGravity(float, float)"/> for a quick implementation.
        /// </summary>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        protected virtual bool ApplyGravity() {
            if (velocityController.OnApplyGravity()) {
                return true;
            }

            AddGravity();
            return false;
        }

        /// <summary>
        /// Adds gravity as a force velocity on this object.
        /// <br/> Uses the game global gravity (see <see cref="Physics3DSettings.Gravity"/>).
        /// </summary>
        /// <param name="_gravityCoef">Coefficient applied to the gravity.</param>
        /// <param name="_maxGravityCoef">Coefficient applied to the maximum allowed gravity value.</param>
        public void AddGravity(float _gravityCoef = 1f, float _maxGravityCoef = 1f) {
            float _maxGravity = Physics3DSettings.I.MaxGravity * _maxGravityCoef;

            Quaternion _rotation = Quaternion.FromToRotation(Vector3.down, gravitySense);
            float _gravity = GetRelativeVector(Velocity.Force, _rotation).y;

            if (_gravity > _maxGravity) {
                _gravity = Mathf.Max(Physics3DSettings.I.Gravity * DeltaTime * _gravityCoef * gravityFactor, _maxGravity - _gravity);
                AddForceVelocity(gravitySense * -_gravity);
            }
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        /// <summary>
        /// Refreshes this object ground state by forcing to apply gravity.
        /// </summary>
        public void RefreshGravity() {
            forceRefreshGravity = true;
        }
        #endregion

        #region Parenting
        private Transform parent = null;
        private bool isParented  = false;

        private Vector3 previousParentPosition    = new Vector3();
        private Quaternion previousParentRotation = Quaternion.identity;

        // -----------------------

        /// <summary>
        /// Parents this movable to a specific <see cref="Transform"/>.
        /// </summary>
        [Button(ActivationMode.Play, SuperColor.Lavender)]
        public void Parent(Transform _parent) {
            parent     = _parent;
            isParented = true;

            ResetParentState();
        }

        /// <summary>
        /// Unparents this object from any <see cref="Transform"/>.
        /// </summary>
        [Button(ActivationMode.Play, SuperColor.SalmonPink)]
        public void Unparent() {
            parent     = null;
            isParented = false;
        }

        /// <summary>
        /// Resets this movable parent relative state.
        /// </summary>
        public void ResetParentState() {
            if (!isParented)
                return;

            previousParentPosition = parent.position;
            previousParentRotation = parent.rotation;
        }

        // -------------------------------------------
        // Internal
        // -------------------------------------------

        /// <summary>
        /// Makes this object follow its reference parent <see cref="UnityEngine.Transform"/>.
        /// </summary>
        private void FollowParent() {
            if (!isParented)
                return;

            Vector3 _parentPos    = parent.position;
            Quaternion _parentRot = parent.rotation;

            Vector3 _positionDifference    = _parentPos - previousParentPosition;
            Quaternion _rotationDifference = _parentRot * Quaternion.Inverse(previousParentRotation);

            previousParentPosition = _parentPos;
            previousParentRotation = _parentRot;

            Vector3 _newPosition = Position + _positionDifference;
            Vector3 _difference  = _newPosition - _parentPos;

            _newPosition = _parentPos + (_rotationDifference * _difference);
            Quaternion _newRotation = Rotation * _rotationDifference;

            SetPositionAndRotation(_newPosition, _newRotation);
        }
        #endregion

        // --- Collision --- \\

        #region Computation
        /// <summary>
        /// Called before computing this object frame velocity.
        /// <br/> Use this to perform additional operations, like incrementing the object speed.
        /// </summary>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        protected virtual bool OnPreComputeVelocity() {
            return computationController.OnPreComputeVelocity();
        }

        /// <summary>
        /// Computes this object velocity just before its collision calculs.
        /// </summary>
        /// <param name="_velocity">Velocity to apply this frame.</param>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        protected virtual bool ComputeVelocity(out FrameVelocity _velocity) {
            _velocity = new FrameVelocity();

            if (computationController.OnComputeVelocity(Velocity, ref _velocity)) {
                return true;
            }

            float _delta = DeltaTime;

            // Base computations.
            Velocity.ComputeVelocity(_delta);

            // Get the movement and force velocity relative to this object local space.
            // Prefere caching the transform rotation value for optimization.
            Quaternion _rotation = transform.rotation;
            Vector3 _movement    = GetRelativeVector(Velocity.Movement, _rotation);
            Vector3 _force       = GetRelativeVector(Velocity.Force, _rotation);

            float _speed = speed;

            // Add instant movement.
            if ((_delta != 0f) && (_speed != 0f)) {
                Vector3 _instantMovement = GetRelativeVector(Velocity.InstantMovement, _rotation);
                _movement += (_instantMovement.Flat() / (_delta * _speed)).SetY(_instantMovement.y);
            }

            // If movement and force have opposite vertical velocity, accordingly reduce them.
            if (Mathm.HaveDifferentSignAndNotNull(_movement.y, _force.y)) {
                float _absMovement = Mathf.Abs(_movement.y);

                _movement.y = Mathf.MoveTowards(_movement.y, 0f, Mathf.Abs(_force.y));
                _force.y    = Mathf.MoveTowards(_force.y,    0f, _absMovement);
            }

            // Compute movement and force flat velocity.
            Vector3 _flatMovement = _movement.Flat() * _speed;
            Vector3 _flatForce    = _force.Flat();

            if (!_flatMovement.IsNull() && !_flatForce.IsNull()) {
                _movement = Vector3.MoveTowards(_flatMovement, _flatMovement.PerpendicularSurface(_flatForce), _flatForce   .magnitude * _delta).SetY(_movement.y);
                _force    = Vector3.MoveTowards(_flatForce,    _flatForce.PerpendicularSurface(_flatMovement), _flatMovement.magnitude * _delta).SetY(_force.y);
            }


            // When movement is added to the opposite force direction, the resulting velocity is the addition of both.
            // But when this opposite movement is stopped, we need to resume the velocity where it previously was.
            if (EqualizeVelocity) {
                Quaternion _lastRotation = lastFrameVelocity.Rotation;

                Vector3 _previousMovement = GetRelativeVector(lastFrameVelocity.Movement, _lastRotation).SetY(0f);
                Vector3 _previousForce    = GetRelativeVector(lastFrameVelocity.Force,    _lastRotation).SetY(0f);

                if (_flatMovement.IsNull() && !_previousMovement.IsNull() && !_previousForce.IsNull()) {
                    _force = (_previousMovement + _previousForce) + (_force - _previousForce);
                }
            }

            // Get this frame velocity.
            _delta   *= velocityCoef;

            _velocity = new FrameVelocity() {
                Movement    = GetWorldVector(GetAxisVelocity(_movement, false), _rotation) * _delta,
                Force       = GetWorldVector(GetAxisVelocity(_force,    false), _rotation) * _delta,
                Instant     = Velocity.Instant,

                DeltaTime   = _delta,
                Rotation    = _rotation,
            };

            // Reduce flat force velocity for the next frame.
            if (!_force.Flat().IsNull()) {
                float forceDeceleration = isGrounded
                                        ? Physics3DSettings.I.GroundForceDeceleration
                                        : Physics3DSettings.I.AirForceDeceleration;

                _force = Vector3.MoveTowards(_force, new Vector3(0f, _force.y, 0f), forceDeceleration * DeltaTime);
            }

            // Update velocity.
            Velocity.Force = GetWorldVector(_force, _rotation);

            return false;
        }

        /// <summary>
        /// Called after computing this object frame velocity.
        /// <br/> Use this to perform additional operations.
        /// </summary>
        /// <param name="_velocity">Velocity to apply this frame.</param>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        protected virtual bool OnPostComputeVelocity(ref FrameVelocity _velocity) {
            return computationController.OnPostComputeVelocity(ref _velocity);
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        /// <summary>
        /// Get the compute velocity with frozen axises.
        /// </summary>
        internal Vector3 GetAxisVelocity(Vector3 _velocity, bool _isWorldVelocity = true) {
            if (freezeAxis != AxisConstraints.None) {

                if (_isWorldVelocity) {
                    _velocity = _velocity.RotateInverse(Rotation);
                }

                _velocity.x = ClampAxis(_velocity.x, AxisConstraints.X);
                _velocity.y = ClampAxis(_velocity.y, AxisConstraints.Y);
                _velocity.z = ClampAxis(_velocity.z, AxisConstraints.Z);

                if (_isWorldVelocity) {
                    _velocity = _velocity.Rotate(Rotation);
                }
            }

            return _velocity;

            // ----- Local Method ----- \\

            float ClampAxis(float _value, AxisConstraints _axis) {
                return freezeAxis.HasFlagUnsafe(_axis) ? 0f : _value;
            }
        }
        #endregion

        #region Collision
        private const float DynamicGravityDetectionDistance = 15f;

        // -----------------------

        /// <summary>
        /// Set this object ground state, from collision results.
        /// </summary>
        /// <param name="_isGrounded">Is the object grounded at the end of the collisions.</param>
        /// <param name="_hit">Collision ground hit (default is not grounded).</param>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        internal protected virtual bool SetGroundState(bool _isGrounded, RaycastHit _hit) {
            if (collisionController.OnSetGroundState(ref _isGrounded, _hit)) {
                return true;
            }

            // Changed ground state callback.
            if (isGrounded != _isGrounded) {
                OnGrounded(_isGrounded);
            }

            bool _isDynamicGravity = gravityMode == GravityMode.Dynamic;

            // Only update normal when grounded (hit is set to default when not).
            if (isGrounded) {
                groundNormal = _hit.normal;

                if (_isDynamicGravity) {
                    gravitySense = -_hit.normal;
                }
            }
            else if (_isDynamicGravity && PhysicsCollider.Cast(gravitySense, out _hit, DynamicGravityDetectionDistance, QueryTriggerInteraction.Ignore, true, selfColliders)
                  && Physics3DUtility.IsGroundSurface(_hit, -gravitySense)) {

                // When using dynamic gravity, detect nearest ground and use it as reference surface.
                groundNormal = _hit.normal;
                gravitySense = -_hit.normal;
            }

            return false;
        }

        // -------------------------------------------
        // Callbacks
        // -------------------------------------------

        /// <summary>
        /// Called after velocity is applied on this object, but before extracting the object from overlapping collider(s).
        /// </summary>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        protected virtual bool OnAppliedVelocity(FrameVelocity _velocity, CollisionData3D _data) {
            if (collisionController.OnAppliedVelocity(ref _velocity, _data)) {
                return true;
            }

            // Rotates according to the ground normal.
            Vector3 _up = isGrounded ? GroundNormal : -gravitySense;

            Quaternion _from = transform.rotation;
            Quaternion _to = Quaternion.LookRotation(transform.forward, Vector3.Lerp(Vector3.up, _up, groundOrientationFactor));

            if (_from != _to) {
                _to = Quaternion.RotateTowards(_from, _to, groundOrientationSpeed * DeltaTime * 90f);
                SetRotation(_to);
            }

            #if DEVELOPMENT
            // --- Debug --- \\

            if (debugCollisions) {
                for (int i = 0; i < _data.HitBuffer.Count; i++) {
                    this.LogMessage("Hit Collider => " + _data.HitBuffer[i].HitCollider.name, _data.HitBuffer[i].HitCollider);
                }
            }
            #endif

            return false;
        }

        /// <summary>
        /// Called at the end of the update, after all velocity calculs and overlap operations have been performed.
        /// </summary>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        protected virtual bool OnRefreshedObject(FrameVelocity _velocity, CollisionData3D _data) {
            return collisionController.OnRefreshedObject(ref _velocity, _data);
        }

        /// <summary>
        /// Called when this object extracts from a collider.
        /// </summary>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        internal protected virtual bool OnExtractFromCollider(Collider _collider, Vector3 _direction, float _distance) {
            if (collisionController.OnExtractFromCollider(_collider, _direction, _distance)) {
                return true;
            }

            bool _rockBehaviour = RockBehaviour;
            Vector3 _offset     = _direction * _distance;

            // Apply collision effects.
            if (_collider.TryGetComponentInParent(out Movable3D _other) && _other.isActiveAndEnabled) {

                if (!_rockBehaviour) {

                    _offset += PushObject(_other, -_offset);

                } else if (!_other.RockBehaviour) {

                    _other.OffsetPosition(_offset);
                }
            }

            // Rock behaviour means that the object should always stick to its position when interacting with other objects.
            // For instance, a tree should never be moved when overlapping with a leave - it's the leave that should be moved.
            if (!_rockBehaviour) {
                SetPosition(Position + _offset);
            }

            #if DEVELOPMENT
            // --- Debug --- \\

            if (debugCollisions) {
                this.LogMessage("Extract from Collider => " + _collider.name + " - " + _offset.ToStringX(3));
            }
            #endif

            return false;
        }

        /// <summary>
        /// Called when this object ground state is changed.
        /// </summary>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        protected virtual bool OnGrounded(bool _isGrounded) {
            if (collisionController.OnGrounded(_isGrounded)) {
                return true;
            }

            isGrounded = _isGrounded;

            // Dampens force velocity when hitting ground.
            if (_isGrounded && !Velocity.Force.IsNull()) {
                Quaternion _rotation = transform.rotation;

                Vector3 _force = GetRelativeVector(Velocity.Force, _rotation).SetY(0f);
                _force *= Physics3DSettings.I.OnGroundedForceMultiplier;

                Velocity.Force = GetWorldVector(_force, _rotation);
            }

            return false;
        }

        /// <summary>
        /// Called whenever this object collides with another <see cref="Movable3D"/>.
        /// </summary>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        internal protected virtual bool OnHitByMovable(Movable3D _other, Collider _otherCollider, Collider _thisCollider) {
            return collisionController.OnHitByMovable(_other, _otherCollider, _thisCollider);
        }

        // -------------------------------------------
        // Impact
        // -------------------------------------------

        /// <summary>
        /// Compute a collision impact.
        /// </summary>
        internal Vector3 ComputeImpact(Vector3 _velocity, CollisionHit3D _hit) {
            RaycastHit _raycastHit = _hit.RaycastHit;

            // Security.
            if (Physics3DSettings.I.CheckForNAN && _raycastHit.normal.IsAnyNaN()) {
                _raycastHit.normal = Vector3.one;
                this.LogErrorMessage("NaN detected => " + _raycastHit.collider.name + " - " + _raycastHit.distance);
            }

            _velocity = _velocity.PerpendicularSurface(_raycastHit.normal, SlideOnSurfaces);
            return GetAxisVelocity(_velocity);
        }
        #endregion

        #region Trigger
        private static readonly List<ITrigger> getTriggerComponentBuffer    = new List<ITrigger>();
        private static readonly List<ITrigger> triggerBuffer                = new List<ITrigger>();

        // All triggers currently overlapping with this object - use an EnhancedCollection to use Reference equality comparer.
        protected readonly EnhancedCollection<ITrigger> overlappingTriggers = new EnhancedCollection<ITrigger>();

        /// <inheritdoc cref="ITriggerActor.Behaviour"/>
        EnhancedBehaviour ITriggerActor.Behaviour {
            get { return this; }
        }

        // -----------------------

        /// <summary>
        /// Refreshes this object trigger interaction.
        /// </summary>
        protected void RefreshTriggers() {

            // Overlapping triggers.
            int _amount = TriggerOverlap();

            List<ITrigger> _overlappingTriggers = overlappingTriggers.collection;
            List<ITrigger> _getComponentBuffer  = getTriggerComponentBuffer;
            List<ITrigger> _buffer = triggerBuffer;

            _buffer.Clear();

            for (int i = 0; i < _amount; i++) {
                Collider _overlap = GetOverlapAt(i);

                if (!_overlap.isTrigger) {
                    continue;
                }

                _getComponentBuffer.Clear();

                // If there is a LevelTrigger, ignore any other trigger.
                if (_overlap.TryGetComponent(out LevelTrigger _levelTrigger) && _levelTrigger.isActiveAndEnabled) {

                    _getComponentBuffer.Add(_levelTrigger);

                } else {

                    _overlap.GetComponents(_getComponentBuffer);
                }

                // Activation.
                int _count = _getComponentBuffer.Count;
                for (int j = 0; j < _count; j++) {

                    ITrigger _trigger = _getComponentBuffer[j];
                    if ((_trigger is Behaviour _behaviour) && !_behaviour.isActiveAndEnabled) {
                        continue;
                    }

                    _buffer.Add(_trigger);

                    // Trigger enter.
                    if (HasEnteredTrigger(_trigger, _overlappingTriggers)) {

                        _overlappingTriggers.Add(_trigger);
                        OnEnterTrigger(_trigger);
                    }
                }
            }

            // Exits from no more detected triggers.
            for (int i = _overlappingTriggers.Count; i-- > 0;) {

                ITrigger _trigger = _overlappingTriggers[i];
                if (HasExitedTrigger(_trigger, _buffer)) {

                    _overlappingTriggers.RemoveAt(i);
                    OnExitTrigger(_trigger);
                }
            }

            // ----- Local Methods ----- \\

            static bool HasEnteredTrigger(ITrigger _trigger, List<ITrigger> _overlappingTriggers) {

                for (int i = _overlappingTriggers.Count; i-- > 0;) {
                    if (_trigger.Equals(_overlappingTriggers[i])) {
                        return false;
                    }
                }

                return true;
            }

            static bool HasExitedTrigger(ITrigger _trigger, List<ITrigger> _buffer) {

                for (int i = _buffer.Count; i-- > 0;) {
                    if (_trigger.Equals(_buffer[i])) {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Exits from all overlapping triggers.
        /// </summary>
        protected void ExitTriggers() {

            List<ITrigger> _overlappingTriggers = overlappingTriggers.collection;
            for (int i = _overlappingTriggers.Count; i-- > 0;) {

                ITrigger _trigger = _overlappingTriggers[i];
                OnExitTrigger(_trigger);
            }

            _overlappingTriggers.Clear();
        }

        // -------------------------------------------
        // Callbacks
        // -------------------------------------------

        /// <summary>
        /// Called when this object enters in a trigger.
        /// </summary>
        /// <param name="_trigger">Entering <see cref="ITrigger"/>.</param>
        protected virtual void OnEnterTrigger(ITrigger _trigger) {

            _trigger.OnEnterTrigger(this);
            triggerController.OnEnterTrigger(_trigger);
        }

        /// <summary>
        /// Called when this object exits from a trigger.
        /// </summary>
        /// <param name="_trigger">Exiting <see cref="ITrigger"/>.</param>
        protected virtual void OnExitTrigger(ITrigger _trigger) {

            _trigger.OnExitTrigger(this);
            triggerController.OnExitTrigger(_trigger);
        }

        // -------------------------------------------
        // Trigger Actor
        // -------------------------------------------

        /// <inheritdoc cref="ITriggerActor.ExitTrigger(ITrigger)"/>
        void ITriggerActor.ExitTrigger(ITrigger _trigger) {

            EnhancedCollection<ITrigger> _overlappingTriggers = overlappingTriggers;

            // Remove from list.
            int _index = _overlappingTriggers.IndexOf(_trigger);
            if (_index != -1) {
                _overlappingTriggers.RemoveAt(_index);
            }

            OnExitTrigger(_trigger);
        }
        #endregion

        #region Refresh
        /// <summary>
        /// Refresh this object position, and extract it from any overlapping collider.
        /// </summary>
        public void RefreshPosition() {

            // Extract from all overlapping colliders.
            PhysicsSystem.ExtractFromColliders(this, selfColliders);

            RefreshTriggers();

            shouldBeRefreshed = false;
            lastPosition = transform.position;
        }

        // -------------------------------------------
        // Overlap
        // -------------------------------------------

        /// <summary>
        /// Get all <see cref="UnityEngine.Collider"/> currently overlapping with this object.
        /// </summary>
        /// <param name="_buffer">Buffer used to store all overlapping <see cref="UnityEngine.Collider"/>.</param>
        /// <returns>Total count of overlapping objects.</returns>
        public int ColliderOverlap(List<Collider> _buffer) {
            return PhysicsSystem.PerformOverlap(this, _buffer, selfColliders);
        }

        /// <summary>
        /// Performs an overlap operation using this object main physics collider.
        /// </summary>
        /// <returns>Total count of overlapping colliders.</returns>
        public int ColliderOverlap() {
            return PhysicsCollider.Overlap(selfColliders, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// Performs an overlap operation using this object trigger.
        /// </summary>
        /// <returns>Total count of overlapping triggers.</returns>
        public int TriggerOverlap() {
            return PhysicsTrigger.Overlap(selfColliders, QueryTriggerInteraction.Collide);
        }

        /// <summary>
        /// Get the overlapping collider or trigger at a given index.
        /// <para/>
        /// Use <see cref="ColliderOverlap"/> or <see cref="TriggerOverlap"/> to get the count of overlapping objects.
        /// </summary>
        /// <param name="_index">Index of the collider to get.</param>
        /// <returns>The overlapping collider at the given index.</returns>
        public Collider GetOverlapAt(int _index) {
            return PhysicsCollider3D.GetOverlapCollider(_index);
        }
        #endregion

        #region Collision Effect
        public const float MinEffectWeight = .01f;

        // -------------------------------------------
        // Effects are used to interact with other
        // encountered Movable instances.
        //
        // For instance, when entering in contact with another object,
        // this Movable can push it according to its velocity and not consider it as an obstacle.
        // -------------------------------------------

        /// <summary>
        /// Collides and push another <see cref="Movable3D"/> according to a given velocity.
        /// </summary>
        /// <param name="_other">The other <see cref="Movable3D"/> colliding with this object.</param>
        /// <param name="_velocity">Original velocity of this object.</param>
        /// <returns>Computed velocity of this object after collision.</returns>
        public Vector3 PushObject(Movable3D _other, Vector3 _velocity) {
            if (_other.RockBehaviour)
                return Vector3.zero;

            _velocity *= GetPushVelocityCoef(_other, _velocity);
            _other.OffsetPosition(_velocity);

            return _velocity;
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        /// <summary>
        /// Comptutes the velocity of this object after colliding with another <see cref="Movable3D"/>.
        /// </summary>
        /// <param name="_other">The other <see cref="Movable3D"/> colliding with this object.</param>
        /// <param name="_velocity">Original velocity of this object.</param>
        /// <returns>Percentage of this object velocity after collision (between 0 and 1).</returns>
        public float GetPushVelocityCoef(Movable3D _other, Vector3 _velocity) {
            if (_other.RockBehaviour)
                return 0f;

            return GetPushVelocityCoef_Internal(_other, _velocity);
        }

        /// <inheritdoc cref="GetPushVelocityCoef"/>
        protected virtual float GetPushVelocityCoef_Internal(Movable3D _other, Vector3 _velocity) {
            if (pushRange.y < MinEffectWeight)
                return 0f;

            float _start = _other.Weight - pushRange.x;
            float _range = pushRange.y   - pushRange.x;

            if (Mathm.ApproximatelyZero(_range)) {
                _range = 1f;
            }

            float _percent = 1f - Mathf.Clamp01(_start / _range);
            return pushCurve.Evaluate(_percent);
        }
        #endregion

        // --- Utility --- \\

        #region Transform
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
            SetPosition(Position + _offset);
        }

        /// <summary>
        /// Adds an offset to this object rotation.
        /// </summary>
        /// <param name="_offset">Transform rotation offset.</param>
        public void OffsetRotation(Quaternion _offset) {
            SetRotation(Rotation * _offset);
        }

        // -------------------------------------------
        // Editor
        // -------------------------------------------

        [Button(ActivationMode.Play, SuperColor.Raspberry, IsDrawnOnTop = false)]
        #pragma warning disable IDE0051
        private void SetTransformValues(Transform _transform, bool _usePosition = true, bool _useRotation = true) {
            if (_usePosition) {
                SetPosition(_transform.position);
            }

            if (_useRotation) {
                SetRotation(_transform.rotation);
            }
        }
        #endregion        

        #region Utility
        /// <summary>
        /// Enables/Disables this object colliders.
        /// </summary>
        /// <param name="_enabled">Whether to enable or disable colliders.</param>
        public void EnableColliders(bool _enabled) {
            collider.enabled = _enabled;
            trigger .enabled = _enabled;
        }

        /// <summary>
        /// Get if a specific option is active on this object.
        /// </summary>
        /// <param name="_option">The option to check activation.</param>
        /// <returns>True if this option is active on this object, false otherwise.</returns>
        public bool HasOption(MovableOption _option) {
            return options.HasFlagUnsafe(_option);
        }
        #endregion

        #region Documentation
        /// <summary>
        /// Documentation only method.
        /// </summary>
        /// <returns>True to override this behaviour from the controller, false to call the base definition.</returns>
        protected bool Doc() { return false; }
        #endregion
    }
}
