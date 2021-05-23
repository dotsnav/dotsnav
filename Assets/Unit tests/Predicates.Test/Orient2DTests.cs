using DotsNav.Predicates;
using NUnit.Framework;

namespace DotsNav.Test.Predicates.Test
{
    [TestFixture]
    public class Orient2DTests
    {

        // This case is from p.13 of 'Robustness Problems'
        // (Classroom examples of robustness problems in geometric computations - Kettner et al)
        //
        [Test]
        public void Orient()
        {
            double[] p1 = new[] { 0.10000000000000001,  0.10000000000000001 };
            double[] p2 = new[] { 0.20000000000000001,  0.20000000000000004 };
            double[] p3 = new[] { 0.79999999999999993,  0.80000000000000004 };
            double[] p4 = new[] { 1.267650600228229e30, 1.2676506002282291e30 };

            NUnit.Framework.Assert.IsTrue(GeometricPredicatesManaged.Orient2DFast(p1, p2, p3) > 0.0);  // Wrong!
            NUnit.Framework.Assert.IsTrue(GeometricPredicatesManaged.Orient2DExact(p1, p2, p3) < 0.0);
            NUnit.Framework.Assert.IsTrue(GeometricPredicatesManaged.Orient2DSlow(p1, p2, p3) < 0.0);
            NUnit.Framework.Assert.IsTrue(GeometricPredicatesManaged.Orient2D(p1, p2, p3) < 0.0);

            NUnit.Framework.Assert.IsTrue(GeometricPredicatesManaged.Orient2DFast(p1, p2, p4) == 0.0); // Wrong!
            NUnit.Framework.Assert.IsTrue(GeometricPredicatesManaged.Orient2DExact(p1, p2, p4) < 0.0);
            NUnit.Framework.Assert.IsTrue(GeometricPredicatesManaged.Orient2DSlow(p1, p2, p4) < 0.0);
            NUnit.Framework.Assert.IsTrue(GeometricPredicatesManaged.Orient2D(p1, p2, p4) < 0.0);

            NUnit.Framework.Assert.IsTrue(GeometricPredicatesManaged.Orient2DFast(p2, p3, p4) == 0.0); // Wrong!
            NUnit.Framework.Assert.IsTrue(GeometricPredicatesManaged.Orient2DExact(p2, p3, p4) < 0.0);
            NUnit.Framework.Assert.IsTrue(GeometricPredicatesManaged.Orient2DSlow(p2, p3, p4) < 0.0);
            NUnit.Framework.Assert.IsTrue(GeometricPredicatesManaged.Orient2D(p2, p3, p4) < 0.0);

            NUnit.Framework.Assert.IsTrue(GeometricPredicatesManaged.Orient2DFast(p3, p1, p4) == 0.0); // Wrong!
            NUnit.Framework.Assert.IsTrue(GeometricPredicatesManaged.Orient2DExact(p3, p1, p4) > 0.0);
            NUnit.Framework.Assert.IsTrue(GeometricPredicatesManaged.Orient2DSlow(p3, p1, p4) > 0.0);
            NUnit.Framework.Assert.IsTrue(GeometricPredicatesManaged.Orient2D(p3, p1, p4) > 0.0);
        }

        // More examples from 'Robustness Problems' -
        // Failure A1

//        p1 = ( 7.3000000000000194, 7.3000000000000167 )
//p2 = (24.000000000000068, 24.000000000000071 )
//p3 = (24.00000000000005, 24.000000000000053 )
//p4 = ( 0.50000000000001621, 0.50000000000001243)
//p5 = ( 8, 4) p6 = ( 4, 9) p7 = (15,27)
//p8 = (26,25) p9 = (19,11)
//float orient(p1, p2, p3) > 0
//float orient(p1, p2, p4) > 0
//float orient(p2, p3, p4) > 0
//float orient(p3, p1, p4) > 0 (??)


        // Failure A2

//        p1 = (27.643564356435643, −21.881188118811881 )
//p2 = (83.366336633663366, 15.544554455445542 )
//p3 = ( 4.0, 4.0 )
//p4 = (73.415841584158414, 8.8613861386138595)
//float orient(p1, p2, p3) > 0
//float orient(p1, p2, p4) < 0 (??)
//float orient(p2, p3, p4) > 0
//float orient(p3, p1, p4) > 0

        // Failure B1

//        p1 = ( 200.0, 49.200000000000003)
//p2 = ( 100.0, 49.600000000000001)
//p3 = (−233.33333333333334, 50.93333333333333 )
//p4 = ( 166.66666666666669, 49.333333333333336)
//float orient(p1, p2, p3) > 0
//float orient(p1, p2, p4) < 0
//float orient(p2, p3, p4) < 0
//float orient(p3, p1, p4) < 0 (??)


        // Failure B2

//        p1 = ( 0.50000000000001243, 0.50000000000000189)
//p2 = ( 0.50000000000001243, 0.50000000000000333)
//p3 = (24.00000000000005, 24.000000000000053 )
//p4 = (24.000000000000068, 24.000000000000071 )
//p5 = (17.300000000000001, 17.300000000000001 )
//float orient(p1, p4, p5) < 0 (??)
//float orient(p4, p3, p5) > 0
//float orient(p3, p2, p5) < 0
//float orient(p2, p1, p5) > 0

    }
}
