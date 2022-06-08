using Artanim.Tracking;

namespace Artanim.HandAnimation
{
	//Bones which take part in the animation of the hands
	public static class HandUpdateBones
	{
		// Bones for which we send the rotation in a HandTransformStream
		public static readonly ERigBones[] Rotation = new ERigBones[]
		{
			ERigBones.LeftHand, //left wrist

			ERigBones.RightHand, //right wrist

			//Left hand finger bones
			ERigBones.LeftThumbProximal,
			ERigBones.LeftThumbIntermediate,
			ERigBones.LeftThumbDistal,
			ERigBones.LeftIndexProximal,
			ERigBones.LeftIndexIntermediate,
			ERigBones.LeftIndexDistal,
			ERigBones.LeftMiddleProximal,
			ERigBones.LeftMiddleIntermediate,
			ERigBones.LeftMiddleDistal,
			ERigBones.LeftRingProximal,
			ERigBones.LeftRingIntermediate,
			ERigBones.LeftRingDistal,
			ERigBones.LeftLittleProximal,
			ERigBones.LeftLittleIntermediate,
			ERigBones.LeftLittleDistal,

			//Right hand finger bones
			ERigBones.RightThumbProximal,
			ERigBones.RightThumbIntermediate,
			ERigBones.RightThumbDistal,
			ERigBones.RightIndexProximal,
			ERigBones.RightIndexIntermediate,
			ERigBones.RightIndexDistal,
			ERigBones.RightMiddleProximal,
			ERigBones.RightMiddleIntermediate,
			ERigBones.RightMiddleDistal,
			ERigBones.RightRingProximal,
			ERigBones.RightRingIntermediate,
			ERigBones.RightRingDistal,
			ERigBones.RightLittleProximal,
			ERigBones.RightLittleIntermediate,
			ERigBones.RightLittleDistal,
		};

		public static readonly ERigBones[] LeftHandBones = new ERigBones[]
		{
			ERigBones.LeftHand, //left wrist
			//Left hand finger bones
			ERigBones.LeftThumbProximal,
			ERigBones.LeftThumbIntermediate,
			ERigBones.LeftThumbDistal,
			ERigBones.LeftIndexProximal,
			ERigBones.LeftIndexIntermediate,
			ERigBones.LeftIndexDistal,
			ERigBones.LeftMiddleProximal,
			ERigBones.LeftMiddleIntermediate,
			ERigBones.LeftMiddleDistal,
			ERigBones.LeftRingProximal,
			ERigBones.LeftRingIntermediate,
			ERigBones.LeftRingDistal,
			ERigBones.LeftLittleProximal,
			ERigBones.LeftLittleIntermediate,
			ERigBones.LeftLittleDistal,
		};

		public static readonly ERigBones[] RightHandBones = new ERigBones[]
		{
			ERigBones.RightHand, //right wrist
			//Right hand finger bones
			ERigBones.RightThumbProximal,
			ERigBones.RightThumbIntermediate,
			ERigBones.RightThumbDistal,
			ERigBones.RightIndexProximal,
			ERigBones.RightIndexIntermediate,
			ERigBones.RightIndexDistal,
			ERigBones.RightMiddleProximal,
			ERigBones.RightMiddleIntermediate,
			ERigBones.RightMiddleDistal,
			ERigBones.RightRingProximal,
			ERigBones.RightRingIntermediate,
			ERigBones.RightRingDistal,
			ERigBones.RightLittleProximal,
			ERigBones.RightLittleIntermediate,
			ERigBones.RightLittleDistal,
		};
	}
}