// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

#if UNITY_2022_2_OR_NEWER
#define OVERLAP_COMMANDS
#endif

using EnhancedEditor;
using EnhancedFramework.Core;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        // -------------------------------------------
        // Manual
        // -------------------------------------------

        /// <summary>
        /// Performs a cast for a given <see cref="Movable3D"/>.
        /// </summary>
        /// <param name="_movable"><see cref="Movable3D"/> instance instigator of this cast.</param>
        /// <param name="_operation">Data wrapper of this cast operation.</param>
        /// <param name="_velocity">Velocity to use to perform this cast.</param>
        /// <param name="_distance">Max distance of this cast operation.</param>
        /// <param name="_hit">Main <see cref="RaycastHit"/> result.</param>
        /// <param name="_registerHits">Whether to register or not hit results.</param>
        /// <returns>Total amount of hit results.</returns>
        public abstract int Cast(Movable3D _movable, CollisionOperationData3D _operation, Vector3 _velocity, float _distance, out RaycastHit _hit, bool _registerHits);

        /// <summary>
        /// Performs an overlap for a given <see cref="Movable3D"/>.
        /// </summary>
        /// <param name="_movable"><see cref="Movable3D"/> instance instigator of this overlap.</param>
        /// <param name="_buffer">Buffer used to store overlap results.</param>
        /// <param name="_ignoredColliders">All <see cref="Collider"/> to ignore.</param>
        /// <returns>Total count of overlapping colliders.</returns>
        public abstract int Overlap(Movable3D _movable, List<Collider> _buffer, IList<Collider> _ignoredColliders);

        /// <summary>
        /// Performs an extract operation for a given <see cref="Movable3D"/>.
        /// </summary>
        /// <param name="_movable"><see cref="Movable3D"/> instance to extract.</param>
        /// <param name="_ignoredColliders">All <see cref="Collider"/> to ignore.</param>
        public abstract void Extract(Movable3D _movable, IList<Collider> _ignoredColliders);

        // -------------------------------------------
        // Commands
        // -------------------------------------------

        /// <summary>
        /// Registers a cast command for a given <see cref="Movable3D"/>
        /// </summary>
        public abstract void RegisterCastCommand(Movable3D _movable, CollisionOperationData3D _operation, CastOperationCommands3D _commands, Vector3 _velocity);

        /// <summary>
        /// Registers an overlap command for a given <see cref="Movable3D"/>
        /// </summary>
        public abstract void RegisterOverlapCommand(Movable3D _movable, OverlapOperationCommands3D _commands);

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        internal static void RegisterCollisionHits(CollisionOperationData3D _operation, Collider _collider, RaycastHit[] _hits, int _startIndex, int _count) {

            EnhancedCollection<CollisionHit3D> _hitBuffer = _operation.Data.InternalTempBuffer;
            _operation.Data.TempBufferOperationCount++;

            // Early return - only get first hit.
            if (_operation.PhysicsSettings.CollisionOneHitIfNoEffect && !_operation.MovableInstance.CanApplyCollisionEffect(_operation)) {

                if (_count != 0) {
                    CollisionHit3D _collisionHit = new CollisionHit3D(_hits[_startIndex], _collider, false);
                    _hitBuffer.Add(_collisionHit);
                }

                return;
            }

            // Store informations about all Movable-related hits - used later to apply special effects and for callbacks.
            for (int i = 0; i < _count; i++) {

                RaycastHit _hit = _hits[_startIndex + i];

                // In case the same object is hit multiple times, only keep the closest hit.
                if (FindIndex(_hit.collider, out int _index) && (_hit.distance > _hitBuffer[_index].Distance))
                    continue;

                // Register hit.
                CollisionHit3D _collisionHit = new CollisionHit3D(_hit, _collider, true);

                if (_index == -1) {
                    _hitBuffer.Add(_collisionHit);
                } else {
                    _hitBuffer[_index] = _collisionHit;
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
        // -------------------------------------------
        // Manual
        // -------------------------------------------

        public override int Cast    (Movable3D _movable, CollisionOperationData3D _operation, Vector3 _velocity, float _distance, out RaycastHit _hit, bool _registerHits) {
            // Setup.
            PhysicsCollider3D _physicsCollider = _movable.PhysicsCollider;

            // Perform cast.
            int _amount = _physicsCollider.CastAll(_velocity, out _hit, _distance, QueryTriggerInteraction.Ignore, !_registerHits, _operation.SelfColliders);

            // Register hits.
            if (_registerHits) {
                RegisterCollisionHits(_operation, _physicsCollider.Collider, PhysicsCollider3D.castBuffer, 0, _amount);
            }

            return _amount;
        }

        public override int Overlap (Movable3D _movable, List<Collider> _buffer, IList<Collider> _ignoredColliders) {
            // Setup.
            PhysicsCollider3D _physicsCollider = _movable.PhysicsCollider;

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
            PhysicsCollider3D _physicsCollider = _movable.PhysicsCollider;

            // Perform overlap.
            int _amount = _physicsCollider.Overlap(_ignoredColliders, QueryTriggerInteraction.Ignore);

            // Extract while authorized.
            for (int i = 0; i < _amount; i++) {
                if (!_movable.ExtractFromCollider(_physicsCollider.Collider, PhysicsCollider3D.GetOverlapCollider(i), false))
                    break;
            }
        }

        // -------------------------------------------
        // Commands
        // -------------------------------------------

        public override void RegisterCastCommand   (Movable3D _movable, CollisionOperationData3D _operation, CastOperationCommands3D _commands, Vector3 _velocity) {
            _commands.RegisterCommand(_movable, _movable.Collider, _velocity, _velocity.magnitude, _movable.GetColliderMask());
        }

        public override void RegisterOverlapCommand(Movable3D _movable, OverlapOperationCommands3D _commands) {
            #if OVERLAP_COMMANDS
            _commands.RegisterCommand(_movable, _movable.Collider, _movable.GetColliderMask());
            #else
            _movable.LogErrorMessage("Overlap commands are only available in Unity version 2022.2 and above");
            #endif
        }
        #endregion
    }

    /// <summary>
    /// <see cref="Movable3DPhysicsWrapper{T}"/> performing collisions with multiple colliders on the same object.
    /// </summary>
    internal sealed class MultiColliderMovable3DPhysicsWrapper : Movable3DPhysicsWrapper<MultiColliderMovable3DPhysicsWrapper> {
        #region Content
        // -------------------------------------------
        // Manual
        // -------------------------------------------
        
        public override int Cast    (Movable3D _movable, CollisionOperationData3D _operation, Vector3 _velocity, float _distance, out RaycastHit _hit, bool _registerHits) {
            // Setup.
            List<Collider> _colliders = _operation.SelfColliders;
            int _count = _colliders.Count;
            int _total = 0;

            int _collisionMask = _movable.GetColliderMask();
            _hit = new RaycastHit() { distance = int.MaxValue };

            // Perform cast for each colliders.
            for (int i = 0; i < _count; i++) {

                Collider _collider = _colliders[i];
                if (!IsValid(_collider))
                    continue;

                PhysicsCollider3D _physicsCollider = PhysicsCollider3D.GetTemp(_collider, _collisionMask);
                int _amount = _physicsCollider.CastAll(_velocity, out RaycastHit _castHit, _distance, QueryTriggerInteraction.Ignore, !_registerHits, _colliders);

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
                    RegisterCollisionHits(_operation, _collider, PhysicsCollider3D.castBuffer, 0, _amount);
                }
            }

            return _total;
        }

        public override int Overlap (Movable3D _movable, List<Collider> _buffer, IList<Collider> _ignoredColliders) {

            // Setup.
            List<Collider> _colliders = _movable.SelfColliders;
            int _count = _colliders.Count;
            int _total = 0;

            int _collisionMask = _movable.GetColliderMask();
            if (_buffer != null) {
                _buffer.Clear();
            }

            // Perform overlap for each colliders.
            for (int i = 0; i < _count; i++) {

                Collider _collider = _colliders[i];
                if (!IsValid(_collider))
                    continue;

                PhysicsCollider3D _physicsCollider = PhysicsCollider3D.GetTemp(_collider, _collisionMask);
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

            List<Collider> _colliders = _movable.SelfColliders;
            int _count = _colliders.Count;

            int _collisionMask = _movable.GetColliderMask();

            // Perform extract for each colliders.
            for (int i = 0; i < _count; i++) {

                Collider _collider = _colliders[i];
                if (!IsValid(_collider))
                    continue;

                PhysicsCollider3D _physicsCollider = PhysicsCollider3D.GetTemp(_collider, _collisionMask);
                int _amount = _physicsCollider.Overlap(_ignoredColliders, QueryTriggerInteraction.Ignore);

                // Extract while authorized.
                for (int j = 0; j < _amount; j++) {
                    if (!_movable.ExtractFromCollider(_physicsCollider.Collider, PhysicsCollider3D.GetOverlapCollider(j), false))
                        return;
                }
            }
        }

        // -------------------------------------------
        // Commands
        // -------------------------------------------

        public override void RegisterCastCommand   (Movable3D _movable, CollisionOperationData3D _operation, CastOperationCommands3D _commands, Vector3 _velocity) {
            List<Collider> _selfColliders = _operation.SelfColliders;

            int _collisionMask = _movable.GetColliderMask();
            float _distance    = _velocity.magnitude;

            int _count = _selfColliders.Count;
            for (int i = 0; i < _count; i++) {

                Collider _collider = _selfColliders[i];
                if (!IsValid(_collider))
                    continue;

                _commands.RegisterCommand(_movable, _collider, _velocity, _distance, _collisionMask);
            }
        }

        public override void RegisterOverlapCommand(Movable3D _movable, OverlapOperationCommands3D _commands) {
            #if OVERLAP_COMMANDS
            List<Collider> _selfColliders = _movable.SelfColliders;
            int _collisionMask = _movable.GetColliderMask();

            int _count = _selfColliders.Count;
            for (int i = 0; i < _count; i++) {

                Collider _collider = _selfColliders[i];
                if (!IsValid(_collider))
                    continue;

                _commands.RegisterCommand(_movable, _collider, _collisionMask);
            }
            #else
            _movable.LogErrorMessage("Overlap commands are only available in Unity version 2022.2 and above");
            #endif
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValid(Collider _collider) {
            return !_collider.isTrigger && _collider.enabled;
        }
        #endregion
    }
}
