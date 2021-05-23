using NUnit.Framework;
using RobustGeometricPredicates;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;

namespace DotsNav.Test.Predicates.Test
{
    [TestFixture]
    public class Orient2DBurstTests
    {
        [Test]
        public void Run()
        {
            new Orient2DTestJob().Run();
        }

        [BurstCompile]
        struct Orient2DTestJob : IJob
        {
            public void Execute()
            {
                var p1 = new double2(0.10000000000000001, 0.10000000000000001);
                var p2 = new double2(0.20000000000000001, 0.20000000000000004);
                var p3 = new double2(0.79999999999999993, 0.80000000000000004);
                var p4 = new double2(1.267650600228229e30, 1.2676506002282291e30);

                // todo reinstate these when merging tests with unity package, if ever
                NUnit.Framework.Assert.IsTrue(GeometricPredicates.Orient2DFast(p1, p2, p3) > 0.0); // Wrong!
                // NUnit.Framework.Assert.IsTrue(GeometricPredicates.Orient2DExact(p1, p2, p3) < 0.0);
                // NUnit.Framework.Assert.IsTrue(GeometricPredicates.Orient2DSlow(p1, p2, p3) < 0.0);
                NUnit.Framework.Assert.IsTrue(GeometricPredicates.Orient2D(p1, p2, p3) < 0.0);

                NUnit.Framework.Assert.IsTrue(GeometricPredicates.Orient2DFast(p1, p2, p4) == 0.0); // Wrong!
                // NUnit.Framework.Assert.IsTrue(GeometricPredicates.Orient2DExact(p1, p2, p4) < 0.0);
                // NUnit.Framework.Assert.IsTrue(GeometricPredicates.Orient2DSlow(p1, p2, p4) < 0.0);
                NUnit.Framework.Assert.IsTrue(GeometricPredicates.Orient2D(p1, p2, p4) < 0.0);

                NUnit.Framework.Assert.IsTrue(GeometricPredicates.Orient2DFast(p2, p3, p4) == 0.0); // Wrong!
                // NUnit.Framework.Assert.IsTrue(GeometricPredicates.Orient2DExact(p2, p3, p4) < 0.0);
                // NUnit.Framework.Assert.IsTrue(GeometricPredicates.Orient2DSlow(p2, p3, p4) < 0.0);
                NUnit.Framework.Assert.IsTrue(GeometricPredicates.Orient2D(p2, p3, p4) < 0.0);

                NUnit.Framework.Assert.IsTrue(GeometricPredicates.Orient2DFast(p3, p1, p4) == 0.0); // Wrong!
                // NUnit.Framework.Assert.IsTrue(GeometricPredicates.Orient2DExact(p3, p1, p4) > 0.0);
                // NUnit.Framework.Assert.IsTrue(GeometricPredicates.Orient2DSlow(p3, p1, p4) > 0.0);
                NUnit.Framework.Assert.IsTrue(GeometricPredicates.Orient2D(p3, p1, p4) > 0.0);
            }
        }
    }
}
