#region License
// License for this implementation in C#:
// --------------------------------------
//
// Copyright (c) 2012 Govert van Drimmelen
//
// This software is provided 'as-is', without any express or implied
// warranty. In no event will the authors be held liable for any damages
// arising from the use of this software.
//
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it
// freely, subject to the following restrictions:
//
// 1. The origin of this software must not be misrepresented; you must not
//    claim that you wrote the original software. If you use this software
//    in a product, an acknowledgment in the product documentation would
//    be appreciated but is not required.
//
// 2. Altered source versions must be plainly marked as such, and must not
//    be misrepresented as being the original software.
//
// 3. This notice may not be removed or altered from any source distribution.
//
//
// License from original C source version:
// ---------------------------------------
//                                                                           
//  Routines for Arbitrary Precision Floating-point Arithmetic               
//  and Fast Robust Geometric Predicates                                     
//  (predicates.c)                                                           
//                                                                           
//  May 18, 1996                                                             
//                                                                           
//  Placed in the public domain by                                           
//  Jonathan Richard Shewchuk                                                
//  School of Computer Science                                               
//  Carnegie Mellon University                                               
//  5000 Forbes Avenue                                                       
//  Pittsburgh, Pennsylvania  15213-3891                                     
//  jrs@cs.cmu.edu                                                           
//                                                                           
//  This file contains C implementation of algorithms for exact addition     
//    and multiplication of floating-point numbers, and predicates for       
//    robustly performing the orientation and incircle tests used in         
//    computational geometry.  The algorithms and underlying theory are      
//    described in Jonathan Richard Shewchuk.  "Adaptive Precision Floating- 
//    Point Arithmetic and Fast Robust Geometric Predicates."  Technical     
//    Report CMU-CS-96-140, School of Computer Science, Carnegie Mellon      
//    University, Pittsburgh, Pennsylvania, May 1996.  (Submitted to         
//    Discrete & Computational Geometry.)                                    
//                                                                           
//  This file, the paper listed above, and other information are available   
//    from the Web page http://www.cs.cmu.edu/~quake/robust.html .           
//                                                                           
//-------------------------------------------------------------------------
#endregion

namespace DotsNav.Predicates
{
    using EA = ExactArithmeticManaged;

    /// <summary>
    /// Implements the four geometric predicates described by Shewchuck, and implemented in predicates.c.
    /// For each predicate, exports a ~Fast version that is a non-robust implementation directly with double arithmetic, 
    /// an ~Exact version which completed the full calculation in exact arithmetic, and the preferred version which
    /// implements the adaptive routines returning the correct sign and an approximate value.
    /// </summary>
    static class GeometricPredicatesManaged
    {
        #region Error bounds
        // epsilon is equal to Math.Pow(2.0, -53) and is the largest power of 
        // two that 1.0 + epsilon = 1.0.
        // NOTE: Don't confuse this with double.Epsilon.
        const double epsilon = 1.1102230246251565E-16; 

        // Error bounds for orientation and incircle tests.
        const double resulterrbound = (3.0 + 8.0 * epsilon) * epsilon;
        const double ccwerrboundA = (3.0 + 16.0 * epsilon) * epsilon;
        const double ccwerrboundB = (2.0 + 12.0 * epsilon) * epsilon;
        const double ccwerrboundC = (9.0 + 64.0 * epsilon) * epsilon * epsilon;
        const double o3derrboundA = (7.0 + 56.0 * epsilon) * epsilon;
        const double o3derrboundB = (3.0 + 28.0 * epsilon) * epsilon;
        const double o3derrboundC = (26.0 + 288.0 * epsilon) * epsilon * epsilon;
        const double iccerrboundA = (10.0 + 96.0 * epsilon) * epsilon;
        const double iccerrboundB = (4.0 + 48.0 * epsilon) * epsilon;
        const double iccerrboundC = (44.0 + 576.0 * epsilon) * epsilon * epsilon;
        const double isperrboundA = (16.0 + 224.0 * epsilon) * epsilon;
        const double isperrboundB = (5.0 + 72.0 * epsilon) * epsilon;
        const double isperrboundC = (71.0 + 1408.0 * epsilon) * epsilon * epsilon;
        
        #endregion

        #region Orient2D
        /// <summary>
        /// Non-robust approximate 2D orientation test.
        /// </summary>
        /// <param name="pa">array with x and y coordinates of pa.</param>
        /// <param name="pb">array with x and y coordinates of pb.</param>
        /// <param name="pc">array with x and y coordinates of pc.</param>
        /// <returns>a positive value if the points pa, pb, and pc occur
        /// in counterclockwise order; a negative value if they occur in 
        /// clockwise order; and zero if they are collinear. 
        /// The result is also a rough aproximation of twice the signed 
        /// area of the triangle defined by the three points.</returns>
        /// <remarks>The implementation computed the determinant using simple double arithmetic.</remarks>
        public static double Orient2DFast(double[] pa, double[] pb, double[] pc)
        {
            double acx, bcx, acy, bcy;

            acx = pa[0] - pc[0];
            bcx = pb[0] - pc[0];
            acy = pa[1] - pc[1];
            bcy = pb[1] - pc[1];
            return acx * bcy - acy * bcx; 
        }

        internal static double Orient2DExact(double[] pa, double[] pb, double[] pc)
        {
            double axby1, axcy1, bxcy1, bxay1, cxay1, cxby1;
            double axby0, axcy0, bxcy0, bxay0, cxay0, cxby0;
            double[] aterms = new double[4];
            double[] bterms = new double[4];
            double[] cterms = new double[4];
            double aterms3, bterms3, cterms3;
            double[] v = new double[8];
            double[] w = new double[12];
            int vlength, wlength;

            EA.TwoProduct(pa[0], pb[1], out axby1, out axby0);
            EA.TwoProduct(pa[0], pc[1], out axcy1, out axcy0);
            EA.TwoTwoDiff(axby1, axby0, axcy1, axcy0, out aterms3, out aterms[2], out aterms[1], out aterms[0]);
            aterms[3] = aterms3;

            EA.TwoProduct(pb[0], pc[1], out bxcy1, out bxcy0);
            EA.TwoProduct(pb[0], pa[1], out bxay1, out bxay0);
            EA.TwoTwoDiff(bxcy1, bxcy0, bxay1, bxay0, out bterms3, out bterms[2], out bterms[1], out bterms[0]);
            bterms[3] = bterms3;

            EA.TwoProduct(pc[0], pa[1], out cxay1, out cxay0);
            EA.TwoProduct(pc[0], pb[1], out cxby1, out cxby0);
            EA.TwoTwoDiff(cxay1, cxay0, cxby1, cxby0, out cterms3, out cterms[2], out cterms[1], out cterms[0]);
            cterms[3] = cterms3;

            vlength = EA.FastExpansionSumZeroElim(4, aterms, 4, bterms, v);
            wlength = EA.FastExpansionSumZeroElim(vlength, v, 4, cterms, w);

            // In S. predicates.c, this returns the largest component: 
            // return w[wlength - 1];
            // However, this is not stable due to the expansions not being unique,
            // So we return the summed estimate as the 'Exact' value.
            return EA.Estimate(wlength, w);
        }

        internal static double Orient2DSlow(double[] pa, double[] pb, double[] pc)
        {
            double acx, acy, bcx, bcy;
            double acxtail, acytail;
            double bcxtail, bcytail;
            double negate, negatetail;
            double[] axby = new double[8];
            double[] bxay = new double[8];
            double axby7, bxay7;
            double[] deter = new double[16];
            int deterlen;

            EA.TwoDiff(pa[0], pc[0], out acx, out acxtail);
            EA.TwoDiff(pa[1], pc[1], out acy, out acytail);
            EA.TwoDiff(pb[0], pc[0], out bcx, out bcxtail);
            EA.TwoDiff(pb[1], pc[1], out bcy, out bcytail);

            EA.TwoTwoProduct(acx, acxtail, bcy, bcytail,
                            out axby7, out axby[6], out axby[5], out axby[4],
                            out axby[3], out axby[2], out axby[1], out axby[0]);
            axby[7] = axby7;
            negate = -acy;
            negatetail = -acytail;
            EA.TwoTwoProduct(bcx, bcxtail, negate, negatetail,
                            out bxay7, out bxay[6], out bxay[5], out bxay[4],
                            out bxay[3], out bxay[2], out bxay[1], out bxay[0]);
            bxay[7] = bxay7;

            deterlen = EA.FastExpansionSumZeroElim(8, axby, 8, bxay, deter);

            // In S. predicates.c, this returns the largest component: 
            // deter[deterlen - 1];
            // However, this is not stable due to the expansions not being unique (even for ZeroElim),
            // So we return the summed estimate as the 'Exact' value.
            return EA.Estimate(deterlen, deter);
        }

        /// <summary>
        /// Adaptive, robust 2D orientation test.
        /// </summary>
        /// <param name="pa">array with x and y coordinates of pa.</param>
        /// <param name="pb">array with x and y coordinates of pb.</param>
        /// <param name="pc">array with x and y coordinates of pc.</param>
        /// <returns>a positive value if the points pa, pb, and pc occur
        /// in counterclockwise order; a negative value if they occur in 
        /// clockwise order; and zero if they are collinear. 
        /// The result is also an aproximation of twice the signed 
        /// area of the triangle defined by the three points.</returns>
        public static double Orient2D(double[] pa, double[] pb, double[] pc)
        {
            double detleft, detright, det;
            double detsum, errbound;

            detleft = (pa[0] - pc[0]) * (pb[1] - pc[1]);
            detright = (pa[1] - pc[1]) * (pb[0] - pc[0]);
            det = detleft - detright;

            if (detleft > 0.0) 
            {
                if (detright <= 0.0) 
                {
                    return det;
                } 
                else 
                {
                    detsum = detleft + detright;
                }
            } 
            else if (detleft < 0.0) 
            {
                if (detright >= 0.0) 
                {
                    return det;
                } 
                else 
                {
                    detsum = -detleft - detright;
                }
            } 
            else 
            {
                return det;
            }

            errbound = ccwerrboundA * detsum;
            if ((det >= errbound) || (-det >= errbound)) 
            {
                return det;
            }

            return Orient2DAdapt(pa, pb, pc, detsum);
        }

        // Internal adaptive continuation
        static double Orient2DAdapt(double[] pa, double[] pb, double[] pc, double detsum)
        {
            double acx, acy, bcx, bcy;
            double acxtail, acytail, bcxtail, bcytail;
            double detleft, detright;
            double detlefttail, detrighttail;
            double det, errbound;
            double[] B = new double[4];
            double[] C1 = new double[8];
            double[] C2 = new double[12];
            double[] D = new double[16];
            double B3;
            int C1length, C2length, Dlength;
            double[] u = new double[4];
            double u3;
            double s1, t1;
            double s0, t0;

            acx = pa[0] - pc[0];
            bcx = pb[0] - pc[0];
            acy = pa[1] - pc[1];
            bcy = pb[1] - pc[1];

            EA.TwoProduct(acx, bcy, out detleft, out detlefttail);
            EA.TwoProduct(acy, bcx, out detright, out detrighttail);

            EA.TwoTwoDiff(detleft, detlefttail, detright, detrighttail,
                        out B3, out B[2], out B[1], out B[0]);
            B[3] = B3;

            det = EA.Estimate(4, B);
            errbound = ccwerrboundB * detsum;
            if ((det >= errbound) || (-det >= errbound))
            {
                return det;
            }

            EA.TwoDiffTail(pa[0], pc[0], acx, out acxtail);
            EA.TwoDiffTail(pb[0], pc[0], bcx, out bcxtail);
            EA.TwoDiffTail(pa[1], pc[1], acy, out acytail);
            EA.TwoDiffTail(pb[1], pc[1], bcy, out bcytail);

            if ((acxtail == 0.0) && (acytail == 0.0)
                && (bcxtail == 0.0) && (bcytail == 0.0))
            {
                return det;
            }

            errbound = ccwerrboundC * detsum + resulterrbound * System.Math.Abs(det);
            det += (acx * bcytail + bcy * acxtail)
                - (acy * bcxtail + bcx * acytail);
            if ((det >= errbound) || (-det >= errbound))
            {
                return det;
            }

            EA.TwoProduct(acxtail, bcy, out s1, out s0);
            EA.TwoProduct(acytail, bcx, out t1, out t0);
            EA.TwoTwoDiff(s1, s0, t1, t0, out u3, out u[2], out u[1], out u[0]);
            u[3] = u3;
            C1length = EA.FastExpansionSumZeroElim(4, B, 4, u, C1);

            EA.TwoProduct(acx, bcytail, out s1, out s0);
            EA.TwoProduct(acy, bcxtail, out t1, out t0);
            EA.TwoTwoDiff(s1, s0, t1, t0, out u3, out u[2], out u[1], out u[0]);
            u[3] = u3;
            C2length = EA.FastExpansionSumZeroElim(C1length, C1, 4, u, C2);

            EA.TwoProduct(acxtail, bcytail, out s1, out s0);
            EA.TwoProduct(acytail, bcxtail, out t1, out t0);
            EA.TwoTwoDiff(s1, s0, t1, t0, out u3, out u[2], out u[1], out u[0]);
            u[3] = u3;
            Dlength = EA.FastExpansionSumZeroElim(C2length, C2, 4, u, D);

            return (D[Dlength - 1]);
        }

        #endregion

        #region Orient3D
        
        /*****************************************************************************/
        /*                                                                           */
        /*  orient3dfast()   Approximate 3D orientation test.  Nonrobust.            */
        /*  orient3dexact()   Exact 3D orientation test.  Robust.                    */
        /*  orient3dslow()   Another exact 3D orientation test.  Robust.             */
        /*  orient3d()   Adaptive exact 3D orientation test.  Robust.                */
        /*                                                                           */
        /*               Return a positive value if the point pd lies below the      */
        /*               plane passing through pa, pb, and pc; "below" is defined so */
        /*               that pa, pb, and pc appear in counterclockwise order when   */
        /*               viewed from above the plane.  Returns a negative value if   */
        /*               pd lies above the plane.  Returns zero if the points are    */
        /*               coplanar.  The result is also a rough approximation of six  */
        /*               times the signed volume of the tetrahedron defined by the   */
        /*               four points.                                                */
        /*                                                                           */
        /*  Only the first and last routine should be used; the middle two are for   */
        /*  timings.                                                                 */
        /*                                                                           */
        /*  The last three use exact arithmetic to ensure a correct answer.  The     */
        /*  result returned is the determinant of a matrix.  In orient3d() only,     */
        /*  this determinant is computed adaptively, in the sense that exact         */
        /*  arithmetic is used only to the degree it is needed to ensure that the    */
        /*  returned value has the correct sign.  Hence, orient3d() is usually quite */
        /*  fast, but will run more slowly when the input points are coplanar or     */
        /*  nearly so.                                                               */
        /*                                                                           */
        /*****************************************************************************/

        public static double Orient3DFast(double[] pa, double[] pb, double[] pc, double[] pd)
        {
            double adx, bdx, cdx;
            double ady, bdy, cdy;
            double adz, bdz, cdz;

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

        internal static double Orient3DExact(double[] pa, double[] pb, double[] pc, double[] pd)
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

            EA.TwoProduct(pa[0], pb[1], out axby1, out axby0);
            EA.TwoProduct(pb[0], pa[1], out bxay1, out bxay0);
            EA.TwoTwoDiff(axby1, axby0, bxay1, bxay0, out ab[3], out ab[2], out ab[1], out ab[0]);

            EA.TwoProduct(pb[0], pc[1], out bxcy1, out bxcy0);
            EA.TwoProduct(pc[0], pb[1], out cxby1, out cxby0);
            EA.TwoTwoDiff(bxcy1, bxcy0, cxby1, cxby0, out bc[3], out bc[2], out bc[1], out bc[0]);

            EA.TwoProduct(pc[0], pd[1], out cxdy1, out cxdy0);
            EA.TwoProduct(pd[0], pc[1], out dxcy1, out dxcy0);
            EA.TwoTwoDiff(cxdy1, cxdy0, dxcy1, dxcy0, out cd[3], out cd[2], out cd[1], out cd[0]);

            EA.TwoProduct(pd[0], pa[1], out dxay1, out dxay0);
            EA.TwoProduct(pa[0], pd[1], out axdy1, out axdy0);
            EA.TwoTwoDiff(dxay1, dxay0, axdy1, axdy0, out da[3], out da[2], out da[1], out da[0]);

            EA.TwoProduct(pa[0], pc[1], out axcy1, out axcy0);
            EA.TwoProduct(pc[0], pa[1], out cxay1, out cxay0);
            EA.TwoTwoDiff(axcy1, axcy0, cxay1, cxay0, out ac[3], out ac[2], out ac[1], out ac[0]);

            EA.TwoProduct(pb[0], pd[1], out bxdy1, out bxdy0);
            EA.TwoProduct(pd[0], pb[1], out dxby1, out dxby0);
            EA.TwoTwoDiff(bxdy1, bxdy0, dxby1, dxby0, out bd[3], out bd[2], out bd[1], out bd[0]);

            templen = EA.FastExpansionSumZeroElim(4, cd, 4, da, temp8);
            cdalen = EA.FastExpansionSumZeroElim(templen, temp8, 4, ac, cda);
            templen = EA.FastExpansionSumZeroElim(4, da, 4, ab, temp8);
            dablen = EA.FastExpansionSumZeroElim(templen, temp8, 4, bd, dab);
            for (i = 0; i < 4; i++)
            {
                bd[i] = -bd[i];
                ac[i] = -ac[i];
            }
            templen = EA.FastExpansionSumZeroElim(4, ab, 4, bc, temp8);
            abclen = EA.FastExpansionSumZeroElim(templen, temp8, 4, ac, abc);
            templen = EA.FastExpansionSumZeroElim(4, bc, 4, cd, temp8);
            bcdlen = EA.FastExpansionSumZeroElim(templen, temp8, 4, bd, bcd);

            alen = EA.ScaleExpansionZeroElim(bcdlen, bcd, pa[2], adet);
            blen = EA.ScaleExpansionZeroElim(cdalen, cda, -pb[2], bdet);
            clen = EA.ScaleExpansionZeroElim(dablen, dab, pc[2], cdet);
            dlen = EA.ScaleExpansionZeroElim(abclen, abc, -pd[2], ddet);

            ablen = EA.FastExpansionSumZeroElim(alen, adet, blen, bdet, abdet);
            cdlen = EA.FastExpansionSumZeroElim(clen, cdet, dlen, ddet, cddet);
            deterlen = EA.FastExpansionSumZeroElim(ablen, abdet, cdlen, cddet, deter);

            // In S. predicates.c, this returns the largest component: 
            // deter[deterlen - 1];
            // However, this is not stable due to the expansions not being unique (even for ZeroElim),
            // So we return the summed estimate as the 'Exact' value.
            return EA.Estimate(deterlen, deter);
        }

