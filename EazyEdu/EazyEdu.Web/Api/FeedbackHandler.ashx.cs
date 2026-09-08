using System;
using System.Web;
using EazyEdu.Web.Models;
using EazyEdu.Web.Services;

namespace EazyEdu.Web.Api
{
    public class FeedbackRequest
    {
        public string TurnId { get; set; }
        /// <summary>"up" or "down".</summary>
        public string Value { get; set; }
    }

    /// <summary>POST /Api/feedback.ashx — thumbs up/down on a bot answer, fed into analytics.</summary>
    public class FeedbackHandler : ApiHandlerBase
    {
        protected override void Handle(HttpContext context, UserAccount user)
        {
            if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
            {
                Fail(context, 405, "Use POST to record feedback.");
                return;
            }

            FeedbackRequest request = ReadJson<FeedbackRequest>(context);

            bool valid = string.Equals(request.Value, "up", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(request.Value, "down", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(request.TurnId) || !valid)
            {
                Fail(context, 400, "Send a turn id and a value of 'up' or 'down'.");
                return;
            }

            bool stored = AppServices.ChatLog.SetFeedback(request.TurnId, request.Value.ToLowerInvariant());
            WriteJson(context, new { ok = stored });
        }
    }
}
