// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using EnhancedEditor;
using System;
using UnityEngine;

namespace EnhancedFramework.Physics3D {
    /// <summary>
    /// Custom physics wrapper for all engine 3D primitive colliders.
    /// <br/> Use this to perform precise cast and overlap operations.
    /// </summary>
    [Serializable]
    public class PhysicsCollider3D {
        /// <summary>
        /// Maximum distance compared to the first hit of the cast to be considered as a valid.
        /// </summary>
        public const float MaxCastDifferenceDetection = .001f;

        /// <summary>
        /// Minimum distance to be used for collider casts.
        /// </summary>
        public const float MinCastDistance = .0001f;

        #region Global Members
        [SerializeField, Enhanced, Required] private Collider collider = null;
        private ColliderWrapper3D wrapper = null;

        /// <summary>
        /// Default mask used for collision detections.
        /// </summary>
        public int CollisionMask { get; set; } = 0;

        /// <summary>
        /// The wrapped <see cref="Collider"/> reference.
        /// </summary>
        public Collider Collider {
            get => collider;
            set {
                collider = value;
                Initialize();
            }
        }

        // -----------------------

        /// <summary>
        /// World-space collider bounding box center.
        /// </summary>
        public Vector3 Center => collider.bounds.center;

        /// <summary>
        /// World-space non-rotated collider extents.
        /// </summary>
        public Vector3 Extents => wrapper.GetExtents();
        #endregion

        #region Initialization
        /// <inheritdoc cref="Initialize(int)"/>
        public void Initialize() {
            int _layer = Physics3DUtility.GetLayerCollisionMask(collider.gameObject);
            Initialize(_layer);
        }

        /// <summary>
        /// Initializes this <see cref="PhysicsCollider3D"/>.
        /// <br/> Always call this before any use, preferably on Start or on Awake.
        /// </summary>
        /// <param name="_collisionMask">Default mask to be used for collider collision detections.</param>
        public void Initialize(int _collisionMask) {
            CollisionMask = _collisionMask;
            wrapper = ColliderWrapper3D.CreateWrapper(collider);
        }
        #endregion

        #region Bounds
        /// <summary>
        /// Modifies the bounds of this collider.
        /// </summary>
        /// <param name="_boundsCenter">New collider bounds center (measured in the object local space).</param>
        /// <param name="_boundsSize">New collider bounds size (measured in the object local space).</param>
        public void ModifyBounds(Vector3 _boundsCenter, Vector3 _boundsSize) {
            switch (collider) {
                case BoxCollider _box:
                    _box.center = _boundsCenter;
                    _box.size = _boundsSize;
                    break;

                case CapsuleCollider _capsule:
                    _capsule.center = _boundsCenter;

                    _capsule.radius = _boundsSize.x;
                    _capsule.height = _boundsSize.y;
                    break;

                case SphereCollider _sphere:
                    _sphere.center = _boundsCenter;
                    _sphere.radius = _boundsSize.x;
                    break;

                default:
                    collider.LogWarning("This collider is not of a primitive type, and as so its bounds cannot be modified!");
                    break;
            }
        }
        #endregion

        #region Raycast
        /// <inheritdoc cref="Raycast(Vector3, out RaycastHit, float, int, QueryTriggerInteraction)"/>
        public bool Raycast(Vector3 _velocity, out RaycastHit _hit, QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore) {
            float _distance = _velocity.magnitude;
            bool _doHit = Raycast(_velocity, out _hit, _distance, CollisionMask, _triggerInteraction);

            return _doHit;
        }

        /// <inheritdoc cref="Raycast(Vector3, out RaycastHit, float, int, QueryTriggerInteraction)"/>
        public bool Raycast(Vector3 _velocity, out RaycastHit _hit, int _mask, QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore) {
            float _distance = _velocity.magnitude;
            bool _doHit = Raycast(_velocity, out _hit, _distance, _mask, _triggerInteraction);

            return _doHit;
        }

        /// <inheritdoc cref="Raycast(Vector3, out RaycastHit, float, int, QueryTriggerInteraction)"/>
        public bool Raycast(Vector3 _direction, out RaycastHit _hit, float _distance, QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore) {
            bool _doHit = Raycast(_direction, out _hit, _distance, CollisionMask, _triggerInteraction);
            return _doHit;
        }

        /// <summary>
        /// Performs a raycasts from this collider in a given direction.
        /// </summary>
        /// <param name="_direction">Raycast direction.</param>
        /// <param name="_hit">Detailed informations on raycast hit.</param>
        /// <param name="_distance">Maximum raycast distance.</param>
        /// <param name="_mask"><see cref="LayerMask"/> to use for collisions detection.</param>
        /// <param name="_triggerInteraction">How should the raycast interact with triggers?</param>
        /// <returns>True if the raycast hit something on the way, false otherwise.</returns>
        public bool Raycast(Vector3 _direction, out RaycastHit _hit, float _distance, int _mask, QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore) {
            bool _doHit = wrapper.Raycast(_direction, out _hit, _distance, _mask, _triggerInteraction);
            return _doHit;
        }
        #endregion

