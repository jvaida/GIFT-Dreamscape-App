using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Artanim.Location.Data;
using Artanim.Location.Network;
using Artanim.Location.SharedData;
using Artanim.Tracking;
using System;
using UnityEngine.SceneManagement;
using Artanim.Location.Messages;

namespace Artanim
{

	[RequireComponent(typeof(TrackingController))]
	public class ObserverController : MonoBehaviour
	{
		private const string NAME_OBSERVER_VIEW = "Observer";

		public Dropdown DropdownSessions;
		public Dropdown DropdownViews;

        [Header("UI Hiding")]
        public bool HideUIWhenInactive = true;
		public float UIHideTime = 5f;
        public LayerMask UILayers;

		private Session SelectedSession;
		private ExperienceClient SelectedView;

		private bool Initialized;
		private bool UIHidden;
		
		void OnEnable()
		{
			//Hook on session added/removed to refresh GUI
			SharedDataUtils.SessionAdded += SharedDataUtils_SessionAdded;
			SharedDataUtils.SessionRemoved += SharedDataUtils_SessionRemoved;
			//Hook on player & skeleton added/removed to refresh GUI
			SharedDataUtils.ComponentAdded += SharedDataUtils_ComponentOrSkeletonAddedOrRemoved;
			SharedDataUtils.ComponentRemoved += SharedDataUtils_ComponentOrSkeletonAddedOrRemoved;
			SharedDataUtils.SkeletonAdded += SharedDataUtils_ComponentOrSkeletonAddedOrRemoved;
			SharedDataUtils.SkeletonRemoved += SharedDataUtils_ComponentOrSkeletonAddedOrRemoved;
			SceneController.Instance.OnSceneLoaded += OnSceneLoaded;

			NetworkInterface.Instance.Subscribe<SetObserverView>(OnSetObserverView);
			NetworkInterface.Instance.Subscribe<ComponentJoinedSession>(OnComponentJoinedSession);

			//Note: when starting the observer, it's possible we pick up a session before getting the player, so we need to be notified when we pick up the player too

			UpdateSessions();

		}
		
		void OnDisable()
		{
			SharedDataUtils.SessionAdded -= SharedDataUtils_SessionAdded;
			SharedDataUtils.SessionRemoved -= SharedDataUtils_SessionRemoved;
			SharedDataUtils.ComponentAdded -= SharedDataUtils_ComponentOrSkeletonAddedOrRemoved;
			SharedDataUtils.ComponentRemoved -= SharedDataUtils_ComponentOrSkeletonAddedOrRemoved;
			SharedDataUtils.SkeletonAdded -= SharedDataUtils_ComponentOrSkeletonAddedOrRemoved;
			SharedDataUtils.SkeletonRemoved -= SharedDataUtils_ComponentOrSkeletonAddedOrRemoved;

			NetworkInterface.SafeUnsubscribe<SetObserverView>(OnSetObserverView);
			NetworkInterface.SafeUnsubscribe<ComponentJoinedSession>(OnComponentJoinedSession);

			if (SceneController.Instance)
				SceneController.Instance.OnSceneLoaded -= OnSceneLoaded;
		}

		private void Update()
		{
			UpdateIdleTime();
		}

		#region Events

		private void OnSetObserverView(SetObserverView args)
		{
			if(args != null)
			{
				//Debug.LogFormat("OnSetObserverView: session={0}, client={1}", args.ViewSessionId, args.ViewExperienceClientId);
				SwitchSession(args.ViewSessionId, args.ViewExperienceClientId);
			}
		}

		private void OnComponentJoinedSession(ComponentJoinedSession args)
		{
			if (args != null)
			{
				//Debug.LogFormat("OnComponentJoinSession: session={0}, client={1}", args.ComponentId, args.SessionId);
				SwitchSession(args.SessionId);
			}
		}

		private void SharedDataUtils_SessionAdded(BaseSharedData sharedData)
		{
			(sharedData as Session).PropertyChanged += SelectedSession_PropertyChanged;
			UpdateSessions();
		}

		private void SharedDataUtils_SessionRemoved(BaseSharedData sharedData)
		{
			(sharedData as Session).PropertyChanged -= SelectedSession_PropertyChanged;
			UpdateSessions();
		}

		private void SharedDataUtils_ComponentOrSkeletonAddedOrRemoved(BaseSharedData sharedData)
		{
			UpdateViews();
		}
		
		private void SelectedSession_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if(e.PropertyName == "Status")
			{
				foreach (SessionOptionData opt in DropdownSessions.options)
				{
					opt.Refresh();
				}
				DropdownSessions.RefreshShownValue();
			}
		}

