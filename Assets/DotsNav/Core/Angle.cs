using System;
using System.Diagnostics;
using Unity.Mathematics;

namespace DotsNav
{
    [DebuggerDisplay("{_value} ({_value * 57.295779513f})")]
    readonly struct Angle : IFormattable
    {
        const float Pi2 = 2 * math.PI;
        readonly float _value;

        public Angle(float2 vector)
        {
            _value = math.atan2(vector.x, vector.y);
        }

        Angle(float angle)
        {
            _value = angle;
        }

        public static Angle Clamp(Angle angle, Angle from, Angle to)
        {
            var fromTo = to - from;
            var angleFrom = from - angle;
            var angleTo = to - angle;
            if (fromTo >= 0 == angleTo >= 0 && fromTo >= 0 != angleFrom >= 0)
                return angle;
            return math.abs(angleFrom) < math.abs(angleTo) ? from : to;
        }

        public static Angle Average(Angle from, Angle to) => from + (to - from) / 2;

        static float NormalizeAngle(float angle)
        {
            if (angle < -math.PI)
                angle += Pi2;
            else if (angle > math.PI)
                angle -= Pi2;
            return angle;
        }

        static float NormalizeAnyAngle(float angle)
        {
            while (angle < -math.PI)
                angle += Pi2;
            while (angle > math.PI)
                angle -= Pi2;
            return angle;
        }

        public float GetContinuousAngle(Angle angle) => _value + (float) (angle - _value);

        public static implicit operator float(Angle v) => v._value;
        public static implicit operator Angle(float v) => new Angle(NormalizeAnyAngle(v));
        public static Angle operator +(Angle a, Angle b) => new Angle(NormalizeAngle(a._value + b._value));
        public static Angle operator -(Angle a, Angle b) => new Angle(NormalizeAngle(a._value - b._value));
        public static Angle operator *(Angle a, float b) => new Angle(NormalizeAnyAngle(a._value * b));
        public static Angle operator *(float a, Angle b) => new Angle(NormalizeAnyAngle(a * b._value));
        public static Angle operator /(Angle a, float b) => new Angle(NormalizeAnyAngle(a._value / b));

        public override string ToString() => $"{_value:0.00}";
        public string ToString(string format, IFormatProvider formatProvider) => _value.ToString(format, formatProvider);

        public static Angle Lerp(in Angle a, Angle b, float f) => math.lerp(a, a.GetContinuousAngle(b), f);

        public float2 ToVector() => Math.Rotate(_value);
    }
}