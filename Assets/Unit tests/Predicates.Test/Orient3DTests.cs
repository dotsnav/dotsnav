using System.Diagnostics;
using System.Linq;
using System.Numerics;
using DotsNav.Predicates;
using NUnit.Framework;
using RobustArithmetic.Test.FpuControl;
using RobustArithmetic.Test.Util;

namespace DotsNav.Test.Predicates.Test
{
    using EA = ExactArithmeticManaged;
    using GP = GeometricPredicatesManaged;

    [TestFixture]
    public class Orient3DTests
    {
        [Test]
        public void Orient3D_Simple()
        {
            double[] p1 = new[] { 0.0, 0.0, 0.0 };
            double[] p2 = new[] { 0.0, 1.2, 0.0 };
            double[] p3 = new[] { 1.3, 1.4, 0.0 };

            double[] p4 = new[] { 1.0, 1.0e30, 1e-50 };

            double res = Orient3D_Checked(p1, p2, p3, p4);


        }

        double Orient3D_Checked(double[] pa, double[] pb, double[] pc, double[] pd)
        {
            // Call Orient3D in all the possible ways, and check that the results agree

            var P1 = GP.Orient3D(pa, pb, pc, pd);
            var P2 = GP.Orient3D(pb, pc, pa, pd);
            var P3 = GP.Orient3D(pc, pa, pb, pd);
            var p1 = GP.Orient3D(pa, pc, pb, pd);
            var p2 = GP.Orient3D(pc, pb, pa, pd);
            var p3 = GP.Orient3D(pb, pa, pc, pd);

            var p1X = GP.Orient3DExact(pa, pb, pc, pd);
            var p2X = GP.Orient3DExact(pb, pc, pa, pd);
            var p3X = GP.Orient3DExact(pc, pa, pb, pd);
            var p1R = GP.Orient3DExact(pa, pc, pb, pd);
            var p2R = GP.Orient3DExact(pc, pb, pa, pd);
            var p3R = GP.Orient3DExact(pb, pa, pc, pd);

            var p1s = GP.Orient3DSlow(pa, pb, pc, pd);
            var p2s = GP.Orient3DSlow(pb, pc, pa, pd);
            var p3s = GP.Orient3DSlow(pc, pa, pb, pd);
            var p1r = GP.Orient3DSlow(pa, pc, pb, pd);
            var p2r = GP.Orient3DSlow(pc, pb, pa, pd);
            var p3r = GP.Orient3DSlow(pb, pa, pc, pd);

            AssertEqual(p1X, p2X, p3X, -p1R, -p2R, -p3R, p1s, p2s, p3s, -p1r, -p2r, -p3r);
 //           AssertClose(p1X, P2, P3, -p1, -p2, -p3);
            AssertSameSign(p1X, P1, P2, P3, -p1, -p2, -p3);

            // TODO: Add a check that shows adaptive is 'close' to exact.

            return P1;
        }

        void AssertEqual(params double[] doubles)
        {
            for (int i = 1; i < doubles.Length; i++)
            {
                NUnit.Framework.Assert.AreEqual(doubles[0], doubles[i]);
            }
        }

        void AssertSameSign(params double[] doubles)
        {
            for (int i = 1; i < doubles.Length; i++)
            {
                NUnit.Framework.Assert.AreEqual(System.Math.Sign(doubles[0]), System.Math.Sign(doubles[i]));
            }
        }

