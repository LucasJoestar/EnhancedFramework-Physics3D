// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using EnhancedEditor;
using UnityEngine;

namespace EnhancedFramework.Movable3D {
    /// <summary>
    /// Controller contract for a <see cref="Movable3D"/>.
    /// <br/> Use this to receive various callbacks and override this object default behaviour.
    /// <para/>
    /// Should be set on Awake, as some operations are made on the Start callback.
    /// </summary>
    public interface IMovable3DController {
        #region Collision Configuration
        /// <inheritdoc cref="Movable3D.CollisionType"/>
        CollisionSystem3DType CollisionType { get; }

        /// <inheritdoc cref="Movable3D.CollisionMask"/>
        int CollisionMask { get; }

        /// <inheritdoc cref="Movable3D.TriggerMask"/>
        int TriggerMask { get; }
        #endregion

        #region Velocity
        /// <inheritdoc cref="Movable3D.ResetVelocity"/>
        /// <returns>False to completely override this behaviour, true to continue execution and call the base definition.</returns>
        bool OnResetVelocity();
        #endregion

        #region Speed
        /// <param name="_doIncrease"><inheritdoc cref="Movable3D.DoIncreaseSpeed()" path="/returns"/></param>
        /// <returns><inheritdoc cref="OnResetVelocity" path="/returns"/></returns>
        /// <inheritdoc cref="Movable3D.DoIncreaseSpeed"/>
        bool OnDoIncreaseSpeed(out bool _doIncrease);

        /// <inheritdoc cref="Movable3D.DecreaseSpeed(float)"/>
        /// <returns><inheritdoc cref="OnResetVelocity" path="/returns"/></returns>
        bool OnDecreaseSpeed(float _coef);

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

        #region Computing
        /// <param name="_velocity"><inheritdoc cref="Movable3D.ComputeVelocity()" path="/returns"/></param>
        /// <returns><inheritdoc cref="OnResetVelocity" path="/returns"/></returns>
        /// <inheritdoc cref="Movable3D.ComputeVelocity"/>
        bool OnComputeVelocity(out FrameVelocity _velocity);
        #endregion

        #region Collision Callbacks
        /// <inheritdoc cref="Movable3D.SetGroundState(bool, RaycastHit)"/>
        /// <returns><inheritdoc cref="OnResetVelocity" path="/returns"/></returns>
        bool SetGroundState(ref bool _isGrounded, RaycastHit _hit);

        /// <inheritdoc cref="Movable3D.OnAppliedVelocity(FrameVelocity, CollisionInfos)"/>
        /// <returns><inheritdoc cref="OnResetVelocity" path="/returns"/></returns>
        bool OnAppliedVelocity(ref FrameVelocity _velocity, CollisionInfos _infos);

        /// <inheritdoc cref="Movable3D.OnRefreshedObject(FrameVelocity, CollisionInfos)"/>
        /// <returns><inheritdoc cref="OnResetVelocity" path="/returns"/></returns>
        bool OnRefreshedObject(ref FrameVelocity _velocity, CollisionInfos _infos);

        /// <inheritdoc cref="Movable3D.OnSetGrounded(bool)"/>
        /// <returns><inheritdoc cref="OnResetVelocity" path="/returns"/></returns>
        bool OnSetGrounded(bool _isGrounded);

        /// <inheritdoc cref="Movable3D.OnSetMoving(bool)"/>
        /// <returns><inheritdoc cref="OnResetVelocity" path="/returns"/></returns>
        bool OnSetMoving(bool isMoving);

        /// <inheritdoc cref="Movable3D.OnReachedMaxSpeed(bool)"/>
        /// <returns><inheritdoc cref="OnResetVelocity" path="/returns"/></returns>
        bool OnReachedMaxSpeed(bool _hasReachedMaxSpeed);
        #endregion

        #region Overlapping & Triggers
        /// <inheritdoc cref="Movable3D.OnExtractCollider(Collider, Vector3, float)"/>
        /// <returns><inheritdoc cref="OnResetVelocity" path="/returns"/></returns>
        bool OnExtractFromCollider(Collider _collider, Vector3 _direction, float _distance);

        /// <inheritdoc cref="Movable3D.OnEnterTrigger(Trigger3D)"/>
        void OnEnterTrigger(Trigger3D _trigger);

        /// <inheritdoc cref="Movable3D.OnExitTrigger(Trigger3D)"/>
        void OnExitTrigger(Trigger3D _trigger);
        #endregion
    }

