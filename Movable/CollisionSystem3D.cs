// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using System;
using EnhancedFramework.Core;
using UnityEngine;

namespace EnhancedFramework.Physics3D {
    /// <summary>
    /// <see cref="CollisionSystem3D"/>-related enum used to determine an object collision calculs.
    /// </summary>
    public enum CollisionSystem3DType {
        Simple,
        Complex,
        Creature,
    }

    /// <summary>
    /// Utility methods related to the <see cref="CollisionSystem3DType"/> enum.
    /// </summary>
    internal static class CollisionSystem3DTypeExtensions {
        #region Content
        /// <summary>
        /// Performs collision calculs for a specific <see cref="Movable3D"/> according to this <see cref="CollisionSystem3DType"/>,
        /// <br/> moving the object rigidbody accordingly in space.
        /// </summary>
        /// <param name="_type">Collision type to perform.</param>
        /// <param name="_movable"><see cref="Movable3D"/> to perform collisions for.</param>
        /// <param name="_objectVelocity"><see cref="Velocity"/> of the associated <see cref="Movable3D"/>.</param>
        /// <param name="_frameVelocity">Velocity to use to compute and perform collisions.</param>
        /// <param name="_ignoredColliders">Colliders to ignore during collisions.</param>
        /// <returns><see cref="CollisionInfos"/> containing various informations about all the collisions performed by the object.</returns>
        public static CollisionInfos PerformCollisions(this CollisionSystem3DType _type, Movable3D _movable, Velocity _objectVelocity, FrameVelocity _frameVelocity,
                                                       Collider[] _ignoredColliders) {
            switch (_type) {
                case CollisionSystem3DType.Simple:
                    return CollisionSystem3D.Instance.PerformCollisions(_movable, _objectVelocity, _frameVelocity, _ignoredColliders, 1);

                case CollisionSystem3DType.Complex:
                    return CollisionSystem3D.Instance.PerformCollisions(_movable, _objectVelocity, _frameVelocity, _ignoredColliders, 3);

                case CollisionSystem3DType.Creature:
                    return CreatureCollisionSystem3D.Instance.PerformCollisions(_movable, _objectVelocity, _frameVelocity, _ignoredColliders, 3);

                default:
                    throw new InvalidCollisionSystem3DTypeException();
            }
        }
        #endregion
    }

    /// <summary>
    /// Data-wrapper for a <see cref="CollisionSystem3D"/>-related informations and result, during this frame.
    /// <para/>
    /// Configured as a class with a static instance to avoid creating a new instance
    /// <br/> each time it is passed as a parameter, or its value is changed (which happens a lot).
    /// </summary>
    public class CollisionInfos {
        #region Global Members
        /// <summary>
        /// Buffer of all collision hits encountered during calculs.
        /// </summary>
        public readonly EnhancedCollection<RaycastHit> HitBuffer = new EnhancedCollection<RaycastHit>(3);

        public Vector3 OriginalVelocity = Vector3.zero;
        public Vector3 DynamicVelocity  = Vector3.zero;
        public Vector3 AppliedVelocity  = Vector3.zero;

        /// <summary>
        /// Is the object considered as grounded after collisions?
        /// </summary>
        public bool IsGrounded  = false;
        #endregion

        #region Initialization
        /// <summary>
        /// Initializes this collision infos, reseting all its collision results.
        /// </summary>
        /// <param name="_velocity">Velocity to use as base value.</param>
        /// <returns>This collisions infos.</returns>
        public CollisionInfos Init(FrameVelocity _velocity) {
            OriginalVelocity = DynamicVelocity
                             = _velocity.Movement + _velocity.Force;

            AppliedVelocity = Vector3.zero;

            IsGrounded = false;
            HitBuffer.Clear();

            return this;
        }
        #endregion

        #region Utility
        /// <summary>
        /// Compute a collision impact
        /// <br/> (Should not be called outside of a <see cref="CollisionSystem3D"/>).
        /// </summary>
        /// <param name="_hit">Impact hit to compute.</param>
        public void ComputeImpact(RaycastHit _hit) {
            HitBuffer.Add(_hit);
            DynamicVelocity = DynamicVelocity.PerpendicularSurface(_hit.normal);
        }
        #endregion
    }

