using Artanim;
using Artanim.Location.Messages;
using Artanim.Location.Network;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_2017_3_OR_NEWER
using UnityEngine.XR;
#else
using UnityEngine.VR;
using XRNode = UnityEngine.VR.VRNode;
using XRNodeState = UnityEngine.VR.VRNodeState;
using XRSettings = UnityEngine.VR.VRSettings;
using XRDevice = UnityEngine.VR.VRDevice;
#endif

namespace Artanim
{
	public class SubtitleController : SingletonBehaviour<SubtitleController>
	{
        private bool _ShowSubtitles = true;
		public bool ShowSubtitles
        {
            get { return _ShowSubtitles; }
            set
            {
                _ShowSubtitles = value;
                if (!_ShowSubtitles)
                    HideSubtitle();
            }
        }


		[Tooltip("SDK default VR subtitle displayer")]
		public GameObject DefaultVRSubtitleDisplayerTemplate;

		[Tooltip("SDK default observer subtitle displayer")]
		public GameObject DefaultObserverSubtitleDisplayerTemplate;

		[Tooltip("Subtitle displayer used in the editor")]
		public GameObject EditorSubtitleDisplayer;

		private BaseSubtitleDisplayer _SubtitleDisplayer;
		private BaseSubtitleDisplayer SubtitleDisplayer
		{
			get
			{
				if (_SubtitleDisplayer == null)
				{
					InitSubtitleDisplayer();
				}
				return _SubtitleDisplayer;
			}

			set
			{
				_SubtitleDisplayer = value;
			}
		}


		#region Unity events

		private void Start()
		{
			InitSubtitleDisplayer();
		}

		private void OnEnable()
		{
			// Can't access NetworkInterface if not playing
			if(Application.isPlaying)
			{
				if(NetworkInterface.Instance.IsConnected)
					OnNetworkConnected();
				else
					NetworkInterface.Instance.Connected += OnNetworkConnected;
			}
		}

		private void OnDisable()
		{
			NetworkInterface.SafeUnsubscribe<ShowSubtitle>(OnShowSubtitle);
		}

		private void OnNetworkConnected()
		{
			NetworkInterface.Instance.Subscribe<ShowSubtitle>(OnShowSubtitle);
		}

		#endregion

		#region Public interface

		/// <summary>
		/// Displays a subtitle to the player.
		/// </summary>
		/// <param name="subtitle">Subtitle or textId to display</param>
		/// <param name="isTextId">Indicates if the given subtitle is a textId to be translated</param>
		/// <param name="displayDuration">Duration in seconds to display the subtitle</param>
		/// <param name="keepSubtitleBackground">Should the subtitle background stay visible after the displayDuration</param>
		/// <param name="forceDisplay">Forces to display the subtitle even if the player does not see subtitles</param>
		/// <param name="syncToSession">Show this subtitle on all session components. True will only have an effect on the server side</param>
		/// <param name="displayer">The displayer to be used to hide the subtitle. Default: SDK default displayer</param>
		public void ShowSubtitle(string subtitle, bool isTextId=false, float displayDuration=0, bool keepSubtitleBackground=false, bool forceDisplay=false, bool syncToSession = false, BaseSubtitleDisplayer displayer=null)
		{
			var networkInterface = NetworkInterface.Instance;
			if(syncToSession && networkInterface != null && networkInterface.IsServer)
			{
				//Send sync message to session
				networkInterface.SendMessage(new ShowSubtitle
				{
					Action = Location.Messages.ShowSubtitle.EAction.Show,
					Subtitle = subtitle,
					IsTextId = isTextId,
					DisplayDuration = displayDuration,
					KeepSubtitleBackground = keepSubtitleBackground,
					ForceDisplay = forceDisplay,
					DisplayerId = displayer != null ? displayer.GetDisplayerId() : null,
				});
			}
			else
			{
				//Show subtitle locally
				if (SubtitleDisplayer != null || displayer != null)
				{
					if (ShowSubtitles || forceDisplay)
					{
						//Stop already started hide routines
						StopAllCoroutines();

						//Translate subtitle
						if (isTextId)
						{
							subtitle = TextService.Instance.GetText(subtitle);
						}

						//Display subtitle
						if (displayer != null)
							displayer.ShowSubtitle(subtitle);
						else
							SubtitleDisplayer.ShowSubtitle(subtitle);

						//Display duration
						if (displayDuration > 0)
						{
							StartCoroutine(DelayedHideSubtitle(displayDuration, keepSubtitleBackground));
						}
					}
				}
			}
		}

		
		/// <summary>
		/// Hides the currently displayed subtitle.
		/// </summary>
		/// <param name="keepSubtitleBackground">Indicated if the subtitle background should be kept visible</param>
		/// <param name="syncToSession">Sync to the session compoents. If set to true, the method can only be called on the server</param>
		/// <param name="displayer">The displayer to be used to hide the subtitle. Default: SDK default displayer</param>
		public void HideSubtitle(bool keepSubtitleBackground = false, bool syncToSession = false, BaseSubtitleDisplayer displayer = null)
		{
			if (syncToSession)
			{
				if (NetworkInterface.Instance.IsServer)
				{
					//Send sync message to session
					NetworkInterface.Instance.SendMessage(new ShowSubtitle
					{
						Action = Location.Messages.ShowSubtitle.EAction.Hide,
						KeepSubtitleBackground = keepSubtitleBackground,
						DisplayerId = displayer != null ? displayer.GetDisplayerId() : null,
					});
				}
			}
			else
			{
				if (SubtitleDisplayer != null || displayer != null)
				{
					//Stop already started hide routines
					StopAllCoroutines();

					//Hide subtitle
					if (displayer != null)
						displayer.HideSubtitle(keepSubtitleBackground);
					else
						SubtitleDisplayer.HideSubtitle(keepSubtitleBackground);
				}
			}
		}