        [Test]
        public void InPlane_ProblemCase()
        {
            /*
	            CAUTION: (From a comment to http://digestingduck.blogspot.com/2010/12/computational-geometry-sucks.html)

	                "Shewchuk predicates are not completely stable, at least I wasn't able to make it stable on new HW:

	                double pointA[3] = {0.12539999f, 0.0016452915f, -0.019413333f};
	                double pointB[3] = {0.12539999f, 0.0017933375f, -0.019214222f};
	                double pointC[3] = {0.12539999f, 0.0017933375f, -0.017919999f};
	                double pointD[3] = {0.11700000f, 0.0017933375f, -0.018710587f};
	                double pointE[3] = {0.12539999f, 0.0017933375f, -0.019413333f};

	                std::cout << "On plane: A, C, B, E: " << onPlane(pointA, pointC, pointB, pointE) << std::endl;
	                std::cout << "On plane: B, C, D, E: " << onPlane(pointB, pointC, pointD, pointE) << std::endl;
	                std::cout << "On plane: B, C, D, A: " << onPlane(pointB, pointC, pointD, pointA) << std::endl;

	                This three tests ultimately fail telling you that first 2 tests are true and the last one is false,
                    which is logically impossible and therefore predicates are not working correctly on modern HW at least."


                HOWEVER: This is not really a problem of instability with the Shewchuk predicates. The points A, B, C and E
                above are collinear. This makes the first onPlane test meaningless.
                (It did keep me busy for a while, though.)

            */

            var oldState = new FpuControl.State(FpuControl.GetState());
            var oldPc = oldState.PrecisionControl;

            double[] pA = new double[] {0.12539999f, 0.0017933375f, -0.019214222f};
	        double[] pB = new double[] {0.12539999f, 0.0017933375f, -0.017919999f};
            double[] pC = new double[] {0.12539999f, 0.0016452915f, -0.019413333f};
	        double[] pD = new double[] {0.11700000f, 0.0017933375f, -0.018710587f};
            double[] pE = new double[] {0.12539999f, 0.0017933375f, -0.019413333f};

            double d1 = Orient3DExact_ScaledChecked(pA, pB, pC, pD, 1.0);
            double d2 = Orient3DExact_ScaledChecked(pA, pB, pC, pE, 1.0);
            double d3 = Orient3DExact_ScaledChecked(pB, pC, pE, pD, 1.0);

            var l1 = GP.Orient2D(pA, pB, pC);
            var l2 = GP.Orient2D(pB, pC, pD);
            var l3 = GP.Orient2D(pB, pC, pE);
            var l4 = GP.Orient2D(pA, pC, pE);

            var le1 = GP.Orient2DExact(pA, pB, pC);
            var le2 = GP.Orient2DExact(pB, pC, pD);
            var le3 = GP.Orient2DExact(pB, pC, pE);
            var le4 = GP.Orient2DExact(pA, pC, pE);

            double scale = System.Math.Pow(2.0, 32);
            double[] psA = pA.Select(d => d * scale).ToArray();
            double[] psB = pB.Select(d => d * scale).ToArray();
            double[] psC = pC.Select(d => d * scale).ToArray();
            double[] psD = pD.Select(d => d * scale).ToArray();
            double[] psE = pE.Select(d => d * scale).ToArray();

            double s1 = Orient3DExact_ScaledChecked(psA, psB, psC, psD, scale);
            double s2 = Orient3DExact_ScaledChecked(psA, psB, psC, psE, scale);
            double s3 = Orient3DExact_ScaledChecked(psB, psC, psE, psD, scale);

            BigInteger[] plA = psA.Select(d => (BigInteger)d).ToArray();
            BigInteger[] plB = psB.Select(d => (BigInteger)d).ToArray();
            BigInteger[] plC = psC.Select(d => (BigInteger)d).ToArray();
            BigInteger[] plD = psD.Select(d => (BigInteger)d).ToArray();
            BigInteger[] plE = psE.Select(d => (BigInteger)d).ToArray();

            var bl1 = Orient2DBigInteger(plA, plB, plC);
            var bl2 = Orient2DBigInteger(plB, plC, plE);

            BigInteger b1 = Orient3DInt64(plA, plB, plC, plD);
            BigInteger b2 = Orient3DInt64(plA, plB, plC, plE);
            BigInteger b3 = Orient3DInt64(plB, plC, plE, plD);

            NUnit.Framework.Assert.AreEqual(s1, (double)b1);

            //double d1m = Orient3DExact_Checked2(pA, pC, pB, pD);

            //Assert.AreEqual(d1, -d1m);
            //double[] pE = new double[]{0.12539999f, 0.0017933375f, -0.019413333f};

            //// Not zero !? - Already res4 and res5 differ, and res4 and resF4 differ.
            //double res4 = Orient3DExact_Checked(pA, pB, pC, pD, true);


            //double res5b = Orient3DExact_Checked2(pB, pC, pD, pA);

            //double res5c = Orient3DExact_Checked2(pA, pC, pB, pD);


            //double resF4 = GP.Orient3DFast(pA, pB, pC, pD);

            //double res6 = Orient3DExact_Checked2(pA, pC, pD, pB);
            //double res7 = Orient3DExact_Checked2(pA, pB, pD, pC);

            //double res1 = Orient3DExact_Checked(pA, pC, pB, pE);
            //double res12 = Orient3DExact_Checked(pC, pB, pE, pA);
            //double res13 = Orient3DExact_Checked(pA, pB, pC, pE);
            //double res14 = Orient3DExact_Checked(pA, pE, pC, pB);
            //double res2 = Orient3DExact_Checked(pB, pC, pD, pE);
            //double res3 = Orient3DExact_Checked2(pB, pC, pD, pA);
            //double resF1 = GP.Orient3DFast(pA, pC, pB, pE);
            //double resF2 = GP.Orient3DFast(pB, pC, pD, pE);
            //double resF3 = GP.Orient3DFast(pB, pC, pD, pA);


            //double scale = 2.0;
            //double[] psA = pA.Select(d => d * scale).ToArray();
            //double[] psB = pA.Select(d => d * scale).ToArray();
            //double[] psC = pA.Select(d => d * scale).ToArray();
            //double[] psD = pA.Select(d => d * scale).ToArray();
            //double[] psE = pA.Select(d => d * scale).ToArray();



            //double rss4 = Orient3DExact_Checked2(psA, psB, psC, psD);
            //double rss5 = Orient3DExact_Checked2(psB, psC, psA, psD);
            //double rssF4 = GP.Orient3DFast(psA, psB, psC, psD);

            //double rss6 = Orient3DExact_Checked2(psA, psC, psD, psB);
            //double rss7 = Orient3DExact_Checked2(psA, psB, psD, psC);

            //double rss1 = Orient3DExact_Checked(psA, psC, psB, psE);
            //double rss12 = Orient3DExact_Checked(psC, psB, psE, psA);
            //double rss13 = Orient3DExact_Checked(psA, psB, psC, psE);
            //double rss14 = Orient3DExact_Checked(psA, psE, psC, psB);
            //double rss2 = Orient3DExact_Checked(psB, psC, psD, psE);
            //double rss3 = Orient3DExact_Checked2(psB, psC, psD, psA);
            //double rssF1 = GP.Orient3DFast(psA, psC, psB, psE);
            //double rssF2 = GP.Orient3DFast(psB, psC, psD, psE);
            //double rssF3 = GP.Orient3DFast(psB, psC, psD, psA);


            //Assert.AreEqual(res1, res2);
            //Assert.AreEqual(res1, res4);
            //Assert.AreEqual(res1, res5);
            //Assert.AreEqual(res1, res6);
            //Assert.AreEqual(res1, res7);
            //Assert.AreEqual(res1, res3);
            //Assert.AreEqual(res1, res12);
            //Assert.AreEqual(res1, res13);

        }