    /// <summary>
    /// Collision system used to move an object in a 3D space (<see cref="Movable3D"/>).
    /// <br/> Configured as a non-static class to allow using inheritance for creating new collision system types.
    /// </summary>
    internal class CollisionSystem3D {
        #region Global Members
        /// <summary>
        /// The one and only instance of this collision system.
        /// </summary>
        public static readonly CollisionSystem3D Instance = new CollisionSystem3D();

        /// <summary>
        /// Informations used to calculate collisions.
        /// <br/> Shared across all objects for better performances.
        /// </summary>
        public static readonly CollisionInfos Infos = new CollisionInfos();

        /// <summary>
        /// Default empty collision informations used when no collision has been performed.
        /// </summary>
        public static CollisionInfos DefaultInfos {
            get {
                return Infos.Init(default);
            }
        }

        // -----------------------

        /// <summary>
        /// Prevent the creation of any new instance of this class.
        /// </summary>
        protected CollisionSystem3D() { }
        #endregion

        #region Collision Calculs
        public virtual CollisionInfos PerformCollisions(Movable3D _movable, Velocity _objectVelocity, FrameVelocity _frameVelocity, Collider[] _ignoredColliders, int _recursivity) {
            Rigidbody _rigidbody = _movable.rigidbody;
            Vector3 _position = _rigidbody.position;

            // The same infos wrapper is used for every collisions in the game, so clear and initialize its content before any use.
            CollisionInfos _infos = Infos.Init(ComputeVelocity(_movable, _frameVelocity));

            // The instant velocity requiring more precision and an absolute movement, perform it independently.
            Vector3 _instant = _frameVelocity.Instant;

            if (!_instant.IsNull()) {
                if (_movable.collider.Cast(_instant, out float _distance, QueryTriggerInteraction.Ignore, _ignoredColliders)) {
                    MoveObject(_rigidbody, _instant, _distance);
                } else {
                    MoveObject(_rigidbody, _instant);
                }
            }

            // Then, calculate the collision for the remaining velocity using recursivity.
            PerformCollisionsRecursively(_movable, _objectVelocity, _infos, _ignoredColliders, _recursivity);
            _infos.AppliedVelocity = _rigidbody.position - _position;

            // Finally, reset velocity before setting the ground state, to allow valid velocity modifications on ground callbacks.
            _objectVelocity.ResetFrameVelocity();
            ComputeGround(_movable, _infos, _ignoredColliders);

            return _infos;
        }

        protected virtual void PerformCollisionsRecursively(Movable3D _movable, Velocity _objectVelocity, CollisionInfos _infos, Collider[] _ignoredColliders, int _recursivity) {
            Rigidbody _rigidbody = _movable.rigidbody;
            PhysicsCollider3D _collider = _movable.collider;

            // Velocity cast.
            ref Vector3 _velocity = ref _infos.DynamicVelocity;
            int _amount = _collider.CastAll(_velocity, out float _distance, QueryTriggerInteraction.Ignore, _ignoredColliders);

            if (_amount == 0) {
                MoveObject(_rigidbody, _velocity);
                OnCollisionBreak(_movable, _objectVelocity, _infos, _ignoredColliders);
                return;
            }

            RaycastHit _hit = PhysicsCollider3D.GetCastHit(0);

            // Zero distance means the object is stuck into something.
            if (_distance == 0f) {
                ComputeCast(_objectVelocity, _infos, _hit);
                return;
            }

            // Move object and get remaining velocity after displacement.
            _velocity = MoveObject(_rigidbody, _velocity, _distance);
            ComputeCast(_objectVelocity, _infos, _amount);

            // Recursivity limit.
            if (--_recursivity == 0) {
                OnCollisionBreak(_movable, _objectVelocity, _infos, _ignoredColliders);
                return;
            }

            // Compute collision on main impact.
            OnComputeCollision(_movable, _objectVelocity, _infos, _hit, _ignoredColliders);

            if (_velocity.IsNull()) {
                OnCollisionBreak(_movable, _objectVelocity, _infos, _ignoredColliders);
            } else {
                PerformCollisionsRecursively(_movable, _objectVelocity, _infos, _ignoredColliders, _recursivity);
            }
        }
        #endregion

        #region Collision Callbacks
        protected virtual void OnComputeCollision(Movable3D _movable, Velocity _objectVelocity, CollisionInfos _infos, RaycastHit _hit, Collider[] _ignoredColliders) { }

