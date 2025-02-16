using System;

namespace KMPCommon
{
    public static class Conversion
    {
        public static object Parse(this Type type, string s)
        {
            if (type == typeof(decimal))
            {
                return Convert.ToDecimal(s);
            }
            else if (type == typeof(DateTime))
            {
                return DateTime.Parse(s);
            }

            throw new NotSupportedException($"Converting {type} to string is not supported");
        }
    }
}