        internal static double Orient3DSlow(double[] pa, double[] pb, double[] pc, double[] pd)
        {
            double adx, ady, adz, bdx, bdy, bdz, cdx, cdy, cdz;
            double adxtail, adytail, adztail;
            double bdxtail, bdytail, bdztail;
            double cdxtail, cdytail, cdztail;
            double negate, negatetail;
            double axby7, bxcy7, axcy7, bxay7, cxby7, cxay7;
            double[] axby = new double[8];
            double[] bxcy = new double[8];
            double[] axcy = new double[8];
            double[] bxay = new double[8];
            double[] cxby = new double[8];
            double[] cxay = new double[8];
            double[] temp16 = new double[16];
            double[] temp32 = new double[32];
            double[] temp32t = new double[32];
            int temp16len, temp32len, temp32tlen;
            double[] adet = new double[64];
            double[] bdet = new double[64];
            double[] cdet = new double[64];
            int alen, blen, clen;
            double[] abdet = new double[128];
            int ablen;
            double[] deter = new double[192];
            int deterlen;

            EA.TwoDiff(pa[0], pd[0], out adx, out adxtail);
            EA.TwoDiff(pa[1], pd[1], out ady, out adytail);
            EA.TwoDiff(pa[2], pd[2], out adz, out adztail);
            EA.TwoDiff(pb[0], pd[0], out bdx, out bdxtail);
            EA.TwoDiff(pb[1], pd[1], out bdy, out bdytail);
            EA.TwoDiff(pb[2], pd[2], out bdz, out bdztail);
            EA.TwoDiff(pc[0], pd[0], out cdx, out cdxtail);
            EA.TwoDiff(pc[1], pd[1], out cdy, out cdytail);
            EA.TwoDiff(pc[2], pd[2], out cdz, out cdztail);

            EA.TwoTwoProduct(adx, adxtail, bdy, bdytail,
                            out axby7, out axby[6], out axby[5], out axby[4],
                            out axby[3], out axby[2], out axby[1], out axby[0]);
            axby[7] = axby7;
            negate = -ady;
            negatetail = -adytail;
            EA.TwoTwoProduct(bdx, bdxtail, negate, negatetail,
                            out bxay7, out bxay[6], out bxay[5], out bxay[4],
                            out bxay[3], out bxay[2], out bxay[1], out bxay[0]);
            bxay[7] = bxay7;
            EA.TwoTwoProduct(bdx, bdxtail, cdy, cdytail,
                            out bxcy7, out bxcy[6], out bxcy[5], out bxcy[4],
                            out bxcy[3], out bxcy[2], out bxcy[1], out bxcy[0]);
            bxcy[7] = bxcy7;
            negate = -bdy;
            negatetail = -bdytail;
            EA.TwoTwoProduct(cdx, cdxtail, negate, negatetail,
                            out cxby7, out cxby[6], out cxby[5],out cxby[4],
                            out cxby[3], out cxby[2], out cxby[1], out cxby[0]);
            cxby[7] = cxby7;
            EA.TwoTwoProduct(cdx, cdxtail, ady, adytail,
                            out cxay7, out cxay[6], out cxay[5], out cxay[4],
                            out cxay[3], out cxay[2], out cxay[1], out cxay[0]);
            cxay[7] = cxay7;
            negate = -cdy;
            negatetail = -cdytail;
            EA.TwoTwoProduct(adx, adxtail, negate, negatetail,
                            out axcy7, out axcy[6], out axcy[5], out axcy[4],
                            out axcy[3], out axcy[2], out axcy[1], out axcy[0]);
            axcy[7] = axcy7;

            temp16len = EA.FastExpansionSumZeroElim(8, bxcy, 8, cxby, temp16);
            temp32len = EA.ScaleExpansionZeroElim(temp16len, temp16, adz, temp32);
            temp32tlen = EA.ScaleExpansionZeroElim(temp16len, temp16, adztail, temp32t);
            alen = EA.FastExpansionSumZeroElim(temp32len, temp32, temp32tlen, temp32t, adet);

            temp16len = EA.FastExpansionSumZeroElim(8, cxay, 8, axcy, temp16);
            temp32len = EA.ScaleExpansionZeroElim(temp16len, temp16, bdz, temp32);
            temp32tlen = EA.ScaleExpansionZeroElim(temp16len, temp16, bdztail, temp32t);
            blen = EA.FastExpansionSumZeroElim(temp32len, temp32, temp32tlen, temp32t, bdet);

            temp16len = EA.FastExpansionSumZeroElim(8, axby, 8, bxay, temp16);
            temp32len = EA.ScaleExpansionZeroElim(temp16len, temp16, cdz, temp32);
            temp32tlen = EA.ScaleExpansionZeroElim(temp16len, temp16, cdztail, temp32t);
            clen = EA.FastExpansionSumZeroElim(temp32len, temp32, temp32tlen, temp32t, cdet);

            ablen = EA.FastExpansionSumZeroElim(alen, adet, blen, bdet, abdet);
            deterlen = EA.FastExpansionSumZeroElim(ablen, abdet, clen, cdet, deter);

            // In S. predicates.c, this returns the largest component: 
            // deter[deterlen - 1];
            // However, this is not stable due to the expansions not being unique (even for ZeroElim),
            // So we return the summed estimate as the 'Exact' value.
            return EA.Estimate(deterlen, deter);
        }

        public static double Orient3D(double[] pa, double[] pb, double[] pc, double[] pd)
        {
            double adx, bdx, cdx, ady, bdy, cdy, adz, bdz, cdz;
            double bdxcdy, cdxbdy, cdxady, adxcdy, adxbdy, bdxady;
            double det;
            double permanent, errbound;

            adx = pa[0] - pd[0];
            bdx = pb[0] - pd[0];
            cdx = pc[0] - pd[0];
            ady = pa[1] - pd[1];
            bdy = pb[1] - pd[1];
            cdy = pc[1] - pd[1];
            adz = pa[2] - pd[2];
            bdz = pb[2] - pd[2];
            cdz = pc[2] - pd[2];

            bdxcdy = bdx * cdy;
            cdxbdy = cdx * bdy;

            cdxady = cdx * ady;
            adxcdy = adx * cdy;

            adxbdy = adx * bdy;
            bdxady = bdx * ady;

            det = adz * (bdxcdy - cdxbdy) 
                + bdz * (cdxady - adxcdy)
                + cdz * (adxbdy - bdxady);

            permanent = (System.Math.Abs(bdxcdy) + System.Math.Abs(cdxbdy)) * System.Math.Abs(adz)
                    + (System.Math.Abs(cdxady) + System.Math.Abs(adxcdy)) * System.Math.Abs(bdz)
                    + (System.Math.Abs(adxbdy) + System.Math.Abs(bdxady)) * System.Math.Abs(cdz);
            errbound = o3derrboundA * permanent;

            if ((det > errbound) || (-det > errbound)) 
            {
                return det;
            }

            return Orient3DAdapt(pa, pb, pc, pd, permanent);
        }

        // Adaptive continuation for Orient3D
        static double Orient3DAdapt(double[] pa, double[] pb, double[] pc, double[] pd, double permanent)
        {
            double adx, bdx, cdx, ady, bdy, cdy, adz, bdz, cdz;
            double det, errbound;

            double bdxcdy1, cdxbdy1, cdxady1, adxcdy1, adxbdy1, bdxady1;
            double bdxcdy0, cdxbdy0, cdxady0, adxcdy0, adxbdy0, bdxady0;
            double[] bc = new double[4];
            double[] ca = new double[4];
            double[] ab = new double[4];
            double bc3, ca3, ab3;
            double[] adet = new double[8];
            double[] bdet = new double[8];
            double[] cdet = new double[8];
            int alen, blen, clen;
            double[] abdet = new double[16];
            int ablen;
            double[] finnow, finother, finswap;
            double[] fin1 = new double[192];
            double[] fin2 = new double[192];
            int finlength;

            double adxtail, bdxtail, cdxtail;
            double adytail, bdytail, cdytail;
            double adztail, bdztail, cdztail;
            double at_blarge, at_clarge;
            double bt_clarge, bt_alarge;
            double ct_alarge, ct_blarge;
            double[] at_b = new double[4];
            double[] at_c = new double[4];
            double[] bt_c = new double[4];
            double[] bt_a = new double[4];
            double[] ct_a = new double[4];
            double[] ct_b = new double[4];
            int at_blen, at_clen, bt_clen, bt_alen, ct_alen, ct_blen;
            double bdxt_cdy1, cdxt_bdy1, cdxt_ady1;
            double adxt_cdy1, adxt_bdy1, bdxt_ady1;
            double bdxt_cdy0, cdxt_bdy0, cdxt_ady0;
            double adxt_cdy0, adxt_bdy0, bdxt_ady0;
            double bdyt_cdx1, cdyt_bdx1, cdyt_adx1;
            double adyt_cdx1, adyt_bdx1, bdyt_adx1;
            double bdyt_cdx0, cdyt_bdx0, cdyt_adx0;
            double adyt_cdx0, adyt_bdx0, bdyt_adx0;
            double[] bct = new double[8];
            double[] cat = new double[8];
            double[] abt = new double[8];
            int bctlen, catlen, abtlen;
            double bdxt_cdyt1, cdxt_bdyt1, cdxt_adyt1;
            double adxt_cdyt1, adxt_bdyt1, bdxt_adyt1;
            double bdxt_cdyt0, cdxt_bdyt0, cdxt_adyt0;
            double adxt_cdyt0, adxt_bdyt0, bdxt_adyt0;
            double[] u = new double[4];
            double[] v = new double[12];
            double[] w = new double[16];
            double u3;
            int vlength, wlength;
            double negate;

            adx = pa[0] - pd[0];
            bdx = pb[0] - pd[0];
            cdx = pc[0] - pd[0];
            ady = pa[1] - pd[1];
            bdy = pb[1] - pd[1];
            cdy = pc[1] - pd[1];
            adz = pa[2] - pd[2];
            bdz = pb[2] - pd[2];
            cdz = pc[2] - pd[2];

            EA.TwoProduct(bdx, cdy, out bdxcdy1, out bdxcdy0);
            EA.TwoProduct(cdx, bdy, out cdxbdy1, out cdxbdy0);
            EA.TwoTwoDiff(bdxcdy1, bdxcdy0, cdxbdy1, cdxbdy0, out bc3, out bc[2], out bc[1], out bc[0]);
            bc[3] = bc3;
            alen = EA.ScaleExpansionZeroElim(4, bc, adz, adet);

            EA.TwoProduct(cdx, ady, out cdxady1, out cdxady0);
            EA.TwoProduct(adx, cdy, out adxcdy1, out adxcdy0);
            EA.TwoTwoDiff(cdxady1, cdxady0, adxcdy1, adxcdy0, out ca3, out ca[2], out ca[1], out ca[0]);
            ca[3] = ca3;
            blen = EA.ScaleExpansionZeroElim(4, ca, bdz, bdet);

            EA.TwoProduct(adx, bdy, out adxbdy1, out adxbdy0);
            EA.TwoProduct(bdx, ady, out bdxady1, out bdxady0);
            EA.TwoTwoDiff(adxbdy1, adxbdy0, bdxady1, bdxady0, out ab3, out ab[2], out ab[1], out ab[0]);
            ab[3] = ab3;
            clen = EA.ScaleExpansionZeroElim(4, ab, cdz, cdet);

            ablen = EA.FastExpansionSumZeroElim(alen, adet, blen, bdet, abdet);
            finlength = EA.FastExpansionSumZeroElim(ablen, abdet, clen, cdet, fin1);

            det = EA.Estimate(finlength, fin1);
            errbound = o3derrboundB * permanent;
            if ((det >= errbound) || (-det >= errbound))
            {
                return det;
            }

            EA.TwoDiffTail(pa[0], pd[0], adx, out adxtail);
            EA.TwoDiffTail(pb[0], pd[0], bdx, out bdxtail);
            EA.TwoDiffTail(pc[0], pd[0], cdx, out cdxtail);
            EA.TwoDiffTail(pa[1], pd[1], ady, out adytail);
            EA.TwoDiffTail(pb[1], pd[1], bdy, out bdytail);
            EA.TwoDiffTail(pc[1], pd[1], cdy, out cdytail);
            EA.TwoDiffTail(pa[2], pd[2], adz, out adztail);
            EA.TwoDiffTail(pb[2], pd[2], bdz, out bdztail);
            EA.TwoDiffTail(pc[2], pd[2], cdz, out cdztail);

            if ((adxtail == 0.0) && (bdxtail == 0.0) && (cdxtail == 0.0)
                && (adytail == 0.0) && (bdytail == 0.0) && (cdytail == 0.0)
                && (adztail == 0.0) && (bdztail == 0.0) && (cdztail == 0.0))
            {
                return det;
            }

            errbound = o3derrboundC * permanent + resulterrbound * System.Math.Abs(det);
            det += (adz * ((bdx * cdytail + cdy * bdxtail)
                            - (bdy * cdxtail + cdx * bdytail))
                    + adztail * (bdx * cdy - bdy * cdx))
                + (bdz * ((cdx * adytail + ady * cdxtail)
                            - (cdy * adxtail + adx * cdytail))
                    + bdztail * (cdx * ady - cdy * adx))
                + (cdz * ((adx * bdytail + bdy * adxtail)
                            - (ady * bdxtail + bdx * adytail))
                    + cdztail * (adx * bdy - ady * bdx));
            if ((det >= errbound) || (-det >= errbound))
            {
                return det;
            }

            finnow = fin1;
            finother = fin2;

            if (adxtail == 0.0)
            {
                if (adytail == 0.0)
                {
                    at_b[0] = 0.0;
                    at_blen = 1;
                    at_c[0] = 0.0;
                    at_clen = 1;
                }
                else
                {
                    negate = -adytail;
                    EA.TwoProduct(negate, bdx, out at_blarge, out at_b[0]);
                    at_b[1] = at_blarge;
                    at_blen = 2;
                    EA.TwoProduct(adytail, cdx, out at_clarge, out at_c[0]);
                    at_c[1] = at_clarge;
                    at_clen = 2;
                }
            }
            else
            {
                if (adytail == 0.0)
                {
                    EA.TwoProduct(adxtail, bdy, out at_blarge, out at_b[0]);
                    at_b[1] = at_blarge;
                    at_blen = 2;
                    negate = -adxtail;
                    EA.TwoProduct(negate, cdy, out at_clarge, out at_c[0]);
                    at_c[1] = at_clarge;
                    at_clen = 2;
                }
                else
                {
                    EA.TwoProduct(adxtail, bdy, out adxt_bdy1, out adxt_bdy0);
                    EA.TwoProduct(adytail, bdx, out adyt_bdx1, out adyt_bdx0);
                    EA.TwoTwoDiff(adxt_bdy1, adxt_bdy0, adyt_bdx1, adyt_bdx0,
                                out at_blarge, out at_b[2], out at_b[1], out at_b[0]);
                    at_b[3] = at_blarge;
                    at_blen = 4;
                    EA.TwoProduct(adytail, cdx, out adyt_cdx1, out adyt_cdx0);
                    EA.TwoProduct(adxtail, cdy, out adxt_cdy1, out adxt_cdy0);
                    EA.TwoTwoDiff(adyt_cdx1, adyt_cdx0, adxt_cdy1, adxt_cdy0,
                                out at_clarge, out at_c[2], out at_c[1], out at_c[0]);
                    at_c[3] = at_clarge;
                    at_clen = 4;
                }
            }
            if (bdxtail == 0.0)
            {
                if (bdytail == 0.0)
                {
                    bt_c[0] = 0.0;
                    bt_clen = 1;
                    bt_a[0] = 0.0;
                    bt_alen = 1;
                }
                else
                {
                    negate = -bdytail;
                    EA.TwoProduct(negate, cdx, out bt_clarge, out bt_c[0]);
                    bt_c[1] = bt_clarge;
                    bt_clen = 2;
                    EA.TwoProduct(bdytail, adx, out bt_alarge, out bt_a[0]);
                    bt_a[1] = bt_alarge;
                    bt_alen = 2;
                }
            }
            else
            {
                if (bdytail == 0.0)
                {
                    EA.TwoProduct(bdxtail, cdy, out bt_clarge, out bt_c[0]);
                    bt_c[1] = bt_clarge;
                    bt_clen = 2;
                    negate = -bdxtail;
                    EA.TwoProduct(negate, ady, out bt_alarge, out bt_a[0]);
                    bt_a[1] = bt_alarge;
                    bt_alen = 2;
                }
                else
                {
                    EA.TwoProduct(bdxtail, cdy, out bdxt_cdy1, out bdxt_cdy0);
                    EA.TwoProduct(bdytail, cdx, out bdyt_cdx1, out bdyt_cdx0);
                    EA.TwoTwoDiff(bdxt_cdy1, bdxt_cdy0, bdyt_cdx1, bdyt_cdx0,
                                out bt_clarge, out bt_c[2], out bt_c[1], out bt_c[0]);
                    bt_c[3] = bt_clarge;
                    bt_clen = 4;
                    EA.TwoProduct(bdytail, adx, out bdyt_adx1, out bdyt_adx0);
                    EA.TwoProduct(bdxtail, ady, out bdxt_ady1, out bdxt_ady0);
                    EA.TwoTwoDiff(bdyt_adx1, bdyt_adx0, bdxt_ady1, bdxt_ady0,
                                out bt_alarge, out bt_a[2], out bt_a[1], out bt_a[0]);
                    bt_a[3] = bt_alarge;
                    bt_alen = 4;
                }
            }
            if (cdxtail == 0.0)
            {
                if (cdytail == 0.0)
                {
                    ct_a[0] = 0.0;
                    ct_alen = 1;
                    ct_b[0] = 0.0;
                    ct_blen = 1;
                }
                else
                {
                    negate = -cdytail;
                    EA.TwoProduct(negate, adx, out ct_alarge, out ct_a[0]);
                    ct_a[1] = ct_alarge;
                    ct_alen = 2;
                    EA.TwoProduct(cdytail, bdx, out ct_blarge, out ct_b[0]);
                    ct_b[1] = ct_blarge;
                    ct_blen = 2;
                }
            }
            else
            {
                if (cdytail == 0.0)
                {
                    EA.TwoProduct(cdxtail, ady, out ct_alarge, out ct_a[0]);
                    ct_a[1] = ct_alarge;
                    ct_alen = 2;
                    negate = -cdxtail;
                    EA.TwoProduct(negate, bdy, out ct_blarge, out ct_b[0]);
                    ct_b[1] = ct_blarge;
                    ct_blen = 2;
                }
                else
                {
                    EA.TwoProduct(cdxtail, ady, out cdxt_ady1, out cdxt_ady0);
                    EA.TwoProduct(cdytail, adx, out cdyt_adx1, out cdyt_adx0);
                    EA.TwoTwoDiff(cdxt_ady1, cdxt_ady0, cdyt_adx1, cdyt_adx0,
                                out ct_alarge, out ct_a[2], out ct_a[1], out ct_a[0]);
                    ct_a[3] = ct_alarge;
                    ct_alen = 4;
                    EA.TwoProduct(cdytail, bdx, out cdyt_bdx1, out cdyt_bdx0);
                    EA.TwoProduct(cdxtail, bdy, out cdxt_bdy1, out cdxt_bdy0);
                    EA.TwoTwoDiff(cdyt_bdx1, cdyt_bdx0, cdxt_bdy1, cdxt_bdy0,
                                out ct_blarge, out ct_b[2], out ct_b[1], out ct_b[0]);
                    ct_b[3] = ct_blarge;
                    ct_blen = 4;
                }
            }

            bctlen = EA.FastExpansionSumZeroElim(bt_clen, bt_c, ct_blen, ct_b, bct);
            wlength = EA.ScaleExpansionZeroElim(bctlen, bct, adz, w);
            finlength = EA.FastExpansionSumZeroElim(finlength, finnow, wlength, w, finother);
            finswap = finnow; finnow = finother; finother = finswap;

            catlen = EA.FastExpansionSumZeroElim(ct_alen, ct_a, at_clen, at_c, cat);
            wlength = EA.ScaleExpansionZeroElim(catlen, cat, bdz, w);
            finlength = EA.FastExpansionSumZeroElim(finlength, finnow, wlength, w, finother);
            finswap = finnow; finnow = finother; finother = finswap;

            abtlen = EA.FastExpansionSumZeroElim(at_blen, at_b, bt_alen, bt_a, abt);
            wlength = EA.ScaleExpansionZeroElim(abtlen, abt, cdz, w);
            finlength = EA.FastExpansionSumZeroElim(finlength, finnow, wlength, w, finother);
            finswap = finnow; finnow = finother; finother = finswap;

            if (adztail != 0.0)
            {
                vlength = EA.ScaleExpansionZeroElim(4, bc, adztail, v);
                finlength = EA.FastExpansionSumZeroElim(finlength, finnow, vlength, v, finother);
                finswap = finnow; finnow = finother; finother = finswap;
            }
            if (bdztail != 0.0)
            {
                vlength = EA.ScaleExpansionZeroElim(4, ca, bdztail, v);
                finlength = EA.FastExpansionSumZeroElim(finlength, finnow, vlength, v, finother);
                finswap = finnow; finnow = finother; finother = finswap;
            }
            if (cdztail != 0.0)
            {
                vlength = EA.ScaleExpansionZeroElim(4, ab, cdztail, v);
                finlength = EA.FastExpansionSumZeroElim(finlength, finnow, vlength, v, finother);
                finswap = finnow; finnow = finother; finother = finswap;
            }

            if (adxtail != 0.0)
            {
                if (bdytail != 0.0)
                {
                    EA.TwoProduct(adxtail, bdytail, out adxt_bdyt1, out adxt_bdyt0);
                    EA.TwoOneProduct(adxt_bdyt1, adxt_bdyt0, cdz, out u3, out u[2], out u[1], out u[0]);
                    u[3] = u3;
                    finlength = EA.FastExpansionSumZeroElim(finlength, finnow, 4, u,
                                                            finother);
                    finswap = finnow; finnow = finother; finother = finswap;
                    if (cdztail != 0.0)
                    {
                        EA.TwoOneProduct(adxt_bdyt1, adxt_bdyt0, cdztail, out u3, out u[2], out u[1], out u[0]);
                        u[3] = u3;
                        finlength = EA.FastExpansionSumZeroElim(finlength, finnow, 4, u,
                                                                finother);
                        finswap = finnow; finnow = finother; finother = finswap;
                    }
                }
                if (cdytail != 0.0)
                {
                    negate = -adxtail;
                    EA.TwoProduct(negate, cdytail, out adxt_cdyt1, out adxt_cdyt0);
                    EA.TwoOneProduct(adxt_cdyt1, adxt_cdyt0, bdz, out u3, out u[2], out u[1], out u[0]);
                    u[3] = u3;
                    finlength = EA.FastExpansionSumZeroElim(finlength, finnow, 4, u,
                                                            finother);
                    finswap = finnow; finnow = finother; finother = finswap;
                    if (bdztail != 0.0)
                    {
                        EA.TwoOneProduct(adxt_cdyt1, adxt_cdyt0, bdztail, out u3, out u[2], out u[1], out u[0]);
                        u[3] = u3;
                        finlength = EA.FastExpansionSumZeroElim(finlength, finnow, 4, u,
                                                                finother);
                        finswap = finnow; finnow = finother; finother = finswap;
                    }
                }
            }
            if (bdxtail != 0.0)
            {
                if (cdytail != 0.0)
                {
                    EA.TwoProduct(bdxtail, cdytail, out bdxt_cdyt1, out bdxt_cdyt0);
                    EA.TwoOneProduct(bdxt_cdyt1, bdxt_cdyt0, adz, out u3, out u[2], out u[1], out u[0]);
                    u[3] = u3;
                    finlength = EA.FastExpansionSumZeroElim(finlength, finnow, 4, u,
                                                            finother);
                    finswap = finnow; finnow = finother; finother = finswap;
                    if (adztail != 0.0)
                    {
                        EA.TwoOneProduct(bdxt_cdyt1, bdxt_cdyt0, adztail, out u3, out u[2], out u[1], out u[0]);
                        u[3] = u3;
                        finlength = EA.FastExpansionSumZeroElim(finlength, finnow, 4, u,
                                                                finother);
                        finswap = finnow; finnow = finother; finother = finswap;
                    }
                }
                if (adytail != 0.0)
                {
                    negate = -bdxtail;
                    EA.TwoProduct(negate, adytail, out bdxt_adyt1, out bdxt_adyt0);
                    EA.TwoOneProduct(bdxt_adyt1, bdxt_adyt0, cdz, out u3, out  u[2], out u[1], out u[0]);
                    u[3] = u3;
                    finlength = EA.FastExpansionSumZeroElim(finlength, finnow, 4, u,
                                                            finother);
                    finswap = finnow; finnow = finother; finother = finswap;
                    if (cdztail != 0.0)
                    {
                        EA.TwoOneProduct(bdxt_adyt1, bdxt_adyt0, cdztail, out u3, out u[2], out u[1], out u[0]);
                        u[3] = u3;
                        finlength = EA.FastExpansionSumZeroElim(finlength, finnow, 4, u,
                                                                finother);
                        finswap = finnow; finnow = finother; finother = finswap;
                    }
                }
            }
            if (cdxtail != 0.0)
            {
                if (adytail != 0.0)
                {
                    EA.TwoProduct(cdxtail, adytail, out cdxt_adyt1, out cdxt_adyt0);
                    EA.TwoOneProduct(cdxt_adyt1, cdxt_adyt0, bdz, out u3, out u[2], out u[1], out u[0]);
                    u[3] = u3;
                    finlength = EA.FastExpansionSumZeroElim(finlength, finnow, 4, u,
                                                            finother);
                    finswap = finnow; finnow = finother; finother = finswap;
                    if (bdztail != 0.0)
                    {
                        EA.TwoOneProduct(cdxt_adyt1, cdxt_adyt0, bdztail, out u3, out u[2], out u[1], out u[0]);
                        u[3] = u3;
                        finlength = EA.FastExpansionSumZeroElim(finlength, finnow, 4, u, finother);
                        finswap = finnow; finnow = finother; finother = finswap;
                    }
                }
                if (bdytail != 0.0)
                {
                    negate = -cdxtail;
                    EA.TwoProduct(negate, bdytail, out cdxt_bdyt1, out cdxt_bdyt0);
                    EA.TwoOneProduct(cdxt_bdyt1, cdxt_bdyt0, adz, out u3, out u[2], out u[1], out u[0]);
                    u[3] = u3;
                    finlength = EA.FastExpansionSumZeroElim(finlength, finnow, 4, u,
                                                            finother);
                    finswap = finnow; finnow = finother; finother = finswap;
                    if (adztail != 0.0)
                    {
                        EA.TwoOneProduct(cdxt_bdyt1, cdxt_bdyt0, adztail, out u3, out u[2], out u[1], out u[0]);
                        u[3] = u3;
                        finlength = EA.FastExpansionSumZeroElim(finlength, finnow, 4, u, finother);
                        finswap = finnow; finnow = finother; finother = finswap;
                    }
                }
            }

            if (adztail != 0.0)
            {
                wlength = EA.ScaleExpansionZeroElim(bctlen, bct, adztail, w);
                finlength = EA.FastExpansionSumZeroElim(finlength, finnow, wlength, w, finother);
                finswap = finnow; finnow = finother; finother = finswap;
            }
            if (bdztail != 0.0)
            {
                wlength = EA.ScaleExpansionZeroElim(catlen, cat, bdztail, w);
                finlength = EA.FastExpansionSumZeroElim(finlength, finnow, wlength, w, finother);
                finswap = finnow; finnow = finother; finother = finswap;
            }
            if (cdztail != 0.0)
            {
                wlength = EA.ScaleExpansionZeroElim(abtlen, abt, cdztail, w);
                finlength = EA.FastExpansionSumZeroElim(finlength, finnow, wlength, w, finother);
                finswap = finnow; finnow = finother; finother = finswap;
            }

            return finnow[finlength - 1];
        }
        #endregion

