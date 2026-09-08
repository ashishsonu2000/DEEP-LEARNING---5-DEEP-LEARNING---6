using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using EazyEdu.Web.Data;
using EazyEdu.Web.Models;
using EazyEdu.Web.Nlu;

namespace EazyEdu.Web.Services
{
    /// <summary>
    /// The assistant's brain: classify, look the answer up in the school records,
    /// and render it as a bubble with follow-up chips.
    /// </summary>
    public class ChatService
    {
        private static readonly CultureInfo India = CultureInfo.GetCultureInfo("en-IN");
        private static readonly Regex TagStripper = new Regex("<[^>]+>", RegexOptions.Compiled);

        private readonly ISchoolRepository _repository;
        private readonly IntentEngine _engine;
        private readonly SentimentAnalyzer _sentiment;
        private readonly PracticePaperService _practice;
        private readonly ChatLogStore _log;
        private readonly TicketStore _tickets;
        private readonly IAiCompletionProvider _ai;

        public ChatService(
            ISchoolRepository repository,
            IntentEngine engine,
            SentimentAnalyzer sentiment,
            PracticePaperService practice,
            ChatLogStore log,
            TicketStore tickets,
            IAiCompletionProvider ai)
        {
            if (repository == null) throw new ArgumentNullException("repository");
            if (engine == null) throw new ArgumentNullException("engine");
            if (sentiment == null) throw new ArgumentNullException("sentiment");
            if (practice == null) throw new ArgumentNullException("practice");
            if (log == null) throw new ArgumentNullException("log");
            if (tickets == null) throw new ArgumentNullException("tickets");

            _repository = repository;
            _engine = engine;
            _sentiment = sentiment;
            _practice = practice;
            _log = log;
            _tickets = tickets;
            _ai = ai ?? new NullAiCompletionProvider();
        }

        public string Greeting(ConversationState state, UserAccount user)
        {
            Student student = _repository.GetStudent(state.StudentId);
            if (student == null) return Localization.Get(state.Language, "error.nostudent");

            if (string.Equals(state.Role, UserRoles.Student, StringComparison.OrdinalIgnoreCase))
                return Localization.Get(state.Language, "greeting.student", Escape(student.FirstName));

            return Localization.Get(state.Language, "greeting.parent", DayPart(), Escape(student.Name), Escape(student.ClassName));
        }

        public ChatResponse Respond(ChatRequest request, UserAccount user, ConversationState state, string sessionId)
        {
            var watch = Stopwatch.StartNew();
            var response = new ChatResponse { Ok = true, Source = "rules" };

            Student student = _repository.GetStudent(state.StudentId);
            if (student == null)
            {
                response.Html = Localization.Get(state.Language, "error.nostudent");
                response.Intent = Intents.Fallback;
                response.PlainText = ToPlainText(response.Html);
                response.ServerTime = Clock.Now.ToString("HH:mm", India);
                return response;
            }

            string text = (request.Message ?? string.Empty).Trim();
            IntentResult intent = _engine.Recognize(text, student.Subjects, state.PendingIntent);
            SentimentResult sentiment = _sentiment.Analyze(text);

            response.Intent = intent.Intent;
            response.Confidence = Math.Round(intent.Confidence, 3);
            response.Sentiment = sentiment.Label;

            LogTurn(sessionId, user, state, "user", text, intent.Intent, intent.Confidence, sentiment.Label, 0);

            // A complaint outranks whatever the sentence was nominally about.
            if (sentiment.NeedsHuman || intent.Intent == Intents.Escalate)
            {
                Escalate(response, state, student, user, text);
            }
            else
            {
                Dispatch(response, intent, state, student, user, text);
            }

            state.Remember(response.Intent);
            if (intent.Entities.Has(EntityKeys.Subject)) state.LastSubject = intent.Entities.Get(EntityKeys.Subject);

            // Three fallbacks in a row means the rules are not going to get there.
            if (response.Intent == Intents.Fallback && state.IsLooping(Intents.Fallback) && !response.Escalated)
            {
                response.Html += "<br><br>" + Localization.Get(state.Language, "escalate.offer");
                response.Replies.Add(new QuickReply { Label = "🙋 Talk to the office", Query = "I want to talk to the office" });
            }

            watch.Stop();
            response.ElapsedMs = watch.ElapsedMilliseconds;
            response.PlainText = ToPlainText(response.Html);
            response.ServerTime = Clock.Now.ToString("HH:mm", India);
            response.TurnId = LogTurn(sessionId, user, state, "bot", response.PlainText, response.Intent,
                                      response.Confidence, sentiment.Label, response.ElapsedMs);

            return response;
        }

        // ------------------------------------------------------------------ dispatch

