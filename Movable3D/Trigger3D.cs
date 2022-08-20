// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using UnityEngine;

namespace EnhancedFramework.Movable3D {
    /// <summary>
    /// Base class to inherit all game triggers from.
    /// <br/> Provides easy-to-use callbacks when something enters or exits this trigger.
    /// </summary>
	public abstract class Trigger3D : MonoBehaviour {
        #region Callbacks
        /// <inheritdoc cref="OnEnterTrigger(Movable3D, IMovable3DController)"/>
        public virtual void OnEnterTrigger(Movable3D _movable) { }

        /// <summary>
        /// Called when something enters this trigger.
        /// </summary>
        /// <param name="_movable">Movable who entered this trigger.</param>
        /// <param name="_controller">Controller of the associated movable (if one).</param>
        public virtual void OnEnterTrigger(Movable3D _movable, IMovable3DController _controller) { }

        // -----------------------

        /// <inheritdoc cref="OnExitTrigger(Movable3D, IMovable3DController)"/>
        public virtual void OnExitTrigger(Movable3D _movable) { }

        /// <summary>
        /// Called when something exits this trigger.
        /// </summary>
        /// <param name="_movable">Movable who exited this trigger.</param>
        /// <param name="_controller">Controller of the associated movable (if one).</param>
        public virtual void OnExitTrigger(Movable3D _movable, IMovable3DController _controller) { }
        #endregion
    }
}
