using Artanim.Location.Messages;
using Artanim.Location.Network;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Artanim
{
    [AddComponentMenu("Artanim/Avatar LipSync Controller")]
    [RequireComponent(typeof(AvatarFaceController))]
    public class AvatarLipSyncController : MonoBehaviour
    {
        private const float DELAYED_LIPSYNC_PAUSE_TIME = 0.5f;

        public bool IgnoreVisemeSilence;
        
        private bool StreamLipsyncUpdates;
        private System.Guid LipsyncUpdatePlayerId;

        [ReadOnlyProperty] public bool LipSyncEnabled;
        public bool PauseLipsyncWhenInvisible = true;

        private AvatarFaceController _FaceController;
        private AvatarFaceController FaceController
        {
            get
            {
                if (!_FaceController)
                    _FaceController = GetComponent<AvatarFaceController>();
                return _FaceController;
            }
        }

        private AvatarFaceDefintion FaceDefinition
        {
            get
            {
                return FaceController.FaceDefinition;
            }
        }

        private LipSyncAudioSource lipsyncContext = null;

        private AvatarLipsyncUpdate LipsyncUpdateCache;

        #region Public Interface

        public void InitLipSync(LipSyncAudioSource context)
        {
            if (ConfigService.VerboseSdkLog)
                Debug.LogFormat("Initializing lipsync: avatar={0}, context={1}", name, context.name);

            lipsyncContext = context;
            lipsyncContext.Smoothing = FaceDefinition.LipSyncSmoothAmount;

            //Init message cache
            LipsyncUpdateCache = new AvatarLipsyncUpdate { VisemeStates = new float[numViseme], };
        }

        public void SetupLipsyncStreaming()
        {
            Debug.LogFormat("AvatarLipSyncController setup to stream lipsync for player={0}", GameController.Instance.CurrentPlayer.Player.ComponentId);
            StreamLipsyncUpdates = true;
        }

        public void SetupLipsyncListener(System.Guid playerId)
        {
            Debug.LogFormat("AvatarLipSyncController setup to listen for updates form  player={0}", playerId);
            LipsyncUpdatePlayerId = playerId;
            NetworkInterface.Instance.Subscribe<AvatarLipsyncUpdate>(OnAvatarLipsyncUpdate);
        }

        #endregion

        #region Unity events

        private void Update()
        {
            processLipSync();
        }

        private void OnDestroy()
        {
            NetworkInterface.SafeUnsubscribe<AvatarLipsyncUpdate>(OnAvatarLipsyncUpdate);
        }

        #endregion

        #region Location events

        private void OnAvatarLipsyncUpdate(AvatarLipsyncUpdate args)
        {
            if(args.SenderId == LipsyncUpdatePlayerId)
            {
                float maxW = 0;

                for (int i = 0; i < args.VisemeStates.Length; i++)
                {
                    float w = args.VisemeStates[i];

                    var faceState = FaceDefinition.GetVisemeState((OVRLipSync.Viseme)i);

                    w = Mathf.Clamp01(w);
                    maxW = Mathf.Max(maxW, w);

                    //Done activate face states on small weights
                    if (w <= 0.05f)
                    {
                        FaceController.DeactivateFaceState(faceState);
                    }
                    else
                    {
                        FaceController.ActivateFaceState(faceState, w);
                    }
                }

                //Apply randome frace state
                processRandomFaceStateDuringLipSync(maxW);
            }
        }

        #endregion

        #region Internals
        private float lipSyncPauseTimer = 0.0f;
        private float nextRandomDuringLipSync = 0.0f;
        private int numViseme = System.Enum.GetValues(typeof(OVRLipSync.Viseme)).Length;
        private bool emptyUpdateSent;
        private float lastSyncSendTime;

        private void processLipSync()
        {
            if (lipsyncContext)
            {
                //Enable?
                if(PauseLipsyncWhenInvisible)
                {
                    if (FaceController.IsFaceVisible)
                    {
                        lipSyncPauseTimer = 0.0f;
                        lipsyncContext.Paused = false;
                    }
                    else
                    {
                        lipSyncPauseTimer += Time.deltaTime;
                        if (lipSyncPauseTimer > DELAYED_LIPSYNC_PAUSE_TIME) lipsyncContext.Paused = true;
                    }
                }
                else
                {
                    lipsyncContext.Paused = false;
                }
                

                LipSyncEnabled = !lipsyncContext.Paused;

                if (!lipsyncContext.Paused)
                {
                    // get the current viseme frame
                    OVRLipSync.Frame frame = lipsyncContext.GetCurrentPhonemeFrame();
                    if (frame != null)
                    {
                        float maxW = 0;
                        var hasStates = false;

                        for (int i = 0; i < numViseme; i++)
                        {
                            if (IgnoreVisemeSilence && (OVRLipSync.Viseme)i == OVRLipSync.Viseme.sil)
                                continue;

                            float w = frame.Visemes[i] * FaceDefinition.VisemeWeightScale;
                            //FaceController.FaceStateContext context;
                            var faceState = FaceDefinition.GetVisemeState((OVRLipSync.Viseme)i);

                            w = Mathf.Clamp01(w);
                            maxW = Mathf.Max(maxW, w);

                            //Done activate face states on small weights

                            if (w <= 0.05f)
                            {
                                FaceController.DeactivateFaceState(faceState);
                                LipsyncUpdateCache.VisemeStates[i] = 0f;
                            }
                            else
                            {
                                FaceController.ActivateFaceState(faceState, w);
                                LipsyncUpdateCache.VisemeStates[i] = w;
                                hasStates = true;
                            }
                        }

                        //Send visam stream when its not empty (0 values) or once when returning to empty
                        if (StreamLipsyncUpdates)
                        {
                            if (Time.realtimeSinceStartup > lastSyncSendTime + ConfigService.Instance.ExperienceConfig.LipsyncSyncUpdatePeriod)
                            {
                                if(hasStates || !emptyUpdateSent)
                                {
                                    //Send lipsync update to session components
                                    NetworkInterface.Instance.SendMessage(LipsyncUpdateCache);
                                    lastSyncSendTime = Time.realtimeSinceStartup;
                                }

                                emptyUpdateSent = hasStates ? false : true;
                            }
                        }

                        //Apply randome frace state
                        processRandomFaceStateDuringLipSync(maxW);
                    }
                }
            }
        }

        private void processRandomFaceStateDuringLipSync(float currentLipSincWeight)
        {
            if (FaceDefinition.EnableRandomFaceStatesDuringLipsync && FaceDefinition.RandomFaceStateDuringLipSync.Length > 0)
            {
                if (currentLipSincWeight > 0.2f)
                {
                    nextRandomDuringLipSync -= Time.deltaTime;
                    if (nextRandomDuringLipSync <= 0)
                    {
                        nextRandomDuringLipSync = Random.Range(FaceDefinition.RandomFaceStatesTimeRange.x, FaceDefinition.RandomFaceStatesTimeRange.y);
                        FaceState fs = FaceDefinition.RandomFaceStateDuringLipSync[Random.Range(0, FaceDefinition.RandomFaceStateDuringLipSync.Length)];

                        FaceController.ActivateFaceState(fs);
                    }
                }
            }
        }
        
        #endregion

    }
}