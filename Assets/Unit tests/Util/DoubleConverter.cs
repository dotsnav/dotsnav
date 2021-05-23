using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace RobustArithmetic.Test.Util
{
    /// <summary>
    /// A class to allow the conversion of doubles to string representations of
    /// their exact decimal values. The implementation aims for readability over
    /// efficiency.
    /// </summary>
    public static class DoubleConverter
    {

        /// <summary>
        /// Converts the given double to a string representation of its
        /// exact decimal value.
        /// This procedure (and ArbitraryDecimal) from Jon Skeet! 
        /// here: http://www.yoda.arachsys.com/csharp/DoubleConverter.cs
        /// </summary>
        /// <param name="d">The double to convert.</param>
        /// <returns>A string representation of the double's exact decimal value.</returns>
        public static string ToExactString(this double d)
        {
            if (double.IsPositiveInfinity(d))
                return "+Infinity";
            if (double.IsNegativeInfinity(d))
                return "-Infinity";
            if (double.IsNaN(d))
                return "NaN";

            // Translate the double into sign, exponent and mantissa.
            long bits = BitConverter.DoubleToInt64Bits(d);
            // Note that the shift is sign-extended, hence the test against -1 not 1
            bool negative = (bits < 0);
            int exponent = (int)((bits >> 52) & 0x7ffL);
            long mantissa = bits & 0xfffffffffffffL;

            // Subnormal numbers; exponent is effectively one higher,
            // but there's no extra normalisation bit in the mantissa
            if (exponent == 0)
            {
                exponent++;
            }
            else
            {     
                // Normal numbers; leave exponent as it is but add extra
                // bit to the front of the mantissa
                mantissa = mantissa | (1L << 52);
            }

            // Bias the exponent. It's actually biased by 1023, but we're
            // treating the mantissa as m.0 rather than 0.m, so we need
            // to subtract another 52 from it.
            exponent -= 1075;

            if (mantissa == 0)
            {
                return "0";
            }

            // Normalize
            while ((mantissa & 1) == 0)
            {    //  i.e., Mantissa is even
                mantissa >>= 1;
                exponent++;
            }

            // Construct a new decimal expansion with the mantissa
            ArbitraryDecimal ad = new ArbitraryDecimal(mantissa);

            // If the exponent is less than 0, we need to repeatedly
            // divide by 2 - which is the equivalent of multiplying
            // by 5 and dividing by 10.
            if (exponent < 0)
            {
                for (int i = 0; i < -exponent; i++)
                    ad.MultiplyBy(5);
                ad.Shift(-exponent);
            }
            // Otherwise, we need to repeatedly multiply by 2
            else
            {
                for (int i = 0; i < exponent; i++)
                    ad.MultiplyBy(2);
            }

            // Finally, return the string with an appropriate sign
            if (negative)
                return "-" + ad.ToString();
            else
                return ad.ToString();
        }

        /// <summary>
        /// Parses a floating point binary string into a double.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static double FromFloatingPointBinaryString(string s)
        {
            double sign;
            double val = 0.0;

            var binRegex = "(?<Sign>[+-]?)(?<Whole>[01]+).?(?<Fraction>[01]*)";
            var match = Regex.Match(s, binRegex);

            if (!match.Success) throw new ArgumentException("s");
            
            sign = match.Groups["Sign"].Value == "-" ? -1.0 : 1.0;
            if (match.Groups["Fraction"].Success)
            {
                var fraction = match.Groups["Fraction"].Value;
                for (int i = 0; i < fraction.Length; i++)
                {
                    if (fraction[i] == '1')
                    {
                        val += Math.Pow(2.0, -i-1);
                    }
                }
            }
            var whole = match.Groups["Whole"].Value;
            for (int i = 0; i < whole.Length; i++)
            {
                if (whole[whole.Length - i - 1] == '1')
                {
                    val += Math.Pow(2.0, i);
                }
            }
            return sign * val;
        }
        
        /// <summary>
        /// Formats a double as a floating point binary string, e.g. 0.25 -> 0.01
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        /// <example>4    = 1.2^2    -> 100
        /// 0.25 = 1.2^-2   -> 0.01
        /// </example>
        public static string ToFloatingPointBinaryString(this double d)
        {
            var dc = new DoubleComponents(d);
            dc.MaximizeExponent();

            var revChars = new List<char>();

            while (dc.Exponent < 0)
            {
                revChars.Add(((dc.Mantissa & 1L) == 1L) ? '1' : '0');
                dc.Exponent++;
                dc.Mantissa >>= 1;
            }
            revChars.Add('.');
            // Write out 0s as long as the exponent suggests
            while (dc.Exponent > 0)
            {
                revChars.Add('0');
                dc.Exponent--;
            }
            // write out the rest of the Mantissa
            while (dc.Mantissa != 0L)
            {
                revChars.Add( ((dc.Mantissa & 1L) == 1L) ? '1' : '0');
                dc.Mantissa >>= 1;
            }
            
            // Before we reverse - add a '0' to the end (which will go in front) if needed
            if (revChars.Last() == '.') revChars.Add('0');
            
            // Prepend the sign (by adding before we reverse)
            if (dc.Negative) revChars.Add('-');
            revChars.Reverse();
            
            // Now a '0' after the '.' if needed (alternative would be to strip to '.')
            if (revChars.Last() == '.') revChars.Add('0');

            return new string(revChars.ToArray());
        }

        /// <summary>
        /// Repeat a character a number of times
        /// </summary>
        /// <param name="c"></param>
        /// <param name="times"></param>
        /// <returns>String with the character repeat</returns>
        public static string Repeat(this char c, int times)
        {
            return new string(c, times);
        }

        /// <summary>Private class used for manipulating deciaml strings.
        /// From the Jon Skeet! example.
        /// </summary>
        class ArbitraryDecimal
        {
            /// <summary>Digits in the decimal expansion, one byte per digit</summary>
            byte[] digits;
            /// <summary> 
            /// How many digits are *after* the decimal point
            /// </summary>
            int decimalPoint = 0;

            /// <summary> 
            /// Constructs an arbitrary decimal expansion from the given long.
            /// The long must not be negative.
            /// </summary>
            internal ArbitraryDecimal(long x)
            {
                string tmp = x.ToString(CultureInfo.InvariantCulture);
                digits = new byte[tmp.Length];
                for (int i = 0; i < tmp.Length; i++)
                    digits[i] = (byte)(tmp[i] - '0');
                Normalize();
            }

            /// <summary>
            /// Multiplies the current expansion by the given amount, which should
            /// only be 2 or 5.
            /// </summary>
            internal void MultiplyBy(int amount)
            {
                byte[] result = new byte[digits.Length + 1];
                for (int i = digits.Length - 1; i >= 0; i--)
                {
                    int resultDigit = digits[i] * amount + result[i + 1];
                    result[i] = (byte)(resultDigit / 10);
                    result[i + 1] = (byte)(resultDigit % 10);
                }
                if (result[0] != 0)
                {
                    digits = result;
                }
                else
                {
                    Array.Copy(result, 1, digits, 0, digits.Length);
                }
                Normalize();
            }

            /// <summary>
            /// Shifts the decimal point; a negative value makes
            /// the decimal expansion bigger (as fewer digits come after the
            /// decimal place) and a positive value makes the decimal
            /// expansion smaller.
            /// </summary>
            internal void Shift(int amount)
            {
                decimalPoint += amount;
            }

            /// <summary>
            /// Removes leading/trailing zeroes from the expansion.
            /// </summary>
            internal void Normalize()
            {
                int first;
                for (first = 0; first < digits.Length; first++)
                    if (digits[first] != 0)
                        break;
                int last;
                for (last = digits.Length - 1; last >= 0; last--)
                    if (digits[last] != 0)
                        break;

                if (first == 0 && last == digits.Length - 1)
                    return;

                byte[] tmp = new byte[last - first + 1];
                for (int i = 0; i < tmp.Length; i++)
                    tmp[i] = digits[i + first];

                decimalPoint -= digits.Length - (last + 1);
                digits = tmp;
            }

            /// <summary>
            /// Converts the value to a proper decimal string representation.
            /// </summary>
            public override String ToString()
            {
                char[] digitString = new char[digits.Length];
                for (int i = 0; i < digits.Length; i++)
                    digitString[i] = (char)(digits[i] + '0');

                // Simplest case - nothing after the decimal point,
                // and last real digit is non-zero, eg value=35
                if (decimalPoint == 0)
                {
                    return new string(digitString);
                }

                // Fairly simple case - nothing after the decimal
                // point, but some 0s to add, eg value=350
                if (decimalPoint < 0)
                {
                    return new string(digitString) +
                           new string('0', -decimalPoint);
                }

                // Nothing before the decimal point, eg 0.035
                if (decimalPoint >= digitString.Length)
                {
                    return "0." +
                        new string('0', (decimalPoint - digitString.Length)) +
                        new string(digitString);
                }

                // Most complicated case - part of the string comes
                // before the decimal point, part comes after it,
                // eg 3.5
                return new string(digitString, 0,
                                   digitString.Length - decimalPoint) +
                    "." +
                    new string(digitString,
                                digitString.Length - decimalPoint,
                                decimalPoint);
            }
        }
    }
}