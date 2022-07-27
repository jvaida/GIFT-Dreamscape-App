using Artanim.Haptics.PodLayout;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Artanim.Haptics.Internal
{
    public static class DevicePicker
    {
        public enum PickMethod { Distance, Angle };

        public static IEnumerable<T> Select<T>(IEnumerable<T> devices, Func<T, Vector2> getPos, PodSide side)
        {
            if (getPos == null)
            {
                throw new ArgumentNullException("getPos");
            }

            var selection = Enumerable.Empty<T>();
            if ((side & PodSide.Left) != 0)
            {
                selection = selection.Concat(
                    devices.Where(d => { var v = getPos(d); return (v.x > -Mathf.Epsilon) && ((v.x - Mathf.Abs(v.y)) > -Mathf.Epsilon); }));
            }
            if ((side & PodSide.Right) != 0)
            {
                selection = selection.Concat(
                    devices.Where(d => { var v = getPos(d); return (v.x < Mathf.Epsilon) && ((-v.x - Mathf.Abs(v.y)) > -Mathf.Epsilon); }));
            }
            if ((side & PodSide.Front) != 0)
            {
                selection = selection.Concat(
                    devices.Where(d => { var v = getPos(d); return (v.y > -Mathf.Epsilon) && ((v.y - Mathf.Abs(v.x)) > -Mathf.Epsilon); }));
            }
            if ((side & PodSide.Back) != 0)
            {
                selection = selection.Concat(
                    devices.Where(d => { var v = getPos(d); return (v.y < Mathf.Epsilon) && ((-v.y - Mathf.Abs(v.x)) > -Mathf.Epsilon); }));
            }
            return selection.Distinct();
        }

        public static IEnumerable<T> SelectAndOrder<T>(IEnumerable<T> devices, Func<T, Vector2> getPos, Vector2 posOrForward, PickMethod pickMethod = PickMethod.Distance, PodSide side = PodSide.All)
        {
            var selection = Select(devices, getPos, side);
            switch (pickMethod)
            {
                case PickMethod.Distance:
                    return selection.OrderBy(f => Vector2.Distance(posOrForward, getPos(f)));
                case PickMethod.Angle:
                    return selection.OrderBy(f => Vector2.Angle(posOrForward, getPos(f)));
                default:
                    throw new InvalidOperationException("Unexpected value for pickMethod: " + pickMethod);
            }
        }
    }
}