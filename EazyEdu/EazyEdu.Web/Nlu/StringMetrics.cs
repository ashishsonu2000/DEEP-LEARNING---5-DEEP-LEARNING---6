using System;
using System.Collections.Generic;
using System.Linq;

namespace EazyEdu.Web.Nlu
{
    /// <summary>Small string-distance helpers used for typo tolerance and phrase matching.</summary>
    public static class StringMetrics
    {
        /// <summary>Classic Levenshtein edit distance, computed with two rolling rows.</summary>
        public static int Levenshtein(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return string.IsNullOrEmpty(b) ? 0 : b.Length;
            if (string.IsNullOrEmpty(b)) return a.Length;

            int[] previous = new int[b.Length + 1];
            int[] current = new int[b.Length + 1];

            for (int j = 0; j <= b.Length; j++) previous[j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                current[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
                }

                int[] swap = previous;
                previous = current;
                current = swap;
            }

            return previous[b.Length];
        }

        /// <summary>1.0 for identical strings, 0.0 for completely different ones.</summary>
        public static double Similarity(string a, string b)
        {
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return 1.0;
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0.0;

            int longest = Math.Max(a.Length, b.Length);
            return 1.0 - (double)Levenshtein(a, b) / longest;
        }

        /// <summary>Overlap of two token sets: |A ∩ B| / |A ∪ B|.</summary>
        public static double Jaccard(IEnumerable<string> left, IEnumerable<string> right)
        {
            var a = new HashSet<string>(left, StringComparer.OrdinalIgnoreCase);
            var b = new HashSet<string>(right, StringComparer.OrdinalIgnoreCase);
            if (a.Count == 0 || b.Count == 0) return 0.0;

            int intersection = a.Count(x => b.Contains(x));
            int union = a.Count + b.Count - intersection;
            return union == 0 ? 0.0 : (double)intersection / union;
        }
    }
}