        private void Dispatch(ChatResponse response, IntentResult intent, ConversationState state,
                              Student student, UserAccount user, string rawText)
        {
            string lang = state.Language;

            switch (intent.Intent)
            {
                case Intents.Greeting:
                    response.Html = Greeting(state, user);
                    response.Replies.AddRange(DefaultChips(lang));
                    state.ClearPending();
                    break;

                case Intents.Identity:
                    response.Html = Localization.Get(lang, "identity");
                    response.Replies.AddRange(DefaultChips(lang));
                    break;

                case Intents.Thanks:
                    response.Html = Localization.Get(lang, "thanks");
                    break;

                case Intents.Goodbye:
                    response.Html = Localization.Get(lang, "goodbye");
                    break;

                case Intents.Help:
                    BuildHelp(response, state, student);
                    break;

                case Intents.FeeStatus:
                    BuildFeeStatus(response, state, student);
                    break;

                case Intents.FeeBreakdown:
                    BuildFeeBreakdown(response, state, student);
                    break;

                case Intents.FeeReceipt:
                    response.Html = "Your fee statement is ready to download — it lists the head-wise breakdown and every payment received.";
                    response.Replies.Add(new QuickReply { Label = "⬇️ Download statement", Action = "download", Argument = "receipt" });
                    response.Replies.Add(new QuickReply("Show fee status", "What is my fee status?"));
                    break;

                case Intents.FeeHistory:
                    BuildFeeHistory(response, state, student);
                    break;

                case Intents.ExamSchedule:
                    BuildExamSchedule(response, state, student, intent);
                    break;

                case Intents.ExamSyllabus:
                    BuildSyllabus(response, state, student, intent);
                    break;

                case Intents.PracticeRequest:
                    BuildPracticePaper(response, state, student, intent);
                    break;

                case Intents.PracticeAnswerKey:
                    BuildAnswerKey(response, state, student, intent);
                    break;

                case Intents.StudyTips:
                    response.Html = Localization.Get(lang, "studytips");
                    response.Replies.Add(new QuickReply(Localization.Get(lang, "chip.practice"), "Give me a practice test"));
                    response.Replies.Add(new QuickReply(Localization.Get(lang, "chip.homework"), "What homework is due?"));
                    break;

                case Intents.AttendanceStatus:
                    BuildAttendance(response, state, student);
                    break;

                case Intents.AttendanceDetail:
                    BuildAttendanceLog(response, state, student);
                    break;

                case Intents.HomeworkList:
                    BuildHomework(response, state, student);
                    break;

                case Intents.MarksReport:
                    BuildMarks(response, state, student);
                    break;

                case Intents.NoticeList:
                    BuildNotices(response, state, student);
                    break;

                case Intents.Timetable:
                    BuildTimetable(response, state, student, intent);
                    break;

                case Intents.Transport:
                    BuildTransport(response, state, student);
                    break;

                case Intents.Library:
                    BuildLibrary(response, state, student);
                    break;

                case Intents.TeacherContact:
                    response.Html = Localization.Get(lang, "teacher.line", Escape(student.FirstName), Escape(student.ClassTeacher), Escape(student.ClassName));
                    response.Replies.Add(new QuickReply("📅 When is the PTM?", "When is the PTM?"));
                    break;

                case Intents.Meeting:
                    BuildMeeting(response, state, student);
                    break;

                case Intents.SwitchStudent:
                    BuildSwitchStudent(response, state, user, intent);
                    break;

                case Intents.SwitchLanguage:
                    BuildLanguageSwitch(response, state, intent);
                    break;

                case Intents.Transcript:
                    response.Html = Localization.Get(lang, "transcript");
                    response.Replies.Add(new QuickReply { Label = "⬇️ Download transcript", Action = "download", Argument = "transcript" });
                    break;

                default:
                    BuildFallback(response, state, student, intent, rawText);
                    break;
            }

            if (intent.Intent != Intents.PracticeRequest && intent.Intent != Intents.ExamSyllabus &&
                intent.Intent != Intents.PracticeAnswerKey)
            {
                state.ClearPending();
            }
        }

        // ------------------------------------------------------------------ builders

        private void BuildHelp(ChatResponse response, ConversationState state, Student student)
        {
            string lang = state.Language;
            var sb = new StringBuilder();
            sb.Append(Localization.Get(lang, "help.intro", Escape(student.Name), Escape(student.ClassName)));
            sb.Append("<ul class=\"bubble-list\">");
            sb.Append("<li><b>Fees</b> — balance, due date, head-wise breakdown, payment history, downloadable statement</li>");
            sb.Append("<li><b>Exams</b> — datesheet, venue, countdown, syllabus per subject</li>");
            sb.Append("<li><b>Practice</b> — generated papers by subject and difficulty, with an answer key</li>");
            sb.Append("<li><b>Attendance</b> — percentage, day-by-day log, how many days can still be missed</li>");
            sb.Append("<li><b>Homework, marks, notices, timetable, transport, library</b></li>");
            sb.Append("<li><b>Escalation</b> — raise a ticket with the school office at any time</li>");
            sb.Append("</ul>");
            sb.Append(Localization.Get(lang, "help.footer"));

            response.Html = sb.ToString();
            response.Replies.AddRange(DefaultChips(lang));
        }

