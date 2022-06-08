using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
#if UNITY_2019_1_OR_NEWER
using System.Threading.Tasks;
#endif
using UnityEngine;

namespace Artanim
{
    public static class TaskUtils
    {
        public class Wrapper<T>
        {
            public T Value;
        }

        public class Status
        {
            public bool Success;
        }

#if UNITY_2019_1_OR_NEWER

        public static IEnumerator RunTask(Func<CancellationToken, Task> asyncFunc, Status status = null)
        {
            bool taskCompleted = false;
            bool success = false;

            Task.Run(() =>
            {
                try
                {
                    asyncFunc(new CancellationToken()).Wait();
                    success = true;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                taskCompleted = true;
            });

            yield return new WaitUntil(() => taskCompleted);

            if (status != null)
            {
                status.Success = success;
            }
        }

        public static IEnumerator RunTask<T>(Func<CancellationToken, Task<T>> asyncFunc, Wrapper<T> result, Status status = null)
        {
            bool taskCompleted = false;
            bool success = false;

            Task.Run(() =>
            {
                try
                {
                    result.Value = asyncFunc(new CancellationToken()).Result;
                    success = true;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                taskCompleted = true;
            });

            yield return new WaitUntil(() => taskCompleted);

            if (status != null)
            {
                status.Success = success;
            }
        }

#endif
    }
}