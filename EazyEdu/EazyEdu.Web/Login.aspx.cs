using System;
using System.Web;
using System.Web.Security;
using EazyEdu.Web.Models;
using EazyEdu.Web.Security;
using EazyEdu.Web.Services;

namespace EazyEdu.Web
{
    public partial class LoginPage : System.Web.UI.Page
    {
        protected string ErrorMessage { get; private set; }
        protected string SubmittedUserName { get; private set; }
        protected string FormToken { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            SubmittedUserName = string.Empty;
            FormToken = AntiForgery.GetToken(Context);

            if (!string.Equals(Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
            {
                if (User != null && User.Identity.IsAuthenticated) Response.Redirect("~/Default.aspx", true);
                return;
            }

            SubmittedUserName = (Request.Form["userName"] ?? string.Empty).Trim();
            string password = Request.Form["password"] ?? string.Empty;
            bool remember = string.Equals(Request.Form["remember"], "1", StringComparison.Ordinal);

            if (!string.Equals(Request.Form["csrf"], FormToken, StringComparison.Ordinal))
            {
                ErrorMessage = "This form expired. Please try again.";
                return;
            }

            if (SubmittedUserName.Length == 0 || password.Length == 0)
            {
                ErrorMessage = "Enter both your user name and password.";
                return;
            }

            UserAccount account = AppServices.Auth.ValidateCredentials(SubmittedUserName, password);
            if (account == null)
            {
                ErrorMessage = "That user name and password don't match an account.";
                return;
            }

            AppServices.Auth.SignIn(Context, account, remember);

            // Only follow a same-site return url, never an absolute one supplied by a caller.
            string returnUrl = Request.QueryString["ReturnUrl"];
            if (!string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith("/", StringComparison.Ordinal) &&
                !returnUrl.StartsWith("//", StringComparison.Ordinal))
            {
                Response.Redirect(returnUrl, true);
                return;
            }

            Response.Redirect(FormsAuthentication.DefaultUrl, true);
        }
    }
}
