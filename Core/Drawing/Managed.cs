using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace DotsNav.Core.Drawing
{
    class Managed : IDisposable
    {
        readonly Material _lineMaterial;
        int _vertsTodraw;

        ComputeBuffer _lineVertexBuffer; // one big 1D array of line vertex positions.
        // ComputeBuffer _colorBuffer;

        static Managed _instance;
        // static readonly int ColorBuffer = Shader.PropertyToID("colorBuffer");
        static readonly int PositionBuffer = Shader.PropertyToID("DotsNavPos");

        internal static Managed Instance;
        bool _warned;

        internal Managed(int maxLines)
        {
            Unmanaged.Instance.Data.Initialize(maxLines);
            _lineMaterial = Resources.Load<Material>("DotsNavLineMaterial");
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
            // if (_colorBuffer == null || _colorBuffer.count != Unmanaged.Instance.Data.ColorData.Length)
            // {
            //     if (_colorBuffer != null)
            //     {
            //         _colorBuffer.Release();
            //         _colorBuffer = null;
            //     }
            //
            //     _colorBuffer = new ComputeBuffer(Unmanaged.Instance.Data.ColorData.Length, UnsafeUtility.SizeOf<float4>());
            //     _lineMaterial.SetBuffer(ColorBuffer, _colorBuffer);
            //     _colorBuffer.SetData(Unmanaged.Instance.Data.ColorData.ToNativeArray());
            // }

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

            _vertsTodraw = Unmanaged.Instance.Data.LineBufferAllocations.Filled * 2;
            if (!_warned && _vertsTodraw == _lineVertexBuffer.count)
            {
                _warned = true;
                Debug.Log($"### Warning - Maximum number of lines reached, additional lines will not be drawn");
            }

            _lineVertexBuffer.SetData(Unmanaged.Instance.Data.LineBuffer.Instance.ToNativeArray(), 0, 0, _vertsTodraw);
        }

        internal void Clear()
        {
            Unmanaged.Instance.Data.Clear();
        }

        internal void Render()
        {
            _lineMaterial.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Lines, _vertsTodraw);
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
            // _colorBuffer?.Dispose();
            // _colorBuffer = null;

            Unmanaged.Instance.Data.Dispose();
            if (_instance == this)
                _instance = null;
        }
    }
}