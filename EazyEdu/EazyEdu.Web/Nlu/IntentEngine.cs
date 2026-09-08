using System;
using System.Collections.Generic;
using System.Linq;
using EazyEdu.Web.Models;

namespace EazyEdu.Web.Nlu
{
    /// <summary>
    /// Hybrid rule + similarity classifier. Every intent is scored three ways — regex trigger,
    /// weighted keyword coverage and example-phrase overlap — and the best of the three wins.
    /// It is deterministic, explainable and needs no model file, which is what a school
    /// deployment on-premise usually wants.
    /// </summary>
    public class IntentEngine
    {
        /// <summary>Below this the answer becomes a "did you mean" fallback.</summary>
        public const double ConfidenceFloor = 0.34;

        private readonly IList<IntentDefinition> _intents;
        private readonly EntityExtractor _entities;

        public IntentEngine()
            : this(IntentCatalog.Build(), new EntityExtractor())
        {
        }

        public IntentEngine(IList<IntentDefinition> intents, EntityExtractor entityExtractor)
        {
            if (intents == null) throw new ArgumentNullException("intents");
            if (entityExtractor == null) throw new ArgumentNullException("entityExtractor");
            _intents = intents;
            _entities = entityExtractor;
        }

        /// <param name="expectedIntent">
        /// Intent the assistant is waiting on (e.g. it just asked which subject);
        /// matches for it get a small boost so short follow-ups land in the right place.
        /// </param>
        public IntentResult Recognize(string text, IEnumerable<string> knownSubjects, string expectedIntent)
        {
            var result = new IntentResult();

            string normalized = TextNormalizer.Normalize(text);
            string corrected = TextNormalizer.Correct(normalized);
            result.NormalizedText = normalized;
            result.CorrectedText = corrected;

            if (string.IsNullOrEmpty(corrected))
            {
                result.Intent = Intents.Fallback;
                result.Confidence = 0;
                return result;
            }

            string[] tokens = TextNormalizer.Tokenize(corrected);
            result.Entities = _entities.Extract(corrected, normalized, text, knownSubjects);

            // A short answer naming a subject, right after the assistant asked "which subject?",
            // is that answer — not a new topic. Without this, "Hindi" would read as a request
            // to switch language rather than as the subject for the practice paper.
            bool asksForLanguage = result.Entities.Has(EntityKeys.Language) && tokens.Length > 1;
            if (expectedIntent != null && tokens.Length <= 3 && !asksForLanguage &&
                result.Entities.Has(EntityKeys.Subject))
            {
                result.Intent = expectedIntent;
                result.Confidence = 0.85;
                result.Alternatives.Add(new IntentMatch { Intent = expectedIntent, Score = 0.85 });
                return result;
            }

            var scores = new List<IntentMatch>();

            foreach (IntentDefinition intent in _intents)
            {
                double score = Score(intent, corrected, normalized, tokens);
                if (score <= 0) continue;

                if (expectedIntent != null && intent.Name == expectedIntent)
                    score = Math.Min(0.99, score + 0.18);

                scores.Add(new IntentMatch { Intent = intent.Name, Score = Math.Round(score, 4) });
            }

            if (scores.Count == 0 && expectedIntent != null && result.Entities.Has(EntityKeys.Subject))
                scores.Add(new IntentMatch { Intent = expectedIntent, Score = 0.72 });

            result.Alternatives = scores.OrderByDescending(s => s.Score).Take(4).ToList();

            IntentMatch top = result.Alternatives.FirstOrDefault();
            if (top == null || top.Score < ConfidenceFloor)
            {
                result.Intent = Intents.Fallback;
                result.Confidence = top == null ? 0 : top.Score;
            }
            else
            {
                result.Intent = top.Intent;
                result.Confidence = top.Score;
            }

            return result;
        }

