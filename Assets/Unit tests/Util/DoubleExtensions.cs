using System;

namespace RobustArithmetic.Test.Util
{
    public static class DoubleExtensions
    {
        public static bool IsNumber(this double d)
        {
            if (double.IsInfinity(d) || double.IsNaN(d)) return false;
            return true;
        }

        public static bool IsPowerOfTwo(this double d)
        {
            var dc = new DoubleComponents(d);
            dc.MaximizeExponent();
            return dc.Mantissa == 1;
        }

        // Checks whether a number is in the range where we expect the robust arithmetic to work
        // - exponents between -142 and 201. (According to S. p.3)
        public static bool IsInRange(this double d)
        {
            double dabs = Math.Abs(d);
            return dabs >= 1e-142 && dabs < 1e202;
        }

        /// <summary>
        ///  Returns the width of the significand of d
        ///  E.g. if d = -0100.01 it returns 5
        ///  CONSIDER: This takes into account the implicit leading 1 for normalised doubles.
        ///            I'm still a bit confused about how to consider this.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static int BitWidth(this double d)
        {
            return d.ToFloatingPointBinaryString()
                    .Replace(".", "")
                    .TrimStart('-', '0')
                    .TrimEnd('0')
                    .Length;
        }
    }
}
