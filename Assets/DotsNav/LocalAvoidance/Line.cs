﻿using Unity.Mathematics;

namespace DotsNav.LocalAvoidance
{
    public struct Line
    {
        public float2 Point;
        public float2 Direction;

        public override string ToString()
        {
            return $"Line: {Point} => {Direction}";
        }
    }
}