        private void BuildFeeStatus(ChatResponse response, ConversationState state, Student student)
        {
            string lang = state.Language;
            FeeAccount fee = student.Fee;

            var sb = new StringBuilder();
            sb.Append(Localization.Get(lang, "fee.header", Escape(fee.Installment), Escape(student.Name)));
            sb.Append("<br>");
            sb.Append(Localization.Get(lang, "fee.paid", Money(fee.Paid), Money(fee.Total), fee.PercentPaid));

            if (fee.IsCleared)
            {
                sb.Append("<br>").Append(Localization.Get(lang, "fee.cleared"));
                response.Html = sb.ToString();
                response.Replies.Add(new QuickReply { Label = "⬇️ Download statement", Action = "download", Argument = "receipt" });
                response.Replies.Add(new QuickReply("Payment history", "Show my payment history"));
                return;
            }

            DateTime due = fee.DueDate ?? Clock.Today;
            int days = Clock.DaysUntil(due);
            sb.Append("<br>");
            if (days >= 0)
                sb.Append(Localization.Get(lang, "fee.remaining", Money(fee.Balance), FormatDate(due), days + " day(s) from now"));
            else
                sb.Append(Localization.Get(lang, "fee.overdue", Money(fee.Balance), FormatDate(due), Math.Abs(days)));

            sb.Append(ProgressBar(fee.PercentPaid));

            response.Html = sb.ToString();
            response.Replies.Add(new QuickReply("Fee breakdown", "Show the fee breakdown"));
            response.Replies.Add(new QuickReply { Label = "⬇️ Download statement", Action = "download", Argument = "receipt" });
            response.Replies.Add(new QuickReply("Payment history", "Show my payment history"));
        }

        private void BuildFeeBreakdown(ChatResponse response, ConversationState state, Student student)
        {
            FeeAccount fee = student.Fee;
            var card = new ChatCard
            {
                Kind = "table",
                Title = Localization.Get(state.Language, "fee.breakdown.title", Escape(fee.Installment), Escape(fee.AcademicYear)),
                Columns = new[] { "Head", "Amount" }
            };

            var rows = fee.Components
                          .Select(c => new[] { Escape(c.Head), Money(c.Amount) })
                          .ToList();
            rows.Add(new[] { "<b>Total</b>", "<b>" + Money(fee.Total) + "</b>" });
            card.Rows.AddRange(rows);

            response.Html = "<b>" + card.Title + "</b>" + Table(card.Columns, card.Rows);
            response.Cards.Add(card);
            response.Replies.Add(new QuickReply("Fee status", "What is my fee status?"));
            response.Replies.Add(new QuickReply { Label = "⬇️ Download statement", Action = "download", Argument = "receipt" });
        }

        private void BuildFeeHistory(ChatResponse response, ConversationState state, Student student)
        {
            string lang = state.Language;

            if (student.Payments.Count == 0)
            {
                response.Html = Localization.Get(lang, "fee.history.empty");
                return;
            }

            var rows = student.Payments
                              .OrderByDescending(p => p.Date)
                              .Select(p => new[] { FormatDate(p.Date), Money(p.Amount), Escape(p.Mode), Escape(p.ReceiptNo) })
                              .ToList();

            response.Html = "<b>" + Localization.Get(lang, "fee.history.title") + "</b>" +
                            Table(new[] { "Date", "Amount", "Mode", "Receipt" }, rows);
            response.Replies.Add(new QuickReply { Label = "⬇️ Download statement", Action = "download", Argument = "receipt" });
        }

        private void BuildExamSchedule(ChatResponse response, ConversationState state, Student student, IntentResult intent)
        {
            string lang = state.Language;
            string subject = intent.Entities.Get(EntityKeys.Subject);

            List<Exam> exams = student.Exams
                                      .Where(e => e.Date >= Clock.Today)
                                      .OrderBy(e => e.Date)
                                      .ToList();

            if (subject != null)
            {
                Exam single = exams.FirstOrDefault(e => string.Equals(e.Subject, subject, StringComparison.OrdinalIgnoreCase));
                if (single != null)
                {
                    response.Html = string.Format(India,
                        "The <b>{0}</b> paper is on <b>{1}</b> ({2} day(s) away), {3} session, in {4}.<br>Syllabus: {5}",
                        Escape(single.Subject), FormatDate(single.Date), Clock.DaysUntil(single.Date),
                        Escape(single.Session), Escape(single.Room), Escape(single.Syllabus));
                    response.Replies.Add(new QuickReply("Full datesheet", "Show the exam schedule"));
                    response.Replies.Add(new QuickReply("Practice paper", "Practice test " + single.Subject));
                    return;
                }
            }

            if (exams.Count == 0)
            {
                response.Html = Localization.Get(lang, "exam.none", Escape(student.ClassName));
                return;
            }

            var rows = exams.Select(e => new[]
            {
                Escape(e.Subject),
                FormatDate(e.Date),
                Clock.DaysUntil(e.Date).ToString(CultureInfo.InvariantCulture) + " d",
                Escape(e.Room)
            }).ToList();

            Exam next = exams[0];
            response.Html = "<b>" + Localization.Get(lang, "exam.title", Escape(student.ClassName)) + "</b>" +
                            Table(new[] { "Subject", "Date", "In", "Venue" }, rows) +
                            Localization.Get(lang, "exam.next", Escape(next.Subject), Clock.DaysUntil(next.Date),
                                             FormatDate(next.Date), Escape(next.Room));

            response.Replies.Add(new QuickReply("📖 Study tips", "Give me study tips"));
            response.Replies.Add(new QuickReply("📝 Practice paper", "Give me a practice test"));
            response.Replies.Add(new QuickReply("Syllabus", "What is the exam syllabus?"));
        }