        #region InCircle

        /*****************************************************************************/
        /*                                                                           */
        /*  incirclefast()   Approximate 2D incircle test.  Nonrobust.               */
        /*  incircleexact()   Exact 2D incircle test.  Robust.                       */
        /*  incircleslow()   Another exact 2D incircle test.  Robust.                */
        /*  incircle()   Adaptive exact 2D incircle test.  Robust.                   */
        /*                                                                           */
        /*               Return a positive value if the point pd lies inside the     */
        /*               circle passing through pa, pb, and pc; a negative value if  */
        /*               it lies outside; and zero if the four points are cocircular.*/
        /*               The points pa, pb, and pc must be in counterclockwise       */
        /*               order, or the sign of the result will be reversed.          */
        /*                                                                           */
        /*  Only the first and last routine should be used; the middle two are for   */
        /*  timings.                                                                 */
        /*                                                                           */
        /*  The last three use exact arithmetic to ensure a correct answer.  The     */
        /*  result returned is the determinant of a matrix.  In incircle() only,     */
        /*  this determinant is computed adaptively, in the sense that exact         */
        /*  arithmetic is used only to the degree it is needed to ensure that the    */
        /*  returned value has the correct sign.  Hence, incircle() is usually quite */
        /*  fast, but will run more slowly when the input points are cocircular or   */
        /*  nearly so.                                                               */
        /*                                                                           */
        /*****************************************************************************/
        // |pax pay pax^2+pay^2 1|
        // |pbx pby pbx^2+pby^2 1|
        // |pcx pcy pcx^2+pcy^2 1|
        // |pdx pdy pdx^2+pdy^2 1|
        public static double InCircleFast(double[] pa, double[] pb, double[] pc, double[] pd)
        {
            double adx, ady, bdx, bdy, cdx, cdy;
            double abdet, bcdet, cadet;
            double alift, blift, clift;

            adx = pa[0] - pd[0];
            ady = pa[1] - pd[1];
            bdx = pb[0] - pd[0];
            bdy = pb[1] - pd[1];
            cdx = pc[0] - pd[0];
            cdy = pc[1] - pd[1];

            abdet = adx * bdy - bdx * ady;
            bcdet = bdx * cdy - cdx * bdy;
            cadet = cdx * ady - adx * cdy;
            alift = adx * adx + ady * ady;
            blift = bdx * bdx + bdy * bdy;
            clift = cdx * cdx + cdy * cdy;

            return alift * bcdet + blift * cadet + clift * abdet;
        }

        internal static double InCircleExact(double[] pa, double[] pb, double[] pc, double[] pd)
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
            double[] det24x = new double[24];
            double[] det24y = new double[24];
            double[] det48x = new double[48];
            double[] det48y = new double[48];
            int xlen, ylen;
            double[] adet = new double[96];
            double[] bdet = new double[96];
            double[] cdet = new double[96];
            double[] ddet = new double[96];
            int alen, blen, clen, dlen;
            double[] abdet = new double[192];
            double[] cddet = new double[192];
            int ablen, cdlen;
            double[] deter = new double[384];
            int deterlen;
            int i;

            EA.TwoProduct(pa[0], pb[1], out axby1, out axby0);
            EA.TwoProduct(pb[0], pa[1], out bxay1, out bxay0);
            EA.TwoTwoDiff(axby1, axby0, bxay1, bxay0, out ab[3], out ab[2], out ab[1], out ab[0]);

            EA.TwoProduct(pb[0], pc[1], out bxcy1, out bxcy0);
            EA.TwoProduct(pc[0], pb[1], out cxby1, out cxby0);
            EA.TwoTwoDiff(bxcy1, bxcy0, cxby1, cxby0, out bc[3], out bc[2], out bc[1], out bc[0]);

            EA.TwoProduct(pc[0], pd[1], out cxdy1, out cxdy0);
            EA.TwoProduct(pd[0], pc[1], out dxcy1, out dxcy0);
            EA.TwoTwoDiff(cxdy1, cxdy0, dxcy1, dxcy0, out cd[3], out cd[2], out cd[1], out cd[0]);

            EA.TwoProduct(pd[0], pa[1], out dxay1, out dxay0);
            EA.TwoProduct(pa[0], pd[1], out axdy1, out axdy0);
            EA.TwoTwoDiff(dxay1, dxay0, axdy1, axdy0, out da[3], out da[2], out da[1], out da[0]);

            EA.TwoProduct(pa[0], pc[1], out axcy1, out axcy0);
            EA.TwoProduct(pc[0], pa[1], out cxay1, out cxay0);
            EA.TwoTwoDiff(axcy1, axcy0, cxay1, cxay0, out ac[3], out ac[2], out ac[1], out ac[0]);

            EA.TwoProduct(pb[0], pd[1], out bxdy1, out bxdy0);
            EA.TwoProduct(pd[0], pb[1], out dxby1, out dxby0);
            EA.TwoTwoDiff(bxdy1, bxdy0, dxby1, dxby0, out bd[3], out bd[2], out bd[1], out bd[0]);

            templen = EA.FastExpansionSumZeroElim(4, cd, 4, da, temp8);
            cdalen = EA.FastExpansionSumZeroElim(templen, temp8, 4, ac, cda);
            templen = EA.FastExpansionSumZeroElim(4, da, 4, ab, temp8);
            dablen = EA.FastExpansionSumZeroElim(templen, temp8, 4, bd, dab);
            for (i = 0; i < 4; i++) 
            {
                bd[i] = -bd[i];
                ac[i] = -ac[i];
            }
            templen = EA.FastExpansionSumZeroElim(4, ab, 4, bc, temp8);
            abclen = EA.FastExpansionSumZeroElim(templen, temp8, 4, ac, abc);
            templen = EA.FastExpansionSumZeroElim(4, bc, 4, cd, temp8);
            bcdlen = EA.FastExpansionSumZeroElim(templen, temp8, 4, bd, bcd);

            xlen = EA.ScaleExpansionZeroElim(bcdlen, bcd, pa[0], det24x);
            xlen = EA.ScaleExpansionZeroElim(xlen, det24x, pa[0], det48x);
            ylen = EA.ScaleExpansionZeroElim(bcdlen, bcd, pa[1], det24y);
            ylen = EA.ScaleExpansionZeroElim(ylen, det24y, pa[1], det48y);
            alen = EA.FastExpansionSumZeroElim(xlen, det48x, ylen, det48y, adet);

            xlen = EA.ScaleExpansionZeroElim(cdalen, cda, pb[0], det24x);
            xlen = EA.ScaleExpansionZeroElim(xlen, det24x, -pb[0], det48x);
            ylen = EA.ScaleExpansionZeroElim(cdalen, cda, pb[1], det24y);
            ylen = EA.ScaleExpansionZeroElim(ylen, det24y, -pb[1], det48y);
            blen = EA.FastExpansionSumZeroElim(xlen, det48x, ylen, det48y, bdet);

            xlen = EA.ScaleExpansionZeroElim(dablen, dab, pc[0], det24x);
            xlen = EA.ScaleExpansionZeroElim(xlen, det24x, pc[0], det48x);
            ylen = EA.ScaleExpansionZeroElim(dablen, dab, pc[1], det24y);
            ylen = EA.ScaleExpansionZeroElim(ylen, det24y, pc[1], det48y);
            clen = EA.FastExpansionSumZeroElim(xlen, det48x, ylen, det48y, cdet);

            xlen = EA.ScaleExpansionZeroElim(abclen, abc, pd[0], det24x);
            xlen = EA.ScaleExpansionZeroElim(xlen, det24x, -pd[0], det48x);
            ylen = EA.ScaleExpansionZeroElim(abclen, abc, pd[1], det24y);
            ylen = EA.ScaleExpansionZeroElim(ylen, det24y, -pd[1], det48y);
            dlen = EA.FastExpansionSumZeroElim(xlen, det48x, ylen, det48y, ddet);

            ablen = EA.FastExpansionSumZeroElim(alen, adet, blen, bdet, abdet);
            cdlen = EA.FastExpansionSumZeroElim(clen, cdet, dlen, ddet, cddet);
            deterlen = EA.FastExpansionSumZeroElim(ablen, abdet, cdlen, cddet, deter);
            
            // In S. predicates.c, this returns the largest component: 
            // deter[deterlen - 1];
            // However, this is not stable due to the expansions not being unique (even for ZeroElim),
            // So we return the summed estimate as the 'Exact' value.
            return EA.Estimate(deterlen, deter);
        }

        internal static double InCircleSlow(double[] pa, double[] pb, double[] pc, double[] pd)
        {
            double adx, bdx, cdx, ady, bdy, cdy;
            double adxtail, bdxtail, cdxtail;
            double adytail, bdytail, cdytail;
            double negate, negatetail;
            double axby7, bxcy7, axcy7, bxay7, cxby7, cxay7;
            double[] axby = new double[8];
            double[] bxcy = new double[8];
            double[] axcy = new double[8];
            double[] bxay = new double[8];
            double[] cxby = new double[8];
            double[] cxay = new double[8];
            double[] temp16 = new double[16];
            int temp16len;
            double[] detx = new double[32];
            double[] detxx = new double[64];
            double[] detxt = new double[32];
            double[] detxxt = new double[64];
            double[] detxtxt = new double[64];
            int xlen, xxlen, xtlen, xxtlen, xtxtlen;
            double[] x1 = new double[128];
            double[] x2 = new double[192];
            int x1len, x2len;
            double[] dety = new double[32];
            double[] detyy = new double[64];
            double[] detyt = new double[32];
            double[] detyyt = new double[64];
            double[] detytyt = new double[64];
            int ylen, yylen, ytlen, yytlen, ytytlen;
            double[] y1 = new double[128];
            double[] y2 = new double[192];
            int y1len, y2len;
            double[] adet = new double[384];
            double[] bdet = new double[384];
            double[] cdet = new double[384];
            double[] abdet = new double[768];
            double[] deter = new double[1152];
            int alen, blen, clen, ablen, deterlen;
            int i;

            EA.TwoDiff(pa[0], pd[0], out adx, out adxtail);
            EA.TwoDiff(pa[1], pd[1], out ady, out adytail);
            EA.TwoDiff(pb[0], pd[0], out bdx, out bdxtail);
            EA.TwoDiff(pb[1], pd[1], out bdy, out bdytail);
            EA.TwoDiff(pc[0], pd[0], out cdx, out cdxtail);
            EA.TwoDiff(pc[1], pd[1], out cdy, out cdytail);

            EA.TwoTwoProduct(adx, adxtail, bdy, bdytail,
                            out axby7, out axby[6], out axby[5], out axby[4],
                            out axby[3], out axby[2], out axby[1], out axby[0]);
            axby[7] = axby7;
            negate = -ady;
            negatetail = -adytail;
            EA.TwoTwoProduct(bdx, bdxtail, negate, negatetail,
                            out bxay7, out bxay[6], out bxay[5], out bxay[4],
                            out bxay[3], out bxay[2], out bxay[1], out bxay[0]);
            bxay[7] = bxay7;
            EA.TwoTwoProduct(bdx, bdxtail, cdy, cdytail,
                            out bxcy7, out bxcy[6], out bxcy[5], out bxcy[4],
                            out bxcy[3], out bxcy[2], out bxcy[1], out bxcy[0]);
            bxcy[7] = bxcy7;
            negate = -bdy;
            negatetail = -bdytail;
            EA.TwoTwoProduct(cdx, cdxtail, negate, negatetail,
                            out cxby7, out cxby[6], out cxby[5], out cxby[4],
                            out cxby[3], out cxby[2], out cxby[1], out cxby[0]);
            cxby[7] = cxby7;
            EA.TwoTwoProduct(cdx, cdxtail, ady, adytail,
                            out cxay7, out cxay[6], out cxay[5], out cxay[4],
                            out cxay[3], out cxay[2], out cxay[1], out cxay[0]);
            cxay[7] = cxay7;
            negate = -cdy;
            negatetail = -cdytail;
            EA.TwoTwoProduct(adx, adxtail, negate, negatetail,
                            out axcy7, out axcy[6], out axcy[5], out axcy[4],
                            out axcy[3], out axcy[2], out axcy[1], out axcy[0]);
            axcy[7] = axcy7;


            temp16len = EA.FastExpansionSumZeroElim(8, bxcy, 8, cxby, temp16);

            xlen = EA.ScaleExpansionZeroElim(temp16len, temp16, adx, detx);
            xxlen = EA.ScaleExpansionZeroElim(xlen, detx, adx, detxx);
            xtlen = EA.ScaleExpansionZeroElim(temp16len, temp16, adxtail, detxt);
            xxtlen = EA.ScaleExpansionZeroElim(xtlen, detxt, adx, detxxt);
            for (i = 0; i < xxtlen; i++) 
            {
                detxxt[i] *= 2.0;
            }
            xtxtlen = EA.ScaleExpansionZeroElim(xtlen, detxt, adxtail, detxtxt);
            x1len = EA.FastExpansionSumZeroElim(xxlen, detxx, xxtlen, detxxt, x1);
            x2len = EA.FastExpansionSumZeroElim(x1len, x1, xtxtlen, detxtxt, x2);

            ylen = EA.ScaleExpansionZeroElim(temp16len, temp16, ady, dety);
            yylen = EA.ScaleExpansionZeroElim(ylen, dety, ady, detyy);
            ytlen = EA.ScaleExpansionZeroElim(temp16len, temp16, adytail, detyt);
            yytlen = EA.ScaleExpansionZeroElim(ytlen, detyt, ady, detyyt);
            for (i = 0; i < yytlen; i++) 
            {
                detyyt[i] *= 2.0;
            }
            ytytlen = EA.ScaleExpansionZeroElim(ytlen, detyt, adytail, detytyt);
            y1len = EA.FastExpansionSumZeroElim(yylen, detyy, yytlen, detyyt, y1);
            y2len = EA.FastExpansionSumZeroElim(y1len, y1, ytytlen, detytyt, y2);

            alen = EA.FastExpansionSumZeroElim(x2len, x2, y2len, y2, adet);


            temp16len = EA.FastExpansionSumZeroElim(8, cxay, 8, axcy, temp16);

            xlen = EA.ScaleExpansionZeroElim(temp16len, temp16, bdx, detx);
            xxlen = EA.ScaleExpansionZeroElim(xlen, detx, bdx, detxx);
            xtlen = EA.ScaleExpansionZeroElim(temp16len, temp16, bdxtail, detxt);
            xxtlen = EA.ScaleExpansionZeroElim(xtlen, detxt, bdx, detxxt);
            for (i = 0; i < xxtlen; i++) 
            {
                detxxt[i] *= 2.0;
            }
            xtxtlen = EA.ScaleExpansionZeroElim(xtlen, detxt, bdxtail, detxtxt);
            x1len = EA.FastExpansionSumZeroElim(xxlen, detxx, xxtlen, detxxt, x1);
            x2len = EA.FastExpansionSumZeroElim(x1len, x1, xtxtlen, detxtxt, x2);

            ylen = EA.ScaleExpansionZeroElim(temp16len, temp16, bdy, dety);
            yylen = EA.ScaleExpansionZeroElim(ylen, dety, bdy, detyy);
            ytlen = EA.ScaleExpansionZeroElim(temp16len, temp16, bdytail, detyt);
            yytlen = EA.ScaleExpansionZeroElim(ytlen, detyt, bdy, detyyt);
            for (i = 0; i < yytlen; i++) 
            {
                detyyt[i] *= 2.0;
            }
            ytytlen = EA.ScaleExpansionZeroElim(ytlen, detyt, bdytail, detytyt);
            y1len = EA.FastExpansionSumZeroElim(yylen, detyy, yytlen, detyyt, y1);
            y2len = EA.FastExpansionSumZeroElim(y1len, y1, ytytlen, detytyt, y2);

            blen = EA.FastExpansionSumZeroElim(x2len, x2, y2len, y2, bdet);


            temp16len = EA.FastExpansionSumZeroElim(8, axby, 8, bxay, temp16);

            xlen = EA.ScaleExpansionZeroElim(temp16len, temp16, cdx, detx);
            xxlen = EA.ScaleExpansionZeroElim(xlen, detx, cdx, detxx);
            xtlen = EA.ScaleExpansionZeroElim(temp16len, temp16, cdxtail, detxt);
            xxtlen = EA.ScaleExpansionZeroElim(xtlen, detxt, cdx, detxxt);
            for (i = 0; i < xxtlen; i++) 
            {
                detxxt[i] *= 2.0;
            }
            xtxtlen = EA.ScaleExpansionZeroElim(xtlen, detxt, cdxtail, detxtxt);
            x1len = EA.FastExpansionSumZeroElim(xxlen, detxx, xxtlen, detxxt, x1);
            x2len = EA.FastExpansionSumZeroElim(x1len, x1, xtxtlen, detxtxt, x2);

            ylen = EA.ScaleExpansionZeroElim(temp16len, temp16, cdy, dety);
            yylen = EA.ScaleExpansionZeroElim(ylen, dety, cdy, detyy);
            ytlen = EA.ScaleExpansionZeroElim(temp16len, temp16, cdytail, detyt);
            yytlen = EA.ScaleExpansionZeroElim(ytlen, detyt, cdy, detyyt);
            for (i = 0; i < yytlen; i++) 
            {
                detyyt[i] *= 2.0;
            }
            ytytlen = EA.ScaleExpansionZeroElim(ytlen, detyt, cdytail, detytyt);
            y1len = EA.FastExpansionSumZeroElim(yylen, detyy, yytlen, detyyt, y1);
            y2len = EA.FastExpansionSumZeroElim(y1len, y1, ytytlen, detytyt, y2);

            clen = EA.FastExpansionSumZeroElim(x2len, x2, y2len, y2, cdet);

            ablen = EA.FastExpansionSumZeroElim(alen, adet, blen, bdet, abdet);
            deterlen = EA.FastExpansionSumZeroElim(ablen, abdet, clen, cdet, deter);

            // In S. predicates.c, this returns the largest component: 
            // deter[deterlen - 1];
            // However, this is not stable due to the expansions not being unique (even for ZeroElim),
            // So we return the summed estimate as the 'Exact' value.
            return EA.Estimate(deterlen, deter);
        }

