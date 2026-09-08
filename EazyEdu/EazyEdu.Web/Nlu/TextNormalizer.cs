using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EazyEdu.Web.Nlu
{
    /// <summary>
    /// Turns whatever the user typed into a predictable bag of words: contractions expanded,
    /// SMS shorthand spelled out, punctuation dropped, obvious typos repaired.
    /// </summary>
    public static class TextNormalizer
    {
        private static readonly Regex NonWord = new Regex(@"[^\p{L}\p{Nd}\s]+", RegexOptions.Compiled);
        private static readonly Regex Spaces = new Regex(@"\s+", RegexOptions.Compiled);

        private static readonly Dictionary<string, string> Contractions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "whats", "what is" }, { "what's", "what is" }, { "wheres", "where is" }, { "where's", "where is" },
            { "hows", "how is" }, { "how's", "how is" }, { "whens", "when is" }, { "when's", "when is" },
            { "im", "i am" }, { "i'm", "i am" }, { "ive", "i have" }, { "i've", "i have" },
            { "dont", "do not" }, { "don't", "do not" }, { "doesnt", "does not" }, { "doesn't", "does not" },
            { "cant", "can not" }, { "can't", "can not" }, { "isnt", "is not" }, { "isn't", "is not" },
            { "wasnt", "was not" }, { "havent", "have not" }, { "haven't", "have not" }, { "wont", "will not" }
        };

        private static readonly Dictionary<string, string> Shorthand = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "hw", "homework" }, { "att", "attendance" }, { "attndnce", "attendance" },
            { "plz", "please" }, { "pls", "please" }, { "thx", "thanks" }, { "ty", "thanks" },
            { "u", "you" }, { "ur", "your" }, { "r", "are" }, { "pymt", "payment" },
            { "fees", "fee" }, { "fee's", "fee" }, { "exams", "exam" }, { "marks", "mark" },
            { "notices", "notice" }, { "assignments", "assignment" }, { "books", "book" },
            { "results", "result" }, { "grades", "grade" }, { "subjects", "subject" },
            { "dates", "date" }, { "days", "day" }, { "tests", "test" }, { "papers", "paper" },
            { "ques", "question" }, { "qs", "question" }, { "sched", "schedule" },
            { "tt", "timetable" }, { "ptm", "parent teacher meeting" }, { "rcpt", "receipt" }
        };

        /// <summary>Vocabulary the spell-corrector snaps unknown words to.</summary>
        private static readonly string[] Vocabulary =
        {
            "fee", "payment", "receipt", "balance", "due", "installment", "breakdown", "refund",
            "exam", "schedule", "datesheet", "timetable", "syllabus", "room", "test", "paper",
            "practice", "question", "answer", "key", "difficulty", "easy", "medium", "hard",
            "attendance", "present", "absent", "leave", "percentage", "homework", "assignment",
            "submission", "notice", "announcement", "circular", "bus", "transport", "route",
            "pickup", "drop", "library", "book", "return", "fine", "mark", "grade", "result",
            "report", "card", "progress", "teacher", "contact", "meeting", "holiday", "help",
            "download", "summary", "today", "tomorrow", "week", "month", "term", "subject",
            "mathematics", "science", "english", "hindi", "social", "studies", "evs", "computer",
            "principal", "complaint", "problem", "wrong", "language", "history", "transcript"
        };

        private static readonly HashSet<string> VocabularySet =
            new HashSet<string>(Vocabulary, StringComparer.OrdinalIgnoreCase);

        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            string lowered = text.Trim().ToLowerInvariant();

            foreach (var pair in Contractions)
                lowered = Regex.Replace(lowered, @"\b" + Regex.Escape(pair.Key) + @"\b", pair.Value);

            lowered = NonWord.Replace(lowered, " ");
            lowered = Spaces.Replace(lowered, " ").Trim();

            var words = lowered.Split(' ')
                               .Where(w => w.Length > 0)
                               .Select(ExpandShorthand);

            return string.Join(" ", words);
        }

        private static string ExpandShorthand(string word)
        {
            string expanded;
            return Shorthand.TryGetValue(word, out expanded) ? expanded : word;
        }

        public static string[] Tokenize(string normalizedText)
        {
            if (string.IsNullOrEmpty(normalizedText)) return new string[0];
            return normalizedText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Snaps near-miss words onto the known vocabulary ("attendence" → "attendance").
        /// Only latin-script words of four characters or more are touched.
        /// </summary>
        public static string Correct(string normalizedText)
        {
            string[] tokens = Tokenize(normalizedText);
            if (tokens.Length == 0) return normalizedText;

            var builder = new StringBuilder();
            for (int i = 0; i < tokens.Length; i++)
            {
                if (i > 0) builder.Append(' ');
                builder.Append(CorrectToken(tokens[i]));
            }
            return builder.ToString();
        }

        private static string CorrectToken(string token)
        {
            if (token.Length < 4) return token;
            if (VocabularySet.Contains(token)) return token;
            if (token.Any(c => c > 127)) return token;   // leave Devanagari and friends alone

            int budget = token.Length <= 5 ? 1 : 2;
            string best = null;
            int bestDistance = int.MaxValue;

            foreach (string candidate in Vocabulary)
            {
                if (Math.Abs(candidate.Length - token.Length) > budget) continue;

                int distance = StringMetrics.Levenshtein(token, candidate);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return bestDistance <= budget && best != null ? best : token;
        }
    }
}
