using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Artanim
{
	[TrackColor(0f, 0.6f, 1f)]
	[TrackClipType(typeof(TranslatedAudioClip))]
	[TrackBindingType(typeof(TranslatedAudioSource))]
	public class TranslatedAudioTrack : TrackAsset
	{

	}

}