        double Orient3DExact_Checked(double[] pa, double[] pb, double[] pc, double[] pd, bool test = false)
        {
            double axby1, bxcy1, cxdy1, dxay1, axcy1, bxdy1;
            double bxay1, cxby1, dxcy1, axdy1, cxay1, dxby1;
            double axby0, bxcy0, cxdy0, dxay0, axcy0, bxdy0;
            double bxay0, cxby0, dxcy0, axdy0, cxay0, dxby0;
            double[] ab = new double[4];
            double[] bc = new double[4];
            double[] cd = new double[4];
            double[] da = new double[4];
            double[] ac = new double[4];
            double[] bd = new double[4];
            double[] temp8 = new double[8];
            int templen;
            double[] abc = new double[12];
            double[] bcd = new double[12];
            double[] cda = new double[12];
            double[] dab = new double[12];

            int abclen, bcdlen, cdalen, dablen;

            double[] adet = new double[24];
            double[] bdet = new double[24];
            double[] cdet = new double[24];
            double[] ddet = new double[24];

            int alen, blen, clen, dlen;

            double[] abdet = new double[48];
            double[] cddet = new double[48];
            int ablen, cdlen;

            double[] deter = new double[96];
            int deterlen;

            int i;

            ProductTests.TwoProduct_Checked(pa[0], pb[1], out axby1, out axby0);
            ProductTests.TwoProduct_Checked(pb[0], pa[1], out bxay1, out bxay0);
            SumTests.TwoTwoDiff_Checked(axby1, axby0, bxay1, bxay0, out ab[3], out ab[2], out ab[1], out ab[0]); // ab

            ProductTests.TwoProduct_Checked(pb[0], pc[1], out bxcy1, out bxcy0);
            ProductTests.TwoProduct_Checked(pc[0], pb[1], out cxby1, out cxby0);
            SumTests.TwoTwoDiff_Checked(bxcy1, bxcy0, cxby1, cxby0, out bc[3], out bc[2], out bc[1], out bc[0]); // bc

            ProductTests.TwoProduct_Checked(pc[0], pd[1], out cxdy1, out cxdy0);
            ProductTests.TwoProduct_Checked(pd[0], pc[1], out dxcy1, out dxcy0);
            SumTests.TwoTwoDiff_Checked(cxdy1, cxdy0, dxcy1, dxcy0, out cd[3], out cd[2], out cd[1], out cd[0]); // cd

            ProductTests.TwoProduct_Checked(pd[0], pa[1], out dxay1, out dxay0);
            ProductTests.TwoProduct_Checked(pa[0], pd[1], out axdy1, out axdy0);
            SumTests.TwoTwoDiff_Checked(dxay1, dxay0, axdy1, axdy0, out da[3], out da[2], out da[1], out da[0]); // da

            ProductTests.TwoProduct_Checked(pa[0], pc[1], out axcy1, out axcy0);
            ProductTests.TwoProduct_Checked(pc[0], pa[1], out cxay1, out cxay0);
            SumTests.TwoTwoDiff_Checked(axcy1, axcy0, cxay1, cxay0, out ac[3], out ac[2], out ac[1], out ac[0]); // ac

            ProductTests.TwoProduct_Checked(pb[0], pd[1], out bxdy1, out bxdy0);
            ProductTests.TwoProduct_Checked(pd[0], pb[1], out dxby1, out dxby0);
            SumTests.TwoTwoDiff_Checked(bxdy1, bxdy0, dxby1, dxby0, out bd[3], out bd[2], out bd[1], out bd[0]); // bd


            templen = ExpansionTests.FastExpansionSumZeroElim_Checked(4, cd, 4, da, temp8);
            cdalen = ExpansionTests.FastExpansionSumZeroElim_Checked(templen, temp8, 4, ac, cda);           // cda
            templen = ExpansionTests.FastExpansionSumZeroElim_Checked(4, da, 4, ab, temp8);
            dablen = ExpansionTests.FastExpansionSumZeroElim_Checked(templen, temp8, 4, bd, dab);           // dab
            for (i = 0; i < 4; i++)
            {
                bd[i] = -bd[i];
                ac[i] = -ac[i];
            }
            templen = ExpansionTests.FastExpansionSumZeroElim_Checked(4, ab, 4, bc, temp8);
            abclen = ExpansionTests.FastExpansionSumZeroElim_Checked(templen, temp8, 4, ac, abc);           // abc
            templen = ExpansionTests.FastExpansionSumZeroElim_Checked(4, bc, 4, cd, temp8);
            bcdlen = ExpansionTests.FastExpansionSumZeroElim_Checked(templen, temp8, 4, bd, bcd);           // bcd

            alen = ProductTests.ScaleExpansionZeroElim_Checked(bcdlen, bcd, pa[2], adet);
            blen = ProductTests.ScaleExpansionZeroElim_Checked(cdalen, cda, -pb[2], bdet);
            clen = ProductTests.ScaleExpansionZeroElim_Checked(dablen, dab, pc[2], cdet);
            dlen = ProductTests.ScaleExpansionZeroElim_Checked(abclen, abc, -pd[2], ddet);


            ablen = ExpansionTests.FastExpansionSumZeroElim_Checked(alen, adet, blen, bdet, abdet);
            cdlen = ExpansionTests.FastExpansionSumZeroElim_Checked(clen, cdet, dlen, ddet, cddet);
            deterlen = ExpansionTests.FastExpansionSumZeroElim_Checked(ablen, abdet, cdlen, cddet, deter);

            return deter[deterlen - 1];
        }



