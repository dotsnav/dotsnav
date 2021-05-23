using System;
using System.Diagnostics;
using NUnit.Framework;
using RobustArithmetic.Test.Util;

namespace RobustArithmetic.Test.FpuControl.Test
{
    [TestFixture]
    public class RoundingTests
    {

        /// <summary>
        /// This test shows how double rounding means that explicit casting does not get around Fpu settings
        /// </summary>
        [Test]
        public void ShowDoubleRounding()
        {
            double a = DoubleConverter.FromFloatingPointBinaryString('1'.Repeat(52) + "0");          // 111....11110
            double b = DoubleConverter.FromFloatingPointBinaryString("0.100000000001");              // 000....00000.100000000001
            double expected53 = DoubleConverter.FromFloatingPointBinaryString('1'.Repeat(52) + "1"); // 111....11111
            double expected64 = a;

            // Set Fpu to 53-bit precision (the default)
            FpuControl.SetState((uint)FpuControl.PrecisionControl.Double53Bits, FpuControl.Mask.PrecisionControl);

            double result53 = a + b;
            Assert.AreEqual(expected53, result53);

            // Set Fpu to 64-bit precision (extended precision)
            FpuControl.SetState((uint)FpuControl.PrecisionControl.Extended64Bits, FpuControl.Mask.PrecisionControl);
            double result64 = (double)(a + b);
            Assert.AreEqual(expected64, result64);

            double result64_0 = (a + b) - a;
            Assert.AreNotEqual(0.0, result64_0);
            Assert.AreNotEqual(b, result64_0);
            Assert.AreEqual(0.5, result64_0);       // 000....00000.1

        }

        /// <summary>
        /// This is the double-rounding example from Priest.
        /// The test exhibits the double rounding problem, where a floating point calculation is wrong if it is 
        /// rounded to 80-bits in the FPU, and then rounded to 64-bits by storing.
        /// We get a different result to when the calculation is just rounded to 64-bits (53-bit precision FPU mode)
        /// 
        /// Priest:
        /// For example, in IEEE 754 arithmetic rounded first to 64
        /// and then to 53 significant bits, the sum of
        /// 2^52 + 1 and .5 - 2^-54 rounds to 2^52 + 1.5,
        /// which then rounds to 2^52 + 2 by the round-to-even rule for halfway cases.
        /// The roundoff is then .5 + 2^-54, which is not representable in 53 bits.
        /// </summary>
        [Test]
        public void ShowDoubleRoundingPriest()
        {
            double a = Math.Pow(2.0, 52.0) + 1.0;
            double b = 0.5 - Math.Pow(2.0, -54.0);
            double expected53 = a;
            double expected64 = a + 1.0; // Math.Pow(2.0, 52.0) + 2.0;

            Debug.Print(DoubleConverter.ToFloatingPointBinaryString(a));
            Debug.Print(DoubleConverter.ToFloatingPointBinaryString(b));
            Debug.Print(DoubleConverter.ToFloatingPointBinaryString(expected53));
            Debug.Print(DoubleConverter.ToFloatingPointBinaryString(expected64));

            // Set Fpu to 53-bit precision (the default)
            FpuControl.SetState((uint)FpuControl.PrecisionControl.Double53Bits, FpuControl.Mask.PrecisionControl);

            double result53 = a + b;
            Debug.Print(DoubleConverter.ToFloatingPointBinaryString(result53));
            Assert.AreEqual(expected53, result53);

            // Set Fpu to 64-bit precision (extended precision)
            FpuControl.SetState((uint)FpuControl.PrecisionControl.Extended64Bits, FpuControl.Mask.PrecisionControl);
            double result64 = (double)(a + b);
            Debug.Print(DoubleConverter.ToFloatingPointBinaryString(result64));
            Assert.AreEqual(expected64, result64);
        }

        // This test exhibits the double rounding problem, where a floating point calculation is wrong if it is 
        // rounded to 80-bits in the FPU, and then rounded to 64-bits by storing.
        // We get a different result to when the calculation is just rounded to 64-bits (53-bit precision FPU mode)
        [Test]
        public void ShowDoubleRoundingPriestExplicit()
        {
            double a = DoubleConverter.FromFloatingPointBinaryString("1" + '0'.Repeat(51) + "1"); // 100....00001 (53-bits wide)
            double b = DoubleConverter.FromFloatingPointBinaryString("0.0" + '1'.Repeat(53));     //            0.0111...111 (53 1's)
            double expected53 = a;
            double expected64 = a + 1.0; // The point is that this is different to expceted53.

            Debug.Print(DoubleConverter.ToFloatingPointBinaryString(a));
            Debug.Print(DoubleConverter.ToFloatingPointBinaryString(b));
            Debug.Print(DoubleConverter.ToFloatingPointBinaryString(expected53));
            Debug.Print(DoubleConverter.ToFloatingPointBinaryString(expected64));

            // Set Fpu to 53-bit precision (the default)
            FpuControl.SetState((uint)FpuControl.PrecisionControl.Double53Bits, FpuControl.Mask.PrecisionControl);

            double result53 = a + b;
            Debug.Print(DoubleConverter.ToFloatingPointBinaryString(result53));
            Assert.AreEqual(expected53, result53);

            // Explicit rounding makes no difference here (since we're in Double53bits precision FPU mode)
            result53 = (double)(a + b);
            Debug.Print(DoubleConverter.ToFloatingPointBinaryString(result53));
            Assert.AreEqual(expected53, result53);

            // Set Fpu to 64-bit precision (extended precision)
            FpuControl.SetState((uint)FpuControl.PrecisionControl.Extended64Bits, FpuControl.Mask.PrecisionControl);
            double result64 = (double)(a + b);
            Debug.Print(DoubleConverter.ToFloatingPointBinaryString(result64));
            Assert.AreEqual(expected64, result64);
        }
    }
}