        private void BuildSyllabus(ChatResponse response, ConversationState state, Student student, IntentResult intent)
        {
            string subject = intent.Entities.Get(EntityKeys.Subject) ?? state.LastSubject;

            if (subject == null)
            {
                response.Html = Localization.Get(state.Language, "syllabus.ask");
                response.Replies.AddRange(student.Exams.Select(e => new QuickReply(e.Subject, "Syllabus for " + e.Subject)));
                state.AwaitSlot(Intents.ExamSyllabus, EntityKeys.Subject);
                return;
            }

            Exam exam = student.Exams.FirstOrDefault(e => string.Equals(e.Subject, subject, StringComparison.OrdinalIgnoreCase));
            if (exam == null)
            {
                response.Html = string.Format(India, "I don't have a syllabus entry for <b>{0}</b> in {1}.",
                    Escape(subject), Escape(student.ClassName));
                return;
            }

            response.Html = string.Format(India, "<b>{0}</b><br>{1}<br><br>Paper on {2} in {3}, max marks {4}.",
                Localization.Get(state.Language, "syllabus.title", Escape(exam.Subject)),
                Escape(exam.Syllabus), FormatDate(exam.Date), Escape(exam.Room), exam.MaxMarks);

            response.Replies.Add(new QuickReply("📝 Practice paper", "Practice test " + exam.Subject));
            state.LastSubject = exam.Subject;
            state.ClearPending();
        }

        private void BuildPracticePaper(ChatResponse response, ConversationState state, Student student, IntentResult intent)
        {
            string lang = state.Language;
            IList<string> subjects = _practice.GetSubjects(student.Id);

            string requested = intent.Entities.Get(EntityKeys.Subject);
            if (requested == null && state.PendingIntent == Intents.PracticeRequest) requested = state.LastSubject;

            string subject = _practice.ResolveSubject(student.Id, requested);
            if (subject == null)
            {
                response.Html = Localization.Get(lang, "practice.ask", Escape(student.ClassName));
                response.Replies.AddRange(subjects.Select(s => new QuickReply(s, "Practice test " + s)));
                state.AwaitSlot(Intents.PracticeRequest, EntityKeys.Subject);
                return;
            }

            int? count = null;
            string countText = intent.Entities.Get(EntityKeys.Count);
            int parsedCount;
            if (countText != null && int.TryParse(countText, out parsedCount)) count = parsedCount;

            PracticePaper paper = _practice.Build(student, subject, intent.Entities.Get(EntityKeys.Difficulty), count);
            if (paper == null)
            {
                response.Html = Localization.Get(lang, "practice.none", Escape(subject), string.Join(", ", subjects));
                return;
            }

            var rows = new List<string[]>();
            for (int i = 0; i < paper.Questions.Count; i++)
            {
                PracticeQuestion q = paper.Questions[i];
                rows.Add(new[]
                {
                    (i + 1).ToString(CultureInfo.InvariantCulture),
                    Escape(q.Question),
                    q.Marks.ToString(CultureInfo.InvariantCulture)
                });
            }

            response.Html = "<b>" + Localization.Get(lang, "practice.header", Escape(paper.Subject), Escape(paper.ClassName)) + "</b><br>" +
                            Localization.Get(lang, "practice.meta", paper.DurationMinutes, paper.TotalMarks, Escape(paper.Difficulty)) +
                            Table(new[] { "#", "Question", "Marks" }, rows) +
                            Localization.Get(lang, "practice.footer");

            response.Replies.Add(new QuickReply { Label = "⬇️ Download paper", Action = "download", Argument = "paper:" + paper.Subject });
            response.Replies.Add(new QuickReply("✅ Answer key", "Answer key for " + paper.Subject));
            response.Replies.Add(new QuickReply("🎯 Harder set", "Give me a hard practice test in " + paper.Subject));

            state.LastPaperSubject = paper.Subject;
            state.LastSubject = paper.Subject;
            state.ClearPending();
        }