        protected virtual void OnCollisionBreak(Movable3D _movable, Velocity _objectVelocity, CollisionInfos _infos, Collider[] _ignoredColliders) { }
        #endregion

        #region Additional Calculs
        /// <summary>
        /// Computes a specific <see cref="FrameVelocity"/> before collision calculs.
        /// </summary>
        protected virtual FrameVelocity ComputeVelocity(Movable3D _movable, FrameVelocity _velocity) {
            return _velocity;
        }

        /// <summary>
        /// Performs additional calculs before setting a <see cref="Movable3D"/> ground state.
        /// </summary>
        protected virtual bool ComputeGround(Movable3D _movable, CollisionInfos _infos, Collider[] _ignoredColliders) {
            bool _isGrounded = _infos.IsGrounded;
            RaycastHit _groundHit = default;

            if (!_isGrounded && _movable.UseGravity) {
                // Iterate over collision impacts to find if one of these can be considered as ground.
                // Use a reverse loop to get the last ground surface hit first.
                for (int i = _infos.HitBuffer.Count; i-- > 0;) {
                    RaycastHit _hit = _infos.HitBuffer[i];

                    if (IsGroundSurface(_movable, _hit)) {
                        _isGrounded = true;
                        _groundHit = _hit;

                        break;
                    }
                }

                // If didn't hit ground during movement, try to get it using two casts:
                //  • A raycast from collider bottom,
                //  • A Shapecast if the previous raycast failed.
                //
                // Necessary when movement magnitude is inferior to default contact offset.
                //
                // If using a sphere or a capsule collider, the cast can retrieve an obstacle
                // different than the ground when against a slope,
                // that's why a raycast from the bottom center is required.
                if (!_isGrounded) {
                    PhysicsCollider3D _collider = _movable.collider;
                    float _distance = Physics.defaultContactOffset * 2.5f;

                    if ((_collider.Raycast(-_movable.GroundNormal, out RaycastHit _hit, _distance)
                        || _collider.Cast(_movable.GravitySense, out _hit, _distance,QueryTriggerInteraction.Ignore, _ignoredColliders))
                        && IsGroundSurface(_movable, _hit)) {
                        // If found, set ground.
                        _isGrounded = true;
                        _groundHit = _hit;
                    }
                }
            }

            // Update ground state.
            _movable.SetGroundState(_isGrounded, _groundHit);
            return _isGrounded;
        }
        #endregion

        #region Utility
        protected virtual Vector3 MoveObject(Rigidbody _rigidbody, Vector3 _velocity, float _distance) {
            // To not stuck the object into another collider, be sure the compute contact offset.
            if ((_distance -= Physics.defaultContactOffset) > 0f) {
                Vector3 _move = _velocity.normalized * _distance;

                MoveObject(_rigidbody, _move);
                _velocity -= _move;
            }

            return _velocity;
        }

        protected virtual void MoveObject(Rigidbody _rigidbody, Vector3 _velocity) {
            _rigidbody.position += _velocity;
        }

        protected bool IsGroundSurface(Movable3D _movable, RaycastHit _hit) {
            return Physics3DUtility.IsGroundSurface(_hit, -_movable.GravitySense);
        }

        // -----------------------

        protected void ComputeCast(Velocity _velocity, CollisionInfos _infos, int _amount) {
            for (int i = 0; i < _amount; i++) {
                ComputeCast(_velocity, _infos, PhysicsCollider3D.GetCastHit(i));
            }
        }

        protected void ComputeCast(Velocity _velocity, CollisionInfos _infos, RaycastHit _hit) {
            _velocity.ComputeImpact(_hit);
            _infos.ComputeImpact(_hit);
        }
        #endregion
    }

    /// <summary>
    /// Creature-like collision system.
    /// <br/> Performs additional operations like step climbing and ground snapping.
    /// </summary>
    internal class CreatureCollisionSystem3D : CollisionSystem3D {
        #region Global Members
        /// <inheritdoc cref="CollisionSystem3D.Instance"/>
        public static readonly new CreatureCollisionSystem3D Instance = new CreatureCollisionSystem3D();

        // -----------------------

        /// <inheritdoc cref="CollisionSystem3D()"/>
        protected CreatureCollisionSystem3D() { }
        #endregion

