using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
	[AddComponentMenu("Artanim/Session Timing Section")]
    public class SessionTimingSection : ServerSideBehaviour
    {
        public string SectionName;

        private void OnEnable()
        {
            if(!string.IsNullOrEmpty(SectionName))
            {
                SessionTiming.Instance.RegisterSectionEnd(SectionName);
            }
        }

    }
}