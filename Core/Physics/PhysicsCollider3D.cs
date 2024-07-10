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

namespace EnhancedFramework.Physics3D {
    /// <summary>
    /// Physics wrapper for any engine 3D primitive collider.
    /// <br/> Use this to perform precise cast and overlap operations.
    /// </summary>
    [Serializable]
    public sealed class PhysicsCollider3D {
        #region Global Members
        [SerializeField, Enhanced, Required] private Collider collider = null;

        /// <summary>
        /// Default mask used for collision detections.
        /// </summary>
        [NonSerialized] public int CollisionMask = 0;

        private ColliderWrapper3D wrapper = null;

        // -----------------------

        /// <summary>
        /// Wrapped <see cref="UnityEngine.Collider"/> reference.
        /// </summary>
        public Collider Collider {
            get { return collider; }
            set {
                collider = value;
                Initialize();
            }
        }

        /// <summary>
        /// World-space center of the collider bounding box.
        /// </summary>
        public Vector3 Center {
            get { return collider.bounds.center; }
        }

        /// <summary>
        /// World-space non-rotated extents of the collider.
        /// </summary>
        public Vector3 Extents {
            get { return wrapper.GetExtents(); }
        }

        /// <summary>
        /// Contact offset of this collider.
        /// </summary>
        public float ContactOffset {
            get { return collider.contactOffset; }
        }

        // -------------------------------------------
        // Constructor(s)
        // -------------------------------------------

        /// <inheritdoc cref="PhysicsCollider3D"/>
        public PhysicsCollider3D() { }

        /// <inheritdoc cref="PhysicsCollider3D(Collider, int)"/>
        public PhysicsCollider3D(Collider _collider) : this(_collider, Physics3DUtility.GetLayerCollisionMask(_collider.gameObject)) { }

        /// <param name="_collider"><see cref="UnityEngine.Collider"/> to create a <see cref="PhysicsCollider3D"/> for.</param>
        /// <param name="_collisionMask">Optional collision mask of this collider.</param>
        /// <returns>New <see cref="PhysicsCollider3D"/> instance for this collider.</returns>
        /// <inheritdoc cref="PhysicsCollider3D"/>
        public PhysicsCollider3D(Collider _collider, int _collisionMask) {
            collider = _collider;
            wrapper  = ColliderWrapper3D.Get(_collider);

            Initialize(_collisionMask);
        }
        #endregion

        #region Initialization
        /// <inheritdoc cref="Initialize(int)"/>
        public void Initialize() {
            int _layer = Physics3DUtility.GetLayerCollisionMask(collider.gameObject);
            Initialize(_layer);
        }

        /// <summary>
        /// Initializes this <see cref="PhysicsCollider3D"/>.
        /// <br/> Should be called before any use, preferably during <see cref="EnhancedBehaviour.OnInit"/>.
        /// </summary>
        /// <param name="_collisionMask">Default mask to be used for collider collision detections.</param>
        public void Initialize(int _collisionMask) {
            CollisionMask = _collisionMask;
            wrapper       = ColliderWrapper3D.Create(collider);
        }
        #endregion

        #region Overlap
        private const QueryTriggerInteraction OverlapDefaultQueryTrigger = QueryTriggerInteraction.Collide;
        private static Collider[] overlapBuffer = new Collider[16];

        // -------------------------------------------
        // Overlap
        // -------------------------------------------

        /// <inheritdoc cref="Overlap(int, QueryTriggerInteraction)"/>
        public int Overlap(QueryTriggerInteraction _triggerInteraction = OverlapDefaultQueryTrigger) {
            return Overlap(CollisionMask, _triggerInteraction);
        }

        /// <inheritdoc cref="Overlap(IList{Collider}, int, QueryTriggerInteraction)"/>
        public int Overlap(int _mask, QueryTriggerInteraction _triggerInteraction = OverlapDefaultQueryTrigger) {
            int amount = wrapper.Overlap(overlapBuffer, _mask, _triggerInteraction);
            int size   = overlapBuffer.Length;

            if (amount == size) {
                int newSize = overlapBuffer.Length * 2;
                Debug.LogWarning($"Maximum Overlap detection limit reached! Increasing buffer size from {size} to {newSize}");

                Array.Resize(ref overlapBuffer, newSize);
            }

            return amount;
        }