		private void Players_SharedDataListChanged(object sender, SharedDataListChangedEventArgs e)
		{
			UpdateViews();
		}

		private void SelectedPlayer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Status")
			{
				foreach (ViewOptionData opt in DropdownViews.options)
				{
					opt.Refresh();
				}
				DropdownViews.RefreshShownValue();
			}
		}

		private void OnSceneLoaded(string sceneName, Scene scene, bool isMainChildScene)
		{
			ShowHideUI(!UIHidden);
		}

		#endregion

		#region Public interface

		public void DoSelectSession(bool forceSelection = true)
		{
			if (DropdownSessions && DropdownSessions.options.Count > 0)
			{
				var sessionOptionData = DropdownSessions.options[DropdownSessions.value] as SessionOptionData;
				if(sessionOptionData != null && sessionOptionData.Session != null)
				{
					var session = sessionOptionData.Session;
					if (SelectedSession == null || session != SelectedSession)
					{
						//Detach update events
						if(SelectedSession != null)
						{
							SelectedSession.Players.SharedDataListChanged -= Players_SharedDataListChanged;
						}

						//Select
						SelectedSession = session;
						Debug.LogFormat("Session selected: sessionId={0}", SelectedSession.SharedId);

						//Attach update events
						SelectedSession.Players.SharedDataListChanged += Players_SharedDataListChanged;
					}
				}
				else
				{
					Debug.LogWarningFormat("Failed to find session data in option index: {0}", DropdownSessions.value);
				}
			}
			else
			{
				//Deselect session
				SelectedSession = null;
			}

			//Update client views
			UpdateViews(forceSelection);
		}


		public void DoSelectView(bool forceSelection = true)
		{
			if (SelectedSession != null && DropdownViews && DropdownViews.options.Count > 0)
			{
				DropdownViews.RefreshShownValue();

				var viewOption = DropdownViews.options[DropdownViews.value] as ViewOptionData;
				if(viewOption != null)
				{
					if(SelectedView != viewOption.Client || !Initialized || forceSelection)
					{
						Debug.LogFormat("View selected: session={0} view={1}", SelectedSession.SharedId.Description, SelectedView != null ? SelectedView.SharedId.Description : NAME_OBSERVER_VIEW);
						if (IsDataValid(SelectedSession, viewOption.Client))
						{
							SelectedView = viewOption.Client;
							GameController.Instance.ObserveSession(SelectedSession, SelectedView);
							Initialized = true;
						}
					}
				}
			}
		}

        public void DoSwitchToObserver()
        {
            DropdownViews.value = 0;
        }

        #endregion

        #region UI Hiding

        private float NextHideUITime;
		private Vector3 LastMousePosition;
		private void UpdateIdleTime()
		{
            if (!HideUIWhenInactive)
                return;

			if(Input.mousePosition == LastMousePosition && !UIHidden && Time.unscaledTime > UIHideTime)
			{
				//Mouse did not move... is ui already hidden?
				if(!UIHidden)
				{
					//Need to hide?
					if(Time.unscaledTime > NextHideUITime)
					{
						//Hide UI
						UIHidden = true;

						//Only hide world space canvass
						ShowHideUI(false);
					}
				}
			}
			else if(Input.mousePosition != LastMousePosition && Application.isFocused)
			{
				if(UIHidden)
				{
					//Show UI
					UIHidden = false;

					//Only hide world space canvas
					ShowHideUI(true);
				}

				//Update state
				NextHideUITime = Time.unscaledTime + UIHideTime;
			}

			LastMousePosition = Input.mousePosition;
		}

		private void ShowHideUI(bool show)
		{
			//Close dropdowns first to avoid to bug them later
			foreach(var dropdown in FindObjectsOfType<Dropdown>())
			{
                if (UILayers.value == (UILayers.value | (1 << dropdown.gameObject.layer)))
				    dropdown.Hide();
			}

			//Hide all screenspace canvas
			foreach (var canvas in FindObjectsOfType<Canvas>())
			{
				if (canvas.renderMode != RenderMode.WorldSpace && UILayers.value == (UILayers.value | (1 << canvas.gameObject.layer)))
					canvas.enabled = show;
			}
		}

        #endregion


        // Switch to given session and select a player's view, or the default view if the Guid is nil
        private void SwitchSession(Guid sessionId, Guid clientGuid = default(Guid))
		{
			//Find the session in dropdown options
			var sessionData = DropdownSessions.options.OfType<SessionOptionData>().FirstOrDefault(s => s.Session.SharedId.Guid == sessionId);
			if (sessionData != null)
			{
				//Switch session...
				DropdownSessions.value = DropdownSessions.options.IndexOf(sessionData);

                //Find corresponding view
                ViewOptionData viewData = null;
                if (clientGuid != Guid.Empty)
                    viewData = DropdownViews.options.OfType<ViewOptionData>().FirstOrDefault(v => v.Client != null && (clientGuid == Guid.Empty || clientGuid == v.Client.SharedId.Guid));
                else if(DropdownViews.options.Count > 0)
                    viewData = DropdownViews.options[0] as ViewOptionData;

				//Find / switch client view
				if (viewData != null)
				{
					Debug.LogFormat("Switching to view: session={0}, view={1}", sessionData.Session.SharedId, viewData.Client != null ? viewData.Client.SharedId : null);

					//Set view
					DropdownViews.value = DropdownViews.options.IndexOf(viewData);
				}
				else
				{
					Debug.LogErrorFormat("Unable to switch to view. View was not found in options: viewClientId={0}", clientGuid);
				}
			}
			else
			{
				Debug.LogErrorFormat("Unable to switch to session. Session was not found in options: sessionId={0}", sessionId);
			}
		}

		private void UpdateSessions()
		{
			Debug.Log("Updating sessions");

			//Session drop-down
			if (DropdownSessions)
			{
				//Remove old
				DropdownSessions.ClearOptions();

				//Add session
				foreach (var session in SharedDataUtils.Sessions)
				{
					DropdownSessions.options.Add(new SessionOptionData(session));
				}

				DropdownSessions.RefreshShownValue();

				//Check current selection
				if (DropdownSessions.value >= DropdownSessions.options.Count())
				{
					DropdownSessions.value = DropdownSessions.options.Count() - 1;
				}
			}

			//Select value
			DoSelectSession(false);
		}

		private void UpdateViews(bool forceSelection = true)
		{
			Debug.Log("Updating views");

			if (DropdownViews && DropdownSessions)
			{
				//Remove old
				DropdownViews.ClearOptions();

				if (SelectedSession != null)
				{
					//Add observer view
					DropdownViews.options.Add(new ViewOptionData());

					var clientsQuery = from p in SelectedSession.Players
									   from c in SharedDataUtils.Components.OfType<ExperienceClient>()
									   where c.SharedId.Guid == p.ComponentId
									   select c;

					//Add session players
					foreach (var client in clientsQuery)
					{
						DropdownViews.options.Add(new ViewOptionData(client));
					}
					DropdownViews.RefreshShownValue();

					//Check current selection
					if (DropdownViews.value >= DropdownViews.options.Count())
					{
						DropdownViews.value = 0; //Observer is always there
					}
				}
			}

			//Select value
			DoSelectView(forceSelection);
		}

		private bool IsDataValid(Session session, ExperienceClient view)
		{
			//Check session
			if (session == null)
			{
				Debug.LogWarning("Observer data validation failed: session is null");
				return false;
			}

			//Check if skeletons in session are
			foreach (var sessionPlayer in session.Players)
			{
				if (SharedDataUtils.FindChildSharedData<SkeletonConfig>(sessionPlayer.SkeletonId) == null)
				{
					//Debug.LogWarningFormat("Observer data validation failed: session player skeleton not found: sharedId={0}", sessionPlayer.SkeletonId);
					return false;
				}
			}

			//Check view
			if (view != null) //null view = Observer
			{
				var player = session.Players.FirstOrDefault(p => p.ComponentId == view.SharedId);
				//Check if client is in session
				if (player == null)
				{
					Debug.LogWarning("Observer data validation failed: client is not in session");
					return false;
				}
			}

			return true;
		}

		public class SessionOptionData : Dropdown.OptionData
		{
			public Session Session;

			public SessionOptionData(Session session) : base()
			{
				Session = session;
				Refresh();
			}

			public void Refresh()
			{
				text = Session != null ? string.Format("{0} : {1}", Session.SharedId.Description, Session.Status.ToString()) : "null";
			}
		}

		public class ViewOptionData : Dropdown.OptionData
		{
			public ExperienceClient Client;

			public ViewOptionData(ExperienceClient client = null) : base()
			{
				Client = client;
				Refresh();
			}

			public void Refresh()
			{
				text = Client != null ? string.Format("{0}", Client.SharedId.Description) : NAME_OBSERVER_VIEW;
			}
		}
	}

}