        public static double InCircle(double[] pa, double[] pb, double[] pc, double[] pd)
        {
            double adx, bdx, cdx, ady, bdy, cdy;
            double bdxcdy, cdxbdy, cdxady, adxcdy, adxbdy, bdxady;
            double alift, blift, clift;
            double det;
            double permanent, errbound;

            adx = pa[0] - pd[0];
            bdx = pb[0] - pd[0];
            cdx = pc[0] - pd[0];
            ady = pa[1] - pd[1];
            bdy = pb[1] - pd[1];
            cdy = pc[1] - pd[1];

            bdxcdy = bdx * cdy;
            cdxbdy = cdx * bdy;
            alift = adx * adx + ady * ady;

            cdxady = cdx * ady;
            adxcdy = adx * cdy;
            blift = bdx * bdx + bdy * bdy;

            adxbdy = adx * bdy;
            bdxady = bdx * ady;
            clift = cdx * cdx + cdy * cdy;

            det = alift * (bdxcdy - cdxbdy)
                + blift * (cdxady - adxcdy)
                + clift * (adxbdy - bdxady);

            permanent = (System.Math.Abs(bdxcdy) + System.Math.Abs(cdxbdy)) * alift
                    + (System.Math.Abs(cdxady) + System.Math.Abs(adxcdy)) * blift
                    + (System.Math.Abs(adxbdy) + System.Math.Abs(bdxady)) * clift;
            errbound = iccerrboundA * permanent;
            if ((det > errbound) || (-det > errbound)) 
            {
                return det;
            }

            return InCircleAdapt(pa, pb, pc, pd, permanent);
        }

