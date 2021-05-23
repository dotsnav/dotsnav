using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class LineDrawer : MonoBehaviour
{
    struct Line
    {
        public readonly Vector2 From;
        public readonly Vector2 To;
        public readonly Color Color;

        public Line(Vector2 from, Vector2 to, Color color)
        {
            From = from;
            To = to;
            Color = color;
        }
    }

    Material _lineMat;
    readonly List<Line> _lines = new List<Line>();

    void Awake()
    {
        _lineMat = new Material(Shader.Find("Lines/Colored Blended"));
    }

    public void DrawCircle(Vector2 center, Vector2 arm, float angle, Color color, bool delimit = false, int res = 32)
    {
        delimit &= angle < 2 * math.PI;
        var q = quaternion.AxisAngle(Vector3.up, angle / res);
        var currentArm = arm.ToXxY();
        if (delimit)
            DrawLine(center, center + (Vector2)currentArm.xz(), color);
        for (var i = 0; i < res; i++)
        {
            var nextArm = math.mul(q, currentArm);
            DrawLine(center + (Vector2)currentArm.xz(), center + (Vector2)nextArm.xz, color);
            currentArm = nextArm;
        }
        if (delimit)
            DrawLine(center, center + (Vector2)currentArm.xz(), color);
    }

    public void DrawLine(Vector2 from, Vector2 to, Color color)
    {
        _lines.Add(new Line(from, to, color));
    }

    void OnPostRender()
    {
        _lineMat.SetPass(0);
        GL.Begin(GL.LINES);
        for (int i = 0; i < _lines.Count; i++)
        {
            var line = _lines[i];
            GL.Color(line.Color);
            var f = line.From;
            var t = line.To;
            GL.Vertex(f.ToXxY(.005f));
            GL.Vertex(t.ToXxY(.005f));
        }

        GL.End();
        _lines.Clear();
    }

    public void DrawPoly(List<Vector2> poly, Color color)
    {
        if (poly != null)
            for (int i = 0; i < poly.Count - 1; i++)
                DrawLine(poly[i], poly[i + 1], color);
    }

    public void DrawPoint(float2 pos, Color color, float size = .05f)
    {
        var diag = size / 2.8284f;
        var hor = size / 2;
        DrawLine(pos + new float2(-diag, -diag), pos + new float2(diag, diag), color);
        DrawLine(pos + new float2(-diag, diag), pos + new float2(diag, -diag), color);
        DrawLine(pos + new float2(-hor, 0), pos + new float2(hor, 0), color);
        DrawLine(pos + new float2(0, -hor), pos + new float2(0, hor), color);
    }
}