using System;
using System.Diagnostics;

namespace RobustArithmetic.Test.Util
{
    public class RandomDouble
    {
        readonly Random _random;

        public RandomDouble(int? seed = null)
        {
            _random = seed == null ? new Random() : new Random(seed.Value);
        }

        /// <summary>
        /// Returns a random double from the full range of possible double values.
        /// The sign, mantissa and the exponent are independent random numbers, 
        /// thus the distribution of numbers is not uniform.
        /// </summary>
        /// <returns></returns>
        public double NextDoubleFullRange()
        {
            long mantissa = NextLong(52);
            int exponent = _random.Next(-1023-52, 1024-52);
            int sign = NextBool() ? -1 : +1; // Gets us -1 or +1.
            double result = sign * (double)mantissa * (double)Math.Pow(2.0, exponent);
            var dc = new DoubleComponents(result);
            Debug.Assert(result == dc.CalcValue());
            return result;
        }

        // Returns a double in the range where we expect the robust arithmetic to be valid 
        // - exponents between -142 and 201. (According to S. p.3)
        public double NextDoubleValidRange()
        {
            double result = double.NaN;

            while (!(result.IsNumber() && result.IsInRange()))
            {
                long mantissa = NextLong(52);
                int exponent = _random.Next(-1023 - 52, 1024 - 52);
                int sign = NextBool() ? -1 : +1; // Gets us -1 or +1.
                result = sign * (double)mantissa * (double)Math.Pow(2.0, exponent);
                var dc = new DoubleComponents(result);
                Debug.Assert(result == dc.CalcValue());
            }
            return result;
        }

        long NextLong(int numBits)
        {
            byte[] longBytes = new byte[8];
            _random.NextBytes(longBytes);
            long l = 0;
            for (int i = 0; i < 8; i++)
            {
                l += (long)longBytes[i] << (i * 8);
            }
            return l & ((1L << numBits) - 1);
        }

        bool NextBool()
        {
            return _random.Next(2) == 0; // Next(2) returns 0 or 1
        }
    }
}
