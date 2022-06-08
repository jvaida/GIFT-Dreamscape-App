using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
    [AddComponentMenu("Artanim/Avatar Eye Controller")]
    [RequireComponent(typeof(AvatarFaceController))]
    public class AvatarEyeController : MonoBehaviour
    {
        [Header("Editor Debug")]
        public bool DebugMode;
        [ReadOnlyProperty] public List<string> RegisteredHotSpotsDebug = new List<string>();

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

        private AvatarFaceDefintion FaceDefinition { get { return FaceController.FaceDefinition; } }

        private List<BaseFaceHotSpot> ActiveHotSpotsSorted = new List<BaseFaceHotSpot>();

        #region Unity events

        private void Start()
        {
#if !UNITY_EDITOR
            DebugMode = false;
#endif

            //Check current state
            if(!FaceController.LeftEyeTransform || !FaceController.RightEyeTransform)
            {
                Debug.LogWarning("Failed to find eye transforms in FaceController. EyeController will be disabled!");
                enabled = false;
                return;
            }

            //Validate blinking state
            if(FaceDefinition.EyeBlinkingFaceState && FaceDefinition.EyeBlinkingFaceState.LifeCycle != FaceState.ELifeCycle.PingPong1)
            {
                Debug.LogWarningFormat("Eye blinking FaceState is not set to PingPong1 lifecycle! FaceState={0}, LifeCycle={1}",
                    FaceDefinition.EyeBlinkingFaceState.name, FaceDefinition.EyeBlinkingFaceState.LifeCycle);
            }
            
            ResetBlinkTimer();
        }

        private void Update()
        {
            processBlinking();

            //Find eye hotspot
            if (!processEyeHotSpots())
                processDefaultEyeAnimation();

            //Move eyes to target
            processEyesLookAt();

            //Find face hotspot
            processHotSpotBlendShapes();

            //Debug
            if(DebugMode)
            {
                RegisteredHotSpotsDebug.Clear();
                for(var i=0; i < ActiveHotSpotsSorted.Count; ++i)
                {
                    RegisteredHotSpotsDebug.Add(ActiveHotSpotsSorted[i].ToString());
                }
            }
        }

        #endregion

        #region Hotspots

        public void RegisterHotSpot(BaseFaceHotSpot hotSpot)
        {
            if(hotSpot)
            {
                if (!ActiveHotSpotsSorted.Contains(hotSpot))
                {
                    ActiveHotSpotsSorted.Add(hotSpot);
                    ActiveHotSpotsSorted.Sort((x, y) => y.Priority.CompareTo(x.Priority));
                }
            }
        }

        public void UnRegisterHotSpot(BaseFaceHotSpot hotSpot)
        {
            if(hotSpot)
            {
                if (ActiveHotSpotsSorted.Contains(hotSpot))
                {
                    ActiveHotSpotsSorted.Remove(hotSpot);
                    ActiveHotSpotsSorted.Sort((x, y) => y.Priority.CompareTo(x.Priority));

                    //Deactivate face state
                    if(hotSpot is FaceHotSpot)
                    {
                        FaceController.DeactivateFaceState((hotSpot as FaceHotSpot).FaceState);
                    }
                }
            }
        }

        #endregion


        #region Blinking
        private float blinkTimer;
        private bool isDoubleBlink = false;

        void processBlinking()
        {
            if (FaceDefinition.EnableEyeBlinking && FaceDefinition.EyeBlinkingFaceState)
            {
                blinkTimer -= Time.deltaTime;

                if (blinkTimer < 0 && !FaceController.IsFaceStateActive(FaceDefinition.EyeBlinkingFaceState))
                {
                    //Blink
                    FaceController.ActivateFaceState(FaceDefinition.EyeBlinkingFaceState);
                }
                else if(blinkTimer < 0 &&  blinkTimer < -(FaceDefinition.EyeBlinkingFaceState.TransitionSecs * 2)) //*2 -> Ping Pong cycle
                {
                    //Blinking done
                    if(!isDoubleBlink)
                    {
                        //Check for double blinking chance
                        if (Random.Range(0, 100.0f) < FaceDefinition.DoubleBlinkChancePercent)
                        {
                            //Double blink once
                            blinkTimer = 0f;
                            isDoubleBlink = true;
                        }
                        else
                        {
                            //No double blink now
                            ResetBlinkTimer();
                        }
                    }
                    else
                    {
                        isDoubleBlink = false;
                        ResetBlinkTimer();
                    }
                }
            }
        }

        private void ResetBlinkTimer()
        {
            blinkTimer = Random.Range(FaceDefinition.BlinkTimeRange.x, FaceDefinition.BlinkTimeRange.y);
        }

        #endregion

        #region Eye look at
        private enum RandomEyeHotspotState { Waiting, Active };

        private Vector3 eyeTarget = Vector3.forward, eyeCurrent = Vector3.forward;
        private Quaternion currentRandomEyeAngleHotspot = Quaternion.identity;
        private float randomEyeTimer = 0, randomEyeTimerHotspot = 0;
        private Quaternion currentRandomEyeAngle = Quaternion.identity;
        private RandomEyeHotspotState hotspotState = RandomEyeHotspotState.Waiting;


        private void processEyesLookAt()
        {
            eyeCurrent = Vector3.Slerp(eyeCurrent, eyeTarget, Time.deltaTime * FaceDefinition.EyeMotionSpeed);

            FaceController.LeftEyeTransform.localRotation =
                Quaternion.FromToRotation(FaceDefinition.EyeForward, eyeCurrent) * FaceController.LeftEyeInitialRotation;

            FaceController.RightEyeTransform.transform.localRotation =
                Quaternion.FromToRotation(FaceDefinition.EyeForward, eyeCurrent) * FaceController.RightEyeInitialRotation;
        }

        bool processEyeHotSpots()
        {
            bool hotSpotFound = false;

            processRandomEyeRotationInHotSpot();

            var currentPriority = -1;
            var lastFoundDistance = -1f;
            var eyeCenter = (FaceController.RightEyeTransform.position + FaceController.LeftEyeTransform.position) * 0.5f;
            for (var i=0; i < ActiveHotSpotsSorted.Count; ++i)
            {
                var hotSpot = ActiveHotSpotsSorted[i];
                if(hotSpot && hotSpot is EyesHotSpot)
                {
                    //Init priority?
                    if (currentPriority == -1) currentPriority = hotSpot.Priority;

                    var hp = hotSpot.gameObject.transform.position;
                    var eyeToObject = hp - eyeCenter;

                    //Do we need to consider this?
                    if (hotSpot.Priority < currentPriority && hotSpotFound)
                    {
                        DrawHotSpotDebug(hotSpot, Color.red);
                        break;
                    }

                    if(eyeToObject != Vector3.zero)
                    {
                        var distanceToHotSpot = eyeToObject.magnitude;

                        if (!hotSpotFound || distanceToHotSpot < lastFoundDistance)
                        {
                            Vector3 target = currentRandomEyeAngleHotspot * Quaternion.Inverse(FaceController.LeftEyeTransform.parent.rotation) * eyeToObject;
                            Vector3 v = (Quaternion.FromToRotation(FaceDefinition.EyeForward, Vector3.forward) * Quaternion.LookRotation(target)).eulerAngles;

                            float horizontalAngle = 360.0f - v.y;
                            if (horizontalAngle > 180.0f) horizontalAngle = horizontalAngle - 360.0f;

                            float verticalAngle = v.x;
                            if (verticalAngle > 180.0f) verticalAngle = verticalAngle - 360.0f;

                            if ((horizontalAngle >= -FaceDefinition.HorizontalAngle &&
                                horizontalAngle <= FaceDefinition.HorizontalAngle &&
                                verticalAngle >= -FaceDefinition.VerticalAngle &&
                                verticalAngle <= FaceDefinition.VerticalAngle) &&
                                (horizontalAngle >= hotSpot.MinHorizontalAngle &&
                                horizontalAngle <= hotSpot.MaxHorizontalAngle &&
                                verticalAngle >= hotSpot.MinVerticalAngle &&
                                verticalAngle <= hotSpot.MaxVerticalAngle))
                            {
                                DrawHotSpotDebug(hotSpot, Color.green);

                                eyeTarget = target;
                                lastFoundDistance = distanceToHotSpot;
                                hotSpotFound = true;
                            }
                            else
                            {
                                DrawHotSpotDebug(hotSpot, Color.blue);
                            }
                        }
                    }
                }
            }

            return hotSpotFound;
        }

        void processDefaultEyeAnimation()
        {

            //Randome eye movement
            if(FaceDefinition.EnableRandomEyeMotion)
            {
                randomEyeTimer -= Time.deltaTime;
                if (randomEyeTimer <= 0)
                {
                    randomEyeTimer = Random.Range(FaceDefinition.RandomEyeMotionTimeRange.x, FaceDefinition.RandomEyeMotionTimeRange.y);
                    currentRandomEyeAngle = Quaternion.Euler(
                        Random.Range(-FaceDefinition.RandomEyeMaxAngle, FaceDefinition.RandomEyeMaxAngle),
                        Random.Range(-FaceDefinition.RandomEyeMaxAngle, FaceDefinition.RandomEyeMaxAngle), 0f);
                }
            }
            

            float yEuler = FaceController.HeadTransform.localRotation.eulerAngles.y;

            if (yEuler > 180.0f) yEuler = yEuler - 360.0f;

            yEuler = Mathf.Clamp(yEuler, -FaceDefinition.HorizontalAngle, FaceDefinition.HorizontalAngle);

            eyeTarget = Quaternion.Euler(0, yEuler, 0) * currentRandomEyeAngle * FaceDefinition.EyeForward;
        }

        void processRandomEyeRotationInHotSpot()
        {
            if(FaceDefinition.EnableHotspotRandomEyeMotion)
            {
                if (hotspotState == RandomEyeHotspotState.Waiting)
                {
                    randomEyeTimerHotspot -= Time.deltaTime;
                    if (randomEyeTimerHotspot <= 0)
                    {
                        randomEyeTimerHotspot = Random.Range(FaceDefinition.RandomEyeMotionDurationHotspotTimeRange.x, FaceDefinition.RandomEyeMotionDurationHotspotTimeRange.y);
                        currentRandomEyeAngleHotspot = Quaternion.Euler(0f, Random.Range(-FaceDefinition.RandomEyeMaxAngleHotspot, FaceDefinition.RandomEyeMaxAngleHotspot), 0f);
                        hotspotState = RandomEyeHotspotState.Active;
                    }
                }
                else
                {
                    randomEyeTimerHotspot -= Time.deltaTime;
                    if (randomEyeTimerHotspot <= 0)
                    {
                        randomEyeTimerHotspot = Random.Range(FaceDefinition.RandomEyeMotionHotspotTimeRange.x, FaceDefinition.RandomEyeMotionHotspotTimeRange.y);
                        hotspotState = RandomEyeHotspotState.Waiting;
                        currentRandomEyeAngleHotspot = Quaternion.identity;
                    }
                }
            }
        }

        #endregion

        #region Hotspot Face State

        void processHotSpotBlendShapes()
        {
            bool hotSpotFound = false;

            int currentPriority = -1;
            var eyeCenter = (FaceController.RightEyeTransform.position + FaceController.LeftEyeTransform.position) * 0.5f;

            for (var i = 0; i < ActiveHotSpotsSorted.Count; ++i)
            {
                var hotSpot = ActiveHotSpotsSorted[i];
                if(hotSpot && hotSpot is FaceHotSpot)
                {
                    //Init priority?
                    if (currentPriority == -1) currentPriority = hotSpot.Priority;

                    var hp = hotSpot.gameObject.transform.position;
                    var eyeToObject = hp - eyeCenter;

                    //Do we need to consider this?
                    if (hotSpot.Priority < currentPriority && hotSpotFound)
                    {
                        DrawHotSpotDebug(hotSpot, Color.red);
                        break;
                    }

                    if (eyeToObject != Vector3.zero)
                    {
                        Vector3 target = Quaternion.Inverse(FaceController.LeftEyeTransform.parent.rotation) * eyeToObject;
                        Vector3 v = (Quaternion.FromToRotation(FaceDefinition.EyeForward, Vector3.forward) * Quaternion.LookRotation(target)).eulerAngles;

                        float horizontalAngle = 360.0f - v.y;
                        if (horizontalAngle > 180.0f) horizontalAngle = horizontalAngle - 360.0f;

                        float verticalAngle = v.x;
                        if (verticalAngle > 180.0f) verticalAngle = verticalAngle - 360.0f;

                        if (horizontalAngle >= hotSpot.MinHorizontalAngle &&
                            horizontalAngle <= hotSpot.MaxHorizontalAngle &&
                            verticalAngle >= hotSpot.MinVerticalAngle &&
                            verticalAngle <= hotSpot.MaxVerticalAngle)
                        {
                            DrawHotSpotDebug(hotSpot, Color.green);
                            hotSpotFound = true;

                            FaceController.ActivateFaceState((hotSpot as FaceHotSpot).FaceState);
                        }
                        else
                        {
                            DrawHotSpotDebug(hotSpot, Color.blue);
                        }
                    }
                }
            }
        }

        #endregion

        #region Debug

        private void DrawHotSpotDebug(BaseFaceHotSpot hotSpot, Color color)
        {
            if(DebugMode)
            {
                Debug.DrawLine(hotSpot.transform.position, FaceController.RightEyeTransform.position, color);
                Debug.DrawLine(hotSpot.transform.position, FaceController.LeftEyeTransform.position, color);
            }
        }

        #endregion

    }
}