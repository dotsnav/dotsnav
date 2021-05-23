using System;
using NUnit.Framework;

namespace RobustArithmetic.Test.Util.Test
{
    /// <summary>
    /// Summary description for UtilTests
    /// </summary>
    [TestFixture]
    public class UtilTests
    {
        [Test]
        public void DoubleComponents_PreservesValue()
        {
            double d = 0.25;
            var dstr = d.ToExactString();
            var dc = new DoubleComponents(d);

            var dcValue = (dc.Negative ? -1.0 : 1.0) * dc.Mantissa * Math.Pow(2.0, dc.Exponent);

            Assert.AreEqual(d, dcValue);
        }

        [Test]
        public void DoubleComponents_PreservesValue_Random()
        {
            int testCount = 1000000;
            Random r = new Random(0);
            for (int i = 0; i < testCount; i++)
            {
                double d = r.NextDouble();

            }
        }

        [Test]
        public void NonOverlapping_Are_NonOverlapping()
        {
            var d1 = DoubleConverter.FromFloatingPointBinaryString("1100");
            var d2 = DoubleConverter.FromFloatingPointBinaryString("-10.1");

            Assert.IsTrue(ExpansionExtensions.AreNonOverlapping(d1, d2));
        }

        [Test]
        public void Overlapping_AreNot_NonOverlapping()
        {
            var d1 = DoubleConverter.FromFloatingPointBinaryString("101");
            var d2 = DoubleConverter.FromFloatingPointBinaryString("10");

            Assert.IsFalse(ExpansionExtensions.AreNonOverlapping(d1, d2));
        }

        [Test]
        public void StronglyNonOverlapping()
        {
            // Two examples that are strongly nonoverlapping (S.p12)
            var sn1 = new[] {   DoubleConverter.FromFloatingPointBinaryString("11000"),
                                DoubleConverter.FromFloatingPointBinaryString(   "11") };

            var sn2 = new[] {   DoubleConverter.FromFloatingPointBinaryString("10000"),
                                DoubleConverter.FromFloatingPointBinaryString( "1000"),
                                DoubleConverter.FromFloatingPointBinaryString(   "10"),
                                DoubleConverter.FromFloatingPointBinaryString(    "1") };

            Assert.IsTrue(sn1.IsStronglyNonOverlapping());
            Assert.IsTrue(sn2.IsStronglyNonOverlapping());

            // Two examples that are _not_ strongly nonoverlapping (S.p12)
            var nsn1 = new[] {  DoubleConverter.FromFloatingPointBinaryString("11100"),
                                DoubleConverter.FromFloatingPointBinaryString(   "11") };

            var nsn2 = new[] {  DoubleConverter.FromFloatingPointBinaryString("100"),
                                DoubleConverter.FromFloatingPointBinaryString( "10"),
                                DoubleConverter.FromFloatingPointBinaryString(  "1") };

            Assert.IsFalse(nsn1.IsStronglyNonOverlapping());
            Assert.IsFalse(nsn2.IsStronglyNonOverlapping());

        }

        [Test]
        public void FloatingPointBinaryString_MatchesValue()
        {
            var tests = new[] 
            { 
                Tuple.Create(4.0,  "100.0"),
                Tuple.Create(0.25, "0.01"),
                Tuple.Create(0.0, "0.0"),
                Tuple.Create(-2.5, "-10.1"),
                Tuple.Create(double.MaxValue,  "1111111111111111111111111111111111111111111111111111100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000.0"),
                Tuple.Create(double.MinValue, "-1111111111111111111111111111111111111111111111111111100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000.0"),
                Tuple.Create(double.Epsilon, "0.000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001"),
                Tuple.Create(DoubleEpsilonSplitter().Item1, "0.00000000000000000000000000000000000000000000000000001"), // biggest exponent of 2 s.t. 1.0 + eps = 1.0
            };

            foreach (var t in tests)
            {
                string r = DoubleConverter.ToFloatingPointBinaryString(t.Item1);
                Assert.AreEqual(t.Item2, r);
                double d = DoubleConverter.FromFloatingPointBinaryString(r);
                Assert.AreEqual(t.Item1, d);
            }
        }

