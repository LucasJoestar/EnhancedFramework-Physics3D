// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using EnhancedEditor;
using EnhancedFramework.Core;
using UnityEngine;

#if DOTWEEN_ENABLED
using DG.Tweening;
#endif

using Range = EnhancedEditor.RangeAttribute;

namespace EnhancedFramework.Physics3D {
	/// <summary>
	/// <see cref="CreatureMovable3D"/> instance associated configurable attributes.
	/// </summary>
    [CreateAssetMenu(fileName = "ATB_CreatureAttributes", menuName = FrameworkUtility.MenuPath + "Attributes/Creature Movable 3D Attributes",
					 order = FrameworkUtility.MenuOrder)]
	public class CreatureMovable3DAttributes : ScriptableObject {
		#region Global Members
		[Section("Creature Movable Attributes")]

		[Tooltip("The speed of the creature movement, in unit/second.")]
		public AdvancedCurveValue MoveSpeed = new AdvancedCurveValue(new Vector2(0f, 1f), .5f, AnimationCurve.Linear(0f, 0f, 1f, 1f));

		[Enhanced, Range(0f, 1f)] public float AirAccelCoef = .65f;

		[Space(10f)]

		[Tooltip("The speed of the creature rotation, in quarter circle/second.")]
		#if DOTWEEN_ENABLED
		public EaseValue TurnSpeed = new EaseValue(new Vector2(0f, 1f), 1f, Ease.InOutSine);
		#else
		public CurveValue TurnSpeed = new CurveValue(new Vector2(0f, 1f), 1f, AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
		#endif

		[Space(10f), HorizontalLine(SuperColor.Grey, 1f), Space(10f)]

		public bool OverrideCollisionSettings = false;

		[SerializeField, Enhanced, ShowIf("OverrideCollisionSettings"), Range(0f, 5f)] private float climbHeight = .2f;
		[SerializeField, Enhanced, ShowIf("OverrideCollisionSettings"), Range(0f, 5f)] private float snapHeight	= .2f;

		// -----------------------

		public float ClimbHeight {
            get {
				return OverrideCollisionSettings
					 ? climbHeight
					 : Physics3DSettings.I.ClimbHeight;
            }
        }

		public float SnapHeight {
			get {
				return OverrideCollisionSettings
					 ? snapHeight
					 : Physics3DSettings.I.SnapHeight;
			}
		}
		#endregion

		#region Setup
		/// <summary>
		/// Setup these attributes for a specific <see cref="CreatureMovable3D"/> instance.
		/// <br/> Automatically registers the associated ease and curve values for the object.
		/// </summary>
		/// <param name="_movable">The <see cref="CreatureMovable3D"/> to setup these attributes with.</param>
		public void Setup(CreatureMovable3D _movable) {
			int _id = _movable.ID;

			MoveSpeed.Register(_id);
			TurnSpeed.Register(_id);
        }

		/// <summary>
		/// Unset these attributes from a specific <see cref="CreatureMovable3D"/> instance.
		/// <br/> Automatically unregisters the associated ease and curve values from the object.
		/// </summary>
		/// <param name="_movable">The <see cref="CreatureMovable3D"/> to unset these attributes with.</param>
		public void Unset(CreatureMovable3D _movable) {
			int _id = _movable.ID;

			MoveSpeed.Unregister(_id);
			TurnSpeed.Unregister(_id);
		}
        #endregion
    }
}