		#endregion

		#region Location events

		private void OnShowSubtitle(ShowSubtitle args)
		{
			BaseSubtitleDisplayer displayer = null;
			if (args.DisplayerId != null)
			{
				//Ok.. we need to search the correct displayer
				displayer = FindObjectsOfType<BaseSubtitleDisplayer>().FirstOrDefault(d => d.GetDisplayerId() == args.DisplayerId);
			}

			switch (args.Action)
			{
				case Location.Messages.ShowSubtitle.EAction.Show:

					ShowSubtitle(args.Subtitle,
						isTextId: args.IsTextId,
						displayDuration: args.DisplayDuration,
						keepSubtitleBackground: args.KeepSubtitleBackground,
						forceDisplay: args.ForceDisplay,
						displayer: displayer);

					break;
				case Location.Messages.ShowSubtitle.EAction.Hide:

					HideSubtitle(args.KeepSubtitleBackground, displayer: displayer);

					break;
			}
		}

#endregion

		#region Internals

		private IEnumerator DelayedHideSubtitle(float hideAfter, bool keepSubtitleBackground)
		{
			yield return new WaitForSecondsRealtime(hideAfter);

			HideSubtitle(keepSubtitleBackground);

			yield return null;
		}
		
		private void InitSubtitleDisplayer()
		{
#if UNITY_EDITOR
			if(!UnityEditor.EditorApplication.isPlaying)
			{
				//Editor not playing, use the set editor displayer
				_SubtitleDisplayer = EditorSubtitleDisplayer.GetComponent<BaseSubtitleDisplayer>();
				return;
			}
#endif
			//Search configured displayer or fallback to SDK default

			//Clear old one
			UnityUtils.RemoveAllChildren(transform);
			SubtitleDisplayer = null;

			if (DefaultVRSubtitleDisplayerTemplate)
			{
				GameObject template = null;

				if(ConfigService.Instance.HasExperienceSettings)
				{
					template = XRSettings.enabled ? 
						ConfigService.Instance.ExperienceSettings.VRSubtitleDisplayer : 
						ConfigService.Instance.ExperienceSettings.ObserverSubtitleDisplayer;
				}

				if(!template || template.GetComponent<BaseSubtitleDisplayer>() == null)
				{
					if(template)
					{
						//Template in settings is set but no ISubtitleDisplayer behaviour found!
						Debug.LogWarning("The subtitle displayer set in experience settings does not have a ISubtitleDisplayer behaviour set. Falling back to SDK default displayer.");
					}

					//Fallback to SDK default
					template = XRSettings.enabled ? DefaultVRSubtitleDisplayerTemplate : DefaultObserverSubtitleDisplayerTemplate;
				}
				
				if (template)
				{
					//Create instance
					var subtitleDisplayer = UnityUtils.InstantiatePrefab<BaseSubtitleDisplayer>(template, transform);
					
					//Position
					var behaviour = subtitleDisplayer as MonoBehaviour;
					behaviour.transform.localPosition = Vector3.zero;
					behaviour.transform.localRotation = Quaternion.identity;
					behaviour.transform.localScale = Vector3.one;

					SubtitleDisplayer = subtitleDisplayer;
				}
				else
				{
					//Very bad... the default displayer is not even set!
					Debug.LogError("No subtitle displayer found.");
				}
			}
		}

		#endregion

	}
}