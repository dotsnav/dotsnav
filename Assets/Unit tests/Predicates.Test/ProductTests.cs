using System.Diagnostics;
using System.Linq;
using DotsNav.Predicates;
using NUnit.Framework;
using RobustArithmetic.Test.Util;

namespace DotsNav.Test.Predicates.Test
{
    using EA = ExactArithmeticManaged;
    
    [TestFixture]
    public class ProductTests
    {
        [Test]
        public void Split_Simple()
        {
            double d = DoubleConverter.FromFloatingPointBinaryString("-0.001010101000");
            double ahi;
            double alo;

            Split_Checked(d, out ahi, out alo);
        }

        [Test]
        public void Split_FullPrecision()
        {
            var dstr = "1." + '1'.Repeat(52);
            double d = DoubleConverter.FromFloatingPointBinaryString(dstr);

            var dstr2 = d.ToFloatingPointBinaryString();
            NUnit.Framework.Assert.AreEqual(dstr, dstr2);

            double ahi;
            double alo;

            Split_Checked(d, out ahi, out alo);
        }

        [Test]
        public void Split_FullPrecisionLarge()
        {
            // -1111...(53)...11111000000....000000000
            var dstr = "-1" + '1'.Repeat(52) + '0'.Repeat(200);
            double d = DoubleConverter.FromFloatingPointBinaryString(dstr);

            var dstr2 = d.ToFloatingPointBinaryString();
            NUnit.Framework.Assert.AreEqual(DoubleConverter.FromFloatingPointBinaryString(dstr),
                            DoubleConverter.FromFloatingPointBinaryString(dstr2));

            double ahi;
            double alo;

            Split_Checked(d, out ahi, out alo);
        }

        // These are the conditions from S. Theorem 17
        // We split a double into two numbers, together having one fewer bit
        void Split_Checked(double a, out double ahi, out double alo)
        {
            // Precision (keeping in mind the normal 1)
            int p = 53;
            // Splitting point is chosen as
            int s = 27;

            EA.Split(a, out ahi, out alo);

            NUnit.Framework.Assert.IsTrue(ahi.BitWidth() <= p - s); // 26 = floor(p/2)
            NUnit.Framework.Assert.IsTrue(alo.BitWidth() <= s - 1); // 26 = floor(p/2)
            NUnit.Framework.Assert.IsTrue(System.Math.Abs(ahi) >= System.Math.Abs(alo));
            NUnit.Framework.Assert.IsTrue(a == ahi + alo);
        }

        [Test]
        public void TwoProduct_Simple()
        {
            double x, y;
            TwoProduct_Checked(-3e30, 123456789, out x, out y);
        }

        // These are the conditions from S. Theorem 18
        // multiplies and results in a nonoverlapping and nonadjacent expansion
        public static void TwoProduct_Checked(double a, double b, out double x, out double y)
        {
            EA.TwoProduct(a, b, out x, out y);

            // Don't check in case we'd overflow...
            if (!(2.0 * x).IsNumber() || !(2.0 * y).IsNumber()) return;

            var result = new[] { x, y };
            NUnit.Framework.Assert.IsTrue(result.IsNonOverlapping());
            NUnit.Framework.Assert.IsTrue(result.IsNonAdjacent());
            NUnit.Framework.Assert.AreEqual(a * b, x + y);
        }

        // (((Non in the valid range)))
        //[Test]
        public void TwoProduct_ResultNonOverlapping()
        {
            double a = -1.4923680386317254E-201;
            string astr = a.ToFloatingPointBinaryString();

            double b = -1.6707649245302712E-111;
            string bstr = b.ToFloatingPointBinaryString();

            double x, y;

            EA.TwoProduct(a, b, out x, out y);
            string xstr = x.ToFloatingPointBinaryString();
            string ystr = y.ToFloatingPointBinaryString();

            var result = new[] { x, y };
            // FAILS! But our input is not in the valid range according to S. p3
            NUnit.Framework.Assert.IsTrue(result.IsNonOverlapping());
        }

        [Test]
        public void TwoProduct_Random()
        {
            var rnd = new RandomDouble(3); // Use a specific seed to ensure repeatability
            int testCount = 100000;

            for (int i = 0; i < testCount; i++)
            {
                double a = rnd.NextDoubleValidRange();
                double b = rnd.NextDoubleValidRange();

                double x; double y;
                TwoProduct_Checked(a, b, out x, out y);
            }

            Debug.Print("TwoProduct_Random Tested {0} tries", testCount);
        }

        [Test]
        public void TwoProductPreSplit_Random()
        {
            // Just checks that the result from pre-split is same as Split + TwoProduct
            var rnd = new RandomDouble(4); // Use a specific seed to ensure repeatability
            int testCount = 100000;

            for (int i = 0; i < testCount; i++)
            {
                double a = rnd.NextDoubleValidRange();
                double b = rnd.NextDoubleValidRange();

                double x; double y;
                TwoProduct_Checked(a, b, out x, out y);

                double bhi, blo;
                EA.Split(b, out bhi, out blo);

                double xps, yps;
                EA.TwoProductPresplit(a, b, bhi, blo, out xps, out yps);

                NUnit.Framework.Assert.AreEqual(x, xps);
                NUnit.Framework.Assert.AreEqual(y, yps);
            }

            Debug.Print("TwoProduct_Random Tested {0} tries", testCount);
        }