        // Adaptive continuation of InCircle
        static double InCircleAdapt(double[] pa, double[] pb, double[] pc, double[] pd, double permanent)
        {
            double adx, bdx, cdx, ady, bdy, cdy;
            double det, errbound;

            double bdxcdy1, cdxbdy1, cdxady1, adxcdy1, adxbdy1, bdxady1;
            double bdxcdy0, cdxbdy0, cdxady0, adxcdy0, adxbdy0, bdxady0;
            double[] bc = new double[4];
            double[] ca = new double[4];
            double[] ab = new double[4];
            double bc3, ca3, ab3;
            double[] axbc = new double[8];
            double[] axxbc = new double[16];
            double[] aybc = new double[8];
            double[] ayybc = new double[16];
            double[] adet = new double[32];
            int axbclen, axxbclen, aybclen, ayybclen, alen;
            double[] bxca = new double[8];
            double[] bxxca = new double[16];
            double[] byca = new double[8];
            double[] byyca = new double[16];
            double[] bdet = new double[32];
            int bxcalen, bxxcalen, bycalen, byycalen, blen;
            double[] cxab = new double[8];
            double[] cxxab = new double[16];
            double[] cyab = new double[8];
            double[] cyyab = new double[16];
            double[] cdet = new double[32];
            int cxablen, cxxablen, cyablen, cyyablen, clen;
            double[] abdet = new double[64];
            int ablen;
            double[] fin1 = new double[1152];
            double[] fin2 = new double[1152];
            double[] finnow, finother, finswap;
            int finlength;

            double adxtail, bdxtail, cdxtail, adytail, bdytail, cdytail;
            double adxadx1, adyady1, bdxbdx1, bdybdy1, cdxcdx1, cdycdy1;
            double adxadx0, adyady0, bdxbdx0, bdybdy0, cdxcdx0, cdycdy0;
            double[] aa = new double[4];
            double[] bb = new double[4];
            double[] cc = new double[4];
            double aa3, bb3, cc3;
            double ti1, tj1;
            double ti0, tj0;
            double[] u = new double[4];
            double[] v = new double[4];
            double u3, v3;
            double[] temp8 = new double[8];
            double[] temp16a = new double[16];
            double[] temp16b = new double[16];
            double[] temp16c = new double[16];
            double[] temp32a = new double[32];
            double[] temp32b = new double[32];
            double[] temp48 = new double[48];
            double[] temp64 = new double[64];
            int temp8len, temp16alen, temp16blen, temp16clen;
            int temp32alen, temp32blen, temp48len, temp64len;
            double[] axtbb = new double[8];
            double[] axtcc = new double[8];
            double[] aytbb = new double[8];
            double[] aytcc = new double[8];
            int axtbblen, axtcclen, aytbblen, aytcclen;
            double[] bxtaa = new double[8];
            double[] bxtcc = new double[8];
            double[] bytaa = new double[8];
            double[] bytcc = new double[8];
            int bxtaalen, bxtcclen, bytaalen, bytcclen;
            double[] cxtaa = new double[8];
            double[] cxtbb = new double[8];
            double[] cytaa = new double[8];
            double[] cytbb = new double[8];
            int cxtaalen, cxtbblen, cytaalen, cytbblen;
            double[] axtbc = new double[8];
            double[] aytbc = new double[8];
            double[] bxtca = new double[8];
            double[] bytca = new double[8];
            double[] cxtab = new double[8];
            double[] cytab = new double[8];
            int axtbclen, aytbclen, bxtcalen, bytcalen, cxtablen, cytablen;
            double[] axtbct = new double[16];
            double[] aytbct = new double[16];
            double[] bxtcat = new double[16];
            double[] bytcat = new double[16];
            double[] cxtabt = new double[16];
            double[] cytabt = new double[16];
            int axtbctlen, aytbctlen, bxtcatlen, bytcatlen, cxtabtlen, cytabtlen;
            double[] axtbctt = new double[8];
            double[] aytbctt = new double[8];
            double[] bxtcatt = new double[8];
            double[] bytcatt = new double[8];
            double[] cxtabtt = new double[8];
            double[] cytabtt = new double[8];
            int axtbcttlen, aytbcttlen, bxtcattlen, bytcattlen, cxtabttlen, cytabttlen;
            double[] abt = new double[8];
            double[] bct = new double[8];
            double[] cat = new double[8];
            int abtlen, bctlen, catlen;
            double[] abtt = new double[4];
            double[] bctt = new double[4];
            double[] catt = new double[4];
            int abttlen, bcttlen, cattlen;
            double abtt3, bctt3, catt3;
            double negate;

            // RobustGeometry.NET - Additional initialization, for C# compiler,
            //                      to a value that should cause an error if used by accident.
            axtbclen = 9999;
            aytbclen = 9999;
            bxtcalen = 9999;
            bytcalen = 9999;
            cxtablen = 9999;
            cytablen = 9999;

            adx = pa[0] - pd[0];
            bdx = pb[0] - pd[0];
            cdx = pc[0] - pd[0];
            ady = pa[1] - pd[1];
            bdy = pb[1] - pd[1];
            cdy = pc[1] - pd[1];

            EA.TwoProduct(bdx, cdy, out bdxcdy1, out bdxcdy0);
            EA.TwoProduct(cdx, bdy, out cdxbdy1, out cdxbdy0);
            EA.TwoTwoDiff(bdxcdy1, bdxcdy0, cdxbdy1, cdxbdy0, out bc3, out bc[2], out bc[1], out bc[0]);
            bc[3] = bc3;
            axbclen = EA.ScaleExpansionZeroElim(4, bc, adx, axbc);
            axxbclen = EA.ScaleExpansionZeroElim(axbclen, axbc, adx, axxbc);
            aybclen = EA.ScaleExpansionZeroElim(4, bc, ady, aybc);
            ayybclen = EA.ScaleExpansionZeroElim(aybclen, aybc, ady, ayybc);
            alen = EA.FastExpansionSumZeroElim(axxbclen, axxbc, ayybclen, ayybc, adet);

            EA.TwoProduct(cdx, ady, out cdxady1, out cdxady0);
            EA.TwoProduct(adx, cdy, out adxcdy1, out adxcdy0);
            EA.TwoTwoDiff(cdxady1, cdxady0, adxcdy1, adxcdy0, out ca3, out ca[2], out ca[1], out ca[0]);
            ca[3] = ca3;
            bxcalen = EA.ScaleExpansionZeroElim(4, ca, bdx, bxca);
            bxxcalen = EA.ScaleExpansionZeroElim(bxcalen, bxca, bdx, bxxca);
            bycalen = EA.ScaleExpansionZeroElim(4, ca, bdy, byca);
            byycalen = EA.ScaleExpansionZeroElim(bycalen, byca, bdy, byyca);
            blen = EA.FastExpansionSumZeroElim(bxxcalen, bxxca, byycalen, byyca, bdet);

            EA.TwoProduct(adx, bdy, out adxbdy1, out adxbdy0);
            EA.TwoProduct(bdx, ady, out bdxady1, out bdxady0);
            EA.TwoTwoDiff(adxbdy1, adxbdy0, bdxady1, bdxady0, out ab3, out ab[2], out ab[1], out ab[0]);
            ab[3] = ab3;
            cxablen = EA.ScaleExpansionZeroElim(4, ab, cdx, cxab);
            cxxablen = EA.ScaleExpansionZeroElim(cxablen, cxab, cdx, cxxab);
            cyablen = EA.ScaleExpansionZeroElim(4, ab, cdy, cyab);
            cyyablen = EA.ScaleExpansionZeroElim(cyablen, cyab, cdy, cyyab);
            clen = EA.FastExpansionSumZeroElim(cxxablen, cxxab, cyyablen, cyyab, cdet);

            ablen = EA.FastExpansionSumZeroElim(alen, adet, blen, bdet, abdet);
            finlength = EA.FastExpansionSumZeroElim(ablen, abdet, clen, cdet, fin1);

            det = EA.Estimate(finlength, fin1);
            errbound = iccerrboundB * permanent;
            if ((det >= errbound) || (-det >= errbound))
            {
                return det;
            }

            EA.TwoDiffTail(pa[0], pd[0], adx, out adxtail);
            EA.TwoDiffTail(pa[1], pd[1], ady, out adytail);
            EA.TwoDiffTail(pb[0], pd[0], bdx, out bdxtail);
            EA.TwoDiffTail(pb[1], pd[1], bdy, out bdytail);
            EA.TwoDiffTail(pc[0], pd[0], cdx, out cdxtail);
            EA.TwoDiffTail(pc[1], pd[1], cdy, out cdytail);
            if ((adxtail == 0.0) && (bdxtail == 0.0) && (cdxtail == 0.0)
                && (adytail == 0.0) && (bdytail == 0.0) && (cdytail == 0.0))
            {
                return det;
            }

            errbound = iccerrboundC * permanent + resulterrbound * System.Math.Abs(det);
            det += ((adx * adx + ady * ady) * ((bdx * cdytail + cdy * bdxtail)
                                                - (bdy * cdxtail + cdx * bdytail))
                    + 2.0 * (adx * adxtail + ady * adytail) * (bdx * cdy - bdy * cdx))
                + ((bdx * bdx + bdy * bdy) * ((cdx * adytail + ady * cdxtail)
                                                - (cdy * adxtail + adx * cdytail))
                    + 2.0 * (bdx * bdxtail + bdy * bdytail) * (cdx * ady - cdy * adx))
                + ((cdx * cdx + cdy * cdy) * ((adx * bdytail + bdy * adxtail)
                                                - (ady * bdxtail + bdx * adytail))
                    + 2.0 * (cdx * cdxtail + cdy * cdytail) * (adx * bdy - ady * bdx));
            if ((det >= errbound) || (-det >= errbound))
            {
                return det;
            }

            finnow = fin1;
            finother = fin2;

            if ((bdxtail != 0.0) || (bdytail != 0.0)
                || (cdxtail != 0.0) || (cdytail != 0.0))
            {
                EA.Square(adx, out adxadx1, out adxadx0);
                EA.Square(ady, out adyady1, out adyady0);
                EA.TwoTwoSum(adxadx1, adxadx0, adyady1, adyady0, out aa3, out aa[2], out aa[1], out aa[0]);
                aa[3] = aa3;
            }
            if ((cdxtail != 0.0) || (cdytail != 0.0)
                || (adxtail != 0.0) || (adytail != 0.0))
            {
                EA.Square(bdx, out bdxbdx1, out bdxbdx0);
                EA.Square(bdy, out bdybdy1, out bdybdy0);
                EA.TwoTwoSum(bdxbdx1, bdxbdx0, bdybdy1, bdybdy0, out bb3, out bb[2], out bb[1], out bb[0]);
                bb[3] = bb3;
            }
            if ((adxtail != 0.0) || (adytail != 0.0)
                || (bdxtail != 0.0) || (bdytail != 0.0))
            {
                EA.Square(cdx, out cdxcdx1, out cdxcdx0);
                EA.Square(cdy, out cdycdy1, out cdycdy0);
                EA.TwoTwoSum(cdxcdx1, cdxcdx0, cdycdy1, cdycdy0, out cc3, out cc[2], out cc[1], out cc[0]);
                cc[3] = cc3;
            }

            if (adxtail != 0.0)
            {
                axtbclen = EA.ScaleExpansionZeroElim(4, bc, adxtail, axtbc);
                temp16alen = EA.ScaleExpansionZeroElim(axtbclen, axtbc, 2.0 * adx, temp16a);

                axtcclen = EA.ScaleExpansionZeroElim(4, cc, adxtail, axtcc);
                temp16blen = EA.ScaleExpansionZeroElim(axtcclen, axtcc, bdy, temp16b);

                axtbblen = EA.ScaleExpansionZeroElim(4, bb, adxtail, axtbb);
                temp16clen = EA.ScaleExpansionZeroElim(axtbblen, axtbb, -cdy, temp16c);

                temp32alen = EA.FastExpansionSumZeroElim(temp16alen, temp16a, temp16blen, temp16b, temp32a);
                temp48len = EA.FastExpansionSumZeroElim(temp16clen, temp16c, temp32alen, temp32a, temp48);
                finlength = EA.FastExpansionSumZeroElim(finlength, finnow, temp48len, temp48, finother);
                finswap = finnow; finnow = finother; finother = finswap;
            }
            if (adytail != 0.0)
            {
                aytbclen = EA.ScaleExpansionZeroElim(4, bc, adytail, aytbc);
                temp16alen = EA.ScaleExpansionZeroElim(aytbclen, aytbc, 2.0 * ady, temp16a);

                aytbblen = EA.ScaleExpansionZeroElim(4, bb, adytail, aytbb);
                temp16blen = EA.ScaleExpansionZeroElim(aytbblen, aytbb, cdx, temp16b);

                aytcclen = EA.ScaleExpansionZeroElim(4, cc, adytail, aytcc);
                temp16clen = EA.ScaleExpansionZeroElim(aytcclen, aytcc, -bdx, temp16c);

                temp32alen = EA.FastExpansionSumZeroElim(temp16alen, temp16a, temp16blen, temp16b, temp32a);
                temp48len = EA.FastExpansionSumZeroElim(temp16clen, temp16c, temp32alen, temp32a, temp48);
                finlength = EA.FastExpansionSumZeroElim(finlength, finnow, temp48len, temp48, finother);
                finswap = finnow; finnow = finother; finother = finswap;
            }
            if (bdxtail != 0.0)
            {
                bxtcalen = EA.ScaleExpansionZeroElim(4, ca, bdxtail, bxtca);
                temp16alen = EA.ScaleExpansionZeroElim(bxtcalen, bxtca, 2.0 * bdx, temp16a);

                bxtaalen = EA.ScaleExpansionZeroElim(4, aa, bdxtail, bxtaa);
                temp16blen = EA.ScaleExpansionZeroElim(bxtaalen, bxtaa, cdy, temp16b);

                bxtcclen = EA.ScaleExpansionZeroElim(4, cc, bdxtail, bxtcc);
                temp16clen = EA.ScaleExpansionZeroElim(bxtcclen, bxtcc, -ady, temp16c);

                temp32alen = EA.FastExpansionSumZeroElim(temp16alen, temp16a, temp16blen, temp16b, temp32a);
                temp48len = EA.FastExpansionSumZeroElim(temp16clen, temp16c, temp32alen, temp32a, temp48);
                finlength = EA.FastExpansionSumZeroElim(finlength, finnow, temp48len, temp48, finother);
                finswap = finnow; finnow = finother; finother = finswap;
            }
            if (bdytail != 0.0)
            {
                bytcalen = EA.ScaleExpansionZeroElim(4, ca, bdytail, bytca);
                temp16alen = EA.ScaleExpansionZeroElim(bytcalen, bytca, 2.0 * bdy, temp16a);

                bytcclen = EA.ScaleExpansionZeroElim(4, cc, bdytail, bytcc);
                temp16blen = EA.ScaleExpansionZeroElim(bytcclen, bytcc, adx, temp16b);

                bytaalen = EA.ScaleExpansionZeroElim(4, aa, bdytail, bytaa);
                temp16clen = EA.ScaleExpansionZeroElim(bytaalen, bytaa, -cdx, temp16c);

                temp32alen = EA.FastExpansionSumZeroElim(temp16alen, temp16a, temp16blen, temp16b, temp32a);
                temp48len = EA.FastExpansionSumZeroElim(temp16clen, temp16c, temp32alen, temp32a, temp48);
                finlength = EA.FastExpansionSumZeroElim(finlength, finnow, temp48len, temp48, finother);
                finswap = finnow; finnow = finother; finother = finswap;
            }
            if (cdxtail != 0.0)
            {
                cxtablen = EA.ScaleExpansionZeroElim(4, ab, cdxtail, cxtab);
                temp16alen = EA.ScaleExpansionZeroElim(cxtablen, cxtab, 2.0 * cdx, temp16a);

                cxtbblen = EA.ScaleExpansionZeroElim(4, bb, cdxtail, cxtbb);
                temp16blen = EA.ScaleExpansionZeroElim(cxtbblen, cxtbb, ady, temp16b);

                cxtaalen = EA.ScaleExpansionZeroElim(4, aa, cdxtail, cxtaa);
                temp16clen = EA.ScaleExpansionZeroElim(cxtaalen, cxtaa, -bdy, temp16c);

                temp32alen = EA.FastExpansionSumZeroElim(temp16alen, temp16a, temp16blen, temp16b, temp32a);
                temp48len = EA.FastExpansionSumZeroElim(temp16clen, temp16c, temp32alen, temp32a, temp48);
                finlength = EA.FastExpansionSumZeroElim(finlength, finnow, temp48len, temp48, finother);
                finswap = finnow; finnow = finother; finother = finswap;
            }
            if (cdytail != 0.0)
            {
                cytablen = EA.ScaleExpansionZeroElim(4, ab, cdytail, cytab);
                temp16alen = EA.ScaleExpansionZeroElim(cytablen, cytab, 2.0 * cdy, temp16a);

                cytaalen = EA.ScaleExpansionZeroElim(4, aa, cdytail, cytaa);
                temp16blen = EA.ScaleExpansionZeroElim(cytaalen, cytaa, bdx, temp16b);

                cytbblen = EA.ScaleExpansionZeroElim(4, bb, cdytail, cytbb);
                temp16clen = EA.ScaleExpansionZeroElim(cytbblen, cytbb, -adx, temp16c);

                temp32alen = EA.FastExpansionSumZeroElim(temp16alen, temp16a, temp16blen, temp16b, temp32a);
                temp48len = EA.FastExpansionSumZeroElim(temp16clen, temp16c, temp32alen, temp32a, temp48);
                finlength = EA.FastExpansionSumZeroElim(finlength, finnow, temp48len, temp48, finother);
                finswap = finnow; finnow = finother; finother = finswap;
            }

            if ((adxtail != 0.0) || (adytail != 0.0))
            {
                if ((bdxtail != 0.0) || (bdytail != 0.0)
                    || (cdxtail != 0.0) || (cdytail != 0.0))
                {
                    EA.TwoProduct(bdxtail, cdy, out ti1, out ti0);
                    EA.TwoProduct(bdx, cdytail, out tj1, out tj0);
                    EA.TwoTwoSum(ti1, ti0, tj1, tj0, out u3, out u[2], out u[1], out u[0]);
                    u[3] = u3;
                    negate = -bdy;
                    EA.TwoProduct(cdxtail, negate, out ti1, out ti0);
                    negate = -bdytail;
                    EA.TwoProduct(cdx, negate, out tj1, out tj0);
                    EA.TwoTwoSum(ti1, ti0, tj1, tj0, out v3, out v[2], out v[1], out v[0]);
                    v[3] = v3;
                    bctlen = EA.FastExpansionSumZeroElim(4, u, 4, v, bct);

                    EA.TwoProduct(bdxtail, cdytail, out ti1, out ti0);
                    EA.TwoProduct(cdxtail, bdytail, out tj1, out tj0);
                    EA.TwoTwoDiff(ti1, ti0, tj1, tj0, out bctt3, out bctt[2], out bctt[1], out bctt[0]);
                    bctt[3] = bctt3;
                    bcttlen = 4;
                }
                else
                {
                    bct[0] = 0.0;
                    bctlen = 1;
                    bctt[0] = 0.0;
                    bcttlen = 1;
                }

                if (adxtail != 0.0)
                {
                    temp16alen = EA.ScaleExpansionZeroElim(axtbclen, axtbc, adxtail, temp16a);
                    axtbctlen = EA.ScaleExpansionZeroElim(bctlen, bct, adxtail, axtbct);
                    temp32alen = EA.ScaleExpansionZeroElim(axtbctlen, axtbct, 2.0 * adx, temp32a);
                    temp48len = EA.FastExpansionSumZeroElim(temp16alen, temp16a, temp32alen, temp32a, temp48);
                    finlength = EA.FastExpansionSumZeroElim(finlength, finnow, temp48len, temp48, finother);
                    finswap = finnow; finnow = finother; finother = finswap;
                    if (bdytail != 0.0)
                    {
                        temp8len = EA.ScaleExpansionZeroElim(4, cc, adxtail, temp8);
                        temp16alen = EA.ScaleExpansionZeroElim(temp8len, temp8, bdytail, temp16a);
                        finlength = EA.FastExpansionSumZeroElim(finlength, finnow, temp16alen, temp16a, finother);
                        finswap = finnow; finnow = finother; finother = finswap;
                    }
                    if (cdytail != 0.0)
                    {
                        temp8len = EA.ScaleExpansionZeroElim(4, bb, -adxtail, temp8);
                        temp16alen = EA.ScaleExpansionZeroElim(temp8len, temp8, cdytail, temp16a);
                        finlength = EA.FastExpansionSumZeroElim(finlength, finnow, temp16alen, temp16a, finother);
                        finswap = finnow; finnow = finother; finother = finswap;
                    }

                    temp32alen = EA.ScaleExpansionZeroElim(axtbctlen, axtbct, adxtail, temp32a);
                    axtbcttlen = EA.ScaleExpansionZeroElim(bcttlen, bctt, adxtail, axtbctt);
                    temp16alen = EA.ScaleExpansionZeroElim(axtbcttlen, axtbctt, 2.0 * adx, temp16a);
                    temp16blen = EA.ScaleExpansionZeroElim(axtbcttlen, axtbctt, adxtail, temp16b);
                    temp32blen = EA.FastExpansionSumZeroElim(temp16alen, temp16a, temp16blen, temp16b, temp32b);
                    temp64len = EA.FastExpansionSumZeroElim(temp32alen, temp32a, temp32blen, temp32b, temp64);
                    finlength = EA.FastExpansionSumZeroElim(finlength, finnow, temp64len, temp64, finother);
                    finswap = finnow; finnow = finother; finother = finswap;
                }
                if (adytail != 0.0)
                {
                    temp16alen = EA.ScaleExpansionZeroElim(aytbclen, aytbc, adytail, temp16a);
                    aytbctlen = EA.ScaleExpansionZeroElim(bctlen, bct, adytail, aytbct);
                    temp32alen = EA.ScaleExpansionZeroElim(aytbctlen, aytbct, 2.0 * ady, temp32a);
                    temp48len = EA.FastExpansionSumZeroElim(temp16alen, temp16a, temp32alen, temp32a, temp48);
                    finlength = EA.FastExpansionSumZeroElim(finlength, finnow, temp48len, temp48, finother);
                    finswap = finnow; finnow = finother; finother = finswap;


                    temp32alen = EA.ScaleExpansionZeroElim(aytbctlen, aytbct, adytail, temp32a);
                    aytbcttlen = EA.ScaleExpansionZeroElim(bcttlen, bctt, adytail, aytbctt);
                    temp16alen = EA.ScaleExpansionZeroElim(aytbcttlen, aytbctt, 2.0 * ady, temp16a);
                    temp16blen = EA.ScaleExpansionZeroElim(aytbcttlen, aytbctt, adytail, temp16b);
                    temp32blen = EA.FastExpansionSumZeroElim(temp16alen, temp16a, temp16blen, temp16b, temp32b);
                    temp64len = EA.FastExpansionSumZeroElim(temp32alen, temp32a, temp32blen, temp32b, temp64);
                    finlength = EA.FastExpansionSumZeroElim(finlength, finnow, temp64len, temp64, finother);
                    finswap = finnow; finnow = finother; finother = finswap;
                }
            }
            if ((bdxtail != 0.0) || (bdytail != 0.0))
            {
                if ((cdxtail != 0.0) || (cdytail != 0.0)
                    || (adxtail != 0.0) || (adytail != 0.0))
                {
                    EA.TwoProduct(cdxtail, ady, out ti1, out ti0);
                    EA.TwoProduct(cdx, adytail, out tj1, out tj0);
                    EA.TwoTwoSum(ti1, ti0, tj1, tj0, out u3, out u[2], out u[1], out u[0]);
                    u[3] = u3;
                    negate = -cdy;
                    EA.TwoProduct(adxtail, negate, out ti1, out ti0);
                    negate = -cdytail;
                    EA.TwoProduct(adx, negate, out tj1, out tj0);
                    EA.TwoTwoSum(ti1, ti0, tj1, tj0, out v3, out v[2], out v[1], out v[0]);
                    v[3] = v3;
                    catlen = EA.FastExpansionSumZeroElim(4, u, 4, v, cat);

                    EA.TwoProduct(cdxtail, adytail, out ti1, out ti0);
                    EA.TwoProduct(adxtail, cdytail, out tj1, out tj0);
                    EA.TwoTwoDiff(ti1, ti0, tj1, tj0, out catt3, out catt[2], out catt[1], out catt[0]);
                    catt[3] = catt3;
                    cattlen = 4;
                }
                else
                {
                    cat[0] = 0.0;
                    catlen = 1;
                    catt[0] = 0.0;
                    cattlen = 1;
                }

                if (bdxtail != 0.0)
                {
                    temp16alen = EA.ScaleExpansionZeroElim(bxtcalen, bxtca, bdxtail, temp16a);
                    bxtcatlen = EA.ScaleExpansionZeroElim(catlen, cat, bdxtail, bxtcat);
                    temp32alen = EA.ScaleExpansionZeroElim(bxtcatlen, bxtcat, 2.0 * bdx, temp32a);
                    temp48len = EA.FastExpansionSumZeroElim(temp16alen, temp16a, temp32alen, temp32a, temp48);
                    finlength = EA.FastExpansionSumZeroElim(finlength, finnow, temp48len, temp48, finother);
                    finswap = finnow; finnow = finother; finother = finswap;
                    if (cdytail != 0.0)
                    {
                        temp8len = EA.ScaleExpansionZeroElim(4, aa, bdxtail, temp8);
                        temp16alen = EA.ScaleExpansionZeroElim(temp8len, temp8, cdytail, temp16a);
                        finlength = EA.FastExpansionSumZeroElim(finlength, finnow, temp16alen, temp16a, finother);
                        finswap = finnow; finnow = finother; finother = finswap;
                    }
                    if (adytail != 0.0)
                    {
                        temp8len = EA.ScaleExpansionZeroElim(4, cc, -bdxtail, temp8);
                        temp16alen = EA.ScaleExpansionZeroElim(temp8len, temp8, adytail, temp16a);
                        finlength = EA.FastExpansionSumZeroElim(finlength, finnow, temp16alen, temp16a, finother);
                        finswap = finnow; finnow = finother; finother = finswap;
                    }

                    temp32alen = EA.ScaleExpansionZeroElim(bxtcatlen, bxtcat, bdxtail, temp32a);
                    bxtcattlen = EA.ScaleExpansionZeroElim(cattlen, catt, bdxtail, bxtcatt);
                    temp16alen = EA.ScaleExpansionZeroElim(bxtcattlen, bxtcatt, 2.0 * bdx, temp16a);
                    temp16blen = EA.ScaleExpansionZeroElim(bxtcattlen, bxtcatt, bdxtail, temp16b);
                    temp32blen = EA.FastExpansionSumZeroElim(temp16alen, temp16a, temp16blen, temp16b, temp32b);
                    temp64len = EA.FastExpansionSumZeroElim(temp32alen, temp32a, temp32blen, temp32b, temp64);
                    finlength = EA.FastExpansionSumZeroElim(finlength, finnow, temp64len, temp64, finother);
                    finswap = finnow; finnow = finother; finother = finswap;
                }
                if (bdytail != 0.0)
                {
                    temp16alen = EA.ScaleExpansionZeroElim(bytcalen, bytca, bdytail, temp16a);
                    bytcatlen = EA.ScaleExpansionZeroElim(catlen, cat, bdytail, bytcat);
                    temp32alen = EA.ScaleExpansionZeroElim(bytcatlen, bytcat, 2.0 * bdy, temp32a);
                    temp48len = EA.FastExpansionSumZeroElim(temp16alen, temp16a, temp32alen, temp32a, temp48);
                    finlength = EA.FastExpansionSumZeroElim(finlength, finnow, temp48len, temp48, finother);
                    finswap = finnow; finnow = finother; finother = finswap;


                    temp32alen = EA.ScaleExpansionZeroElim(bytcatlen, bytcat, bdytail, temp32a);
                    bytcattlen = EA.ScaleExpansionZeroElim(cattlen, catt, bdytail, bytcatt);
                    temp16alen = EA.ScaleExpansionZeroElim(bytcattlen, bytcatt, 2.0 * bdy, temp16a);
                    temp16blen = EA.ScaleExpansionZeroElim(bytcattlen, bytcatt, bdytail, temp16b);
                    temp32blen = EA.FastExpansionSumZeroElim(temp16alen, temp16a, temp16blen, temp16b, temp32b);
                    temp64len = EA.FastExpansionSumZeroElim(temp32alen, temp32a, temp32blen, temp32b, temp64);
                    finlength = EA.FastExpansionSumZeroElim(finlength, finnow, temp64len, temp64, finother);
                    finswap = finnow; finnow = finother; finother = finswap;
                }
            }
            if ((cdxtail != 0.0) || (cdytail != 0.0))
            {
                if ((adxtail != 0.0) || (adytail != 0.0)
                    || (bdxtail != 0.0) || (bdytail != 0.0))
                {
                    EA.TwoProduct(adxtail, bdy, out ti1, out ti0);
                    EA.TwoProduct(adx, bdytail, out tj1, out tj0);
                    EA.TwoTwoSum(ti1, ti0, tj1, tj0, out u3, out u[2], out u[1], out u[0]);
                    u[3] = u3;
                    negate = -ady;
                    EA.TwoProduct(bdxtail, negate, out ti1, out ti0);
                    negate = -adytail;
                    EA.TwoProduct(bdx, negate, out tj1, out tj0);
                    EA.TwoTwoSum(ti1, ti0, tj1, tj0, out v3, out v[2], out v[1], out v[0]);
                    v[3] = v3;
                    abtlen = EA.FastExpansionSumZeroElim(4, u, 4, v, abt);

                    EA.TwoProduct(adxtail, bdytail, out ti1, out ti0);
                    EA.TwoProduct(bdxtail, adytail, out tj1, out tj0);
                    EA.TwoTwoDiff(ti1, ti0, tj1, tj0, out abtt3, out abtt[2], out abtt[1], out abtt[0]);
                    abtt[3] = abtt3;
                    abttlen = 4;
                }
                else
                {
                    abt[0] = 0.0;
                    abtlen = 1;
                    abtt[0] = 0.0;
                    abttlen = 1;
                }

                if (cdxtail != 0.0)
                {
                    temp16alen = EA.ScaleExpansionZeroElim(cxtablen, cxtab, cdxtail, temp16a);
                    cxtabtlen = EA.ScaleExpansionZeroElim(abtlen, abt, cdxtail, cxtabt);
                    temp32alen = EA.ScaleExpansionZeroElim(cxtabtlen, cxtabt, 2.0 * cdx, temp32a);
                    temp48len = EA.FastExpansionSumZeroElim(temp16alen, temp16a, temp32alen, temp32a, temp48);
                    finlength = EA.FastExpansionSumZeroElim(finlength, finnow, temp48len, temp48, finother);
                    finswap = finnow; finnow = finother; finother = finswap;
                    if (adytail != 0.0)
                    {
                        temp8len = EA.ScaleExpansionZeroElim(4, bb, cdxtail, temp8);
                        temp16alen = EA.ScaleExpansionZeroElim(temp8len, temp8, adytail, temp16a);
                        finlength = EA.FastExpansionSumZeroElim(finlength, finnow, temp16alen, temp16a, finother);
                        finswap = finnow; finnow = finother; finother = finswap;
                    }
                    if (bdytail != 0.0)
                    {
                        temp8len = EA.ScaleExpansionZeroElim(4, aa, -cdxtail, temp8);
                        temp16alen = EA.ScaleExpansionZeroElim(temp8len, temp8, bdytail, temp16a);
                        finlength = EA.FastExpansionSumZeroElim(finlength, finnow, temp16alen, temp16a, finother);
                        finswap = finnow; finnow = finother; finother = finswap;
                    }

                    temp32alen = EA.ScaleExpansionZeroElim(cxtabtlen, cxtabt, cdxtail, temp32a);
                    cxtabttlen = EA.ScaleExpansionZeroElim(abttlen, abtt, cdxtail, cxtabtt);
                    temp16alen = EA.ScaleExpansionZeroElim(cxtabttlen, cxtabtt, 2.0 * cdx, temp16a);
                    temp16blen = EA.ScaleExpansionZeroElim(cxtabttlen, cxtabtt, cdxtail, temp16b);
                    temp32blen = EA.FastExpansionSumZeroElim(temp16alen, temp16a, temp16blen, temp16b, temp32b);
                    temp64len = EA.FastExpansionSumZeroElim(temp32alen, temp32a, temp32blen, temp32b, temp64);
                    finlength = EA.FastExpansionSumZeroElim(finlength, finnow, temp64len, temp64, finother);
                    finswap = finnow; finnow = finother; finother = finswap;
                }
                if (cdytail != 0.0)
                {
                    temp16alen = EA.ScaleExpansionZeroElim(cytablen, cytab, cdytail, temp16a);
                    cytabtlen = EA.ScaleExpansionZeroElim(abtlen, abt, cdytail, cytabt);
                    temp32alen = EA.ScaleExpansionZeroElim(cytabtlen, cytabt, 2.0 * cdy, temp32a);
                    temp48len = EA.FastExpansionSumZeroElim(temp16alen, temp16a, temp32alen, temp32a, temp48);
                    finlength = EA.FastExpansionSumZeroElim(finlength, finnow, temp48len, temp48, finother);
                    finswap = finnow; finnow = finother; finother = finswap;


                    temp32alen = EA.ScaleExpansionZeroElim(cytabtlen, cytabt, cdytail, temp32a);
                    cytabttlen = EA.ScaleExpansionZeroElim(abttlen, abtt, cdytail, cytabtt);
                    temp16alen = EA.ScaleExpansionZeroElim(cytabttlen, cytabtt, 2.0 * cdy, temp16a);
                    temp16blen = EA.ScaleExpansionZeroElim(cytabttlen, cytabtt, cdytail, temp16b);
                    temp32blen = EA.FastExpansionSumZeroElim(temp16alen, temp16a, temp16blen, temp16b, temp32b);
                    temp64len = EA.FastExpansionSumZeroElim(temp32alen, temp32a, temp32blen, temp32b, temp64);
                    finlength = EA.FastExpansionSumZeroElim(finlength, finnow, temp64len, temp64, finother);
                    finswap = finnow; finnow = finother; finother = finswap;
                }
            }

            return finnow[finlength - 1];
        }
        #endregion

        #region InSphere

        /*****************************************************************************/
        /*                                                                           */
        /*  inspherefast()   Approximate 3D insphere test.  Nonrobust.               */
        /*  insphereexact()   Exact 3D insphere test.  Robust.                       */
        /*  insphereslow()   Another exact 3D insphere test.  Robust.                */
        /*  insphere()   Adaptive exact 3D insphere test.  Robust.                   */
        /*                                                                           */
        /*               Return a positive value if the point pe lies inside the     */
        /*               sphere passing through pa, pb, pc, and pd; a negative value */
        /*               if it lies outside; and zero if the five points are         */
        /*               cospherical.  The points pa, pb, pc, and pd must be ordered */
        /*               so that they have a positive orientation (as defined by     */
        /*               orient3d()), or the sign of the result will be reversed.    */
        /*                                                                           */
        /*  Only the first and last routine should be used; the middle two are for   */
        /*  timings.                                                                 */
        /*                                                                           */
        /*  The last three use exact arithmetic to ensure a correct answer.  The     */
        /*  result returned is the determinant of a matrix.  In insphere() only,     */
        /*  this determinant is computed adaptively, in the sense that exact         */
        /*  arithmetic is used only to the degree it is needed to ensure that the    */
        /*  returned value has the correct sign.  Hence, insphere() is usually quite */
        /*  fast, but will run more slowly when the input points are cospherical or  */
        /*  nearly so.                                                               */
        /*                                                                           */
        /*****************************************************************************/

