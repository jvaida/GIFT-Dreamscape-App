using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Video;

namespace Artanim
{
	[TrackColor(0f, 0.4f, 1f)]
	[TrackClipType(typeof(TranslatedVideoClip))]
	[TrackBindingType(typeof(TranslatedVideoPlayer))]
	public class TranslatedVideoTrack : TrackAsset
	{

	}
}