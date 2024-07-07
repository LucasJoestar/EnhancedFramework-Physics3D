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
	/// <see cref="CreatureMovable3D"/>-related configurable attributes.
	/// </summary>
    [CreateAssetMenu(fileName = "MVB_MovableAttributes", menuName = FrameworkUtility.MenuPath + "Attributes/Creature Movable 3D", order = FrameworkUtility.MenuOrder)]
	public class CreatureMovable3DAttributes : ScriptableObject {
		#region Global Members
		[Section("Movable Attributes")]

		[Tooltip("Movement speed curve, in unit/second")]
		public AdvancedCurveValue MoveSpeed = new AdvancedCurveValue(new Vector2(0f, 1f), .5f, AnimationCurve.Linear(0f, 0f, 1f, 1f));

		[Tooltip("Rotation speed curve, in quarter circle/second.")]
		#if DOTWEEN_ENABLED
		public EaseValue TurnSpeed = new EaseValue(new Vector2(0f, 1f), 1f, Ease.InOutSine);
		#else
		public CurveValue TurnSpeed = new CurveValue(new Vector2(0f, 1f), 1f, AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
		#endif

		[Space(10f)]

		[Tooltip("Acceleration coefficient applied while in the air")]
		[Enhanced, Range(0f, 1f)] public float AirAccelCoef = .65f;

		[Tooltip("Determines how to manage the object rotation when following a path")]
		public CreatureMovable3D.PathRotationMode PathRotationMode = CreatureMovable3D.PathRotationMode.TurnDuringMovement;

		[Space(10f), HorizontalLine(SuperColor.Crimson, 2f), Space(10f)]

		public bool OverrideCollisionSettings = false;

		[Tooltip("Maximum height used to climb steps and surfaces")]
		[SerializeField, Enhanced, ShowIf(nameof(OverrideCollisionSettings)), Range(0f, 5f)] private float climbHeight	= .2f;

		[Tooltip("Maximum height used for snapping to the nearest surface")]
		[SerializeField, Enhanced, ShowIf(nameof(OverrideCollisionSettings)), Range(0f, 5f)] private float snapHeight	= .2f;

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
    }
}