        private double Score(IntentDefinition intent, string corrected, string normalized, string[] tokens)
        {
            double patternScore = 0;
            foreach (var pattern in intent.Patterns)
            {
                if (pattern.IsMatch(corrected) || pattern.IsMatch(normalized))
                {
                    patternScore = 0.9;
                    break;
                }
            }

            int hits = intent.Keywords.Count(k => ContainsKeyword(tokens, corrected, k));
            double keywordScore = hits == 0 ? 0 : Math.Min(0.86, 0.42 + 0.16 * (hits - 1));

            double exampleScore = 0;
            foreach (string example in intent.Examples)
            {
                string[] exampleTokens = TextNormalizer.Tokenize(TextNormalizer.Normalize(example));
                double overlap = StringMetrics.Jaccard(tokens, exampleTokens);
                double fuzzy = StringMetrics.Similarity(corrected, TextNormalizer.Normalize(example));
                double combined = Math.Max(overlap, fuzzy * 0.85);
                if (combined > exampleScore) exampleScore = combined;
            }
            exampleScore *= 0.92;

            double score = Math.Max(patternScore, Math.Max(keywordScore, exampleScore));
            if (score <= 0) return 0;

            // Two independent signals agreeing is worth more than either alone.
            int signals = (patternScore > 0 ? 1 : 0) + (hits > 0 ? 1 : 0) + (exampleScore > 0.35 ? 1 : 0);
            if (signals >= 2) score = Math.Min(0.99, score + 0.05 * (signals - 1));

            if (intent.NegativeKeywords.Any(k => ContainsKeyword(tokens, corrected, k)))
                score *= 0.35;

            score *= intent.Weight;
            return Math.Min(0.99, score);
        }

        private static bool ContainsKeyword(string[] tokens, string corrected, string keyword)
        {
            if (keyword.IndexOf(' ') >= 0)
                return corrected.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;

            return tokens.Any(t => string.Equals(t, keyword, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Human-readable labels for the "did you mean" chips.</summary>
        public static string Describe(string intent)
        {
            switch (intent)
            {
                case Intents.FeeStatus: return "Fee status";
                case Intents.FeeBreakdown: return "Fee breakdown";
                case Intents.FeeReceipt: return "Fee receipt";
                case Intents.FeeHistory: return "Payment history";
                case Intents.ExamSchedule: return "Exam schedule";
                case Intents.ExamSyllabus: return "Exam syllabus";
                case Intents.PracticeRequest: return "Practice paper";
                case Intents.PracticeAnswerKey: return "Answer key";
                case Intents.StudyTips: return "Study tips";
                case Intents.AttendanceStatus: return "Attendance";
                case Intents.AttendanceDetail: return "Attendance log";
                case Intents.HomeworkList: return "Homework";
                case Intents.MarksReport: return "Marks";
                case Intents.NoticeList: return "Notices";
                case Intents.Timetable: return "Timetable";
                case Intents.Transport: return "Bus route";
                case Intents.Library: return "Library";
                case Intents.TeacherContact: return "Class teacher";
                case Intents.Meeting: return "Parent-teacher meeting";
                case Intents.Transcript: return "Chat transcript";
                case Intents.Escalate: return "Talk to the office";
                case Intents.Help: return "What I can do";
                default: return intent;
            }
        }

        /// <summary>The query text a "did you mean" chip should send back.</summary>
        public static string SampleQuery(string intent)
        {
            switch (intent)
            {
                case Intents.FeeStatus: return "What is my fee status?";
                case Intents.FeeBreakdown: return "Show the fee breakdown";
                case Intents.FeeReceipt: return "Download my fee receipt";
                case Intents.FeeHistory: return "Show my payment history";
                case Intents.ExamSchedule: return "When are the exams?";
                case Intents.ExamSyllabus: return "What is the exam syllabus?";
                case Intents.PracticeRequest: return "Give me a practice test";
                case Intents.PracticeAnswerKey: return "Show the answer key";
                case Intents.StudyTips: return "Give me study tips";
                case Intents.AttendanceStatus: return "What is my attendance?";
                case Intents.AttendanceDetail: return "Show my attendance log";
                case Intents.HomeworkList: return "What homework is due?";
                case Intents.MarksReport: return "Show my marks";
                case Intents.NoticeList: return "Any notices?";
                case Intents.Timetable: return "What is my timetable today?";
                case Intents.Transport: return "What is my bus route?";
                case Intents.Library: return "What library books do I have?";
                case Intents.TeacherContact: return "Who is my class teacher?";
                case Intents.Meeting: return "When is the PTM?";
                case Intents.Transcript: return "Download the chat transcript";
                case Intents.Escalate: return "I want to talk to the office";
                default: return "Help";
            }
        }
    }
}
