using System.Web;
using EazyEdu.Web.Models;
using EazyEdu.Web.Services;

namespace EazyEdu.Web.Api
{
    /// <summary>GET /Api/dashboard.ashx — sidebar state for the active student.</summary>
    public class DashboardHandler : ApiHandlerBase
    {
        protected override bool RequiresCsrf { get { return false; } }

        protected override void Handle(HttpContext context, UserAccount user)
        {
            ConversationState state = ConversationState.FromSession(context);
            SessionBootstrap.Apply(state, user,
                context.Request.QueryString["role"],
                context.Request.QueryString["studentId"],
                context.Request.QueryString["lang"]);
            ConversationState.Save(context, state);

            WriteJson(context, DashboardBuilder.Build(user, state));
        }
    }
}
