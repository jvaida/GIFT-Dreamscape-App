using Artanim.HandAnimation.Config;
using Artanim.HandAnimation.Leap;
using Artanim.HandAnimation.Procedural;
using UnityEngine;

namespace Artanim.HandAnimation
{
    [RequireComponent(typeof(AvatarController))]
    public class AvatarHandController : MonoBehaviour
    {
        public AvatarHandDefinition HandDefinition;

        private AvatarController _AvatarController;
        private AvatarController AvatarController
        {
            get
            {
                if (!_AvatarController)
                    _AvatarController = GetComponent<AvatarController>();
                return _AvatarController;
            }
        }

        private void Start()
        {
			if (!HandDefinition)
			{
				Debug.LogErrorFormat("No hand definition defined on avatar {0}. Disabling AvatarHandController.", name);
				enabled = false;
			}
			else
			{
				InitHands();
			}
        }

        #region Internals

        private void InitHands()
        {
            var leftHand = AvatarController.AvatarAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            var rightHand = AvatarController.AvatarAnimator.GetBoneTransform(HumanBodyBones.RightHand);

			//Get or add the HandAnimationManagers to each hand and set handedness
			HandAnimationManager leftHandAM = GetComponentOrAddIfNull<HandAnimationManager>(leftHand.gameObject);
			leftHandAM.AvatarHandDefinition = HandDefinition;
			leftHandAM.Handedness = AvatarHandDefinition.ESide.Left;
			HandAnimationManager rightHandAM = GetComponentOrAddIfNull<HandAnimationManager>(rightHand.gameObject);
			rightHandAM.AvatarHandDefinition = HandDefinition;
			rightHandAM.Handedness = AvatarHandDefinition.ESide.Right;

			//Assign procedural controllers
			HandController leftHandController = GetComponentOrAddIfNull<HandController>(leftHand.gameObject);
			leftHandAM.ProceduralAnimator = leftHandController;
			leftHandController.Side = AvatarHandDefinition.ESide.Left;

			HandController rightHandController = GetComponentOrAddIfNull<HandController>(rightHand.gameObject);
			rightHandAM.ProceduralAnimator = rightHandController;
			rightHandController.Side = AvatarHandDefinition.ESide.Right;

			//Assign Leap tracking controllers
			//Leap tracking current requires manual setup and may not be used in every experience. 
			//So just assign the component if it's there, but don't try to automagically add it. 
			leftHandAM.TrackedAnimator = leftHand.GetComponent<ArtanimRiggedHand>();
			rightHandAM.TrackedAnimator = rightHand.GetComponent<ArtanimRiggedHand>();
		}

		private T GetComponentOrAddIfNull<T>(GameObject gameObject) where T : Component
		{
			T component = gameObject.GetComponent<T>();
			if(component == null)
			{
				component = gameObject.AddComponent<T>();
			}

			return component;
		}

        #endregion

    }
}