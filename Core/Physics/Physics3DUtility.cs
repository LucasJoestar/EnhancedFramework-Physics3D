// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using EnhancedEditor;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace EnhancedFramework.Physics3D {
    /// <summary>
    /// Contains multiple 3D Physics related utility methods.
    /// </summary>
    public static class Physics3DUtility {
        #region Raycast Hit
        private static readonly List<RaycastHit> raycastHitBuffer = new List<RaycastHit>();
        private static readonly Comparison<RaycastHit> raycastHitComparison = CompareRaycastHits;

        // -----------------------

        /// <summary>
        /// Sort an array of <see cref="RaycastHit"/> by their distance.
        /// </summary>
        /// <param name="_hits">Hits to sort.</param>
        /// <param name="_count">Total count of hits to sort.</param>
        public static void SortRaycastHitByDistance(RaycastHit[] _hits, int _count) {

            List<RaycastHit> _buffer = raycastHitBuffer;

            // Use List.Sort instead of Array.Sort to avoid any memory allocation.
            _buffer.Resize(_count);
            for (int i = 0; i < _count; i++) {
                _buffer[i] = _hits[i];
            }

            SortRaycastHitByDistance(_buffer);

            // Update array content.
            _buffer.CopyTo(0, _hits, 0, _count);
        }

        /// <inheritdoc cref="SortRaycastHitByDistance(RaycastHit[], int)"/>
        public static void SortRaycastHitByDistance(List<RaycastHit> _hits) {
            _hits.Sort(raycastHitComparison);
        }

        // -----------------------

        private static int CompareRaycastHits(RaycastHit a, RaycastHit b) {
            return a.distance.CompareTo(b.distance);
        }
        #endregion

        #region Collider
        private static readonly List<Collider> colliderBuffer = new List<Collider>();
        private static readonly Comparison<Collider> colliderComparison = CompareColliders;

        private static Vector3 reference = Vector3.zero;

        // -----------------------

        /// <summary>
        /// Sort an array of <see cref="Collider"/> by their distance from a reference <see cref="Vector3"/>.
        /// </summary>
        /// <param name="_colliders">Colliders to sort.</param>
        /// <param name="_count">Total count of colliders to sort.</param>
        /// <param name="_reference">Reference position used for sorting.</param>
        public static void SortCollidersByDistance(Collider[] _colliders, int _count, Vector3 _reference) {

            List<Collider> _buffer = colliderBuffer;

            // Use List.Sort instead of Array.Sort to avoid any memory allocation.
            _buffer.Resize(_count);
            for (int i = 0; i < _count; i++) {
                _buffer[i] = _colliders[i];
            }

            SortCollidersByDistance(_buffer, reference);

            // Update array content.
            _buffer.CopyTo(0, _colliders, 0, _count);
        }

        /// <inheritdoc cref="SortCollidersByDistance(Collider[], int, Vector3)"/>
        public static void SortCollidersByDistance(List<Collider> _colliders, Vector3 _reference) {
            reference = _reference;
            _colliders.Sort(colliderComparison);
        }

        // -----------------------

        static int CompareColliders(Collider a, Collider b) {
            return (a.transform.position - reference).sqrMagnitude.CompareTo((b.transform.position - reference).sqrMagnitude);
        }
        #endregion

        #region Collision Mask
        /// <summary>
        /// Get the collision layer mask that indicates which layer(s) the specified <see cref="GameObject"/> can collide with.
        /// </summary>
        /// <param name="_gameObject">The <see cref="GameObject"/> to retrieve the collision layer mask for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetLayerCollisionMask(GameObject _gameObject) {
            return GetLayerCollisionMask(_gameObject.layer);
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
        /// <summary>
        /// Physics related default contact offset.
        /// </summary>
        public static float ContactOffset {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Physics.defaultContactOffset; }
        }

        // -----------------------

        /// <inheritdoc cref="IsGroundSurface(Collider, Vector3, Vector3)"/>
        /// <param name="_hit">Hit result of the surface to stand on.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsGroundSurface(RaycastHit _hit, Vector3 _up) {
            return IsGroundSurface(_hit.collider, _hit.normal, _up);
        }

        /// <inheritdoc cref="IsGroundAngle(Collider, Vector3, Vector3, out bool)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsGroundSurface(Collider _collider, Vector3 _normal, Vector3 _up) {
            bool isGroundAngle = IsGroundAngle(_collider, _normal, _up, out bool _isGroundSurface);
            return isGroundAngle && _isGroundSurface;
        }

        /// <inheritdoc cref="IsGroundAngle(Collider, Vector3, Vector3, out bool)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsGroundAngle(RaycastHit _hit, Vector3 _up, out bool _isGroundSurface) {
            return IsGroundAngle(_hit.collider, _hit.normal, _up, out _isGroundSurface);
        }

        /// <summary>
        /// Get if a specific surface can be considered as a ground (surface to stand on) or not.
        /// </summary>
        /// <param name="_collider">Collider attached to the testing surface.</param>
        /// <param name="_normal">The normal surface to check.</param>
        /// <param name="_up">Referential up vector of the object to stand on the surface.</param>
        /// <param name="_isGroundSurface">True if this collider has the <see cref="GroundSurface"/> component, false otherwise.</param>
        /// <returns>True if this surface angle can be considered as ground, false otherwise.</returns>
        public static bool IsGroundAngle(Collider _collider, Vector3 _normal, Vector3 _up, out bool _isGroundSurface) {
            float _angle = Vector3.Angle(_normal, _up);
            _isGroundSurface = IsGroundSurface(_collider);

            return _angle <= Physics3DSettings.I.GroundAngle;
        }

        /// <summary>
        /// Gat if a specific <see cref="Collider"/> can be considered as a ground surface.
        /// </summary>
        /// <param name="collider">The collider to check.</param>
        /// <returns>True if this collider can be considered as a ground surface, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsGroundSurface(Collider _collider) {
            return !_collider.TryGetComponent<NonGroundSurface3D>(out _);
        }
        #endregion
    }
}
