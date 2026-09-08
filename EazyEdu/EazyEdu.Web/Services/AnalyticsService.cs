using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using EazyEdu.Web.Data;
using EazyEdu.Web.Models;
using EazyEdu.Web.Nlu;

namespace EazyEdu.Web.Services
{
    public class IntentStat
    {
        public string Intent { get; set; }
        public string Label { get; set; }
        public int Count { get; set; }
        public double AverageConfidence { get; set; }
        public double Share { get; set; }
    }

    public class AnalyticsSummary
    {
        public AnalyticsSummary()
        {
            TopIntents = new List<IntentStat>();
            UnrecognizedQueries = new List<string>();
            HourlyVolume = new int[24];
        }

        public int TotalTurns { get; set; }
        public int UserTurns { get; set; }
        public int Sessions { get; set; }
        public double AverageConfidence { get; set; }
        public double FallbackRate { get; set; }
        public int ThumbsUp { get; set; }
        public int ThumbsDown { get; set; }
        public double AverageResponseMs { get; set; }
        public int NegativeMessages { get; set; }
        public List<IntentStat> TopIntents { get; set; }
        public List<string> UnrecognizedQueries { get; set; }
        public int[] HourlyVolume { get; set; }
    }

    /// <summary>Rolls the chat log up into the numbers shown on the admin dashboard.</summary>
    public class AnalyticsService
    {
        private readonly ChatLogStore _log;

        public AnalyticsService(ChatLogStore log)
        {
            if (log == null) throw new ArgumentNullException("log");
            _log = log;
        }

        public AnalyticsSummary Summarize()
        {
            IList<ChatTurn> turns = _log.All();
            var summary = new AnalyticsSummary { TotalTurns = turns.Count };

            var userTurns = turns.Where(t => string.Equals(t.Author, "user", StringComparison.OrdinalIgnoreCase)).ToList();
            var botTurns = turns.Where(t => string.Equals(t.Author, "bot", StringComparison.OrdinalIgnoreCase)).ToList();

            summary.UserTurns = userTurns.Count;
            summary.Sessions = turns.Select(t => t.SessionId).Distinct(StringComparer.Ordinal).Count();
            summary.ThumbsUp = turns.Count(t => string.Equals(t.Feedback, "up", StringComparison.OrdinalIgnoreCase));
            summary.ThumbsDown = turns.Count(t => string.Equals(t.Feedback, "down", StringComparison.OrdinalIgnoreCase));
            summary.NegativeMessages = userTurns.Count(t => string.Equals(t.Sentiment, "negative", StringComparison.OrdinalIgnoreCase));

            if (userTurns.Count > 0)
            {
                summary.AverageConfidence = Math.Round(userTurns.Average(t => t.Confidence), 3);
                summary.FallbackRate = Math.Round(
                    userTurns.Count(t => string.Equals(t.Intent, Intents.Fallback, StringComparison.OrdinalIgnoreCase)) * 100.0 / userTurns.Count, 1);
            }

            if (botTurns.Count > 0)
                summary.AverageResponseMs = Math.Round(botTurns.Average(t => (double)t.ElapsedMs), 1);

            summary.TopIntents = userTurns
                .Where(t => !string.IsNullOrEmpty(t.Intent))
                .GroupBy(t => t.Intent, StringComparer.OrdinalIgnoreCase)
                .Select(g => new IntentStat
                {
                    Intent = g.Key,
                    Label = IntentEngine.Describe(g.Key),
                    Count = g.Count(),
                    AverageConfidence = Math.Round(g.Average(t => t.Confidence), 3),
                    Share = userTurns.Count == 0 ? 0 : Math.Round(g.Count() * 100.0 / userTurns.Count, 1)
                })
                .OrderByDescending(s => s.Count)
                .Take(12)
                .ToList();

            summary.UnrecognizedQueries = userTurns
                .Where(t => string.Equals(t.Intent, Intents.Fallback, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.TimestampUtc)
                .Select(t => t.Text)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Take(15)
                .ToList();

            foreach (ChatTurn turn in userTurns)
            {
                int hour = turn.TimestampUtc.ToLocalTime().Hour;
                if (hour >= 0 && hour < 24) summary.HourlyVolume[hour]++;
            }

            return summary;
        }

        public string ExportCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("timestamp_utc,session,author,role,student,intent,confidence,sentiment,elapsed_ms,feedback,text");

            foreach (ChatTurn turn in _log.All().OrderBy(t => t.TimestampUtc))
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    turn.TimestampUtc.ToString("s", CultureInfo.InvariantCulture),
                    Csv(turn.SessionId),
                    Csv(turn.Author),
                    Csv(turn.Role),
                    Csv(turn.StudentId),
                    Csv(turn.Intent),
                    turn.Confidence.ToString("F3", CultureInfo.InvariantCulture),
                    Csv(turn.Sentiment),
                    turn.ElapsedMs.ToString(CultureInfo.InvariantCulture),
                    Csv(turn.Feedback),
                    Csv(turn.Text)
                }));
            }

            return sb.ToString();
        }

        private static string Csv(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            string escaped = value.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ");
            return "\"" + escaped + "\"";
        }
    }
}
