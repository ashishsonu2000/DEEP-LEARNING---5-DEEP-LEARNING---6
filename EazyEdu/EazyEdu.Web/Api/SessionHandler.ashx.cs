using System;
using System.Web;
using EazyEdu.Web.Models;
using EazyEdu.Web.Services;

namespace EazyEdu.Web.Api
{
    public class SessionActionRequest
    {
        public string Action { get; set; }
        public string Role { get; set; }
        public string StudentId { get; set; }
        public string Language { get; set; }
    }

    /// <summary>
    /// POST /Api/session.ashx — role switch, student switch, language switch and
    /// "clear chat". Each one returns a fresh dashboard so the UI stays in step.
    /// </summary>
    public class SessionHandler : ApiHandlerBase
    {
        protected override void Handle(HttpContext context, UserAccount user)
        {
            if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
            {
                Fail(context, 405, "Use POST for session changes.");
                return;
            }

            SessionActionRequest request = ReadJson<SessionActionRequest>(context);
            ConversationState state = ConversationState.FromSession(context);

            if (string.Equals(request.Action, "reset", StringComparison.OrdinalIgnoreCase))
            {
                string role = state.Role;
                string studentId = state.StudentId;
                string language = state.Language;

                ConversationState.Reset(context);
                state = ConversationState.FromSession(context);
                state.Role = role;
                state.StudentId = studentId;
                state.Language = language;
            }

            SessionBootstrap.Apply(state, user, request.Role, request.StudentId, request.Language);

            if (!string.IsNullOrEmpty(request.StudentId) && !AppServices.Auth.CanAccessStudent(user, request.StudentId))
            {
                Fail(context, 403, "That student isn't linked to your account.");
                return;
            }

            ConversationState.Save(context, state);
            WriteJson(context, DashboardBuilder.Build(user, state));
        }
    }
}