        private void BuildAnswerKey(ChatResponse response, ConversationState state, Student student, IntentResult intent)
        {
            string requested = intent.Entities.Get(EntityKeys.Subject) ?? state.LastPaperSubject ?? state.LastSubject;
            string subject = _practice.ResolveSubject(student.Id, requested);

            if (subject == null)
            {
                response.Html = Localization.Get(state.Language, "answerkey.ask");
                response.Replies.AddRange(_practice.GetSubjects(student.Id).Select(s => new QuickReply(s, "Answer key for " + s)));
                state.AwaitSlot(Intents.PracticeAnswerKey, EntityKeys.Subject);
                return;
            }

            PracticePaper paper = _practice.Build(student, subject, null, null);
            if (paper == null)
            {
                response.Html = Localization.Get(state.Language, "practice.none", Escape(subject),
                                                 string.Join(", ", _practice.GetSubjects(student.Id)));
                return;
            }

            var rows = new List<string[]>();
            for (int i = 0; i < paper.Questions.Count; i++)
            {
                rows.Add(new[]
                {
                    (i + 1).ToString(CultureInfo.InvariantCulture),
                    Escape(paper.Questions[i].Answer)
                });
            }

            response.Html = "<b>" + Localization.Get(state.Language, "answerkey.title", Escape(paper.Subject)) + "</b>" +
                            Table(new[] { "#", "Answer" }, rows);

            response.Replies.Add(new QuickReply { Label = "⬇️ Download paper + key", Action = "download", Argument = "paper:" + paper.Subject });
            state.LastPaperSubject = paper.Subject;
            state.ClearPending();
        }

        private void BuildAttendance(ChatResponse response, ConversationState state, Student student)
        {
            string lang = state.Language;
            Attendance attendance = student.Attendance;
            int percent = attendance.Percent;

            string note = percent >= 85
                ? Localization.Get(lang, "attendance.good")
                : percent >= 75
                    ? Localization.Get(lang, "attendance.ok")
                    : Localization.Get(lang, "attendance.low");

            var sb = new StringBuilder();
            sb.Append(Localization.Get(lang, "attendance.summary", Escape(student.Name), attendance.Present, attendance.Total, percent));
            sb.Append(' ').Append(note);
            sb.Append(ProgressBar(percent));

            if (percent >= 75)
                sb.Append("<br>").Append(Localization.Get(lang, "attendance.canmiss", attendance.DaysCanMiss(75)));

            response.Html = sb.ToString();
            response.Replies.Add(new QuickReply("Day-by-day log", "Show my attendance log"));
        }

        private void BuildAttendanceLog(ChatResponse response, ConversationState state, Student student)
        {
            var days = student.Attendance.RecentDays
                              .Where(d => !string.Equals(d.Status, "Holiday", StringComparison.OrdinalIgnoreCase))
                              .OrderByDescending(d => d.Date)
                              .ToList();

            if (days.Count == 0)
            {
                response.Html = "No attendance has been marked in the last two weeks.";
                return;
            }

            var rows = days.Select(d => new[]
            {
                d.Date.ToString("ddd, dd MMM", India),
                StatusBadge(d.Status),
                Escape(string.IsNullOrEmpty(d.Remark) ? "—" : d.Remark)
            }).ToList();

            int absent = days.Count(d => string.Equals(d.Status, "Absent", StringComparison.OrdinalIgnoreCase));

            response.Html = "<b>" + Localization.Get(state.Language, "attendance.log.title") + "</b>" +
                            Table(new[] { "Date", "Status", "Remark" }, rows) +
                            string.Format(India, "{0} absence(s) in this window.", absent);

            response.Replies.Add(new QuickReply("Attendance summary", "What is my attendance?"));
        }

        private void BuildHomework(ChatResponse response, ConversationState state, Student student)
        {
            string lang = state.Language;
            var pending = student.Homework.Where(h => !h.Submitted).OrderBy(h => h.Due).ToList();

            if (pending.Count == 0)
            {
                response.Html = Localization.Get(lang, "homework.none");
                return;
            }

            var rows = pending.Select(h => new[]
            {
                Escape(h.Subject),
                Escape(h.Task),
                FormatDue(h.Due)
            }).ToList();

            var sb = new StringBuilder();
            sb.Append("<b>").Append(Localization.Get(lang, "homework.title", Escape(student.FirstName))).Append("</b>");
            sb.Append(Table(new[] { "Subject", "Task", "Due" }, rows));

            int overdue = pending.Count(h => h.Due < Clock.Today);
            if (overdue > 0) sb.Append(Localization.Get(lang, "homework.overdue", overdue));

            response.Html = sb.ToString();
            response.Replies.Add(new QuickReply("📝 Practice paper", "Give me a practice test"));
        }

