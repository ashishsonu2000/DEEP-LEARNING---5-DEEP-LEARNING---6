using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EazyEdu.Web.Nlu
{
    /// <summary>Names of every intent the assistant can act on.</summary>
    public static class Intents
    {
        public const string Greeting = "greeting";
        public const string Identity = "smalltalk.identity";
        public const string Thanks = "smalltalk.thanks";
        public const string Goodbye = "smalltalk.goodbye";
        public const string Help = "help.capabilities";

        public const string FeeStatus = "fee.status";
        public const string FeeBreakdown = "fee.breakdown";
        public const string FeeReceipt = "fee.receipt";
        public const string FeeHistory = "fee.history";

        public const string ExamSchedule = "exam.schedule";
        public const string ExamSyllabus = "exam.syllabus";

        public const string PracticeRequest = "practice.request";
        public const string PracticeAnswerKey = "practice.answerkey";
        public const string StudyTips = "study.tips";

        public const string AttendanceStatus = "attendance.status";
        public const string AttendanceDetail = "attendance.detail";

        public const string HomeworkList = "homework.list";
        public const string MarksReport = "marks.report";
        public const string NoticeList = "notice.list";
        public const string Timetable = "timetable.today";
        public const string Transport = "transport.info";
        public const string Library = "library.info";
        public const string TeacherContact = "teacher.contact";
        public const string Meeting = "meeting.ptm";

        public const string SwitchStudent = "profile.switch";
        public const string SwitchLanguage = "profile.language";
        public const string Transcript = "chat.transcript";
        public const string Escalate = "support.escalate";

        public const string Fallback = "fallback";
    }

    /// <summary>One trainable intent: regex triggers, weighted keywords and example phrases.</summary>
    public class IntentDefinition
    {
        public IntentDefinition(string name)
        {
            Name = name;
            Keywords = new List<string>();
            NegativeKeywords = new List<string>();
            Examples = new List<string>();
            Patterns = new List<Regex>();
            Weight = 1.0;
        }

        public string Name { get; private set; }
        public List<string> Keywords { get; private set; }
        public List<string> NegativeKeywords { get; private set; }
        public List<string> Examples { get; private set; }
        public List<Regex> Patterns { get; private set; }
        /// <summary>Tie-breaker for intents that share vocabulary (e.g. fee.status vs fee.breakdown).</summary>
        public double Weight { get; set; }

        public IntentDefinition WithKeywords(params string[] keywords)
        {
            Keywords.AddRange(keywords);
            return this;
        }

        public IntentDefinition Without(params string[] keywords)
        {
            NegativeKeywords.AddRange(keywords);
            return this;
        }

        public IntentDefinition WithExamples(params string[] examples)
        {
            Examples.AddRange(examples);
            return this;
        }

        public IntentDefinition WithPattern(string pattern)
        {
            Patterns.Add(new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase));
            return this;
        }

        public IntentDefinition WithWeight(double weight)
        {
            Weight = weight;
            return this;
        }
    }
}
