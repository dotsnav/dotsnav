using System.Linq;
using DotsNav.Predicates;
using NUnit.Framework;
using RobustArithmetic.Test.Util;

namespace DotsNav.Test.Predicates.Test
{
    using EA = ExactArithmeticManaged;

    [TestFixture]
    public class ExpansionTests
    {
        [Test]
        public void GrowExpansion_Simple()
        {
            var e = new double[] { 5.0, 1.0e60};
            var h = new double[e.Length + 1];
            var b = 20.0;

            // Call with expansion invariants checked
            var n = GrowExpansion_Checked(e.Length, e, b, h);

            // Check that we get the expected result
            NUnit.Framework.Assert.AreEqual(3, n);
            NUnit.Framework.Assert.AreEqual(0.0, h[0]);
            NUnit.Framework.Assert.AreEqual(25.0, h[1]);
            NUnit.Framework.Assert.AreEqual(e[1], h[2]);
        }

        // This checks all the expansion conditions of S. Theorem 10, but not the actual sum.
        int GrowExpansion_Checked(int elen, double[] e, double b, double[] h)
        {
            // Always conditions for calling GrowExpansion
            NUnit.Framework.Assert.IsTrue(e.Length >= elen);
            NUnit.Framework.Assert.IsTrue(h.Length >= elen + 1);

            NUnit.Framework.Assert.IsTrue(h.Length == e.Length + 1);
            NUnit.Framework.Assert.IsTrue(e.IsNonOverlapping() && e.IsSorted());

            int  e_Length = e.Length;
            bool e_IsNonAdjacent = e.IsNonAdjacent();
            bool e_IsStronglyNonOverlapping = e.IsStronglyNonOverlapping();

            int n = EA.GrowExpansion(elen, e, b, h);

            NUnit.Framework.Assert.AreEqual(e_Length + 1, n);
            NUnit.Framework.Assert.IsTrue(h.IsNonOverlapping() && h.IsSorted());

            // Extra invariants that are maintained by GrowExpansion
            // That NonAdjacent is maintained is part of S. Theorem 10
            if (e_IsNonAdjacent) NUnit.Framework.Assert.IsTrue(h.IsNonAdjacent());
            // That StronglyNonOverlapping is maintained is not part of Theorem 10,
            // but claimed in the predicates.c implementation comments.
            if (e_IsStronglyNonOverlapping) NUnit.Framework.Assert.IsTrue(h.IsStronglyNonOverlapping());

            return n;
        }

        [Test]
        public void ExpansionSum_Simple()
        {
            var e = new double[] { 5.0, 1.0e60 };
            var f = new double[] { 10.0, -3e40, 1e100 };
            var h = new double[e.Length + f.Length];

            // Call with expansion invariants checked
            var p = ExpansionSum_Checked(e, f, h);

            // Check that we get the expected result
            NUnit.Framework.Assert.AreEqual(5, p);
            NUnit.Framework.Assert.AreEqual(0.0, h[0]);
            NUnit.Framework.Assert.AreEqual(15.0, h[1]);
            // TODO: Check a bit more...
        }


        // This checks all the expansion conditions of S. Theorem 12, but not the actual sum.
        public static int ExpansionSum_Checked(double[] e, double[] f, double[] h)
        {
            // Always conditions for calling GrowExpansion
            NUnit.Framework.Assert.IsTrue(h.Length == e.Length + f.Length);
            NUnit.Framework.Assert.IsTrue(e.IsNonOverlapping() && e.IsSorted());
            NUnit.Framework.Assert.IsTrue(f.IsNonOverlapping() && f.IsSorted());

            int  e_Length = e.Length;
            bool e_IsNonAdjacent = e.IsNonAdjacent();
            int  f_Length = f.Length;
            bool f_IsNonAdjacent = f.IsNonAdjacent();

            int p = EA.ExpansionSum(e.Length, e, f.Length, f, h);

            NUnit.Framework.Assert.AreEqual(e_Length + f_Length, p);
            NUnit.Framework.Assert.IsTrue(h.IsNonOverlapping() && h.IsSorted());

            // Extra invariant that is maintained by ExpansionSum
            // That NonAdjacent is maintained is part of S. Theorem 12
            if (e_IsNonAdjacent && f_IsNonAdjacent) NUnit.Framework.Assert.IsTrue(h.IsNonAdjacent());
            // In addition, the predicates.c file asserts that if e is NonAdjacent,
            // then h will also be (without mentioning f) - (maybe a copy from Grow_Expansion?)
            if (e_IsNonAdjacent) NUnit.Framework.Assert.IsTrue(h.IsNonAdjacent());

            return p;
        }