        double Orient3DExact_Checked2(double[] pa, double[] pb, double[] pc, double[] pd, bool test = false)
        {
            double axby1, bxcy1, cxdy1, dxay1, axcy1, bxdy1;
            double bxay1, cxby1, dxcy1, axdy1, cxay1, dxby1;
            double axby0, bxcy0, cxdy0, dxay0, axcy0, bxdy0;
            double bxay0, cxby0, dxcy0, axdy0, cxay0, dxby0;
            double[] ab = new double[4];
            double[] bc = new double[4];
            double[] cb = new double[4]; // for test
            double[] cd = new double[4];
            double[] da = new double[4];
            double[] ad = new double[4]; // for test
            double[] ac = new double[4];
            double[] bd = new double[4];
            double[] temp8 = new double[8];
            int templen;
            double[] abc = new double[12];
            double[] bcd = new double[12];
            double[] cda = new double[12];
            double[] dab = new double[12];
            double[] abd = new double[12];
            double[] acd = new double[12];
            double[] acb = new double[12];
            double[] cbd = new double[12];
            int abclen, bcdlen, cdalen, dablen;
            int acblen, cbdlen, abdlen, acdlen;
            double[] adet = new double[24];
            double[] bdet = new double[24];
            double[] cdet = new double[24];
            double[] ddet = new double[24];
            double[] a2det = new double[24];
            double[] b2det = new double[24];
            double[] c2det = new double[24];
            double[] d2det = new double[24];
            int alen, blen, clen, dlen;
            int a2len, b2len, c2len, d2len;
            double[] abdet = new double[48];
            double[] cddet = new double[48];
            int ablen, cdlen;
            double[] ab2det = new double[48];
            double[] cd2det = new double[48];
            int ab2len, cd2len;
            double[] ac2det = new double[48];
            double[] bd2det = new double[48];
            int ac2len, bd2len;
            double[] deter = new double[96];
            int deterlen;
            double[] deter2 = new double[96];
            int deter2len;
            double[] deter3 = new double[96];
            int deter3len;
            int i;

            ProductTests.TwoProduct_Checked(pa[0], pb[1], out axby1, out axby0);
            ProductTests.TwoProduct_Checked(pb[0], pa[1], out bxay1, out bxay0);
            SumTests.TwoTwoDiff_Checked(axby1, axby0, bxay1, bxay0, out ab[3], out ab[2], out ab[1], out ab[0]); // ab

            ProductTests.TwoProduct_Checked(pb[0], pc[1], out bxcy1, out bxcy0);
            ProductTests.TwoProduct_Checked(pc[0], pb[1], out cxby1, out cxby0);
            SumTests.TwoTwoDiff_Checked(bxcy1, bxcy0, cxby1, cxby0, out bc[3], out bc[2], out bc[1], out bc[0]); // bc

            ProductTests.TwoProduct_Checked(pc[0], pb[1], out cxby1, out cxby0);
            ProductTests.TwoProduct_Checked(pb[0], pc[1], out bxcy1, out bxcy0);
            SumTests.TwoTwoDiff_Checked(cxby1, cxby0, bxcy1, bxcy0, out cb[3], out cb[2], out cb[1], out cb[0]); // cb

            ProductTests.TwoProduct_Checked(pc[0], pd[1], out cxdy1, out cxdy0);
            ProductTests.TwoProduct_Checked(pd[0], pc[1], out dxcy1, out dxcy0);
            SumTests.TwoTwoDiff_Checked(cxdy1, cxdy0, dxcy1, dxcy0, out cd[3], out cd[2], out cd[1], out cd[0]); // cd

            ProductTests.TwoProduct_Checked(pd[0], pa[1], out dxay1, out dxay0);
            ProductTests.TwoProduct_Checked(pa[0], pd[1], out axdy1, out axdy0);
            SumTests.TwoTwoDiff_Checked(dxay1, dxay0, axdy1, axdy0, out da[3], out da[2], out da[1], out da[0]); // da

            ProductTests.TwoProduct_Checked(pa[0], pd[1], out axdy1, out axdy0);
            ProductTests.TwoProduct_Checked(pd[0], pa[1], out dxay1, out dxay0);
            SumTests.TwoTwoDiff_Checked(axdy1, axdy0, dxay1, dxay0, out ad[3], out ad[2], out ad[1], out ad[0]); // ad

            ProductTests.TwoProduct_Checked(pa[0], pc[1], out axcy1, out axcy0);
            ProductTests.TwoProduct_Checked(pc[0], pa[1], out cxay1, out cxay0);
            SumTests.TwoTwoDiff_Checked(axcy1, axcy0, cxay1, cxay0, out ac[3], out ac[2], out ac[1], out ac[0]); // ac

            ProductTests.TwoProduct_Checked(pb[0], pd[1], out bxdy1, out bxdy0); // bx.dy
            ProductTests.TwoProduct_Checked(pd[0], pb[1], out dxby1, out dxby0); // dx.by
            SumTests.TwoTwoDiff_Checked(bxdy1, bxdy0, dxby1, dxby0, out bd[3], out bd[2], out bd[1], out bd[0]); // bd = bx.dy - dx.by


            NUnit.Framework.Assert.AreEqual(bc[3], -cb[3]);
            NUnit.Framework.Assert.AreEqual(bc[2], -cb[2]);
            NUnit.Framework.Assert.AreEqual(da[2], -ad[2]);
            NUnit.Framework.Assert.AreEqual(da[2], -ad[2]);

            templen = ExpansionTests.FastExpansionSumZeroElim_Checked(4, cd, 4, da, temp8);
            cdalen = ExpansionTests.FastExpansionSumZeroElim_Checked(templen, temp8, 4, ac, cda);           // cda
            templen = ExpansionTests.FastExpansionSumZeroElim_Checked(4, da, 4, ab, temp8);
            dablen = ExpansionTests.FastExpansionSumZeroElim_Checked(templen, temp8, 4, bd, dab);           // dab

            // Set bd = -bd
            //     ac = -ac
            for (i = 0; i < 4; i++)
            {
                bd[i] = -bd[i];
                ac[i] = -ac[i];
            }
            templen = ExpansionTests.FastExpansionSumZeroElim_Checked(4, ab, 4, bc, temp8);
            abclen = ExpansionTests.FastExpansionSumZeroElim_Checked(templen, temp8, 4, ac, abc);           // abc
            templen = ExpansionTests.FastExpansionSumZeroElim_Checked(4, bc, 4, cd, temp8);
            bcdlen = ExpansionTests.FastExpansionSumZeroElim_Checked(templen, temp8, 4, bd, bcd);           // bcd

            // (Re)set bd = -bd
            //         ac = -ac
            for (i = 0; i < 4; i++)
            {
                bd[i] = -bd[i];
                ac[i] = -ac[i];
            }

            //if (test)
            //{
            //    Assert.AreEqual(0.0, abc[abclen - 1]);
            //    Assert.AreEqual(0.0, bcd[bcdlen - 1]);
            //    Assert.AreEqual(dab[0], cda[0]);
            //    Assert.AreEqual(dab[1], cda[1]);
            //    Assert.AreNotEqual(pb[2], pc[2]);
            //}

            alen = ProductTests.ScaleExpansionZeroElim_Checked(bcdlen, bcd, pa[2], adet);
            blen = ProductTests.ScaleExpansionZeroElim_Checked(cdalen, cda, -pb[2], bdet);
            clen = ProductTests.ScaleExpansionZeroElim_Checked(dablen, dab, pc[2], cdet);
            dlen = ProductTests.ScaleExpansionZeroElim_Checked(abclen, abc, -pd[2], ddet);


            ablen = ExpansionTests.FastExpansionSumZeroElim_Checked(alen, adet, blen, bdet, abdet);
            cdlen = ExpansionTests.FastExpansionSumZeroElim_Checked(clen, cdet, dlen, ddet, cddet);
            deterlen = ExpansionTests.FastExpansionSumZeroElim_Checked(ablen, abdet, cdlen, cddet, deter);

            //////////////////
            ///////////////////////////////////

            templen = ExpansionTests.FastExpansionSumZeroElim_Checked(4, bd, 4, da, temp8);
            abdlen = ExpansionTests.FastExpansionSumZeroElim_Checked(templen, temp8, 4, ab, abd);           // abd
            templen = ExpansionTests.FastExpansionSumZeroElim_Checked(4, da, 4, ac, temp8);
            acdlen = ExpansionTests.FastExpansionSumZeroElim_Checked(templen, temp8, 4, cd, acd);           // acd
            for (i = 0; i < 4; i++)
            {
                cd[i] = -cd[i];
                ab[i] = -ab[i];
            }
            templen = ExpansionTests.FastExpansionSumZeroElim_Checked(4, ac, 4, cb, temp8);
            acblen = ExpansionTests.FastExpansionSumZeroElim_Checked(templen, temp8, 4, ab, acb);           // acb
            templen = ExpansionTests.FastExpansionSumZeroElim_Checked(4, cb, 4, bd, temp8);
            cbdlen = ExpansionTests.FastExpansionSumZeroElim_Checked(templen, temp8, 4, cd, cbd);           // cbd

            NUnit.Framework.Assert.IsTrue(bcd.SequenceEqual(cbd.Select(d=>-d)));
            NUnit.Framework.Assert.IsTrue(cda.SequenceEqual(acd));
            NUnit.Framework.Assert.IsTrue(dab.SequenceEqual(abd));
            NUnit.Framework.Assert.IsTrue(abc.SequenceEqual(acb.Select(d => -d)));

            //Assert.AreEqual(dab[0], bda[0]);
            //Assert.AreEqual(bcd[0], -cbd[0]);

            a2len = ProductTests.ScaleExpansionZeroElim_Checked(cbdlen, cbd, pa[2], a2det);
            c2len = ProductTests.ScaleExpansionZeroElim_Checked(abdlen, abd, -pc[2], c2det);
            b2len = ProductTests.ScaleExpansionZeroElim_Checked(acdlen, acd, pb[2], b2det);
            d2len = ProductTests.ScaleExpansionZeroElim_Checked(acblen, acb, -pd[2], d2det);

            NUnit.Framework.Assert.IsTrue(adet.SequenceEqual(a2det.Select(d => -d)));
            NUnit.Framework.Assert.IsTrue(bdet.SequenceEqual(b2det.Select(d => -d)));
            NUnit.Framework.Assert.IsTrue(cdet.SequenceEqual(c2det.Select(d => -d)));
            NUnit.Framework.Assert.IsTrue(ddet.SequenceEqual(d2det.Select(d => -d)));

            /// ///////////////

            ac2len = ExpansionTests.FastExpansionSumZeroElim_Checked(a2len, a2det, c2len, c2det, ac2det);
            bd2len = ExpansionTests.FastExpansionSumZeroElim_Checked(b2len, b2det, d2len, d2det, bd2det);

            deter2len = ExpansionTests.FastExpansionSumZeroElim_Checked(ac2len, ac2det, bd2len, bd2det, deter2);

            ab2len = ExpansionTests.FastExpansionSumZeroElim_Checked(a2len, a2det, b2len, b2det, ab2det);
            cd2len = ExpansionTests.FastExpansionSumZeroElim_Checked(c2len, c2det, d2len, d2det, cd2det);

            deter3len = ExpansionTests.FastExpansionSumZeroElim_Checked(ab2len, ab2det, cd2len, cd2det, deter3);

            Debug.Print("Before: {0:R}, {1:R}", ab2det[0], ab2det[1]);
            Debug.Print("After: {0:R}, {1:R}", deter3[0], deter3[1]);

            //Assert.AreEqual(deter2[deter2len - 1], deter3[deter3len - 1]);
            //Assert.AreEqual(deter[deterlen - 1], -deter2[deter2len - 1]);
            NUnit.Framework.Assert.IsTrue((deter[deterlen - 1] == 0.0) == (deter2[deter2len - 1] == 0.0));
            NUnit.Framework.Assert.IsTrue((deter2[deter2len - 1] == 0.0) == (deter3[deter3len - 1] == 0.0));
            return deter[deterlen - 1];
        }


