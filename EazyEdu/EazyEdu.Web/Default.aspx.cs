using System;
using System.Web.Script.Serialization;
using EazyEdu.Web.Models;
using EazyEdu.Web.Security;
using EazyEdu.Web.Services;

namespace EazyEdu.Web
{
    public partial class DefaultPage : System.Web.UI.Page
    {
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();

        protected string DisplayName { get; private set; }
        protected string RoleLabel { get; private set; }
        protected string TodayLabel { get; private set; }
        protected bool IsAdmin { get; private set; }

        // Pre-serialised JSON literals, so the bootstrap block cannot be broken by a stray quote.
        protected string ClientToken { get; private set; }
        protected string ClientRole { get; private set; }
        protected string ClientStudentId { get; private set; }
        protected string ClientLanguage { get; private set; }
        protected string ClientChatUrl { get; private set; }
        protected string ClientDashboardUrl { get; private set; }
        protected string ClientSessionUrl { get; private set; }
        protected string ClientFeedbackUrl { get; private set; }
        protected string ClientDownloadUrl { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            UserAccount user = AppServices.Auth.GetCurrentUser(Context);
            if (user == null)
            {
                Response.Redirect("~/Login.aspx", true);
                return;
            }

            ConversationState state = ConversationState.FromSession(Context);
            if (string.IsNullOrEmpty(state.Role)) state.Role = user.Role;
            if (string.IsNullOrEmpty(state.StudentId)) state.StudentId = AppServices.Auth.ResolveStudentId(user, null);
            if (string.IsNullOrEmpty(state.Language)) state.Language = Localization.English;
            ConversationState.Save(Context, state);

            DisplayName = user.DisplayName;
            RoleLabel = Describe(user.Role);
            TodayLabel = Clock.Today.ToString("ddd, dd MMM yyyy");
            IsAdmin = user.Role == UserRoles.Admin;

            ClientToken = Json.Serialize(AntiForgery.GetToken(Context));
            ClientRole = Json.Serialize(state.Role);
            ClientStudentId = Json.Serialize(state.StudentId);
            ClientLanguage = Json.Serialize(state.Language);
            ClientChatUrl = Json.Serialize(ResolveUrl("~/Api/chat.ashx"));
            ClientDashboardUrl = Json.Serialize(ResolveUrl("~/Api/dashboard.ashx"));
            ClientSessionUrl = Json.Serialize(ResolveUrl("~/Api/session.ashx"));
            ClientFeedbackUrl = Json.Serialize(ResolveUrl("~/Api/feedback.ashx"));
            ClientDownloadUrl = Json.Serialize(ResolveUrl("~/Api/download.ashx"));
        }

        private static string Describe(string role)
        {
            switch (role)
            {
                case UserRoles.Student: return "Student";
                case UserRoles.Parent: return "Parent";
                case UserRoles.Teacher: return "Class teacher";
                case UserRoles.Admin: return "School office";
                default: return role;
            }
        }
    }
}
