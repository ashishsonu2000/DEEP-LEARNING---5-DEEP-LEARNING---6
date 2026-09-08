using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using EazyEdu.Web.Models;
using EazyEdu.Web.Services;

namespace EazyEdu.Web.Api
{
    /// <summary>
    /// GET /Api/download.ashx?type=receipt|paper|transcript|analytics
    /// Files are generated on the server so the same document can be mailed or archived,
    /// unlike a browser-side blob.
    /// </summary>
    public class DownloadHandler : ApiHandlerBase
    {
        private static readonly Regex UnsafeFileChars = new Regex(@"[^A-Za-z0-9._-]+", RegexOptions.Compiled);

        protected override bool RequiresCsrf { get { return false; } }

        protected override void Handle(HttpContext context, UserAccount user)
        {
            ConversationState state = ConversationState.FromSession(context);
            SessionBootstrap.Apply(state, user, null, context.Request.QueryString["studentId"], null);
            ConversationState.Save(context, state);

            Student student = AppServices.Repository.GetStudent(state.StudentId);
            string type = (context.Request.QueryString["type"] ?? "receipt").ToLowerInvariant();

            string content;
            string fileName;
            string contentType = "text/plain; charset=utf-8";

            switch (type)
            {
                case "receipt":
                    if (student == null) { Fail(context, 404, "No student record to build a statement from."); return; }
                    content = AppServices.Documents.BuildFeeReceipt(student);
                    fileName = SafeName(student.Name) + "_fee_statement.txt";
                    break;

                case "paper":
                    if (student == null) { Fail(context, 404, "No student record to build a paper from."); return; }
                    string subject = AppServices.Practice.ResolveSubject(student.Id,
                        context.Request.QueryString["subject"] ?? state.LastPaperSubject);

                    if (subject == null) { Fail(context, 400, "Ask for a practice paper first, then download it."); return; }

                    PracticePaper paper = AppServices.Practice.Build(student, subject, context.Request.QueryString["difficulty"], null);
                    if (paper == null) { Fail(context, 404, "No question bank for that subject."); return; }

                    content = AppServices.Documents.BuildPracticePaper(paper, true);
                    fileName = SafeName(student.Name) + "_" + SafeName(subject) + "_practice_paper.txt";
                    break;

                case "transcript":
                    content = AppServices.Documents.BuildTranscript(
                        AppServices.ChatLog.GetSession(context.Session.SessionID), student);
                    fileName = "eazyedu_chat_transcript.txt";
                    break;

                case "analytics":
                    if (user.Role != UserRoles.Admin)
                    {
                        Fail(context, 403, "Only the school office can export analytics.");
                        return;
                    }
                    content = AppServices.Analytics.ExportCsv();
                    fileName = "eazyedu_chat_analytics.csv";
                    contentType = "text/csv; charset=utf-8";
                    break;

                default:
                    Fail(context, 400, "Unknown download type.");
                    return;
            }

            context.Response.Clear();
            context.Response.ContentType = contentType;
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.AddHeader("X-Content-Type-Options", "nosniff");
            context.Response.AddHeader("Content-Disposition", "attachment; filename=\"" + fileName + "\"");
            context.Response.Write(content);
        }

        private static string SafeName(string value)
        {
            string cleaned = UnsafeFileChars.Replace(value ?? "file", "_").Trim('_');
            return cleaned.Length == 0 ? "file" : cleaned;
        }
    }
}
