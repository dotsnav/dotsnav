using System.Diagnostics;
using NUnit.Framework;
using RobustArithmetic.Test.Util;
using RobustGeometricPredicates;

namespace DotsNav.Test.Predicates.Test
{
    using EA = ExactArithmetic;
    [TestFixture]
    public class SumTests
    {
        [Test]
        public void FastTwoSum_SameSign()
        {
            // Example ismilar to Shewchuk p. 7 - Figure 3 (expanded for double)
            // We expect a+b=x+y

            double a = DoubleConverter.FromFloatingPointBinaryString('1'.Repeat(53) + "00");                   //  1111...1100
            double b = DoubleConverter.FromFloatingPointBinaryString("1001");                                  //         1001
            double x_correct = DoubleConverter.FromFloatingPointBinaryString("1" + '0'.Repeat(51) + "1000");   // 1000...00100
            double y_correct = DoubleConverter.FromFloatingPointBinaryString("-11");                           //         - 11

            double x; double y;
            EA.FastTwoSum(a, b, out x, out y);

            NUnit.Framework.Assert.AreEqual(x_correct, x);
            NUnit.Framework.Assert.AreEqual(y_correct, y);
        }

        [Test]
        public void FastTwoSum_OppositeSign()
        {
            // Adapted example from Shewchuk p. 7 - Figure 4 - expanded to double
            // We expect a+b=x+y
            double a = DoubleConverter.FromFloatingPointBinaryString("1" + '0'.Repeat(51) + "10");              //  100...0010
            double b = DoubleConverter.FromFloatingPointBinaryString("-" + '1'.Repeat(49) + "1011");            //  -11...1011
            double x_correct = DoubleConverter.FromFloatingPointBinaryString("111");                            //         111
            double y_correct = 0.0;

            double x; double y;
            EA.FastTwoSum(a, b, out x, out y);

            NUnit.Framework.Assert.AreEqual(x_correct, x);
            NUnit.Framework.Assert.AreEqual(y_correct, y);
        }

        [Test]
        public void FastTwoSum_ResultsNonOverlappingNonAdjacent_Random()
        {
            var rnd = new RandomDouble(1); // Use a specific seed to ensure repeatability
            int testCount = 1000000;
            int testsPerformed = 0;

            for (int i = 0; i < testCount; i++)
            {
                double a = rnd.NextDoubleFullRange();
                double b = rnd.NextDoubleFullRange();
                double x; double y;

                if (System.Math.Abs(a) >= System.Math.Abs(b))
                {
                    testsPerformed++;
                    EA.FastTwoSum(a, b, out x, out y);
                    NUnit.Framework.Assert.IsTrue(ExpansionExtensions.AreNonOverlapping(x, y));
                    NUnit.Framework.Assert.IsTrue(ExpansionExtensions.AreNonAdjacent(x, y));
                }
            }

            Debug.Print("FastTwoSum_MaintainsNonOverlapping_Random Tested {0} out of {1} tries", testsPerformed, testCount);
        }

        [Test]
        public void TwoSum_Small_a()
        {
            // This is an example that FastTwoSum gets wrong (|a|<|b|), but TwoSum does right
            // Shewchuck p. 8, Figure 5
            double a = DoubleConverter.FromFloatingPointBinaryString('1'.Repeat(51) + ".11");       //    111...111.11
            double b = DoubleConverter.FromFloatingPointBinaryString("110" + '0'.Repeat(49) + "1"); //  11000...001
            double x_correct = DoubleConverter.FromFloatingPointBinaryString("1" + '0'.Repeat(53)); // 100000...000
            double y_correct = DoubleConverter.FromFloatingPointBinaryString("0.11");               //            0.11

            double x; double y;
            EA.TwoSum(a, b, out x, out y);

            NUnit.Framework.Assert.AreEqual(x_correct, x);
            NUnit.Framework.Assert.AreEqual(y_correct, y);
        }

        [Test]
        public void TwoSum_ResultsNonOverlappingNonAdjacent_Random()
        {
            var rnd = new RandomDouble(1); // Use a specific seed to ensure repeatability
            int testCount = 1000000;

            for (int i = 0; i < testCount; i++)
            {
                double a = rnd.NextDoubleFullRange();
                double b = rnd.NextDoubleFullRange();
                double x; double y;

                EA.TwoSum(a, b, out x, out y);
                NUnit.Framework.Assert.IsTrue(ExpansionExtensions.AreNonOverlapping(x, y));
                NUnit.Framework.Assert.IsTrue(ExpansionExtensions.AreNonAdjacent(x, y));
            }
        }

        public static void TwoTwoDiff_Checked(double a1, double a0, double b1, double b0, out double x3, out double x2, out double x1, out double x0)
        {
            double[] a = new double[] { a0, a1 };
            double[] b = new double[] { -b0, -b1 };

            double[] x = new double[4];

            int xlen = ExpansionTests.ExpansionSum_Checked(a, b, x);

            x0 = x[0];
            x1 = x[1];
            x2 = x[2];
            x3 = x[3];
        }
    }
}
