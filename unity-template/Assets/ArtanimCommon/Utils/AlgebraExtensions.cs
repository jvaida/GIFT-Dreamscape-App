using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim.Algebra
{
    static class Vect2fExt
    {
        public static Vector2 ToUnity(this Vect2f vect) { return new Vector2(vect.X, vect.Y); }
    }

    static class Vect3fExt
    {
        public static Vector3 ToUnity(this Vect3f vect) { return new Vector3(vect.X, vect.Y, vect.Z); }
    }

    static class QuatfExt
    {
        public static Quaternion ToUnity(this Quatf quat) { return new Quaternion(quat.X, quat.Y, quat.Z, quat.W); }
    }

    static class Vector2Ext
    {
        public static Vect2f ToVect2f(this Vector2 vect) { return new Vect2f(vect.x, vect.y); }
    }

    static class Vector3Ext
    {
        public static Vect3f ToVect3f(this Vector3 vect) { return new Vect3f(vect.x, vect.y, vect.z); }
    }

    static class QuaternionExt
    {
        public static Quatf ToQuatf(this Quaternion quat) { return new Quatf(quat.x, quat.y, quat.z, quat.w); }
    }
}
