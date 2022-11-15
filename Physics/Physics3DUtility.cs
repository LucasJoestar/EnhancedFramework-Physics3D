// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using System;
using System.Collections.Generic;
using UnityEngine;

namespace EnhancedFramework.Physics3D {
    /// <summary>
    /// Contains a bunch of usefull methods related to the game Physics.
    /// </summary>
    public static class Physics3DUtility {
        #region Raycast Hit Comparer
        /// <summary>
        /// Comparer for <see cref="RaycastHit"/> by distance.
        /// </summary>
        private class RaycastHitDistanceComparer : IComparer<RaycastHit> {
            public static readonly RaycastHitDistanceComparer Default = new RaycastHitDistanceComparer();

            public int Compare(RaycastHit _a, RaycastHit _b) {
                return _a.distance.CompareTo(_b.distance);
            }
        }
        #endregion

        #region Collider Comparer
        /// <summary>
        /// Comparer for <see cref="Collider"/> by distance.
        /// </summary>
        private class ColliderDistanceComparer : IComparer<Collider> {
            private static readonly ColliderDistanceComparer comparer = new ColliderDistanceComparer();
            private static Vector3 reference = Vector3.zero;

            public static ColliderDistanceComparer GetComparer(Vector3 _reference) {
                reference = _reference;
                return comparer;
            }

            public int Compare(Collider _a, Collider _b) {
                return (_a.transform.position - reference).sqrMagnitude.CompareTo((_b.transform.position - reference).sqrMagnitude);
            }
        }
        #endregion

        #region Raycast Hit
        /// <summary>
        /// Sort an array of <see cref="RaycastHit"/> by their distance.
        /// </summary>
        public static void SortRaycastHitByDistance(RaycastHit[] _hits, int _amount) {
            Array.Sort(_hits, 0, _amount, RaycastHitDistanceComparer.Default);
        }
        #endregion

        #region Collider
        /// <summary>
        /// Sort an array of <see cref="Collider"/> by their distance from a reference <see cref="Vector3"/>.
        /// </summary>
        public static void SortCollidersByDistance(Collider[] _colliders, int _amount, Vector3 _reference) {
            Array.Sort(_colliders, 0, _amount, ColliderDistanceComparer.GetComparer(_reference));
        }
        #endregion

        #region Collision Mask
        /// <summary>
        /// Get the collision layer mask that indicates which layer(s) the specified <see cref="GameObject"/> can collide with.
        /// </summary>
        /// <param name="_gameObject">The <see cref="GameObject"/> to retrieve the collision layer mask for.</param>
        public static int GetLayerCollisionMask(GameObject _gameObject) {
            int _layer = _gameObject.layer;
            return GetLayerCollisionMask(_layer);
        }

        /// <summary>
        /// Get the collision layer mask that indicates which layer(s) the specified layer can collide with.
        /// </summary>
        /// <param name="_layer">The layer to retrieve the collision layer mask for.</param>
        public static int GetLayerCollisionMask(int _layer) {
            int _layerMask = 0;
            for (int i = 0; i < 32; i++) {
                if (!Physics.GetIgnoreLayerCollision(_layer, i))
                    _layerMask |= 1 << i;
            }

            return _layerMask;
        }
        #endregion

        #region Utility
        /// <inheritdoc cref="IsGroundSurface(Collider, Vector3, Vector3)"/>
        /// <param name="_hit">Hit result of the surface to stand on.</param>
        public static bool IsGroundSurface(RaycastHit _hit, Vector3 _up) {
            return IsGroundSurface(_hit.collider, _hit.normal, _up);
        }

        /// <summary>
        /// Get if a specific surface can be considered as a ground (surface to stand on) or not.
        /// </summary>
        /// <param name="_collider">Collider attached to the testing surface.</param>
        /// <param name="_normal">The normal surface to check.</param>
        /// <param name="_up">Referential up vector of the object to stand on the surface.</param>
        public static bool IsGroundSurface(Collider _collider, Vector3 _normal, Vector3 _up) {
            float _angle = Vector3.Angle(_normal, _up);
            return (_angle <= Physics3DSettings.I.GroundAngle) && !_collider.TryGetComponent<NonGroundSurface3D>(out _);
        }
        #endregion
    }
}
