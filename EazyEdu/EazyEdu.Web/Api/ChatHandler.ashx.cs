using System;
using System.Web;
using EazyEdu.Web.Models;
using EazyEdu.Web.Services;

namespace EazyEdu.Web.Api
{
    /// <summary>POST /Api/chat.ashx — one conversational turn.</summary>
    public class ChatHandler : ApiHandlerBase
    {
        private const int MaxMessageLength = 500;

        protected override void Handle(HttpContext context, UserAccount user)
        {
            if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
            {
                Fail(context, 405, "Use POST for chat messages.");
                return;
            }

            ChatRequest request = ReadJson<ChatRequest>(context);
            string message = (request.Message ?? string.Empty).Trim();

            if (message.Length == 0)
            {
                Fail(context, 400, "Type a question first.");
                return;
            }

            if (message.Length > MaxMessageLength)
                message = message.Substring(0, MaxMessageLength);

            request.Message = message;

            ConversationState state = ConversationState.FromSession(context);
            SessionBootstrap.Apply(state, user, request.Role, request.StudentId, request.Language);

            if (string.IsNullOrEmpty(state.StudentId))
            {
                Fail(context, 400, "No student record is linked to this account.");
                return;
            }

            ChatResponse response = AppServices.Chat.Respond(request, user, state, context.Session.SessionID);
            ConversationState.Save(context, state);

            WriteJson(context, response);
        }
    }
}
