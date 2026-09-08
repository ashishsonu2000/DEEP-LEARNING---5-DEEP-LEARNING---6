using System;
using System.Diagnostics;
using System.Globalization;
using System.Security.Principal;
using System.Threading;
using System.Web;
using System.Web.Security;
using EazyEdu.Web.Services;

namespace EazyEdu.Web
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            AppServices.Initialize(Server.MapPath("~/App_Data"));
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            // Money and dates are Indian throughout the app.
            var culture = CultureInfo.GetCultureInfo("en-IN");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }

        /// <summary>
        /// Rehydrates the role stored in the forms ticket so &lt;allow roles="admin"&gt;
        /// in Web.config and User.IsInRole both work without a role provider.
        /// </summary>
        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
            HttpCookie cookie = Context.Request.Cookies[FormsAuthentication.FormsCookieName];
            if (cookie == null || string.IsNullOrEmpty(cookie.Value)) return;

            try
            {
                FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(cookie.Value);
                if (ticket == null || ticket.Expired) return;

                string[] roles = string.IsNullOrEmpty(ticket.UserData)
                    ? new string[0]
                    : ticket.UserData.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                Context.User = new GenericPrincipal(new FormsIdentity(ticket), roles);
            }
            catch (ArgumentException)
            {
                // Tampered or stale cookie: leave the request anonymous.
                FormsAuthentication.SignOut();
            }
        }

        protected void Application_EndRequest(object sender, EventArgs e)
        {
            try
            {
                HttpResponse response = Context.Response;
                response.AddHeader("X-Frame-Options", "SAMEORIGIN");
                response.AddHeader("X-Content-Type-Options", "nosniff");
                response.AddHeader("Referrer-Policy", "same-origin");
            }
            catch (HttpException)
            {
                // Headers already flushed (static file or an early write) — nothing to add.
            }
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            Exception error = Server.GetLastError();
            if (error != null) Trace.TraceError("EazyEdu unhandled error: " + error);
        }

        protected void Session_End(object sender, EventArgs e)
        {
        }
    }
}
