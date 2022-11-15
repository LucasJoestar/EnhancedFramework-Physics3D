// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using EnhancedFramework.Core;

namespace EnhancedFramework.Physics3D {
    /// <summary>
    /// Base class to inherit all game triggers from.
    /// <br/> Provides easy-to-use callbacks when something enters or exits.
    /// </summary>
    public interface ITrigger3D {
        #region Content
        /// <summary>
        /// Called when a <see cref="Movable3D"/> enters this trigger.
        /// </summary>
        /// <param name="_movable">The <see cref="Movable3D"/> who entered this trigger.</param>
        void OnEnterTrigger(Movable3D _movable);

        /// <summary>
        /// Called when a <see cref="Movable3D"/> exits this trigger.
        /// </summary>
        /// <param name="_movable">The <see cref="Movable3D"/> who exited this trigger.</param>
        void OnExitTrigger(Movable3D _movable);
        #endregion
    }

    /// <summary>
    /// Base generic trigger class to inherit your own triggers from.
    /// </summary>
	public abstract class Trigger3D : EnhancedBehaviour, ITrigger3D {
        #region Trigger
        public virtual void OnEnterTrigger(Movable3D _movable) { }

        public virtual void OnExitTrigger(Movable3D _movable) { }
        #endregion
    }
}