        public static double InSphereFast(double[] pa, double[] pb, double[] pc, double[] pd, double[] pe)
        {
            double aex, bex, cex, dex;
            double aey, bey, cey, dey;
            double aez, bez, cez, dez;
            double alift, blift, clift, dlift;
            double ab, bc, cd, da, ac, bd;
            double abc, bcd, cda, dab;

            aex = pa[0] - pe[0];
            bex = pb[0] - pe[0];
            cex = pc[0] - pe[0];
            dex = pd[0] - pe[0];
            aey = pa[1] - pe[1];
            bey = pb[1] - pe[1];
            cey = pc[1] - pe[1];
            dey = pd[1] - pe[1];
            aez = pa[2] - pe[2];
            bez = pb[2] - pe[2];
            cez = pc[2] - pe[2];
            dez = pd[2] - pe[2];

            ab = aex * bey - bex * aey;
            bc = bex * cey - cex * bey;
            cd = cex * dey - dex * cey;
            da = dex * aey - aex * dey;

            ac = aex * cey - cex * aey;
            bd = bex * dey - dex * bey;

            abc = aez * bc - bez * ac + cez * ab;
            bcd = bez * cd - cez * bd + dez * bc;
            cda = cez * da + dez * ac + aez * cd;
            dab = dez * ab + aez * bd + bez * da;

            alift = aex * aex + aey * aey + aez * aez;
            blift = bex * bex + bey * bey + bez * bez;
            clift = cex * cex + cey * cey + cez * cez;
            dlift = dex * dex + dey * dey + dez * dez;

            return (dlift * abc - clift * dab) + (blift * cda - alift * bcd);
        }

        internal static double InSphereExact(double[] pa, double[] pb, double[] pc, double[] pd, double[] pe)
        {
            double axby1, bxcy1, cxdy1, dxey1, exay1;
            double bxay1, cxby1, dxcy1, exdy1, axey1;
            double axcy1, bxdy1, cxey1, dxay1, exby1;
            double cxay1, dxby1, excy1, axdy1, bxey1;
            double axby0, bxcy0, cxdy0, dxey0, exay0;
            double bxay0, cxby0, dxcy0, exdy0, axey0;
            double axcy0, bxdy0, cxey0, dxay0, exby0;
            double cxay0, dxby0, excy0, axdy0, bxey0;
            double[] ab = new double[4];
            double[] bc = new double[4];
            double[] cd = new double[4];
            double[] de = new double[4];
            double[] ea = new double[4];
            double[] ac = new double[4];
            double[] bd = new double[4];
            double[] ce = new double[4];
            double[] da = new double[4];
            double[] eb = new double[4];
            double[] temp8a = new double[8];
            double[] temp8b = new double[8];
            double[] temp16 = new double[16];
            int temp8alen, temp8blen, temp16len;
            double[] abc = new double[24];
            double[] bcd = new double[24];
            double[] cde = new double[24];
            double[] dea = new double[24];
            double[] eab = new double[24];
            double[] abd = new double[24];
            double[] bce = new double[24];
            double[] cda = new double[24];
            double[] deb = new double[24];
            double[] eac = new double[24];
            int abclen, bcdlen, cdelen, dealen, eablen;
            int abdlen, bcelen, cdalen, deblen, eaclen;
            double[] temp48a = new double[48];
            double[] temp48b = new double[48];
            int temp48alen, temp48blen;
            double[] abcd = new double[96];
            double[] bcde = new double[96];
            double[] cdea = new double[96];
            double[] deab = new double[96];
            double[] eabc = new double[96];
            int abcdlen, bcdelen, cdealen, deablen, eabclen;
            double[] temp192 = new double[192];
            double[] det384x = new double[384];
            double[] det384y = new double[384];
            double[] det384z = new double[384];
            int xlen, ylen, zlen;
            double[] detxy = new double[768];
            int xylen;
            double[] adet = new double[1152];
            double[] bdet = new double[1152];
            double[] cdet = new double[1152];
            double[] ddet = new double[1152];
            double[] edet = new double[1152];
            int alen, blen, clen, dlen, elen;
            double[] abdet = new double[2304];
            double[] cddet = new double[2304];
            double[] cdedet = new double[3456];
            int ablen, cdlen;
            double[] deter = new double[5760];
            int deterlen;
            int i;

            EA.TwoProduct(pa[0], pb[1], out axby1, out axby0);
            EA.TwoProduct(pb[0], pa[1], out bxay1, out bxay0);
            EA.TwoTwoDiff(axby1, axby0, bxay1, bxay0, out ab[3], out ab[2], out ab[1], out ab[0]);

            EA.TwoProduct(pb[0], pc[1], out bxcy1, out bxcy0);
            EA.TwoProduct(pc[0], pb[1], out cxby1, out cxby0);
            EA.TwoTwoDiff(bxcy1, bxcy0, cxby1, cxby0, out bc[3], out bc[2], out bc[1], out bc[0]);

            EA.TwoProduct(pc[0], pd[1], out cxdy1, out cxdy0);
            EA.TwoProduct(pd[0], pc[1], out dxcy1, out dxcy0);
            EA.TwoTwoDiff(cxdy1, cxdy0, dxcy1, dxcy0, out cd[3], out cd[2], out cd[1], out cd[0]);

            EA.TwoProduct(pd[0], pe[1], out dxey1, out dxey0);
            EA.TwoProduct(pe[0], pd[1], out exdy1, out exdy0);
            EA.TwoTwoDiff(dxey1, dxey0, exdy1, exdy0, out de[3], out de[2], out de[1], out de[0]);

            EA.TwoProduct(pe[0], pa[1], out exay1, out exay0);
            EA.TwoProduct(pa[0], pe[1], out axey1, out axey0);
            EA.TwoTwoDiff(exay1, exay0, axey1, axey0, out ea[3], out ea[2], out ea[1], out ea[0]);

            EA.TwoProduct(pa[0], pc[1], out axcy1, out axcy0);
            EA.TwoProduct(pc[0], pa[1], out cxay1, out cxay0);
            EA.TwoTwoDiff(axcy1, axcy0, cxay1, cxay0, out ac[3], out ac[2], out ac[1], out ac[0]);

            EA.TwoProduct(pb[0], pd[1], out bxdy1, out bxdy0);
            EA.TwoProduct(pd[0], pb[1], out dxby1, out dxby0);
            EA.TwoTwoDiff(bxdy1, bxdy0, dxby1, dxby0, out bd[3], out bd[2], out bd[1], out bd[0]);

            EA.TwoProduct(pc[0], pe[1], out cxey1, out cxey0);
            EA.TwoProduct(pe[0], pc[1], out excy1, out excy0);
            EA.TwoTwoDiff(cxey1, cxey0, excy1, excy0, out ce[3], out ce[2], out ce[1], out ce[0]);

            EA.TwoProduct(pd[0], pa[1], out dxay1, out dxay0);
            EA.TwoProduct(pa[0], pd[1], out axdy1, out axdy0);
            EA.TwoTwoDiff(dxay1, dxay0, axdy1, axdy0, out da[3], out da[2], out da[1], out da[0]);

            EA.TwoProduct(pe[0], pb[1], out exby1, out exby0);
            EA.TwoProduct(pb[0], pe[1], out bxey1, out bxey0);
            EA.TwoTwoDiff(exby1, exby0, bxey1, bxey0, out eb[3], out eb[2], out eb[1], out eb[0]);

            temp8alen = EA.ScaleExpansionZeroElim(4, bc, pa[2], temp8a);
            temp8blen = EA.ScaleExpansionZeroElim(4, ac, -pb[2], temp8b);
            temp16len = EA.FastExpansionSumZeroElim(temp8alen, temp8a, temp8blen, temp8b, temp16);
            temp8alen = EA.ScaleExpansionZeroElim(4, ab, pc[2], temp8a);
            abclen = EA.FastExpansionSumZeroElim(temp8alen, temp8a, temp16len, temp16, abc);

            temp8alen = EA.ScaleExpansionZeroElim(4, cd, pb[2], temp8a);
            temp8blen = EA.ScaleExpansionZeroElim(4, bd, -pc[2], temp8b);
            temp16len = EA.FastExpansionSumZeroElim(temp8alen, temp8a, temp8blen, temp8b, temp16);
            temp8alen = EA.ScaleExpansionZeroElim(4, bc, pd[2], temp8a);
            bcdlen = EA.FastExpansionSumZeroElim(temp8alen, temp8a, temp16len, temp16, bcd);

            temp8alen = EA.ScaleExpansionZeroElim(4, de, pc[2], temp8a);
            temp8blen = EA.ScaleExpansionZeroElim(4, ce, -pd[2], temp8b);
            temp16len = EA.FastExpansionSumZeroElim(temp8alen, temp8a, temp8blen, temp8b, temp16);
            temp8alen = EA.ScaleExpansionZeroElim(4, cd, pe[2], temp8a);
            cdelen = EA.FastExpansionSumZeroElim(temp8alen, temp8a, temp16len, temp16, cde);

            temp8alen = EA.ScaleExpansionZeroElim(4, ea, pd[2], temp8a);
            temp8blen = EA.ScaleExpansionZeroElim(4, da, -pe[2], temp8b);
            temp16len = EA.FastExpansionSumZeroElim(temp8alen, temp8a, temp8blen, temp8b, temp16);
            temp8alen = EA.ScaleExpansionZeroElim(4, de, pa[2], temp8a);
            dealen = EA.FastExpansionSumZeroElim(temp8alen, temp8a, temp16len, temp16, dea);

            temp8alen = EA.ScaleExpansionZeroElim(4, ab, pe[2], temp8a);
            temp8blen = EA.ScaleExpansionZeroElim(4, eb, -pa[2], temp8b);
            temp16len = EA.FastExpansionSumZeroElim(temp8alen, temp8a, temp8blen, temp8b, temp16);
            temp8alen = EA.ScaleExpansionZeroElim(4, ea, pb[2], temp8a);
            eablen = EA.FastExpansionSumZeroElim(temp8alen, temp8a, temp16len, temp16, eab);

            temp8alen = EA.ScaleExpansionZeroElim(4, bd, pa[2], temp8a);
            temp8blen = EA.ScaleExpansionZeroElim(4, da, pb[2], temp8b);
            temp16len = EA.FastExpansionSumZeroElim(temp8alen, temp8a, temp8blen, temp8b, temp16);
            temp8alen = EA.ScaleExpansionZeroElim(4, ab, pd[2], temp8a);
            abdlen = EA.FastExpansionSumZeroElim(temp8alen, temp8a, temp16len, temp16, abd);

            temp8alen = EA.ScaleExpansionZeroElim(4, ce, pb[2], temp8a);
            temp8blen = EA.ScaleExpansionZeroElim(4, eb, pc[2], temp8b);
            temp16len = EA.FastExpansionSumZeroElim(temp8alen, temp8a, temp8blen, temp8b, temp16);
            temp8alen = EA.ScaleExpansionZeroElim(4, bc, pe[2], temp8a);
            bcelen = EA.FastExpansionSumZeroElim(temp8alen, temp8a, temp16len, temp16, bce);

            temp8alen = EA.ScaleExpansionZeroElim(4, da, pc[2], temp8a);
            temp8blen = EA.ScaleExpansionZeroElim(4, ac, pd[2], temp8b);
            temp16len = EA.FastExpansionSumZeroElim(temp8alen, temp8a, temp8blen, temp8b, temp16);
            temp8alen = EA.ScaleExpansionZeroElim(4, cd, pa[2], temp8a);
            cdalen = EA.FastExpansionSumZeroElim(temp8alen, temp8a, temp16len, temp16, cda);

            temp8alen = EA.ScaleExpansionZeroElim(4, eb, pd[2], temp8a);
            temp8blen = EA.ScaleExpansionZeroElim(4, bd, pe[2], temp8b);
            temp16len = EA.FastExpansionSumZeroElim(temp8alen, temp8a, temp8blen, temp8b, temp16);
            temp8alen = EA.ScaleExpansionZeroElim(4, de, pb[2], temp8a);
            deblen = EA.FastExpansionSumZeroElim(temp8alen, temp8a, temp16len, temp16, deb);

            temp8alen = EA.ScaleExpansionZeroElim(4, ac, pe[2], temp8a);
            temp8blen = EA.ScaleExpansionZeroElim(4, ce, pa[2], temp8b);
            temp16len = EA.FastExpansionSumZeroElim(temp8alen, temp8a, temp8blen, temp8b, temp16);
            temp8alen = EA.ScaleExpansionZeroElim(4, ea, pc[2], temp8a);
            eaclen = EA.FastExpansionSumZeroElim(temp8alen, temp8a, temp16len, temp16, eac);

            temp48alen = EA.FastExpansionSumZeroElim(cdelen, cde, bcelen, bce, temp48a);
            temp48blen = EA.FastExpansionSumZeroElim(deblen, deb, bcdlen, bcd, temp48b);
            for (i = 0; i < temp48blen; i++) 
            {
                temp48b[i] = -temp48b[i];
            }
            bcdelen = EA.FastExpansionSumZeroElim(temp48alen, temp48a, temp48blen, temp48b, bcde);
            xlen = EA.ScaleExpansionZeroElim(bcdelen, bcde, pa[0], temp192);
            xlen = EA.ScaleExpansionZeroElim(xlen, temp192, pa[0], det384x);
            ylen = EA.ScaleExpansionZeroElim(bcdelen, bcde, pa[1], temp192);
            ylen = EA.ScaleExpansionZeroElim(ylen, temp192, pa[1], det384y);
            zlen = EA.ScaleExpansionZeroElim(bcdelen, bcde, pa[2], temp192);
            zlen = EA.ScaleExpansionZeroElim(zlen, temp192, pa[2], det384z);
            xylen = EA.FastExpansionSumZeroElim(xlen, det384x, ylen, det384y, detxy);
            alen = EA.FastExpansionSumZeroElim(xylen, detxy, zlen, det384z, adet);

            temp48alen = EA.FastExpansionSumZeroElim(dealen, dea, cdalen, cda, temp48a);
            temp48blen = EA.FastExpansionSumZeroElim(eaclen, eac, cdelen, cde, temp48b);
            for (i = 0; i < temp48blen; i++) 
            {
                temp48b[i] = -temp48b[i];
            }
            cdealen = EA.FastExpansionSumZeroElim(temp48alen, temp48a, temp48blen, temp48b, cdea);
            xlen = EA.ScaleExpansionZeroElim(cdealen, cdea, pb[0], temp192);
            xlen = EA.ScaleExpansionZeroElim(xlen, temp192, pb[0], det384x);
            ylen = EA.ScaleExpansionZeroElim(cdealen, cdea, pb[1], temp192);
            ylen = EA.ScaleExpansionZeroElim(ylen, temp192, pb[1], det384y);
            zlen = EA.ScaleExpansionZeroElim(cdealen, cdea, pb[2], temp192);
            zlen = EA.ScaleExpansionZeroElim(zlen, temp192, pb[2], det384z);
            xylen = EA.FastExpansionSumZeroElim(xlen, det384x, ylen, det384y, detxy);
            blen = EA.FastExpansionSumZeroElim(xylen, detxy, zlen, det384z, bdet);

            temp48alen = EA.FastExpansionSumZeroElim(eablen, eab, deblen, deb, temp48a);
            temp48blen = EA.FastExpansionSumZeroElim(abdlen, abd, dealen, dea, temp48b);
            for (i = 0; i < temp48blen; i++) 
            {
                temp48b[i] = -temp48b[i];
            }
            deablen = EA.FastExpansionSumZeroElim(temp48alen, temp48a, temp48blen, temp48b, deab);
            xlen = EA.ScaleExpansionZeroElim(deablen, deab, pc[0], temp192);
            xlen = EA.ScaleExpansionZeroElim(xlen, temp192, pc[0], det384x);
            ylen = EA.ScaleExpansionZeroElim(deablen, deab, pc[1], temp192);
            ylen = EA.ScaleExpansionZeroElim(ylen, temp192, pc[1], det384y);
            zlen = EA.ScaleExpansionZeroElim(deablen, deab, pc[2], temp192);
            zlen = EA.ScaleExpansionZeroElim(zlen, temp192, pc[2], det384z);
            xylen = EA.FastExpansionSumZeroElim(xlen, det384x, ylen, det384y, detxy);
            clen = EA.FastExpansionSumZeroElim(xylen, detxy, zlen, det384z, cdet);

            temp48alen = EA.FastExpansionSumZeroElim(abclen, abc, eaclen, eac, temp48a);
            temp48blen = EA.FastExpansionSumZeroElim(bcelen, bce, eablen, eab, temp48b);
            for (i = 0; i < temp48blen; i++) 
            {
                temp48b[i] = -temp48b[i];
            }
            eabclen = EA.FastExpansionSumZeroElim(temp48alen, temp48a, temp48blen, temp48b, eabc);
            xlen = EA.ScaleExpansionZeroElim(eabclen, eabc, pd[0], temp192);
            xlen = EA.ScaleExpansionZeroElim(xlen, temp192, pd[0], det384x);
            ylen = EA.ScaleExpansionZeroElim(eabclen, eabc, pd[1], temp192);
            ylen = EA.ScaleExpansionZeroElim(ylen, temp192, pd[1], det384y);
            zlen = EA.ScaleExpansionZeroElim(eabclen, eabc, pd[2], temp192);
            zlen = EA.ScaleExpansionZeroElim(zlen, temp192, pd[2], det384z);
            xylen = EA.FastExpansionSumZeroElim(xlen, det384x, ylen, det384y, detxy);
            dlen = EA.FastExpansionSumZeroElim(xylen, detxy, zlen, det384z, ddet);

            temp48alen = EA.FastExpansionSumZeroElim(bcdlen, bcd, abdlen, abd, temp48a);
            temp48blen = EA.FastExpansionSumZeroElim(cdalen, cda, abclen, abc, temp48b);
            for (i = 0; i < temp48blen; i++) 
            {
                temp48b[i] = -temp48b[i];
            }
            abcdlen = EA.FastExpansionSumZeroElim(temp48alen, temp48a, temp48blen, temp48b, abcd);
            xlen = EA.ScaleExpansionZeroElim(abcdlen, abcd, pe[0], temp192);
            xlen = EA.ScaleExpansionZeroElim(xlen, temp192, pe[0], det384x);
            ylen = EA.ScaleExpansionZeroElim(abcdlen, abcd, pe[1], temp192);
            ylen = EA.ScaleExpansionZeroElim(ylen, temp192, pe[1], det384y);
            zlen = EA.ScaleExpansionZeroElim(abcdlen, abcd, pe[2], temp192);
            zlen = EA.ScaleExpansionZeroElim(zlen, temp192, pe[2], det384z);
            xylen = EA.FastExpansionSumZeroElim(xlen, det384x, ylen, det384y, detxy);
            elen = EA.FastExpansionSumZeroElim(xylen, detxy, zlen, det384z, edet);

            ablen = EA.FastExpansionSumZeroElim(alen, adet, blen, bdet, abdet);
            cdlen = EA.FastExpansionSumZeroElim(clen, cdet, dlen, ddet, cddet);
            cdelen = EA.FastExpansionSumZeroElim(cdlen, cddet, elen, edet, cdedet);
            deterlen = EA.FastExpansionSumZeroElim(ablen, abdet, cdelen, cdedet, deter);

            // In S. predicates.c, this returns the largest component: 
            // deter[deterlen - 1];
            // However, this is not stable due to the expansions not being unique (even for ZeroElim),
            // So we return the summed estimate as the 'Exact' value.
            return EA.Estimate(deterlen, deter);
        }

