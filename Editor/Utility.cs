using System;
using UnityEngine;

namespace UnityEditor.Performance.ProfileAnalyzer
{
    internal class Utility
    {
        internal static bool IsNullOrWhiteSpace(string s)
        {
            if (string.IsNullOrEmpty(s))
                return true;

            for (int i = 0; i < s.Length; i++)
            {
                if (!char.IsWhiteSpace(s[i]))
                    return false;
            }

            return true;
        }
    }
}
