// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using EnhancedEditor;
using EnhancedFramework.Core.Settings;
using UnityEngine;

using Min = EnhancedEditor.MinAttribute;
using Range = EnhancedEditor.RangeAttribute;

namespace EnhancedFramework.Physics3D {
    /// <summary>
    /// Physics3D-related ruling settings.
    /// </summary>
    [CreateAssetMenu(fileName = MenuPrefix + "Physics3DSettings", menuName = MenuPath + "Physics 3D", order = MenuOrder)]
	public class Physics3DSettings : BaseSettings<Physics3DSettings> {
        #region Global Members
        [Section("Physics 3D Settings")]

        [Enhanced] public float Gravity = -9.81f;
        [Enhanced, Max(0f)] public float MaxGravity = -25f;

        [Space]

        [Enhanced, Range(.1f, 90f)] public float GroundAngle    = 30f;
        [Enhanced, Min(0f)] public float ClimbHeight      = .2f;
        [Enhanced, Min(0f)] public float SnapHeight       = .2f;

        [Space]

        [Enhanced, Min(0f)] public float SteepSlopeRequiredMovement = 20f;
        [Enhanced, Min(0f)] public float SteepSlopeRequiredForce    = 10f;

        [Space(5f), HorizontalLine(SuperColor.Green), Space(5f)]

        [Enhanced, Range(0f, 1f)] public float OnGroundedForceMultiplier = .55f;

        [Space]

        [Enhanced, Min(0f)] public float GroundDecelerationForce    = 17f;
        [Enhanced, Min(0f)] public float AirDecelerationForce       = 5f;
        #endregion
    }
}
