using System;
using System.Linq;
using EazyEdu.Web.Models;

namespace EazyEdu.Web.Nlu
{
    /// <summary>
    /// Lexicon scorer that decides whether a message is a happy note, a neutral question
    /// or a complaint that deserves a human being.
    /// </summary>
    public class SentimentAnalyzer
    {
        private static readonly string[] Negative =
        {
            "angry", "annoyed", "upset", "frustrated", "frustrating", "useless", "terrible", "worst",
            "horrible", "pathetic", "rude", "unfair", "wrong", "incorrect", "mistake", "error",
            "complaint", "complain", "disappointed", "unacceptable", "harass", "bully", "bullying",
            "not working", "does not work", "no response", "still waiting", "again and again",
            "गलत", "शिकायत", "परेशान"
        };

        private static readonly string[] Positive =
        {
            "thanks", "thank", "great", "awesome", "helpful", "perfect", "excellent", "good job",
            "well done", "nice", "love", "appreciate", "धन्यवाद", "बढ़िया"
        };

        private static readonly string[] HumanRequest =
        {
            "talk to a human", "talk to human", "speak to someone", "real person", "principal",
            "class teacher directly", "school office", "raise a complaint", "raise complaint",
            "escalate", "customer care", "call me"
        };

        public SentimentResult Analyze(string text)
        {
            var result = new SentimentResult { Label = "neutral", Score = 0, NeedsHuman = false };
            if (string.IsNullOrWhiteSpace(text)) return result;

            string normalized = TextNormalizer.Normalize(text);

            int negative = Negative.Count(w => normalized.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0);
            int positive = Positive.Count(w => normalized.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0);
            bool asksForHuman = HumanRequest.Any(w => normalized.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0);

            // Shouting is a signal too, but only when the message is long enough to mean it.
            bool shouting = text.Length > 12 &&
                            text.Count(char.IsUpper) > text.Count(char.IsLetter) * 0.7;

            int net = positive - negative;
            if (net > 0)
            {
                result.Label = "positive";
                result.Score = Math.Min(1.0, net / 3.0);
            }
            else if (net < 0 || shouting)
            {
                result.Label = "negative";
                result.Score = Math.Min(1.0, (Math.Abs(net) + (shouting ? 1 : 0)) / 3.0);
            }

            result.NeedsHuman = asksForHuman || (result.Label == "negative" && (negative >= 2 || shouting));
            return result;
        }
    }
}
