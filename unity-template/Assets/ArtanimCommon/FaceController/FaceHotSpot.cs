using UnityEngine;
using System.IO;

namespace Artanim
{
    [AddComponentMenu("Artanim/Face Hotspot")]
    public class FaceHotSpot : BaseFaceHotSpot
    {
        [Tooltip("The FaceState to blend to the avatars face")]
        public FaceState FaceState;

    }
}