        /// <inheritdoc cref="Overlap(IList{Collider}, int, QueryTriggerInteraction)"/>
        public int Overlap(IList<Collider> _ignoredColliders, QueryTriggerInteraction _triggerInteraction = OverlapDefaultQueryTrigger) {
            return Overlap(_ignoredColliders, CollisionMask, _triggerInteraction);
        }

        /// <summary>
        /// Get detailed informations about the current overlapping colliders.
        /// <para/>
        /// Note that this collider itself may be found depending on the used detection mask.
        /// </summary>
        /// <param name="_ignoredColliders">Colliders to ignore.</param>
        /// <param name="_mask"><see cref="LayerMask"/> to use for detection.</param>
        /// <param name="_triggerInteraction">Determines if triggers should be detected.</param>
        /// <returns>Total amount of overlapping colliders.</returns>
        public int Overlap(IList<Collider> _ignoredColliders, int _mask, QueryTriggerInteraction _triggerInteraction = OverlapDefaultQueryTrigger) {
            int _amount = Overlap(_mask, _triggerInteraction);

            if ((_ignoredColliders != null) && (_ignoredColliders.Count != 0)) {
                for (int i = 0; i < _amount; i++) {

                    if (_ignoredColliders.Contains(overlapBuffer[i])) {
                        overlapBuffer[i] = overlapBuffer[--_amount];
                        i--;
                    }
                }
            }

            return _amount;
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        /// <summary>
        /// Get the overlapping collider at a given index.
        /// <br/> Note that the last overlap is from the whole game loop, not specific to this collider.
        /// <para/>
        /// Use <see cref="Overlap(IList{Collider}, int, QueryTriggerInteraction)"/> to get the total count of overlapping colliders.
        /// </summary>
        /// <param name="_index">Index to get the collider at.</param>
        /// <returns>The overlapping collider at the given index.</returns>
        public static Collider GetOverlapCollider(int _index) {
            return overlapBuffer[_index];
        }

        /// <summary>
        /// Sorts overlapping colliders by distance, using a specific <see cref="Vector3"/> reference position.
        /// </summary>
        /// <inheritdoc cref="Physics3DUtility.SortCollidersByDistance(Collider[], int, Vector3)"/>
        public static void SortOverlapCollidersByDistance(int _count, Vector3 _reference) {
            Physics3DUtility.SortCollidersByDistance(overlapBuffer, _count, _reference);
        }
        #endregion

        #region Raycast
        private const QueryTriggerInteraction RaycastDefaultQueryTrigger = QueryTriggerInteraction.Ignore;

        // -----------------------

        /// <inheritdoc cref="Raycast(Vector3, out RaycastHit, float, int, QueryTriggerInteraction)"/>
        public bool Raycast(Vector3 _velocity, out RaycastHit _hit, QueryTriggerInteraction _triggerInteraction = RaycastDefaultQueryTrigger) {
            return Raycast(_velocity, out _hit, CollisionMask, _triggerInteraction);
        }

        /// <inheritdoc cref="Raycast(Vector3, out RaycastHit, float, int, QueryTriggerInteraction)"/>
        public bool Raycast(Vector3 _velocity, out RaycastHit _hit, int _mask, QueryTriggerInteraction _triggerInteraction = RaycastDefaultQueryTrigger) {
            float _distance = _velocity.magnitude;
            return Raycast(_velocity, out _hit, _distance, _mask, _triggerInteraction);
        }

        /// <inheritdoc cref="Raycast(Vector3, out RaycastHit, float, int, QueryTriggerInteraction)"/>
        public bool Raycast(Vector3 _direction, out RaycastHit _hit, float _distance, QueryTriggerInteraction _triggerInteraction = RaycastDefaultQueryTrigger) {
            return Raycast(_direction, out _hit, _distance, CollisionMask, _triggerInteraction);
        }

        /// <summary>
        /// Performs a raycasts from this collider in a given direction.
        /// </summary>
        /// <param name="_direction">Raycast direction.</param>
        /// <param name="_hit">Detailed informations on raycast hit.</param>
        /// <param name="_distance">Maximum raycast distance.</param>
        /// <param name="_mask"><see cref="LayerMask"/> used for collisions detection.</param>
        /// <param name="_triggerInteraction">Determines if triggers should be detected.</param>
        /// <returns>True if the raycast hit something, false otherwise.</returns>
        public bool Raycast(Vector3 _direction, out RaycastHit _hit, float _distance, int _mask, QueryTriggerInteraction _triggerInteraction = RaycastDefaultQueryTrigger) {
            return wrapper.Raycast(_direction, out _hit, _distance, _mask, _triggerInteraction);
        }
        #endregion

        #region Cast
        /// <summary>
        /// Maximum distance when compared to the first hit of a cast, to be considered as valid.
        /// </summary>
        public const float MaxCastDifferenceDetection = .001f;
        private const QueryTriggerInteraction CastDefaultQueryTrigger = QueryTriggerInteraction.Ignore;

        internal static readonly RaycastHit[] castBuffer = new RaycastHit[8];

        // -------------------------------------------
        // Cast
        // -------------------------------------------

        /// <param name="_velocity"><inheritdoc cref="CastAll(Vector3, out RaycastHit, QueryTriggerInteraction, IList{Collider})" path="/param[@name='_velocity']"/></param>
        /// <inheritdoc cref="Cast(Vector3, out RaycastHit, float, int, QueryTriggerInteraction, bool, IList{Collider})"/>
        public bool Cast(Vector3 _velocity, out float _distance, QueryTriggerInteraction _triggerInteraction = CastDefaultQueryTrigger, bool ignoreFurtherHits = true,
                         IList<Collider> _ignoredColliders = null) {
            bool _doHit = Cast(_velocity, out RaycastHit _hit, CollisionMask, _triggerInteraction, ignoreFurtherHits, _ignoredColliders);

            _distance = _hit.distance;
            return _doHit;
        }

        /// <param name="_velocity"><inheritdoc cref="CastAll(Vector3, out RaycastHit, QueryTriggerInteraction, IList{Collider})" path="/param[@name='_velocity']"/></param>
        /// <inheritdoc cref="Cast(Vector3, out RaycastHit, float, int, QueryTriggerInteraction, bool, IList{Collider})"/>
        public bool Cast(Vector3 _velocity, out RaycastHit _hit, QueryTriggerInteraction _triggerInteraction = CastDefaultQueryTrigger, bool ignoreFurtherHits = true,
                         IList<Collider> _ignoredColliders = null) {
            return Cast(_velocity, out _hit, CollisionMask, _triggerInteraction, ignoreFurtherHits, _ignoredColliders);
        }

        /// <param name="_velocity"><inheritdoc cref="CastAll(Vector3, out RaycastHit, QueryTriggerInteraction, IList{Collider})" path="/param[@name='_velocity']"/></param>
        /// <inheritdoc cref="Cast(Vector3, out RaycastHit, float, int, QueryTriggerInteraction, bool, IList{Collider})"/>
        public bool Cast(Vector3 _velocity, out RaycastHit _hit, int _mask, QueryTriggerInteraction _triggerInteraction = CastDefaultQueryTrigger, bool ignoreFurtherHits = true,
                         IList<Collider> _ignoredColliders = null) {
            float _distance = _velocity.magnitude;
            return Cast(_velocity, out _hit, _distance, _mask, _triggerInteraction, ignoreFurtherHits, _ignoredColliders);
        }

        /// <inheritdoc cref="Cast(Vector3, out RaycastHit, float, int, QueryTriggerInteraction, bool, IList{Collider})"/>
        public bool Cast(Vector3 _direction, out RaycastHit _hit, float _distance, QueryTriggerInteraction _triggerInteraction = CastDefaultQueryTrigger, bool ignoreFurtherHits = true,
                         IList<Collider> _ignoredColliders = null) {
            return Cast(_direction, out _hit, _distance, CollisionMask, _triggerInteraction, ignoreFurtherHits, _ignoredColliders);
        }

        /// <returns>True if this collider hit something, false otherwise.</returns>
        /// <inheritdoc cref="CastAll(Vector3, out RaycastHit, float, int, QueryTriggerInteraction, bool, IList{Collider})"/>
        public bool Cast(Vector3 _direction, out RaycastHit _hit, float _distance, int _mask, QueryTriggerInteraction _triggerInteraction = CastDefaultQueryTrigger, bool ignoreFurtherHits = true,
                         IList<Collider> _ignoredColliders = null) {
            return CastAll(_direction, out _hit, _distance, _mask, _triggerInteraction, ignoreFurtherHits, _ignoredColliders) != 0;
        }

        // -------------------------------------------
        // Cast All
        // -------------------------------------------

        /// <param name="_velocity"><inheritdoc cref="CastAll(Vector3, out RaycastHit, QueryTriggerInteraction, IList{Collider})" path="/param[@name='_velocity']"/></param>
        /// <inheritdoc cref="CastAll(Vector3, out RaycastHit, float, int, QueryTriggerInteraction, bool, IList{Collider})"/>
        public int CastAll(Vector3 _velocity, out float _distance, QueryTriggerInteraction _triggerInteraction = CastDefaultQueryTrigger, bool ignoreFurtherHits = true,
                           IList<Collider> _ignoredColliders = null) {
            int _amount = CastAll(_velocity, out RaycastHit _hit, _triggerInteraction, ignoreFurtherHits, _ignoredColliders);

            _distance = _hit.distance;
            return _amount;
        }

        /// <param name="_velocity">Velocity used to perform this cast.</param>
        /// <inheritdoc cref="CastAll(Vector3, out RaycastHit, float, int, QueryTriggerInteraction, bool, IList{Collider})"/>
        public int CastAll(Vector3 _velocity, out RaycastHit _hit, QueryTriggerInteraction _triggerInteraction = CastDefaultQueryTrigger, bool ignoreFurtherHits = true,
                           IList<Collider> _ignoredColliders = null) {
            float _distance = _velocity.magnitude;
            return CastAll(_velocity, out _hit, _distance, CollisionMask, _triggerInteraction, ignoreFurtherHits, _ignoredColliders);
        }

        /// <inheritdoc cref="CastAll(Vector3, out RaycastHit, float, int, QueryTriggerInteraction, bool, IList{Collider})"/>
        public int CastAll(Vector3 _direction, out RaycastHit _hit, float _distance, QueryTriggerInteraction _triggerInteraction = CastDefaultQueryTrigger, bool ignoreFurtherHits = true,
                           IList<Collider> _ignoredColliders = null) {
            return CastAll(_direction, out _hit, _distance,  CollisionMask, _triggerInteraction, ignoreFurtherHits, _ignoredColliders);
        }

        /// <summary>
        /// Performs a cast from this collider in a given direction.
        /// </summary>
        /// <param name="_direction">Cast direction.</param>
        /// <param name="_hit">Main trajectory hit detailed informations.</param>
        /// <param name="_distance">Maximum cast distance.</param>
        /// <param name="_mask"><see cref="LayerMask"/> used for collisions detection.</param>
        /// <param name="_triggerInteraction">Determines if triggers should be detected.</param>
        /// <param name="ignoreFurtherHits">If true, ignore all hits with a greater distance than to the closest one.</param>
        /// <param name="_ignoredColliders">Colliders to ignore.</param>
        /// <returns>Total amount of consistent hits on the trajectory.</returns>
        public int CastAll(Vector3 _direction, out RaycastHit _hit, float _distance, int _mask, QueryTriggerInteraction _triggerInteraction = CastDefaultQueryTrigger, bool ignoreFurtherHits = true,
                           IList<Collider> _ignoredColliders = null) {
            // Distance security.
            float _contactOffset = ContactOffset;
            _distance += _contactOffset * 2f;

            int _amount = wrapper.Cast(_direction, castBuffer, _distance, _mask, _triggerInteraction);

            if (_amount > 0) {
                if ((_ignoredColliders == null) || (_ignoredColliders.Count == 0)) {
                    // Remove this object collider if detected.
                    if (castBuffer[_amount - 1].collider == collider) {
                        _amount--;
                        if (_amount == 0) {
                            _hit = GetDefaultHit();
                            return 0;
                        }
                    }

                    #if DEVELOPMENT
                    // Debug utility. Should be remove at some point.
                    for (int i = 0; i < _amount; i++) {
                        if (castBuffer[i].collider == collider) {
                            collider.LogError($"This object collider found => {i}/{_amount}");
                        }
                    }
                    #endif
                } else {
                    // Ignored colliders.
                    for (int i = 0; i < _amount; i++) {
                        if (_ignoredColliders.Contains(castBuffer[i].collider)) {
                            castBuffer[i] = castBuffer[--_amount];
                            i--;
                        }
                    }
                }

                Physics3DUtility.SortRaycastHitByDistance(castBuffer, _amount);

                _hit = castBuffer[0];
                _hit.distance = Mathf.Max(0f, _hit.distance - _contactOffset);

                // Ignore hits that are too distants from the closest one.
                if (ignoreFurtherHits) {

                    for (int i = 1; i < _amount; i++) {
                        if (castBuffer[i].distance > (_hit.distance + MaxCastDifferenceDetection))
                            return i;
                    }
                }
            } else {
                // No hit, so get full distance.
                _hit = GetDefaultHit();
            }

            return _amount;

            // ----- Local Method ----- \\

            RaycastHit GetDefaultHit() {
                return new RaycastHit {
                    distance = _distance - _contactOffset
                };
            }
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        /// <summary>
        /// Get detailed informations from the last cast at a given index.
        /// <br/> Note that the last cast is from the whole game loop, not specific to this collider.
        /// </summary>
        /// <param name="_index">Index to get the hit at.</param>
        /// <returns>Detailed informations about the hit at the given index.</returns>
        public static RaycastHit GetCastHit(int _index) {
            return castBuffer[_index];
        }

        /// <summary>
        /// Sorts detected hits by distance.
        /// </summary>
        /// <inheritdoc cref="Physics3DUtility.SortRaycastHitByDistance(RaycastHit[], int)"/>
        public static void SortCastHitByDistance(int _count) {
            Physics3DUtility.SortRaycastHitByDistance(castBuffer, _count);
        }
        #endregion

        #region Utility
        private static readonly PhysicsCollider3D sharedInstance = new PhysicsCollider3D();

        // -----------------------

        /// <inheritdoc cref="GetTemp(Collider, int)"/>
        public static PhysicsCollider3D GetTemp(Collider collider) {
            int layer = Physics3DUtility.GetLayerCollisionMask(collider.gameObject);
            return GetTemp(collider, layer);
        }

        /// <summary>
        /// Get a temporary <see cref="PhysicsCollider3D"/> configured for a specific <see cref="UnityEngine.Collider"/>.
        /// </summary>
        /// <param name="_collider"><see cref="UnityEngine.Collider"/> to get a <see cref="PhysicsCollider3D"/> for.</param>
        /// <param name="_collisionMask">Optional collision mask of this collider.</param>
        /// <returns>Temporary <see cref="PhysicsCollider3D"/> for this collider.</returns>
        public static PhysicsCollider3D GetTemp(Collider _collider, int _collisionMask) {
            return sharedInstance.Setup(_collider, _collisionMask);
        }

        /// <inheritdoc cref="ColliderWrapper3D.SetBounds"/>
        public void SetBounds(Vector3 _center, Vector3 _size) {
            wrapper.SetBounds(_center, _size);
        }


        /// <inheritdoc cref="ColliderWrapper3D.GetBounds"/>
        public Bounds GetBounds() {
            return wrapper.GetBounds();
        }

        // -----------------------

        private PhysicsCollider3D Setup(Collider _collider, int _collisionMask) {
            collider      = _collider;
            CollisionMask = _collisionMask;
            wrapper       = ColliderWrapper3D.Get(_collider);

            return this;
        }
        #endregion
    }
}
