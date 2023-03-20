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

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace EnhancedFramework.Physics3D {
    /// <summary>
    /// <see cref="Movable3D"/>-related class used to perform root motion as instant movement,
    /// according to the current animator state.
    /// </summary>
    [ScriptGizmos(false, true)]
    [RequireComponent(typeof(Animator))]
    [AddComponentMenu(FrameworkUtility.MenuPath + "Physics 3D/Movable 3D Root Motion"), DisallowMultipleComponent]
    public class Movable3DRootMotion : EnhancedBehaviour {
        #region Wrappers
        /// <summary>
        /// <see cref="Animator"/> layer wrapper for root motion.
        /// </summary>
        [Serializable]
        public class AnimationLayerMotion {
            [SerializeField, HideInInspector] public string Name    = "Layer";

            [Tooltip("Toggles this layer root motion")]
            [Enhanced, DisplayName("Name", true)] public bool Enabled = false;

            [Space(5f)]

            public BlockArray<AnimatorStateMotion> States = new BlockArray<AnimatorStateMotion>(false, true, false);

            // -----------------------

            /// <inheritdoc cref="AnimationLayerMotion"/>
            public AnimationLayerMotion(string _name) {
                Name = _name;
                States = new BlockArray<AnimatorStateMotion>(false, true, false);
            }

            // -----------------------

            /// <summary>
            /// Get the <see cref="AnimatorStateMotion"/> associated with a specific hash.
            /// </summary>
            /// <param name="_hash">Hash of the state to get.</param>
            /// <param name="_state">State associated with the given hash (null if none).</param>
            /// <returns>True if the associated state could be found, false otherwise.</returns>
            public bool GetState(int _hash, out AnimatorStateMotion _state) {
                foreach (AnimatorStateMotion _temp in States) {
                    if (_temp.Hash == _hash) {
                        _state = _temp;
                        return true;
                    }
                }

                _state = null;
                return false;
            }

            /// <summary>
            /// Copies the values of another <see cref="AnimationLayerMotion"/> in this one.
            /// </summary>
            /// <param name="_layer">Object to copy values from.</param>
            public void Copy(AnimationLayerMotion _layer) {
                if (_layer.Name != Name) {
                    return;
                }

                Enabled = _layer.Enabled;

                foreach (var _state in States) {

                    if (_layer.States.Array.Find(s => s.Name == _state.Name, out var _motion)) {

                        _state.Enabled = _motion.Enabled;
                        _state.PositionConstraints = _motion.PositionConstraints;
                        _state.RotationConstraints = _motion.RotationConstraints;
                    }
                }
            }
        }

        /// <summary>
        /// <see cref="Animator"/> state wrapper for root motion.
        /// </summary>
        [Serializable]
        public class AnimatorStateMotion {
            [SerializeField, HideInInspector] public string Name    = "State";
            [SerializeField, HideInInspector] public int Hash       = 0;

            [Tooltip("Toggles this state root motion")]
            [Enhanced, DisplayName("Name", true)] public bool Enabled       = false;

            [Space(5f)]

            [Tooltip("Root motion influence on the object position")]
            [SerializeField, Enhanced, ShowIf("Enabled")] public AxisConstraints PositionConstraints = AxisConstraints.X | AxisConstraints.Z;

            [Tooltip("Root motion influence on the object rotation")]
            [SerializeField, Enhanced, ShowIf("Enabled")] public AxisConstraints RotationConstraints = AxisConstraints.None;

            // -----------------------

            /// <inheritdoc cref="AnimatorStateMotion"/>
            public AnimatorStateMotion() { }

            #if UNITY_EDITOR
            /// <inheritdoc cref="AnimatorStateMotion"/>
            public AnimatorStateMotion(AnimatorState _state) {
                Name = _state.name;
                Hash = _state.nameHash;
            }
            #endif

            // -----------------------

            public void ApplyMotion(ref Vector3 _position, ref Quaternion _rotation) {
                // Position.
                if (!PositionConstraints.HasFlag(AxisConstraints.X)) {
                    _position.x = 0f;
                }

                if (!PositionConstraints.HasFlag(AxisConstraints.Y)) {
                    _position.y = 0f;
                }

                if (!PositionConstraints.HasFlag(AxisConstraints.Z)) {
                    _position.z = 0f;
                }

                // Rotation.
                Vector3 _eulers = _rotation.eulerAngles;

                if (!RotationConstraints.HasFlag(AxisConstraints.X)) {
                    _eulers.x = 0f;
                }

                if (!RotationConstraints.HasFlag(AxisConstraints.Y)) {
                    _eulers.y = 0f;
                }

                if (!RotationConstraints.HasFlag(AxisConstraints.Z)) {
                    _eulers.z = 0f;
                }

                _rotation = Quaternion.Euler(_eulers);
            }
        }
        #endregion

        #region Global Members
        [Section("Root Motion")]

        [SerializeField, Enhanced, Required] private Movable3D movable = null;

        [Space(10f), HorizontalLine(SuperColor.Grey, 1f), Space(10f)]

        [SerializeField] private BlockArray<AnimationLayerMotion> animations = new BlockArray<AnimationLayerMotion>(false, false, false);

        // -----------------------

        [SerializeField, HideInInspector] private Animator animator = null;
        #endregion

        #region Enhanced Behaviour
        private void OnAnimatorMove() {
            UpdateRootMotion();
        }

        // -------------------------------------------
        // Editor
        // -------------------------------------------

        #if UNITY_EDITOR
        protected override void OnValidate() {
            base.OnValidate();

            if (!animator) {
                animator = GetComponentInChildren<Animator>();
            }

            if (!movable) {
                movable = GetComponentInChildren<Movable3D>();
            }
        }
        #endif
        #endregion

        #region Root Motion
        /// <summary>
        /// Updates this object root motion.
        /// <para/>
        /// Can be called in editor.
        /// </summary>
        public void UpdateRootMotion() {
            // Iterate through all animator states, to see if root motion should be performed.
            int _count = Mathf.Min(animator.layerCount, animations.Count);

            for (int i = 0; i < _count; i++) {
                var _layer = animations[i];

                if (!_layer.Enabled) {
                    continue;
                }

                AnimatorStateInfo _info = animator.GetCurrentAnimatorStateInfo(i);
                int _hash = _info.shortNameHash;

                if (_layer.GetState(_hash, out AnimatorStateMotion _state) && _state.Enabled) {

                    PerformRootMotion(_state);
                    break;
                }
            }
        }

        /// <summary>
        /// Performs root motion according to a specific state.
        /// </summary>
        /// <param name="_state">Current motion state.</param>
        private void PerformRootMotion(AnimatorStateMotion _state) {
            Vector3 _positionMotion = animator.deltaPosition.RotateInverse(movable.Transform.rotation);
            Quaternion _rotationMotion = animator.deltaRotation;

            _state.ApplyMotion(ref _positionMotion, ref _rotationMotion);
            _positionMotion = _positionMotion.Rotate(movable.Transform.rotation);

            #if UNITY_EDITOR
            // Editor motion.
            if (!Application.isPlaying) {
                movable.transform.position += _positionMotion;
                return;
            }
            #endif

            movable.AddInstantMovementVelocity(_positionMotion);
            movable.OffsetRotation(_rotationMotion);
        }
        #endregion

        #region Editor
        #if UNITY_EDITOR
        /// <summary>
        /// Editor method used to get informations on the associated animator.
        /// </summary>
        [Button(SuperColor.Green)]
        private void Setup() {
            // Issue management.
            if (!animator) {
                this.LogErrorMessage("Missing Animator reference");
                return;
            }

            AnimatorController _controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController));
            if (!_controller) {
                this.LogErrorMessage("Missing Animator Controller reference");
                return;
            }

            // Get animations.
            var _animations = new BlockArray<AnimationLayerMotion>(false, false, false);

            for (int i = 0; i < _controller.layers.Length; i++) {
                AnimatorControllerLayer _layer = _controller.layers[i];
                AnimationLayerMotion _layerMotion = new AnimationLayerMotion(_layer.name);
                
                List<AnimatorState> _states = new List<AnimatorState>();
                GetStates(_layer.stateMachine, _states);

                foreach (var _state in _states) {
                    _layerMotion.States.Add(new AnimatorStateMotion(_state));
                }

                if (i < animations.Count) {
                    _layerMotion.Copy(animations[i]);
                }

                _animations.Add(_layerMotion);
            }

            animations = _animations;

            // ----- Local Method ----- \\

            void GetStates(AnimatorStateMachine _root, List<AnimatorState> _list) {
                foreach (var _state in _root.states) {
                    _list.Add(_state.state);

                    foreach (var _subState in _root.stateMachines) {
                        GetStates(_subState.stateMachine, _list);
                    }
                }
            }
        }
        #endif
        #endregion
    }
}
