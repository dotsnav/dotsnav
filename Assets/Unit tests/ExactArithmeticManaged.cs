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
    /// <summary>
    /// Implements the exact floating-point described by Shewchuck, and implemented in predicates.c
    /// </summary>
    static class ExactArithmeticManaged
    {
        #region Basic arithmetic - Sum, Diff and Product
        // Only valid if |a| >= |b|
        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void FastTwoSum(double a, double b, out double x, out double y)
        {
            x = a + b;
            FastTwoSumTail(a, b, x, out y);
        }
        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void FastTwoSumTail(double a, double b, double x, out double y)
        {
            double bvirt = x - a;
            y = b - bvirt;
        }

        // Only valid if |a| >= |b| ??
        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void FastTwoDiff(double a, double b, out double x, out double y)
        {
            x = a - b;
            FastTwoDiffTail(a, b, x, out y);
        }
        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void FastTwoDiffTail(double a, double b, double x, out double y)
        {
            double bvirt = a - x;
            y = bvirt - b;
        }

        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void TwoSum(double a, double b, out double x, out double y)
        {
            x = a + b;
            TwoSumTail(a, b, x, out y);
        }

        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void TwoSumTail(double a, double b, double x, out double y)
        {
            double bvirt = x - a;
            double avirt = x - bvirt;
            double bround = b - bvirt;
            double around = a - avirt;
            y = around + bround;
        }

        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void TwoDiff(double a, double b, out double x, out double y)
        {
            x = a - b;
            TwoDiffTail(a, b, x, out y);
        }

        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void TwoDiffTail(double a, double b, double x, out double y)
        {
            double bvirt = a - x;
            double avirt = x + bvirt;
            double bround = bvirt - b;
            double around = a - avirt;
            y = around + bround;
        }

        // S. p18 with s = 27
        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void Split(double a, out double ahi, out double alo)
        {
            // Set splitter used for Product (using s = 27)
            // Agrees with value calculated by EpsilonSplitter
            const double splitter = (1 << 27) + 1.0; // 2^ceiling(p / 2) + 1 (and p=53)
            double c = splitter * a;
            double abig = c - a;
            ahi = c - abig;
            alo = a - ahi;
        }

        // S. p19 with s = 27
        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void TwoProduct(double a, double b, out double x, out double y)
        {
            x = a * b;
            TwoProductTail(a, b, x, out y);
        }

        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void TwoProductTail(double a, double b, double x, out double y)
        {
            double ahi, alo, bhi, blo;

            Split(a, out ahi, out alo);
            Split(b, out bhi, out blo);
            double err1 = x - (ahi * bhi);
            double err2 = err1 - (alo * bhi);
            double err3 = err2 - (ahi * blo);
            y = (alo * blo) - err3;
        }

        // This is TwoProduct for the case where one factor has already been Split
        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void TwoProductPresplit(double a, double b, double bhi, double blo, out double x, out double y)
        {
            double ahi, alo;

            x = a * b;
            Split(a, out ahi, out alo);
            double err1 = x - (ahi * bhi);
            double err2 = err1 - (alo * bhi);
            double err3 = err2 - (ahi * blo);
            y = (alo * blo) - err3;
        }

        // This is TwoProduct for the case where one factor has already been Split
        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void TwoProduct2Presplit(double a, double ahi, double alo, double b, double bhi, double blo, out double x, out double y)
        {
            x = a * b;
            double err1 = x - (ahi * bhi);
            double err2 = err1 - (alo * bhi);
            double err3 = err2 - (ahi * blo);
            y = (alo * blo) - err3;
        }

        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void Square(double a, out double x, out double y)
        {
            x = a * a;
            SquareTail(a, x, out y);
        }

        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void SquareTail(double a, double x, out double y)
        {
            double err1, err3;
            double ahi, alo;

            Split(a, out ahi, out alo);
            err1 = x - (ahi * ahi);
            err3 = err1 - ((ahi + ahi) * alo);
            y = (alo * alo) - err3;
        }
        #endregion

        #region Summing expansions of various fixed lengths
        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void TwoOneSum(double a1, double a0, double b, out double x2, out double x1, out double x0)
        {
            double _i;
            TwoSum(a0, b , out _i, out x0);
            TwoSum(a1, _i, out x2, out x1);
        }

        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void TwoOneDiff(double a1, double a0, double b, out double x2, out double x1, out double x0)
        {
            double _i;
            TwoDiff(a0, b, out _i, out x0);
            TwoSum(a1, _i, out x2, out x1);
        }

        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void TwoTwoSum(double a1, double a0, double b1, double b0, out double x3, out double x2, out double x1, out double x0)
        {
            double _j, _0;
            TwoOneSum(a1, a0, b0, out _j, out _0, out x0);
            TwoOneSum(_j, _0, b1, out x3, out x2, out x1);
        }

        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void TwoTwoDiff(double a1, double a0, double b1, double b0, out double x3, out double x2, out double x1, out double x0)
        {
            double _j, _0;
            TwoOneDiff(a1, a0, b0, out _j, out _0, out x0); 
            TwoOneDiff(_j, _0, b1, out x3, out x2, out x1);
        }

        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void FourOneSum(double a3, double a2, double a1, double a0, double b, out double x4, out double x3, out double x2, out double x1, out double x0)
        {
            double _j;
            TwoOneSum(a1, a0, b, out _j, out x1, out x0);
            TwoOneSum(a3, a2, _j, out x4, out x3, out x2);
        }

        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void FourTwoSum(double a3, double a2, double a1, double a0, double b1, double b0, out double x5, out double x4, out double x3, out double x2, out double x1, out double x0)
        {
            double _k, _2, _1, _0;
            FourOneSum(a3, a2, a1, a0, b0, out _k, out _2, out _1, out _0, out x0);
            FourOneSum(_k, _2, _1, _0, b1, out x5, out x4, out x3, out x2, out x1);
        }

        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void FourFourSum(double a3, double a2, double a1, double a0, double b4, double b3, double b1, double b0, 
                    out double x7, out double x6, out double x5, out double x4, out double x3, out double x2, out double x1, out double x0)
        {
            double _l, _2, _1, _0;
            FourTwoSum(a3, a2, a1, a0, b1, b0, out _l, out _2, out _1, out _0, out x1, out x0);
            FourTwoSum(_l, _2, _1, _0, b4, b3, out x7, out x6, out x5, out x4, out x3, out x2);
        }

        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void EightOneSum(double a7, double a6, double a5, double a4, double a3, double a2, double a1, double a0, double b, 
                    out double x8, out double x7, out double x6, out double x5, out double x4, out double x3, out double x2, out double x1, out double x0)
        {
            double _j;
            FourOneSum(a3, a2, a1, a0, b, out _j, out x3, out x2, out x1, out x0);
            FourOneSum(a7, a6, a5, a4, _j, out x8, out x7, out x6, out x5, out x4);
        }

        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void EightTwoSum(double a7, double a6, double a5, double a4, double a3, double a2, double a1, double a0, double b1, double b0, 
            out double x9, out double x8, out double x7, out double x6, out double x5, out double x4, out double x3, out double x2, out double x1, out double x0)
        {
            double _k, _6, _5, _4, _3, _2, _1, _0;
            EightOneSum(a7, a6, a5, a4, a3, a2, a1, a0, b0, out _k, out _6, out _5, out _4, out _3, out _2, out _1, out _0, out x0);
            EightOneSum(_k, _6, _5, _4, _3, _2, _1, _0, b1, out x9, out x8, out x7, out x6, out x5, out x4, out x3, out x2, out x1);
        }

        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void EightFourSum(double a7, double a6, double a5, double a4, double a3, double a2, double a1, double a0, double b4, double b3, double b1, double b0, 
            out double x11, out double x10, out double x9, out double x8, out double x7, out double x6, out double x5, out double x4, out double x3, out double x2, out double x1, out double x0)
        {
            double _l, _6, _5, _4, _3, _2, _1, _0;
            EightTwoSum(a7, a6, a5, a4, a3, a2, a1, a0, b1, b0, out _l, out _6, out _5, out _4, out _3, out _2, out _1, out _0, out x1, out x0);
            EightTwoSum(_l, _6, _5, _4, _3, _2, _1, _0, b4, b3, out x11, out x10, out x9, out x8, out x7, out x6, out x5, out x4, out x3, out x2);
        }
        #endregion

        #region Multiplying expansions of various lengths
        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void TwoOneProduct(double a1, double a0, double b, out double x3, out double x2, out double x1, out double x0)
        {
            double bhi, blo;
            double _i, _j, _k, _0;
            Split(b, out bhi, out blo);
            TwoProductPresplit(a0, b, bhi, blo, out _i, out x0);
            TwoProductPresplit(a1, b, bhi, blo, out _j, out _0);
            TwoSum(_i, _0, out _k, out x1);
            FastTwoSum(_j, _k, out x3, out x2);
        }

        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void FourOneProduct(double a3, double a2, double a1, double a0, double b, out double x7, out double x6, out double x5, out double x4, out double x3, out double x2, out double x1, out double x0)
        {
            double bhi, blo;
            double _i, _j, _k, _0;
            Split(b, out bhi, out blo);
            TwoProductPresplit(a0, b, bhi, blo, out _i, out x0);
            TwoProductPresplit(a1, b, bhi, blo, out _j, out _0);
            TwoSum(_i, _0, out _k, out x1);
            FastTwoSum(_j, _k, out _i, out x2);
            TwoProductPresplit(a2, b, bhi, blo, out _j, out _0);
            TwoSum(_i, _0, out _k, out x3);
            FastTwoSum(_j, _k, out _i, out x4);
            TwoProductPresplit(a3, b, bhi, blo, out _j, out _0);
            TwoSum(_i, _0, out _k, out x5);
            FastTwoSum(_j, _k, out x7, out x6);
        }

        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void TwoTwoProduct(double a1, double a0, double b1, double b0, out double x7, out double x6, out double x5, out double x4, out double x3, out double x2, out double x1, out double x0)
        {
            double a0hi, a0lo, a1hi, a1lo, bhi, blo;
            double _i, _j, _k, _l, _m, _n, _0, _1, _2;
            Split(a0, out a0hi, out a0lo);
            Split(b0, out bhi, out blo);
            TwoProduct2Presplit(a0, a0hi, a0lo, b0, bhi, blo, out _i, out x0);
            Split(a1, out a1hi, out a1lo);
            TwoProduct2Presplit(a1, a1hi, a1lo, b0, bhi, blo, out _j, out _0);
            TwoSum(_i, _0, out _k, out _1);
            FastTwoSum(_j, _k, out _l, out _2);
            Split(b1, out bhi, out blo);
            TwoProduct2Presplit(a0, a0hi, a0lo, b1, bhi, blo, out _i, out _0);
            TwoSum(_1, _0, out _k, out x1);
            TwoSum(_2, _k, out _j, out _1);
            TwoSum(_l, _j, out _m, out _2);
            TwoProduct2Presplit(a1, a1hi, a1lo, b1, bhi, blo, out _j, out _0);
            TwoSum(_i, _0, out _n, out _0);
            TwoSum(_1, _0, out _i, out x2);
            TwoSum(_2, _i, out _k, out _1);
            TwoSum(_m, _k, out _l, out _2);
            TwoSum(_j, _n, out _k, out _0);
            TwoSum(_1, _0, out _j, out x3);
            TwoSum(_2, _j, out _i, out _1);
            TwoSum(_l, _i, out _m, out _2);
            TwoSum(_1, _k, out _i, out x4);
            TwoSum(_2, _i, out _k, out x5);
            TwoSum(_m, _k, out x7, out x6);
        }

        // [MethodImplOptions(MethodImplOptions.AggressiveInlining)]
        public static void TwoSquare(double a1, double a0, out double x5, out double x4, out double x3, out double x2, out double x1, out double x0)
        {
            double _j, _k, _l, _0, _1, _2;
            Square(a0, out _j, out x0);
            _0 = a0 + a0;
            TwoProduct(a1, _0, out _k, out _1);
            TwoOneSum(_k, _1, _j, out _l, out _2, out x1);
            Square(a1, out _j, out _1);
            TwoTwoSum(_j, _1, _l, _2, out x5, out x4, out x3, out x2);
        }
        #endregion

        #region Expansion arithmetic - ExpansionSum, ScaleExpansion and helpers
        // Algorithm Grow-Expansion (Shewchuk p. 10)
        // NOTE: This algorithm is not actually used further, but good for checking the S. paper.
        // Adds a single value to an expansion - predicates version
        // e and h can be the same.
        public static int GrowExpansion(int elen, double[] e, double b, double[] h)
        {
            double Q;
            int eindex;

            Q = b;
            for (eindex = 0; eindex < e.Length; eindex++)
            {
                TwoSum(Q, e[eindex], out Q, out h[eindex]);
            }
            h[eindex] = Q;
            return eindex + 1;
        }

        // Algorithm Expansion-Sum
        // NOTE: This algorithm is not actually used further, but good for checking the S. paper.
        // e and h can be the same, but f and h cannot.
        public static int ExpansionSum(int elen, double[] e, int flen, double[] f, double[] h)
        {
            double Q, Qnew;
            int findex, hindex, hlast;
            double hnow;

            // h <= e+f[0]
            // GrowExpansion(e, f[0], h)
            Q = f[0];
            for (hindex = 0; hindex < elen; hindex++)
            {
                hnow = e[hindex];
                TwoSum(Q, hnow, out Qnew, out h[hindex]);
                Q = Qnew;
            }
            h[hindex] = Q;

            hlast = hindex;
            for (findex = 1; findex < flen; findex++)
            {
                // GrowExpansion(e, f[findex], h[findex...])
                Q = f[findex];
                for (hindex = findex; hindex <= hlast; hindex++)
                {
                    hnow = h[hindex];
                    TwoSum(Q, hnow, out Qnew, out h[hindex]);
                    Q = Qnew;
                }
                h[++hlast] = Q;
            }
            return hlast + 1;
        }

        // Algorithm Fast-Expansion-Sum
        // NOTE: This algorithm is not actually used further, but good for checking the S. paper.
        //       Instead the Zero Eliminating version is used.
        // h cannot be the same as e or f
        // Read this code together with fast_expansion_sum in predicates.c
        // There are some minor changes here, because we don't want to read outside the array bounds.
        public static int FastExpansionSum(int elen, double[] e, int flen, double[] f, double[] h)
        {
            double Q;
            double Qnew;
            int eindex, findex, hindex;
            double enow, fnow;

            // We traverse the lists e and f together. moving from small to large magnitude
            // enow and fnow keep track of the current value in each list, 
            // and eindex and findex the current index
            enow = e[0];
            fnow = f[0];
            eindex = findex = 0;

            // First step is to assign to Q the entry with smaller magnitude
            if ((fnow > enow) == (fnow > -enow)) // if |fnow| >= |enow|
            {
                Q = enow;
                eindex++;

                // NOTE: The original prdicates.c code here would read past the array bound here (but never use the value).
                // Q = enow;
                // enow = e[++eindex]; <<< PROBLEM HERE

                // Instead I just increment the index, and do an extra read later.
                // Pattern is then to read both enow and fnow for every step
                // This adds some extra array evaluations, especially for long arrays, but removes one at the end of each array.
            }
            else
            {
                Q = fnow;
                findex++;
            }

            // Start adding entries into h, carrying Q
            hindex = 0;
            
            // Check whether we still have entries in both lists
            if ((eindex < elen) && (findex < flen)) 
            {
                // Note we have an extra 'unrolled' step here, where we are allowed to use FastTwoSum
                // This is becuase we know the next expansion entry is smaller than Q (according to how Q was picked above)
                enow = e[eindex];
                fnow = f[findex];
                // Pick smaller magnitude
                // if |fnow| >= |enow|
                if ((fnow > enow) == (fnow > -enow))
                {
                    // Add e and advance eindex
                    FastTwoSum(enow, Q, out Qnew, out h[0]);
                    eindex++;
                } 
                else 
                {
                    // Add f and advance findex
                    FastTwoSum(fnow, Q, out Qnew, out h[0]);
                    findex++;
                }
                Q = Qnew;
                hindex = 1;
                // While we still have entries in both lists
                while ((eindex < elen) && (findex < flen)) 
                {
                    // Can no longer use FastTwoSum - use TwoSum
                    enow = e[eindex];
                    fnow = f[findex];
                    // Pick smaller magnitude
                    // if |fnow| >= |enow|
                    if ((fnow > enow) == (fnow > -enow))
                    {
                        TwoSum(Q, enow, out Qnew, out h[hindex]);
                        eindex++;
                    } 
                    else 
                    {
                        TwoSum(Q, fnow, out Qnew, out h[hindex]);
                        findex++;
                    }
                    Q = Qnew;
                    hindex++;
                }
            }
            // Now we have exhausted one of the lists
            // For the rest, we just run along the list that has values left, 
            //    no more tests to try to pull from the correct list
            while (eindex < elen)
            {
                enow = e[eindex];
                TwoSum(Q, enow, out Qnew, out h[hindex]);
                eindex++;
                Q = Qnew;
                hindex++;
            }
            while (findex < flen)
            {
                fnow = f[findex];
                TwoSum(Q, fnow, out Qnew, out h[hindex]);
                findex++;
                Q = Qnew;
                hindex++;
            }
            h[hindex] = Q;
            return hindex + 1;
        }

        // Algorithm Fast-Expansion-Sum-Zero-Elim
        //
        // Sum two expansions, elimiating zero components from the output expansion.
        //
        // h cannot be the same as e or f
        // Read this code together with fast_expansion_sum_zeroelim in predicates.c
        // There are some minor changes here, because we don't want to read outside the array bounds.
        public static int FastExpansionSumZeroElim(int elen, double[] e, int flen, double[] f, double[] h)
        {
            double Q;
            double Qnew;
            double hh;
            int eindex, findex, hindex;
            double enow, fnow;

            // We traverse the lists e and f together. moving from small to large magnitude
            // enow and fnow keep track of the current value in each list, 
            // and eindex and findex the current index
            enow = e[0];
            fnow = f[0];
            eindex = findex = 0;

            // First step is to assign to Q the entry with smaller magnitude
            if ((fnow > enow) == (fnow > -enow)) // if |fnow| >= |enow|
            {
                Q = enow;
                eindex++;

                // NOTE: The original prdicates.c code here would read past the array bound here (but never use the value).
                // Q = enow;
                // enow = e[++eindex]; <<< PROBLEM HERE

                // Instead I just increment the index, and do an extra read later.
                // Pattern is then to read both enow and fnow for every step
                // This adds some extra array evaluations, especially for long arrays, but removes one at the end of each array.
            }
            else
            {
                Q = fnow;
                findex++;
            }

            // Start adding entries into h, carrying Q
            hindex = 0;

            // Check whether we still have entries in both lists
            if ((eindex < elen) && (findex < flen))
            {
                // Note we have an extra 'unrolled' step here, where we are allowed to use FastTwoSum
                // This is becuase we know the next expansion entry is smaller than Q (according to how Q was picked above)
                enow = e[eindex];
                fnow = f[findex];
                // Pick smaller magnitude
                // if |fnow| >= |enow|
                if ((fnow > enow) == (fnow > -enow))
                {
                    // Add e and advance eindex
                    FastTwoSum(enow, Q, out Qnew, out hh);
                    eindex++;
                }
                else
                {
                    // Add f and advance findex
                    FastTwoSum(fnow, Q, out Qnew, out hh);
                    findex++;
                }
                Q = Qnew;
                if (hh != 0.0)
                {
                    h[hindex++] = hh;
                }
                // While we still have entries in both lists
                while ((eindex < elen) && (findex < flen))
                {
                    // Can no longer use FastTwoSum - use TwoSum
                    enow = e[eindex];
                    fnow = f[findex];
                    // Pick smaller magnitude
                    // if |fnow| >= |enow|
                    if ((fnow > enow) == (fnow > -enow))
                    {
                        TwoSum(Q, enow, out Qnew, out hh);
                        eindex++;
                    }
                    else
                    {
                        TwoSum(Q, fnow, out Qnew, out hh);
                        findex++;
                    }
                    Q = Qnew;
                    if (hh != 0.0)
                    {
                        h[hindex++] = hh;
                    }
                }
            }
            // Now we have exhausted one of the lists
            // For the rest, we just run along the list that has values left, 
            //    no more tests to try to pull from the correct list
            while (eindex < elen)
            {
                enow = e[eindex];
                TwoSum(Q, enow, out Qnew, out hh);
                eindex++;
                Q = Qnew;
                if (hh != 0.0)
                {
                    h[hindex++] = hh;
                }
            }
            while (findex < flen)
            {
                fnow = f[findex];
                TwoSum(Q, fnow, out Qnew, out hh);
                findex++;
                Q = Qnew;
                if (hh != 0.0)
                {
                    h[hindex++] = hh;
                }
            }
            if ((Q != 0.0) || (hindex == 0))
            {
                h[hindex++] = Q;
            }
            return hindex;
        }

        // Algorithm Scale-Expansion S.p20
        //
        // Multiplies an expansion by a double
        // (Not actually used - zero elim version is used instead)
        //
        // (h should be at least twice as long as elen)
        //
        // Given a sorted, nonoverlapping expansion e, produces a sorted non-overlapping expansion h = be.
        // If e is nonadjacent then h is nonadjacent
        public static int ScaleExpansion(int elen, double[] e, double b, double[] h)
        {
            double Q, sum;
            double product1;
            double product0;
            int eindex, hindex;
            double enow;
            double bhi, blo;

            Split(b, out bhi, out blo);
            TwoProductPresplit(e[0], b, bhi, blo, out Q, out h[0]);
            hindex = 1;
            for (eindex = 1; eindex < elen; eindex++)
            {
                enow = e[eindex];
                TwoProductPresplit(enow, b, bhi, blo, out product1, out product0);
                TwoSum(Q, product0, out sum, out h[hindex]);
                hindex++;
                // NOTE: Next step is indicated (and proven safe) as FastTwoSum in the S. paper, 
                // but TwoSum is used in predicates.c
                FastTwoSum(product1, sum, out Q, out h[hindex]); 
                hindex++;
            }
            h[hindex] = Q;
            return elen + elen;
        }

        // Algorithm Scale-Expansion-Zeroelim
        //
        // Multiplies an expansion by a double, eliminating zero entries in the result were possible
        //
        // (h should be at least twice as long as elen)
        //
        // Given a sorted, nonoverlapping expansion e, produces a sorted non-overlapping expansion h = be.
        // If e is nonadjacent then h is nonadjacent
        public static int ScaleExpansionZeroElim(int elen, double[] e, double b, double[] h)
        {
            double Q, sum;
            double hh;
            double product1;
            double product0;
            int eindex, hindex;
            double enow;
            double bhi, blo;

            Split(b, out bhi, out blo);
            TwoProductPresplit(e[0], b, bhi, blo, out Q, out hh);
            hindex = 0;
            if (hh != 0.0)
            {
                h[hindex++] = hh;
            }
            for (eindex = 1; eindex < elen; eindex++)
            {
                enow = e[eindex];
                TwoProductPresplit(enow, b, bhi, blo, out product1, out product0);
                TwoSum(Q, product0, out sum, out hh);
                if (hh != 0.0)
                {
                    h[hindex++] = hh;
                }
                FastTwoSum(product1, sum, out Q, out hh);
                if (hh != 0.0)
                {
                    h[hindex++] = hh;
                }
            }
            if ((Q != 0.0) || (hindex == 0))
            {
                h[hindex++] = Q;
            }
            return hindex;
        }

        // Produce a one double estimate of an expansion's value
        // Also referred to as 'Approximate' in S.
        // This assumes e is sorted
        public static double Estimate(int elen, double[] e)
        {
            double Q;
            int eindex;

            Q = e[0];
            for (eindex = 1; eindex < elen; eindex++)
            {
                Q += e[eindex];
            }
            return Q;
        }

        // Compress an expansion
        public static int Compress(int elen, double[] e, double[] h)
        {
            double Q, q;
            double Qnew;
            int eindex, hindex;
            double enow, hnow;
            int top, bottom;

            bottom = elen - 1;
            Q = e[bottom];
            for (eindex = elen - 2; eindex >= 0; eindex--)
            {
                enow = e[eindex];
                FastTwoSum(Q, enow, out Qnew, out q);
                if (q != 0)
                {
                    h[bottom--] = Qnew;
                    Q = q;
                }
                else
                {
                    Q = Qnew;
                }
            }
            top = 0;
            for (hindex = bottom + 1; hindex < elen; hindex++)
            {
                hnow = h[hindex];
                FastTwoSum(hnow, Q, out Qnew, out q);
                if (q != 0)
                {
                    h[top++] = q;
                }
                Q = Qnew;
            }
            h[top] = Q;
            return top + 1;
        }
        #endregion
    }
}
