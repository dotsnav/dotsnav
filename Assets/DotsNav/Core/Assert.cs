using System;
using System.Diagnostics;
using Unity.Collections;
using Debug = UnityEngine.Debug;

namespace DotsNav.Core
{
    static class Assert
    {
        [Conditional("UNITY_EDITOR")]
        public static void IsTrue(bool b)
        {
            if (!b)
            {
                Debug.Log($"<color=red>##### ASSERT</color>");
                throw new AssertException();
            }
        }

        [Conditional("UNITY_EDITOR")]
        public static void IsTrue(bool b, FixedString128 s)
        {
            if (!b)
            {
                Debug.Log(FixedString.Format("<color=red>##### ASSERT =>  {0}</color>", s));
                throw new AssertException();
            }
        }

        class AssertException : Exception { }
    }
}