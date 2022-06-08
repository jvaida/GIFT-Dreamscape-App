using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Artanim
{
    [AddComponentMenu("Artanim/Avatar Face Controller")]
    public class AvatarFaceController : MonoBehaviour
    {
        private const string RUNTIME_MUSCLE_JAW_CLOSE_NAME = "Muscle Jaw Close";
        private const string RUNTIME_MUSCLE_JAW_LEFT_RIGHT_NAME = "Muscle Jaw Left-Right";

        [Tooltip("Animator used for the facial animations. If not set, the AvatarAnimator of the AvatarController is used. When using on a non playable avatar, this property must be set.")]
        [SerializeField]
        private Animator AvatarAnimator;

        [Header("Main Face Definition")]
        [Tooltip("Face definition used for this avatar. A new face definition can be created using the context menu 'Artanim/Character Face Definition'.")]
        public AvatarFaceDefintion FaceDefinition;

        [Header("Face Objects")]
        [Tooltip("List of all SkinnedMeshRenderer's containing the blendshapes used to animate the face.")]
        public List<SkinnedMeshRenderer> FaceRenderers = new List<SkinnedMeshRenderer>();

        [Header("Editor Debug")]
        public bool DebugMode;
        [ReadOnlyProperty]
        public List<string> ActiveFaceStatesDebug = new List<string>();


        private Animator InternalAvatarAnimator
        {
            get
            {
                if (!AvatarAnimator)
                {
                    //Try to find the animator using the AvatarController if available
                    var avatarController = GetComponent<AvatarController>();
                    if (avatarController)
                        AvatarAnimator = avatarController.AvatarAnimator;
                    else
                        Debug.LogErrorFormat("AvatarFaceController is not setup properly. Make sure to add an AvatarController or to set the AvatarAnimator property on avatar:{0}", name);
                }

                return AvatarAnimator;
            }
        }


        //Face transforms
        private Transform _LeftEyeTransform;
        public virtual Transform LeftEyeTransform
        {
            get
            {
                if (!_LeftEyeTransform && InternalAvatarAnimator)
                    _LeftEyeTransform = InternalAvatarAnimator.GetBoneTransform(HumanBodyBones.LeftEye);
                return _LeftEyeTransform;
            }
        }

        private Transform _RightEyeTransform;
        public virtual Transform RightEyeTransform
        {
            get
            {
                if (!_RightEyeTransform && InternalAvatarAnimator)
                    _RightEyeTransform = InternalAvatarAnimator.GetBoneTransform(HumanBodyBones.RightEye);
                return _RightEyeTransform;
            }
        }

        private Transform _JawTransform;
        public virtual Transform JawTransform
        {
            get
            {
                if (!_JawTransform && InternalAvatarAnimator)
                    _JawTransform = InternalAvatarAnimator.GetBoneTransform(HumanBodyBones.Jaw);
                return _JawTransform;
            }
        }

        private Transform _HeadTransform;
        public virtual Transform HeadTransform
        {
            get
            {
                if (!_HeadTransform && InternalAvatarAnimator)
                    _HeadTransform = InternalAvatarAnimator.GetBoneTransform(HumanBodyBones.Head);
                return _HeadTransform;
            }
        }

        public bool IsFaceVisible
        {
            get
            {
                for (var r = 0; r < FaceRenderers.Count; ++r)
                    if (FaceRenderers[r].isVisible)
                        return true;
                return false;
            }
        }

        //Initial/base transform states
        public Quaternion LeftEyeInitialRotation { get; private set; }
        public Quaternion RightEyeInitialRotation { get; private set; }

        private Quaternion _JawInitialRotation = Quaternion.identity;
        public Quaternion JawInitialrotation { get { return _JawInitialRotation; } private set { _JawInitialRotation = value; } }

        //Face states
        private Dictionary<FaceState, FaceStateContext> ActiveFaceStates = new Dictionary<FaceState, FaceStateContext>();
        private Dictionary<string, BaseRuntimeFaceValue> blendingShapeNameToValueTarget = new Dictionary<string, BaseRuntimeFaceValue>();

        #region Unity events

        void Start()
        {
#if !UNITY_EDITOR
            DebugMode = false;
#endif

            //Make sure we have a reference to the avatar animator
            if (!InternalAvatarAnimator)
            {
                Debug.LogErrorFormat("Avatar animator was not set nor found in the AvatarController. Disabling FaceController on avatar: {0}", name);
                enabled = false;
                return;
            }

            if (initFaceDefinition())
            {
                initRuntimeValues();

                //Initial rotations
                LeftEyeInitialRotation = LeftEyeTransform.localRotation;
                RightEyeInitialRotation = RightEyeTransform.localRotation;
                JawInitialrotation = JawTransform.localRotation;
            }
        }

        private void Update()
        {
            processFaceStates();
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// 
        /// </summary>
        /// <param name="faceState"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        public void ActivateFaceState(FaceState faceState, float weightOverride = 0f)
        {
            if(faceState)
            {
                FaceStateContext context = null;

                //New
                if (!ActiveFaceStates.TryGetValue(faceState, out context))
                {
                    context = new FaceStateContext
                    {
                        faceState = faceState,
                    };
                    ActiveFaceStates[faceState] = context;
                }
                else
                {
                    context.CurrentState = FaceStateContext.EState.TransitionOn;   
                }

                if (weightOverride != 0f)
                    context.currentTime = Mathf.Clamp01(weightOverride);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="faceState"></param>
        public void DeactivateFaceState(FaceState faceState)
        {
            if(faceState)
            {
                //if (ConfigService.VerboseSdkLog)
                //    Debug.LogFormat("Deactivating face state: avatar={0}, state={1}", name, faceState.name);

                var context = GetFaceStateContext(faceState);
                if (context != null)
                    context.CurrentState = FaceStateContext.EState.TransitionOff;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="faceState"></param>
        /// <returns></returns>
        public bool IsFaceStateActive(FaceState faceState)
        {
            if (faceState)
                return GetFaceStateContext(faceState) != null;
            else
                return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value">0f-1f value. Jaw full closed=0f, Jaw full open=1f</param>
        public void SetJawPose(float jawOpenValue, float jawLeftRightValue)
        {
            //Init if not already... can be the case in editor
            if (JawInitialrotation == Quaternion.identity)
            {
                JawInitialrotation = JawTransform.localRotation;
            }

            var jawRotation = JawInitialrotation;
            if(!float.IsNaN(jawOpenValue))
                jawRotation *= Quaternion.AngleAxis(jawOpenValue * FaceDefinition.JawMaxOpenAngle, FaceDefinition.JawOpenAxis);

            if (!float.IsNaN(jawLeftRightValue))
                jawRotation *= Quaternion.AngleAxis(jawLeftRightValue * FaceDefinition.JawMaxLeftRightAngle, FaceDefinition.JawLeftRightAxis);

            JawTransform.localRotation = jawRotation;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetAllBlendshapeNames()
        {
            var bsNames = new List<string>();
            foreach (var mesh in FaceRenderers.Select(r => r.sharedMesh))
            {
                for (var i = 0; i < mesh.blendShapeCount; ++i)
                {
                    bsNames.Add(mesh.GetBlendShapeName(i));
                }
            }
            return bsNames;
        }

        #endregion


        #region Internals

        private bool initFaceDefinition()
        {
            if(!FaceDefinition)
            {
                Debug.LogError("FaceController needs a CharacterFaceDefinition");
                return false;
            }

            return true;
        }


        private void initRuntimeValues()
        {
            //Blendshapes
            foreach(var renderer in FaceRenderers)
            {
                var mesh = renderer.sharedMesh;
                if (mesh && mesh.blendShapeCount > 0)
                {
                    for (int i = 0; i < mesh.blendShapeCount; i++)
                    {
                        blendingShapeNameToValueTarget[mesh.GetBlendShapeName(i)] = new BlendingShapeValue
                        {
                            index = i,
                            skinMeshRenderer = renderer,
                        };
                    }
                }
            }

            //Muscles
            blendingShapeNameToValueTarget[RUNTIME_MUSCLE_JAW_CLOSE_NAME] = new MuscleValue();
            blendingShapeNameToValueTarget[RUNTIME_MUSCLE_JAW_LEFT_RIGHT_NAME] = new MuscleValue();
        }

        private List<FaceState> ToRemoveFaceStates = new List<FaceState>();
        private void processFaceStates()
        {
            if(DebugMode)
                ActiveFaceStatesDebug.Clear();

            ToRemoveFaceStates.Clear();

            foreach (var activeState in ActiveFaceStates)
            {
                var faceState = activeState.Key;
                var context = activeState.Value;

                //Update context state and time
                var contextState = context.Update();

                //Remove context?
                if(contextState == FaceStateContext.EState.Off)
                {
                    ToRemoveFaceStates.Add(faceState);
                }

                //Mix blendshape values
                var currentWeight = faceState.TransitionCurve.Evaluate(context.currentTime);
                for(var i=0; i < faceState.BlendShapeValues.Count; ++i)
                {
                    var blendShapeValue = faceState.BlendShapeValues[i];
                    BaseRuntimeFaceValue runtimeBlendShapeValue;
                    if(blendingShapeNameToValueTarget.TryGetValue(blendShapeValue.Name, out runtimeBlendShapeValue))
                    {
                        runtimeBlendShapeValue.AddFrameValue(blendShapeValue.Value * currentWeight);
                    }
                }

                //Mix muscle values
                blendingShapeNameToValueTarget[RUNTIME_MUSCLE_JAW_CLOSE_NAME].AddFrameValue(faceState.JawOpen * currentWeight);
                blendingShapeNameToValueTarget[RUNTIME_MUSCLE_JAW_LEFT_RIGHT_NAME].AddFrameValue(faceState.JawLeftRight * currentWeight);

                if(DebugMode)
                    ActiveFaceStatesDebug.Add(context.ToString());
            }

            //Apply blendshapes to renderers
            foreach(var blendShapeValue in blendingShapeNameToValueTarget)
            {
                if(blendShapeValue.Value is BlendingShapeValue)
                    blendShapeValue.Value.ApplyFrameValue(FaceDefinition.SmoothingWeight);
            }

            //Apply muscles at once
            SetJawPose(
                blendingShapeNameToValueTarget[RUNTIME_MUSCLE_JAW_CLOSE_NAME].CurrentFrameAverage,
                blendingShapeNameToValueTarget[RUNTIME_MUSCLE_JAW_LEFT_RIGHT_NAME].CurrentFrameAverage);

            //Clean face states
            for(var i=0; i < ToRemoveFaceStates.Count; ++i)
            {
                ActiveFaceStates.Remove(ToRemoveFaceStates[i]);
            }
        }

        private FaceStateContext GetFaceStateContext(FaceState faceState)
        {
            if (faceState)
            {
                FaceStateContext context = null;
                ActiveFaceStates.TryGetValue(faceState, out context);
                return context;
            }
            return null;
        }

        #endregion

        #region Classes

        private class FaceStateContext
        {
            public enum EState { TransitionOn, TransitionOff, On, Off }

            public EState CurrentState;
            public FaceState faceState;
            public float currentTime = 0f;

            private float timeFactor
            {
                get
                {
                    switch (CurrentState)
                    {
                        case EState.TransitionOn:
                            return 1f;
                        case EState.TransitionOff:
                            return -1f;
                        default:
                            return 0f;
                    }
                }
            }

            public EState Update()
            {
                //Move time forward or backward
                if (faceState.TransitionSecs > 0)
                    currentTime += Time.deltaTime / faceState.TransitionSecs * timeFactor;
                else
                    currentTime = 1;
                currentTime = Mathf.Clamp01(currentTime);


                //Handle on/off states
                if (currentTime == 0f)
                    CurrentState = EState.Off;
                else if (currentTime == 1f)
                    CurrentState = EState.On;

                //Handle lifecycle
                switch (faceState.LifeCycle)
                {
                    case FaceState.ELifeCycle.PingPong1:
                        if (currentTime == 1f && CurrentState == EState.On)
                            CurrentState = EState.TransitionOff;
                        break;

                    case FaceState.ELifeCycle.On:
                        break;
                }

                return CurrentState;
            }

            public override string ToString()
            {
                return string.Format("{0}: time={1}, state={2}", faceState.name, currentTime, CurrentState);
            }
        };


        private abstract class BaseRuntimeFaceValue
        {
            //Runtime average calculations
            private int CurrentAverageFrame;
            private int FrameValuesCount;
            private float FrameValuesSum;

            public float CurrentFrameAverage { get { return FrameValuesCount > 0 ? FrameValuesSum / FrameValuesCount : float.NaN; } }
            public abstract float ApplyFrameValue(float smoothingWeight = 0f);

            protected float LastFrameAverage = float.NaN;

            public void AddFrameValue(float value)
            {
                //Reset?
                if (CurrentAverageFrame != Time.frameCount)
                {
                    LastFrameAverage = CurrentFrameAverage;
                    FrameValuesCount = 0;
                    FrameValuesSum = 0f;
                    CurrentAverageFrame = Time.frameCount;
                }

                if(value > 0f)
                {
                    FrameValuesCount++;
                    FrameValuesSum += value;
                }
                
            }
        }

        private class BlendingShapeValue : BaseRuntimeFaceValue
        {
            public SkinnedMeshRenderer skinMeshRenderer;
            public int index;

            public override float ApplyFrameValue(float smoothingWeight = 0f)
            {
                var newValue = CurrentFrameAverage;
                if(!float.IsNaN(newValue))
                {
                    if (float.IsNaN(LastFrameAverage))
                        newValue = CurrentFrameAverage;
                    else
                        newValue = Mathf.Lerp(CurrentFrameAverage, LastFrameAverage, smoothingWeight);

                    //Debug.LogFormat("new={0}, current={1}, weight={2}, result={3}", CurrentFrameAverage, LastFrameAverage, smoothingWeight, newValue);

                    skinMeshRenderer.SetBlendShapeWeight(index, newValue * 100f);
                    return newValue;
                }
                return -1;
            }
        };

        private class MuscleValue : BaseRuntimeFaceValue
        {
            public override float ApplyFrameValue(float smoothingWeight = 0f)
            {
                //Bones are applied all in one go
                throw new System.NotImplementedException();
            }
        }

        #endregion

#if UNITY_EDITOR
        private FaceState CurrentPreviewFaceState;

        public void PreviewFaceState(FaceState faceState)
        {
            if (faceState)
            {
                //First reset
                if (CurrentPreviewFaceState)
                    ResetPreviewFaceState();

                //Apply blendshape values
                foreach (var bsValue in faceState.BlendShapeValues)
                {
                    int bsIndex;
                    var renderer = GetRendererWithBlendShapName(bsValue.Name, out bsIndex);
                    if (renderer)
                    {
                        renderer.SetBlendShapeWeight(bsIndex, bsValue.Value * 100f);
                    }
                    else
                    {
                        Debug.LogWarningFormat("Failed to find renderer containing blendshape with name {0}. Maybe the FaceState is not compatible with the given avatar.", bsValue.Name);
                    }
                }

                //Apply jaw rotation
                SetJawPose(faceState.JawOpen, faceState.JawLeftRight);

                CurrentPreviewFaceState = faceState;
            }
        }

        public void ResetPreviewFaceState()
        {
            //Reset blendshapes to 0
            foreach (var renderer in FaceRenderers)
            {
                var mesh = renderer.sharedMesh;
                if (mesh)
                {
                    for (var i = 0; i < mesh.blendShapeCount; ++i)
                    {
                        renderer.SetBlendShapeWeight(i, 0f);
                    }
                }
            }

            //Reset jaw
            SetJawPose(0f, 0f);

            CurrentPreviewFaceState = null;
        }


        private SkinnedMeshRenderer GetRendererWithBlendShapName(string blendShapeName, out int index)
        {
            if (!string.IsNullOrEmpty(blendShapeName))
            {
                foreach (var renderer in FaceRenderers)
                {
                    var mesh = renderer.sharedMesh;
                    if (mesh)
                    {
                        for (var i = 0; i < mesh.blendShapeCount; ++i)
                        {
                            if (mesh.GetBlendShapeName(i) == blendShapeName)
                            {
                                index = i;
                                return renderer;
                            }
                        }
                    }
                }
            }

            index = -1;
            return null;
        }

#endif

    }
}