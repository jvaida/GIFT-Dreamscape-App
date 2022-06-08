using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Artanim.Location.Data;

namespace Artanim.Tracking
{
	public static class IKUpdateBones
	{
		public static bool IsRollBone(ERigBones rigBone)
		{
			return (rigBone == ERigBones.LeftArmRoll) || (rigBone == ERigBones.RightArmRoll);
		}

		// Bones for which we send the rotation
		public static readonly ERigBones[] Rotation = new ERigBones[]
		{
			ERigBones.Hips,

			// Legs
			ERigBones.LeftUpperLeg,
			ERigBones.RightUpperLeg,
			ERigBones.LeftLowerLeg,
			ERigBones.RightLowerLeg,
			ERigBones.LeftFoot,
			ERigBones.RightFoot,
			ERigBones.LeftToes,
			ERigBones.RightToes,

			// Trunk
			ERigBones.Spine,
			ERigBones.Chest,
			ERigBones.UpperChest,
			ERigBones.Neck,
			ERigBones.Head,

			// Arms
			ERigBones.LeftShoulder,
			ERigBones.RightShoulder,
			ERigBones.LeftUpperArm,
			ERigBones.RightUpperArm,
			ERigBones.LeftArmRoll, // Optional
			ERigBones.RightArmRoll, // Optional
			ERigBones.LeftLowerArm,
			ERigBones.RightLowerArm,
			ERigBones.LeftHand,
			ERigBones.RightHand,
		};

		// Bones for which we send the position
		public static readonly ERigBones[] Position = new ERigBones[]
		{
			ERigBones.Hips, //Send hips position since it's not the root position anymore (IK update to VRIK)
			ERigBones.LeftLowerArm, //Send hands position to make sure we reach the physical targets even if IK stretches bones
			ERigBones.RightLowerArm, //Send hands position to make sure we reach the physical targets even if IK stretches bones
			ERigBones.LeftHand, //Send hands position to make sure we reach the physical targets even if IK stretches bones
			ERigBones.RightHand, //Send hands position to make sure we reach the physical targets even if IK stretches bones
            ERigBones.LeftLowerLeg, //Send hands position to make sure we reach the physical targets even if IK stretches bones
			ERigBones.RightLowerLeg, //Send hands position to make sure we reach the physical targets even if IK stretches bones
			ERigBones.LeftFoot, //Send hands position to make sure we reach the physical targets even if IK stretches bones
			ERigBones.RightFoot, //Send hands position to make sure we reach the physical targets even if IK stretches bones
		};
	}
}