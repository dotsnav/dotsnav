using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RobustArithmetic.Test.Util
{
    /// <summary>
    /// Various checks and conditions related for arbitrary-precision double expansions, 
    /// implementing the various conditions from Shewchuk's paper.
    /// </summary>
    public static class ExpansionExtensions
    {
        /// <summary>
        /// "Two floating-point values x and y are nonoverlapping if 
        ///  there exist integers r and s such that x = r.2^s and |y| &lt; 2^s, 
        ///  OR y = r.2^s and |x| &lt; 2^s." 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>true if x and y are nonoverlapping</returns>
        public static bool AreNonOverlapping(double x, double y)
        {
            return x.NonOverlaps(y) || y.NonOverlaps(x);
        }

        static bool NonOverlaps(this double x, double y)
        {
            // Check non-number flags
            if (!x.IsNumber()) throw new ArgumentOutOfRangeException("x");
            if (!y.IsNumber()) throw new ArgumentOutOfRangeException("y");

            // Special case for 0:
            // Shewchuk p3: "The number zero does not overlap any number."
            if (x == 0.0 || y == 0.0) return true;

            // Break into components
            var xc = new DoubleComponents(x);

            // We now have that x = xc.Mantissa * 2 ^ xc.Exponent

            // Normalize - this writes x = r.2^s with the maximal exponent s
            xc.MaximizeExponent();

            // now compare abs of y with 2^s
            return (double)Math.Abs(y) < (double)(Math.Pow(2.0, xc.Exponent));
        }

        public static bool AreOverlapping(double x, double y)
        {
            return !AreNonOverlapping(x, y);
        }

        /// <summary>
        /// Shewchuk p. 3
        /// "Two floating-point values x and y are adjacent if they overlap, if x overlaps 2y, or if 2x overlaps y."
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>true if x and y are adjacent</returns>
        public static bool AreAdjacent(double x, double y)
        {
            return AreOverlapping(x, y) || AreOverlapping(x, 2 * y) || AreOverlapping(2 * x, y);
        }

        public static bool AreNonAdjacent(double x, double y)
        {
            return !AreAdjacent(x,y);
        }

        /// <summary>
        /// Returns 
        /// The order of adjacency is not important, so the order of entries in the returned tuple is not significant.
        /// </summary>
        /// <param name="doubles"></param>
        /// <returns>a list of the adjacent tuples from the given list.</returns>
        static IEnumerable<Tuple<double, double>> GetAdjacent(IEnumerable<double> doubles)
        {
            // We only have to check if d1 < d2, since the adjacency check is symmetric.
            var adjacent = from d1 in doubles
                           from d2 in doubles
                           where d1 < d2 && AreAdjacent(d1, d2)
                           select Tuple.Create(d1, d2);
            return adjacent;
        }

        /// <summary>
        /// S.p3 "An expansion is nonoverlapping if all its components are mutually nonoverlapping."
        /// </summary>
        /// <param name="doubles"></param>
        /// <returns>true if the given doubles are nonoverlapping.</returns>
        public static bool IsNonOverlapping(this IEnumerable<double> doubles)
        {
            var overlapping = from d1 in doubles
                              from d2 in doubles
                              where d1 < d2 && AreOverlapping(d1, d2)
                              select Tuple.Create(d1, d2);
            return !overlapping.Any();
        }

        /// <summary>
        /// S.p12 "An expansion is strongly nonoverlapping if no two of its components are overlapping, 
        /// no component is adjacent to two other components, and any pair of adjacent components have the property 
        /// that both components can be expressed with a one-bit significand (that is, both are powers of two)."
        /// 
        /// </summary>
        /// <param name="doubles"></param>
        /// <returns>true if the given doubles are stringly nonoverlapping</returns>
        public static bool IsStronglyNonOverlapping(this IEnumerable<double> doubles)
        {
            if (!doubles.IsNonOverlapping()) return false;
            
            var adjacent = GetAdjacent(doubles).ToList();
            var counts = from pair in adjacent
                         from entry in new[] { pair.Item1, pair.Item2 }
                         group entry by entry into grp
                         select new { Value = grp.Key, Count = grp.Count() };
            if (counts.Any(c => c.Count > 1)) return false;

            // Here is the check that both components of an adjacent pair are powers of two
            if (adjacent.Any(t => !t.ArePowersOfTwo())) return false;

            return true;
        }


        /// <summary>
        /// S.p3 "An expansion is nonadjacent if no two of its components are adjacent."
        /// </summary>
        /// <param name="doubles"></param>
        /// <returns>true if the given doubles are nonadjacent</returns>
        public static bool IsNonAdjacent(this IEnumerable<double> doubles)
        {
            return !GetAdjacent(doubles).Any();
        }

        /// <summary>
        /// Checks that the components are "sorted in order of increasing magnitude, 
        ///     except that any of the components may be zero".
        /// This is a condition used in Shewchuk Theorem 10, p. 10.
        /// </summary>
        /// <param name="doubles"></param>
        /// <returns>true if the given doubles are nonadjacent</returns>
        public static bool IsSorted(this IEnumerable<double> doubles)
        {
            double max = 0.0;

            foreach (double d in doubles)
            {
                double mag = Math.Abs(d);
                if (mag == 0.0) continue;
                if (mag < max) return false;
                max = mag;
            }
            return true;
        }

        /// <summary>
        /// Checks whether an expansion has been zero-eliminated.
        /// Either the expansion must have exactly one element, which is 0.0, 
        /// or the expansion must have no 0.0 elements.
        /// </summary>
        /// <param name="doubles"></param>
        /// <returns></returns>
        public static bool IsZeroElim(this IEnumerable<double> doubles)
        {
            return (doubles.Count() == 1 && doubles.First() == 0.0) || !doubles.Contains(0.0);
        }

        static bool ArePowersOfTwo(this Tuple<double, double> doubles)
        {
            return doubles.Item1.IsPowerOfTwo() && doubles.Item2.IsPowerOfTwo();
        }

        public static void Print(this IEnumerable<double> e, int elen, double scale)
        {
            Debug.Print("----------");
            foreach (var d in e.Take(elen))
            {
                Debug.Print((d / scale).ToString("R"));
            }
        }
    }


    public class DoubleComponents
    {
        public long Bits;
        public bool Negative;
        public int Exponent;
        public long Mantissa;

        // Store a copy of the initial value for comparison.
        double _value;

        public DoubleComponents(double d)
        {
            if (!d.IsNumber()) throw new ArgumentOutOfRangeException("d");

            _value = d;
            // Translate the double into sign, exponent and mantissa.
            Bits = BitConverter.DoubleToInt64Bits(d);
            // Note that the shift is sign-extended, hence the test against -1 not 1
            Negative = (Bits < 0);
            Exponent = (int)((Bits >> 52) & 0x7ffL);
            Mantissa = Bits & 0xfffffffffffffL;

            // Subnormal numbers; exponent is effectively one higher,
            // but there's no extra normalisation bit in the mantissa
            if (Exponent == 0)
            {
                Exponent++;
            }
            else
            {
                // Normal numbers; leave exponent as it is but add extra
                // bit to the front of the mantissa
                Mantissa = Mantissa | (1L << 52);
            }

            // Bias the exponent. It's actually biased by 1023, but we're
            // treating the mantissa as m.0 rather than 0.m, so we need
            // to subtract another 52 from it.
            Exponent -= 1075;
            AssertValid();
        }

        void AssertValid()
        {
            if (CalcValue() != _value) throw new Exception("Invalid DoubleComponents!");
        }

        public double CalcValue()
        {
            double sign = Negative ? -1.0 : 1.0;
            double exp = Math.Pow(2.0, Exponent);
            double val = sign * (double)Mantissa * (double)exp;
            return val;
        }

        // Makes the exponent as big is possible, and makes the mantissa odd, 
        // by shifting mantissa until it is odd (has a 1 as the least significant bit)
        public void MaximizeExponent()
        {
            if (Mantissa == 0L) 
            {
                Exponent = 0;
                return; 
            }
            while ((Mantissa & 1) == 0)
            {    /*  i.e., Mantissa is even */
                Mantissa >>= 1;
                Exponent++;
            }
            AssertValid();
        }

    }
}
