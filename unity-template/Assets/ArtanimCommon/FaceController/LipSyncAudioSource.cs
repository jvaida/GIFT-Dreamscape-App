using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{

    [RequireComponent(typeof(AudioSource))]
    public class LipSyncAudioSource : MonoBehaviour
    {
        public bool DelayCompensate = false;
        public bool MuteOutput = false, skipAudioSource = false;

        [ReadOnlyProperty] public bool Paused = false;

        public OVRLipSync.ContextProviders Provider = OVRLipSync.ContextProviders.Original;

        private OVRLipSync.Frame frame = new OVRLipSync.Frame();
        private uint context = 0;   // 0 is no context
        private float Gain = 1.0f;

        public int Smoothing
        {
            set
            {
                OVRLipSync.SendSignal(context, OVRLipSync.Signals.VisemeSmoothing, value, 0);
            }
        }

        public uint Context
        {
            get
            {
                return context;
            }
        }

        protected OVRLipSync.Frame Frame
        {
            get
            {
                return frame;
            }
        }

        #region Unity Events

        void Start()
        {
            Gain = ConfigService.Instance.ExperienceConfig.LipsyncAudioGain;

            var lipSyncController = GetComponentInParent<AvatarLipSyncController>();
            if(lipSyncController)
            {
                //Init OVR lipsync context
                lock (this)
                {
                    if (context == 0)
                    {
                        if (OVRLipSync.CreateContext(ref context, Provider) != OVRLipSync.Result.Success)
                        {
                            Debug.LogError("FaceLipSyncContext.Start ERROR: Could not create Phoneme context. Please check that you have a OVRLipSync interface in your scene.");
                            return;
                        }
                    }
                }

                //Init
                lipSyncController.InitLipSync(this);
            }
        }

        /// <summary>
        /// Postprocess F32 PCM audio buffer
        /// </summary>
        /// <param name="data">Data.</param>
        /// <param name="channels">Channels.</param>
        public void PostprocessAudioSamples(float[] data, int channels)
        {
            // Turn off output (so that we don't get feedback from mics too close to speakers)
            if (MuteOutput)
            {
                for (int i = 0; i < data.Length; ++i)
                    data[i] = data[i] * 0.0f;
            }
        }

        /// <summary>
        /// Pass F32 PCM audio buffer to the lip sync module
        /// </summary>
        /// <param name="data">Data.</param>
        /// <param name="channels">Channels.</param>
        public void ProcessAudioSamplesRaw(float[] data, int channels)
        {
            // Send data into Phoneme context for processing (if context is not 0)
            lock (this)
            {
                if (Context == 0 || OVRLipSync.IsInitialized() != OVRLipSync.Result.Success)
                {
                    return;
                }
                var frame = this.Frame;
                OVRLipSync.ProcessFrame(Context, data, frame, channels == 2);
            }
        }

        /// <summary>
        /// Pass S16 PCM audio buffer to the lip sync module
        /// </summary>
        /// <param name="data">Data.</param>
        /// <param name="channels">Channels.</param>
        public void ProcessAudioSamplesRaw(short[] data, int channels)
        {
            // Send data into Phoneme context for processing (if context is not 0)
            lock (this)
            {
                if (Context == 0 || OVRLipSync.IsInitialized() != OVRLipSync.Result.Success)
                {
                    return;
                }
                var frame = this.Frame;
                OVRLipSync.ProcessFrame(Context, data, frame, channels == 2);
            }
        }


        /// <summary>
        /// Preprocess F32 PCM audio buffer
        /// </summary>
        /// <param name="data">Data.</param>
        /// <param name="channels">Channels.</param>
        public void PreprocessAudioSamples(float[] data, int channels)
        {
            // Increase the gain of the input
            if(Gain != 1f)
            {
                for (int i = 0; i < data.Length; ++i)
                {
                    data[i] = data[i] * Gain;
                }
            }
            
        }

        /// <summary>
        /// Process F32 audio sample and pass it to the lip sync module for computation
        /// </summary>
        /// <param name="data">Data.</param>
        /// <param name="channels">Channels.</param>
        public void ProcessAudioSamples(float[] data, int channels)
        {
            // Do not process if we are not initialized, or if there is no
            // audio source attached to game object
            if ((OVRLipSync.IsInitialized() != OVRLipSync.Result.Success))
            {
                return;
            }
            PreprocessAudioSamples(data, channels);
            ProcessAudioSamplesRaw(data, channels);
            PostprocessAudioSamples(data, channels);
        }

        /// <summary>
        /// Raises the audio filter read event.
        /// </summary>
        /// <param name="data">Data.</param>
        /// <param name="channels">Channels.</param>
        void OnAudioFilterRead(float[] data, int channels)
        {
            if (!skipAudioSource)
            {
                ProcessAudioSamples(data, channels);
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            // Create the context that we will feed into the audio buffer
            lock (this)
            {
                if (context != 0)
                {
                    if (OVRLipSync.DestroyContext(context) != OVRLipSync.Result.Success)
                    {
                        Debug.LogWarning("OVRPhonemeContext.OnDestroy ERROR: Could not delete Phoneme context.");
                    }
                }
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Gets the current phoneme frame (lock and copy current frame to caller frame)
        /// </summary>
        /// <returns>error code</returns>
        /// <param name="inFrame">In frame.</param>
        public OVRLipSync.Frame GetCurrentPhonemeFrame()
        {
            return frame;
        }

        public void SetVisemeBlend(int viseme, int amount)
        {
            OVRLipSync.SendSignal(context, OVRLipSync.Signals.VisemeAmount, viseme, amount);
        }

        /// <summary>
        /// Resets the context.
        /// </summary>
        /// <returns>error code</returns>
        public OVRLipSync.Result ResetContext()
        {
            return OVRLipSync.ResetContext(context);
        }

        #endregion

        private void ClearData(float[] data)
        {
            for (var i = 0; i < data.Length; ++i)
                data[i] = 0f;
        }


    }


}