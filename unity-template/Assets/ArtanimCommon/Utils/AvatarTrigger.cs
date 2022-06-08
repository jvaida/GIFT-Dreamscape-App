using Artanim.Location.Network;
using Artanim.Location.Messages;
using Artanim.Location.SharedData;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{

	[AddComponentMenu("Artanim/Avatar Trigger")]
	public class AvatarTrigger : NetworkSyncedBehaviour
	{
		[Header("Action triggering")]
		[Tooltip("Specifies if actions have to be invoked on the triggering avatars client only. (Only applicable if mode allowed client actions)")]
		public bool TriggeringClientOnly = false;

		[Header("Basic actions")]
		public AvatarEvent OnHeadEnter;
		public AvatarEvent OnHeadExit;

		public AvatarEvent OnHandEnter;
		public AvatarEvent OnHandExit;

		public AvatarEvent OnFootEnter;
		public AvatarEvent OnFootExit;

		[Header("Specific actions")]

		public AvatarEvent OnLeftFootEnter;
		public AvatarEvent OnLeftFootExit;

		public AvatarEvent OnRightFootEnter;
		public AvatarEvent OnRightFootExit;

		public AvatarEvent OnLeftHandEnter;
		public AvatarEvent OnLeftHandExit;

		public AvatarEvent OnRightHandEnter;
		public AvatarEvent OnRightHandExit;

		private bool[] BodyPartStates = new bool[Enum.GetValues(typeof(EAvatarBodyPart)).Length];

		#region Unity events
		
		void Start()
		{
			//Check for collider
			if(!GetComponent<Collider>())
			{
				Debug.LogWarning("No collider found. Please add at least one collider to the AvatarTrigger object.");
			}
			else
			{
				//Only activate the collider on server side
				if (NetworkInterface.Instance != null && !NetworkInterface.Instance.IsServer)
					GetComponent<Collider>().enabled = false;
			}
		}

		void OnEnable()
		{
			if(NetworkInterface.Instance != null)
				NetworkInterface.Instance.Subscribe<Location.Messages.AvatarTrigger>(NetworkMessage_AvatarTrigger);
		}
		
		void OnDisable()
		{
			NetworkInterface.SafeUnsubscribe<Location.Messages.AvatarTrigger>(NetworkMessage_AvatarTrigger);
		}

		void OnTriggerEnter(Collider other)
		{
			var avatar = other.GetComponentInParent<AvatarController>();
			if (avatar)
			{
				var bodyPart = other.GetComponent<AvatarBodyPart>();
				if (bodyPart)
				{
					//Send network message
					SendTriggerEvent(bodyPart.BodyPart, avatar.PlayerId, true);
				}
			}
		}

		void OnTriggerExit(Collider other)
		{
			var avatar = other.GetComponentInParent<AvatarController>();

			if (avatar)
			{
				var bodyPart = other.GetComponent<AvatarBodyPart>();
				if (bodyPart)
				{
					//Send network message
					SendTriggerEvent(bodyPart.BodyPart, avatar.PlayerId, false);
				}
			}
		}

		#endregion

		#region Network events

		private void NetworkMessage_AvatarTrigger(Location.Messages.AvatarTrigger args)
		{
			if(NeedTrigger(args.TriggerName))
			{
				//Check if we only need to trigger on the corresponding client
				if(!TriggeringClientOnly || SharedDataUtils.MySharedId == args.PlayerId)
				{
					var player = GameController.Instance.GetPlayerByPlayerId(args.PlayerId);
					if (player != null && player.AvatarController)
					{
						SetBodyPartState(args.BodyPart, player.AvatarController, args.State);
					}
					else
					{
						Debug.LogWarningFormat("Failed to execute AvatarTrigger. No avatar found with playerId={0}", args.PlayerId);
					}
				}
			}
		}

		#endregion

		#region Public interface

		/// <summary>
		/// Checks if a specified body part is inside the collider.
		/// </summary>
		/// <param name="bodyPart"></param>
		/// <returns></returns>
		public bool IsInside(EAvatarBodyPart bodyPart)
		{
			return BodyPartStates[(int)bodyPart];
		}

		#endregion

		#region Internals

		private void SendTriggerEvent(EAvatarBodyPart bodyPart, Guid playerId, bool state)
		{
			if(NetworkInterface.Instance.IsServer && ValidateObjectId())
			{
				NetworkInterface.Instance.SendMessage(new Artanim.Location.Messages.AvatarTrigger
				{
					TriggerName = ObjectId,
					PlayerId = playerId,
					BodyPart = bodyPart,
					State = state,
				});
			}
		}

		private void SetBodyPartState(EAvatarBodyPart bodyPart, AvatarController avatar, bool state)
		{
			if (ConfigService.VerboseSdkLog) Debug.LogFormat("Setting body part state: bodyPart={0}, avatar={1}, state={2}", bodyPart.ToString(), avatar.PlayerId, state);
			if(BodyPartStates[(int)bodyPart] != state && avatar)
			{
				BodyPartStates[(int)bodyPart] = state;

				//Check specific part
				switch (bodyPart)
				{
					case EAvatarBodyPart.LeftFoot:
						if(state)
							OnLeftFootEnter.Invoke(avatar);
						else
							OnLeftFootExit.Invoke(avatar);
						
						break;

					case EAvatarBodyPart.RightFoot:
						if (state)
							OnRightFootEnter.Invoke(avatar);
						else
							OnRightFootExit.Invoke(avatar);
						
						break;

					case EAvatarBodyPart.LeftHand:
						if (state)
							OnLeftHandEnter.Invoke(avatar);
						else
							OnLeftHandExit.Invoke(avatar);
						
						break;

					case EAvatarBodyPart.RightHand:
						if (state)
							OnRightHandEnter.Invoke(avatar);
						else
							OnRightHandExit.Invoke(avatar);
						
						break;

					case EAvatarBodyPart.Head:
						if (state)
							OnHeadEnter.Invoke(avatar);
						else
							OnHeadExit.Invoke(avatar);
						break;

					default:
						break;
				}

				//Check combined part triggers
				if(bodyPart != EAvatarBodyPart.Head)
					CheckCombinedState(bodyPart, avatar, state);
			}
		}

		private void CheckCombinedState(EAvatarBodyPart bodyPart, AvatarController avatar, bool newState)
		{
			var stateCount = CountCombinedState(bodyPart, newState);

			if(newState)
			{
				//Do we need to fire event or was it already triggered?
				if(stateCount == 1)
				{
					switch (bodyPart)
					{
						case EAvatarBodyPart.LeftFoot:
						case EAvatarBodyPart.RightFoot:
								OnFootEnter.Invoke(avatar);
							break;

						case EAvatarBodyPart.LeftHand:
						case EAvatarBodyPart.RightHand:
								OnHandEnter.Invoke(avatar);
							break;
					}
				}

			}
			else
			{
				//Do we need to fire event or was it already triggered?
				if (stateCount == 2)
				{
					switch (bodyPart)
					{
						case EAvatarBodyPart.LeftFoot:
						case EAvatarBodyPart.RightFoot:
								OnFootExit.Invoke(avatar);
							break;

						case EAvatarBodyPart.LeftHand:
						case EAvatarBodyPart.RightHand:
								OnHandExit.Invoke(avatar);
							break;
					}
				}
			}
		}

		private int CountCombinedState(EAvatarBodyPart bodyPart, bool state)
		{
			var stateCount = 0;
			switch (bodyPart)
			{
				case EAvatarBodyPart.LeftFoot:
				case EAvatarBodyPart.RightFoot:
					if (BodyPartStates[(int)EAvatarBodyPart.LeftFoot] == state)
						stateCount++;

					if (BodyPartStates[(int)EAvatarBodyPart.RightFoot] == state)
						stateCount++;
					break;

				case EAvatarBodyPart.LeftHand:
				case EAvatarBodyPart.RightHand:
					if (BodyPartStates[(int)EAvatarBodyPart.LeftHand] == state)
						stateCount++;

					if (BodyPartStates[(int)EAvatarBodyPart.RightHand] == state)
						stateCount++;
					break;

				case EAvatarBodyPart.Head:
					if (BodyPartStates[(int)EAvatarBodyPart.Head] == state)
						stateCount++;
					break;

				default:
					break;
			}
			return stateCount;
		}

		private void SetMaskValue(EAvatarBodyPart bodyPart, bool on)
		{
			var mask = GameSessionController.Instance.GetValue(ObjectId, 0);
			var bodyPartValue = 1 << (int)bodyPart;

			if (on)
				mask |= bodyPartValue;
			else
				mask &= bodyPartValue;

			GameSessionController.Instance.SetValue(ObjectId, mask);
		}
		
		#endregion

	}

}