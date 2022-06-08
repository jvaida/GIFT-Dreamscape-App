//#define TALK_SIN

using UnityEngine;
using DisruptorUnity3d;
using System;
using System.Runtime.InteropServices;

using anyID = System.UInt16;
using uint64 = System.UInt64;
using Artanim.Location.Data;
using UnityEngine.UI;
using Artanim.Monitoring;

namespace Artanim
{
	/// <summary>
	/// Teamspeak Unity AudioSource integration based on Teamspeak example TSObject.
	/// </summary>
	[AddComponentMenu("Artanim/Teamspeak AudioSource")]
	[RequireComponent(typeof(AudioSource))]
	public class TeamSpeakAudioSource : MonoBehaviour
	{
		private const int SAMPLE_RATE = 48000;

        public bool MuteOutput = false;
		[Range(0f, 1f)]
		public float Amplitude = 1;

        public anyID ClientId { get; private set; }
		public bool IsHostessSource { get; private set; }

		private RingBuffer<short> RingBuffer;

		private AudioSource _audioSource;
		private AudioSource AudioSource
		{
			get
			{
				if(!_audioSource)
				{
					_audioSource = GetComponent<AudioSource>();
				}
				return _audioSource;
			}
		}

		private TalkStatus _talkStatus = TalkStatus.STATUS_NOT_TALKING;
		public TalkStatus TalkStatus
		{
			get { return _talkStatus; }
			private set
			{
				if (TalkStatus == value)
					return;

				_talkStatus = value;
			}
		}

		public int CurrentBufferSize
		{
			get
			{
				return RingBuffer != null ? RingBuffer.Count : 0;
			}
		}

		#region Public Interface

		public void Initialize(anyID tsClientId, bool hostessSource)
		{
			RingBuffer = new RingBuffer<short>(SAMPLE_RATE);

			IsHostessSource = hostessSource;
			ClientId = !IsHostessSource ? tsClientId : (anyID)0;
			
			AudioSource.Play();

			//Connect TS events
			Debug.LogFormat("TS callback connected for TSClientId={0}, IsHostessSource={1}", ClientId, IsHostessSource);
			TeamSpeakCallbacks.onTalkStatusChangeEvent += OnTalkStatusChanged;
			TeamSpeakCallbacks.onEditPlaybackVoiceDataEvent += OnEditPlaybackVoiceData;
        }

        #endregion

        #region Unity events

        private void OnDestroy()
		{
			StopAudioSource();
		}

#if !TALK_SIN
		void OnAudioFilterRead(float[] data, int channels)
		{
            int frameCount = data.Length / channels;
            for (int i = 0; i < frameCount; ++i)
            {
                short tmp;
                if (RingBuffer.TryDequeue(out tmp))
                {
                    if (MuteOutput)
                        tmp = 0;

                    float tmp_f = (float)tmp / (tmp < 0 ? System.Int16.MinValue * -1 : System.Int16.MaxValue);
                    tmp_f = Mathf.Clamp(tmp_f, -1f, 1f);
                        
                    for (int channel = 0; channel < channels; ++channel)
                    {
                        data[i * channels + channel] = tmp_f; // ToDo: while the channels are monaural, we don't receive the 1.0f as expected. Envelope for avoiding clicks? rollOff still pre (would be ok)?
                    }
                }
                else
                {
                    for (int channel = 0; channel < channels; ++channel)
                        data[i * channels + channel] = 0.0f;
                }
            }
		}

#else
		public double Frequency = 440;
		public double Gain = 0.8;

		private double Increment;
		private double Phase;
		private double Sampling_Frequency = 48000;

		void OnAudioFilterRead(float[] data, int channels)
		{
			if (TalkStatus == TalkStatus.STATUS_TALKING)
			{
				// update increment in case frequency has changed
				Increment = Frequency * 2 * Math.PI / Sampling_Frequency;
				for (var i = 0; i < data.Length; i = i + channels)
				{
					Phase = Phase + Increment;
					// this is where we copy audio data to make them “available” to Unity
					data[i] = (float)(Gain * Math.Sin(Phase));
					// if we have stereo, we copy the mono data to each channel
					if (channels == 2) data[i + 1] = data[i];
					if (Phase > 2 * Math.PI) Phase = 0;
				}
			}
		}
#endif


		#endregion

		#region Teamspeak events

		void OnTalkStatusChanged(uint64 serverConnectionHandlerID, int status, int isReceivedWhisper, anyID clientID)
		{
			if (NeedProcessing(clientID))
			{
				Debug.LogFormat("Talk status changed for clientId={0} to {1}", ClientId, (TalkStatus)status);
				TalkStatus = (TalkStatus)status;
			}
		}

		void OnEditPlaybackVoiceData(uint64 serverConnectionHandlerID, anyID clientID, IntPtr samples, int frameCount, int channels)
		{
			if (!NeedProcessing(clientID))
				return;

			int sampleCount = frameCount * channels;
			short[] samples_in_array = new short[sampleCount];
			Marshal.Copy(samples, samples_in_array, 0, sampleCount);

            int enqueuedCount = 0;
            int failedEnqueue = 0;
            //foreach (short sample in samples_in_array)
            for(var i=0; i < sampleCount; ++i)
            {
                if (RingBuffer.TryEnqueue((short)(samples_in_array[i] * Amplitude)))
                    enqueuedCount++;
                else
                    failedEnqueue++;
            }

            if (failedEnqueue > 0)
                Debug.LogErrorFormat("Couldn't enqueue {0} samples", failedEnqueue);
        }

#endregion

#region Internals

		private void StopAudioSource()
		{
			AudioSource.Stop();

			//Detach TS listeners
			TeamSpeakCallbacks.onTalkStatusChangeEvent -= OnTalkStatusChanged;
			TeamSpeakCallbacks.onEditPlaybackVoiceDataEvent -= OnEditPlaybackVoiceData;
			Debug.LogFormat("Audiosource stopped for TSClientId={0}, IsHostessSource={1}", ClientId, IsHostessSource);
		}

		private bool NeedProcessing(anyID clientId)
		{
			return clientId == ClientId || (IsHostessSource && IsHostess(clientId));
		}

		private bool IsHostess(anyID clientId)
		{
			var nickName = TeamSpeakClient.GetInstance().GetClientVariableAsString(clientId, ClientProperties.CLIENT_NICKNAME);
			return !string.IsNullOrEmpty(nickName) ? nickName.StartsWith(ELocationComponentType.HostessApp.ToString()) : false;
		}

#endregion
	}
}
