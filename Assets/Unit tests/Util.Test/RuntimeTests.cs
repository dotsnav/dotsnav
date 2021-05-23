using NUnit.Framework;

namespace RobustArithmetic.Test.Util.Test
{
    [TestFixture]
    public class RuntimeTests
    {
        // TODO: Consider this issue, for whether 32-bit and 64-bit .NET parse differently: https://connect.microsoft.com/VisualStudio/feedback/details/914964/double-round-trip-conversion-via-a-string-is-not-safe#tabs

        /// <summary>
        ///  This test tries to confirm that the floating point rounding is the round-to-even tiebreaking rule specified by the IEEE 754 spec.
        /// </summary>
        [Test]
        public void RoundToEvenTiebreaking()
        {
            var d1 = DoubleConverter.FromFloatingPointBinaryString("100000000000000000000000000000000000000000000000000010");
            var d2 = DoubleConverter.FromFloatingPointBinaryString(                                                     "1");
            // Closest even to ~~~011
            var d3 = DoubleConverter.FromFloatingPointBinaryString("100000000000000000000000000000000000000000000000000100");
            var dtest = (double)d1 + d2;
            var stest = dtest.ToFloatingPointBinaryString();
            Assert.AreEqual(d3, dtest);

            // Closest even to ~~~001
            var d4 = DoubleConverter.FromFloatingPointBinaryString("100000000000000000000000000000000000000000000000000000");
            var dtest2 = (double)d1 - d2;
            var stest2 = dtest2.ToFloatingPointBinaryString();
            Assert.AreEqual(d4, dtest2);
        }

    }
}
