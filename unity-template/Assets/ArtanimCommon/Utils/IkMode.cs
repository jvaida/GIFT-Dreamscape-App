using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim.IKServer
{
    public enum EIkMode { Tracking, Player, Simulation }

    public static class IkMode
    {
        public static EIkMode CurrentMode { get; set; }
    }
}