        [Test]
        public void FloatingPoint_CheckContants()
        {
            var des = DoubleEpsilonSplitter();
            var epsilon = des.Item1;
            var splitter = des.Item2;

            double dd = 1.1102230246251565E-16;

            double d = Math.Pow(2.0, -53);
            string str = d.ToExactString();
            string str2 = d.ToString("R");
            Assert.AreEqual(d, dd);
            Assert.AreEqual(d, epsilon);
            Assert.AreEqual(Math.Pow(2.0, 27) + 1.0, splitter);
            Assert.AreEqual((1 << 27) + 1.0, splitter);
            Assert.AreEqual(Math.Pow(2.0, Math.Ceiling(53.0 / 2.0)) + 1.0, splitter);
        }

        [Test]
        public void GetRandom()
        {
            RandomDouble rd = new RandomDouble();
//            foreach (var i in Enumerable.Range(0, 10000))
            {
                double d = rd.NextDoubleFullRange();
            }
        }

        [Test]
        public void OptimTest()
        {
            // 53 ones
            string a_s = "11111111111111111111111111111111111111111111111111110.0";
            double a = DoubleConverter.FromFloatingPointBinaryString( "11111111111111111111111111111111111111111111111111110.0");
            double b = DoubleConverter.FromFloatingPointBinaryString("-11111111111111111111111111111111111111111111111111110.0");
            double c = DoubleConverter.FromFloatingPointBinaryString( "00000000000000000000000000000000000000000000000000000.01");

            string a_s2 = DoubleConverter.ToFloatingPointBinaryString(9.0071992547409900e+0015);
            Assert.AreEqual(a_s, a_s2);

            bool ok = Test(a,b,c);
            Assert.IsTrue(ok);

            double acc = TestAcc(a, b, c);
            double acc2 = TestAcc(a, b, c);
            if (acc != acc2)
            {
                Assert.Fail();
            }
            Assert.AreEqual(0.0, acc);
            Assert.AreEqual(0.0, acc2);

            double tst2 = Test2(a, b, c);
            if (tst2 != 0.0)
            {
                Assert.Fail();
            }

        }

        public bool Test(double a, double b, double c)
        {
            // Check rounding.
            double d = (double)(a + c);
            if (a != d) return false;

            double e = (double)(a + c + b);
            if (e != 0.0) return false;

            double f = (double)((double)(a + c) + b);
            if (f != 0.0) return false;

            return true;
        }

        public double TestAcc(double a, double b, double c)
        {
            double a_ = (double)a;
            double b_ = (double)b;
            double c_ = (double)c;
            double acc = (a_ + c_) + b_;
            return acc;
        }

        public double Test2(double a, double b, double c)
        {
            double a_ = (double)a;
            double c_ = (double)c;
            double d_ = a_ + c_;
            return a_ == d_ ? 0.0 : 1.0;
        }

        [Test]
        public void BitWidth_Simple()
        {
            double d = DoubleConverter.FromFloatingPointBinaryString("-0.001010101000");
            int width = d.BitWidth();
            Assert.AreEqual(7, width);
        }

        [Test]
        public void BitWidth_Large()
        {
            double d = DoubleConverter.FromFloatingPointBinaryString("1010101" + '0'.Repeat(200));
            int width = d.BitWidth();
            Assert.AreEqual(7, width);
        }

        // From Shewchuk (predicate.c)
        /*  `epsilon' is the largest power of two such that 1.0 + epsilon = 1.0 in   */
        /*  floating-point arithmetic.  `epsilon' bounds the relative roundoff       */
        /*  error.  It is used for floating-point error analysis.                    */
        /*                                                                           */
        /*  `splitter' is used to split floating-point numbers into two half-        */
        /*  length significands for exact multiplication.                            */
        Tuple<double, double> DoubleEpsilonSplitter()
        {
            bool every_other = true;
            const double half = 0.5;
            double epsilon = 1.0;
            double splitter = 1.0;
            double check = 1.0;
            double lastcheck;

            /* Repeatedly divide `epsilon' by two until it is too small to add to   */
            /*   one without causing roundoff.                                      */
            do
            {
                lastcheck = check;
                epsilon *= half;
                if (every_other)
                {
                    splitter *= 2.0;
                }
                every_other = !every_other;
                check = (double)(1.0 + epsilon);
            } while ((check != 1.0) && (check != lastcheck));
            splitter += 1.0;
            return Tuple.Create(epsilon, splitter);
        }
    }


}
