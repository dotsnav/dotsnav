using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace DotsNav.Drawing
{
    class Managed : IDisposable
    {
        readonly Material _lineMaterial;
        int _numLinesToDraw = 0;

        ComputeBuffer _lineVertexBuffer; // one big 1D array of line vertex positions.
        ComputeBuffer _colorBuffer;

        static Managed _instance;
        static readonly int ColorBuffer = Shader.PropertyToID("colorBuffer");
        static readonly int PositionBuffer = Shader.PropertyToID("positionBuffer");

        internal static Managed Instance;

        internal Managed(int maxLines)
        {
            Unmanaged.Instance.Data.Initialize(maxLines);
            _lineMaterial = Resources.Load<Material>("LineMaterial");
#if !UNITY_DOTSRUNTIME
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
#endif
        }
        static void OnDomainUnload(object sender, EventArgs e)
        {
            _instance?.Dispose();
        }

        internal void CopyFromCpuToGpu()
        {
            // Recreate compute buffer if needed.
            if (_colorBuffer == null || _colorBuffer.count != Unmanaged.Instance.Data.ColorData.Length)
            {
                if (_colorBuffer != null)
                {
                    _colorBuffer.Release();
                    _colorBuffer = null;
                }

                _colorBuffer = new ComputeBuffer(Unmanaged.Instance.Data.ColorData.Length, UnsafeUtility.SizeOf<float4>());
                _lineMaterial.SetBuffer(ColorBuffer, _colorBuffer);
                _colorBuffer.SetData(Unmanaged.Instance.Data.ColorData.ToNativeArray());
            }

            if (_lineVertexBuffer == null || _lineVertexBuffer.count != Unmanaged.Instance.Data.LineBuffer.Instance.Length)
            {
                if (_lineVertexBuffer != null)
                {
                    _lineVertexBuffer.Release();
                    _lineVertexBuffer = null;
                }

                _lineVertexBuffer = new ComputeBuffer(Unmanaged.Instance.Data.LineBuffer.Instance.Length, UnsafeUtility.SizeOf<float4>());
                _lineMaterial.SetBuffer(PositionBuffer, _lineVertexBuffer);
            }

            _numLinesToDraw = Unmanaged.Instance.Data.LineBufferAllocations.Filled;
            _lineVertexBuffer.SetData(Unmanaged.Instance.Data.LineBuffer.Instance.ToNativeArray(), 0, 0, _numLinesToDraw * 2);
        }

        internal void Clear()
        {
            Unmanaged.Instance.Data.Clear();
        }

        internal void Render()
        {
            _lineMaterial.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Lines, _numLinesToDraw * 2);
        }

/*
        internal void Render(HDCamera hdCamera, CommandBuffer cmd)
        {
            if (hdCamera.camera.cameraType != CameraType.Game)
                return;
            cmd.DrawProcedural(Matrix4x4.identity, resources.textMaterial, 0, MeshTopology.Triangles, NumTextBoxesToDraw * 6, 1);
            cmd.DrawProcedural(Matrix4x4.identity, resources.graphMaterial, 0, MeshTopology.Triangles, NumGraphsToDraw * 6, 1);
        }

        internal void Render3D(HDCamera hdCamera, CommandBuffer cmd)
        {
            cmd.DrawProcedural(Matrix4x4.identity, _lineMaterial, 0, MeshTopology.Lines, NumLinesToDraw, 1);
        }
*/

        public void Dispose()
        {
            _lineVertexBuffer?.Dispose();
            _lineVertexBuffer = null;
            _colorBuffer?.Dispose();
            _colorBuffer = null;

            Unmanaged.Instance.Data.Dispose();
            if (_instance == this)
                _instance = null;
        }
    }
}