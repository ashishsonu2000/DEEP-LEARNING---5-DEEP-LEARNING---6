using System;
using EazyEdu.Web.Services;

namespace EazyEdu.Web
{
    public partial class LogoutPage : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            AppServices.Auth.SignOut(Context);
            Response.Redirect("~/Login.aspx", true);
        }
    }
}