        [Test]
        public void TwoProductPre2Split_Random()
        {
            // Just checks that the result from 2-pre-split is same as 2xSplit + TwoProduct
            var rnd = new RandomDouble(2); // Use a specific seed to ensure repeatability
            int testCount = 100000;

            for (int i = 0; i < testCount; i++)
            {
                double a = rnd.NextDoubleValidRange();
                double b = rnd.NextDoubleValidRange();

                double x; double y;
                TwoProduct_Checked(a, b, out x, out y);

                double ahi, alo;
                EA.Split(a, out ahi, out alo);
                double bhi, blo;
                EA.Split(b, out bhi, out blo);

                double xps, yps;
                EA.TwoProduct2Presplit(a, ahi, alo, b, bhi, blo, out xps, out yps);

                NUnit.Framework.Assert.AreEqual(x, xps);
                NUnit.Framework.Assert.AreEqual(y, yps);
            }

            Debug.Print("TwoProduct_Random Tested {0} tries", testCount);
        }

        [Test]
        public void ScaleExpansion_Simple()
        {
            double[] e = new[] {3e-80, -2, 1e100};
            double   b = 2.0;
            double[] h = new double[6];
            double[] hexp = new[] { 6e-80, -4, 2e100 };

            int hlen = ScaleExpansion_Checked(e.Length, e, b, h);
            double[] hfiltered = h.Where(d => d != 0.0).ToArray();

            NUnit.Framework.Assert.IsTrue(hexp.SequenceEqual(hfiltered));
        }

        // Checks the conditions of Theorem 19 (and corollary 22)
        int ScaleExpansion_Checked(int elen, double[] e, double b, double[] h)
        {
            NUnit.Framework.Assert.IsTrue(e.Take(elen).IsNonOverlapping());
            NUnit.Framework.Assert.IsTrue(e.Take(elen).IsSorted());

            int hlen = EA.ScaleExpansion(elen, e, b, h);

            NUnit.Framework.Assert.IsTrue(2 * elen == hlen);
            NUnit.Framework.Assert.IsTrue(h.IsNonOverlapping());
            NUnit.Framework.Assert.IsTrue(h.IsSorted());
            if (e.IsNonAdjacent()) NUnit.Framework.Assert.IsTrue(h.IsNonAdjacent());
            // Corollary 22
            if (e.IsStronglyNonOverlapping()) NUnit.Framework.Assert.IsTrue(h.IsStronglyNonOverlapping());

            return hlen;
        }

        [Test]
        public void ScaleExpansionZeroElim_Simple()
        {
            double[] e = new[] { 1.2e-80, -2.3, 3.4e100 };
            double b = 2.0;
            double[] h = new double[6];
            double[] hexp = new[] { 2.4e-80, -4.6, 6.8e100 };

            int hlen = ScaleExpansionZeroElim_Checked(e.Length, e, b, h);
            NUnit.Framework.Assert.IsTrue(hexp.SequenceEqual(h.Take(hlen)));
        }

        // Checks the conditions of Theorem 19 (and corollary 22), and the zero-elim condition
        public static int ScaleExpansionZeroElim_Checked(int elen, double[] e, double b, double[] h)
        {
            NUnit.Framework.Assert.IsTrue(e.Take(elen).IsNonOverlapping());
            NUnit.Framework.Assert.IsTrue(e.Take(elen).IsSorted());

            int hlen = EA.ScaleExpansionZeroElim(elen, e, b, h);

            NUnit.Framework.Assert.IsTrue(2 * elen >= hlen);
            NUnit.Framework.Assert.IsTrue(h.IsNonOverlapping());
            NUnit.Framework.Assert.IsTrue(h.IsSorted());
            if (e.IsNonAdjacent()) NUnit.Framework.Assert.IsTrue(h.IsNonAdjacent());
            // Corollary 22
            if (e.IsStronglyNonOverlapping()) NUnit.Framework.Assert.IsTrue(h.IsStronglyNonOverlapping());

            // Zero elimintion
            NUnit.Framework.Assert.IsTrue(h.Take(hlen).IsZeroElim());

            return hlen;
        }

        [Test]
        public void Square_Random()
        {
            var rnd = new RandomDouble(3); // Use a specific seed to ensure repeatability
            int testCount = 100000;

            for (int i = 0; i < testCount; i++)
            {
                double a = rnd.NextDoubleValidRange();

                double xp, yp;
                EA.TwoProduct(a, a, out xp, out yp);

                double xs, ys;
                EA.Square(a, out xs, out ys);

                NUnit.Framework.Assert.AreEqual(xp, xs);
                NUnit.Framework.Assert.AreEqual(yp, ys);
            }
        }
    }
}