    /// <summary>
    /// Extended <see cref="Movable3D"/> class, using an external <see cref="IMovable3DController"/> instance.
    /// </summary>
    public class ControlledMovable3D : Movable3D {
        #region Collision Settings
        public override CollisionSystem3DType CollisionType {
            get {
                return Controller.CollisionType;
            }
        }

        public override int CollisionMask {
            get {
                return Controller.CollisionMask;
            }
        }

        public override int TriggerMask {
            get {
                return Controller.TriggerMask;
            }
        }
        #endregion

        #region Global Members
        [Section("Controlled Movable 3D")]

        [SerializeField] protected SerializedInterface<IMovable3DController> controller = null;

        /// <summary>
        /// This movable controller instance.
        /// </summary>
        public IMovable3DController Controller {
            get {
                return controller.Interface;
            } set {
                controller.Interface = value;
            }
        }
        #endregion

        #region Velocity
        public override void ResetVelocity() {
            if (Controller.OnResetVelocity()) {
                base.ResetVelocity();
            }
        }
        #endregion

        #region Speed
        protected override bool DoIncreaseSpeed() {
            if (!Controller.OnDoIncreaseSpeed(out bool _doIncrease)) {
                return _doIncrease;
            }

            return base.DoIncreaseSpeed();
        }

        public override void DecreaseSpeed() {
            if (Controller.OnDecreaseSpeed(0f)) {
                base.DecreaseSpeed();
            }
        }

        public override void ResetSpeed() {
            if (Controller.OnResetSpeed()) {
                base.ResetSpeed();
            }
        }
        #endregion

        #region Update
        protected override void OnPreUpdate() {
            base.OnPreUpdate();

            Controller.OnPreUpdate();
        }

        protected override void OnPostUpdate() {
            base.OnPostUpdate();

            Controller.OnPostUpdate();
        }
        #endregion

        #region Gravity
        protected override void ApplyGravity() {
            if (Controller.OnApplyGravity()) {
                base.ApplyGravity();
            }
        }
        #endregion

        #region Computing
        protected override FrameVelocity ComputeVelocity() {
            if (!Controller.OnComputeVelocity(out FrameVelocity _velocity)) {
                return _velocity;
            }

            return base.ComputeVelocity();
        }
        #endregion

        #region Collision Callbacks
        protected internal override void SetGroundState(bool _isGrounded, RaycastHit _hit) {
            if (Controller.SetGroundState(ref _isGrounded, _hit)) {
                base.SetGroundState(_isGrounded, _hit);
            }
        }

        protected override void OnAppliedVelocity(FrameVelocity _velocity, CollisionInfos _infos) {
            if (Controller.OnAppliedVelocity(ref _velocity, _infos)) {
                base.OnAppliedVelocity(_velocity, _infos);
            }
        }

        protected override void OnRefreshedObject(FrameVelocity _velocity, CollisionInfos _infos) {
            if (Controller.OnRefreshedObject(ref _velocity, _infos)) {
                base.OnRefreshedObject(_velocity, _infos);
            }
        }

        protected override void OnSetGrounded(bool _isGrounded) {
            if (Controller.OnSetGrounded(_isGrounded)) {
                base.OnSetGrounded(_isGrounded);
            }
        }

        protected override void OnSetMoving(bool _isMoving) {
            if (Controller.OnSetMoving(_isMoving)) {
                base.OnSetMoving(_isMoving);
            }
        }

        protected override void OnReachedMaxSpeed(bool _hasReachedMaxSpeed) {
            if (Controller.OnReachedMaxSpeed(_hasReachedMaxSpeed)) {
                base.OnReachedMaxSpeed(_hasReachedMaxSpeed);
            }
        }
        #endregion

        #region Overlapping & Triggers
        protected override void OnExtractCollider(Collider _collider, Vector3 _direction, float _distance) {
            if (Controller.OnExtractFromCollider(_collider, _direction, _distance)) {
                base.OnExtractCollider(_collider, _direction, _distance);
            }
        }

        protected override void OnEnterTrigger(Trigger3D _trigger) {
            _trigger.OnEnterTrigger(this, Controller);
            Controller.OnEnterTrigger(_trigger);
        }

        protected override void OnExitTrigger(Trigger3D _trigger) {
            _trigger.OnExitTrigger(this, Controller);
            Controller.OnExitTrigger(_trigger);
        }
        #endregion
    }
}
