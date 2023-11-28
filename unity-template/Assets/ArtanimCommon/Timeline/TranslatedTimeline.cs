using Artanim;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Video;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Artanim
{
	[AddComponentMenu("Artanim/Translated Timeline")]
	[RequireComponent(typeof(PlayableDirector))]
	public class TranslatedTimeline : MonoBehaviour
	{
		[Tooltip("The path within the Unity resource folder where the language specific timeline playable are searched.")]
		public string TimelineResourcePath;

		private PlayableDirector _Director;
		private PlayableDirector Director
		{
			get
			{
				if (!_Director)
					_Director = GetComponent<PlayableDirector>();
				return _Director;
			}
		}

		private TimelineAsset OriginalTimeline;

		#region Unity events

		void Awake()
		{
			//Keep original playable
			OriginalTimeline = Director.playableAsset as TimelineAsset;

			var doPlay = Director.state == PlayState.Playing;

			//Translate timeline
			TranslateTimeline();

			if (doPlay)
				Play();
		}

		private void OnEnable()
		{
			TextService.OnLanguageChanged += Instance_OnLanguageChanged;
		}

		private void OnDisable()
		{
			TextService.OnLanguageChanged -= Instance_OnLanguageChanged;
		}

		private void Instance_OnLanguageChanged(string language)
		{
			TranslateTimeline();
		}

		#endregion

		#region Public interface

		/// <summary>
		/// Plays the playable director.
		/// </summary>
		public void Play()
		{
			Director.Play();
		}

		/// <summary>
		/// Pauses the playable director.
		/// </summary>
		public void Pause()
		{
			Director.Pause();
		}

		/// <summary>
		/// Stops the playable director.
		/// </summary>
		public void Stop()
		{
			Director.Stop();
		}

		#endregion

		#region Internals

		private void TranslateTimeline()
		{
			//Reset timeline to 0
			Director.Stop();
			Director.time = 0f;

			//Reset to original playable asset
			Director.playableAsset = OriginalTimeline;

			//Replace the whole timeline with language specific one
			ReplaceTimeline();
		}

		private void ReplaceTimeline()
		{
			if (!string.IsNullOrEmpty(TimelineResourcePath))
			{
				var langSpecificTimeline = ResourceUtils.GetPlayerLanguageResource(OriginalTimeline.name, TimelineResourcePath, OriginalTimeline);
				if (langSpecificTimeline)
				{
					//Keep original timeline bindings
					var origBindings = new List<Object>();
					var timeline = Director.playableAsset as TimelineAsset;
					foreach (var track in timeline.GetOutputTracks())
						origBindings.Add(Director.GetGenericBinding(track));

					Director.playableAsset = langSpecificTimeline;

					//Set bindings
					var timelineTracks = langSpecificTimeline.GetOutputTracks().ToArray();
					if (timelineTracks.Count() == origBindings.Count())
					{
						for (var i = 0; i < timelineTracks.Count(); ++i)
						{
							//Debug.LogErrorFormat("Setting binding: key={0}, value={1}", timelineTracks[i] ? timelineTracks[i].name : "null", origBindings[i] ? origBindings[i].name : "null");
							Director.SetGenericBinding(timelineTracks[i], origBindings[i]);
						}

						Debug.LogFormat("Replaced timeline playable with language specific one: playable={0}", langSpecificTimeline.name);
					}
					else
					{
						Debug.LogError("The language specific timeline does not have the same amount of tracks. Language specific timelines must have the same number and sequence of tracks.");
					}
				}
			}
		}

		#endregion
	}
}