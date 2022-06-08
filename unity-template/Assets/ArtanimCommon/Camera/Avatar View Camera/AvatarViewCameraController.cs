using Artanim.Location.SharedData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Artanim
{
    [RequireComponent(typeof(Camera))]
	public class AvatarViewCameraController : MonoBehaviour
	{
		public enum ELookDirection { Front, Back, Left, Right, }

		public GameObject CameraTemplate;

		public ELookDirection LookDirection;

		public Vector3 CameraPositionOffset;

		public GameObject PanelShowPlayers;
		public GameObject PanelPlayerViews;
        public GameObject PanelObserverView;
		public Toggle ToggleShowPlayers;
		public Toggle ToggleOrthographic;

		public Transform[] Targets;
		public Transform[] Cameras;

		public float PlayerViewCullingRange = 0.5f;
		public LayerMask AdditionalCullingMask;

		public float AvatarDistance = 2f;


        private Camera _ObserverCamera;
        private Camera ObserverCamera
        {
            get
            {
                if (!_ObserverCamera)
                    _ObserverCamera = GetComponent<Camera>();
                return _ObserverCamera;
            }
        }

		public void SetViewFront() { LookDirection = ELookDirection.Front; }
		public void SetViewBack() { LookDirection = ELookDirection.Back; }
		public void SetViewLeft() { LookDirection = ELookDirection.Left; }
		public void SetViewRight() { LookDirection = ELookDirection.Right; }

		public void SetOrthographic(bool orthographicView)
		{
			if(Cameras != null)
			{
				foreach (var cam in Cameras)
					cam.GetComponent<Camera>().orthographic = orthographicView;
			}
		}

		public void ShowPlayers(bool showPlayers)
		{
			CreateLayout();
		}

        public void DoToggleShowPlayers()
        {
            if (ToggleShowPlayers)
                ToggleShowPlayers.isOn = !ToggleShowPlayers.isOn;
        }

		private void OnEnable()
		{
			CreateLayout();

			if(GameController.Instance)
			{
				GameController.Instance.OnJoinedSession += Instance_OnJoinedSession;
				GameController.Instance.OnLeftSession += Instance_OnLeftSession;
				GameController.Instance.OnSessionPlayerJoined += Instance_OnSessionPlayerJoined;
				GameController.Instance.OnSessionPlayerLeft += Instance_OnSessionPlayerLeft;
			}
		}
		
		private void OnDisable()
		{
			if(GameController.Instance)
			{
				GameController.Instance.OnJoinedSession -= Instance_OnJoinedSession;
				GameController.Instance.OnLeftSession -= Instance_OnLeftSession;
				GameController.Instance.OnSessionPlayerJoined -= Instance_OnSessionPlayerJoined;
				GameController.Instance.OnSessionPlayerLeft -= Instance_OnSessionPlayerLeft;
			}
		}

        private void Instance_OnJoinedSession(Location.Data.Session session, System.Guid playerId)
		{
            //Switch to avatars view
            if (ToggleShowPlayers)
                ToggleShowPlayers.isOn = true;

            CreateLayout();
		}

		private void Instance_OnLeftSession()
		{
			CreateLayout();
		}

		private void Instance_OnSessionPlayerLeft(Location.Data.Session session, System.Guid playerId)
		{
			CreateLayout();
		}

		private void Instance_OnSessionPlayerJoined(Location.Data.Session session, System.Guid playerId)
		{
			CreateLayout();
		}

		private void LateUpdate()
		{
            if (Targets != null && Targets.Length > 0)
			{
				for (var i = 0; i < Targets.Length; ++i)
				{
					var target = Targets[i];
					if(target)
					{
						PositionCamera(target, Cameras[i]);
					}
				}
			}
		}

		private void PositionCamera(Transform target, Transform camera)
		{
			var direction = GetLookDirection(target);
			direction.Normalize();
			camera.position = direction * AvatarDistance + target.position + CameraPositionOffset;

			camera.LookAt(target.position + CameraPositionOffset);
		}

		private Vector3 GetLookDirection(Transform target)
		{
			switch (LookDirection)
			{
				case ELookDirection.Front:
					return target.forward;
				case ELookDirection.Back:
					return target.forward * -1;
				case ELookDirection.Left:
					return target.right * -1;
				case ELookDirection.Right:
					return target.right;
				default:
					return target.forward;
			}
		}

		private void CreateLayout()
		{
			if(ToggleShowPlayers)
			{
                if (GameController.Instance.CurrentSession == null)
                    ToggleShowPlayers.isOn = false;

				if(ToggleShowPlayers.isOn && GameController.Instance.RuntimePlayers.Count > 0)
				{
                    if (ObserverCamera)
                        ObserverCamera.enabled = false;

					BuildPlayerViews();
				}
				else
				{
                    if (ObserverCamera)
                        ObserverCamera.enabled = true;

                    ClearPlayerViews();
				}

                //Show or hide view controls based on available players and mode
                ShowHideViewControls();
            }
		}

		private void ShowHideViewControls()
		{
			//Show hide view controls in function if there are players available
			if (PanelShowPlayers)
				PanelShowPlayers.SetActive(GameController.Instance.RuntimePlayers.Count > 0);

			if (PanelPlayerViews)
				PanelPlayerViews.SetActive(ToggleShowPlayers.isActiveAndEnabled && ToggleShowPlayers.isOn);

            if (PanelObserverView)
                PanelObserverView.SetActive(ToggleShowPlayers.isActiveAndEnabled && !ToggleShowPlayers.isOn);

        }

		private void BuildPlayerViews()
		{
			if (CameraTemplate)
			{
				//Clear avatar states
				ResetAvatarStates();

				//Clear existing cameras
				ClearPlayerViews();

				//Find all targets and create cameras
				Targets = new Transform[GameController.Instance.RuntimePlayers.Count];
				Cameras = new Transform[Targets.Length];
				for (var p = 0; p < GameController.Instance.RuntimePlayers.Count; ++p)
				{
					var player = GameController.Instance.RuntimePlayers[p];

					//Keep target transform
					Targets[p] = player.AvatarController.GetAvatarRoot();

					//Create camera AvatarViewCamera
					var avatarViewCamera = UnityUtils.InstantiatePrefab<AvatarViewCamera>(CameraTemplate, transform);
					avatarViewCamera.name = string.Format("Camera for {0}", Targets[p].name);

					//Set player name visual
					avatarViewCamera.SetPlayer(player, this, p == 0);

					//Setup camera
					var cam = avatarViewCamera.GetComponent<Camera>();
					cam.cullingMask = 1 << player.PlayerInstance.layer | AdditionalCullingMask.value;
					cam.clearFlags = CameraClearFlags.SolidColor;
					cam.backgroundColor = GameController.Instance.GetPlayerColor(player.Player.ComponentId, Color.black);
					cam.nearClipPlane = AvatarDistance - PlayerViewCullingRange;
					cam.farClipPlane = AvatarDistance + PlayerViewCullingRange;
					if (ToggleOrthographic)
						cam.orthographic = ToggleOrthographic.isOn;

					Cameras[p] = avatarViewCamera.transform;
				}

				//Calc views grid dimension
				var gridDimension = CalcGridDimension(Targets.Length);

				//Layout cameras
				var camIndex = 0;
				var camW = 1f / gridDimension.x;
				var camH = 1f / gridDimension.y;
				for (var line = 0; line < gridDimension.y; ++line)
				{
					for (var col = 0; col < gridDimension.x; ++col)
					{
						if (camIndex < Cameras.Length)
						{
							var camera = Cameras[camIndex].GetComponent<Camera>();

							//Set viewport rect based on grid size
							var camRect = camera.rect;
							camRect.width = camW;
							camRect.height = camH;
							camRect.x = col * camW;
							camRect.y = (gridDimension.y - 1 - line) * camH;
							camera.rect = camRect;
						}

						camIndex++;
					}
				}
			}
			else
			{
				Debug.LogError("No camera template set");
			}
		}

		private void ClearPlayerViews()
		{
			if (Cameras != null)
			{
				// We have issues with the canvas being disabled, trying to work around that problem
				foreach (var cam in Cameras)
				{
					var canvas = cam.GetComponentInChildren<Canvas>();
					if (canvas != null)
                    {
						canvas.renderMode = RenderMode.ScreenSpaceOverlay;
						canvas.gameObject.SetActive(false);
					}
				}

				foreach (var cam in Cameras)
				{
					Destroy(cam.gameObject);
				}

				Cameras = null;
			}

			if(Targets != null)
			{
				Targets = null;
			}
		}

		private Vector2 CalcGridDimension(int numElements)
		{
			var gridDimension = new Vector2();
			gridDimension.y = Mathf.Floor(Mathf.Sqrt(numElements));
			gridDimension.x = Mathf.Floor(Mathf.Ceil(numElements / gridDimension.y));

			Debug.LogFormat("Calculated grid for {0} elements: {0}", numElements, gridDimension);
			return gridDimension;
		}

		#region Avatar view control

		private int LastAvatarRenderersUpdate = -1;
		private Dictionary<Guid, AvatarRenderersState> AvatarRendererStates;

		public void PrepareAvatarVisibility(Guid playerId)
		{
			UpdateAvatarStates();
			
			for(var p = 0; p < GameController.Instance.RuntimePlayers.Count; ++p)
			{
				var player = GameController.Instance.RuntimePlayers[p];

				//Hide all but the requested player
				if(player.Player.ComponentId != playerId)
				{
					var avatarState = AvatarRendererStates[player.Player.ComponentId];
					for (var r = 0; r < player.AvatarController.Renderers.Count; ++r)
					{
						player.AvatarController.Renderers[r].enabled = false;
					}
				}
			}
		}

		public void ResetAvatarsVisibility()
		{
			for (var p = 0; p < GameController.Instance.RuntimePlayers.Count; ++p)
			{
				var player = GameController.Instance.RuntimePlayers[p];

				var avatarState = AvatarRendererStates[GameController.Instance.RuntimePlayers[p].Player.ComponentId];
				for (var r = 0; r < player.AvatarController.Renderers.Count; ++r)
				{
					player.AvatarController.Renderers[r].enabled = avatarState.RendererStates[r];
				}
			}
		}

		private void ResetAvatarStates()
		{
			AvatarRendererStates = new Dictionary<Guid, AvatarRenderersState>();
			LastAvatarRenderersUpdate = -1;
		}

		private void UpdateAvatarStates()
		{
			//Only update state once per frame
			if (LastAvatarRenderersUpdate != Time.frameCount)
			{
				AvatarRenderersState avatarState;
				for (var p = 0; p < GameController.Instance.RuntimePlayers.Count; ++p)
				{
					var player = GameController.Instance.RuntimePlayers[p];

					//Do we already have the state for this avatar?
					if (!AvatarRendererStates.TryGetValue(player.Player.ComponentId, out avatarState))
					{
						avatarState = new AvatarRenderersState();
						avatarState.PlayerId = player.Player.ComponentId;
						AvatarRendererStates.Add(player.Player.ComponentId, avatarState);
					}

					//Update state of the avatar renderers
					if (avatarState.RendererStates == null || avatarState.RendererStates.Length != player.AvatarController.Renderers.Count)
					{
						//Set renderers
						var renderers = player.AvatarController.Renderers;
						avatarState.RendererStates = new bool[renderers.Count];

						for(var r = 0; r < renderers.Count; ++r)
						{
							avatarState.RendererStates[r] = renderers[r].enabled;
						}
					}
					else
					{
						//Just update the current renderer states
						for (var r = 0; r < player.AvatarController.Renderers.Count; ++r)
						{
							avatarState.RendererStates[r] = player.AvatarController.Renderers[r].enabled;
						}
					}

					
				}

				LastAvatarRenderersUpdate = Time.frameCount;
			}
		}

		private class AvatarRenderersState
		{
			public Guid PlayerId { get; set; }
			public bool[] RendererStates { get; set; }
		}
		
		#endregion

	}

}