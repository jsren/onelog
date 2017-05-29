/* Utils.cs - (c) 2017 James S Renwick
 * -----------------------------------
 * Authors: James S Renwick
 * 
 * Shared utility classes.
 */
using System;
using System.Text.RegularExpressions;

namespace onelog
{
    /// <summary>
    /// Regex extension methods.
    /// </summary>
    public static class RegexExtensions
    {
        private static string stringPattern1 = "(?:\"(?:[^\\\\\"]|(?:\\\\.))*\")";
        private static string stringPattern2 = "(?:'(?:[^\\\\']|(?:\\\\.))*')";

        private static Regex stringRegex1 = new Regex(
            "((?:[^\\\\\"]|(?:\\\\[^\"]))*)\\\\\"", RegexOptions.Compiled);
        private static Regex stringRegex2 = new Regex(
            "((?:[^\\\\']|(?:\\\\[^']))*)\\\\'", RegexOptions.Compiled);

        /// <summary>
        /// Converts a pattern using the non-standard extended escapes (\" \') into a
        /// standard regex pattern.
        /// </summary>
        /// <param name="pattern">The non-standard pattern to convert.</param>
        /// <returns>The equivalent standard regex pattern.</returns>
        public static string EnableStringExtension(string pattern)
        {
            pattern = stringRegex1.Replace(pattern, "$1" + stringPattern1);
            return stringRegex2.Replace(pattern, "$1" + stringPattern2);
        }
    }


    /// <summary>
    /// Object which could be of any one of the given types.
    /// </summary>
    public struct Any<T1, T2, T3>
    {
        private bool t1;
        private bool t2;
        private bool t3;
        private readonly T1 valueAsT1;
        private readonly T2 valueAsT2;
        private readonly T3 valueAsT3;

        public T1 ValueAsT1
        {
            get
            {
                if (!t1) throw new Exception("Value not present for T1");
                else return valueAsT1;
            }
        }
        public T2 ValueAsT2
        {
            get
            {
                if (!t2) throw new Exception("Value not present for T2");
                else return valueAsT2;
            }
        }
        public T3 ValueAsT3
        {
            get
            {
                if (!t3) throw new Exception("Value not present for T3");
                else return valueAsT3;
            }
        }

        public bool IsT1 { get { return t1; } }
        public bool IsT2 { get { return t2; } }
        public bool IsT3 { get { return t3; } }


        public Any(T1 value)
        {
            t1 = true;
            t2 = false;
            t3 = false;
            valueAsT1 = value;
            valueAsT2 = default(T2);
            valueAsT3 = default(T3);
        }
        public Any(T2 value)
        {
            t1 = false;
            t2 = true;
            t3 = false;
            valueAsT1 = default(T1);
            valueAsT2 = value;
            valueAsT3 = default(T3);
        }
        public Any(T3 value)
        {
            t1 = false;
            t2 = false;
            t3 = true;
            valueAsT1 = default(T1);
            valueAsT2 = default(T2);
            valueAsT3 = value;
        }
    }
}
