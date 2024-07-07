// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using EnhancedEditor;
using EnhancedFramework.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace EnhancedFramework.Physics3D {
    // -------------------------------------------
    // Data & Utility
    // -------------------------------------------

    /// <summary>
    /// <see cref="PhysicsSystem3D{T}"/>-related enum used to determine collision calculs.
    /// </summary>
    public enum PhysicsSystem3DType {
        [Tooltip("Simple collisions for afordable performances\n\nIterations: 1")]
        Simple          = 0,

        [Tooltip("Intermediate collisions complexity\n\nIterations: 2")]
        Intermediate    = 2,

        [Tooltip("Complex collisions for a more accurate behaviour\n\nIterations: 3")]
        Complex         = 3,

        [Tooltip("Creature-like collisions with additional operations according to the surface\n\nIterations: 3")]
        Creature        = 10,

        [Separator(SeparatorPosition.Top)]

        [Tooltip("Complex collisions with multiple colliders\n\nIterations: 3")]
        MultiComplex    = 21,

        [Tooltip("Creature-like collisions with multiple colliders\n\nIterations: 3")]
        MultiCreature   = 22,
    }

    /// <summary>
    /// Data wrapper for a single <see cref="Movable3D"/>-related <see cref="UnityEngine.RaycastHit"/>.
    /// </summary>
    public struct CollisionHit3D {
        #region Global Members
        public readonly RaycastHit RaycastHit;

        public readonly Collider SourceCollider;
        public readonly Movable3D HitMovable;

        public readonly bool HasHitMovable;
        public float Distance;

        public readonly Collider HitCollider {
            get { return RaycastHit.collider; }
        }

        // -------------------------------------------
        // Constructor(s)
        // -------------------------------------------

        /// <inheritdoc cref="CollisionHit3D"/>
        public CollisionHit3D(float _distance) : this(new RaycastHit() { distance = _distance }, null, false) { }

        /// <inheritdoc cref="CollisionHit3D"/>
        public CollisionHit3D(RaycastHit _hit) : this(_hit, null, false) { }

        /// <inheritdoc cref="CollisionHit3D"/>
        public CollisionHit3D(RaycastHit _hit, Collider _collider, bool _getMovable = true) {
            RaycastHit = _hit;
            SourceCollider = _collider;

            Distance = _hit.distance;

            if (_getMovable) {
                HasHitMovable = _hit.collider.TryGetComponentInParent(out HitMovable);
            } else {
                HasHitMovable = false;
                HitMovable = null;
            }
        }
        #endregion

        #region Utility
        public static readonly Comparison<CollisionHit3D> DistanceComparer = SortByDistance;

        // -----------------------

        /// <summary>
        /// Get this hit associated <see cref="Movable3D"/>.
        /// </summary>
        /// <param name="_movable"><see cref="Movable3D"/> of the hit object.</param>
        /// <returns>True if a <see cref="Movable3D"/> could be found on the hit object, false otherwise.</returns>
        public readonly bool GetMovable(out Movable3D _movable) {
            _movable = HitMovable;
            return HasHitMovable;
        }

        /// <summary>
        /// Sorts two <see cref="CollisionHit3D"/> by their hit distance.
        /// </summary>
        public static int SortByDistance(CollisionHit3D a, CollisionHit3D b) {
            return a.Distance.CompareTo(b.Distance);
        }
        #endregion
    }

    /// <summary>
    /// <see cref="PhysicsSystem3D{T}"/> result data-wrapper
    /// <para/>
    /// Configured as a class with a static instance to avoid creating a new instance
    /// <br/> each time it is passed as a parameter, or its value is changed (which happens a lot).
    /// </summary>
    public sealed class CollisionData3D {
        #region Global Members
        /// <summary>
        /// Static instance of this class.
        /// </summary>
        public static readonly CollisionData3D Data = new CollisionData3D();

        /// <summary>
        /// Hits of this collision operations.
        /// </summary>
        public readonly EnhancedCollection<CollisionHit3D> HitBuffer = new EnhancedCollection<CollisionHit3D>(3);

        public Vector3 OriginalVelocity = Vector3.zero;
        public Vector3 DynamicVelocity  = Vector3.zero;
        public Vector3 AppliedVelocity  = Vector3.zero;

        /// <summary>
        /// Is the object considered as grounded after collisions?
        /// </summary>
        public bool IsGrounded = false;

        // -------------------------------------------
        // Constructor(s)
        // -------------------------------------------

        /// <summary>
        /// Prevents from creating new instances of this class.
        /// </summary>
        private CollisionData3D() { }
        #endregion

        #region Initialization
        /// <summary>
        /// Initializes this collision infos, reseting all its results to their default values.
        /// </summary>
        /// <param name="_velocity">Initial velocity used to perform collisions.</param>
        /// <returns>This <see cref="CollisionData3D"/>.</returns>
        internal CollisionData3D Init(FrameVelocity _velocity) {
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
        /// Computes a collision impact.
        /// </summary>
        /// <param name="_movable"><see cref="Movable3D"/> instance instigator of this collision.</param>
        /// <param name="_hit">Hit to compute.</param>
        internal void ComputeImpact(Movable3D _movable, CollisionHit3D _hit) {
            HitBuffer.Add(_hit);
            DynamicVelocity = _movable.ComputeImpact(DynamicVelocity, _hit);
        }
        #endregion
    }

    /// <summary>
    /// Utility methods related to the <see cref="PhysicsSystem3DType"/> enum.
    /// </summary>
    internal static class PhysicsSystem3DTypeExtensions {
        #region Content
        /// <summary>
        /// Performs collision calculs for a specific <see cref="Movable3D"/> according to this <see cref="PhysicsSystem3DType"/>,
        /// <br/> moving the object rigidbody accordingly in space.
        /// </summary>
        /// <param name="_type">Physics system to use.</param>
        /// <param name="_movable"><see cref="Movable3D"/> to perform collisions for.</param>
        /// <param name="_velocity">Total <see cref="Velocity"/> of the associated <see cref="Movable3D"/>.</param>
        /// <param name="_frameVelocity">This frame velocity, used to compute and perform collisions.</param>
        /// <param name="_ignoredColliders">All colliders to ignore.</param>
        /// <returns><see cref="CollisionData3D"/> containing various informations about performed collisions.</returns>
        public static CollisionData3D PerformCollisions(this PhysicsSystem3DType _type, Movable3D _movable, Velocity _velocity, FrameVelocity _frameVelocity,
                                                       IList<Collider> _ignoredColliders = null) {
            switch (_type) {
                // Standard collisions - only one iteration.
                case PhysicsSystem3DType.Simple:
                    return SimplePhysicsSystem3D.Instance     .PerformCollisions(_movable, _velocity, _frameVelocity, _ignoredColliders, 1, SingleColliderMovable3DPhysicsWrapper.Instance);

                // Standard collisions - two iterations maximum.
                case PhysicsSystem3DType.Intermediate:
                    return SimplePhysicsSystem3D.Instance     .PerformCollisions(_movable, _velocity, _frameVelocity, _ignoredColliders, 2, SingleColliderMovable3DPhysicsWrapper.Instance);

                // Standard collisions - three iterations maximum.
                case PhysicsSystem3DType.Complex:
                    return SimplePhysicsSystem3D.Instance     .PerformCollisions(_movable, _velocity, _frameVelocity, _ignoredColliders, 3, SingleColliderMovable3DPhysicsWrapper.Instance);

                // Complex collisions with additional operations - three iterations maximum.
                case PhysicsSystem3DType.Creature:
                    return CreaturePhysicsSystem3D.Instance   .PerformCollisions(_movable, _velocity, _frameVelocity, _ignoredColliders, 3, SingleColliderMovable3DPhysicsWrapper.Instance);

                // Complex collisions using multiple colliders - two iterations maximum.
                case PhysicsSystem3DType.MultiComplex:
                    return SimplePhysicsSystem3D.Instance     .PerformCollisions(_movable, _velocity, _frameVelocity, _ignoredColliders, 2, MultiColliderMovable3DPhysicsWrapper.Instance);

                // Creature collisions using multiple colliders - three iterations maximum.
                case PhysicsSystem3DType.MultiCreature:
                    return CreaturePhysicsSystem3D.Instance   .PerformCollisions(_movable, _velocity, _frameVelocity, _ignoredColliders, 3, MultiColliderMovable3DPhysicsWrapper.Instance);

                default:
                    throw new InvalidPhysicsSystem3DTypeException();
            }
        }

        /// <summary>
        /// Performs an overlap for a specific <see cref="Movable3D"/> according to this <see cref="PhysicsSystem3DType"/>,
        /// <br/> and get informations about all overlapping <see cref="Collider"/>.
        /// </summary>
        /// <param name="_type">Physics system to use.</param>
        /// <param name="_movable"><see cref="Movable3D"/> to perform an overlap for.</param>
        /// <param name="_buffer">Buffer used to store all overlapping <see cref="Collider"/>.</param>
        /// <param name="_ignoredColliders">All colliders to ignore.</param>
        /// <returns>Total count of overlapping colliders.</returns>
        public static int PerformOverlap(this PhysicsSystem3DType _type, Movable3D _movable, List<Collider> _buffer, IList<Collider> _ignoredColliders = null) {
            switch (_type) {
                // Simple system with a single collider.
                case PhysicsSystem3DType.Simple:
                case PhysicsSystem3DType.Intermediate:
                case PhysicsSystem3DType.Complex:
                    return SimplePhysicsSystem3D    .Instance.PerformOverlap(_movable, _buffer, _ignoredColliders, SingleColliderMovable3DPhysicsWrapper.Instance);

                // Complex system with a single collider.
                case PhysicsSystem3DType.Creature:
                    return CreaturePhysicsSystem3D  .Instance.PerformOverlap(_movable, _buffer, _ignoredColliders, SingleColliderMovable3DPhysicsWrapper.Instance);

                // Simple system with multiple colliders.
                case PhysicsSystem3DType.MultiComplex:
                    return SimplePhysicsSystem3D    .Instance.PerformOverlap(_movable, _buffer, _ignoredColliders, MultiColliderMovable3DPhysicsWrapper.Instance);

                // Complex system with multiple colliders.
                case PhysicsSystem3DType.MultiCreature:
                    return CreaturePhysicsSystem3D  .Instance.PerformOverlap(_movable, _buffer, _ignoredColliders, MultiColliderMovable3DPhysicsWrapper.Instance);

                default:
                    throw new InvalidPhysicsSystem3DTypeException();
            }
        }

        /// <summary>
        /// Performs an overlap and extract a specific <see cref="Movable3D"/> from all colliders according to this <see cref="PhysicsSystem3DType"/>
        /// </summary>
        /// <param name="_type">Physics system to use.</param>
        /// <param name="_movable"><see cref="Movable3D"/> to extract.</param>
        /// <param name="_ignoredColliders">All colliders to ignore.</param>
        public static void ExtractFromColliders(this PhysicsSystem3DType _type, Movable3D _movable, IList<Collider> _ignoredColliders = null) {
            switch (_type) {
                // Simple system with a single collider.
                case PhysicsSystem3DType.Simple:
                case PhysicsSystem3DType.Intermediate:
                case PhysicsSystem3DType.Complex:
                    SimplePhysicsSystem3D   .Instance.ExtractFromColliders(_movable, _ignoredColliders, SingleColliderMovable3DPhysicsWrapper.Instance);
                    break;

                // Complex system with a single collider.
                case PhysicsSystem3DType.Creature:
                    CreaturePhysicsSystem3D .Instance.ExtractFromColliders(_movable, _ignoredColliders, SingleColliderMovable3DPhysicsWrapper.Instance);
                    break;

                // Simple system with multiple colliders.
                case PhysicsSystem3DType.MultiComplex:
                    SimplePhysicsSystem3D   .Instance.ExtractFromColliders(_movable, _ignoredColliders, MultiColliderMovable3DPhysicsWrapper.Instance);
                    break;

                // Complex system with multiple colliders.
                case PhysicsSystem3DType.MultiCreature:
                    CreaturePhysicsSystem3D .Instance.ExtractFromColliders(_movable, _ignoredColliders, MultiColliderMovable3DPhysicsWrapper.Instance);
                    break;

                default:
                    throw new InvalidPhysicsSystem3DTypeException();
            }
        }
        #endregion
    }

    // -------------------------------------------
    // Collision Systems
    // -------------------------------------------

    /// <summary>
    /// Physics system used to move an object in a 3D space and other physics operations (see <see cref="Movable3D"/>).
    /// <br/> Configured as a non-static class to allow using inheritance for creating new systems.
    /// </summary>
    internal abstract class PhysicsSystem3D<T> where T : PhysicsSystem3D<T>, new() {
        #region Global Members
        /// <summary>
        /// The one and only instance of this system.
        /// </summary>
        public static readonly T Instance = new T();

        // -------------------------------------------
        // Constructor(s)
        // -------------------------------------------

        /// <summary>
        /// Prevents from creating new instances of this class.
        /// </summary>
        protected PhysicsSystem3D() { }
        #endregion

        // ===== Collisions ===== \\

        #region Collision Calculs
        private static readonly List<CollisionHit3D> collisionHitBuffer = new List<CollisionHit3D>();
        private Movable3DPhysicsWrapper physicsWrapper = null;

        // -----------------------

        /// <summary>
        /// Performs collisions and move the object in space.
        /// </summary>
        /// <param name="_movable"><see cref="Movable3D"/> to perform collisions for.</param>
        /// <param name="_velocity">Velocity of this object.</param>
        /// <param name="_frameVelocity">This frame velocity, used to perform collisions.</param>
        /// <param name="_ignoredColliders">Colliders to ignore during collisions.</param>
        /// <param name="_recursivity">Maximum allowed recursivity loop.</param>
        /// <param name="_physicsWrapper">Wrapper used to perform physics operations.</param>
        /// <returns>Data of this collision system results.</returns>
        public CollisionData3D PerformCollisions(Movable3D _movable, Velocity _velocity, FrameVelocity _frameVelocity, IList<Collider> _ignoredColliders, int _recursivity, Movable3DPhysicsWrapper _physicsWrapper) {

            Rigidbody _rigidbody = _movable.Rigidbody;
            Vector3 _position    = _rigidbody.position;

            // The same infos wrapper is used for every collisions in the game,
            // so clear and initialize its content before any use.
            _frameVelocity = OnComputeVelocity(_movable, _frameVelocity);
            CollisionData3D _data = CollisionData3D.Data.Init(_frameVelocity);

            // Cast wrapper setup.
            physicsWrapper = _physicsWrapper;
            _physicsWrapper.Prepare(_movable, _ignoredColliders);

            // Instant velocity requires more precision and an absolute displacement,
            // so perform it independently.
            Vector3 _instant = _frameVelocity.Instant;

            if (!_instant.IsNull()) {

                if (PerformCast(_movable, _rigidbody, _instant, out CollisionHit3D _hit, _ignoredColliders, true)) {
                    MoveObjectAndComputeImpacts(_movable, _rigidbody, _velocity, _data, ref _instant, _hit);
                } else {
                    MoveObject(_rigidbody, _instant);
                }
            }

            // Calculate the remaining velocity collisions, using recursivity.
            PerformCollisionsRecursively(_movable, _rigidbody, _velocity, _data, _ignoredColliders, _recursivity);
            _data.AppliedVelocity = _rigidbody.position - _position;

            // Reset the velocity before setting the ground state,
            // allowing velocity modifications on ground callbacks.
            OnComputeGround(_movable, _data, _ignoredColliders);

            return _data;
        }

        /// <param name="_rigidbody"><see cref="Rigidbody"/> of the associated <see cref="Movable3D"/>.</param>
        /// <param name="_velocity">Complete velocity of this object.</param>
        /// <param name="_data">Data used to store collision results.</param>
        /// <inheritdoc cref="PerformCollisions"/>
        private void PerformCollisionsRecursively(Movable3D _movable, Rigidbody _rigidbody, Velocity _velocity, CollisionData3D _data, IList<Collider> _ignoredColliders, int _recursivity) {

            // Velocity cast.
            ref Vector3 _castVelocity = ref _data.DynamicVelocity;
            int _amount = PerformCastAll(_movable, _rigidbody, _castVelocity, out CollisionHit3D _hit, _ignoredColliders, true);

            // Nothing hit along the way, so simply move the object and complete operation.
            if (_amount == 0) {
                MoveObject(_rigidbody, _castVelocity);
                OnCollisionBreak(_movable, _rigidbody, _velocity, _data, _ignoredColliders);
                return;
            }

            // Move this object and compute impacts - return if the object is stuck into something and cannot move.
            if (!MoveObjectAndComputeImpacts(_movable, _rigidbody, _velocity, _data, ref _castVelocity, _hit))
                return;

            // Recursivity limit.
            if (--_recursivity == 0) {
                OnCollisionBreak(_movable, _rigidbody, _velocity, _data, _ignoredColliders);
                return;
            }

            // Compute main collision.
            OnComputeCollision(_movable, _rigidbody, _velocity, _data, _hit, _ignoredColliders);

            if (_castVelocity.IsNull()) {
                OnCollisionBreak(_movable, _rigidbody, _velocity, _data, _ignoredColliders);
            } else {
                PerformCollisionsRecursively(_movable, _rigidbody, _velocity, _data, _ignoredColliders, _recursivity);
            }
        }

        // -------------------------------------------
        // Cast Operations
        // -------------------------------------------

        /// <inheritdoc cref="PerformCastAll(Movable3D, Rigidbody, Vector3, out CollisionHit3D, IList{Collider}, bool)"/>
        protected bool PerformCast(Movable3D _movable, Rigidbody _rigidbody, Vector3 _velocity, out CollisionHit3D _hit, IList<Collider> _ignoredColliders, bool _computeHits = true) {
            return PerformCastAll(_movable, _rigidbody, _velocity, out _hit, _ignoredColliders, _computeHits) != 0;
        }

        /// <summary>
        /// Performs a cast and precisely compute hit datas of the object.
        /// </summary>
        /// <param name="_velocity">Velocity used to perform this cast.</param>
        /// <param name="_hit">First hit of this cast.</param>
        /// <param name="_computeHits">If true automatically registers and computes hits.
        /// <br/> Otherwise, only  returns the closest one without any computation or registration.</param>
        /// <returns>Total amount of hit object.</returns>
        /// <inheritdoc cref="PerformCollisionsRecursively"/>
        protected int PerformCastAll(Movable3D _movable, Rigidbody _rigidbody, Vector3 _velocity, out CollisionHit3D _hit, IList<Collider> _ignoredColliders, bool _computeHits = true) {

            List<CollisionHit3D> _hitBuffer;
            
            if (_computeHits) {
                _hitBuffer = collisionHitBuffer;
                _hitBuffer.Clear();
            } else {
                _hitBuffer = null;
            }

            float _distance = _velocity.magnitude;
            int _amount = physicsWrapper.Cast(_movable, _rigidbody, _velocity, _distance, out RaycastHit _raycastHit, _ignoredColliders, _hitBuffer);

            // Nothing on the way.
            if (_amount == 0) {
                _hit = new CollisionHit3D(_distance);
                return 0;
            }

            // Compute hits.
            if (_computeHits) {
                _hit = ComputeCollisionHits(_movable, _velocity, _hitBuffer);
            } else {
                _hit = new CollisionHit3D(_raycastHit);
            }

            return _amount;
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        /// <summary>
        /// Computes and apply all given <see cref="CollisionHit3D"/>.
        /// </summary>
        /// <param name="_movable">This computing object movable.</param>
        /// <param name="_velocity">Frame velocity of the object.</param>
        /// <param name="_hits">All <see cref="CollisionHit3D"/> to compute and appy.</param>
        /// <returns>Main closest <see cref="CollisionHit3D"/> hit after computations.</returns>
        private CollisionHit3D ComputeCollisionHits(Movable3D _movable, Vector3 _velocity, List<CollisionHit3D> _hits) {
            float _distance = _velocity.magnitude;

            // No hit.
            if (_hits.Count == 0) {
                return new CollisionHit3D() { Distance = _distance };
            }

            _hits.Sort(CollisionHit3D.DistanceComparer);

            float _currentDistance  = _distance;
            bool _isStopped         = false;
            int _maxIndex           = _hits.Count - 1;
            int _count              = _hits.Count;

            // Try to push encountered objects - first, calculate how far we can go.
            for (int i = 0; i < _count; i++) {
                CollisionHit3D _collisionHit = _hits[i];

                if (_collisionHit.GetMovable(out Movable3D _other)) {

                    _other.OnHitByMovable(_movable, _collisionHit.SourceCollider, _collisionHit.HitCollider);
                    _currentDistance *= _movable.GetPushVelocityCoef(_other, _velocity);

                } else {

                    _currentDistance = 0f;
                }

                if (Mathm.ApproximatelyZero(_currentDistance)) {

                    _isStopped = true;
                    _maxIndex = i;

                    break;
                }
            }

            // Get closest hit.
            CollisionHit3D _hit = _hits[_maxIndex];

            if (_isStopped) {
                _velocity = _velocity.normalized * Mathf.Max(0f, _hit.Distance - Physics3DUtility.ContactOffset);
            } else {
                _hit.Distance = _distance;
            }

            // Push objects on the way.
            for (int i = 0; i <= _maxIndex; i++) {
                CollisionHit3D _collisionHit = _hits[i];

                if (_collisionHit.GetMovable(out Movable3D _other) && _other.isActiveAndEnabled) {
                    _velocity = _movable.PushObject(_other, _velocity);
                }
            }

            return _hit;
        }
        #endregion

        #region Collision Callbacks
        /// <summary>
        /// Called before any collision calculs to compute the <see cref="FrameVelocity"/>.
        /// </summary>
        protected virtual FrameVelocity OnComputeVelocity(Movable3D _movable, FrameVelocity _velocity) {
            return _velocity;
        }

        /// <summary>
        /// Called after a collision with another object to compute a <see cref="CollisionHit3D"/>.
        /// </summary>
        protected virtual void OnComputeCollision(Movable3D _movable, Rigidbody _rigidbody, Velocity _velocity, CollisionData3D _data, CollisionHit3D _hit, IList<Collider> _ignoredColliders) { }

        /// <summary>
        /// Called once the collision calculs are stopped, either interrupted or completed.
        /// </summary>
        protected virtual void OnCollisionBreak(Movable3D _movable, Rigidbody _rigidbody, Velocity _velocity, CollisionData3D _data, IList<Collider> _ignoredColliders) { }
        #endregion

        #region Ground
        /// <summary>
        /// Performs additional calculs before setting a <see cref="Movable3D"/> ground state.
        /// </summary>
        protected virtual bool OnComputeGround(Movable3D _movable, CollisionData3D _data, IList<Collider> _ignoredColliders) {
            RaycastHit _groundHit = default;
            bool _isGrounded = _data.IsGrounded;

            if (!_isGrounded && _movable.UseGravity) {

                // Iterate over collision impacts to find if one of these can be considered as ground.
                // Use a reverse loop to get the last ground surface hit first.

                for (int i = _data.HitBuffer.Count; i-- > 0;) {
                    CollisionHit3D _hit = _data.HitBuffer[i];

                    if (IsGroundSurface(_movable, _hit.RaycastHit)) {
                        _isGrounded = true;
                        _groundHit  = _hit.RaycastHit;

                        break;
                    }
                }

                // If didn't hit ground during movement, try to get it using two casts:
                //  • A raycast from collider bottom,
                //  • A shapecast if the previous raycast failed.
                //
                // Necessary when movement magnitude is inferior to default contact offset.
                //
                // If using a sphere or a capsule collider, the cast can retrieve an obstacle
                // different than the ground when against a slope.
                // That's why a raycast from the bottom center is required.

                if (!_isGrounded) {
                    PhysicsCollider3D _collider = _movable.PhysicsCollider;
                    float _distance = Physics3DUtility.ContactOffset * 2.5f;

                    if ((_collider.Raycast(-_movable.GroundNormal, out RaycastHit _hit, _distance) && IsGroundSurface(_movable, _hit)) ||
                        (PerformCast(_movable, _movable.Rigidbody, _movable.GravitySense * _distance, out CollisionHit3D _movableHit, _ignoredColliders, false) && IsGroundSurface(_movable, _movableHit.RaycastHit))) {

                        // If found, set ground.
                        _isGrounded = true;
                        _groundHit  = _hit;
                    }
                }
            }

            // Update ground state.
            _movable.SetGroundState(_isGrounded, _groundHit);
            return _isGrounded;
        }

        /// <summary>
        /// Get if a specific hit surface can be considered as a ground surface.
        /// </summary>
        /// <param name="_hit">Hit to check.</param>
        /// <returns>True if the surface can be considered as ground, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool IsGroundSurface(Movable3D _movable, RaycastHit _hit) {
            return Physics3DUtility.IsGroundSurface(_hit, -_movable.GravitySense);
        }
        #endregion

        #region Utility
        /// <summary>
        /// Displaces an object in space and computes a velocity according to all registered impacts.
        /// </summary>
        /// <param name="_castVelocity">Velocity used for this collision cast (dynamically modified to match the new velocity after displacement).</param>
        /// <param name="_mainHit">Main hit of this collision.</param>
        /// <returns>False if the object is stuck into something and cannot be moved, false otherwise.</returns>
        /// <inheritdoc cref="PerformCollisionsRecursively"/>
        protected static bool MoveObjectAndComputeImpacts(Movable3D _movable, Rigidbody _rigidbody, Velocity _velocity, CollisionData3D _data, ref Vector3 _castVelocity, CollisionHit3D _mainHit) {
            float _distance = _mainHit.Distance;

            // Zero distance means that the object is stuck into something and cannot move - so complete operation.
            if (_distance == 0f) {
                ComputeImpacts(_movable, _velocity, _data);
                return false;
            }

            // Move the object and get the remaining velocity, after displacement, according to the impact normal.
            //
            // For instance, the object may have a normalized velocity of (1, -1, 1).
            // We hit something under the object - the ground -, with a normal of (0, 1, 0).
            // So we can continue to perform collision with a velocity of (1, 0, 1).
            if (Mathf.Approximately(_distance, _castVelocity.magnitude)) {

                MoveObject(_rigidbody, _castVelocity);
                _castVelocity = Vector3.zero;

            } else {

                _castVelocity = MoveObject(_rigidbody, _castVelocity, _distance);
            }

            ComputeImpacts(_movable, _velocity, _data);
            return true;
        }

        // -------------------------------------------
        // Move
        // -------------------------------------------

        /// <param name="_distance">Max distance used to move this object.</param>
        /// <inheritdoc cref="MoveObject(Rigidbody, Vector3)"/>
        protected static Vector3 MoveObject(Rigidbody _rigidbody, Vector3 _velocity, float _distance) {
            // To not stuck the object into another collider, be sure the compute contact offset.
            if ((_distance -= Physics3DUtility.ContactOffset) > 0f) {
                Vector3 _move = _velocity.normalized * _distance;

                MoveObject(_rigidbody, _move);
                _velocity -= _move;
            }

            return _velocity;
        }

        /// <summary>
        /// Displaces an object in space.
        /// </summary>
        /// <param name="_rigidbody"><see cref="Rigidbody"/> of the object to move.</param>
        /// <param name="_velocity">World space position offset.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void MoveObject(Rigidbody _rigidbody, Vector3 _velocity) {
            _rigidbody.position += _velocity;
        }

        // -------------------------------------------
        // Compute
        // -------------------------------------------

        /// <summary>
        /// Computes all <see cref="CollisionHit3D"/> registered during the last cast operation.
        /// </summary>
        /// <inheritdoc cref="ComputeImpact(Movable3D, Velocity, CollisionData3D, CollisionHit3D)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void ComputeImpacts(Movable3D _movable, Velocity _velocity, CollisionData3D _data) {

            List<CollisionHit3D> _buffer = collisionHitBuffer;
            int _amount = _buffer.Count;

            for (int i = 0; i < _amount; i++) {
                ComputeImpact(_movable, _velocity, _data, _buffer[i]);
            }
        }

        /// <summary>
        /// Computes a <see cref="CollisionHit3D"/> data.
        /// </summary>
        /// <param name="_movable"><see cref="Movable3D"/> instigator of the impact.</param>
        /// <param name="_velocity">Velocity of the object.</param>
        /// <param name="_data">Collision data.</param>
        /// <param name="_hit">Hit to compute.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void ComputeImpact(Movable3D _movable, Velocity _velocity, CollisionData3D _data, CollisionHit3D _hit) {

            _velocity.ComputeImpact(_movable, _hit);
            _data    .ComputeImpact(_movable, _hit);
        }
        #endregion

        // ===== Other ===== \\

        #region Overlap
        /// <summary>
        /// Performs an overlap for this object and get all overlapping <see cref="Collider"/>.
        /// </summary>
        /// <param name="_movable"><see cref="Movable3D"/> to perform an overlap for.</param>
        /// <param name="_buffer">Buffer used to store overlap results.</param>
        /// <param name="_ignoredColliders">All colliders to ignore.</param>
        /// <param name="_physicsWrapper">Wrapper used to perform physics operations.</param>
        /// <returns>Total count of overlapping colliders.</returns>
        public int PerformOverlap(Movable3D _movable, List<Collider> _buffer, IList<Collider> _ignoredColliders, Movable3DPhysicsWrapper _physicsWrapper) {
            _physicsWrapper.Prepare(_movable, _ignoredColliders);
            return _physicsWrapper.Overlap(_movable, _buffer, _ignoredColliders);
        }

        /// <summary>
        /// Extracts this object from all overlapping <see cref="Collider"/>.
        /// </summary>
        /// <param name="_movable"><see cref="Movable3D"/> to extract.</param>
        /// <param name="_ignoredColliders">All colliders to ignore.</param>
        /// <param name="_physicsWrapper">Wrapper used to perform physics operations.</param>
        public void ExtractFromColliders(Movable3D _movable, IList<Collider> _ignoredColliders, Movable3DPhysicsWrapper _physicsWrapper) {
            _physicsWrapper.Prepare(_movable, _ignoredColliders);
            _physicsWrapper.Extract(_movable, _ignoredColliders);
        }
        #endregion
    }

    /// <summary>
    /// Simple collision system, without any additional operation.
    /// </summary>
    internal sealed class SimplePhysicsSystem3D : PhysicsSystem3D<SimplePhysicsSystem3D> { }

    /// <summary>
    /// Creature-like collision system.
    /// <br/> Performs additional operations like step climbing and ground snapping.
    /// </summary>
    internal sealed class CreaturePhysicsSystem3D : PhysicsSystem3D<CreaturePhysicsSystem3D> {
        #region Collision Callbacks
        private const float ClimbValidationCastOffsetCoef = 2.5f;

        // -----------------------

        protected override void OnComputeCollision(Movable3D _movable, Rigidbody _rigidbody, Velocity _velocity, CollisionData3D _data, CollisionHit3D _hit, IList<Collider> _ignoredColliders) {
            base.OnComputeCollision(_movable, _rigidbody, _velocity, _data, _hit, _ignoredColliders);

            RaycastHit _raycastHit = _hit.RaycastHit;

            // Obstacle collision.
            if (!IsGroundSurface(_movable, _raycastHit)) {

                // Define if the obstacle can be climbed by casting all along it, then move the object according to cast informations.
                Vector3 _normal = _raycastHit.normal;
                Vector3 _climb  = Vector3.ProjectOnPlane(Vector3.up, _normal).normalized * _movable.ClimbHeight;

                PerformCast(_movable, _rigidbody, _climb, out CollisionHit3D _castHit, _ignoredColliders, false);
                _climb -= MoveObject(_rigidbody, _climb, _castHit.Distance);

                Vector3 _validCast = Physics3DUtility.ContactOffset * ClimbValidationCastOffsetCoef * -_normal;

                // Then perform another cast in the obstacle inverse normal direction. If nothing is hit, then the step can be climbed.
                // To climb it, simply add some velocity according the objstacle surface, and set the object as grounded (so gravity won't apply).
                if (!PerformCast(_movable, _rigidbody, _validCast, out _, _ignoredColliders, false)) {

                    _data.DynamicVelocity += Vector3.ClampMagnitude(_climb, _data.OriginalVelocity.magnitude);
                    _data.IsGrounded = true;
                }

                // Reset the object position as before cast.
                MoveObject(_rigidbody, -_climb);
            }
        }

        protected override void OnCollisionBreak(Movable3D _movable, Rigidbody _rigidbody, Velocity _velocity, CollisionData3D _data, IList<Collider> _ignoredColliders) {
            base.OnCollisionBreak(_movable, _rigidbody, _velocity, _data, _ignoredColliders);

            // Ground snapping.
            // Only snap when grounded, as a falling object near the ground does not need to be snapped (and would be visually strange).
            if (!_movable.IsGrounded)
                return;

            Vector3 _direction = _movable.GravitySense;
            float _dot = Vector3.Dot(_direction, _data.OriginalVelocity);

            // Only snap if the object original vertical velocity was not positive
            // (otherwise, a jumping object would be automatically bring back to the ground).
            if (_dot < 0f)
                return;

            _direction *= _movable.SnapHeight;

            if (PerformCast(_movable, _rigidbody, _direction, out CollisionHit3D _hit, _ignoredColliders, true)) {
                MoveObjectAndComputeImpacts(_movable, _rigidbody, _velocity, _data, ref _direction, _hit);
            }
        }
        #endregion

        #region Additional Calculs
        protected override FrameVelocity OnComputeVelocity(Movable3D _movable, FrameVelocity _velocity) {

            // When grounded, project the object movement (relative velocity) on the ground surface.
            // Only project the object horizontal and forward velocity, to always keep a straight vertical trajectory.
            if (_movable.IsGrounded) {

                ref Vector3 _movement = ref _velocity.Movement;
                Quaternion _rotation  = _velocity.Rotation;

                Vector3 _vertical = Vector3.up.Rotate(_rotation) * _movement.RotateInverse(_rotation).y;
                Vector3 _flat     = Vector3.ProjectOnPlane(_movement - _vertical, _movable.GroundNormal);

                _movement = _flat + _vertical;
            }

            return base.OnComputeVelocity(_movable, _velocity);
        }
        #endregion
    }

    #region Exception
    /// <summary>
    /// Exception raised for an invalid <see cref="PhysicsSystem3DType"/>,
    /// when the int value is outside the enum limits.
    /// </summary>
    public sealed class InvalidPhysicsSystem3DTypeException : Exception {
        public InvalidPhysicsSystem3DTypeException() : base() { }

        public InvalidPhysicsSystem3DTypeException(string _message) : base(_message) { }

        public InvalidPhysicsSystem3DTypeException(string _message, Exception _innerException) : base(_message, _innerException) { }
    }
    #endregion
}