        double Orient3DExact_ScaledChecked(double[] pa, double[] pb, double[] pc, double[] pd, double scale = 1.0)
        {
            double axby1, bxcy1, cxdy1, dxay1, axcy1, bxdy1;
            double bxay1, cxby1, dxcy1, axdy1, cxay1, dxby1;
            double axby0, bxcy0, cxdy0, dxay0, axcy0, bxdy0;
            double bxay0, cxby0, dxcy0, axdy0, cxay0, dxby0;
            double[] ab = new double[4];
            double[] bc = new double[4];
            double[] cd = new double[4];
            double[] da = new double[4];
            double[] ac = new double[4];
            double[] bd = new double[4];
            double[] temp8 = new double[8];
            int templen;
            double[] abc = new double[12];
            double[] bcd = new double[12];
            double[] cda = new double[12];
            double[] dab = new double[12];

            int abclen, bcdlen, cdalen, dablen;

            double[] adet = new double[24];
            double[] bdet = new double[24];
            double[] cdet = new double[24];
            double[] ddet = new double[24];

            int alen, blen, clen, dlen;

            double[] abdet = new double[48];
            double[] cddet = new double[48];
            int ablen, cdlen;

            double[] deter = new double[96];
            int deterlen;

            int i;

            ProductTests.TwoProduct_Checked(pa[0], pb[1], out axby1, out axby0);
            ProductTests.TwoProduct_Checked(pb[0], pa[1], out bxay1, out bxay0);
            SumTests.TwoTwoDiff_Checked(axby1, axby0, bxay1, bxay0, out ab[3], out ab[2], out ab[1], out ab[0]); // ab

            ProductTests.TwoProduct_Checked(pb[0], pc[1], out bxcy1, out bxcy0);
            ProductTests.TwoProduct_Checked(pc[0], pb[1], out cxby1, out cxby0);
            SumTests.TwoTwoDiff_Checked(bxcy1, bxcy0, cxby1, cxby0, out bc[3], out bc[2], out bc[1], out bc[0]); // bc

            ProductTests.TwoProduct_Checked(pc[0], pd[1], out cxdy1, out cxdy0);
            ProductTests.TwoProduct_Checked(pd[0], pc[1], out dxcy1, out dxcy0);
            SumTests.TwoTwoDiff_Checked(cxdy1, cxdy0, dxcy1, dxcy0, out cd[3], out cd[2], out cd[1], out cd[0]); // cd

            ProductTests.TwoProduct_Checked(pd[0], pa[1], out dxay1, out dxay0);
            ProductTests.TwoProduct_Checked(pa[0], pd[1], out axdy1, out axdy0);
            SumTests.TwoTwoDiff_Checked(dxay1, dxay0, axdy1, axdy0, out da[3], out da[2], out da[1], out da[0]); // da

            ProductTests.TwoProduct_Checked(pa[0], pc[1], out axcy1, out axcy0);
            ProductTests.TwoProduct_Checked(pc[0], pa[1], out cxay1, out cxay0);
            SumTests.TwoTwoDiff_Checked(axcy1, axcy0, cxay1, cxay0, out ac[3], out ac[2], out ac[1], out ac[0]); // ac

            ProductTests.TwoProduct_Checked(pb[0], pd[1], out bxdy1, out bxdy0);
            ProductTests.TwoProduct_Checked(pd[0], pb[1], out dxby1, out dxby0);
            SumTests.TwoTwoDiff_Checked(bxdy1, bxdy0, dxby1, dxby0, out bd[3], out bd[2], out bd[1], out bd[0]); // bd

            Debug.Print("Start CD");
            Debug.Print((pc[0] / scale).ToString());
            Debug.Print((pc[1] / scale).ToString());
            Debug.Print((pd[0] / scale).ToString());
            Debug.Print((pd[1] / scale).ToString());
            Debug.Print((cxdy0 / scale).ToString());
            Debug.Print((cxdy1 / scale).ToString());
            Debug.Print((dxcy0 / scale).ToString());
            Debug.Print((dxcy1 / scale).ToString());
            cd.Print(4, scale);
            Debug.Print("End CD");

            da.Print(4, scale);
            ac.Print(4, scale);

            templen = ExpansionTests.FastExpansionSumZeroElim_Checked(4, cd, 4, da, temp8);
            cdalen = ExpansionTests.FastExpansionSumZeroElim_Checked(templen, temp8, 4, ac, cda);           // cda
            templen = ExpansionTests.FastExpansionSumZeroElim_Checked(4, da, 4, ab, temp8);
            dablen = ExpansionTests.FastExpansionSumZeroElim_Checked(templen, temp8, 4, bd, dab);           // dab
            for (i = 0; i < 4; i++)
            {
                bd[i] = -bd[i];
                ac[i] = -ac[i];
            }
            templen = ExpansionTests.FastExpansionSumZeroElim_Checked(4, ab, 4, bc, temp8);
            abclen = ExpansionTests.FastExpansionSumZeroElim_Checked(templen, temp8, 4, ac, abc);           // abc
            templen = ExpansionTests.FastExpansionSumZeroElim_Checked(4, bc, 4, cd, temp8);
            bcdlen = ExpansionTests.FastExpansionSumZeroElim_Checked(templen, temp8, 4, bd, bcd);           // bcd

            cda.Print(cdalen, scale);
            dab.Print(dablen, scale);
            abc.Print(abclen, scale);
            bcd.Print(bcdlen, scale);

            alen = ProductTests.ScaleExpansionZeroElim_Checked(bcdlen, bcd, pa[2], adet);
            blen = ProductTests.ScaleExpansionZeroElim_Checked(cdalen, cda, -pb[2], bdet);
            clen = ProductTests.ScaleExpansionZeroElim_Checked(dablen, dab, pc[2], cdet);
            dlen = ProductTests.ScaleExpansionZeroElim_Checked(abclen, abc, -pd[2], ddet);

            adet.Print(alen, scale);
            bdet.Print(blen, scale);
            cdet.Print(clen, scale);
            ddet.Print(dlen, scale);

            ablen = ExpansionTests.FastExpansionSumZeroElim_Checked(alen, adet, blen, bdet, abdet);
            cdlen = ExpansionTests.FastExpansionSumZeroElim_Checked(clen, cdet, dlen, ddet, cddet);
            deterlen = ExpansionTests.FastExpansionSumZeroElim_Checked(ablen, abdet, cdlen, cddet, deter);

            deter.Print(deterlen, scale);
            return deter[deterlen - 1];
        }