        [Test]
        public void FastExpansionSum_Simple()
        {
            var e = new double[] { 5.0, 1.0e60 };
            var f = new double[] { 10.0, -3e40, 1e100 };
            var h = new double[e.Length + f.Length];

            // Call with expansion invariants checked
            var hlen = FastExpansionSum_Checked(e.Length, e, f.Length, f, h);

            // Check that we get the expected result
            NUnit.Framework.Assert.AreEqual(5, hlen);
            NUnit.Framework.Assert.AreEqual(0.0, h[0]);
            NUnit.Framework.Assert.AreEqual(15.0, h[1]);
            // TODO: Check a bit more...
        }



        // This checks all the expansion conditions of S. Theorem 13, but not the actual sum.
        int FastExpansionSum_Checked(int elen, double[] e, int flen, double[] f, double[] h)
        {
            // Always conditions for calling GrowExpansion
            NUnit.Framework.Assert.IsTrue(e.Length >= elen);
            NUnit.Framework.Assert.IsTrue(f.Length >= flen);
            NUnit.Framework.Assert.IsTrue(h.Length >= elen + flen);
            NUnit.Framework.Assert.IsTrue(e.Take(elen).IsSorted());
            NUnit.Framework.Assert.IsTrue(e.Take(flen).IsStronglyNonOverlapping());
            NUnit.Framework.Assert.IsTrue(f.Take(flen).IsSorted());
            NUnit.Framework.Assert.IsTrue(f.Take(flen).IsStronglyNonOverlapping());

            int hlen = EA.FastExpansionSum(elen, e, flen, f, h);

            NUnit.Framework.Assert.IsTrue(elen + flen == hlen);
            NUnit.Framework.Assert.IsTrue(h.Take(hlen).IsSorted());
            NUnit.Framework.Assert.IsTrue(h.Take(hlen).IsStronglyNonOverlapping());

            return hlen;
        }

        [Test]
        public void FastExpansionSumZeroElim_Simple()
        {
            var e = new double[] { 5.0, 1.0e60, 1.0, -1.0 };
            var elen = 2; // Only first two components used
            var f = new double[] { 10.0, -3e40, 1e100, 5.0 };
            var flen = 3; // Only first three components used
            var h = new double[e.Length + f.Length];

            // Call with expansion invariants checked
            var hlen = FastExpansionSumZeroElim_Checked(elen, e, flen, f, h);

            // Check that we get the expected result
            NUnit.Framework.Assert.AreEqual(4, hlen);
            NUnit.Framework.Assert.AreEqual(15.0, h[0]);
            // TODO: Check a bit more...
        }

