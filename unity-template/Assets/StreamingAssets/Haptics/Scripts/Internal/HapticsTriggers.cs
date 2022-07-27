using Artanim.Haptics.Visuals;
using Artanim.Location.Network;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim.Haptics.Internal
{
	public class HapticsTriggers : MonoBehaviour
	{
		[SerializeField]
		GameObject _podLayoutTemplate;

		[SerializeField]
		GameObject _hapticsVirtualControlsTemplate;

		public void TogglePodLayout()
		{
			if (Visuals.Pod.Instance)
			{
				Debug.Log("Destroy PodVisu");
				GameObject.Destroy(Visuals.Pod.Instance.gameObject);
			}
			else if (_podLayoutTemplate)
			{
				var gameController = GameController.Instance;
				if (gameController)
				{
					Debug.Log("Instantiate PodVisu");

					var podLayout = UnityUtils.InstantiatePrefab<Visuals.Pod>(_podLayoutTemplate);
					if (NetworkInterface.Instance.IsClient)
					{
						Debug.Log("Instantiate PodVisu client");

						var avatarOffset = podLayout.gameObject.AddComponent<FollowAvatarOffset>();
						avatarOffset.SetPlayer(gameController.CurrentPlayerId);
					}
				}
			}
		}

		public void ToggleHapticsVirtualControls()
		{
			var gameController = GameController.Instance;
			if (gameController && (gameController.CurrentSession != null) && (gameController.CurrentSession.Players.Count > 0))
			{
				bool hasVirtualControls = HapticsVirtualControls.GetInstanceForPlayer(gameController.GetPlayerByPlayerId(gameController.CurrentSession.Players[0].ComponentId)) != null;
				foreach (var player in gameController.RuntimePlayers)
				{
					if (hasVirtualControls)
					{
						HapticsVirtualControls.DestroyInstanceForPlayer(player);
					}
					else
					{
						HapticsVirtualControls.CreateInstanceForPlayer(_hapticsVirtualControlsTemplate, player);
					}
				}
			}
		}
	}
}