        #region Cast
        private static readonly RaycastHit[] castBuffer = new RaycastHit[8];

        /// <summary>
        /// Get detailed hit informations from the last cast at a specific index.
        /// <br/> Note that the last cast is from the whole game loop, not specific to this collider.
        /// </summary>
        /// <param name="_index">Index at which to get detailed hit informations.
        /// <br/> Should be used in association with informations from the cast method.</param>
        /// <returns>Details informations about the hit at the specified index from the last cast.</returns>
        public static RaycastHit GetCastHit(int _index) {
            return castBuffer[_index];
        }

        // -----------------------

        /// <param name="_velocity"><inheritdoc cref="CastAll(Vector3, out RaycastHit, QueryTriggerInteraction)" path="/param[@name='_velocity']"/></param>
        /// <inheritdoc cref="Cast(Vector3, out RaycastHit, float, int, QueryTriggerInteraction)"/>
        public bool Cast(Vector3 _velocity, out float _distance, QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore,
                         Collider[] _ignoredColliders = null) {
            _distance = _velocity.magnitude;
            bool _doHit = Cast(_velocity, out RaycastHit _hit, _distance, CollisionMask, _triggerInteraction, _ignoredColliders);

            _distance = _hit.distance;
            return _doHit;
        }

        /// <param name="_velocity"><inheritdoc cref="CastAll(Vector3, out RaycastHit, QueryTriggerInteraction)" path="/param[@name='_velocity']"/></param>
        /// <inheritdoc cref="Cast(Vector3, out RaycastHit, float, int, QueryTriggerInteraction)"/>
        public bool Cast(Vector3 _velocity, out RaycastHit _hit, QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore,
                         Collider[] _ignoredColliders = null) {
            float _distance = _velocity.magnitude;
            return Cast(_velocity, out _hit, _distance, _triggerInteraction, _ignoredColliders);
        }

        /// <param name="_velocity"><inheritdoc cref="CastAll(Vector3, out RaycastHit, QueryTriggerInteraction)" path="/param[@name='_velocity']"/></param>
        /// <inheritdoc cref="Cast(Vector3, out RaycastHit, float, int, QueryTriggerInteraction)"/>
        public bool Cast(Vector3 _velocity, out RaycastHit _hit, int _mask, QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore,
                         Collider[] _ignoredColliders = null) {
            float _distance = _velocity.magnitude;
            return Cast(_velocity, out _hit, _distance, _mask, _triggerInteraction, _ignoredColliders);
        }

        /// <inheritdoc cref="Cast(Vector3, out RaycastHit, float, int, QueryTriggerInteraction)"/>
        public bool Cast(Vector3 _direction, out RaycastHit _hit, float _distance, QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore,
                         Collider[] _ignoredColliders = null) {
            return Cast(_direction, out _hit, _distance, CollisionMask, _triggerInteraction, _ignoredColliders);
        }

        /// <summary>
        /// Performs a cast from this collider in a given direction,
        /// and indicates if it hit something on the way.
        /// </summary>
        /// <returns>True if this collider hit something on the way, false otherwise.</returns>
        /// <inheritdoc cref="CastAll(Vector3, out RaycastHit, float, int, QueryTriggerInteraction, Collider[])"/>
        public bool Cast(Vector3 _direction, out RaycastHit _hit, float _distance, int _mask, QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore,
                         Collider[] _ignoredColliders = null) {
            return CastAll(_direction, out _hit, _distance, _mask, _triggerInteraction, _ignoredColliders) > 0;
        }

        /// <param name="_velocity"><inheritdoc cref="CastAll(Vector3, out RaycastHit, QueryTriggerInteraction)" path="/param[@name='_velocity']"/></param>
        /// <inheritdoc cref="CastAll(Vector3, out RaycastHit, float, int, QueryTriggerInteraction)"/>
        public int CastAll(Vector3 _velocity, out float _distance, QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore,
                           Collider[] _ignoredColliders = null) {
            _distance = _velocity.magnitude;
            int _amount = CastAll(_velocity, out RaycastHit _hit, _distance, CollisionMask, _triggerInteraction, _ignoredColliders);

            _distance = _hit.distance;
            return _amount;
        }

        /// <param name="_velocity"><inheritdoc cref="CastAll(Vector3, out RaycastHit, QueryTriggerInteraction)" path="/param[@name='_velocity']"/></param>
        /// <inheritdoc cref="CastAll(Vector3, out RaycastHit, float, int, QueryTriggerInteraction)"/>
        public int CastAll(Vector3 _velocity, out RaycastHit _hit, QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore,
                           Collider[] _ignoredColliders = null) {
            float _distance = _velocity.magnitude;
            return CastAll(_velocity, out _hit, _distance, CollisionMask, _triggerInteraction, _ignoredColliders);
        }

        /// <inheritdoc cref="CastAll(Vector3, out RaycastHit, float, int, QueryTriggerInteraction)"/>
        public int CastAll(Vector3 _direction, out RaycastHit _hit, float _distance, QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore,
                           Collider[] _ignoredColliders = null) {
            return CastAll(_direction, out _hit, _distance,  CollisionMask, _triggerInteraction, _ignoredColliders);
        }