        private void BuildMarks(ChatResponse response, ConversationState state, Student student)
        {
            string lang = state.Language;

            var latest = student.Marks
                                .GroupBy(m => m.Subject, StringComparer.OrdinalIgnoreCase)
                                .Select(g => g.OrderByDescending(m => m.RecordedOn).First())
                                .OrderByDescending(m => m.Percent)
                                .ToList();

            if (latest.Count == 0)
            {
                response.Html = "No assessment results have been published yet.";
                return;
            }

            var rows = latest.Select(m => new[]
            {
                Escape(m.Subject),
                m.Score + "/" + m.MaxScore,
                m.Percent.ToString("0.#", India) + "%",
                m.Grade
            }).ToList();

            double average = Math.Round(latest.Average(m => m.Percent), 1);
            string band = average >= 85 ? "excellent" : average >= 70 ? "solid" : average >= 50 ? "fair" : "needs attention";

            var sb = new StringBuilder();
            sb.Append("<b>").Append(Localization.Get(lang, "marks.title")).Append("</b>");
            sb.Append(Table(new[] { "Subject", "Score", "%", "Grade" }, rows));
            sb.Append(Localization.Get(lang, "marks.average", average, band));
            sb.Append("<br>").Append(Localization.Get(lang, "marks.best",
                Escape(latest.First().Subject), Escape(latest.Last().Subject)));

            response.Html = sb.ToString();
            response.Replies.Add(new QuickReply("📝 Practice the weakest subject", "Practice test " + latest.Last().Subject));
            response.Replies.Add(new QuickReply("📖 Study tips", "Give me study tips"));
        }

        private void BuildNotices(ChatResponse response, ConversationState state, Student student)
        {
            var notices = _repository.GetNotices(student.Id).ToList();
            if (notices.Count == 0)
            {
                response.Html = Localization.Get(state.Language, "notice.none");
                return;
            }

            var sb = new StringBuilder();
            sb.Append("<b>").Append(Localization.Get(state.Language, "notice.title")).Append("</b>");
            sb.Append("<ul class=\"bubble-list\">");
            foreach (Notice notice in notices)
            {
                sb.AppendFormat(India, "<li>{0}<b>{1}</b> — {2} <span class=\"muted\">({3})</span></li>",
                    notice.Priority ? "📌 " : "",
                    Escape(notice.Title), Escape(notice.Body), FormatDate(notice.PostedOn));
            }
            sb.Append("</ul>");

            response.Html = sb.ToString();
        }

        private void BuildTimetable(ChatResponse response, ConversationState state, Student student, IntentResult intent)
        {
            DateTime day = Clock.Today;
            string timeframe = intent.Entities.Get(EntityKeys.TimeFrame);
            if (timeframe == "tomorrow") day = Clock.Today.AddDays(1);
            if (timeframe == "yesterday") day = Clock.Today.AddDays(-1);

            var periods = student.Timetable
                                 .Where(t => t.Day == day.DayOfWeek)
                                 .OrderBy(t => t.Period)
                                 .ToList();

            string label = day.ToString("dddd, dd MMM", India);
            if (periods.Count == 0)
            {
                response.Html = Localization.Get(state.Language, "timetable.none", label);
                return;
            }

            var rows = periods.Select(p => new[]
            {
                p.Period.ToString(CultureInfo.InvariantCulture),
                Escape(p.StartsAt),
                Escape(p.Subject)
            }).ToList();

            response.Html = "<b>" + Localization.Get(state.Language, "timetable.title", label) + "</b>" +
                            Table(new[] { "Period", "Starts", "Subject" }, rows);

            response.Replies.Add(new QuickReply("Tomorrow's timetable", "What is my timetable tomorrow?"));
        }

        private void BuildTransport(ChatResponse response, ConversationState state, Student student)
        {
            TransportInfo transport = student.Transport;
            response.Html = Localization.Get(state.Language, "transport.line",
                Escape(student.FirstName), Escape(transport.Summary), Escape(transport.DriverContact));
        }

        private void BuildLibrary(ChatResponse response, ConversationState state, Student student)
        {
            string lang = state.Language;
            if (student.Library.Count == 0)
            {
                response.Html = Localization.Get(lang, "library.none");
                return;
            }

            var sb = new StringBuilder();
            sb.Append("<b>").Append(Localization.Get(lang, "library.title")).Append("</b><ul class=\"bubble-list\">");
            foreach (LibraryLoan loan in student.Library)
            {
                int days = Clock.DaysUntil(loan.Due);
                string when = days >= 0 ? "in " + days + " day(s)" : Math.Abs(days) + " day(s) overdue";
                sb.Append("<li>").Append(Localization.Get(lang, "library.due",
                    Escape(loan.Title), Escape(loan.Author), FormatDate(loan.Due), when, Money(loan.FinePerDay))).Append("</li>");
            }
            sb.Append("</ul>");

            response.Html = sb.ToString();
        }