        #region Collision Callbacks
        private const float ClimbValidationCastOffsetCoef = 2.5f;

        // -----------------------

        protected override void OnComputeCollision(Movable3D _movable, Velocity _objectVelocity, CollisionInfos _infos, RaycastHit _hit, Collider[] _ignoredColliders) {
            // Base call.
            base.OnComputeCollision(_movable, _objectVelocity, _infos, _hit, _ignoredColliders);

            // Obstacle collision.
            if (!IsGroundSurface(_movable, _hit)) {
                Rigidbody _rigidbody = _movable.rigidbody;
                PhysicsCollider3D _collider = _movable.collider;

                Vector3 _normal = _hit.normal;

                // Define if the obstacle can be climbed by casting all along it, then move the object according to cast informations.
                Vector3 _climb = Vector3.ProjectOnPlane(Vector3.up, _normal).normalized * _movable.ClimbHeight;

                _collider.Cast(_climb, out float _distance, QueryTriggerInteraction.Ignore, _ignoredColliders);
                _climb -= MoveObject(_rigidbody, _climb, _distance);

                Vector3 _validCast = Physics.defaultContactOffset * ClimbValidationCastOffsetCoef * -_normal;

                // Then perform another cast in the obstacle inverse normal direction. If nothing is hit, then the step can be climbed.
                // To climb it, simply add some velocity according the objstacle surface, and set the object as grounded (so gravity won't apply).
                if (!_collider.Cast(_validCast, out float _, QueryTriggerInteraction.Ignore, _ignoredColliders)) {
                    _infos.DynamicVelocity += Vector3.ClampMagnitude(_climb, _infos.OriginalVelocity.magnitude);
                    _infos.IsGrounded = true;
                }

                // Reset the object position as before cast.
                MoveObject(_rigidbody, -_climb);
            }
        }

        protected override void OnCollisionBreak(Movable3D _movable, Velocity _objectVelocity, CollisionInfos _infos, Collider[] _ignoredColliders) {
            // Base call.
            base.OnCollisionBreak(_movable, _objectVelocity, _infos, _ignoredColliders);

            // Ground snapping.
            // Only snap when grounded, as a falling object near the ground does not need to be snapped (would be visually ugly).
            if (!_movable.IsGrounded)
                return;

            Vector3 _direction = _movable.GravitySense;
            float _dot = Vector3.Dot(_movable.GravitySense, _infos.OriginalVelocity);

            // Only snap if the object original vertical velocity was not positive
            // (otherwise, a jumping object would be automatically bring back to the ground).
            if ((_dot >= 0f) && _movable.collider.Cast(_direction, out RaycastHit _hit, _movable.SnapHeight, QueryTriggerInteraction.Ignore, _ignoredColliders)) {
                MoveObject(_movable.rigidbody, _direction, _hit.distance);
                ComputeCast(_objectVelocity, _infos, _hit);
            }
        }
        #endregion

        #region Additional Calculs
        protected override FrameVelocity ComputeVelocity(Movable3D _movable, FrameVelocity _velocity) {
            // When grounded, project the object movement (relative velocity) on the ground surface.
            // Only project the object horizontal and forward velocity, to always keep a straight vertical trajectory.
            if (_movable.IsGrounded) {
                Quaternion _rotation = _velocity.Rotation;
                ref Vector3 _movement = ref _velocity.Movement;

                Vector3 _vertical = Vector3.up.Rotate(_rotation) * _movement.RotateInverse(_rotation).y;
                Vector3 _flat = Vector3.ProjectOnPlane(_movement - _vertical, _movable.GroundNormal);

                _movement = _flat + _vertical;
            }

            return base.ComputeVelocity(_movable, _velocity);
        }
        #endregion
    }

    #region Exception
    /// <summary>
    /// Exception used for invalid <see cref="CollisionSystem3DType"/>,
    /// when the int value is outside its enum limits.
    /// </summary>
    public class InvalidCollisionSystem3DTypeException : Exception {
        public InvalidCollisionSystem3DTypeException() : base() { }

        public InvalidCollisionSystem3DTypeException(string _message) : base(_message) { }

        public InvalidCollisionSystem3DTypeException(string _message, Exception _innerException) : base(_message, _innerException) { }
    }
    #endregion
}
