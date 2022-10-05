// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using EnhancedFramework.Core;

namespace EnhancedFramework.Movable3D {
    /// <summary>
    /// Base class to inherit all game triggers from.
    /// <br/> Provides easy-to-use callbacks when something enters or exits.
    /// </summary>
    public interface ITrigger3D {
        #region Content
        /// <summary>
        /// Called when something enters this trigger.
        /// </summary>
        /// <param name="_movable">Movable who entered this trigger.</param>
        void OnEnterTrigger(Movable3D _movable);

        /// <summary>
        /// Called when something exits this trigger.
        /// </summary>
        /// <param name="_movable">Movable who exited this trigger.</param>
        void OnExitTrigger(Movable3D _movable);
        #endregion
    }

    /// <summary>
    /// Base class for generic triggers.
    /// </summary>
	public abstract class Trigger3D : EnhancedBehaviour, ITrigger3D {
        #region Trigger
        public virtual void OnEnterTrigger(Movable3D _movable) { }

        public virtual void OnExitTrigger(Movable3D _movable) { }
        #endregion
    }
}
