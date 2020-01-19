using System;

namespace OpenIDClient.Extensions
{
    public static class StringExtensions
    {
        public static void MustEqual(this string subject, string expected, string errorMessage)
        {
            if (!subject.Equals(expected)) throw new InvalidOperationException(errorMessage);
        }
    }
}