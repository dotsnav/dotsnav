using NUnit.Framework;
using RobustArithmetic.Test.Util;

namespace RobustArithmetic.Test.FpuControl.Test
{
    [TestFixture]
    public class FpuControlTests
    {

        // This test 'fails' under 64-bit, since we can't really set the precision to Extended64bit!
        [Test]
        public void TestMethod1()
        {
            double before = TestCalc();
            Assert.AreEqual(0.0, before);

            var oldState = new FpuControl.State(FpuControl.GetState());
            var oldPc = oldState.PrecisionControl;
            uint err = FpuControl.SetState((uint)FpuControl.PrecisionControl.Extended64Bits, FpuControl.Mask.PrecisionControl);
            var newState = new FpuControl.State(FpuControl.GetState());
            var newPc = newState.PrecisionControl;

            double after = TestCalc();
            Assert.AreEqual(0.5, after);

            double afterSafe = TestCalcSafe();
            Assert.AreEqual(0.0, afterSafe);

            FpuControl.SetState((uint)oldState.PrecisionControl, FpuControl.Mask.PrecisionControl);

            double reset = TestCalc();
            Assert.AreEqual(0.0, reset);

        }

        public double TestCalc()
        {
            double a = DoubleConverter.FromFloatingPointBinaryString("11111111111111111111111111111111111111111111111111110.0");
            double b = DoubleConverter.FromFloatingPointBinaryString("00000000000000000000000000000000000000000000000000000.1");

            double result = a + b - a;
            return result;
        }

        // Here we add an explicit cast, which ensures that (a + b) is evaluated to 64-bits before continuing
        public double TestCalcSafe()
        {
            double a = DoubleConverter.FromFloatingPointBinaryString("11111111111111111111111111111111111111111111111111110.0");
            double b = DoubleConverter.FromFloatingPointBinaryString("00000000000000000000000000000000000000000000000000000.1");

            double result = (double)(a + b) - a;
            return result;
        }


        [Test]
        public void TestPrecision64()
        {
            double a = DoubleConverter.FromFloatingPointBinaryString('1'.Repeat(52) + "0"); // 111....11110
            double b = DoubleConverter.FromFloatingPointBinaryString("0.1");                     // 000....00000.1
            double expected64 = a;

            FpuControl.SetState((uint)FpuControl.PrecisionControl.Extended64Bits, FpuControl.Mask.PrecisionControl);
            double result64 = (double)(a + b);
            Assert.AreEqual(expected64, result64);

            double result64_b = (a + b) - a;
            Assert.AreEqual(b, result64_b);

            double result64_0 = ((double)(a + b)) - a;
            Assert.AreNotEqual(b, result64_0);
            Assert.AreEqual(0.0, result64_0);

        }

        [Test]
        public void TestPrecision53()
        {
            double a = DoubleConverter.FromFloatingPointBinaryString('1'.Repeat(52) + "0"); // 111....11110
            double b = DoubleConverter.FromFloatingPointBinaryString("0.1");                     // 000....00000.1
            double expected53 = a;

            FpuControl.SetState((uint)FpuControl.PrecisionControl.Double53Bits, FpuControl.Mask.PrecisionControl);
            double result53 = (double)(a + b);
            Assert.AreEqual(expected53, result53);

            double result53_0 = (a + b) - a;
            Assert.AreEqual(0.0, result53_0);

            double result53_X = ((double)(a + b)) - a;
            Assert.AreEqual(0.0, result53_X);

        }
    }
}