        private void BuildMeeting(ChatResponse response, ConversationState state, Student student)
        {
            Notice ptm = _repository.GetNotices(student.Id)
                                    .FirstOrDefault(n => n.Title.IndexOf("meeting", StringComparison.OrdinalIgnoreCase) >= 0);

            if (ptm == null)
            {
                response.Html = "No parent-teacher meeting is scheduled at the moment.";
                return;
            }

            response.Html = Escape(ptm.Body) + "<br>Class teacher: <b>" + Escape(student.ClassTeacher) + "</b>.";
            response.Replies.Add(new QuickReply("Show all notices", "Any notices?"));
        }

        private void BuildSwitchStudent(ChatResponse response, ConversationState state, UserAccount user, IntentResult intent)
        {
            if (user == null)
            {
                response.Html = Localization.Get(state.Language, "switch.denied");
                return;
            }

            string requested = intent.Entities.Get(EntityKeys.Child);
            if (requested == null)
            {
                response.Html = "Which child's records should I open?";
                foreach (string id in user.StudentIds)
                {
                    Student option = _repository.GetStudent(id);
                    if (option != null) response.Replies.Add(new QuickReply(option.Name, "Switch to " + option.FirstName));
                }
                return;
            }

            bool allowed = user.StudentIds.Any(id => string.Equals(id, requested, StringComparison.OrdinalIgnoreCase)) ||
                           user.Role == UserRoles.Admin || user.Role == UserRoles.Teacher;

            Student target = _repository.GetStudent(requested);
            if (!allowed || target == null)
            {
                response.Html = Localization.Get(state.Language, "switch.denied");
                return;
            }

            state.StudentId = target.Id;
            response.Html = Localization.Get(state.Language, "switch.done", Escape(target.Name), Escape(target.ClassName));
            response.StudentId = target.Id;
            response.Replies.AddRange(DefaultChips(state.Language));
        }

        private void BuildLanguageSwitch(ChatResponse response, ConversationState state, IntentResult intent)
        {
            string language = intent.Entities.Get(EntityKeys.Language) ?? Localization.English;
            state.Language = Localization.Normalize(language);

            response.Html = state.Language == Localization.Hindi
                ? Localization.Get(Localization.Hindi, "language.switched")
                : Localization.Get(Localization.English, "language.switched.en");

            response.Language = state.Language;
            response.Replies.AddRange(DefaultChips(state.Language));
        }

        private void BuildFallback(ChatResponse response, ConversationState state, Student student,
                                   IntentResult intent, string rawText)
        {
            string lang = state.Language;

            // Near misses become "did you mean" chips before anything else is tried.
            var suggestions = intent.Alternatives
                                    .Where(a => a.Score >= 0.2 && a.Intent != Intents.Fallback)
                                    .Take(3)
                                    .ToList();

            if (_ai.IsEnabled)
            {
                string answer = _ai.Complete(BuildAiSystemPrompt(student, state), rawText);
                if (!string.IsNullOrEmpty(answer))
                {
                    response.Html = Escape(answer).Replace("\n", "<br>");
                    response.Source = "ai";
                    response.Intent = Intents.Fallback;
                    response.Replies.AddRange(DefaultChips(lang).Take(3));
                    return;
                }
            }

            var sb = new StringBuilder();
            if (suggestions.Count > 0)
            {
                sb.Append(Localization.Get(lang, "fallback.didyoumean"));
                foreach (IntentMatch match in suggestions)
                    response.Replies.Add(new QuickReply(IntentEngine.Describe(match.Intent), IntentEngine.SampleQuery(match.Intent)));
            }
            else
            {
                sb.Append(Localization.Get(lang, "fallback"));
                response.Replies.AddRange(DefaultChips(lang).Take(4));
            }

            response.Html = sb.ToString();
            response.Suggestions = suggestions.Select(s => s.Intent).ToArray();
        }

        private void Escalate(ChatResponse response, ConversationState state, Student student, UserAccount user, string text)
        {
            SupportTicket ticket = _tickets.Create(
                student.Id,
                user != null ? user.UserName : "guest",
                state.Role,
                "assistant-escalation",
                text);

            response.Escalated = true;
            response.TicketId = ticket.Id;
            response.Intent = Intents.Escalate;
            response.Html = Localization.Get(state.Language, "escalate.created", Escape(ticket.Id)) +
                            "<br><span class=\"muted\">Reference this number when you call the office on the school's main line.</span>";
            response.Replies.Add(new QuickReply { Label = "⬇️ Download transcript", Action = "download", Argument = "transcript" });
        }

