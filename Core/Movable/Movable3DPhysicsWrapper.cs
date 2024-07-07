// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using System.Collections.Generic;
using UnityEngine;

namespace EnhancedFramework.Physics3D {
    // -------------------------------------------
    // Base
    // -------------------------------------------

    /// <summary>
    /// Base non-generic wrapper for a <see cref="Movable3D"/> cast operations.
    /// </summary>
    internal abstract class Movable3DPhysicsWrapper {
        #region Content
        /// <summary>
        /// Use this to cache any additional state or value before performing operations.
        /// </summary>
        public abstract void Prepare(Movable3D _movable, IList<Collider> _ignoredColliders);

        /// <summary>
        /// Performs a cast for a given <see cref="Movable3D"/>.
        /// </summary>
        /// <param name="_movable"><see cref="Movable3D"/> instance instigator of this cast.</param>
        /// <param name="_rigidbody"><see cref="Rigidbody"/> of this <see cref="Movable3D"/>.</param>
        /// <param name="_velocity">Velocity to use to perform this cast.</param>
        /// <param name="_distance">Max distance of this cast operation.</param>
        /// <param name="_hit">Main <see cref="RaycastHit"/> result.</param>
        /// <param name="_ignoredColliders">All <see cref="Collider"/> to ignore.</param>
        /// <param name="_hitBuffer">Buffer used to store all <see cref="CollisionHit3D"/> result.</param>
        /// <returns>Total amount of hit collisions.</returns>
        public abstract int Cast(Movable3D _movable, Rigidbody _rigidbody, Vector3 _velocity, float _distance, out RaycastHit _hit, IList<Collider> _ignoredColliders, List<CollisionHit3D> _hitBuffer);

        /// <summary>
        /// Performs an overlap for a given <see cref="Movable3D"/>.
        /// </summary>
        /// <param name="_movable"><see cref="Movable3D"/> instance instigator of this overlap.</param>
        /// <param name="_buffer">Buffer used to store overlap results.</param>
        /// <param name="_ignoredColliders">All <see cref="Collider"/> to ignore.</param>
        /// <returns>Total count of overlapping colliders.</returns>
        public abstract int Overlap(Movable3D _movable, List<Collider> _buffer, IList<Collider> _ignoredColliders);

        /// <summary>
        /// Performs an extraction for a given <see cref="Movable3D"/>.
        /// </summary>
        /// <param name="_movable"><see cref="Movable3D"/> instance to extract.</param>
        /// <param name="_ignoredColliders">All <see cref="Collider"/> to ignore.</param>
        public abstract void Extract(Movable3D _movable, IList<Collider> _ignoredColliders);

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        protected static void RegisterCollisionHits(Collider _collider, List<CollisionHit3D> _hitBuffer, int _amount) {

            // Store informations about all Movable-related hits - used later to apply special effects and for callbacks.
            for (int i = 0; i < _amount; i++) {

                RaycastHit _hit = PhysicsCollider3D.GetCastHit(i);

                // In case the same object is hit multiple times, only keep the closest hit.
                if (FindIndex(_hit.collider, out int _index) && (_hit.distance > _hitBuffer[_index].Distance))
                    continue;

                // Register hit.
                CollisionHit3D _movableHit = new CollisionHit3D(_hit, _collider, true);

                if (_index == -1) {
                    _hitBuffer.Add(_movableHit);
                } else {
                    _hitBuffer[_index] = _movableHit;
                }

                // ----- Local Method ----- \\

                bool FindIndex(Collider _collider, out int _index) {

                    for (_index = _hitBuffer.Count; _index-- > 0;) {
                        if (_hitBuffer[_index].HitCollider == _collider) {
                            return true;
                        }
                    }

                    _index = -1;
                    return false;
                }
            }
        }

