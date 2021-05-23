using System.Diagnostics;
using DotsNav.Predicates;
using NUnit.Framework;

namespace DotsNav.Test.Predicates.Test
{
    // TODO: InCircle & InSphere tests with MC pi

    [TestFixture]
    public class GeometricPredicatesTests
    {
        [Test]
        public void Orient2D_Simple()
        {
            var pa = new double[] { 0.0, 0.0 };
            var pb = new double[] { 1.0, 0.0 };
            var pc = new double[] { 1.0, 1.0 };

            var res = Orient2D_Checked(pa, pb, pc);

            NUnit.Framework.Assert.AreEqual(1.0, res);

        }

        [Test]
        public void Orient2D_Negative()
        {
            var pa = new double[] { 0.0, 0.0 };
            var pb = new double[] { 1.0, 0.0 };
            var pc = new double[] { 1.0, -1.0 };

            var res = Orient2D_Checked(pa, pb, pc);

            NUnit.Framework.Assert.AreEqual(-1.0, res);
        }

        [Test]
        public void Orient2D_Marginal()
        {
            // Depending on the order of evaulation,
            // the fast routine sometimes finds these in line!
            var pa = new double[] { 0.0, 0.0 };
            var pb = new double[] { 1.0, 0.0 };
            var pc = new double[] { 1e50, 1e-80 };

            var res = Orient2D_Checked(pa, pb, pc);

            NUnit.Framework.Assert.IsTrue(res>0.0);
        }

        // Runs all the different versions, in different call orders
        static double Orient2D_Checked(double[] pa, double[] pb, double[] pc)
        {
            double res1 = Orient2D_Checked_InOrder(pa, pb, pc);
            double res2 = Orient2D_Checked_InOrder(pb, pc, pa);
            double res3 = Orient2D_Checked_InOrder(pc, pa, pb);

            NUnit.Framework.Assert.AreEqual(res1, res2);
            NUnit.Framework.Assert.AreEqual(res2, res3);

            return res1;
        }

        static double Orient2D_Checked_InOrder(double[] pa, double[] pb, double[] pc)
        {
            double fast  = GeometricPredicatesManaged.Orient2DFast(pa, pb, pc);
            double exact = GeometricPredicatesManaged.Orient2DExact(pa, pb, pc);
            double slow  = GeometricPredicatesManaged.Orient2DSlow(pa, pb, pc);
            double adapt = GeometricPredicatesManaged.Orient2D(pa, pb, pc);

            NUnit.Framework.Assert.AreEqual(exact, slow);
            NUnit.Framework.Assert.AreEqual(exact, adapt);

            // We don't expect these to be true, but it's nice to stop when they're not...
//            Assert.IsTrue(Math.Sign(fast) == Math.Sign(exact));
//            Assert.IsTrue(Math.Abs(fast - exact) < 1e-4);
            if (System.Math.Sign(fast) != System.Math.Sign(adapt))
            {
                Debug.Print("Fast: " + fast + " Adapt: " + adapt);
            }


            return adapt;
        }
    }
}