        // We run into the problem that FastExpansionSum might change an input when added to 0.0
        // This is not an error - the expansions just change
        [Test]
        public void FastExpansionSumZeroElim_ZeroAdd()
        {
            var e    = new double[] { 1.2019968276161183E-24, -1.6094782383917832E-09 };
            var f    = new double[] { 0.0, 0.0 };
            var hexp = new double[] { -3.8774091213423172E-26, -1.6094782383917819E-09 };
            var h = new double[e.Length + f.Length];

            // Call with expansion invariants checked
            var hlen = FastExpansionSumZeroElim_Checked(e.Length, e, f.Length, f, h);

            // Check that we get the expected result
            NUnit.Framework.Assert.AreEqual(2, hlen);
            NUnit.Framework.Assert.AreEqual(hexp[0], h[0]);
            NUnit.Framework.Assert.AreEqual(hexp[1], h[1]);

            /* The numbers are like this (2 in e[1c] denotes the carry when subtracting)

            e[0]:  0.00000000000000000000000000000000000000000000000000000000000000000000000000000001011101
            e[1]: -0.0000000000000000000000000000011011101001101000111101100110000011110100010111

            h[0]: -0.00000000000000000000000000000000000000000000000000000000000000000000000000000000000011
            h[1]: -0.000000000000000000000000000001101110100110100011110110011000001111010001011011101
            h[+]: -0.00000000000000000000000000000110111010011010001111011001100000111101000101101110100011

            e[1]: -0.00000000000000000000000000000110111010011010001111011001100000111101000101110000000000
            e[1c]:-0.00000000000000000000000000000110111010011010001111011001100000111101000101101111111112
            e[0]:  0.00000000000000000000000000000000000000000000000000000000000000000000000000000001011101
            e[+]: -0.00000000000000000000000000000110111010011010001111011001100000111101000101101110100011
             */
        }

        // This checks all the expansion conditions of S. Theorem 13, but not the actual sum.
        // Adds check that the output expansion has no zero components (except trivial case).
        public static int FastExpansionSumZeroElim_Checked(int elen, double[] e, int flen, double[] f, double[] h)
        {
            // Always conditions for calling GrowExpansion
            NUnit.Framework.Assert.IsTrue(e.Length >= elen);
            NUnit.Framework.Assert.IsTrue(f.Length >= flen);
            NUnit.Framework.Assert.IsTrue(h.Length >= elen + flen);
            NUnit.Framework.Assert.IsTrue(e.Take(elen).IsSorted());
            NUnit.Framework.Assert.IsTrue(e.Take(elen).IsStronglyNonOverlapping());
            NUnit.Framework.Assert.IsTrue(f.Take(flen).IsSorted());
            NUnit.Framework.Assert.IsTrue(f.Take(flen).IsStronglyNonOverlapping());

            int hlen = EA.FastExpansionSumZeroElim(elen, e, flen, f, h);

            NUnit.Framework.Assert.IsTrue(elen + flen >= hlen);
            NUnit.Framework.Assert.IsTrue(h.Take(hlen).IsSorted());
            NUnit.Framework.Assert.IsTrue(h.Take(hlen).IsStronglyNonOverlapping());
            NUnit.Framework.Assert.IsTrue(h.Take(hlen).IsZeroElim());

            return hlen;
        }

        [Test]
        public void Compress_Simple()
        {
            var e = new double[] { 0.0, 1.2, 3e20, 0.0, /*end*/ 5.0 };
            var elen = 4;
            var h = new double[10];
            var hexp = new double[] {1.2, 3e20};
            var hlenexp = 2;

            int hlen = Compress_Checked(elen, e, h);

            NUnit.Framework.Assert.AreEqual(hlenexp, hlen);
            NUnit.Framework.Assert.AreEqual(hexp[0], h[0]);
            NUnit.Framework.Assert.AreEqual(hexp[1], h[1]);
        }

        // This checks the conditions of S. Theorem 23 (except the approximation condition)
        // Compresses an expansion
        int Compress_Checked(int elen, double[] e, double[] h)
        {
            NUnit.Framework.Assert.IsTrue(e.Length >= elen);
            NUnit.Framework.Assert.IsTrue(e.Take(elen).IsNonOverlapping());
            NUnit.Framework.Assert.IsTrue(e.Take(elen).IsSorted());

            int hlen = EA.Compress(elen, e, h);

            NUnit.Framework.Assert.IsTrue(h.Take(hlen).IsNonOverlapping());
            NUnit.Framework.Assert.IsTrue(h.Take(hlen).IsNonAdjacent());
            NUnit.Framework.Assert.IsTrue(h.Take(hlen).IsSorted());
            NUnit.Framework.Assert.IsTrue(h.Take(hlen).IsZeroElim());

            return hlen;
        }

        [Test]
        public void ScaleExpansionZeroElim_Simple()
        {
        }
    }
}
