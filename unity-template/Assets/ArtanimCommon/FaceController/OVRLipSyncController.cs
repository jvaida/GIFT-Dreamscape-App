using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
    public class OVRLipSyncController : SingletonBehaviour<OVRLipSyncController>
    {
        public GameObject DefaultMicAudioSource;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public bool InitPlayerLipSyncSource(RuntimePlayer player, bool muteOutput)
        {
            //Validate
            if (player != null)
            {
                if (player.AvatarController != null)
                {
                    if (player.AvatarController.GetComponent<AvatarLipSyncController>() != null)
                    {

                        switch (ConfigService.Instance.ExperienceConfig.LipsyncMode)
                        {
                            case Location.Config.ExperienceConfig.ELipsyncMode.Microphone:
                                //Use mic audio source for lipsync
                                //Make sure OVR lipsync is available
                                EnsureLipSyncInit();

                                //Use mic as lipsync audio source... create from default template
                                Transform audioRoot = null;
                                var avatarHeadPart = player.AvatarController.GetAvatarBodyPart(Location.Messages.EAvatarBodyPart.Head);
                                if (avatarHeadPart)
                                    audioRoot = avatarHeadPart.transform;
                                else
                                    audioRoot = player.AvatarController.HeadBone;

                                if (audioRoot)
                                {
                                    //Add a mic lipsync audio source to head
                                    Debug.LogFormat("Initializing audio source for player: player={0}", player.Player.ComponentId);
                                    var lipsyncAudioSource = UnityUtils.InstantiatePrefab<LipSyncAudioSource>(DefaultMicAudioSource, audioRoot);

                                    //Mute?
                                    lipsyncAudioSource.MuteOutput = muteOutput;

                                    //Position
                                    lipsyncAudioSource.transform.localPosition = Vector3.zero;
                                    lipsyncAudioSource.transform.localRotation = Quaternion.identity;
                                }
                                else
                                {
                                    Debug.LogErrorFormat("Failed to find head transform for avatar {0}. The avatar seems not to be setup properly.", player.AvatarController.name);
                                }
                                break;

                            case Location.Config.ExperienceConfig.ELipsyncMode.Teamspeak:

                                //Use TS audio source as lipsync source
                                if (player.TSAudioSource != null)
                                {
                                    //Make sure OVR lipsync is available
                                    EnsureLipSyncInit();

                                    //Init player audio source for lipsync context
                                    var audioSource = player.TSAudioSource.GetComponent<LipSyncAudioSource>();
                                    if (!audioSource)
                                    {
                                        audioSource = player.TSAudioSource.gameObject.AddComponent<LipSyncAudioSource>();
                                    }

                                    //Mute?
                                    audioSource.MuteOutput = muteOutput;

                                    return true;
                                }
                                else
                                {
                                    Debug.LogErrorFormat("Failed to initialize avatar lipsync. The player does not have a TSAudioSource set. PlayerId={0}, Avatar={1}",
                                    player.Player.ComponentId,
                                    player.AvatarController.name);
                                }
                                break;
                        }
                    }
                    else
                    {
                        Debug.LogErrorFormat("Failed to initialize avatar lipsync. Avatar does not have a LipSyncController attached. PlayerId={0}, Avatar={1}",
                            player.Player.ComponentId,
                            player.AvatarController.name);
                    }
                }
                else
                {
                    Debug.LogErrorFormat("Failed to initialize avatar lipsync. Player AvatarController was null. PlayerId={0}", player.Player.ComponentId);
                }
            }
            else
            {
                Debug.LogError("Failed to initialize avatar lipsync. Given player was null.");
            }
            return false;
        }

        private void EnsureLipSyncInit()
        {
            //Make sure OVR lipsync is available
            var lipSyncContext = GetComponent<OVRLipSync>();
            if (!lipSyncContext)
            {
                gameObject.AddComponent<OVRLipSync>();
            }
        }

    }
}