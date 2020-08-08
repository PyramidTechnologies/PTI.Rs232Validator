namespace PTI.Rs232Validator.Internal
{
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
    }
}