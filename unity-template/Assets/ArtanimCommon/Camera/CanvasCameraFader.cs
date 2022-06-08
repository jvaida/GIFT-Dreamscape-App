using Artanim;
using Artanim.Location.Messages;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Artanim
{

    public class CanvasCameraFader : MonoBehaviour, ICameraFader
    {
        public float TransitionTimeSecs = 1f;
        public CanvasGroup FadePanel;
        public Image ImageFade;

        private Transition TargetTransition;

        public IEnumerator DoFadeAsync(Transition transition, string customTransitionName = null)
        {
            TargetTransition = transition;

            //Set color
            SetTransitionColor(transition);

            //Fade out
            var endTime = Time.realtimeSinceStartup + TransitionTimeSecs;
            while(Time.realtimeSinceStartup < endTime)
            {
                if(FadePanel)
                    FadePanel.alpha = Mathf.Lerp(0f, 1f, 1f - Mathf.Clamp01((endTime - Time.realtimeSinceStartup) / TransitionTimeSecs) );
                yield return null;
            }
            if (FadePanel) FadePanel.alpha = 1f;
        }

        public IEnumerator DoFadeInAsync()
        {
            TargetTransition = Transition.None;

            //Fade in
            var endTime = Time.realtimeSinceStartup + TransitionTimeSecs;
            while (Time.realtimeSinceStartup < endTime)
            {
                if (FadePanel)
                    FadePanel.alpha = Mathf.Lerp(1f, 0f, 1f - Mathf.Clamp01((endTime - Time.realtimeSinceStartup) / TransitionTimeSecs));
                yield return null;
            }
            if (FadePanel) FadePanel.alpha = 0f;
        }

        public Transition GetTragetTransition()
        {
            return TargetTransition;
        }

        public void SetFaded(Transition transition, string customTransitionName = null)
        {
            TargetTransition = transition;

            //Set color
            SetTransitionColor(transition);

            if (FadePanel)
                FadePanel.alpha = 0f;
        }

        private void SetTransitionColor(Transition transition)
        {
            if(ImageFade)
            {
                switch (transition)
                {
                    case Transition.None:
                        break;
                    case Transition.FadeWhite:
                        ImageFade.color = Color.white;
                        break;
                    case Transition.FadeBlack:
                        ImageFade.color = Color.black;
                        break;
                    case Transition.Custom:
                        break;
                    default:
                        break;
                }
            }
        }
    }
}