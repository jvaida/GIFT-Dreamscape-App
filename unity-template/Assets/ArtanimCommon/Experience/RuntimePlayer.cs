using Artanim.Location.Data;
using Artanim.Location.Messages;
using Artanim.Location.Network;
using Artanim.Location.SharedData;
using Artanim.Tracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Artanim
{
	/// <summary>
	/// Holder structure for runtime player information
	/// </summary>
	public class RuntimePlayer
	{
		public RuntimePlayer(bool isMainPlayer, Player player, Transform avatarOffset, AvatarController avatarController, GameObject playerInstance)
		{
			IsMainPlayer = isMainPlayer;
			IsSeated = player.CalibrationMode != ECalibrationMode.Normal;
			IsDesktopAvatar = player.IsDesktop;
			Player = player;
			AvatarOffset = avatarOffset;
			AvatarController = avatarController;
			PlayerInstance = playerInstance;
		}

		public bool IsMainPlayer { get; private set; }

		public bool IsWheelchair
        {
			get 
			{ 
				return Player.CalibrationMode == ECalibrationMode.TrackedWheelchair 
					|| Player.CalibrationMode == ECalibrationMode.UserWheelchair 
					|| Player.CalibrationMode == ECalibrationMode.SeatedExperienceWheelchair; 
			}
        }

		public bool IsSeated { get; private set; }

		public bool IsDesktopAvatar { get; private set; }

		public Player Player { get; private set; }

		public Transform AvatarOffset { get; private set; }

		public AvatarController AvatarController { get; private set; }

		public GameObject PlayerInstance { get; set; }

		public Transform TeamspeakListenerRoot { get; private set; }

		public TeamSpeakAudioSource TSAudioSource { get; private set; }

		public MumbleAudioSource MumbleAudioSource { get; private set; }

		public void SetTeamspeakListenerRoot(Transform teamspeakListenerRoot)
        {
			if (TeamspeakListenerRoot != null) throw new InvalidOperationException("TeamspeakListenerRoot already set");
			TeamspeakListenerRoot = teamspeakListenerRoot;
		}

		public void SetTSAudioSource(TeamSpeakAudioSource tsAudioSource)
        {
			if (TSAudioSource != null) throw new InvalidOperationException("TSAudioSource already set");
			TSAudioSource = tsAudioSource;
		}

		public void ResetTSAudioSource()
        {
			TSAudioSource = null;
		}

		public void SetMumbleAudioSource(MumbleAudioSource mumbleAudioSource)
        {
			if (MumbleAudioSource != null) throw new InvalidOperationException("MumbleAudioSource already set");
			MumbleAudioSource = mumbleAudioSource;
		}

		public void ResetMumbleAudioSource()
        {
			MumbleAudioSource = null;
		}
	}
}