        protected static void ExtractFromCollider(Movable3D _movable, Collider _collider, int _amount) {
            Transform _transform = _collider.transform;

            for (int i = 0; i < _amount; i++) {
                Collider _overlap = PhysicsCollider3D.GetOverlapCollider(i);
                Transform _overlapTransform = _overlap.transform;

                if (Physics.ComputePenetration(_collider, _transform.position,       _transform.rotation,
                                               _overlap, _overlapTransform.position, _overlapTransform.rotation,
                                               out Vector3 _direction, out float _distance)) {
                    // Collider extraction.
                    _movable.OnExtractFromCollider(_overlap, _direction, _distance);
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// Base generic wrapper for a <see cref="Movable3D"/> cast operations with an automatic static instance.
    /// </summary>
    internal abstract class Movable3DPhysicsWrapper<T> : Movable3DPhysicsWrapper where T : Movable3DPhysicsWrapper<T>, new() {
        #region Global Members
        /// <summary>
        /// The one and only instance of this class.
        /// </summary>
        public static readonly T Instance = new T();

        // -------------------------------------------
        // Constructor(s)
        // -------------------------------------------

        /// <summary>
        /// Prevents from creating new instances of this class.
        /// </summary>
        protected Movable3DPhysicsWrapper() { }
        #endregion
    }

    // -------------------------------------------
    // Wrappers
    // -------------------------------------------

    /// <summary>
    /// <see cref="Movable3DPhysicsWrapper{T}"/> used for classic collisions with a single collider.
    /// </summary>
    internal sealed class SingleColliderMovable3DPhysicsWrapper : Movable3DPhysicsWrapper<SingleColliderMovable3DPhysicsWrapper> {
        #region Content
        private PhysicsCollider3D collider = null;

        // -----------------------

        public override void Prepare(Movable3D _movable, IList<Collider> _ignoredColliders) {
            collider = _movable.PhysicsCollider;
        }

        public override int Cast(Movable3D _movable, Rigidbody _rigidbody, Vector3 _velocity, float _distance, out RaycastHit _hit, IList<Collider> _ignoredColliders, List<CollisionHit3D> _hitBuffer) {
            // Setup.
            PhysicsCollider3D _physicsCollider = collider;
            bool _registerHits = _hitBuffer != null;

            // Perform cast.
            int _amount = _physicsCollider.CastAll(_velocity, out _hit, _distance, QueryTriggerInteraction.Ignore, !_registerHits, _ignoredColliders);

            // Register hits.
            if (_registerHits) {
                RegisterCollisionHits(_physicsCollider.Collider, _hitBuffer, _amount);
            }

            return _amount;
        }

        public override int Overlap(Movable3D _movable, List<Collider> _buffer, IList<Collider> _ignoredColliders) {
            // Setup.
            PhysicsCollider3D _physicsCollider = collider;

            // Perform overlap.
            int _amount = _physicsCollider.Overlap(_ignoredColliders, QueryTriggerInteraction.Ignore);

            // Register overlap.
            if (_buffer != null) {
                _buffer.Clear();

                for (int i = 0; i < _amount; i++) {
                    _buffer.Add(PhysicsCollider3D.GetOverlapCollider(i));
                }
            }

            return _amount;
        }

        public override void Extract(Movable3D _movable, IList<Collider> _ignoredColliders) {
            // Setup.
            PhysicsCollider3D _physicsCollider = collider;

            // Perform overlap.
            int _amount = _physicsCollider.Overlap(_ignoredColliders, QueryTriggerInteraction.Ignore);

            // Extract.
            if (_amount != 0) {
                ExtractFromCollider(_movable, _physicsCollider.Collider, _amount);
            }
        }
        #endregion
    }

    /// <summary>
    /// <see cref="Movable3DPhysicsWrapper{T}"/> performing collisions with multiple colliders on the same object.
    /// </summary>
    internal sealed class MultiColliderMovable3DPhysicsWrapper : Movable3DPhysicsWrapper<MultiColliderMovable3DPhysicsWrapper> {
        #region Content
        private readonly List<Collider> colliders = new List<Collider>();
        private int collisionMask = -1;

        // -----------------------

        public override void Prepare(Movable3D _movable, IList<Collider> _ignoredColliders) {
            List<Collider> _buffer = colliders;
            List<Collider> _selfColliders = _movable.SelfColliders;

            // Get all active non-trigger colliders.
            _buffer.Clear();

            for (int i = 0; i < _selfColliders.Count; i++) {

                Collider _collider = _selfColliders[i];
                if (_collider.isTrigger || !_collider.enabled)
                    continue;

                _buffer.Add(_collider);
            }

            collisionMask = _movable.GetColliderMask();
        }

        public override int Cast(Movable3D _movable, Rigidbody _rigidbody, Vector3 _velocity, float _distance, out RaycastHit _hit, IList<Collider> _ignoredColliders, List<CollisionHit3D> _hitBuffer) {
            bool _registerHits = _hitBuffer != null;

            // Setup.
            List<Collider> _colliders = colliders;
            int _count = _colliders.Count;
            int _total = 0;

            _hit = new RaycastHit() { distance = int.MaxValue };

            // Perform cast for each colliders.
            for (int i = 0; i < _count; i++) {
                Collider _castCollider = _colliders[i];

                PhysicsCollider3D _physicsCollider = PhysicsCollider3D.GetTemp(_castCollider, collisionMask);
                int _amount = _physicsCollider.CastAll(_velocity, out RaycastHit _castHit, _distance, QueryTriggerInteraction.Ignore, !_registerHits, _ignoredColliders);

                // Zero hit - ignore.
                if (_amount == 0)
                    continue;

                _total += _amount;

                // Get closest hit.
                if (_castHit.distance < _hit.distance) {
                    _hit = _castHit;
                }

                // Register hits.
                if (_registerHits) {
                    RegisterCollisionHits(_castCollider, _hitBuffer, _amount);
                }
            }

            return _total;
        }

        public override int Overlap(Movable3D _movable, List<Collider> _buffer, IList<Collider> _ignoredColliders) {

            // Setup.
            List<Collider> _colliders = colliders;
            int _count = _colliders.Count;
            int _total = 0;

            if (_buffer != null) {
                _buffer.Clear();
            }

            // Perform overlap for each colliders.
            for (int i = 0; i < _count; i++) {
                Collider _collider = _colliders[i];

                PhysicsCollider3D _physicsCollider = PhysicsCollider3D.GetTemp(_collider, collisionMask);
                int _amount = _physicsCollider.Overlap(_ignoredColliders, QueryTriggerInteraction.Ignore);

                // Zero overlap - ignore.
                if (_amount == 0)
                    continue;

                _total += _amount;

                // Register overlap.
                if (_buffer != null) {

                    for (int j = 0; j < _amount; j++) {
                        _buffer.Add(PhysicsCollider3D.GetOverlapCollider(j));
                    }
                }
            }

            return _total;
        }

        public override void Extract(Movable3D _movable, IList<Collider> _ignoredColliders) {

            List<Collider> _colliders = colliders;
            int _count = _colliders.Count;

            // Perform extract for each colliders.
            for (int i = 0; i < _count; i++) {
                Collider _collider = _colliders[i];

                PhysicsCollider3D _physicsCollider = PhysicsCollider3D.GetTemp(_collider, collisionMask);
                int _amount = _physicsCollider.Overlap(_ignoredColliders, QueryTriggerInteraction.Ignore);

                // Zero overlap - ignore.
                if (_amount == 0)
                    return;

                // Extract.
                ExtractFromCollider(_movable, _collider, _amount);
            }
        }
        #endregion
    }
}