        private static string BuildAiSystemPrompt(Student student, ConversationState state)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are the EazyEdu school assistant. Answer briefly (max 4 sentences), politely and only from the record below.");
            sb.AppendLine("If the answer is not in the record, say you don't have that record and suggest contacting the school office.");
            sb.AppendLine("Never invent marks, fees or dates.");
            sb.AppendLine();
            sb.AppendLine("STUDENT RECORD");
            sb.AppendLine("Name: " + student.Name + " (" + student.ClassName + "), class teacher " + student.ClassTeacher);
            sb.AppendLine("Attendance: " + student.Attendance.Present + "/" + student.Attendance.Total + " (" + student.Attendance.Percent + "%)");
            sb.AppendLine("Fee: paid " + student.Fee.Paid + " of " + student.Fee.Total + ", balance " + student.Fee.Balance);
            sb.AppendLine("Next exams: " + string.Join("; ", student.Exams
                .Where(e => e.Date >= Clock.Today)
                .OrderBy(e => e.Date)
                .Take(4)
                .Select(e => e.Subject + " on " + e.Date.ToString("dd MMM yyyy", India))));
            sb.AppendLine("Pending homework: " + string.Join("; ", student.Homework
                .Where(h => !h.Submitted)
                .Select(h => h.Subject + " - " + h.Task)));
            sb.AppendLine("Transport: " + student.Transport.Summary);
            sb.AppendLine("Reply language: " + (state.Language == Localization.Hindi ? "Hindi" : "English"));
            return sb.ToString();
        }

        // ------------------------------------------------------------------ helpers

        public IList<QuickReply> DefaultChips(string language)
        {
            return new List<QuickReply>
            {
                new QuickReply(Localization.Get(language, "chip.fee"), "What is my fee status?"),
                new QuickReply(Localization.Get(language, "chip.exam"), "When are the exams?"),
                new QuickReply(Localization.Get(language, "chip.attendance"), "What is my attendance?"),
                new QuickReply(Localization.Get(language, "chip.practice"), "Give me a practice test"),
                new QuickReply(Localization.Get(language, "chip.homework"), "What homework is due?"),
                new QuickReply(Localization.Get(language, "chip.marks"), "Show my marks"),
                new QuickReply(Localization.Get(language, "chip.notice"), "Any notices?"),
                new QuickReply(Localization.Get(language, "chip.timetable"), "What is my timetable today?"),
                new QuickReply(Localization.Get(language, "chip.bus"), "What is my bus route?")
            };
        }

        private string LogTurn(string sessionId, UserAccount user, ConversationState state, string author,
                               string text, string intent, double confidence, string sentiment, long elapsedMs)
        {
            var turn = new ChatTurn
            {
                Id = Guid.NewGuid().ToString("N"),
                SessionId = sessionId,
                UserId = user != null ? user.Id : null,
                StudentId = state.StudentId,
                Role = state.Role,
                Author = author,
                Text = text,
                Intent = intent,
                Confidence = confidence,
                Sentiment = sentiment,
                TimestampUtc = DateTime.UtcNow,
                ElapsedMs = elapsedMs
            };
            _log.Add(turn);
            return turn.Id;
        }

        private static string Table(IEnumerable<string> columns, IEnumerable<string[]> rows)
        {
            var sb = new StringBuilder("<div class=\"table-wrap\"><table><thead><tr>");
            foreach (string column in columns) sb.Append("<th>").Append(column).Append("</th>");
            sb.Append("</tr></thead><tbody>");

            foreach (string[] row in rows)
            {
                sb.Append("<tr>");
                foreach (string cell in row) sb.Append("<td>").Append(cell).Append("</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table></div>");
            return sb.ToString();
        }

        private static string ProgressBar(int percent)
        {
            int clamped = Math.Max(0, Math.Min(100, percent));
            return string.Format(India,
                "<div class=\"bubble-bar\"><span style=\"width:{0}%\"></span></div>", clamped);
        }

        private static string StatusBadge(string status)
        {
            string css = string.Equals(status, "Present", StringComparison.OrdinalIgnoreCase) ? "ok" :
                         string.Equals(status, "Absent", StringComparison.OrdinalIgnoreCase) ? "bad" : "neutral";
            return "<span class=\"pill " + css + "\">" + Escape(status) + "</span>";
        }

        private static string FormatDue(DateTime due)
        {
            int days = Clock.DaysUntil(due);
            if (days == 0) return "<b>today</b>";
            if (days == 1) return "tomorrow";
            if (days < 0) return "<span class=\"pill bad\">" + Math.Abs(days) + "d late</span>";
            return FormatDate(due);
        }

        private static string DayPart()
        {
            int hour = Clock.Now.Hour;
            if (hour < 12) return "morning";
            return hour < 17 ? "afternoon" : "evening";
        }

        private static string FormatDate(DateTime date)
        {
            return date.ToString("d MMM yyyy", India);
        }

        private static string Money(decimal amount)
        {
            return "₹" + amount.ToString("N0", India);
        }

        /// <summary>HTML-encodes untrusted text before it is placed in a bubble.</summary>
        private static string Escape(string value)
        {
            return HttpUtility.HtmlEncode(value ?? string.Empty);
        }

        public static string ToPlainText(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;

            string withBreaks = html.Replace("</tr>", "\n").Replace("</li>", "\n").Replace("<br>", "\n").Replace("<br/>", "\n");
            string stripped = TagStripper.Replace(withBreaks, " ");
            stripped = HttpUtility.HtmlDecode(stripped);
            return Regex.Replace(stripped, @"[ \t]+", " ").Trim();
        }
    }
}
