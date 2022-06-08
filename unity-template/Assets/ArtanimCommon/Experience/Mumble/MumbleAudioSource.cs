using UnityEngine;
using System;
using System.Runtime.InteropServices;

using Artanim.Location.SharedData;

namespace Artanim
{
	/// <summary>
	/// Mumble Unity AudioSource.
	/// </summary>
	[AddComponentMenu("Artanim/Mumble AudioSource")]
	[RequireComponent(typeof(AudioSource))]
	public class MumbleAudioSource : MonoBehaviour
	{
        private Guid ClientGuid { get;  set; }
        private bool IsHostess { get;  set; }
		private MumbleController MumbleController { get; set; }

		private Artanim.Location.Data.MumbleServerClient _mumbleServerClient = null;
		private Artanim.Location.Mumble.MumbleController _mumbleController;
		private bool _mumbleServerClientsListEventSubsribed = false;

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

		#region Public Interface
		public void Initialize(MumbleController mumbleController, Guid clientGuid, bool isHostess)
		{
			MumbleController = mumbleController;
			ClientGuid = clientGuid;
			IsHostess = isHostess;

			// monitor MumbleServer changes
			MumbleController.OnMumbleServerChanged += OnMumbleServerStarted;

			TryFindClient("Initialize");
        }
        #endregion

        #region Unity events

        private void OnDestroy()
		{
			if (MumbleController != null)
			{
				MumbleController.OnMumbleServerChanged -= OnMumbleServerStarted;

				if (MumbleController.MumbleServer != null)
				{
					MumbleController.MumbleServer.Clients.SharedDataListChanged -= OnMumbleServerClientsListChanged;
				}
			
				MumbleController = null;
			}

			_mumbleServerClient = null;
			_mumbleController = null;
					
			_mumbleServerClientsListEventSubsribed = false;

			StopAudioSource();
		}

		void OnAudioFilterRead(float[] data, int channels)
		{
			if(_mumbleController != null && _mumbleServerClient != null && _mumbleServerClient.UiSession !=0)
			{
				int frameCount = data.Length / channels;
                int frames;

				//frames = _mumbleController.GetMixedAudioData(data, frameCount, channels);
				frames = _mumbleController.GetPlayerAudioData(_mumbleServerClient.UiSession, data, frameCount, channels);

				for (int i = frames; i < frameCount; ++i)
				{
					{
						for (int channel = 0; channel < channels; ++channel)
							data[i * channels + channel] = 0.0f;
					}
				}
			}
			else{
				for (int i = 0; i < data.Length; ++i)
					data[i] = 0.0f;
			}
		}

		#endregion

		#region Internals
		private void OnMumbleServerStarted()
		{
			TryFindClient("OnMumbleServerStarted");
		}

        private void TryFindClient(string reason)
        {
			MumbleController.MumbleLogFormat("Begin TryFindClient caused by {0}: ClientGuid={1}, IsHostess={2}", reason, ClientGuid, IsHostess);

			if (MumbleController.MumbleServer == null)
			{
				MumbleController.MumbleLogFormat("TryFindClient caused by {0}: No MumbleServer. ClientGuid={1}, IsHostess={2}", reason, ClientGuid, IsHostess);
				StopAudioSource();
				_mumbleServerClient = null;
				_mumbleServerClientsListEventSubsribed = false;
			}
			else
			{
				if (IsHostess)
				{
					_mumbleServerClient = MumbleController.MumbleServer.Clients.Find(c => c.IsHostess);
				}
				else
				{
					_mumbleServerClient = MumbleController.MumbleServer.Clients.Find(c => c.ComponentId == ClientGuid);
				}

				if (_mumbleServerClient == null)
				{
					if (_mumbleServerClientsListEventSubsribed == false)
					{
						MumbleController.MumbleServer.Clients.SharedDataListChanged += OnMumbleServerClientsListChanged;
						_mumbleServerClientsListEventSubsribed = true;
					}

					MumbleController.MumbleLogFormat("TryFindClient caused by {0}: Client not found. ClientGuid={1}, IsHostess={2}", reason, ClientGuid, IsHostess);
				}
				else
				{
					MumbleController.MumbleServer.Clients.SharedDataListChanged -= OnMumbleServerClientsListChanged;
					_mumbleServerClientsListEventSubsribed = false;
					_mumbleController = Artanim.Location.Mumble.MumbleController.Instance;
					AudioSource.Play();
					MumbleController.MumbleLogFormat("TryFindClient caused by {0}: Client found. ClientGuid={1}, IsHostess={2}", reason, ClientGuid, IsHostess);
				}
			}

			MumbleController.MumbleLogFormat("End TryFindClient caused by {0}: ClientGuid={1}, IsHostess={2}", reason, ClientGuid, IsHostess);
		}

		private void OnMumbleServerClientsListChanged(object sender, Location.Data.SharedDataListChangedEventArgs e)
        {
			TryFindClient("OnMumbleServerClientsListChanged");
		}

        private void StopAudioSource()
		{
			AudioSource.Stop();
		}
#endregion
	}
}
