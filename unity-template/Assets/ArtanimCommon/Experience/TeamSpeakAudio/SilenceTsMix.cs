using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Linq;

using anyID = System.UInt16;
using uint64 = System.UInt64;

namespace Artanim
{
	public class SilenceTsMix : MonoBehaviour
	{
		private short[] silentArray = Enumerable.Repeat<short>(0, 2048).ToArray();

		private void OnEnable()
		{
			TeamSpeakCallbacks.onEditMixedPlaybackVoiceDataEvent += onEditMixedPlaybackVoiceData;
		}

		private void OnDisable()
		{
			TeamSpeakCallbacks.onEditMixedPlaybackVoiceDataEvent -= onEditMixedPlaybackVoiceData;
		}

		void onEditMixedPlaybackVoiceData(uint64 serverConnectionHandlerID, IntPtr samples, int frameCount, int channels, uint[] channel_speaker_array, ref uint channel_fill_mask)
		{
			int sampleCount = frameCount * channels;
			if (silentArray.Length < sampleCount)
				Array.Resize(ref silentArray, sampleCount);

			Marshal.Copy(silentArray, 0, samples, sampleCount);
		}
	}
}