// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using EnhancedEditor;
using EnhancedFramework.Core;
using EnhancedFramework.Settings;
using UnityEngine;

using Min = EnhancedEditor.MinAttribute;
using Range = EnhancedEditor.RangeAttribute;

namespace EnhancedFramework.Movable3D {
	/// <summary>
	/// <see cref="Movable3D"/> instance associated configurable attributes.
	/// </summary>
    [CreateAssetMenu(fileName = "ATT_MovableAttributes", menuName = "Enhanced Framework/Attributes/Movable Attributes", order = 150)]
	public class Movable3DAttributes : ScriptableObject {
		#region Global Members
		[Section("Movabl 3De Attributes")]

		public AdvancedCurveValue Speed = new AdvancedCurveValue(new Vector2(0f, 1f), .5f, AnimationCurve.Linear(0f, 0f, 1f, 1f));

        [Space(5f)]

		[Enhanced, Range(0f, 1f)] public float AirSpeedAccelCoef = .65f;

		[Space(5f), HorizontalLine(SuperColor.Aquamarine), Space(5f)]

		[Enhanced, Range(0f, 1f)] public float GroundSurfaceRotationCoef	= 1f;
		[Enhanced, Range(0f, 100f)] public float GroundSurfaceRotationSpeed	= 5f;

		[Space(10f)]

		[Enhanced, Min(.001f)] public float GravityCoef = 1f;

		[Space(10f)]

		public bool DoOverrideCreatureCollisionSettings = false;
		[Enhanced, ShowIf("DoOverrideCreatureCollisionSettings"), Range(0f, 5f)] private float climbHeight	= .2f;
		[Enhanced, ShowIf("DoOverrideCreatureCollisionSettings"), Range(0f, 5f)] private float snapHeight	= .2f;

		// -----------------------

		public float ClimbHeight {
            get {
				return DoOverrideCreatureCollisionSettings
					 ? climbHeight
					 : PhysicsSettings.I.ClimbHeight;
            }
        }

		public float SnapHeight {
			get {
				return DoOverrideCreatureCollisionSettings
					 ? snapHeight
					 : PhysicsSettings.I.SnapHeight;
			}
		}
		#endregion
	}
}