        internal static double InSphereSlow(double[] pa, double[] pb, double[] pc, double[] pd, double[] pe)
        {
            double aex, bex, cex, dex, aey, bey, cey, dey, aez, bez, cez, dez;
            double aextail, bextail, cextail, dextail;
            double aeytail, beytail, ceytail, deytail;
            double aeztail, beztail, ceztail, deztail;
            double negate, negatetail;
            double axby7, bxcy7, cxdy7, dxay7, axcy7, bxdy7;
            double bxay7, cxby7, dxcy7, axdy7, cxay7, dxby7;
            double[] axby = new double[8];
            double[] bxcy = new double[8];
            double[] cxdy = new double[8];
            double[] dxay = new double[8];
            double[] axcy = new double[8];
            double[] bxdy = new double[8];
            double[] bxay = new double[8];
            double[] cxby = new double[8];
            double[] dxcy = new double[8];
            double[] axdy = new double[8];
            double[] cxay = new double[8];
            double[] dxby = new double[8];
            double[] ab = new double[16];
            double[] bc = new double[16];
            double[] cd = new double[16];
            double[] da = new double[16];
            double[] ac = new double[16];
            double[] bd = new double[16];
            int ablen, bclen, cdlen, dalen, aclen, bdlen;
            double[] temp32a = new double[32];
            double[] temp32b = new double[32];
            double[] temp64a = new double[64];
            double[] temp64b = new double[64];
            double[] temp64c = new double[64];
            int temp32alen, temp32blen, temp64alen, temp64blen, temp64clen;
            double[] temp128 = new double[128];
            double[] temp192 = new double[192];
            int temp128len, temp192len;
            double[] detx = new double[384];
            double[] detxx = new double[768];
            double[] detxt = new double[384];
            double[] detxxt = new double[768];
            double[] detxtxt = new double[768];
            int xlen, xxlen, xtlen, xxtlen, xtxtlen;
            double[] x1 = new double[1536];
            double[] x2 = new double[2304];
            int x1len, x2len;
            double[] dety = new double[384];
            double[] detyy = new double[768];
            double[] detyt = new double[384];
            double[] detyyt = new double[768];
            double[] detytyt = new double[768];
            int ylen, yylen, ytlen, yytlen, ytytlen;
            double[] y1 = new double[1536];
            double[] y2 = new double[2304];
            int y1len, y2len;
            double[] detz = new double[384];
            double[] detzz = new double[768];
            double[] detzt = new double[384];
            double[] detzzt = new double[768];
            double[] detztzt = new double[768];
            int zlen, zzlen, ztlen, zztlen, ztztlen;
            double[] z1 = new double[1536];
            double[] z2 = new double[2304];
            int z1len, z2len;
            double[] detxy = new double[4608];
            int xylen;
            double[] adet = new double[6912];
            double[] bdet = new double[6912];
            double[] cdet = new double[6912];
            double[] ddet = new double[6912];
            int alen, blen, clen, dlen;
            double[] abdet = new double[13824];
            double[] cddet = new double[13824];
            double[] deter = new double[27648];
            int deterlen;
            int i;

            EA.TwoDiff(pa[0], pe[0], out aex, out aextail);
            EA.TwoDiff(pa[1], pe[1], out aey, out aeytail);
            EA.TwoDiff(pa[2], pe[2], out aez, out aeztail);
            EA.TwoDiff(pb[0], pe[0], out bex, out bextail);
            EA.TwoDiff(pb[1], pe[1], out bey, out beytail);
            EA.TwoDiff(pb[2], pe[2], out bez, out beztail);
            EA.TwoDiff(pc[0], pe[0], out cex, out cextail);
            EA.TwoDiff(pc[1], pe[1], out cey, out ceytail);
            EA.TwoDiff(pc[2], pe[2], out cez, out ceztail);
            EA.TwoDiff(pd[0], pe[0], out dex, out dextail);
            EA.TwoDiff(pd[1], pe[1], out dey, out deytail);
            EA.TwoDiff(pd[2], pe[2], out dez, out deztail);

            EA.TwoTwoProduct(aex, aextail, bey, beytail,
                            out axby7, out axby[6], out axby[5], out axby[4],
                            out axby[3], out axby[2], out axby[1], out axby[0]);
            axby[7] = axby7;
            negate = -aey;
            negatetail = -aeytail;
            EA.TwoTwoProduct(bex, bextail, negate, negatetail,
                            out bxay7, out bxay[6], out bxay[5], out bxay[4],
                            out bxay[3], out bxay[2], out bxay[1], out bxay[0]);
            bxay[7] = bxay7;
            ablen = EA.FastExpansionSumZeroElim(8, axby, 8, bxay, ab);
            EA.TwoTwoProduct(bex, bextail, cey, ceytail,
                            out bxcy7, out bxcy[6], out bxcy[5], out bxcy[4],
                            out bxcy[3], out bxcy[2], out bxcy[1], out bxcy[0]);
            bxcy[7] = bxcy7;
            negate = -bey;
            negatetail = -beytail;
            EA.TwoTwoProduct(cex, cextail, negate, negatetail,
                            out cxby7, out cxby[6], out cxby[5], out cxby[4],
                            out cxby[3], out cxby[2], out cxby[1], out cxby[0]);
            cxby[7] = cxby7;
            bclen = EA.FastExpansionSumZeroElim(8, bxcy, 8, cxby, bc);
            EA.TwoTwoProduct(cex, cextail, dey, deytail,
                            out cxdy7, out cxdy[6], out cxdy[5], out cxdy[4],
                            out cxdy[3], out cxdy[2], out cxdy[1], out cxdy[0]);
            cxdy[7] = cxdy7;
            negate = -cey;
            negatetail = -ceytail;
            EA.TwoTwoProduct(dex, dextail, negate, negatetail,
                            out dxcy7, out dxcy[6], out dxcy[5], out dxcy[4],
                            out dxcy[3], out dxcy[2], out dxcy[1], out dxcy[0]);
            dxcy[7] = dxcy7;
            cdlen = EA.FastExpansionSumZeroElim(8, cxdy, 8, dxcy, cd);
            EA.TwoTwoProduct(dex, dextail, aey, aeytail,
                            out dxay7, out dxay[6], out dxay[5], out dxay[4],
                            out dxay[3], out dxay[2], out dxay[1], out dxay[0]);
            dxay[7] = dxay7;
            negate = -dey;
            negatetail = -deytail;
            EA.TwoTwoProduct(aex, aextail, negate, negatetail,
                            out axdy7, out axdy[6], out axdy[5], out axdy[4],
                            out axdy[3], out axdy[2], out axdy[1], out axdy[0]);
            axdy[7] = axdy7;
            dalen = EA.FastExpansionSumZeroElim(8, dxay, 8, axdy, da);
            EA.TwoTwoProduct(aex, aextail, cey, ceytail,
                            out axcy7, out axcy[6], out axcy[5], out axcy[4],
                            out axcy[3], out axcy[2], out axcy[1], out axcy[0]);
            axcy[7] = axcy7;
            negate = -aey;
            negatetail = -aeytail;
            EA.TwoTwoProduct(cex, cextail, negate, negatetail,
                            out cxay7, out cxay[6], out cxay[5], out cxay[4],
                            out cxay[3], out cxay[2], out cxay[1], out cxay[0]);
            cxay[7] = cxay7;
            aclen = EA.FastExpansionSumZeroElim(8, axcy, 8, cxay, ac);
            EA.TwoTwoProduct(bex, bextail, dey, deytail,
                            out bxdy7, out bxdy[6], out bxdy[5], out bxdy[4],
                            out bxdy[3], out bxdy[2], out bxdy[1], out bxdy[0]);
            bxdy[7] = bxdy7;
            negate = -bey;
            negatetail = -beytail;
            EA.TwoTwoProduct(dex, dextail, negate, negatetail,
                            out dxby7, out  dxby[6], out dxby[5], out dxby[4],
                            out dxby[3], out dxby[2], out dxby[1], out dxby[0]);
            dxby[7] = dxby7;
            bdlen = EA.FastExpansionSumZeroElim(8, bxdy, 8, dxby, bd);

            temp32alen = EA.ScaleExpansionZeroElim(cdlen, cd, -bez, temp32a);
            temp32blen = EA.ScaleExpansionZeroElim(cdlen, cd, -beztail, temp32b);
            temp64alen = EA.FastExpansionSumZeroElim(temp32alen, temp32a, temp32blen, temp32b, temp64a);
            temp32alen = EA.ScaleExpansionZeroElim(bdlen, bd, cez, temp32a);
            temp32blen = EA.ScaleExpansionZeroElim(bdlen, bd, ceztail, temp32b);
            temp64blen = EA.FastExpansionSumZeroElim(temp32alen, temp32a, temp32blen, temp32b, temp64b);
            temp32alen = EA.ScaleExpansionZeroElim(bclen, bc, -dez, temp32a);
            temp32blen = EA.ScaleExpansionZeroElim(bclen, bc, -deztail, temp32b);
            temp64clen = EA.FastExpansionSumZeroElim(temp32alen, temp32a, temp32blen, temp32b, temp64c);
            temp128len = EA.FastExpansionSumZeroElim(temp64alen, temp64a, temp64blen, temp64b, temp128);
            temp192len = EA.FastExpansionSumZeroElim(temp64clen, temp64c, temp128len, temp128, temp192);
            xlen = EA.ScaleExpansionZeroElim(temp192len, temp192, aex, detx);
            xxlen = EA.ScaleExpansionZeroElim(xlen, detx, aex, detxx);
            xtlen = EA.ScaleExpansionZeroElim(temp192len, temp192, aextail, detxt);
            xxtlen = EA.ScaleExpansionZeroElim(xtlen, detxt, aex, detxxt);
            for (i = 0; i < xxtlen; i++) 
            {
                detxxt[i] *= 2.0;
            }
            xtxtlen = EA.ScaleExpansionZeroElim(xtlen, detxt, aextail, detxtxt);
            x1len = EA.FastExpansionSumZeroElim(xxlen, detxx, xxtlen, detxxt, x1);
            x2len = EA.FastExpansionSumZeroElim(x1len, x1, xtxtlen, detxtxt, x2);
            ylen = EA.ScaleExpansionZeroElim(temp192len, temp192, aey, dety);
            yylen = EA.ScaleExpansionZeroElim(ylen, dety, aey, detyy);
            ytlen = EA.ScaleExpansionZeroElim(temp192len, temp192, aeytail, detyt);
            yytlen = EA.ScaleExpansionZeroElim(ytlen, detyt, aey, detyyt);
            for (i = 0; i < yytlen; i++) 
            {
                detyyt[i] *= 2.0;
            }
            ytytlen = EA.ScaleExpansionZeroElim(ytlen, detyt, aeytail, detytyt);
            y1len = EA.FastExpansionSumZeroElim(yylen, detyy, yytlen, detyyt, y1);
            y2len = EA.FastExpansionSumZeroElim(y1len, y1, ytytlen, detytyt, y2);
            zlen = EA.ScaleExpansionZeroElim(temp192len, temp192, aez, detz);
            zzlen = EA.ScaleExpansionZeroElim(zlen, detz, aez, detzz);
            ztlen = EA.ScaleExpansionZeroElim(temp192len, temp192, aeztail, detzt);
            zztlen = EA.ScaleExpansionZeroElim(ztlen, detzt, aez, detzzt);
            for (i = 0; i < zztlen; i++) 
            {
                detzzt[i] *= 2.0;
            }
            ztztlen = EA.ScaleExpansionZeroElim(ztlen, detzt, aeztail, detztzt);
            z1len = EA.FastExpansionSumZeroElim(zzlen, detzz, zztlen, detzzt, z1);
            z2len = EA.FastExpansionSumZeroElim(z1len, z1, ztztlen, detztzt, z2);
            xylen = EA.FastExpansionSumZeroElim(x2len, x2, y2len, y2, detxy);
            alen = EA.FastExpansionSumZeroElim(z2len, z2, xylen, detxy, adet);

            temp32alen = EA.ScaleExpansionZeroElim(dalen, da, cez, temp32a);
            temp32blen = EA.ScaleExpansionZeroElim(dalen, da, ceztail, temp32b);
            temp64alen = EA.FastExpansionSumZeroElim(temp32alen, temp32a, temp32blen, temp32b, temp64a);
            temp32alen = EA.ScaleExpansionZeroElim(aclen, ac, dez, temp32a);
            temp32blen = EA.ScaleExpansionZeroElim(aclen, ac, deztail, temp32b);
            temp64blen = EA.FastExpansionSumZeroElim(temp32alen, temp32a, temp32blen, temp32b, temp64b);
            temp32alen = EA.ScaleExpansionZeroElim(cdlen, cd, aez, temp32a);
            temp32blen = EA.ScaleExpansionZeroElim(cdlen, cd, aeztail, temp32b);
            temp64clen = EA.FastExpansionSumZeroElim(temp32alen, temp32a, temp32blen, temp32b, temp64c);
            temp128len = EA.FastExpansionSumZeroElim(temp64alen, temp64a, temp64blen, temp64b, temp128);
            temp192len = EA.FastExpansionSumZeroElim(temp64clen, temp64c, temp128len, temp128, temp192);
            xlen = EA.ScaleExpansionZeroElim(temp192len, temp192, bex, detx);
            xxlen = EA.ScaleExpansionZeroElim(xlen, detx, bex, detxx);
            xtlen = EA.ScaleExpansionZeroElim(temp192len, temp192, bextail, detxt);
            xxtlen = EA.ScaleExpansionZeroElim(xtlen, detxt, bex, detxxt);
            for (i = 0; i < xxtlen; i++) 
            {
                detxxt[i] *= 2.0;
            }
            xtxtlen = EA.ScaleExpansionZeroElim(xtlen, detxt, bextail, detxtxt);
            x1len = EA.FastExpansionSumZeroElim(xxlen, detxx, xxtlen, detxxt, x1);
            x2len = EA.FastExpansionSumZeroElim(x1len, x1, xtxtlen, detxtxt, x2);
            ylen = EA.ScaleExpansionZeroElim(temp192len, temp192, bey, dety);
            yylen = EA.ScaleExpansionZeroElim(ylen, dety, bey, detyy);
            ytlen = EA.ScaleExpansionZeroElim(temp192len, temp192, beytail, detyt);
            yytlen = EA.ScaleExpansionZeroElim(ytlen, detyt, bey, detyyt);
            for (i = 0; i < yytlen; i++) 
            {
                detyyt[i] *= 2.0;
            }
            ytytlen = EA.ScaleExpansionZeroElim(ytlen, detyt, beytail, detytyt);
            y1len = EA.FastExpansionSumZeroElim(yylen, detyy, yytlen, detyyt, y1);
            y2len = EA.FastExpansionSumZeroElim(y1len, y1, ytytlen, detytyt, y2);
            zlen = EA.ScaleExpansionZeroElim(temp192len, temp192, bez, detz);
            zzlen = EA.ScaleExpansionZeroElim(zlen, detz, bez, detzz);
            ztlen = EA.ScaleExpansionZeroElim(temp192len, temp192, beztail, detzt);
            zztlen = EA.ScaleExpansionZeroElim(ztlen, detzt, bez, detzzt);
            for (i = 0; i < zztlen; i++) 
            {
                detzzt[i] *= 2.0;
            }
            ztztlen = EA.ScaleExpansionZeroElim(ztlen, detzt, beztail, detztzt);
            z1len = EA.FastExpansionSumZeroElim(zzlen, detzz, zztlen, detzzt, z1);
            z2len = EA.FastExpansionSumZeroElim(z1len, z1, ztztlen, detztzt, z2);
            xylen = EA.FastExpansionSumZeroElim(x2len, x2, y2len, y2, detxy);
            blen = EA.FastExpansionSumZeroElim(z2len, z2, xylen, detxy, bdet);

            temp32alen = EA.ScaleExpansionZeroElim(ablen, ab, -dez, temp32a);
            temp32blen = EA.ScaleExpansionZeroElim(ablen, ab, -deztail, temp32b);
            temp64alen = EA.FastExpansionSumZeroElim(temp32alen, temp32a, temp32blen, temp32b, temp64a);
            temp32alen = EA.ScaleExpansionZeroElim(bdlen, bd, -aez, temp32a);
            temp32blen = EA.ScaleExpansionZeroElim(bdlen, bd, -aeztail, temp32b);
            temp64blen = EA.FastExpansionSumZeroElim(temp32alen, temp32a, temp32blen, temp32b, temp64b);
            temp32alen = EA.ScaleExpansionZeroElim(dalen, da, -bez, temp32a);
            temp32blen = EA.ScaleExpansionZeroElim(dalen, da, -beztail, temp32b);
            temp64clen = EA.FastExpansionSumZeroElim(temp32alen, temp32a, temp32blen, temp32b, temp64c);
            temp128len = EA.FastExpansionSumZeroElim(temp64alen, temp64a, temp64blen, temp64b, temp128);
            temp192len = EA.FastExpansionSumZeroElim(temp64clen, temp64c, temp128len, temp128, temp192);
            xlen = EA.ScaleExpansionZeroElim(temp192len, temp192, cex, detx);
            xxlen = EA.ScaleExpansionZeroElim(xlen, detx, cex, detxx);
            xtlen = EA.ScaleExpansionZeroElim(temp192len, temp192, cextail, detxt);
            xxtlen = EA.ScaleExpansionZeroElim(xtlen, detxt, cex, detxxt);
            for (i = 0; i < xxtlen; i++) 
            {
                detxxt[i] *= 2.0;
            }
            xtxtlen = EA.ScaleExpansionZeroElim(xtlen, detxt, cextail, detxtxt);
            x1len = EA.FastExpansionSumZeroElim(xxlen, detxx, xxtlen, detxxt, x1);
            x2len = EA.FastExpansionSumZeroElim(x1len, x1, xtxtlen, detxtxt, x2);
            ylen = EA.ScaleExpansionZeroElim(temp192len, temp192, cey, dety);
            yylen = EA.ScaleExpansionZeroElim(ylen, dety, cey, detyy);
            ytlen = EA.ScaleExpansionZeroElim(temp192len, temp192, ceytail, detyt);
            yytlen = EA.ScaleExpansionZeroElim(ytlen, detyt, cey, detyyt);
            for (i = 0; i < yytlen; i++) 
            {
                detyyt[i] *= 2.0;
            }
            ytytlen = EA.ScaleExpansionZeroElim(ytlen, detyt, ceytail, detytyt);
            y1len = EA.FastExpansionSumZeroElim(yylen, detyy, yytlen, detyyt, y1);
            y2len = EA.FastExpansionSumZeroElim(y1len, y1, ytytlen, detytyt, y2);
            zlen = EA.ScaleExpansionZeroElim(temp192len, temp192, cez, detz);
            zzlen = EA.ScaleExpansionZeroElim(zlen, detz, cez, detzz);
            ztlen = EA.ScaleExpansionZeroElim(temp192len, temp192, ceztail, detzt);
            zztlen = EA.ScaleExpansionZeroElim(ztlen, detzt, cez, detzzt);
            for (i = 0; i < zztlen; i++) 
            {
                detzzt[i] *= 2.0;
            }
            ztztlen = EA.ScaleExpansionZeroElim(ztlen, detzt, ceztail, detztzt);
            z1len = EA.FastExpansionSumZeroElim(zzlen, detzz, zztlen, detzzt, z1);
            z2len = EA.FastExpansionSumZeroElim(z1len, z1, ztztlen, detztzt, z2);
            xylen = EA.FastExpansionSumZeroElim(x2len, x2, y2len, y2, detxy);
            clen = EA.FastExpansionSumZeroElim(z2len, z2, xylen, detxy, cdet);

            temp32alen = EA.ScaleExpansionZeroElim(bclen, bc, aez, temp32a);
            temp32blen = EA.ScaleExpansionZeroElim(bclen, bc, aeztail, temp32b);
            temp64alen = EA.FastExpansionSumZeroElim(temp32alen, temp32a, temp32blen, temp32b, temp64a);
            temp32alen = EA.ScaleExpansionZeroElim(aclen, ac, -bez, temp32a);
            temp32blen = EA.ScaleExpansionZeroElim(aclen, ac, -beztail, temp32b);
            temp64blen = EA.FastExpansionSumZeroElim(temp32alen, temp32a, temp32blen, temp32b, temp64b);
            temp32alen = EA.ScaleExpansionZeroElim(ablen, ab, cez, temp32a);
            temp32blen = EA.ScaleExpansionZeroElim(ablen, ab, ceztail, temp32b);
            temp64clen = EA.FastExpansionSumZeroElim(temp32alen, temp32a, temp32blen, temp32b, temp64c);
            temp128len = EA.FastExpansionSumZeroElim(temp64alen, temp64a, temp64blen, temp64b, temp128);
            temp192len = EA.FastExpansionSumZeroElim(temp64clen, temp64c, temp128len, temp128, temp192);
            xlen = EA.ScaleExpansionZeroElim(temp192len, temp192, dex, detx);
            xxlen = EA.ScaleExpansionZeroElim(xlen, detx, dex, detxx);
            xtlen = EA.ScaleExpansionZeroElim(temp192len, temp192, dextail, detxt);
            xxtlen = EA.ScaleExpansionZeroElim(xtlen, detxt, dex, detxxt);
            for (i = 0; i < xxtlen; i++)
            {
                detxxt[i] *= 2.0;
            }
            xtxtlen = EA.ScaleExpansionZeroElim(xtlen, detxt, dextail, detxtxt);
            x1len = EA.FastExpansionSumZeroElim(xxlen, detxx, xxtlen, detxxt, x1);
            x2len = EA.FastExpansionSumZeroElim(x1len, x1, xtxtlen, detxtxt, x2);
            ylen = EA.ScaleExpansionZeroElim(temp192len, temp192, dey, dety);
            yylen = EA.ScaleExpansionZeroElim(ylen, dety, dey, detyy);
            ytlen = EA.ScaleExpansionZeroElim(temp192len, temp192, deytail, detyt);
            yytlen = EA.ScaleExpansionZeroElim(ytlen, detyt, dey, detyyt);
            for (i = 0; i < yytlen; i++) 
            {
                detyyt[i] *= 2.0;
            }
            ytytlen = EA.ScaleExpansionZeroElim(ytlen, detyt, deytail, detytyt);
            y1len = EA.FastExpansionSumZeroElim(yylen, detyy, yytlen, detyyt, y1);
            y2len = EA.FastExpansionSumZeroElim(y1len, y1, ytytlen, detytyt, y2);
            zlen = EA.ScaleExpansionZeroElim(temp192len, temp192, dez, detz);
            zzlen = EA.ScaleExpansionZeroElim(zlen, detz, dez, detzz);
            ztlen = EA.ScaleExpansionZeroElim(temp192len, temp192, deztail, detzt);
            zztlen = EA.ScaleExpansionZeroElim(ztlen, detzt, dez, detzzt);
            for (i = 0; i < zztlen; i++) 
            {
                detzzt[i] *= 2.0;
            }
            ztztlen = EA.ScaleExpansionZeroElim(ztlen, detzt, deztail, detztzt);
            z1len = EA.FastExpansionSumZeroElim(zzlen, detzz, zztlen, detzzt, z1);
            z2len = EA.FastExpansionSumZeroElim(z1len, z1, ztztlen, detztzt, z2);
            xylen = EA.FastExpansionSumZeroElim(x2len, x2, y2len, y2, detxy);
            dlen = EA.FastExpansionSumZeroElim(z2len, z2, xylen, detxy, ddet);

            ablen = EA.FastExpansionSumZeroElim(alen, adet, blen, bdet, abdet);
            cdlen = EA.FastExpansionSumZeroElim(clen, cdet, dlen, ddet, cddet);
            deterlen = EA.FastExpansionSumZeroElim(ablen, abdet, cdlen, cddet, deter);

            // In S. predicates.c, this returns the largest component: 
            // deter[deterlen - 1];
            // However, this is not stable due to the expansions not being unique (even for ZeroElim),
            // So we return the summed estimate as the 'Exact' value.
            return EA.Estimate(deterlen, deter);
        }

