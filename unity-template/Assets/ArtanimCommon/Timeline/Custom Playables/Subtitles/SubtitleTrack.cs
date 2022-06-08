using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Artanim
{
	[TrackColor(0f, 0.8f, 1f)]
	[TrackBindingType(typeof(BaseSubtitleDisplayer))]
	[TrackClipType(typeof(SubtitleClip))]
	public class SubtitleTrack : TrackAsset
	{
		//public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
		//{
		//	return ScriptPlayable<SubtitleClipMixerBehaviour>.Create(graph, inputCount);
		//}

		public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
		{
#if UNITY_EDITOR

			var comp = director.GetGenericBinding(this) as Transform;
			if (comp == null)
				return;

			var so = new UnityEditor.SerializedObject(comp);
			var iter = so.GetIterator();
			while (iter.NextVisible(true))
			{
				if (iter.hasVisibleChildren)
					continue;

				driver.AddFromName<Transform>(comp.gameObject, iter.propertyPath);
			}
#endif

			base.GatherProperties(director, driver);
		}

	}
}