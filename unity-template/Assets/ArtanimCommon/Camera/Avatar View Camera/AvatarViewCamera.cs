using Artanim.Location.Data;
using Artanim.Location.SharedData;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Artanim
{
    [RequireComponent(typeof(Camera))]
	public class AvatarViewCamera : MonoBehaviour
	{
		public Text TextPlayerName;

        private Camera _Camera;
        private Camera Camera
        {
            get
            {
                if (!_Camera)
                    _Camera = GetComponent<Camera>();
                return _Camera;
            }
        }

		private RuntimePlayer Player;
		private AvatarViewCameraController ViewController;
        private bool IsFirst;

        public void SetPlayer(RuntimePlayer player, AvatarViewCameraController controller, bool isFirst)
		{
            IsFirst = isFirst;

			if(player != null)
			{
				Player = player;
				ViewController = controller;
				if (TextPlayerName)
				{
					var client = SharedDataUtils.FindLocationComponent(Player.Player.ComponentId);
					if(client != null)
					{
						var playerDisplay = "";

						var skeleton = SharedDataUtils.FindChildSharedData<SkeletonConfig>(player.Player.SkeletonId);
						if (skeleton != null && skeleton.Number > 0)
							playerDisplay += string.Format("{0}: ", skeleton.Number);

						var playerName = "";
						if (!string.IsNullOrEmpty(Player.Player.Firstname) || !string.IsNullOrEmpty(Player.Player.Lastname))
							playerName += string.Format("{0} {1}", Player.Player.Firstname, Player.Player.Lastname);

						if (string.IsNullOrEmpty(playerName))
							playerName = client.SharedId.ToString();

						TextPlayerName.text = playerDisplay + playerName;
					}
				}
			}
		}

		private void OnPreRender()
		{
			ViewController.PrepareAvatarVisibility(Player.Player.ComponentId);

            if(IsFirst)
                GL.Clear(false, true, Color.black);
        }

        private void OnPostRender()
		{
			ViewController.ResetAvatarsVisibility();
		}
	}
}