        public static double InSphere(double[] pa, double[] pb, double[] pc, double[] pd, double[] pe)
        {
            double aex, bex, cex, dex;
            double aey, bey, cey, dey;
            double aez, bez, cez, dez;
            double aexbey, bexaey, bexcey, cexbey, cexdey, dexcey, dexaey, aexdey;
            double aexcey, cexaey, bexdey, dexbey;
            double alift, blift, clift, dlift;
            double ab, bc, cd, da, ac, bd;
            double abc, bcd, cda, dab;
            double aezplus, bezplus, cezplus, dezplus;
            double aexbeyplus, bexaeyplus, bexceyplus, cexbeyplus;
            double cexdeyplus, dexceyplus, dexaeyplus, aexdeyplus;
            double aexceyplus, cexaeyplus, bexdeyplus, dexbeyplus;
            double det;
            double permanent, errbound;

            aex = pa[0] - pe[0];
            bex = pb[0] - pe[0];
            cex = pc[0] - pe[0];
            dex = pd[0] - pe[0];
            aey = pa[1] - pe[1];
            bey = pb[1] - pe[1];
            cey = pc[1] - pe[1];
            dey = pd[1] - pe[1];
            aez = pa[2] - pe[2];
            bez = pb[2] - pe[2];
            cez = pc[2] - pe[2];
            dez = pd[2] - pe[2];

            aexbey = aex * bey;
            bexaey = bex * aey;
            ab = aexbey - bexaey;
            bexcey = bex * cey;
            cexbey = cex * bey;
            bc = bexcey - cexbey;
            cexdey = cex * dey;
            dexcey = dex * cey;
            cd = cexdey - dexcey;
            dexaey = dex * aey;
            aexdey = aex * dey;
            da = dexaey - aexdey;

            aexcey = aex * cey;
            cexaey = cex * aey;
            ac = aexcey - cexaey;
            bexdey = bex * dey;
            dexbey = dex * bey;
            bd = bexdey - dexbey;

            abc = aez * bc - bez * ac + cez * ab;
            bcd = bez * cd - cez * bd + dez * bc;
            cda = cez * da + dez * ac + aez * cd;
            dab = dez * ab + aez * bd + bez * da;

            alift = aex * aex + aey * aey + aez * aez;
            blift = bex * bex + bey * bey + bez * bez;
            clift = cex * cex + cey * cey + cez * cez;
            dlift = dex * dex + dey * dey + dez * dez;

            det = (dlift * abc - clift * dab) + (blift * cda - alift * bcd);

            aezplus = System.Math.Abs(aez);
            bezplus = System.Math.Abs(bez);
            cezplus = System.Math.Abs(cez);
            dezplus = System.Math.Abs(dez);
            aexbeyplus = System.Math.Abs(aexbey);
            bexaeyplus = System.Math.Abs(bexaey);
            bexceyplus = System.Math.Abs(bexcey);
            cexbeyplus = System.Math.Abs(cexbey);
            cexdeyplus = System.Math.Abs(cexdey);
            dexceyplus = System.Math.Abs(dexcey);
            dexaeyplus = System.Math.Abs(dexaey);
            aexdeyplus = System.Math.Abs(aexdey);
            aexceyplus = System.Math.Abs(aexcey);
            cexaeyplus = System.Math.Abs(cexaey);
            bexdeyplus = System.Math.Abs(bexdey);
            dexbeyplus = System.Math.Abs(dexbey);
            permanent = ((cexdeyplus + dexceyplus) * bezplus
                        + (dexbeyplus + bexdeyplus) * cezplus
                        + (bexceyplus + cexbeyplus) * dezplus)
                    * alift
                    + ((dexaeyplus + aexdeyplus) * cezplus
                        + (aexceyplus + cexaeyplus) * dezplus
                        + (cexdeyplus + dexceyplus) * aezplus)
                    * blift
                    + ((aexbeyplus + bexaeyplus) * dezplus
                        + (bexdeyplus + dexbeyplus) * aezplus
                        + (dexaeyplus + aexdeyplus) * bezplus)
                    * clift
                    + ((bexceyplus + cexbeyplus) * aezplus
                        + (cexaeyplus + aexceyplus) * bezplus
                        + (aexbeyplus + bexaeyplus) * cezplus)
                    * dlift;
            errbound = isperrboundA * permanent;
            if ((det > errbound) || (-det > errbound)) 
            {
                return det;
            }

            return InSphereAdapt(pa, pb, pc, pd, pe, permanent);
        }

        // Adaptive continuation of InSphere
        static double InSphereAdapt(double[] pa, double[] pb, double[] pc, double[] pd, double[] pe, double permanent)
        {
            double aex, bex, cex, dex, aey, bey, cey, dey, aez, bez, cez, dez;
            double det, errbound;

            double aexbey1, bexaey1, bexcey1, cexbey1;
            double cexdey1, dexcey1, dexaey1, aexdey1;
            double aexcey1, cexaey1, bexdey1, dexbey1;
            double aexbey0, bexaey0, bexcey0, cexbey0;
            double cexdey0, dexcey0, dexaey0, aexdey0;
            double aexcey0, cexaey0, bexdey0, dexbey0;
            double[] ab = new double[4];
            double[] bc = new double[4];
            double[] cd = new double[4];
            double[] da = new double[4];
            double[] ac = new double[4];
            double[] bd = new double[4];
            double ab3, bc3, cd3, da3, ac3, bd3;
            double abeps, bceps, cdeps, daeps, aceps, bdeps;
            double[] temp8a = new double[8];
            double[] temp8b = new double[8];
            double[] temp8c = new double[8];
            double[] temp16 = new double[16];
            double[] temp24 = new double[24];
            double[] temp48 = new double[48];
            int temp8alen, temp8blen, temp8clen, temp16len, temp24len, temp48len;
            double[] xdet = new double[96];
            double[] ydet = new double[96];
            double[] zdet = new double[96];
            double[] xydet = new double[192];
            int xlen, ylen, zlen, xylen;
            double[] adet = new double[288];
            double[] bdet = new double[288];
            double[] cdet = new double[288];
            double[] ddet = new double[288];
            int alen, blen, clen, dlen;
            double[] abdet = new double[576];
            double[] cddet = new double[576];
            int ablen, cdlen;
            double[] fin1 = new double[1152];
            int finlength;

            double aextail, bextail, cextail, dextail;
            double aeytail, beytail, ceytail, deytail;
            double aeztail, beztail, ceztail, deztail;

            aex = pa[0] - pe[0];
            bex = pb[0] - pe[0];
            cex = pc[0] - pe[0];
            dex = pd[0] - pe[0];
            aey = pa[1] - pe[1];
            bey = pb[1] - pe[1];
            cey = pc[1] - pe[1];
            dey = pd[1] - pe[1];
            aez = pa[2] - pe[2];
            bez = pb[2] - pe[2];
            cez = pc[2] - pe[2];
            dez = pd[2] - pe[2];

            EA.TwoProduct(aex, bey, out aexbey1, out aexbey0);
            EA.TwoProduct(bex, aey, out bexaey1, out bexaey0);
            EA.TwoTwoDiff(aexbey1, aexbey0, bexaey1, bexaey0, out ab3, out ab[2], out ab[1], out ab[0]);
            ab[3] = ab3;

            EA.TwoProduct(bex, cey, out bexcey1, out bexcey0);
            EA.TwoProduct(cex, bey, out cexbey1, out cexbey0);
            EA.TwoTwoDiff(bexcey1, bexcey0, cexbey1, cexbey0, out bc3, out bc[2], out bc[1], out bc[0]);
            bc[3] = bc3;

            EA.TwoProduct(cex, dey, out cexdey1, out cexdey0);
            EA.TwoProduct(dex, cey, out dexcey1, out dexcey0);
            EA.TwoTwoDiff(cexdey1, cexdey0, dexcey1, dexcey0, out cd3, out cd[2], out cd[1], out cd[0]);
            cd[3] = cd3;

            EA.TwoProduct(dex, aey, out dexaey1, out dexaey0);
            EA.TwoProduct(aex, dey, out aexdey1, out aexdey0);
            EA.TwoTwoDiff(dexaey1, dexaey0, aexdey1, aexdey0, out da3, out da[2], out da[1], out da[0]);
            da[3] = da3;

            EA.TwoProduct(aex, cey, out aexcey1, out aexcey0);
            EA.TwoProduct(cex, aey, out cexaey1, out cexaey0);
            EA.TwoTwoDiff(aexcey1, aexcey0, cexaey1, cexaey0, out ac3, out ac[2], out ac[1], out ac[0]);
            ac[3] = ac3;

            EA.TwoProduct(bex, dey, out bexdey1, out bexdey0);
            EA.TwoProduct(dex, bey, out dexbey1, out dexbey0);
            EA.TwoTwoDiff(bexdey1, bexdey0, dexbey1, dexbey0, out bd3, out bd[2], out bd[1], out bd[0]);
            bd[3] = bd3;

            temp8alen = EA.ScaleExpansionZeroElim(4, cd, bez, temp8a);
            temp8blen = EA.ScaleExpansionZeroElim(4, bd, -cez, temp8b);
            temp8clen = EA.ScaleExpansionZeroElim(4, bc, dez, temp8c);
            temp16len = EA.FastExpansionSumZeroElim(temp8alen, temp8a, temp8blen, temp8b, temp16);
            temp24len = EA.FastExpansionSumZeroElim(temp8clen, temp8c, temp16len, temp16, temp24);
            temp48len = EA.ScaleExpansionZeroElim(temp24len, temp24, aex, temp48);
            xlen = EA.ScaleExpansionZeroElim(temp48len, temp48, -aex, xdet);
            temp48len = EA.ScaleExpansionZeroElim(temp24len, temp24, aey, temp48);
            ylen = EA.ScaleExpansionZeroElim(temp48len, temp48, -aey, ydet);
            temp48len = EA.ScaleExpansionZeroElim(temp24len, temp24, aez, temp48);
            zlen = EA.ScaleExpansionZeroElim(temp48len, temp48, -aez, zdet);
            xylen = EA.FastExpansionSumZeroElim(xlen, xdet, ylen, ydet, xydet);
            alen = EA.FastExpansionSumZeroElim(xylen, xydet, zlen, zdet, adet);

            temp8alen = EA.ScaleExpansionZeroElim(4, da, cez, temp8a);
            temp8blen = EA.ScaleExpansionZeroElim(4, ac, dez, temp8b);
            temp8clen = EA.ScaleExpansionZeroElim(4, cd, aez, temp8c);
            temp16len = EA.FastExpansionSumZeroElim(temp8alen, temp8a, temp8blen, temp8b, temp16);
            temp24len = EA.FastExpansionSumZeroElim(temp8clen, temp8c, temp16len, temp16, temp24);
            temp48len = EA.ScaleExpansionZeroElim(temp24len, temp24, bex, temp48);
            xlen = EA.ScaleExpansionZeroElim(temp48len, temp48, bex, xdet);
            temp48len = EA.ScaleExpansionZeroElim(temp24len, temp24, bey, temp48);
            ylen = EA.ScaleExpansionZeroElim(temp48len, temp48, bey, ydet);
            temp48len = EA.ScaleExpansionZeroElim(temp24len, temp24, bez, temp48);
            zlen = EA.ScaleExpansionZeroElim(temp48len, temp48, bez, zdet);
            xylen = EA.FastExpansionSumZeroElim(xlen, xdet, ylen, ydet, xydet);
            blen = EA.FastExpansionSumZeroElim(xylen, xydet, zlen, zdet, bdet);

            temp8alen = EA.ScaleExpansionZeroElim(4, ab, dez, temp8a);
            temp8blen = EA.ScaleExpansionZeroElim(4, bd, aez, temp8b);
            temp8clen = EA.ScaleExpansionZeroElim(4, da, bez, temp8c);
            temp16len = EA.FastExpansionSumZeroElim(temp8alen, temp8a, temp8blen, temp8b, temp16);
            temp24len = EA.FastExpansionSumZeroElim(temp8clen, temp8c, temp16len, temp16, temp24);
            temp48len = EA.ScaleExpansionZeroElim(temp24len, temp24, cex, temp48);
            xlen = EA.ScaleExpansionZeroElim(temp48len, temp48, -cex, xdet);
            temp48len = EA.ScaleExpansionZeroElim(temp24len, temp24, cey, temp48);
            ylen = EA.ScaleExpansionZeroElim(temp48len, temp48, -cey, ydet);
            temp48len = EA.ScaleExpansionZeroElim(temp24len, temp24, cez, temp48);
            zlen = EA.ScaleExpansionZeroElim(temp48len, temp48, -cez, zdet);
            xylen = EA.FastExpansionSumZeroElim(xlen, xdet, ylen, ydet, xydet);
            clen = EA.FastExpansionSumZeroElim(xylen, xydet, zlen, zdet, cdet);

            temp8alen = EA.ScaleExpansionZeroElim(4, bc, aez, temp8a);
            temp8blen = EA.ScaleExpansionZeroElim(4, ac, -bez, temp8b);
            temp8clen = EA.ScaleExpansionZeroElim(4, ab, cez, temp8c);
            temp16len = EA.FastExpansionSumZeroElim(temp8alen, temp8a, temp8blen, temp8b, temp16);
            temp24len = EA.FastExpansionSumZeroElim(temp8clen, temp8c, temp16len, temp16, temp24);
            temp48len = EA.ScaleExpansionZeroElim(temp24len, temp24, dex, temp48);
            xlen = EA.ScaleExpansionZeroElim(temp48len, temp48, dex, xdet);
            temp48len = EA.ScaleExpansionZeroElim(temp24len, temp24, dey, temp48);
            ylen = EA.ScaleExpansionZeroElim(temp48len, temp48, dey, ydet);
            temp48len = EA.ScaleExpansionZeroElim(temp24len, temp24, dez, temp48);
            zlen = EA.ScaleExpansionZeroElim(temp48len, temp48, dez, zdet);
            xylen = EA.FastExpansionSumZeroElim(xlen, xdet, ylen, ydet, xydet);
            dlen = EA.FastExpansionSumZeroElim(xylen, xydet, zlen, zdet, ddet);

            ablen = EA.FastExpansionSumZeroElim(alen, adet, blen, bdet, abdet);
            cdlen = EA.FastExpansionSumZeroElim(clen, cdet, dlen, ddet, cddet);
            finlength = EA.FastExpansionSumZeroElim(ablen, abdet, cdlen, cddet, fin1);

            det = EA.Estimate(finlength, fin1);
            errbound = isperrboundB * permanent;
            if ((det >= errbound) || (-det >= errbound))
            {
                return det;
            }

            EA.TwoDiffTail(pa[0], pe[0], aex, out aextail);
            EA.TwoDiffTail(pa[1], pe[1], aey, out aeytail);
            EA.TwoDiffTail(pa[2], pe[2], aez, out aeztail);
            EA.TwoDiffTail(pb[0], pe[0], bex, out bextail);
            EA.TwoDiffTail(pb[1], pe[1], bey, out beytail);
            EA.TwoDiffTail(pb[2], pe[2], bez, out beztail);
            EA.TwoDiffTail(pc[0], pe[0], cex, out cextail);
            EA.TwoDiffTail(pc[1], pe[1], cey, out ceytail);
            EA.TwoDiffTail(pc[2], pe[2], cez, out ceztail);
            EA.TwoDiffTail(pd[0], pe[0], dex, out dextail);
            EA.TwoDiffTail(pd[1], pe[1], dey, out deytail);
            EA.TwoDiffTail(pd[2], pe[2], dez, out deztail);
            if ((aextail == 0.0) && (aeytail == 0.0) && (aeztail == 0.0)
                && (bextail == 0.0) && (beytail == 0.0) && (beztail == 0.0)
                && (cextail == 0.0) && (ceytail == 0.0) && (ceztail == 0.0)
                && (dextail == 0.0) && (deytail == 0.0) && (deztail == 0.0))
            {
                return det;
            }

            errbound = isperrboundC * permanent + resulterrbound * System.Math.Abs(det);
            abeps = (aex * beytail + bey * aextail)
                - (aey * bextail + bex * aeytail);
            bceps = (bex * ceytail + cey * bextail)
                - (bey * cextail + cex * beytail);
            cdeps = (cex * deytail + dey * cextail)
                - (cey * dextail + dex * ceytail);
            daeps = (dex * aeytail + aey * dextail)
                - (dey * aextail + aex * deytail);
            aceps = (aex * ceytail + cey * aextail)
                - (aey * cextail + cex * aeytail);
            bdeps = (bex * deytail + dey * bextail)
                - (bey * dextail + dex * beytail);
            det += (((bex * bex + bey * bey + bez * bez)
                    * ((cez * daeps + dez * aceps + aez * cdeps)
                        + (ceztail * da3 + deztail * ac3 + aeztail * cd3))
                    + (dex * dex + dey * dey + dez * dez)
                    * ((aez * bceps - bez * aceps + cez * abeps)
                        + (aeztail * bc3 - beztail * ac3 + ceztail * ab3)))
                    - ((aex * aex + aey * aey + aez * aez)
                    * ((bez * cdeps - cez * bdeps + dez * bceps)
                        + (beztail * cd3 - ceztail * bd3 + deztail * bc3))
                    + (cex * cex + cey * cey + cez * cez)
                    * ((dez * abeps + aez * bdeps + bez * daeps)
                        + (deztail * ab3 + aeztail * bd3 + beztail * da3))))
                + 2.0 * (((bex * bextail + bey * beytail + bez * beztail)
                            * (cez * da3 + dez * ac3 + aez * cd3)
                            + (dex * dextail + dey * deytail + dez * deztail)
                            * (aez * bc3 - bez * ac3 + cez * ab3))
                        - ((aex * aextail + aey * aeytail + aez * aeztail)
                            * (bez * cd3 - cez * bd3 + dez * bc3)
                            + (cex * cextail + cey * ceytail + cez * ceztail)
                            * (dez * ab3 + aez * bd3 + bez * da3)));
            if ((det >= errbound) || (-det >= errbound))
            {
                return det;
            }

            return InSphereExact(pa, pb, pc, pd, pe);
        }

        #endregion
    }
}
