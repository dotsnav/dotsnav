using System;
using Unity.Jobs;
using UnityEngine;

namespace DotsNav.Drawing
{
    /// <summary>
    /// Attach to any gameobject to render in the scene view, or the camera to also render in the game view
    /// </summary>
    public class DotsNavRenderer : MonoBehaviour
    {
        [Min(1000)]
        public int MaxLines = 250 * 1000;
        public bool DrawInGameView;
        public static JobHandle Handle;

        void Awake()
        {
            if (DrawInGameView && GetComponent<Camera>() == null)
                throw new Exception("DebugDisplay needs to be attached to the camera gameobject to draw in the game view");

            Assert.IsTrue(Managed.Instance == null);
            Managed.Instance = new Managed(MaxLines);
        }

        void OnPostRender()
        {
            if (DrawInGameView)
                Render();
        }

        void OnDrawGizmos()
        {
            if (Application.isPlaying)
                Render();
        }

        void OnDestroy()
        {
            Managed.Instance?.Dispose();
            Managed.Instance = null;
        }

        static void Render()
        {
            Handle.Complete();
            Managed.Instance.CopyFromCpuToGpu();
            Managed.Instance.Render();
        }

        internal static void Clear()
        {
            if (Managed.Instance != null)
                Managed.Instance.Clear();
        }
    }
}