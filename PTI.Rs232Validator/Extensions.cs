namespace PTI.Rs232Validator
{
    using System;
    using System.Text;

    internal static class Extensions
    {
        /// <summary>
        ///     Returns the data formatted as specified by the format string.
        /// </summary>
        /// <param name="arr">byte[]</param>
        /// <param name="delimiter">delimiter such as command or tab</param>
        /// <param name="hexPrefix">Set true to prefix each byte with 0x</param>
        /// <returns></returns>
        public static string ToHexString(this byte[] arr, string delimiter = ", ", bool hexPrefix = false)
        {
            if (arr is null || arr.Length == 0)
            {
                return string.Empty;
            }

            var hex = new StringBuilder(arr.Length * 2);

            var prefix = string.Empty;
            if (hexPrefix)
            {
                prefix = "0x";
            }

            foreach (var b in arr)
            {
                hex.AppendFormat("{0}{1:X2}{2}", prefix, b, delimiter);
            }

            var result = hex.ToString().Trim().TrimEnd(delimiter.ToCharArray());
            return result;
        }

        /// <summary>
        ///     Format byte value as binary
        ///     8 => 0b00001000
        /// </summary>
        /// <param name="b">Value to represent in binary</param>
        /// <returns>Value as binary string</returns>
        public static string ToBinary(this byte b)
        {
            return Convert.ToString(b, 2).PadLeft(8, '0');
        }

        /// <summary>
        ///     Multiple timespan by a constant value as timeSpan * factor
        /// </summary>
        /// <remarks>This is a backwards compatibility feature for NET Framework</remarks>
        /// <param name="timeSpan">Time span</param>
        /// <param name="factor">Factor</param>
        public static TimeSpan _Multiply(this TimeSpan timeSpan, int factor)
        {
            if (factor <= 0)
            {
                throw new ArgumentException($"{nameof(factor)} must be > 0");
            }
            var result = timeSpan;
            for (var i = 0; i < factor; ++i)
            {
                result = result.Add(timeSpan);
            }

            return result;
        }
    }
}