        public static BigInteger Orient3DInt64(BigInteger[] pa, BigInteger[] pb, BigInteger[] pc, BigInteger[] pd)
        {
            checked
            {
                BigInteger adx, bdx, cdx;
                BigInteger ady, bdy, cdy;
                BigInteger adz, bdz, cdz;

                adx = pa[0] - pd[0];
                bdx = pb[0] - pd[0];
                cdx = pc[0] - pd[0];
                ady = pa[1] - pd[1];
                bdy = pb[1] - pd[1];
                cdy = pc[1] - pd[1];
                adz = pa[2] - pd[2];
                bdz = pb[2] - pd[2];
                cdz = pc[2] - pd[2];

                return adx * (bdy * cdz - bdz * cdy)
                    + bdx * (cdy * adz - cdz * ady)
                    + cdx * (ady * bdz - adz * bdy);
            }
        }

        public static BigInteger Orient3DBigInteger(BigInteger[] pa, BigInteger[] pb, BigInteger[] pc, BigInteger[] pd)
        {
            BigInteger adx, bdx, cdx;
            BigInteger ady, bdy, cdy;
            BigInteger adz, bdz, cdz;

            adx = pa[0] - pd[0];
            bdx = pb[0] - pd[0];
            cdx = pc[0] - pd[0];
            ady = pa[1] - pd[1];
            bdy = pb[1] - pd[1];
            cdy = pc[1] - pd[1];
            adz = pa[2] - pd[2];
            bdz = pb[2] - pd[2];
            cdz = pc[2] - pd[2];

            return adx * (bdy * cdz - bdz * cdy)
                + bdx * (cdy * adz - cdz * ady)
                + cdx * (ady * bdz - adz * bdy);
        }

        public static BigInteger Orient2DBigInteger(BigInteger[] pa, BigInteger[] pb, BigInteger[] pc)
        {
            BigInteger acx, bcx, acy, bcy;

            acx = pa[0] - pc[0];
            bcx = pb[0] - pc[0];
            acy = pa[1] - pc[1];
            bcy = pb[1] - pc[1];
            return acx * bcy - acy * bcx;
        }
    }
}