        /// <summary>
        /// Performs a cast from this collider in a given direction.
        /// <br/> Indicates the trajectory consistent hits amount,
        /// and give detailed informations about the main one.
        /// </summary>
        /// <param name="_direction">Cast direction.</param>
        /// <param name="_hit">Main trajectory hit detailed informations.</param>
        /// <param name="_distance">Maximum cast distance.</param>
        /// <param name="_mask"><see cref="LayerMask"/> to use for collisions detection.</param>
        /// <param name="_triggerInteraction">How should the cast interact with triggers?</param>
        /// <param name="_ignoredColliders">Colliders to ignore during cast.</param>
        /// <returns>The total amount of consistent hit during this trajectory.</returns>
        public int CastAll(Vector3 _direction, out RaycastHit _hit, float _distance, int _mask, QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore,
                           Collider[] _ignoredColliders = null) {
            float _contactOffset = Physics.defaultContactOffset;
            _distance += _contactOffset * 2f;

            int _amount = wrapper.Cast(_direction, castBuffer, _distance, _mask, _triggerInteraction);
            if (_amount > 0) {
                if ((_ignoredColliders == null) || (_ignoredColliders.Length == 0)) {
                    // Remove this object collider if detected.
                    if (castBuffer[_amount - 1].collider == collider) {
                        _amount--;
                        if (_amount == 0) {
                            _hit = GetDefaultHit();
                            return 0;
                        }
                    }

                    #if DEBUG_LOGGER
                    // Debug utility. Should be remove at some point.
                    for (int i = 0; i < _amount; i++) {
                        if (castBuffer[i].collider == collider) {
                            collider.LogError($"This object collider found => {i}/{_amount}");
                        }
                    }
                    #endif
                } else {
                    // Ignore specifid colliders.
                    for (int i = 0; i < _amount; i++) {
                        if (ArrayUtility.Contains(_ignoredColliders, castBuffer[i].collider)) {
                            castBuffer[i] = castBuffer[--_amount];
                            i--;
                        }
                    }
                }

                Physics3DUtility.SortRaycastHitByDistance(castBuffer, _amount);

                _hit = castBuffer[0];
                _hit.distance = Mathf.Max(0f, _hit.distance - _contactOffset);

                for (int i = 1; i < _amount; i++) {
                    if (castBuffer[i].distance > (_hit.distance + MaxCastDifferenceDetection))
                        return i;
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
        #endregion

        #region Overlap
        private static readonly Collider[] overlapBuffer = new Collider[16];

        /// <summary>
        /// Get the collider at a specific index from the last overlap.
        /// <br/> Note that the last overlap is from the whole game loop,
        /// not specific to this collider.
        /// </summary>
        /// <param name="_index">Index at which to get the collider.
        /// <br/> Should be used in association with the amount from the overlap method.</param>
        /// <returns>The collider at the specified index from the last overlap.</returns>
        public Collider GetOverlapCollider(int _index) {
            return overlapBuffer[_index];
        }

        // -----------------------

        /// <inheritdoc cref="Overlap(int, QueryTriggerInteraction)"/>
        public int Overlap(QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Collide) {
            return Overlap(CollisionMask, _triggerInteraction);
        }

        /// <inheritdoc cref="Overlap(Collider[], int, QueryTriggerInteraction)"/>
        public int Overlap(int _mask, QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Collide) {
            return wrapper.Overlap(overlapBuffer, _mask, _triggerInteraction);
        }

        /// <summary>
        /// <inheritdoc cref="Overlap(Collider[], int, QueryTriggerInteraction)"/>
        /// <para/> Note that this collider itself may be found
        /// <br/> and returned depending on the detection mask, so be sure to ignore it in this case.
        /// </summary>
        /// <inheritdoc cref="Overlap(Collider[], int, QueryTriggerInteraction)"/>
        public int Overlap(Collider[] _ignoredColliders, QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Collide) {
            return Overlap(_ignoredColliders, CollisionMask, _triggerInteraction);
        }

        /// <summary>
        /// Get detailed informations about the current overlapping colliders.
        /// </summary>
        /// <param name="_mask"><see cref="LayerMask"/> to use for collision detection.</param>
        /// <param name="_ignoredColliders">Colliders to ignore on overlap.</param>
        /// <param name="_triggerInteraction">How should the overlap interact with triggers?</param>
        /// <returns>Total amount of overlapping colliders.</returns>
        public int Overlap(Collider[] _ignoredColliders, int _mask, QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Collide) {
            int _amount = wrapper.Overlap(overlapBuffer, _mask, _triggerInteraction);

            for (int i = 0; i < _amount; i++) {
                if (ArrayUtility.Contains(_ignoredColliders, overlapBuffer[i])) {
                    overlapBuffer[i] = overlapBuffer[--_amount];
                    i--;
                }
            }

            return _amount;
        }
        #endregion
    }
}
