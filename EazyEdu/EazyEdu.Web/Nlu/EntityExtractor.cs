using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using EazyEdu.Web.Models;
using EazyEdu.Web.Services;

namespace EazyEdu.Web.Nlu
{
    public static class EntityKeys
    {
        public const string Subject = "subject";
        public const string Difficulty = "difficulty";
        public const string TimeFrame = "timeframe";
        public const string Date = "date";
        public const string Child = "child";
        public const string Language = "language";
        public const string Count = "count";
    }

    /// <summary>Pulls subjects, dates, difficulty and child names out of a normalised sentence.</summary>
    public class EntityExtractor
    {
        private static readonly Regex CountPattern = new Regex(@"\b(\d{1,2})\s*(question|mark|day)s?\b", RegexOptions.Compiled);

        private static readonly Dictionary<string, string> SubjectAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "maths", "Mathematics" }, { "math", "Mathematics" }, { "mathematics", "Mathematics" }, { "ganit", "Mathematics" },
            { "science", "Science" }, { "sci", "Science" },
            { "english", "English" }, { "eng", "English" },
            { "hindi", "Hindi" },
            { "sst", "Social Studies" }, { "social", "Social Studies" }, { "civics", "Social Studies" }, { "history", "Social Studies" },
            { "evs", "EVS" }, { "environmental", "EVS" },
            { "computer", "Computer Science" }, { "cs", "Computer Science" }
        };

        public EntitySet Extract(string corrected, string normalized, string rawText, IEnumerable<string> knownSubjects)
        {
            var set = new EntitySet();
            string haystack = corrected ?? string.Empty;

            set.Set(EntityKeys.Subject, MatchSubject(haystack, normalized, rawText, knownSubjects));
            set.Set(EntityKeys.Difficulty, MatchDifficulty(haystack));
            set.Set(EntityKeys.Child, MatchChild(haystack));
            set.Set(EntityKeys.Language, MatchLanguage(haystack));

            string timeframe = MatchTimeFrame(haystack);
            set.Set(EntityKeys.TimeFrame, timeframe);

            DateTime? date = ResolveDate(haystack, timeframe);
            if (date.HasValue) set.Set(EntityKeys.Date, date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            Match count = CountPattern.Match(haystack);
            if (count.Success) set.Set(EntityKeys.Count, count.Groups[1].Value);

            return set;
        }

        private static string MatchSubject(string corrected, string normalized, string rawText, IEnumerable<string> knownSubjects)
        {
            // The student's own subject list wins, so "EVS" only resolves for a class that has it.
            if (knownSubjects != null)
            {
                foreach (string subject in knownSubjects)
                {
                    if (string.IsNullOrEmpty(subject)) continue;
                    if (Contains(corrected, subject.ToLowerInvariant()) ||
                        Contains(normalized, subject.ToLowerInvariant()) ||
                        (rawText != null && rawText.IndexOf(subject, StringComparison.OrdinalIgnoreCase) >= 0))
                        return subject;
                }
            }

            foreach (var alias in SubjectAliases)
            {
                if (Contains(corrected, alias.Key) || Contains(normalized, alias.Key))
                {
                    if (knownSubjects == null) return alias.Value;

                    string match = knownSubjects.FirstOrDefault(s => string.Equals(s, alias.Value, StringComparison.OrdinalIgnoreCase));
                    if (match != null) return match;
                }
            }

            return null;
        }

        private static string MatchDifficulty(string text)
        {
            if (Contains(text, "easy") || Contains(text, "simple") || Contains(text, "basic")) return "easy";
            if (Contains(text, "hard") || Contains(text, "difficult") || Contains(text, "tough") || Contains(text, "advanced")) return "hard";
            if (Contains(text, "medium") || Contains(text, "moderate")) return "medium";
            return null;
        }

        private static string MatchChild(string text)
        {
            if (Contains(text, "aarav")) return "aarav";
            if (Contains(text, "isha")) return "isha";
            return null;
        }

        private static string MatchLanguage(string text)
        {
            if (Contains(text, "hindi")) return "hi";
            if (Contains(text, "english")) return "en";
            return null;
        }

        private static string MatchTimeFrame(string text)
        {
            if (Contains(text, "today")) return "today";
            if (Contains(text, "tomorrow")) return "tomorrow";
            if (Contains(text, "yesterday")) return "yesterday";
            if (Contains(text, "next week")) return "next-week";
            if (Contains(text, "this week")) return "this-week";
            if (Contains(text, "this month") || Contains(text, "month")) return "this-month";
            if (Contains(text, "term")) return "term";
            return null;
        }

        private static DateTime? ResolveDate(string text, string timeframe)
        {
            switch (timeframe)
            {
                case "today": return Clock.Today;
                case "tomorrow": return Clock.Today.AddDays(1);
                case "yesterday": return Clock.Today.AddDays(-1);
            }

            var explicitDate = Regex.Match(text, @"\b(\d{1,2})\s+(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)[a-z]*\b");
            if (explicitDate.Success)
            {
                int day = int.Parse(explicitDate.Groups[1].Value, CultureInfo.InvariantCulture);
                int month = MonthNumber(explicitDate.Groups[2].Value);
                if (month > 0 && day >= 1 && day <= DateTime.DaysInMonth(Clock.Today.Year, month))
                    return new DateTime(Clock.Today.Year, month, day);
            }

            return null;
        }

        private static int MonthNumber(string abbreviation)
        {
            switch (abbreviation.ToLowerInvariant())
            {
                case "jan": return 1;
                case "feb": return 2;
                case "mar": return 3;
                case "apr": return 4;
                case "may": return 5;
                case "jun": return 6;
                case "jul": return 7;
                case "aug": return 8;
                case "sep": return 9;
                case "oct": return 10;
                case "nov": return 11;
                case "dec": return 12;
                default: return 0;
            }
        }

        private static bool Contains(string haystack, string needle)
        {
            return !string.IsNullOrEmpty(haystack) && haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
